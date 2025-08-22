using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 拼图块拖放和吸附控制脚本
/// </summary>
public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("拖放设置")]
    public float snapDistance = 1.0f; // 吸附距离
    public LayerMask puzzlePieceLayer = -1; // 拼图块图层

    [Header("视觉反馈")]
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;

    // 全局层级管理
    private static int globalTopSortingOrder = 100; // 全局最高层级

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Collider2D pieceCollider;
    private Vector3 originalPosition;
    private int originalSortingOrder;

    // 拼图块信息
    public int row;
    public int col;
    public Vector3 correctPosition; // 正确位置
    public bool isPlaced = false; // 是否已正确放置
    public int gridSize; // 拼图网格大小，用于构建normal map路径

    public AudioClip snapSound; // 吸附音效

    // Normal map相关
    private Texture2D normalMapTexture;
    private MaterialPropertyBlock materialPropertyBlock;

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        pieceCollider = GetComponent<Collider2D>();
        originalPosition = transform.position;
        originalSortingOrder = spriteRenderer.sortingOrder;

        // 确保碰撞体不是触发器，这样OnMouse事件才能正常工作
        if (pieceCollider != null)
        {
            pieceCollider.isTrigger = false;
        }

        // 注释掉这个逻辑，因为correctPosition应该在SetPieceInfo中正确设置
        // 如果这里重置了correctPosition，会导致第一个拼图块的正确位置丢失
        // if (correctPosition == Vector3.zero)
        // {
        //     correctPosition = originalPosition;
        // }
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        EventDispatcher.Dispatch(EventNames.SELECT_PIECE, this);
        if (isPlaced) return; // 已正确放置的拼图块不能再拖动

        isDragging = true;

    

        // 检查主相机是否存在
        if (mainCamera == null)
        {
            Debug.LogError("主相机为空！无法进行坐标转换");
            return;
        }

        // 计算鼠标与拼图块的偏移
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = mainCamera.nearClipPlane; // 设置正确的Z值
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(screenPos);
        mousePos.z = 0;
        offset = transform.position - mousePos;

        // 提升拼图块的渲染层级到全局最高
        globalTopSortingOrder++;
        spriteRenderer.sortingOrder = globalTopSortingOrder;

        // 同时设置PieceShadow的sortingOrder
        Transform shadowTransform = transform.Find("PieceShadow");
        if (shadowTransform != null)
        {
            SpriteRenderer shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = globalTopSortingOrder;
            }
        }

        // 高亮显示
        spriteRenderer.color = highlightColor;

        //Debug.Log($"OnMouseDown - 主相机: {mainCamera?.name}, 屏幕坐标: {Input.mousePosition}, 世界坐标: {mousePos}, 偏移: {offset}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isPlaced) return;

        // 检查主相机是否存在
        if (mainCamera == null)
        {
            Debug.LogError("主相机为空！无法进行坐标转换");
            return;
        }

        // 更新拼图块位置
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = mainCamera.nearClipPlane; // 设置正确的Z值
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(screenPos);
        mousePos.z = 0;
        transform.position = mousePos + offset;

        // 调试输出坐标信息
        //Debug.Log($"OnMouseDrag - 主相机: {mainCamera?.name}, 屏幕坐标: {Input.mousePosition}, 世界坐标: {mousePos}, 拼图块位置: {transform.position}");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
  
        if (!isDragging) return;

        isDragging = false;

        // 放手后立即恢复正常颜色
        spriteRenderer.color = normalColor;

        // 检查是否可以吸附到正确位置
        CheckForSnap();
    }


    //void OnMouseDown()
    //{
    //    if (isPlaced) return; // 已正确放置的拼图块不能再拖动

    //    isDragging = true;

    //    // 检查主相机是否存在
    //    if (mainCamera == null)
    //    {
    //        Debug.LogError("主相机为空！无法进行坐标转换");
    //        return;
    //    }

    //    // 计算鼠标与拼图块的偏移
    //    Vector3 screenPos = Input.mousePosition;
    //    screenPos.z = mainCamera.nearClipPlane; // 设置正确的Z值
    //    Vector3 mousePos = mainCamera.ScreenToWorldPoint(screenPos);
    //    mousePos.z = 0;
    //    offset = transform.position - mousePos;

    //    // 提升拼图块的渲染层级到全局最高
    //    globalTopSortingOrder++;
    //    spriteRenderer.sortingOrder = globalTopSortingOrder;

    //    // 同时设置PieceShadow的sortingOrder
    //    Transform shadowTransform = transform.Find("PieceShadow");
    //    if (shadowTransform != null)
    //    {
    //        SpriteRenderer shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
    //        if (shadowRenderer != null)
    //        {
    //            shadowRenderer.sortingOrder = globalTopSortingOrder;
    //        }
    //    }

    //    // 高亮显示
    //    spriteRenderer.color = highlightColor;

    //    //Debug.Log($"OnMouseDown - 主相机: {mainCamera?.name}, 屏幕坐标: {Input.mousePosition}, 世界坐标: {mousePos}, 偏移: {offset}");
    //}

    //void OnMouseDrag()
    //{
    //    if (!isDragging || isPlaced) return;

    //    // 检查主相机是否存在
    //    if (mainCamera == null)
    //    {
    //        Debug.LogError("主相机为空！无法进行坐标转换");
    //        return;
    //    }

    //    // 更新拼图块位置
    //    Vector3 screenPos = Input.mousePosition;
    //    screenPos.z = mainCamera.nearClipPlane; // 设置正确的Z值
    //    Vector3 mousePos = mainCamera.ScreenToWorldPoint(screenPos);
    //    mousePos.z = 0;
    //    transform.position = mousePos + offset;

    //    // 调试输出坐标信息
    //    //Debug.Log($"OnMouseDrag - 主相机: {mainCamera?.name}, 屏幕坐标: {Input.mousePosition}, 世界坐标: {mousePos}, 拼图块位置: {transform.position}");
    //}

    //void OnMouseUp()
    //{
    //    if (!isDragging) return;

    //    isDragging = false;

    //    // 放手后立即恢复正常颜色
    //    spriteRenderer.color = normalColor;

    //    // 检查是否可以吸附到正确位置
    //    CheckForSnap();
    //}

    /// <summary>
    /// 检查吸附
    /// </summary>
    void CheckForSnap()
    {
        // 检查是否接近正确位置
        float distanceToCorrect = Vector3.Distance(transform.position, correctPosition);

        if (distanceToCorrect <= snapDistance)
        {
            // 吸附到正确位置
            SnapToCorrectPosition();
        }
        else
        {
            // 检查是否可以与其他拼图块连接，暂时不处理与其他拼图块连接，因为连接之后需要将两者看作一个整体，后面再填充这个逻辑
            //CheckForConnection();
        }
    }

    /// <summary>
    /// 吸附到正确位置
    /// </summary>
    public void SnapToCorrectPosition()
    {
        //transform.position = correctPosition;
        transform.DOMove(correctPosition, 0.2f).SetEase(Ease.OutCubic);
        isPlaced = true;

        // 正确放置后恢复到原始层级和颜色
        spriteRenderer.sortingOrder = originalSortingOrder;

        // 同时恢复PieceShadow的sortingOrder
        Transform shadowTransform = transform.Find("PieceShadow");
        if (shadowTransform != null)
        {
            SpriteRenderer shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = originalSortingOrder;
            }
        }
        spriteRenderer.color = normalColor;

        // 播放吸附音效（如果有的话）
        AudioSource.PlayClipAtPoint(snapSound, transform.position);

        EventDispatcher.Dispatch(EventNames.DESELECT_PIECE, this);

        // 检查是否完成拼图
        CheckPuzzleCompletion();
    }

    /// <summary>
    /// 检查与其他拼图块的连接
    /// </summary>
    void CheckForConnection()
    {
        // 获取附近的拼图块
        Collider2D[] nearbyPieces = Physics2D.OverlapCircleAll(transform.position, snapDistance * 2, puzzlePieceLayer);

        foreach (Collider2D piece in nearbyPieces)
        {
            if (piece.gameObject == gameObject) continue;

            PuzzlePiece otherPiece = piece.GetComponent<PuzzlePiece>();
            if (otherPiece != null && CanConnectTo(otherPiece))
            {
                // 吸附到相邻拼图块
                SnapToPiece(otherPiece);
                return;
            }
        }
    }

    /// <summary>
    /// 检查是否可以连接到指定拼图块
    /// </summary>
    bool CanConnectTo(PuzzlePiece otherPiece)
    {
        // 检查是否是相邻的拼图块
        int rowDiff = Mathf.Abs(row - otherPiece.row);
        int colDiff = Mathf.Abs(col - otherPiece.col);

        // 只能连接到相邻的拼图块（上下左右）
        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }

    /// <summary>
    /// 吸附到指定拼图块
    /// </summary>
    void SnapToPiece(PuzzlePiece otherPiece)
    {
        // 计算相对于其他拼图块的正确位置
        Vector3 targetPosition = CalculateRelativePosition(otherPiece);

        //transform.position = targetPosition;
        transform.DOMove(targetPosition, 0.2f).SetEase(Ease.OutCubic);

        // 如果两个拼图块都在正确的相对位置，则标记为已放置
        if (Vector3.Distance(targetPosition, correctPosition) <= 0.1f)
        {
            //isPlaced = true;
            CheckPuzzleCompletion();
        }
    }

    /// <summary>
    /// 计算相对于其他拼图块的位置
    /// </summary>
    Vector3 CalculateRelativePosition(PuzzlePiece otherPiece)
    {
        Vector3 relativeOffset = correctPosition - otherPiece.correctPosition;
        return otherPiece.transform.position + relativeOffset;
    }

    /// <summary>
    /// 检查拼图是否完成
    /// </summary>
    void CheckPuzzleCompletion()
    {
        PuzzlePiece[] allPieces = FindObjectsOfType<PuzzlePiece>();
        bool allPlaced = true;

        foreach (PuzzlePiece piece in allPieces)
        {
            if (!piece.isPlaced)
            {
                allPlaced = false;
                break;
            }
        }

        if (allPlaced)
        {
            OnPuzzleCompleted();
        }
    }

    /// <summary>
    /// 拼图完成时调用
    /// </summary>
    void OnPuzzleCompleted()
    {
        Debug.Log("拼图完成！");
        // 这里可以添加完成拼图的逻辑，比如播放动画、音效等
        EventDispatcher.Dispatch(EventNames.PUZZLE_COMPLETED);
    }

    /// <summary>
    /// 重置拼图块位置
    /// </summary>
    public void ResetPosition()
    {
        transform.position = originalPosition;
        isPlaced = false;
        spriteRenderer.color = normalColor;
        spriteRenderer.sortingOrder = originalSortingOrder;
    }

    /// <summary>
    /// 设置拼图块信息
    /// </summary>
    public void SetPieceInfo(int pieceRow, int pieceCol, Vector3 correctPos)
    {
        row = pieceRow;
        col = pieceCol;
        correctPosition = correctPos;
    }

    /// <summary>
    /// 设置拼图块信息（包含gridSize）
    /// </summary>
    public void SetPieceInfo(int pieceRow, int pieceCol, Vector3 correctPos, int puzzleGridSize)
    {
        row = pieceRow;
        col = pieceCol;
        correctPosition = correctPos;
        gridSize = puzzleGridSize;

        // 自动设置normal map
        SetNormalMap();
    }

    /// <summary>
    /// 设置法线贴图
    /// </summary>
    public void SetNormalMap()
    {
        if (gridSize <= 0)
        {
            Debug.LogWarning($"PuzzlePiece ({row},{col}): gridSize未设置，无法加载normal map");
            return;
        }

        // 构建normal map路径：Resources/Images/mask/{gridSize}x{gridSize}/{row}_{col}_Normal.png
        string normalMapPath = $"Images/mask/{gridSize}x{gridSize}/{row}_{col}_Normal";

        // 从Resources加载normal map
        normalMapTexture = Resources.Load<Texture2D>(normalMapPath);

        if (normalMapTexture != null)
        {
            // 应用normal map到材质
            ApplyNormalMapToRenderer();
            Debug.Log($"PuzzlePiece ({row},{col}): 成功加载normal map: {normalMapPath}");
        }
        else
        {
            Debug.LogWarning($"PuzzlePiece ({row},{col}): 无法找到normal map: {normalMapPath}");
        }
    }

    /// <summary>
    /// 将法线贴图应用到渲染器
    /// </summary>
    private void ApplyNormalMapToRenderer()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError($"PuzzlePiece ({row},{col}): 未找到Renderer组件");
            return;
        }

        // 初始化MaterialPropertyBlock
        if (materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        // 获取当前的MaterialPropertyBlock
        rend.GetPropertyBlock(materialPropertyBlock);

        // 设置法线贴图 (URP Lit shader使用_BumpMap)
        materialPropertyBlock.SetTexture("_BumpMap", normalMapTexture);

        // 启用法线贴图
        materialPropertyBlock.SetFloat("_BumpScale", 1.0f);

        // 确保材质启用法线贴图关键字
        Material mat = rend.material;
        if (mat != null && normalMapTexture != null)
        {
            mat.EnableKeyword("_NORMALMAP");
        }

        // 应用MaterialPropertyBlock
        rend.SetPropertyBlock(materialPropertyBlock);
    }

    /// <summary>
    /// 手动设置法线贴图（可选的公共接口）
    /// </summary>
    /// <param name="normalTexture">法线贴图纹理</param>
    public void SetCustomNormalMap(Texture2D normalTexture)
    {
        if (normalTexture == null)
        {
            Debug.LogWarning($"PuzzlePiece ({row},{col}): 传入的normal map为空");
            return;
        }

        normalMapTexture = normalTexture;
        ApplyNormalMapToRenderer();
        Debug.Log($"PuzzlePiece ({row},{col}): 手动设置normal map成功");
    }

    // 可视化调试
    void OnDrawGizmos()
    {
        // 绘制正确位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(correctPosition, 0.2f);

        // 绘制吸附范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, snapDistance);
    }

    void OnDrawGizmosSelected()
    {
        // 选中时绘制更明显的调试信息
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(correctPosition, 0.3f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapDistance);

        // 绘制从当前位置到正确位置的连线
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, correctPosition);
    }


}