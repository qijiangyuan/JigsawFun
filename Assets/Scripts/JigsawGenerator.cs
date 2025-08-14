using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class JigsawGenerator : MonoBehaviour
{
    public Texture2D sourceImage;
    public Texture2D edgeImage;
    public GameObject piecePrefab; // 带有 SpriteRenderer + PolygonCollider2D

    [Header("质量设置")]
    public bool useEdgeBleed = true; // 边缘扩展，解决透明边缘渗色
    public FilterMode filterMode = FilterMode.Bilinear; // 采样模式：双线性(性能好) vs 点采样(质量高)

    public int gridSize = 4;

    /// <summary>
    /// 用于管理所有Pieces
    /// </summary>
    private List<PuzzlePiece> puzzlePieces = new List<PuzzlePiece>();

    void Start()
    {
        if (sourceImage == null || piecePrefab == null || edgeImage == null)
        {
            Debug.LogError("请设置源图片和拼图预制件！");
            return;
        }


        // 使用默认的行列数生成拼图
        GeneratePuzzle();
    }


    private int curIndex = 0;
    private void Update()
    {
        //如果点击了空格键
        if (Input.GetKeyDown(KeyCode.Space))
        {
            puzzlePieces[curIndex].SnapToCorrectPosition();
            curIndex++;
        }
    }


    /// <summary>
    /// 加载拼图布局JSON文件
    /// </summary>
    /// <param name="gridSize">网格大小</param>
    /// <returns>拼图布局数据</returns>
    JigsawLayoutData LoadJigsawLayout(int gridSize)
    {
        string jsonPath = Path.Combine(Application.dataPath, "Resources", "Images", "mask", $"{gridSize}x{gridSize}", $"{gridSize}x{gridSize}_layout.json");

        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"无法找到JSON布局文件: {jsonPath}");
            return null;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonPath);
            JigsawLayoutData layoutData = JsonUtility.FromJson<JigsawLayoutData>(jsonContent);
            Debug.Log($"成功加载JSON布局文件，包含 {layoutData.pieces.Count} 个拼图块信息");
            return layoutData;
        }
        catch (Exception e)
        {
            Debug.LogError($"解析JSON文件失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 根据行列索引获取拼图块数据
    /// </summary>
    /// <param name="layoutData">布局数据</param>
    /// <param name="row">行索引</param>
    /// <param name="col">列索引</param>
    /// <returns>拼图块数据</returns>
    MaskPieceData GetPieceData(JigsawLayoutData layoutData, int row, int col)
    {
        if (layoutData == null || layoutData.pieces == null) return null;

        foreach (var piece in layoutData.pieces)
        {
            if (piece.row == row && piece.col == col)
            {
                return piece;
            }
        }

        Debug.LogWarning($"未找到拼图块 ({row}, {col}) 的数据");
        return null;
    }

    /// <summary>
    /// 生成拼图
    /// </summary>
    public void GeneratePuzzle()
    {
        // 加载JSON布局文件
        JigsawLayoutData layoutData = LoadJigsawLayout(gridSize);
        if (layoutData == null)
        {
            Debug.LogError("无法加载拼图布局数据，停止生成拼图");
            return;
        }

        // 设置行列数
        int cols = gridSize;
        int rows = gridSize;

        int pieceWidth = sourceImage.width / cols;
        int pieceHeight = sourceImage.height / rows;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                // 获取当前拼图块的凹凸信息
                MaskPieceData pieceData = GetPieceData(layoutData, y, x);
                if (pieceData == null)
                {
                    Debug.LogError($"无法获取拼图块 ({y}, {x}) 的凹凸信息");
                    continue;
                }

                // 计算原图区域
                int blockStartX = x * pieceWidth;
                int blockStartY = y * pieceHeight;
                int blockEndX = (x + 1) * pieceWidth;
                int blockEndY = (y + 1) * pieceHeight;
                if (x == cols - 1) blockEndX = sourceImage.width;
                if (y == rows - 1) blockEndY = sourceImage.height;

                // 从预生成的mask图片加载
                Texture2D mask = LoadPuzzleMask(x, y, cols, rows);
                if (mask == null)
                {
                    Debug.LogError($"无法加载拼图块 ({x},{y}) 的mask图片");
                    continue;
                }
                Debug.Log($"拼图块 ({x},{y}): 加载预生成mask，尺寸{mask.width}x{mask.height}");

                Texture2D pieceTex = CreatePieceWithMask(x, y, pieceWidth, pieceHeight, mask, cols, rows, pieceData);
                CreatePieceObject(pieceTex, x, y, pieceData);
            }
        }

        // 随机分布拼图块位置
        RandomizePiecePositions();

        // 拼图生成完成后调整摄像机
        AdjustCameraToFitPuzzle();

        // 创建拼图底座
        CreatePuzzleBase();
    }

    /// <summary>
    /// 调整摄像机以适配整个拼图区域
    /// </summary>
    void AdjustCameraToFitPuzzle()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("未找到主摄像机，无法调整摄像机视角");
            return;
        }

        // 确保摄像机是正交模式
        if (!mainCamera.orthographic)
        {
            Debug.LogWarning("摄像机不是正交模式，将自动切换为正交模式");
            mainCamera.orthographic = true;
        }

        // 查找所有拼图块对象
        //GameObject[] puzzlePieces = GameObject.FindGameObjectsWithTag("PuzzlePiece");
        if (puzzlePieces.Count == 0)
        {
            Debug.LogWarning("未找到任何拼图块对象，请确保拼图块有正确的Tag");
            return;
        }

        // 计算所有拼图块的边界
        Bounds totalBounds = new Bounds();
        bool boundsInitialized = false;

        foreach (PuzzlePiece piece in puzzlePieces)
        {
            Renderer renderer = piece.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (!boundsInitialized)
                {
                    totalBounds = renderer.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    totalBounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (!boundsInitialized)
        {
            Debug.LogWarning("无法计算拼图块边界");
            return;
        }

        // 设置摄像机位置到拼图中心
        Vector3 cameraPosition = new Vector3(totalBounds.center.x, totalBounds.center.y, mainCamera.transform.position.z);
        mainCamera.transform.position = cameraPosition;

        // 计算合适的正交size，添加一些边距
        float margin = 1.0f; // 边距
        float requiredWidth = totalBounds.size.x + margin;
        float requiredHeight = totalBounds.size.y + margin;

        // 根据屏幕宽高比调整
        float aspectRatio = (float)Screen.width / Screen.height;
        float cameraSize;

        if (requiredWidth / aspectRatio > requiredHeight)
        {
            // 宽度是限制因素
            cameraSize = requiredWidth / (2.0f * aspectRatio);
        }
        else
        {
            // 高度是限制因素
            cameraSize = requiredHeight / 2.0f;
        }

        mainCamera.orthographicSize = cameraSize;

        Debug.Log($"摄像机已调整: 位置={cameraPosition}, 正交Size={cameraSize}, 拼图边界={totalBounds}");
    }

    /// <summary>
    /// 创建拼图底座
    /// </summary>
    void CreatePuzzleBase()
    {
        // 计算底座尺寸：每个方格512像素，乘以gridSize
        int baseSize = gridSize * 512;

        // 创建底座纹理
        Texture2D baseTexture = new Texture2D(baseSize, baseSize, TextureFormat.RGBA32, false);

        // 填充底座颜色（浅灰色背景）
        Color baseColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
        Color[] pixels = new Color[baseSize * baseSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = baseColor;
        }

        // 绘制网格线（深一点的灰色）
        Color gridColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
        int gridLineWidth = 6; // 网格线宽度（加粗）

        for (int i = 0; i <= gridSize; i++)
        {
            int linePos = i * 512;

            // 绘制垂直线
            if (linePos < baseSize)
            {
                for (int y = 0; y < baseSize; y++)
                {
                    for (int w = 0; w < gridLineWidth && linePos + w < baseSize; w++)
                    {
                        pixels[y * baseSize + linePos + w] = gridColor;
                    }
                }
            }

            // 绘制水平线
            if (linePos < baseSize)
            {
                for (int x = 0; x < baseSize; x++)
                {
                    for (int w = 0; w < gridLineWidth && linePos + w < baseSize; w++)
                    {
                        pixels[(linePos + w) * baseSize + x] = gridColor;
                    }
                }
            }
        }

        baseTexture.SetPixels(pixels);
        baseTexture.Apply();

        // 创建底座游戏对象
        GameObject baseObject = new GameObject("PuzzleBase");
        SpriteRenderer baseSpriteRenderer = baseObject.AddComponent<SpriteRenderer>();

        // 创建精灵，使用与拼图块相同的pixelsPerUnit
        Sprite baseSprite = Sprite.Create(baseTexture, new Rect(0, 0, baseSize, baseSize), new Vector2(0.5f, 0.5f), 100f);
        baseSpriteRenderer.sprite = baseSprite;

        // 计算底座的中心位置，与拼图块对齐
        // 拼图块的世界尺寸是512/pixelsPerUnit = 5.12
        float worldWidth = 512f / 100f; // pixelsPerUnit = 100f
        float worldHeight = 512f / 100f;

        // 拼图的中心位置：(gridSize-1)/2 * worldWidth
        float puzzleCenterX = (gridSize - 1) * 0.5f * worldWidth;
        float puzzleCenterY = -(gridSize - 1) * 0.5f * worldHeight; // Y轴向下为负

        // 设置底座位置（在拼图后面）
        baseObject.transform.position = new Vector3(puzzleCenterX, puzzleCenterY, 1f); // Z轴稍微靠后

        // 设置渲染层级，确保在拼图块后面
        baseSpriteRenderer.sortingOrder = -10;

        Debug.Log($"拼图底座已创建: 尺寸={baseSize}x{baseSize}, 网格={gridSize}x{gridSize}");
    }

    /// <summary>
    /// 随机分布拼图块位置
    /// </summary>
    private void RandomizePiecePositions()
    {
        // 获取所有拼图块
        //GameObject[] puzzlePieces = GameObject.FindGameObjectsWithTag("PuzzlePiece");

        if (puzzlePieces.Count == 0)
        {
            Debug.LogWarning("未找到任何拼图块进行随机分布");
            return;
        }

        // 计算拼图底座的边界
        float worldWidth = 512f / 100f; // 每个拼图块的世界尺寸
        float worldHeight = 512f / 100f;

        // 拼图区域的边界
        float puzzleMinX = 0;
        float puzzleMaxX = (gridSize - 1) * worldWidth;
        float puzzleMinY = -(gridSize - 1) * worldHeight;
        float puzzleMaxY = 0;

        // 扩展随机分布区域，在拼图底座周围留出空间
        float margin = 20f; // 边距
        float randomAreaMinX = puzzleMinX - margin;
        float randomAreaMaxX = puzzleMaxX + margin;
        float randomAreaMinY = puzzleMinY - margin;
        float randomAreaMaxY = puzzleMaxY + margin;

        // 为每个拼图块分配随机位置
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            // 生成随机位置，避免与拼图底座重叠
            Vector3 randomPosition;
            int attempts = 0;
            const int maxAttempts = 50;

            do
            {
                float randomX = UnityEngine.Random.Range(randomAreaMinX, randomAreaMaxX);
                float randomY = UnityEngine.Random.Range(randomAreaMinY, randomAreaMaxY);
                randomPosition = new Vector3(randomX, randomY, 0f);
                attempts++;

                // 检查是否在拼图底座区域内，如果是则重新生成
                bool isInPuzzleArea = randomX >= puzzleMinX && randomX <= puzzleMaxX &&
                                    randomY >= puzzleMinY && randomY <= puzzleMaxY;

                if (!isInPuzzleArea || attempts >= maxAttempts)
                {
                    break;
                }
            } while (true);

            // 设置拼图块位置
            piece.transform.position = randomPosition;
        }

        Debug.Log($"已随机分布 {puzzlePieces.Count} 个拼图块");
    }

    /// <summary>
    /// 加载预生成的拼图mask图片
    /// </summary>
    /// <param name="x">拼图块列索引</param>
    /// <param name="y">拼图块行索引</param>
    /// <param name="cols">总列数</param>
    /// <param name="rows">总行数</param>
    /// <returns>加载的mask纹理</returns>
    Texture2D LoadPuzzleMask(int x, int y, int cols, int rows)
    {
        // 构建Resources路径，使用新的数字命名格式 {row}_{col}.png
        string resourcePath = $"Images/mask/{cols}x{rows}/{y}_{x}";

        // 使用Resources.Load加载纹理
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);

        if (texture != null)
        {
            Debug.Log($"成功加载mask: {resourcePath}, 尺寸: {texture.width}x{texture.height}");
            return texture;
        }
        else
        {
            Debug.LogError($"无法加载mask图片: {resourcePath}，请确保文件位于Assets/Resources/{resourcePath}.png");
            return null;
        }
    }

    /// <summary>
    /// 分析mask图片的边界，确定截取范围
    /// </summary>
    /// <param name="mask">mask纹理</param>
    /// <returns>边界信息：minX, minY, maxX, maxY</returns>
    (int minX, int minY, int maxX, int maxY) GetMaskBounds(Texture2D mask)
    {
        Color[] pixels = mask.GetPixels();
        int width = mask.width;
        int height = mask.height;

        int minX = width, minY = height, maxX = -1, maxY = -1;

        // 扫描所有像素，找到非透明像素的边界
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * width + x];
                // 如果像素不透明（alpha > 0.5）
                if (pixel.a > 0.5f)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        // 如果没有找到非透明像素，返回整个mask区域
        if (maxX == -1)
        {
            minX = 0;
            minY = 0;
            maxX = width - 1;
            maxY = height - 1;
        }

        Debug.Log($"Mask边界: ({minX}, {minY}) 到 ({maxX}, {maxY}), mask尺寸: {width}x{height}");
        return (minX, minY, maxX, maxY);
    }

    /// <summary>
    /// 检查指定边是否为凸出类型
    /// </summary>
    /// <param name="edgeType">边的类型</param>
    /// <returns>是否为凸出</returns>
    bool IsProtruding(MaskPiece.EdgeType edgeType)
    {
        return edgeType == MaskPiece.EdgeType.Protrude;
    }

    /// <summary>
    /// 使用mask创建拼图块纹理
    /// </summary>
    Texture2D CreatePieceWithMask(int cx, int cy, int w, int h, Texture2D mask, int cols, int rows, MaskPieceData pieceData)
    {
        // 使用模板的尺寸作为最终纹理尺寸
        int texWidth = mask.width;
        int texHeight = mask.height;

        Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);
        tex.filterMode = filterMode;

        Color[] pixels = new Color[texWidth * texHeight];

        // 获取模板的像素数据
        Color[] maskPixels = mask.GetPixels();

        // 分析mask的边界，确定实际需要的截取范围
        var bounds = GetMaskBounds(mask);
        int maskMinX = bounds.minX;
        int maskMinY = bounds.minY;
        int maskMaxX = bounds.maxX;
        int maskMaxY = bounds.maxY;

        // 计算mask的实际有效区域尺寸
        int maskEffectiveWidth = maskMaxX - maskMinX + 1;
        int maskEffectiveHeight = maskMaxY - maskMinY + 1;

        // 计算该拼图块在原图中的基础区域
        int blockStartX = cx * w;
        int blockEndX = (cx + 1) * w;
        if (cx == cols - 1) blockEndX = sourceImage.width;

        // 将行索引从"上到下"的逻辑转换为"下到上"的纹理坐标
        int mappedRow = (rows - 1 - cy);
        int blockStartY = mappedRow * h;
        int blockEndY = (mappedRow + 1) * h;
        if (mappedRow == rows - 1) blockEndY = sourceImage.height;

        // 计算基础拼图块的尺寸
        int blockWidth = blockEndX - blockStartX;
        int blockHeight = blockEndY - blockStartY;

        // 扩展区域的初始尺寸（将在后续根据凸出方向调整）
        int expandedWidth = blockWidth;
        int expandedHeight = blockHeight;

        // 计算扩展后的采样区域在原图中的位置
        // 根据凸出方向决定扩展的方向
        int expandedStartX = blockStartX;
        int expandedEndX = blockEndX;
        int expandedStartY = blockStartY;
        int expandedEndY = blockEndY;

        // 根据凸出方向扩展采样区域
        // 计算固定的扩展值，根据edgeImage的高度乘以图片比例除以网格大小
        int fixedExpansion = (sourceImage.width / edgeImage.width) * edgeImage.height / gridSize;

        int extraWidth = fixedExpansion;
        int extraHeight = fixedExpansion;


        // 左边凸出：向左扩展
        if (IsProtruding(pieceData.leftEdge))
        {
            expandedStartX -= extraWidth;
        }

        // 右边凸出：向右扩展
        if (IsProtruding(pieceData.rightEdge))
        {
            expandedEndX += extraWidth;
        }

        // 上边凸出：向上扩展（注意Unity坐标系）
        if (IsProtruding(pieceData.topEdge))
        {
            expandedEndY += extraHeight;
        }

        // 下边凸出：向下扩展
        if (IsProtruding(pieceData.bottomEdge))
        {
            expandedStartY -= extraHeight;
        }

        // 重新计算扩展后的尺寸
        expandedWidth = expandedEndX - expandedStartX;
        expandedHeight = expandedEndY - expandedStartY;


        // 根据mask的有效区域，计算缩放比例
        float scaleX = (float)expandedWidth / maskEffectiveWidth;
        float scaleY = (float)expandedHeight / maskEffectiveHeight;

        // 计算mask中心相对于mask纹理的偏移
        float maskCenterX = (maskMinX + maskMaxX) / 2.0f;
        float maskCenterY = (maskMinY + maskMaxY) / 2.0f;
        float texCenterX = texWidth / 2.0f;
        float texCenterY = texHeight / 2.0f;

        // 使用扩展后的区域中心进行采样
        float expandedCenterX = (expandedStartX + expandedEndX) / 2.0f;
        float expandedCenterY = (expandedStartY + expandedEndY) / 2.0f;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int pixelIndex = y * texWidth + x;
                Color maskColor = maskPixels[pixelIndex];

                // 如果模板在此位置是透明的，则该像素为透明
                if (maskColor.a < 0.5f)
                {
                    pixels[pixelIndex] = Color.clear;
                }
                else
                {
                    float offsetX = x - texCenterX;
                    float offsetY = y - texCenterY;

                    // 映射到扩展后的原图坐标（考虑缩放）
                    float sourceX = expandedCenterX + offsetX * scaleX;
                    float sourceY = expandedCenterY + offsetY * scaleY;

                    // 确保坐标在原图范围内
                    int finalSourceX = Mathf.Clamp(Mathf.RoundToInt(sourceX), 0, sourceImage.width - 1);
                    int finalSourceY = Mathf.Clamp(Mathf.RoundToInt(sourceY), 0, sourceImage.height - 1);

                    pixels[pixelIndex] = sourceImage.GetPixel(finalSourceX, finalSourceY);
                }
            }
        }

        if (useEdgeBleed)
        {
            // 边缘扩展：用邻近的不透明像素颜色填充透明像素的RGB，alpha保留
            Color[] expanded = new Color[pixels.Length];
            System.Array.Copy(pixels, expanded, pixels.Length);

            // 第一轮：4邻域传播（快速修补）
            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    int i = y * texWidth + x;
                    if (expanded[i].a > 0.001f) continue; // 仅处理透明像素

                    Color acc = Color.black;
                    int count = 0;
                    // 上下左右
                    if (y + 1 < texHeight)
                    {
                        Color c = pixels[(y + 1) * texWidth + x];
                        if (c.a > 0.001f) { acc += new Color(c.r, c.g, c.b, 0f); count++; }
                    }
                    if (y - 1 >= 0)
                    {
                        Color c = pixels[(y - 1) * texWidth + x];
                        if (c.a > 0.001f) { acc += new Color(c.r, c.g, c.b, 0f); count++; }
                    }
                    if (x + 1 < texWidth)
                    {
                        Color c = pixels[y * texWidth + (x + 1)];
                        if (c.a > 0.001f) { acc += new Color(c.r, c.g, c.b, 0f); count++; }
                    }
                    if (x - 1 >= 0)
                    {
                        Color c = pixels[y * texWidth + (x - 1)];
                        if (c.a > 0.001f) { acc += new Color(c.r, c.g, c.b, 0f); count++; }
                    }

                    if (count > 0)
                    {
                        Color avg = acc / count;
                        expanded[i] = new Color(avg.r, avg.g, avg.b, 0f);
                    }
                }
            }

            // 第二轮：8邻域补偿（补充斜向的颜色，减少锯齿）
            Color[] expanded2 = new Color[pixels.Length];
            System.Array.Copy(expanded, expanded2, expanded.Length);
            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    int i = y * texWidth + x;
                    if (expanded2[i].a > 0.001f) continue; // 仍为透明

                    Color acc = Color.black;
                    int count = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= texWidth || ny < 0 || ny >= texHeight) continue;
                            Color c = expanded[ny * texWidth + nx];
                            if (c.a > 0.001f)
                            {
                                acc += new Color(c.r, c.g, c.b, 0f);
                                count++;
                            }
                        }
                    }
                    if (count > 0)
                    {
                        Color avg = acc / count;
                        expanded2[i] = new Color(avg.r, avg.g, avg.b, 0f);
                    }
                }
            }

            pixels = expanded2; // 替换为扩展结果
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 创建拼图块游戏对象
    /// </summary>
    void CreatePieceObject(Texture2D tex, int cx, int cy, MaskPieceData pieceData)
    {
        GameObject piece = Instantiate(piecePrefab);
        piece.name = $"Piece_{cy}_{cx}";
        piece.tag = "PuzzlePiece"; // 设置标签便于识别
        piece.layer = 0; // 设置为Default图层 (图层0)

        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        // 创建精灵时启用物理形状生成
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero, true);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10; // 设置较高的排序层级，确保在UI之上

        // 计算基于实际尺寸的位置
        // 根据用户测试结果：00的x是0，10的x是5.12，说明每个拼图块的实际宽度是5.12
        // 切图是按照512分辨率来切的，所以图片大小应该是512除以pixelsPerUnit
        float worldWidth = 512f / sr.sprite.pixelsPerUnit;
        float worldHeight = 512f / sr.sprite.pixelsPerUnit;

        // 考虑sprite的pivot偏移，因为sprite可能有凸出部分
        // 左右都凹时，偏移量x应该为0
        // 只有当有凸出部分时才需要偏移
        Vector2 pivotOffset = Vector2.zero;

        // 对于左右都凹的情况，pivot.x应该等于256（中心），偏移为0
        // 对于有凸出的情况，才需要根据pivot计算偏移
        // 但是左右都凹时，我们强制偏移为0
        float rectCenterX = sr.sprite.rect.width * 0.5f;
        float rectCenterY = sr.sprite.rect.height * 0.5f;

        // 如果sprite的宽度等于512（没有凸出），或者左右都凹，则x偏移为0
        if (sr.sprite.rect.width <= 512f)
        {
            pivotOffset.x = 0f;
        }
        else
        {
            // 使用pieceData的边缘信息来判断凸出方向
            bool isLeftProtrude = pieceData.leftEdge == MaskPiece.EdgeType.Protrude;
            bool isRightProtrude = pieceData.rightEdge == MaskPiece.EdgeType.Protrude;

            float offsetValue = Mathf.Abs(sr.sprite.pivot.x - 256f) / sr.sprite.pixelsPerUnit;

            if (isLeftProtrude && isRightProtrude)
            {
                // 左右同时凸出，偏移为0
                pivotOffset.x = 0f;
            }
            else if (isRightProtrude)
            {
                // 向右凸出，负偏移
                pivotOffset.x = -offsetValue;
            }
            else if (isLeftProtrude)
            {
                // 向左凸出，正偏移
                pivotOffset.x = offsetValue;
            }
            else
            {
                // 左右都凹，偏移为0
                pivotOffset.x = 0f;
            }
        }

        // y方向同样处理
        if (sr.sprite.rect.height <= 512f)
        {
            pivotOffset.y = 0f;
        }
        else
        {
            // 使用pieceData的边缘信息来判断凸出方向
            bool isTopProtrude = pieceData.topEdge == MaskPiece.EdgeType.Protrude;
            bool isBottomProtrude = pieceData.bottomEdge == MaskPiece.EdgeType.Protrude;

            float offsetValue = Mathf.Abs(sr.sprite.pivot.y - 256f) / sr.sprite.pixelsPerUnit;

            if (isTopProtrude && isBottomProtrude)
            {
                // 上下同时凸出，偏移为0
                pivotOffset.y = 0f;
            }
            else if (isBottomProtrude)
            {
                // 向下凸出，正偏移
                pivotOffset.y = offsetValue;
            }
            else if (isTopProtrude)
            {
                // 向上凸出，负偏移
                pivotOffset.y = -offsetValue;
            }
            else
            {
                // 上下都凹，偏移为0
                pivotOffset.y = 0f;
            }
        }

        // 输出pivotOffset信息用于调试
        Debug.Log($"Piece [{cx},{cy}]: sprite.rect=({sr.sprite.rect.width},{sr.sprite.rect.height}), pivot=({sr.sprite.pivot.x},{sr.sprite.pivot.y}), pivotOffset=({pivotOffset.x},{pivotOffset.y})");

        // 计算拼图块在世界坐标中的正确位置
        // 直接根据网格位置和拼图块尺寸计算位置，并考虑pivot偏移
        Vector3 correctWorldPosition = new Vector3(
            cx * worldWidth - pivotOffset.x,
            -cy * worldHeight - pivotOffset.y,
            0
        );

        // 设置初始位置（可以是随机位置或者正确位置）
        piece.transform.position = correctWorldPosition;

        // 生成碰撞体
        PolygonCollider2D col = piece.GetComponent<PolygonCollider2D>();
        if (col != null) Destroy(col);
        col = piece.AddComponent<PolygonCollider2D>();

        // 确保碰撞体不是触发器，这样OnMouse事件才能正常工作
        col.isTrigger = false;

        // 等待一帧让精灵完全设置好，然后生成碰撞体路径
        StartCoroutine(GenerateColliderPath(col, sr));

        // 添加PuzzlePiece组件
        PuzzlePiece puzzlePiece = piece.GetComponent<PuzzlePiece>();
        if (puzzlePiece == null)
        {
            puzzlePiece = piece.AddComponent<PuzzlePiece>();
        }

        // 设置拼图块信息，使用计算出的正确世界位置（包含gridSize以自动设置normal map）
        puzzlePiece.SetPieceInfo(cy, cx, correctWorldPosition, gridSize);

        // 为PieceShadow子节点设置对应的piece mask图片
        Transform shadowTransform = piece.transform.Find("PieceShadow");
        if (shadowTransform != null)
        {
            SpriteRenderer shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
            if (shadowRenderer != null)
            {
                // 加载对应的piece mask图片
                Texture2D maskTexture = LoadPuzzleMask(cx, cy, gridSize, gridSize);
                if (maskTexture != null)
                {
                    // 创建sprite并设置到shadow renderer
                    Sprite maskSprite = Sprite.Create(maskTexture, new Rect(0, 0, maskTexture.width, maskTexture.height), new Vector2(0.5f, 0.5f), 100f);
                    shadowRenderer.sprite = maskSprite;
                    Debug.Log($"为拼图块({cx},{cy})的PieceShadow设置mask图片: {maskTexture.width}x{maskTexture.height}");
                }
                else
                {
                    Debug.LogWarning($"无法为拼图块({cx},{cy})的PieceShadow加载mask图片");
                }
            }
        }

        if (!puzzlePieces.Contains(puzzlePiece)) puzzlePieces.Add(puzzlePiece);


        Debug.Log($"拼图块({cx},{cy}): 世界尺寸={worldWidth:F2}x{worldHeight:F2}, 正确位置={correctWorldPosition}");
    }

    /// <summary>
    /// 生成碰撞体路径的协程
    /// </summary>
    System.Collections.IEnumerator GenerateColliderPath(PolygonCollider2D collider, SpriteRenderer spriteRenderer)
    {
        yield return new WaitForSeconds(1); // 等待一帧

        if (spriteRenderer.sprite != null)
        {
            // 从精灵自动生成碰撞体路径
            Sprite sprite = spriteRenderer.sprite;
            //Debug.Log($"精灵 {collider.gameObject.name} 的物理形状数量: {sprite.GetPhysicsShapeCount()}");

            // 使用精灵的物理形状生成碰撞体
            if (sprite.GetPhysicsShapeCount() > 0)
            {
                try
                {
                    var path = new List<Vector2>();
                    sprite.GetPhysicsShape(0, path);

                    if (path.Count > 0)
                    {
                        collider.points = path.ToArray();
                        //Debug.Log($"成功为拼图块 {collider.gameObject.name} 生成了 {collider.points.Length} 个碰撞体点");
                    }
                    else
                    {
                        //Debug.LogWarning($"拼图块 {collider.gameObject.name} 的物理形状路径为空，使用矩形碰撞体");
                        CreateRectangleCollider(collider, sprite);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"为拼图块 {collider.gameObject.name} 生成物理形状时出错: {e.Message}");
                    CreateRectangleCollider(collider, sprite);
                }
            }
            else
            {
                //Debug.LogWarning($"拼图块 {collider.gameObject.name} 没有物理形状，使用矩形碰撞体");
                CreateRectangleCollider(collider, sprite);
            }
        }
        else
        {
            Debug.LogError($"拼图块 {collider.gameObject.name} 的精灵为空，无法生成碰撞体");
        }
    }

    /// <summary>
    /// 创建矩形碰撞体
    /// </summary>
    void CreateRectangleCollider(PolygonCollider2D collider, Sprite sprite)
    {
        // 使用精灵的边界创建简单的矩形碰撞体
        Bounds bounds = sprite.bounds;
        Vector2[] points = new Vector2[4]
        {
            new Vector2(bounds.min.x, bounds.min.y),
            new Vector2(bounds.min.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.min.y)
        };
        collider.points = points;
        Debug.Log($"为拼图块 {collider.gameObject.name} 创建了矩形碰撞体，边界: {bounds}");
    }

}