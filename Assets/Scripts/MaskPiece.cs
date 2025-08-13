using UnityEngine;

/// <summary>
/// 拼图块类，包含四个边的属性
/// </summary>
public class MaskPiece
{
    /// <summary>
    /// 边的类型枚举
    /// </summary>
    public enum EdgeType
    {
        Edge = 0,      // 边缘（平直边）
        Protrude = 1,  // 凸出
        Concave = 2    // 凹陷
    }

    /// <summary>
    /// 上边属性
    /// </summary>
    public EdgeType TopEdge { get; set; }

    /// <summary>
    /// 右边属性
    /// </summary>
    public EdgeType RightEdge { get; set; }

    /// <summary>
    /// 下边属性
    /// </summary>
    public EdgeType BottomEdge { get; set; }

    /// <summary>
    /// 左边属性
    /// </summary>
    public EdgeType LeftEdge { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="top">上边类型</param>
    /// <param name="right">右边类型</param>
    /// <param name="bottom">下边类型</param>
    /// <param name="left">左边类型</param>
    public MaskPiece(EdgeType top, EdgeType right, EdgeType bottom, EdgeType left)
    {
        TopEdge = top;
        RightEdge = right;
        BottomEdge = bottom;
        LeftEdge = left;
    }

    /// <summary>
    /// 默认构造函数，所有边都是边缘
    /// </summary>
    public MaskPiece()
    {
        TopEdge = EdgeType.Edge;
        RightEdge = EdgeType.Edge;
        BottomEdge = EdgeType.Edge;
        LeftEdge = EdgeType.Edge;
    }

    /// <summary>
    /// 获取指定方向的边类型
    /// </summary>
    /// <param name="direction">方向 (0=上, 1=右, 2=下, 3=左)</param>
    /// <returns>边类型</returns>
    public EdgeType GetEdge(int direction)
    {
        switch (direction)
        {
            case 0: return TopEdge;
            case 1: return RightEdge;
            case 2: return BottomEdge;
            case 3: return LeftEdge;
            default: return EdgeType.Edge;
        }
    }

    /// <summary>
    /// 设置指定方向的边类型
    /// </summary>
    /// <param name="direction">方向 (0=上, 1=右, 2=下, 3=左)</param>
    /// <param name="edgeType">边类型</param>
    public void SetEdge(int direction, EdgeType edgeType)
    {
        switch (direction)
        {
            case 0: TopEdge = edgeType; break;
            case 1: RightEdge = edgeType; break;
            case 2: BottomEdge = edgeType; break;
            case 3: LeftEdge = edgeType; break;
        }
    }

    /// <summary>
    /// 检查是否为边缘块（至少有一边是边缘）
    /// </summary>
    /// <returns>是否为边缘块</returns>
    public bool IsEdgePiece()
    {
        return TopEdge == EdgeType.Edge || RightEdge == EdgeType.Edge || 
               BottomEdge == EdgeType.Edge || LeftEdge == EdgeType.Edge;
    }

    /// <summary>
    /// 检查是否为角块（有两条相邻的边是边缘）
    /// </summary>
    /// <returns>是否为角块</returns>
    public bool IsCornerPiece()
    {
        int edgeCount = 0;
        if (TopEdge == EdgeType.Edge) edgeCount++;
        if (RightEdge == EdgeType.Edge) edgeCount++;
        if (BottomEdge == EdgeType.Edge) edgeCount++;
        if (LeftEdge == EdgeType.Edge) edgeCount++;

        return edgeCount == 2 && (
            (TopEdge == EdgeType.Edge && RightEdge == EdgeType.Edge) ||
            (RightEdge == EdgeType.Edge && BottomEdge == EdgeType.Edge) ||
            (BottomEdge == EdgeType.Edge && LeftEdge == EdgeType.Edge) ||
            (LeftEdge == EdgeType.Edge && TopEdge == EdgeType.Edge)
        );
    }

    /// <summary>
    /// 检查两个拼图块是否可以相邻放置
    /// </summary>
    /// <param name="other">另一个拼图块</param>
    /// <param name="direction">当前块相对于另一个块的方向 (0=上, 1=右, 2=下, 3=左)</param>
    /// <returns>是否可以相邻放置</returns>
    public bool CanConnectTo(MaskPiece other, int direction)
    {
        EdgeType thisEdge = GetEdge(direction);
        EdgeType otherEdge = other.GetEdge((direction + 2) % 4); // 相对方向

        // 边缘只能与边缘连接
        if (thisEdge == EdgeType.Edge || otherEdge == EdgeType.Edge)
        {
            return thisEdge == EdgeType.Edge && otherEdge == EdgeType.Edge;
        }

        // 凸出与凹陷可以连接
        return (thisEdge == EdgeType.Protrude && otherEdge == EdgeType.Concave) ||
               (thisEdge == EdgeType.Concave && otherEdge == EdgeType.Protrude);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"MaskPiece(Top:{TopEdge}, Right:{RightEdge}, Bottom:{BottomEdge}, Left:{LeftEdge})";
    }
}