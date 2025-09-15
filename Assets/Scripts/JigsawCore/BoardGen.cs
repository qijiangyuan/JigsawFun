using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameManager;

public class BoardGen : MonoBehaviour
{
    [Range(2, 50)] public int initialN = 5; // Inspector 可配置的默认 n
    private int mN; // 当前使用的 n

    private string imageFilename;
    Sprite mBaseSpriteOpaque;
    Sprite mBaseSpriteTransparent;

    GameObject mGameObjectOpaque;
    GameObject mGameObjectTransparent;

    public float ghostTransparency = 0.1f;

    // Jigsaw tiles creation.
    public int numTileX { get; private set; }
    public int numTileY { get; private set; }

    Tile[,] mTiles = null;
    GameObject[,] mTileGameObjects = null;

    public Transform parentForTiles = null;

    // Access to the menu.
    public Menu menu = null;
    private List<Rect> regions = new List<Rect>();
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    /// <summary>
    /// 加载源纹理，按左下对齐裁剪到可整除的 n*n 区域，并添加 padding，生成用于拼图的底图精灵：
    /// - 计算 tileSize = floor(min(W, H) / n)
    /// - usedSide = tileSize * n，左下对齐在源图上取 [0, usedSide)×[0, usedSide)（不缩放）
    /// - 创建新纹理尺寸为 (usedSide + 2*padding)，将裁剪区域复制到中心，并强制 alpha=255
    /// - 调用 Tile.SetTileSize(tileSize) 以更新运行时 tileSize 与曲线点集
    /// - 动态计算 padding：按 tileSize 比例调整，避免大尺寸时曲线（凸出/凹入）被裁剪
    /// </summary>
    Sprite LoadBaseTexture(int n)
    {
        Texture2D tex = SpriteUtils.LoadTexture(imageFilename);
        if (tex == null)
        {
            Debug.LogError("Error: Texture not found: " + imageFilename);
            return null;
        }
        if (!tex.isReadable)
        {
            Debug.Log("Error: Texture is not readable");
            return null;
        }

        int srcW = tex.width;
        int srcH = tex.height;

        // 允许任意正方形；若非正方形则先等中心裁剪为正方形基底
        int side = Mathf.Min(srcW, srcH);

        // 计算 tileSize 与最终使用边长
        int tileSize = Mathf.FloorToInt((float)side / Mathf.Max(1, n));
        if (tileSize <= 0)
        {
            Debug.LogError($"Error: tileSize <= 0. side={side}, n={n}. Please choose a smaller n or larger image.");
            return null;
        }

        // 动态计算 padding：随 tileSize 增长，确保凹凸曲线完全包含在 finalCut 纹理内
        // 比例经验值 0.28，可根据美术模板幅度微调；并进行安全钳制避免过大/过小
        int dynPadding = Mathf.RoundToInt(tileSize * 0.28f);
        dynPadding = Mathf.Clamp(dynPadding, 3, Mathf.Max(6, tileSize / 2 - 1));
        Tile.padding = dynPadding;

        // 为了与原逻辑兼容（numTileX = baseTex.width / tileSize 取整），建议 tileSize > padding
        if (tileSize <= Tile.padding)
        {
            Debug.LogWarning($"Warning: tileSize({tileSize}) <= padding({Tile.padding}). Consider using a smaller n or reducing padding.");
        }

        int usedSide = tileSize * n; // 左下对齐可整除区域

        // 计算在源图上的起点（左下对齐）
        int startX = 0;
        int startY = 0;

        // 创建带 padding 的新纹理
        Texture2D newTex = new Texture2D(
            usedSide + Tile.padding * 2,
            usedSide + Tile.padding * 2,
            TextureFormat.ARGB32,
            false);
        // 为避免左/下 1px 因半像素/双线性采样被视觉上“吃掉”，使用点采样与 Clamp 环绕
        newTex.filterMode = FilterMode.Point;
        newTex.wrapMode = TextureWrapMode.Clamp;

        int dstW = newTex.width;
        int dstH = newTex.height;

        // 调试日志：输出关键参数，便于确认是否为左下对齐裁剪
        Debug.Log($"[LoadBaseTexture Debug] src=({srcW}x{srcH}), n={n}, tileSize={tileSize}, usedSide={usedSide}, start=({startX},{startY}), padding={Tile.padding}, dst=({dstW}x{dstH})");

        // 目标缓冲初始化为白色
        Color32[] dst = new Color32[dstW * dstH];
        Color32 white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < dst.Length; i++) dst[i] = white;

