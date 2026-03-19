using Patterns;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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

    // 相机交互参数（可在 Inspector 中配置）
    [Header("相机缩放与拖拽")]
    [Tooltip("最小正交尺寸（越小越近）")]
    public float minOrthoSize = 5f;
    [Tooltip("最大正交尺寸（越大越远）")]
    public float maxOrthoSize = 200f;
    [Tooltip("鼠标滚轮缩放速度（编辑器/PC）")]
    public float zoomSpeedMouse = 5f;
    [Tooltip("双指捏合缩放速度（移动端），以每像素距离变化对应的缩放量")]
    public float zoomSpeedTouch = 0.01f;
    [Tooltip("拖拽摄像机灵敏度（值越大相机移动越快）")]
    public float panSpeed = 1f;
    [Header("适配")]
    public float boardEdgeMargin = 0.02f;
    public bool fitToPuzzleArea = true;
    public float minBoardEdgeMarginWhenFitting = 0.25f;
    [Header("结算")]
    private int tilesInPlace = 0;
    private int tilesTotal = 0;
    public bool cameraInteractionEnabled = false;
    [Header("Board Appearance")]
    [Tooltip("拼图底图显示白边相对于单块边长的系数（仅影响底图显示，不影响拼图块榫卯）")]
    [Range(0f, 0.12f)]
    public float paddingFactor = 0.06f;
    [Tooltip("拼图块生成所需的 padding 系数（影响榫卯空间）。过小会导致拼图块被裁切/破坏。")]
    [Range(0.18f, 0.35f)]
    public float piecePaddingFactor = 0.28f;
    [Tooltip("仅用于底图显示的白边裁剪像素（不影响拼图块蒙版/位置）")]
    [Range(0, 200)]
    public int baseBorderCrop = 0;
    public bool useDifficultyPadding = true;
    public int difficultyMin = 3;
    public int difficultyMax = 12;
    public AnimationCurve paddingByDifficulty = AnimationCurve.Linear(0f, 0.06f, 1f, 0.1f);
    public Color borderColor = new Color(2f / 255f, 191f / 255f, 196f / 255f, 1f);

   
    // 运行时状态
    private bool isPanning = false;
    private bool pinching = false;
    private bool panDisabledDueToTile = false; // 起手点在 TileMovement 上则禁用拖拽
    private Vector3 lastPanWorldPos;
    // 平移微抖阈值，避免极小抖动
    private const float panEpsilon = 1e-4f;
    private int runtimeDisplayCropPixels = 0;

    /// <summary>
    /// 处理给定的纹理，按左下对齐裁剪到可整除的 n*n 区域，并添加 padding，生成用于拼图的底图精灵
    /// </summary>
    Sprite ProcessBaseTexture(Texture2D tex, int n)
    {
        if (tex == null)
        {
            Debug.LogError("Error: Texture is null");
            return null;
        }
        tex = GetReadableTexture(tex);

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

        // 动态计算拼图块生成所需 padding（用于榫卯空间）
        int dynPadding = Mathf.RoundToInt(tileSize * Mathf.Clamp01(piecePaddingFactor));
        dynPadding = Mathf.Clamp(dynPadding, 6, Mathf.Max(10, tileSize / 3 - 1));
        Tile.padding = dynPadding;

        int desiredBorderPx = Mathf.RoundToInt(tileSize * Mathf.Clamp01(paddingFactor));
        desiredBorderPx = Mathf.Clamp(desiredBorderPx, 0, Tile.padding - 1);
        int computedCrop = Tile.padding - desiredBorderPx;
        runtimeDisplayCropPixels = baseBorderCrop > 0 ? baseBorderCrop : Mathf.Clamp(computedCrop, 0, Tile.padding - 1);

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
        newTex.filterMode = FilterMode.Point;
        newTex.wrapMode = TextureWrapMode.Clamp;

        int dstW = newTex.width;
        int dstH = newTex.height;

        Debug.Log($"[ProcessBaseTexture Debug] src=({srcW}x{srcH}), n={n}, tileSize={tileSize}, usedSide={usedSide}, start=({startX},{startY}), padding={Tile.padding}, dst=({dstW}x{dstH})");

        // 目标缓冲初始化为白边颜色
        Color32[] dst = new Color32[dstW * dstH];
        byte br = (byte)Mathf.RoundToInt(Mathf.Clamp01(borderColor.r) * 255f);
        byte bg = (byte)Mathf.RoundToInt(Mathf.Clamp01(borderColor.g) * 255f);
        byte bb = (byte)Mathf.RoundToInt(Mathf.Clamp01(borderColor.b) * 255f);
        Color32 borderC = new Color32(br, bg, bb, 255);
        for (int i = 0; i < dst.Length; i++) dst[i] = borderC;

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

    Sprite LoadBaseTexture(int n)
    {
        Texture2D tex = SpriteUtils.LoadTexture(imageFilename);
        if (tex == null)
        {
            Debug.LogError("Error: Texture not found: " + imageFilename);
            return null;
        }
        tex = GetReadableTexture(tex);
        return ProcessBaseTexture(tex, n);
    }

    private Texture2D GetReadableTexture(Texture2D source)
    {
        if (source == null) return null;
        if (source.isReadable) return source;

        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        Graphics.Blit(source, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }

    private Texture2D ExtractSpriteTexture(Sprite sprite)
    {
        if (sprite == null) return null;
        Texture2D readableAtlas = GetReadableTexture(sprite.texture);
        if (readableAtlas == null) return null;

        Rect r = sprite.textureRect;
        int w = Mathf.RoundToInt(r.width);
        int h = Mathf.RoundToInt(r.height);
        int x0 = Mathf.RoundToInt(r.x);
        int y0 = Mathf.RoundToInt(r.y);
        if (w <= 0 || h <= 0) return null;

        Color32[] src = readableAtlas.GetPixels32();
        int srcW = readableAtlas.width;
        Color32[] dst = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            int srcRow = (y0 + y) * srcW + x0;
            int dstRow = y * w;
            for (int x = 0; x < w; x++)
            {
                dst[dstRow + x] = src[srcRow + x];
            }
        }

        Texture2D outTex = new Texture2D(w, h, TextureFormat.ARGB32, false);
        outTex.SetPixels32(dst);
        outTex.Apply();
        outTex.filterMode = FilterMode.Point;
        outTex.wrapMode = TextureWrapMode.Clamp;
        return outTex;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    private void Awake()
    {
        EventDispatcher.AddListener(EventNames.PUZZLE_GENERATEION_START, StartGame);
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener(EventNames.PUZZLE_GENERATEION_START, StartGame);
    }

    public void StartGame()
    {
        // 清理上一次场景残留的排序引用，避免 MissingReference
        Tile.tilesSorting.Clear();
        if (GameManager.Instance == null)
        {
            //默认测试
            imageFilename = GameApp.Instance.GetJigsawImageName();
            mN = Mathf.Max(2, initialN);
            paddingFactor = GetPaddingFactorForDifficulty(mN);
            mBaseSpriteOpaque = LoadBaseTexture(mN);
        }
        else
        {
            //从gamemanger里面获取拼图数据
            GameData currentData = GameManager.Instance.currentGameData;
            // 默认使用游戏数据的难度
            mN = currentData.difficulty;
            // 如果存在存档，则以存档难度为准
            if (currentData != null && currentData.selectedImage != null)
            {
                var saved = PlayPrefsManager.Instance.LoadPuzzleStateForImage(currentData.selectedImage.name);
                if (saved != null && saved.pieces != null && saved.pieces.Length > 0)
                {
                    Debug.Log($"[BoardGen] Found save for image={currentData.selectedImage.name}, savedGrid={saved.gridSize}, gameDataGrid={mN}, using saved grid");
                    mN = saved.gridSize;
                    GameManager.Instance.currentGameData.difficulty = mN;
                }
                else
                {
                    Debug.Log($"[BoardGen] No save found for image={currentData.selectedImage.name}, using gameDataGrid={mN}");
                }
            }
            paddingFactor = GetPaddingFactorForDifficulty(mN);
            
            // 如果 selectedImage 是通过相机拍摄的，或者是动态生成的，直接使用其 texture
            if (currentData.selectedImage != null && currentData.selectedImage.texture != null)
            {
                // 如果是资源图片，可能需要检查 isReadable，ProcessBaseTexture 内部会处理
                mBaseSpriteOpaque = ProcessBaseTexture(currentData.selectedImage.texture, mN);
                imageFilename = currentData.selectedImage.name;
            }
            else
            {
                imageFilename = "Images/PuzzleImages/Nature/" + currentData.selectedImage.name;
                mBaseSpriteOpaque = LoadBaseTexture(mN);
            }

            Debug.Log($"从游戏数据生成拼图: {imageFilename}, 难度={mN}x{mN}");
        }

        mGameObjectOpaque = new GameObject();
        mGameObjectOpaque.name = imageFilename + "_Opaque";
        var srOpaque = mGameObjectOpaque.AddComponent<SpriteRenderer>();
        if (runtimeDisplayCropPixels > 0)
        {
            int w = mBaseSpriteOpaque.texture.width;
            int h = mBaseSpriteOpaque.texture.height;
            int crop = Mathf.Clamp(runtimeDisplayCropPixels, 0, Mathf.Min(w, h) / 2 - 1);
            var cropped = SpriteUtils.CreateSpriteFromTexture2D(
                mBaseSpriteOpaque.texture,
                crop, crop,
                w - crop * 2,
                h - crop * 2);
            srOpaque.sprite = cropped;
            mGameObjectOpaque.transform.position = new Vector3(crop, crop, 0f);
        }
        else
        {
            srOpaque.sprite = mBaseSpriteOpaque;
        }
        srOpaque.sortingLayerName = "Opaque";

        mBaseSpriteTransparent = CreateTransparentView(mBaseSpriteOpaque.texture, runtimeDisplayCropPixels);
        mGameObjectTransparent = new GameObject();
        mGameObjectTransparent.name = imageFilename + "_Transparent";
        var srTransp = mGameObjectTransparent.AddComponent<SpriteRenderer>();
        if (runtimeDisplayCropPixels > 0)
        {
            int w = mBaseSpriteTransparent.texture.width;
            int h = mBaseSpriteTransparent.texture.height;
            int crop = Mathf.Clamp(runtimeDisplayCropPixels, 0, Mathf.Min(w, h) / 2 - 1);
            var cropped = SpriteUtils.CreateSpriteFromTexture2D(
                mBaseSpriteTransparent.texture,
                crop, crop,
                w - crop * 2,
                h - crop * 2);
            srTransp.sprite = cropped;
            mGameObjectTransparent.transform.position = new Vector3(crop, crop, 0f);
        }
        else
        {
            srTransp.sprite = mBaseSpriteTransparent;
        }
        srTransp.sortingLayerName = "Transparent";

        mGameObjectOpaque.gameObject.SetActive(false);

        SetCameraPosition();

        // Create the Jigsaw tiles.
        StartCoroutine(Coroutine_CreateJigsawTiles());
        // 等待瓦片创建完成后尝试应用存档（即使不洗牌也能恢复）
        Debug.Log("[BoardGen] StartGame: waiting for tiles to be created, then applying saved state if any");
        StartCoroutine(WaitApplySavedState());
    }

    private float GetPaddingFactorForDifficulty(int difficulty)
    {
        if (!useDifficultyPadding) return paddingFactor;
        float t = Mathf.InverseLerp(difficultyMin, difficultyMax, difficulty);
        return Mathf.Clamp(paddingByDifficulty.Evaluate(t), 0f, 0.12f);
    }

    /// <summary>
    /// 基于不透明底图生成“透明视图”（中间区域降低 alpha，四周边框保持原始像素），
    /// 使用 GetPixels32 + SetPixels32 批处理，避免逐像素 API 调用。
    /// </summary>
    Sprite CreateTransparentView(Texture2D tex, int cropPixels = 0)
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

        int cx = Mathf.Clamp(cropPixels, 0, Mathf.Min(newTex.width, newTex.height) / 2 - 1);
        Sprite sprite = SpriteUtils.CreateSpriteFromTexture2D(
          newTex,
          cx,
          cx,
          newTex.width - cx * 2,
          newTex.height - cx * 2);
        return sprite;
    }

    void SetCameraPosition()
    {
        // 计算中心点与适配尺寸，优先使用“实际显示”的精灵（透明视图，可能被裁剪）
        float width, height, ox, oy;
        Sprite displaySprite = null;
        if (mGameObjectTransparent != null)
        {
            var srt = mGameObjectTransparent.GetComponent<SpriteRenderer>();
            if (srt != null && srt.sprite != null)
            {
                displaySprite = srt.sprite;
                ox = mGameObjectTransparent.transform.position.x;
                oy = mGameObjectTransparent.transform.position.y;
                width = displaySprite.rect.width;
                height = displaySprite.rect.height;
                goto ComputeCamera;
            }
        }
        // 回退到不透明底图
        width = mBaseSpriteOpaque.rect.width;
        height = mBaseSpriteOpaque.rect.height;
        ox = mGameObjectOpaque != null ? mGameObjectOpaque.transform.position.x : 0f;
        oy = mGameObjectOpaque != null ? mGameObjectOpaque.transform.position.y : 0f;
ComputeCamera:
        float cx = width / 2f + ox;
        float cy = height / 2f + oy;

        // 若宽/高为偶数像素，中心落在像素间隙，+0.5f 可减少半像素采样导致的边缘1px丢失
        if (((int)width & 1) == 0) cx += 0.5f;
        if (((int)height & 1) == 0) cy += 0.5f;

        Camera.main.transform.position = new Vector3(cx, cy, -10.0f);

        float puzzleBoardWidth = width;
        float puzzleBoardHeight = height;
        float totalWidth = puzzleBoardWidth;
        float totalHeight = puzzleBoardHeight;

        float screenW = Mathf.Max(1f, Screen.width);
        float screenH = Mathf.Max(1f, Screen.height);
        float aspectRatio = screenW / screenH;

        float visibleW = screenW;
        float visibleH = screenH;
        if (fitToPuzzleArea)
        {
            Rect r = GetBoardViewportScreenRect();
            if (r.width > 10f && r.height > 10f)
            {
                visibleW = Mathf.Clamp(r.width, 1f, screenW);
                visibleH = Mathf.Clamp(r.height, 1f, screenH);
            }
        }

        float requiredSizeForWidth = totalWidth / (2.0f * aspectRatio) * (screenW / Mathf.Max(1f, visibleW));
        float requiredSizeForHeight = totalHeight / 2.0f * (screenH / Mathf.Max(1f, visibleH));
        float margin = Mathf.Max(0f, boardEdgeMargin);
        if (fitToPuzzleArea) margin = Mathf.Max(margin, Mathf.Max(0f, minBoardEdgeMarginWhenFitting));
        float marginFactor = 1f + margin;
        float cameraSize = Mathf.Max(requiredSizeForWidth, requiredSizeForHeight) * marginFactor;

        Camera.main.orthographicSize = cameraSize;

        Debug.Log($"摄像机设置: 拼图盘({puzzleBoardWidth}x{puzzleBoardHeight}), 中心({cx},{cy}), 正交大小={cameraSize}");
    }

    private Rect GetBoardViewportScreenRect()
    {
        var gp = UnityEngine.Object.FindObjectOfType<GameplayPage>(true);
        if (gp != null && gp.puzzleArea != null)
        {
            Rect pr = GetRectTransformScreenRect(gp.puzzleArea);
            if (pr.width > 10f && pr.height > 10f) return pr;
        }

        Rect safe = Screen.safeArea;
        if (safe.width <= 1f || safe.height <= 1f) safe = new Rect(0f, 0f, Screen.width, Screen.height);

        float yMin = safe.yMin;
        float yMax = safe.yMax;

        if (menu != null)
        {
            if (menu.panelTopPanel != null && menu.panelTopPanel.activeInHierarchy)
            {
                var rt = menu.panelTopPanel.GetComponent<RectTransform>();
                Rect tr = GetRectTransformScreenRect(rt);
                if (tr.width > 10f && tr.height > 10f) yMax = Mathf.Min(yMax, tr.yMin);
            }
            if (menu.panelBottomPanel != null && menu.panelBottomPanel.activeInHierarchy)
            {
                var rt = menu.panelBottomPanel.GetComponent<RectTransform>();
                Rect br = GetRectTransformScreenRect(rt);
                if (br.width > 10f && br.height > 10f) yMin = Mathf.Max(yMin, br.yMax);
            }
        }

        var tray = UnityEngine.Object.FindObjectOfType<PuzzleScrollTray>(true);
        if (tray != null && tray.scrollRect != null && tray.scrollRect.gameObject.activeInHierarchy)
        {
            Rect tr = GetRectTransformScreenRect(tray.scrollRect.GetComponent<RectTransform>());
            if (tr.width > 10f && tr.height > 10f) yMin = Mathf.Max(yMin, tr.yMax);
        }

        if (yMax - yMin < 10f) return safe;
        return Rect.MinMaxRect(safe.xMin, yMin, safe.xMax, yMax);
    }

    private Rect GetRectTransformScreenRect(RectTransform rt)
    {
        if (rt == null) return new Rect(0f, 0f, 0f, 0f);
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            Vector2 s = RectTransformUtility.WorldToScreenPoint(uiCam, corners[i]);
            if (s.x < minX) minX = s.x;
            if (s.y < minY) minY = s.y;
            if (s.x > maxX) maxX = s.x;
            if (s.y > maxY) maxY = s.y;
        }
        if (!float.IsFinite(minX) || !float.IsFinite(minY) || !float.IsFinite(maxX) || !float.IsFinite(maxY))
        {
            return new Rect(0f, 0f, 0f, 0f);
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
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
        tilesInPlace = 0;
        tilesTotal = numTileX * numTileY;

        for (int i = 0; i < numTileX; i++)
        {
            for (int j = 0; j < numTileY; j++)
            {
                mTiles[i, j] = CreateTile(i, j, baseTexture);
                mTileGameObjects[i, j] = CreateGameObjectFromTile(mTiles[i, j]);
                // 订阅统一放到打乱完成后（OnFinishedShuffling）再进行，避免重复订阅
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
        tilesInPlace = 0;
        tilesTotal = numTileX * numTileY;

        for (int i = 0; i < numTileX; i++)
        {
            for (int j = 0; j < numTileY; j++)
            {
                mTiles[i, j] = CreateTile(i, j, baseTexture);
                mTileGameObjects[i, j] = CreateGameObjectFromTile(mTiles[i, j]);
                // 订阅统一放到打乱完成后（OnFinishedShuffling）再进行，避免重复订阅
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


        //发出拼图生成好了的事件
        EventDispatcher.Dispatch(EventNames.PUZZLE_GENERATEION_DONE);
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
        // 相机缩放与拖拽输入处理（支持触摸/鼠标）
        HandleCameraInput();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleNextTile();
        }
    }

    /// <summary>
    /// 统一处理相机输入：
    /// - 触摸：双指捏合缩放；单指在非 TileMovement 处按下并拖动可平移相机。
    /// - 编辑器/PC：鼠标滚轮缩放；左键在非 TileMovement 处按住拖动可平移相机。
    /// - 缩放范围由 minOrthoSize/maxOrthoSize 限制。
    /// </summary>
    private void HandleCameraInput()
    {
        if (Camera.main == null) return;
        if (!cameraInteractionEnabled) return;

        // 触摸优先
        if (Input.touchCount >= 2)
        {
            pinching = true;
            isPanning = false; // 捏合时不平移

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prevPos0 = t0.position - t0.deltaPosition;
            Vector2 prevPos1 = t1.position - t1.deltaPosition;

            float prevDist = Vector2.Distance(prevPos0, prevPos1);
            float currDist = Vector2.Distance(t0.position, t1.position);
            float delta = currDist - prevDist; // 手指拉开为正、合拢为负

            // 手指拉开（delta>0）应“放大”视图 => 降低正交尺寸；因此缩放量取负号
            ApplyZoom(-delta * zoomSpeedTouch);
            return;
        }
        else if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                panDisabledDueToTile = IsPointerOverAnyTileMovementAtScreenPosition(t.position);
                if (!panDisabledDueToTile)
                {
                    isPanning = true;
                    lastPanWorldPos = ScreenToWorld(t.position);
                }
            }
            else if (t.phase == TouchPhase.Moved && isPanning && !panDisabledDueToTile)
            {
                Vector3 curWorld = ScreenToWorld(t.position);
                Vector3 delta = lastPanWorldPos - curWorld; // 保持起始世界点不动
                delta.z = 0f;
                if (delta.sqrMagnitude > panEpsilon)
                {
                    Camera.main.transform.position += delta * panSpeed;
                    // 重要：移动相机后，需基于“新相机位置”重新计算指针的世界坐标，避免来回抽动
                    lastPanWorldPos = ScreenToWorld(t.position);
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                isPanning = false;
                panDisabledDueToTile = false;
                pinching = false;
            }

            return;
        }
        else
        {
            // 无触摸
            pinching = false;
            isPanning = Input.GetMouseButton(0) && isPanning; // 保持拖拽状态仅在按住时
        }

        // 编辑器/PC：鼠标滚轮缩放（不屏蔽 UI，满足“只要不是 TileMovement”即可）
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            ApplyZoom(-scroll * zoomSpeedMouse);
        }

        // 编辑器/PC：左键拖拽平移（起手点不在 TileMovement 上）
        if (Input.GetMouseButtonDown(0))
        {
            // 检查是否点击在 UI 上
            bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                           UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            panDisabledDueToTile = isOverUI || IsPointerOverAnyTileMovementAtScreenPosition(Input.mousePosition);
            if (!panDisabledDueToTile)
            {
                isPanning = true;
                lastPanWorldPos = ScreenToWorld(Input.mousePosition);
            }
            else
            {
                isPanning = false;
            }
        }
        else if (Input.GetMouseButton(0) && isPanning && !panDisabledDueToTile)
        {
            Vector3 curWorld = ScreenToWorld(Input.mousePosition);
            Vector3 delta = lastPanWorldPos - curWorld;
            delta.z = 0f;

            if (delta.sqrMagnitude > panEpsilon)
            {
                Camera.main.transform.position += delta * panSpeed;
                // 重要：移动相机后，需基于“新相机位置”重新计算指针的世界坐标，避免来回抽动
                lastPanWorldPos = ScreenToWorld(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isPanning = false;
            panDisabledDueToTile = false;
        }
    }

    /// <summary>
    /// 应用缩放并按设定范围进行钳制。
    /// 正值 delta 表示远离（增大正交尺寸），负值表示靠近（减小正交尺寸）。
    /// </summary>
    private void ApplyZoom(float delta)
    {
        if (Camera.main == null) return;
        float lo = Mathf.Min(minOrthoSize, maxOrthoSize);
        float hi = Mathf.Max(minOrthoSize, maxOrthoSize);
        float size = Camera.main.orthographicSize;
        float newSize = Mathf.Clamp(size + delta, lo, hi);
        Camera.main.orthographicSize = newSize;
    }

    /// <summary>
    /// 屏幕坐标是否命中带有 TileMovement 的拼图块（用于决定是否允许拖拽相机）。
    /// </summary>
    private bool IsPointerOverAnyTileMovementAtScreenPosition(Vector3 screenPos)
    {
        Vector3 world = ScreenToWorld(screenPos);
        // 可能有多个重叠碰撞体，取全部以保证检测完整
        Collider2D[] hits = Physics2D.OverlapPointAll((Vector2)world);
        if (hits == null || hits.Length == 0) return false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].GetComponent<TileMovement>() != null)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 将屏幕坐标转换为世界坐标（固定使用主摄像机）。
    /// </summary>
    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;
        Vector3 wp = cam.ScreenToWorldPoint(screenPos);
        wp.z = 0f; // 2D 平面
        return wp;
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

    // 排列参数
    private const float shuffleGap = 10f; // 拼图块之间的间隙
    private const int columnsPerSide = 3; // 每一侧的列数（可根据需要调整）

    //void Shuffle(GameObject obj, Vector3 targetPos)
    //{
    //    Coroutine moveCoroutine = StartCoroutine(Coroutine_MoveOverSeconds(obj, targetPos, 1.0f));
    //    activeCoroutines.Add(moveCoroutine);
    //}

    IEnumerator Coroutine_Shuffle()
    {
        // 1. 收集所有拼图块
        List<GameObject> allTiles = new List<GameObject>();
        for (int i = 0; i < numTileX; ++i)
        {
            for (int j = 0; j < numTileY; ++j)
            {
                allTiles.Add(mTileGameObjects[i, j]);
            }
        }

        // 2. 打乱列表顺序（可选，如果希望排列是乱序的）
        // 如果希望按顺序排列（比如左上角第一块放在第一个位置），则不需要打乱
        // 为了增加趣味性，我们还是打乱一下
        for (int i = 0; i < allTiles.Count; i++)
        {
            GameObject temp = allTiles[i];
            int randomIndex = UnityEngine.Random.Range(i, allTiles.Count);
            allTiles[i] = allTiles[randomIndex];
            allTiles[randomIndex] = temp;
        }

        // 3. 计算排列参数
        // 拼图板尺寸
        float puzzleBoardWidth = mBaseSpriteOpaque.rect.width;
        //float puzzleBoardHeight = mBaseSpriteOpaque.texture.height;
        float halfBoardWidth = puzzleBoardWidth * 0.5f;



        // 4. 分配位置
        // 改为使用 UI Scroll Tray
        PuzzleScrollTray scrollTray = FindObjectOfType<PuzzleScrollTray>();
        if (scrollTray == null)
        {
            // 如果场景中没有 Tray，创建一个
            GameObject trayObj = new GameObject("PuzzleScrollTray", typeof(RectTransform));
            trayObj.transform.SetParent(GameObject.Find("Canvas")?.transform ?? FindObjectOfType<Canvas>()?.transform, false);
            
            // 确保 Tray 填满屏幕（或者至少在底部正确位置）
            RectTransform trayRect = trayObj.GetComponent<RectTransform>();
            trayRect.anchorMin = Vector2.zero;
            trayRect.anchorMax = Vector2.one;
            trayRect.offsetMin = Vector2.zero;
            trayRect.offsetMax = Vector2.zero;
            
            scrollTray = trayObj.AddComponent<PuzzleScrollTray>();
            scrollTray.Initialize();
        }
        
        scrollTray.Clear();

        for (int i = 0; i < allTiles.Count; i++)
        {
            scrollTray.AddPiece(allTiles[i]);
            yield return null;
        }

        OnFinishedShuffling();
    }

    public void ShuffleTiles()
    {
        StartCoroutine(Coroutine_Shuffle());
    }

    public float recallToTrayDuration = 0.35f;
    public float recallToTrayScale = 0.25f;
    public float recallToTrayStagger = 0.02f;
    public Ease recallToTrayEase = Ease.InOutCubic;

    public void MoveUnplacedTilesToTray()
    {
        StartCoroutine(Coroutine_MoveUnplacedTilesToTray());
    }

    private IEnumerator Coroutine_MoveUnplacedTilesToTray()
    {
        PuzzleScrollTray tray = FindObjectOfType<PuzzleScrollTray>(true);
        if (tray == null)
        {
            GameObject trayObj = new GameObject("PuzzleScrollTray", typeof(RectTransform));
            trayObj.transform.SetParent(GameObject.Find("Canvas")?.transform ?? FindObjectOfType<Canvas>()?.transform, false);
            RectTransform trayRect = trayObj.GetComponent<RectTransform>();
            trayRect.anchorMin = Vector2.zero;
            trayRect.anchorMax = Vector2.one;
            trayRect.offsetMin = Vector2.zero;
            trayRect.offsetMax = Vector2.zero;
            tray = trayObj.AddComponent<PuzzleScrollTray>();
            tray.Initialize();
        }

        tray.Clear();

        var tiles = FindObjectsOfType<TileMovement>(true);
        if (tiles == null || tiles.Length == 0) yield break;

        Array.Sort(tiles, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int ay = a.tile != null ? a.tile.yIndex : 0;
            int ax = a.tile != null ? a.tile.xIndex : 0;
            int by = b.tile != null ? b.tile.yIndex : 0;
            int bx = b.tile != null ? b.tile.xIndex : 0;
            int cy = ay.CompareTo(by);
            if (cy != 0) return cy;
            return ax.CompareTo(bx);
        });

        var activeTiles = new List<TileMovement>();
        var inactiveTiles = new List<TileMovement>();
        for (int i = 0; i < tiles.Length; i++)
        {
            var tm = tiles[i];
            if (tm == null || tm.gameObject == null) continue;
            if (tm.gameObject.activeSelf) activeTiles.Add(tm);
            else inactiveTiles.Add(tm);
        }

        Rect trayScreenRect = GetTrayScreenRect(tray);
        int slotCount = Mathf.Max(1, activeTiles.Count);
        var cam = Camera.main;

        for (int i = 0; i < activeTiles.Count; i++)
        {
            var tm = activeTiles[i];
            if (tm == null || tm.gameObject == null) continue;

            tm.enabled = false;

            var go = tm.gameObject;
            var tr = go.transform;
            tr.DOKill();

            var sr = go.GetComponent<SpriteRenderer>();
            Color srColor = sr != null ? sr.color : default;
            Vector3 originalScale = tr.localScale;

            float t = (i + 0.5f) / slotCount;
            float screenX = Mathf.Lerp(trayScreenRect.xMin + 40f, trayScreenRect.xMax - 40f, t);
            float screenY = Mathf.Lerp(trayScreenRect.yMin, trayScreenRect.yMax, 0.5f);
            float zDist = cam != null ? Mathf.Abs(tr.position.z - cam.transform.position.z) : 10f;
            Vector3 targetWorld = cam != null
                ? cam.ScreenToWorldPoint(new Vector3(screenX, screenY, zDist))
                : tr.position;
            targetWorld.z = tr.position.z;

            float duration = Mathf.Max(0.05f, recallToTrayDuration);
            float scaleTo = Mathf.Max(0.01f, recallToTrayScale);

            var seq = DOTween.Sequence();
            seq.Join(tr.DOMove(targetWorld, duration).SetEase(recallToTrayEase));
            seq.Join(tr.DOScale(originalScale * scaleTo, duration).SetEase(recallToTrayEase));
            if (sr != null)
            {
                Color target = new Color(srColor.r, srColor.g, srColor.b, 0f);
                seq.Join(DOTween.To(() => sr.color, c => sr.color = c, target, duration));
            }
            bool done = false;
            seq.OnComplete(() =>
            {
                if (sr != null) sr.color = srColor;
                tr.localScale = originalScale;
                tm.enabled = true;
                go.SetActive(false);
                tray.AddPiece(go);
                done = true;
            });

            float wait = Mathf.Max(0f, recallToTrayStagger);
            float elapsed = 0f;
            while (!done && elapsed < duration + 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
        }

        for (int i = 0; i < inactiveTiles.Count; i++)
        {
            var tm = inactiveTiles[i];
            if (tm == null || tm.gameObject == null) continue;
            tm.enabled = true;
            tray.AddPiece(tm.gameObject);
        }
    }

    private Rect GetTrayScreenRect(PuzzleScrollTray tray)
    {
        if (tray != null && tray.scrollRect != null)
        {
            RectTransform rt = tray.scrollRect.viewport != null ? tray.scrollRect.viewport : tray.scrollRect.GetComponent<RectTransform>();
            if (rt != null)
            {
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                var cam = Camera.main;
                float minX = float.PositiveInfinity;
                float minY = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;
                float maxY = float.NegativeInfinity;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 s = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
                    if (s.x < minX) minX = s.x;
                    if (s.y < minY) minY = s.y;
                    if (s.x > maxX) maxX = s.x;
                    if (s.y > maxY) maxY = s.y;
                }
                if (float.IsFinite(minX) && float.IsFinite(minY) && float.IsFinite(maxX) && float.IsFinite(maxY))
                {
                    return Rect.MinMaxRect(minX, minY, maxX, maxY);
                }
            }
        }
        float fallbackH = Screen.height * 0.15f;
        return new Rect(0f, 0f, Screen.width, fallbackH);
    }

    void OnFinishedShuffling()
    {
        activeCoroutines.Clear();

        menu.SetEnableBottomPanel(false);
        StartCoroutine(Coroutine_CallAfterDelay(() => menu.SetEnableTopPanel(true), 1.0f));
        GameManager.Instance.TileMovementEnabled = true;

        StartTimer();

        for (int i = 0; i < numTileX; ++i)
        {
            for (int j = 0; j < numTileY; ++j)
            {
                TileMovement tm = mTileGameObjects[i, j].GetComponent<TileMovement>();
                if (tm != null)
                {
                    // 防御性：先移除再添加，确保不会重复订阅
                    tm.onTileInPlace -= OnTileInPlace;
                    tm.onTileInPlace += OnTileInPlace;
                }
                SpriteRenderer spriteRenderer = tm.gameObject.GetComponent<SpriteRenderer>();
                PieceSorting.SetIdle(spriteRenderer);
            }
        }

        // 应用存档（如果存在）
        ApplySavedStateIfAny();

        menu.SetTotalTiles(numTileX * numTileY);
    }

    /// <summary>
    /// 从 PlayPrefsManager 加载当前图片的未完成存档，并应用到棋盘。
    /// </summary>
    private void ApplySavedStateIfAny()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.currentGameData == null || gm.currentGameData.selectedImage == null) return;
        string imageId = gm.currentGameData.selectedImage.name;
        var state = PlayPrefsManager.Instance.LoadPuzzleStateForImage(imageId);
        if (state == null || state.pieces == null || state.pieces.Length == 0) return;
        if (state.gridSize != mN) return;

        // 暂停移动，避免还原过程中触发误操作
        bool prevMove = gm.TileMovementEnabled;
        gm.TileMovementEnabled = false;

        int restoredPlaced = 0;
        Debug.Log($"[BoardGen] Applying saved state: grid={state.gridSize}, tiles={state.pieces.Length}, mN={mN}");
        // 逐块恢复位置；已放置的直接定位到正确位置但不触发 OnTileInPlace（避免误结算）
        for (int k = 0; k < state.pieces.Length; k++)
        {
            var pd = state.pieces[k];
            int i = Mathf.Clamp(pd.col, 0, numTileX - 1);
            int j = Mathf.Clamp(pd.row, 0, numTileY - 1);
            var go = mTileGameObjects[i, j];
            if (go == null) continue;
            var tm = go.GetComponent<TileMovement>();
            if (tm == null) continue;
            // 确保世界拼图块可见
            go.SetActive(true);
            if (pd.isPlaced)
            {
                // 直接设置到正确位置并禁用拖拽组件
                Vector3 correct = new Vector3(i * Tile.tileSize, j * Tile.tileSize, 0f);
                tm.transform.position = correct;
                tm.enabled = false;
                // 从排序中移除，避免重复调整层级
                var sr = tm.gameObject.GetComponent<SpriteRenderer>();
                Tile.tilesSorting.Remove(sr);
                PieceSorting.SetPlaced(sr);
                restoredPlaced++;
            }
            else
            {
                tm.transform.position = pd.currentPosition;
                tm.enabled = true;
            }
        }

        // 重新构建托盘：只保留未放置的拼图块
        var tray = UnityEngine.Object.FindObjectOfType<PuzzleScrollTray>();
        if (tray != null)
        {
            tray.Clear();
            for (int k = 0; k < state.pieces.Length; k++)
            {
                var pd = state.pieces[k];
                int i = Mathf.Clamp(pd.col, 0, numTileX - 1);
                int j = Mathf.Clamp(pd.row, 0, numTileY - 1);
                if (!pd.isPlaced)
                {
                    var piece = mTileGameObjects[i, j];
                    if (piece != null) tray.AddPiece(piece);
                }
            }
        }

        // 恢复移动能力
        gm.TileMovementEnabled = prevMove;

        // 刷新上方 UI 的已就位计数
        if (menu != null)
        {
            gm.TotalTilesInCorrectPosition = restoredPlaced;
            menu.SetTilesInPlace(gm.TotalTilesInCorrectPosition);
        }
        Debug.Log($"[BoardGen] ApplySavedStateIfAny done: restoredPlaced={restoredPlaced}, remaining={state.pieces.Length - restoredPlaced}");
    }

    private IEnumerator WaitApplySavedState()
    {
        // 等待瓦片数组与对象创建
        int guard = 0;
        while ((mTileGameObjects == null || mTileGameObjects.Length == 0) && guard < 200)
        {
            guard++;
            yield return null;
        }
        Debug.Log($"[BoardGen] Tiles ready (guard={guard}), applying saved state now");
        ApplySavedStateIfAny();
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
            GameManager.Instance.SecondsSinceStart += 1;

            menu.SetTimeInSeconds(GameManager.Instance.SecondsSinceStart);
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

        // 清理上一次的排序列表，避免包含已销毁的渲染器
        Tile.tilesSorting.Clear();

        // 停止已有协程（如洗牌移动、计时等）
        StopAllCoroutines();
        activeCoroutines.Clear();

        GameManager.Instance.TileMovementEnabled = false;

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

        GameManager.Instance.SecondsSinceStart = 0;
        GameManager.Instance.TotalTilesInCorrectPosition = 0;
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
        GameManager.Instance.TotalTilesInCorrectPosition += 1;

        tm.enabled = false;
        Destroy(tm);

        SpriteRenderer spriteRenderer = tm.gameObject.GetComponent<SpriteRenderer>();
        Tile.tilesSorting.Remove(spriteRenderer);
        PieceSorting.SetPlaced(spriteRenderer);

        if (GameManager.Instance.TotalTilesInCorrectPosition == mTileGameObjects.Length)
        {
            // 新结算：触发胜利页
            EventDispatcher.Dispatch(EventNames.PUZZLE_COMPLETED);
            float timeUsed = 0f;
            var gp = UnityEngine.Object.FindObjectOfType<GameplayPage>();
            if (gp != null)
            {
                timeUsed = gp.GetCurrentGameTime();
            }
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.CompleteGame(timeUsed);
            }
            else
            {
                // 回退：旧的面板
                menu.SetEnableTopPanel(false);
                menu.SetEnableGameCompletionPanel(true);
            }

            // Reset the values.
            GameManager.Instance.SecondsSinceStart = 0;
            GameManager.Instance.TotalTilesInCorrectPosition = 0;
        }
        menu.SetTilesInPlace(GameManager.Instance.TotalTilesInCorrectPosition);
    }
}
