using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Tile
{
  public enum Direction
  {
    UP, DOWN, LEFT, RIGHT,
  }
  public enum PosNegType
  {
    POS,
    NEG,
    NONE,
  }

  // The offset at which the curve will start.
  // For an image of size 140 by 140 it will start at 20, 20.
  //public Vector2Int mOffset = new Vector2Int(20, 20);
  public static int padding = 20;

  // The size of our jigsaw tile.
  public static int tileSize = 100;

  // The line renderers for all directions and types.
  private Dictionary<(Direction, PosNegType), LineRenderer> mLineRenderers
    = new Dictionary<(Direction, PosNegType), LineRenderer>();

  // Lets store the list of bezier curve points created
  // from the template bezier curve control points.
  public static List<Vector2> BezCurve =
    BezierCurve.PointList2(TemplateBezierCurve.templateControlPoints, 0.001f);

  /// <summary>
  /// 设置运行时的拼图块边长（像素），并按比例重建贝塞尔曲线点集：
  /// - 模板控制点以 0..100 为基准，按 scale = tileSize/100f 进行等比缩放
  /// - 重新生成 BezCurve，确保曲线长度与 tileSize 保持一致
  /// </summary>
  public static void SetTileSize(int newTileSize)
  {
    if (newTileSize <= 0) newTileSize = 1;
    if (tileSize == newTileSize && BezCurve != null && BezCurve.Count > 0)
    {
      return;
    }

    tileSize = newTileSize;

    float scale = tileSize / 100f;
    List<Vector2> scaled = new List<Vector2>(TemplateBezierCurve.templateControlPoints.Count);
    for (int i = 0; i < TemplateBezierCurve.templateControlPoints.Count; i++)
    {
      Vector2 p = TemplateBezierCurve.templateControlPoints[i];
      scaled.Add(new Vector2(p.x * scale, p.y * scale));
    }
    BezCurve = BezierCurve.PointList2(scaled, 0.001f);
  }

  // The original texture used to create the jigsaw tile.
  private Texture2D mOriginalTexture;

  public Texture2D finalCut { get; private set; }

  public static readonly Color TransparentColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

  private PosNegType[] mCurveTypes = new PosNegType[4]
  {
    PosNegType.NONE,
    PosNegType.NONE,
    PosNegType.NONE,
    PosNegType.NONE,
  };

  // A 2d boolean array that stores whether a particular
  // pixel is visited. We will need this array for the flood fill.
  private bool[,] mVisited;

  // A stack needed for the flood fill of the texture.
  private Stack<Vector2Int> mStack = new Stack<Vector2Int>();

  // Color32 批处理：源纹理缓存与输出缓冲
  private static Texture2D sCachedTexture;
  private static Color32[] sCachedPixels;
  private static int sCachedWidth;
  private static int sCachedHeight;

  private Color32[] mOutBuffer; // 当前拼图块的输出像素缓冲（宽高均为 mWidthHeight）
  private int mWidthHeight;     // = tileSize + 2*padding
  private int mBaseX;           // 源纹理中的起始 X（xIndex*tileSize）
  private int mBaseY;           // 源纹理中的起始 Y（yIndex*tileSize）

  public int xIndex = 0;
  public int yIndex = 0;

  // For tiles sorting.
  public static TilesSorting tilesSorting = new TilesSorting();
  public void SetCurveType(Direction dir, PosNegType type)
  {
    mCurveTypes[(int)dir] = type;
  }

  public PosNegType GetCurveType(Direction dir)
  {
    return mCurveTypes[(int)dir];
  }

  public Tile(Texture2D texture)
  {
    mOriginalTexture = texture;
    //int padding = mOffset.x;
    int tileSizeWithPadding = 2 * padding + tileSize;

    finalCut = new Texture2D(tileSizeWithPadding, tileSizeWithPadding, TextureFormat.ARGB32, false);
    // 对拼图块的运行时纹理启用像素对齐与边缘 Clamp，避免左/下 1px 因双线性/环绕取样造成的可视丢失
    finalCut.filterMode = FilterMode.Point;
    finalCut.wrapMode = TextureWrapMode.Clamp;

    // We initialise this newly created texture with transparent color.
    for (int i = 0; i < tileSizeWithPadding; ++i)
    {
      for (int j = 0; j < tileSizeWithPadding; ++j)
      {
        finalCut.SetPixel(i, j, TransparentColor);
      }
    }
  }

  /// <summary>
  /// 应用当前 Tile 的像素生成：
  /// - 初始化 FloodFill（计算边界/起点）
  /// - 使用 Color32[] 缓冲执行洪泛填充，将源纹理对应区域复制到输出缓冲
  /// - 一次性 SetPixels32 + Apply 提交，避免逐像素 API 调用带来的 CPU 开销
  /// </summary>
  public void Apply()
  {
    // 计算尺寸与基准偏移
    mWidthHeight = padding * 2 + tileSize;
    // 修正：基准应为“内容起点”，即 xIndex*tileSize / yIndex*tileSize；
    // 外圈全局 padding 的防越界由 Fill() 中的钳制到 [padding, width-1-padding] 负责，
    // 这里不再额外 +Tile.padding，否则会导致内容整体右/上偏移一个 padding。
    mBaseX = xIndex * tileSize;
    mBaseY = yIndex * tileSize;

    // 源纹理像素缓存（仅在纹理变更时获取一次）
    if (sCachedTexture != mOriginalTexture)
    {
      sCachedTexture = mOriginalTexture;
      sCachedWidth = mOriginalTexture.width;
      sCachedHeight = mOriginalTexture.height;
      sCachedPixels = mOriginalTexture.GetPixels32();
    }

    // 分配输出缓冲（默认全 0，即完全透明）
    int total = mWidthHeight * mWidthHeight;
    if (mOutBuffer == null || mOutBuffer.Length != total)
    {
      mOutBuffer = new Color32[total];
    }
    else
    {
      // 清空（置零）
      System.Array.Clear(mOutBuffer, 0, mOutBuffer.Length);
    }

    FloodFillInit();
    FloodFill();

    // 一次性提交像素
    finalCut.SetPixels32(mOutBuffer);
    finalCut.Apply();
  }

  /// <summary>
  /// 将一个像素位置 (x, y) 填充到输出缓冲：
  /// 从源纹理对应位置读取颜色，并将采样坐标钳制在“内容矩形”（去除 padding 的区域）内，
  /// 以避免外圈 piece 采到底图 padding 产生白边。
  /// </summary>
  void Fill(int x, int y)
  {
    int srcX = mBaseX + x;
    int srcY = mBaseY + y;

    // 钳制到“内容矩形”范围 [padding, width-1-padding]，避免采到外侧 padding 造成白边
    int minX = Tile.padding;
    int maxX = sCachedWidth - 1 - Tile.padding;
    int minY = Tile.padding;
    int maxY = sCachedHeight - 1 - Tile.padding;

    if (srcX < minX) srcX = minX; else if (srcX > maxX) srcX = maxX;
    if (srcY < minY) srcY = minY; else if (srcY > maxY) srcY = maxY;

    Color32 c = sCachedPixels[srcY * sCachedWidth + srcX];
    c.a = 255;
    mOutBuffer[y * mWidthHeight + x] = c;
  }

  /// <summary>
  /// 将 (x0,y0)-(x1,y1) 的整数像素连线标记为访问（作为洪泛填充边界）。
  /// 使用 Bresenham 算法保证像素级连通，避免曲线采样点稀疏导致的“漏边”。
  /// </summary>
  void MarkVisitedLine(int x0, int y0, int x1, int y1, int bound)
  {
    // 边界钳制的局部函数
    int Clamp(int v) => (v < 0) ? 0 : (v >= bound ? bound - 1 : v);

    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);
    int sx = x0 < x1 ? 1 : -1;
    int sy = y0 < y1 ? 1 : -1;
    int err = dx - dy;

    int x = x0, y = y0;
    while (true)
    {
      mVisited[Clamp(x), Clamp(y)] = true;
      if (x == x1 && y == y1) break;
      int e2 = 2 * err;
      if (e2 > -dy) { err -= dy; x += sx; }
      if (e2 < dx)  { err += dx; y += sy; }
    }
  }

  /// <summary>
  /// 洪泛填充初始化：
  /// - 重置访问数组
  /// - 构建“完整闭合”的边界像素：严格按顺时针 CW 次序（DOWN→RIGHT→UP→LEFT），
  ///   并对 UP、LEFT 反向点序以确保角点连续；
  /// - 使用 Bresenham 在线段间连线与首尾闭合，得到像素级连续的封闭边界；
  /// - 选择中心点作为起点入栈。
  /// 说明：此前直接按枚举顺序（UP,DOWN,LEFT,RIGHT）首尾相连，会在 UP→DOWN 等非相邻边之间拉“对角线”，
  ///       造成内部阻断或错误裁切。本实现显式按 CW 顺序连接边，避免跨边连线与角点缝隙。
  /// </summary>
  void FloodFillInit()
  {
    int tileSizeWithPadding = 2 * padding + tileSize;

    mVisited = new bool[tileSizeWithPadding, tileSizeWithPadding];
    for (int i = 0; i < tileSizeWithPadding; ++i)
    {
      for (int j = 0; j < tileSizeWithPadding; ++j)
      {
        mVisited[i, j] = false;
      }
    }

    // 1) 按边生成采样点
    var downPts  = CreateCurve(Direction.DOWN,  mCurveTypes[(int)Direction.DOWN]);   // 左→右（底边）
    var rightPts = CreateCurve(Direction.RIGHT, mCurveTypes[(int)Direction.RIGHT]);  // 下→上（右边）
    var upPts    = CreateCurve(Direction.UP,    mCurveTypes[(int)Direction.UP]);     // 左→右（顶边，需反转）
    var leftPts  = CreateCurve(Direction.LEFT,  mCurveTypes[(int)Direction.LEFT]);   // 下→上（左边，需反转）

    // 2) 为保证 CW 行走方向上角点连续：UP 反向（右→左），LEFT 反向（上→下）
    if (upPts != null && upPts.Count > 1) upPts.Reverse();
    if (leftPts != null && leftPts.Count > 1) leftPts.Reverse();

    // 3) 汇总为一条闭合周界（按 CW 顺序）
    List<Vector2> perimeter = new List<Vector2>(
      (downPts?.Count ?? 0) + (rightPts?.Count ?? 0) + (upPts?.Count ?? 0) + (leftPts?.Count ?? 0)
    );
    if (downPts  != null) perimeter.AddRange(downPts);
    if (rightPts != null) perimeter.AddRange(rightPts);
    if (upPts    != null) perimeter.AddRange(upPts);
    if (leftPts  != null) perimeter.AddRange(leftPts);

    // 4) 边界像素标记与连线（包含首尾闭合）
    if (perimeter.Count > 0)
    {
      // 逐点标记以提高密度
      for (int k = 0; k < perimeter.Count; ++k)
      {
        int px = Mathf.Clamp(Mathf.RoundToInt(perimeter[k].x), 0, tileSizeWithPadding - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(perimeter[k].y), 0, tileSizeWithPadding - 1);
        mVisited[px, py] = true;
      }

      // 相邻点连线，保证像素级连通
      int prevX = Mathf.Clamp(Mathf.RoundToInt(perimeter[0].x), 0, tileSizeWithPadding - 1);
      int prevY = Mathf.Clamp(Mathf.RoundToInt(perimeter[0].y), 0, tileSizeWithPadding - 1);
      for (int k = 1; k < perimeter.Count; ++k)
      {
        int x1 = Mathf.Clamp(Mathf.RoundToInt(perimeter[k].x), 0, tileSizeWithPadding - 1);
        int y1 = Mathf.Clamp(Mathf.RoundToInt(perimeter[k].y), 0, tileSizeWithPadding - 1);
        MarkVisitedLine(prevX, prevY, x1, y1, tileSizeWithPadding);
        prevX = x1; prevY = y1;
      }
      // 首尾闭合
      int firstX = Mathf.Clamp(Mathf.RoundToInt(perimeter[0].x), 0, tileSizeWithPadding - 1);
      int firstY = Mathf.Clamp(Mathf.RoundToInt(perimeter[0].y), 0, tileSizeWithPadding - 1);
      MarkVisitedLine(prevX, prevY, firstX, firstY, tileSizeWithPadding);
    }

    // 5) 从中心点开始填充
    Vector2Int start = new Vector2Int(tileSizeWithPadding / 2, tileSizeWithPadding / 2);
    mVisited[start.x, start.y] = true;
    mStack.Push(start);
  }

  /// <summary>
  /// 非递归栈式洪泛填充，按四邻域扩展区域，将每个像素拷贝到输出缓冲。
  /// </summary>
  void FloodFill()
  {
    //int padding = mOffset.x;
    int width_height = padding * 2 + tileSize;

    while (mStack.Count > 0)
    {
      Vector2Int v = mStack.Pop();

      int xx = v.x;
      int yy = v.y;

      Fill(v.x, v.y);

      // Check right.
      int x = xx + 1;
      int y = yy;

      if (x < width_height)
      {
        if (!mVisited[x, y])
        {
          mVisited[x, y] = true;
          mStack.Push(new Vector2Int(x, y));
        }
      }

      // check left.
      x = xx - 1;
      y = yy;
      if (x > 0)
      {
        if (!mVisited[x, y])
        {
          mVisited[x, y] = true;
          mStack.Push(new Vector2Int(x, y));
        }
      }

      // Check up.
      x = xx;
      y = yy + 1;

      if (y < width_height)
      {
        if (!mVisited[x, y])
        {
          mVisited[x, y] = true;
          mStack.Push(new Vector2Int(x, y));
        }
      }

      // Check down.
      x = xx;
      y = yy - 1;

      if (y >= 0)
      {
        if (!mVisited[x, y])
        {
          mVisited[x, y] = true;
          mStack.Push(new Vector2Int(x, y));
        }
      }
    }
  }

  public static LineRenderer CreateLineRenderer(UnityEngine.Color color, float lineWidth = 1.0f)
  {
    GameObject obj = new GameObject();
    LineRenderer lr = obj.AddComponent<LineRenderer>();

    lr.startColor = color;
    lr.endColor = color;
    lr.startWidth = lineWidth;
    lr.endWidth = lineWidth;
    lr.material = new Material(Shader.Find("Sprites/Default"));
    return lr;
  }

  public static void TranslatePoints(List<Vector2> iList, Vector2 offset)
  {
    for (int i = 0; i < iList.Count; i++)
    {
      iList[i] += offset;
    }
  }

  public static void InvertY(List<Vector2> iList)
  {
    for (int i = 0; i < iList.Count; i++)
    {
      iList[i] = new Vector2(iList[i].x, -iList[i].y);
    }
  }

  public static void SwapXY(List<Vector2> iList)
  {
    for (int i = 0; i < iList.Count; ++i)
    {
      iList[i] = new Vector2(iList[i].y, iList[i].x);
    }
  }

  public List<Vector2> CreateCurve(Direction dir, PosNegType type)
  {
    int padding_x = padding;// mOffset.x;
    int padding_y = padding;// mOffset.y;
    int sw = tileSize;
    int sh = tileSize;

    List<Vector2> pts = new List<Vector2>(BezCurve);
    switch (dir)
    {
      case Direction.UP:
        if (type == PosNegType.POS)
        {
          TranslatePoints(pts, new Vector2(padding_x, padding_y + sh));
        }
        else if (type == PosNegType.NEG)
        {
          InvertY(pts);
          TranslatePoints(pts, new Vector2(padding_x, padding_y + sh));
        }
        else
        {
          pts.Clear();
          for (int i = 0; i < tileSize; ++i)
          {
            pts.Add(new Vector2(i + padding_x, padding_y + sh));
          }
        }
        break;
      case Direction.RIGHT:
        if (type == PosNegType.POS)
        {
          SwapXY(pts);
          TranslatePoints(pts, new Vector2(padding_x + sw, padding_y));
        }
        else if (type == PosNegType.NEG)
        {
          InvertY(pts);
          SwapXY(pts);
          TranslatePoints(pts, new Vector2(padding_x + sw, padding_y));
        }
        else
        {
          pts.Clear();
          for (int i = 0; i < tileSize; ++i)
          {
            pts.Add(new Vector2(padding_x + sw, i + padding_y));
          }
        }
        break;
      case Direction.DOWN:
        if (type == PosNegType.POS)
        {
          InvertY(pts);
          TranslatePoints(pts, new Vector2(padding_x, padding_y));
        }
        else if (type == PosNegType.NEG)
        {
          TranslatePoints(pts, new Vector2(padding_x, padding_y));
        }
        else
        {
          pts.Clear();
          for (int i = 0; i < tileSize; ++i)
          {
            pts.Add(new Vector2(i + padding_x, padding_y));
          }
        }
        break;
      case Direction.LEFT:
        if (type == PosNegType.POS)
        {
          InvertY(pts);
          SwapXY(pts);
          TranslatePoints(pts, new Vector2(padding_x, padding_y));
        }
        else if (type == PosNegType.NEG)
        {
          SwapXY(pts);
          TranslatePoints(pts, new Vector2(padding_x, padding_y));
        }
        else
        {
          pts.Clear();
          for (int i = 0; i < tileSize; ++i)
          {
            pts.Add(new Vector2(padding_x, i + padding_y));
          }
        }
        break;
    }
    return pts;
  }

  public void DrawCurve(Direction dir, PosNegType type, UnityEngine.Color color)
  {
    if (!mLineRenderers.ContainsKey((dir, type)))
    {
      mLineRenderers.Add((dir, type), CreateLineRenderer(color));
    }

    LineRenderer lr = mLineRenderers[(dir, type)];
    lr.gameObject.SetActive(true);
    lr.startColor = color;
    lr.endColor = color;
    lr.gameObject.name = "LineRenderer_" + dir.ToString() + "_" + type.ToString();
    List<Vector2> pts = CreateCurve(dir, type);

    lr.positionCount = pts.Count;
    for (int i = 0; i < pts.Count; ++i)
    {
      lr.SetPosition(i, pts[i]);
    }
  }

  public void HideAllCurves()
  {
    foreach (var item in mLineRenderers)
    {
      item.Value.gameObject.SetActive(false);
    }
  }

  public void DestroyAllCurves()
  {
    foreach (var item in mLineRenderers)
    {
      GameObject.Destroy(item.Value.gameObject);
    }

    mLineRenderers.Clear();
  }

}