        // 源像素一次性读取
        Color32[] src = tex.GetPixels32();

        // 将左下对齐裁剪区域拷贝到带 padding 的中心
        for (int y = 0; y < usedSide; y++)
        {
            int srcRow = (y + startY) * srcW;
            int dstRow = (y + Tile.padding) * dstW + Tile.padding;
            for (int x = 0; x < usedSide; x++)
            {
                Color32 c = src[srcRow + (x + startX)];
                c.a = 255;
                dst[dstRow + x] = c;
            }
        }

        // 调试辅助边框绘制已移除（保持成品无彩色辅助线）

        newTex.SetPixels32(dst);
        newTex.Apply();

        // 更新运行时 tileSize 与曲线点集
        Tile.SetTileSize(tileSize);

        Sprite sprite = SpriteUtils.CreateSpriteFromTexture2D(
            newTex,
            0,
            0,
            newTex.width,
            newTex.height);
        return sprite;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (GameManager.Instance == null)
        {
            //默认测试
            imageFilename = GameApp.Instance.GetJigsawImageName();
            mN = Mathf.Max(2, initialN);
            mBaseSpriteOpaque = LoadBaseTexture(mN);
        }
        else
        {
            //从gamemanger里面获取拼图数据
            GameData currentData = GameManager.Instance.currentGameData;
            imageFilename = "Images/PuzzleImages/Nature/" + currentData.selectedImage.name;
            mN = currentData.difficulty;
            //mBaseSpriteOpaque = currentData.selectedImage;
            mBaseSpriteOpaque = LoadBaseTexture(mN);

            Debug.Log($"从游戏数据生成拼图: {currentData.imageName}, 难度={mN}x{mN}");
        }

        mGameObjectOpaque = new GameObject();
        mGameObjectOpaque.name = imageFilename + "_Opaque";
        mGameObjectOpaque.AddComponent<SpriteRenderer>().sprite = mBaseSpriteOpaque;
        mGameObjectOpaque.GetComponent<SpriteRenderer>().sortingLayerName = "Opaque";

        mBaseSpriteTransparent = CreateTransparentView(mBaseSpriteOpaque.texture);
        mGameObjectTransparent = new GameObject();
        mGameObjectTransparent.name = imageFilename + "_Transparent";
        mGameObjectTransparent.AddComponent<SpriteRenderer>().sprite = mBaseSpriteTransparent;
        mGameObjectTransparent.GetComponent<SpriteRenderer>().sortingLayerName = "Transparent";

        mGameObjectOpaque.gameObject.SetActive(false);

        SetCameraPosition();

