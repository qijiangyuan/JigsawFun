using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 拼图块拖放和吸附控制脚本
/// </summary>
public class PuzzlePiece : MonoBehaviour
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

    public AudioClip snapSound; // 吸附音效

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

        // 设置正确位置（如果没有设置的话）
        if (correctPosition == Vector3.zero)
        {
            correctPosition = originalPosition;
        }
    }

    void OnMouseDown()
    {
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

        // 高亮显示
        spriteRenderer.color = highlightColor;

        //Debug.Log($"OnMouseDown - 主相机: {mainCamera?.name}, 屏幕坐标: {Input.mousePosition}, 世界坐标: {mousePos}, 偏移: {offset}");
    }

    void OnMouseDrag()
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

    void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;

        // 放手后立即恢复正常颜色
        spriteRenderer.color = normalColor;

        // 检查是否可以吸附到正确位置
        CheckForSnap();
    }

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
            // 检查是否可以与其他拼图块连接
            CheckForConnection();
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
        spriteRenderer.color = normalColor;

        // 播放吸附音效（如果有的话）
        AudioSource.PlayClipAtPoint(snapSound, transform.position);

        // 检查是否完成拼图
        CheckPuzzleCompletion();
    }

    /// <summary>
    /// 检查与其他拼图块的连接
    /// </summary>
    void CheckForConnection()
    {
        // 获取附近的拼图块
        Collider2D[] nearbyPieces = Physics2D.OverlapCircleAll(transform.position, snapDistance, puzzlePieceLayer);

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