        // Create the Jigsaw tiles.
        StartCoroutine(Coroutine_CreateJigsawTiles());
    }



    /// <summary>
    /// 基于不透明底图生成“透明视图”（中间区域降低 alpha，四周边框保持原始像素），
    /// 使用 GetPixels32 + SetPixels32 批处理，避免逐像素 API 调用。
    /// </summary>
    Sprite CreateTransparentView(Texture2D tex)
    {
        Texture2D newTex = new Texture2D(
          tex.width,
          tex.height,
          TextureFormat.ARGB32,
          false);
        // 保持与底图一致的取样配置，避免边缘像素在透明视图中被采样丢失
        newTex.filterMode = FilterMode.Point;
        newTex.wrapMode = TextureWrapMode.Clamp;

        int w = newTex.width;
        int h = newTex.height;

        // 拷贝源像素到目标缓冲
        Color32[] buf = tex.GetPixels32();

        // 计算内圈区域的 alpha（0..1 -> 0..255）
        byte ghostA = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(ghostTransparency) * 255f), 0, 255);

        // 仅对内圈区域（去掉 padding 的中心）修改 alpha
        for (int y = Tile.padding; y < h - Tile.padding; y++)
        {
            int row = y * w;
            for (int x = Tile.padding; x < w - Tile.padding; x++)
            {
                int idx = row + x;
                Color32 c = buf[idx];
                c.a = ghostA;
                buf[idx] = c;
            }
        }

        newTex.SetPixels32(buf);
        newTex.Apply();

        Sprite sprite = SpriteUtils.CreateSpriteFromTexture2D(
          newTex,
          0,
          0,
          newTex.width,
          newTex.height);
        return sprite;
    }

    void SetCameraPosition()
    {
        // 计算中心点
        float cx = mBaseSpriteOpaque.texture.width / 2f;
        float cy = mBaseSpriteOpaque.texture.height / 2f;

        // 若宽/高为偶数像素，中心落在像素间隙，+0.5f 可减少半像素采样导致的边缘1px丢失
        if ((mBaseSpriteOpaque.texture.width & 1) == 0) cx += 0.5f;
        if ((mBaseSpriteOpaque.texture.height & 1) == 0) cy += 0.5f;

        Camera.main.transform.position = new Vector3(cx, cy, -10.0f);

        // 视野设置保守放大，确保整张底图在视野内
        int smaller_value = Mathf.Min(mBaseSpriteOpaque.texture.width, mBaseSpriteOpaque.texture.height);
        Camera.main.orthographicSize = smaller_value * 0.8f;
    }

    public static GameObject CreateGameObjectFromTile(Tile tile)
    {
        GameObject obj = new GameObject();

        obj.name = "TileGameObe_" + tile.xIndex.ToString() + "_" + tile.yIndex.ToString();

        /// <summary>
        /// 放置拼图块 GameObject（与内容网格对齐）。
        /// 说明：tile.finalCut 的 sprite 尺寸为 tileSize + 2*padding，pivot 为 (0,0)（左下角）。
        /// 内容区域（去除四周 padding）在 sprite 本地坐标系中的左下角为 (padding, padding)。
        /// 底图的内容区域在世界坐标中的该 tile 起点为 baseX = padding + xIndex*tileSize，baseY = padding + yIndex*tileSize。
        /// 因此，将 GameObject 放在 (xIndex*tileSize, yIndex*tileSize)，使得：
        ///  世界上的内容左下角 = 物体位置 + (padding, padding) = (padding + xIndex*tileSize, padding + yIndex*tileSize) = (baseX, baseY)
        /// 这与底图像素严格对齐，无需额外的 -padding 偏移。
        /// </summary>
        obj.transform.position = new Vector3(tile.xIndex * Tile.tileSize, tile.yIndex * Tile.tileSize, 0.0f);

        SpriteRenderer spriteRenderer = obj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteUtils.CreateSpriteFromTexture2D(
          tile.finalCut,
          0,
          0,
          Tile.padding * 2 + Tile.tileSize,
          Tile.padding * 2 + Tile.tileSize);

        BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
        // 将碰撞体限定为拼图块的有效区域（tileSize×tileSize），忽略四周 padding
        // 由于 Sprite pivot 为 (0,0)，碰撞体中心应位于 (padding + tileSize/2, padding + tileSize/2)
        box.size = new Vector2(Tile.tileSize, Tile.tileSize);
        box.offset = new Vector2(Tile.padding + Tile.tileSize * 0.5f, Tile.padding + Tile.tileSize * 0.5f);

        TileMovement tileMovement = obj.AddComponent<TileMovement>();
        tileMovement.tile = tile;

        return obj;
    }

    /// <summary>
    /// 生成 n×n 的拼图（同步版本）。
    /// 依据当前 mN（n）与 mBaseSpriteOpaque.texture 创建 mTiles 与对应 GameObject。
    /// </summary>
    void CreateJigsawTiles()
    {
        Texture2D baseTexture = mBaseSpriteOpaque.texture;

        numTileX = mN;
        numTileY = mN;

        mTiles = new Tile[numTileX, numTileY];
        mTileGameObjects = new GameObject[numTileX, numTileY];

        for (int i = 0; i < numTileX; i++)
        {
            for (int j = 0; j < numTileY; j++)
            {
                mTiles[i, j] = CreateTile(i, j, baseTexture);
                mTileGameObjects[i, j] = CreateGameObjectFromTile(mTiles[i, j]);
                if (parentForTiles != null)
                {
                    mTileGameObjects[i, j].transform.SetParent(parentForTiles);
                }
            }
        }

        // Enable the bottom panel and set the onlcick delegate to the play button.
        menu.SetEnableBottomPanel(true);
        menu.btnPlayOnClick = ShuffleTiles;
    }

    /// <summary>
    /// 生成 n×n 的拼图（逐帧协程版本）。
    /// 依据当前 mN（n）与 mBaseSpriteOpaque.texture 创建 mTiles 与对应 GameObject，并在每个 tile 生成后 yield。
    /// </summary>
    IEnumerator Coroutine_CreateJigsawTiles()
    {
        Texture2D baseTexture = mBaseSpriteOpaque.texture;

        numTileX = mN;
        numTileY = mN;

        mTiles = new Tile[numTileX, numTileY];
        mTileGameObjects = new GameObject[numTileX, numTileY];

        for (int i = 0; i < numTileX; i++)
        {
            for (int j = 0; j < numTileY; j++)
            {
                mTiles[i, j] = CreateTile(i, j, baseTexture);
                mTileGameObjects[i, j] = CreateGameObjectFromTile(mTiles[i, j]);
                if (parentForTiles != null)
                {
                    mTileGameObjects[i, j].transform.SetParent(parentForTiles);
                }

                yield return null;
            }
        }

        // Enable the bottom panel and set the delegate to button play on click.
        menu.SetEnableBottomPanel(true);
        menu.btnPlayOnClick = ShuffleTiles;

    }


    Tile CreateTile(int i, int j, Texture2D baseTexture)
    {
        Tile tile = new Tile(baseTexture);
        tile.xIndex = i;
        tile.yIndex = j;

        // Left side tiles.
        if (i == 0)
        {
            tile.SetCurveType(Tile.Direction.LEFT, Tile.PosNegType.NONE);
        }
        else
        {
            // We have to create a tile that has LEFT direction opposite curve type.
            Tile leftTile = mTiles[i - 1, j];
            Tile.PosNegType rightOp = leftTile.GetCurveType(Tile.Direction.RIGHT);
            tile.SetCurveType(Tile.Direction.LEFT, rightOp == Tile.PosNegType.NEG ?
              Tile.PosNegType.POS : Tile.PosNegType.NEG);
        }

        // Bottom side tiles
        if (j == 0)
        {
            tile.SetCurveType(Tile.Direction.DOWN, Tile.PosNegType.NONE);
        }
        else
        {
            Tile downTile = mTiles[i, j - 1];
            Tile.PosNegType upOp = downTile.GetCurveType(Tile.Direction.UP);
            tile.SetCurveType(Tile.Direction.DOWN, upOp == Tile.PosNegType.NEG ?
              Tile.PosNegType.POS : Tile.PosNegType.NEG);
        }

        // Right side tiles.
        if (i == numTileX - 1)
        {
            tile.SetCurveType(Tile.Direction.RIGHT, Tile.PosNegType.NONE);
        }
        else
        {
            float toss = UnityEngine.Random.Range(0f, 1f);
            if (toss < 0.5f)
            {
                tile.SetCurveType(Tile.Direction.RIGHT, Tile.PosNegType.POS);
            }
            else
            {
                tile.SetCurveType(Tile.Direction.RIGHT, Tile.PosNegType.NEG);
            }
        }

        // Up side tile.
        if (j == numTileY - 1)
        {
            tile.SetCurveType(Tile.Direction.UP, Tile.PosNegType.NONE);
        }
        else
        {
            float toss = UnityEngine.Random.Range(0f, 1f);
            if (toss < 0.5f)
            {
                tile.SetCurveType(Tile.Direction.UP, Tile.PosNegType.POS);
            }
            else
            {
                tile.SetCurveType(Tile.Direction.UP, Tile.PosNegType.NEG);
            }
        }

        tile.Apply();
        return tile;
    }

    private int currentIndex = 0;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleNextTile();
        }
    }

    private void HandleNextTile()
    {
        int total = numTileX * numTileY;

        // 计算 i, j
        int i = currentIndex % numTileX;
        int j = currentIndex / numTileX;

        // 获取 TileMovement
        TileMovement tm = mTileGameObjects[i, j].GetComponent<TileMovement>();
        if (tm != null)
        {
            tm.SnapToCorretPosition();
            Debug.Log($"Tile[{i},{j}] 已复位");
        }

        // 移动到下一个
        currentIndex = (currentIndex + 1) % total;
    }

    #region Shuffling related codes

    private IEnumerator Coroutine_MoveOverSeconds(GameObject objectToMove, Vector3 end, float seconds)
    {
        float elaspedTime = 0.0f;
        Vector3 startingPosition = objectToMove.transform.position;
        while (elaspedTime < seconds)
        {
            objectToMove.transform.position = Vector3.Lerp(
              startingPosition, end, (elaspedTime / seconds));
            elaspedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
        objectToMove.transform.position = end;
    }

    void Shuffle(GameObject obj)
    {
        if (regions.Count == 0)
        {
            regions.Add(new Rect(-300.0f, -100.0f, 50.0f, numTileY * Tile.tileSize));
            regions.Add(new Rect((numTileX + 1) * Tile.tileSize, -100.0f, 50.0f, numTileY * Tile.tileSize));
        }

        int regionIndex = UnityEngine.Random.Range(0, regions.Count);
        float x = UnityEngine.Random.Range(regions[regionIndex].xMin, regions[regionIndex].xMax);
        float y = UnityEngine.Random.Range(regions[regionIndex].yMin, regions[regionIndex].yMax);

        Vector3 pos = new Vector3(x, y, 0.0f);
        Coroutine moveCoroutine = StartCoroutine(Coroutine_MoveOverSeconds(obj, pos, 1.0f));
        activeCoroutines.Add(moveCoroutine);
    }

    IEnumerator Coroutine_Shuffle()
    {
        for (int i = 0; i < numTileX; ++i)
        {
            for (int j = 0; j < numTileY; ++j)
            {
                Shuffle(mTileGameObjects[i, j]);
                yield return null;
            }
        }

        foreach (var item in activeCoroutines)
        {
            if (item != null)
            {
                yield return null;
            }
        }

        OnFinishedShuffling();
    }

    public void ShuffleTiles()
    {
        StartCoroutine(Coroutine_Shuffle());
    }

    void OnFinishedShuffling()
    {
        activeCoroutines.Clear();

        menu.SetEnableBottomPanel(false);
        StartCoroutine(Coroutine_CallAfterDelay(() => menu.SetEnableTopPanel(true), 1.0f));
        GameApp.Instance.TileMovementEnabled = true;

        StartTimer();

        for (int i = 0; i < numTileX; ++i)
        {
            for (int j = 0; j < numTileY; ++j)
            {
                TileMovement tm = mTileGameObjects[i, j].GetComponent<TileMovement>();
                tm.onTileInPlace += OnTileInPlace;
                SpriteRenderer spriteRenderer = tm.gameObject.GetComponent<SpriteRenderer>();
                Tile.tilesSorting.BringToTop(spriteRenderer);
            }
        }

        menu.SetTotalTiles(numTileX * numTileY);
    }

    IEnumerator Coroutine_CallAfterDelay(System.Action function, float delay)
    {
        yield return new WaitForSeconds(delay);
        function();
    }


    public void StartTimer()
    {
        StartCoroutine(Coroutine_Timer());
    }

    IEnumerator Coroutine_Timer()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            GameApp.Instance.SecondsSinceStart += 1;

            menu.SetTimeInSeconds(GameApp.Instance.SecondsSinceStart);
        }
    }

    public void StopTimer()
    {
        StopCoroutine(Coroutine_Timer());
    }

    #endregion

    public void ShowOpaqueImage()
    {
        mGameObjectOpaque.SetActive(true);
    }

    public void HideOpaqueImage()
    {
        mGameObjectOpaque.SetActive(false);
    }

    /// <summary>
    /// 依据给定的 n（n×n）在运行时重建棋盘：
    /// - 终止正在进行的协程与拖拽
    /// - 销毁旧的 Tile GameObject 与纹理，清空数组
    /// - 重新按中心裁剪方案加载底图并生成透明视图
    /// - 重置计时与计数 UI，更新相机位置
    /// - 以新的 n 启动瓦片生成协程
    /// </summary>
    public void Generate(int n)
    {
        // 约束 n 的取值范围
        mN = Mathf.Clamp(n, 2, 50);

        // 停止已有协程（如洗牌移动、计时等）
        StopAllCoroutines();
        activeCoroutines.Clear();

        GameApp.Instance.TileMovementEnabled = false;

        // 销毁旧的瓦片对象与纹理
        if (mTileGameObjects != null)
        {
            for (int i = 0; i < mTileGameObjects.GetLength(0); i++)
            {
                for (int j = 0; j < mTileGameObjects.GetLength(1); j++)
                {
                    if (mTileGameObjects[i, j] != null)
                    {
                        Destroy(mTileGameObjects[i, j]);
                        mTileGameObjects[i, j] = null;
                    }
                }
            }
        }
        if (mTiles != null)
        {
            for (int i = 0; i < mTiles.GetLength(0); i++)
            {
                for (int j = 0; j < mTiles.GetLength(1); j++)
                {
                    if (mTiles[i, j] != null)
                    {
                        // 销毁运行时创建的最终切图纹理
                        if (mTiles[i, j].finalCut != null)
                        {
                            Destroy(mTiles[i, j].finalCut);
                        }
                        mTiles[i, j] = null;
                    }
                }
            }
        }
        mTiles = null;
        mTileGameObjects = null;

        // 重新加载底图（中心裁剪 + padding），并更新透明视图
        var srOpaque = mGameObjectOpaque.GetComponent<SpriteRenderer>();
        var srTransp = mGameObjectTransparent.GetComponent<SpriteRenderer>();

        // 销毁旧 Sprite 以释放旧纹理（仅限运行时创建的）
        if (srOpaque.sprite != null)
        {
            var oldTex = srOpaque.sprite.texture;
            srOpaque.sprite = null;
            if (oldTex != null) Destroy(oldTex);
        }
        if (srTransp.sprite != null)
        {
            var oldTex = srTransp.sprite.texture;
            srTransp.sprite = null;
            if (oldTex != null) Destroy(oldTex);
        }

        mBaseSpriteOpaque = LoadBaseTexture(mN);
        srOpaque.sprite = mBaseSpriteOpaque;

        mBaseSpriteTransparent = CreateTransparentView(mBaseSpriteOpaque.texture);
        srTransp.sprite = mBaseSpriteTransparent;

        // 更新相机与 UI
        SetCameraPosition();

        GameApp.Instance.SecondsSinceStart = 0;
        GameApp.Instance.TotalTilesInCorrectPosition = 0;
        menu.SetEnableTopPanel(false);
        menu.SetEnableBottomPanel(false);
        menu.SetTilesInPlace(0);
        menu.SetTotalTiles(mN * mN);

        // 重新生成瓦片
        StartCoroutine(Coroutine_CreateJigsawTiles());
    }

    /// <summary>
    /// UI 事件：从 Slider 传入的 n（float）触发重建。
    /// 典型绑定：Slider.onValueChanged -> BoardGen.UI_GenerateFromSlider
    /// </summary>
    public void UI_GenerateFromSlider(float value)
    {
        int n = Mathf.RoundToInt(value);
        Generate(n);
    }

    /// <summary>
    /// UI 事件：从 Dropdown/Stepper 传入的 n（int）触发重建。
    /// 典型绑定：Dropdown.onValueChanged(int) -> 将实际显示值映射后调用本方法。
    /// </summary>
    public void UI_GenerateFromInt(int n)
    {
        Generate(n);
    }
    void OnTileInPlace(TileMovement tm)
    {
        GameApp.Instance.TotalTilesInCorrectPosition += 1;

        tm.enabled = false;
        Destroy(tm);

        SpriteRenderer spriteRenderer = tm.gameObject.GetComponent<SpriteRenderer>();
        Tile.tilesSorting.Remove(spriteRenderer);

        if (GameApp.Instance.TotalTilesInCorrectPosition == mTileGameObjects.Length)
        {
            //Debug.Log("Game completed. We will implement an end screen later");
            menu.SetEnableTopPanel(false);
            menu.SetEnableGameCompletionPanel(true);

            // Reset the values.
            GameApp.Instance.SecondsSinceStart = 0;
            GameApp.Instance.TotalTilesInCorrectPosition = 0;
        }
        menu.SetTilesInPlace(GameApp.Instance.TotalTilesInCorrectPosition);
    }
}
