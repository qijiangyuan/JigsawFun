using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 拼图盘管理器
/// </summary>
public class PuzzleBoard : MonoBehaviour
{
    [Header("拼图设置")]
    public GameObject piecePrefab; // 拼图块预制体
    public int gridSize = 4; // 拼图网格大小
    public float pieceSpacing = 1.2f; // 拼图块间距
    
    [Header("布局设置")]
    public Transform puzzleArea; // 拼图区域
    public Transform shuffleArea; // 打乱区域
    public Vector2 shuffleAreaSize = new Vector2(10, 6); // 打乱区域大小
    
    [Header("游戏设置")]
    public bool autoShuffle = true; // 自动打乱
    public float shuffleForce = 5f; // 打乱力度
    
    [Header("组件引用")]
    public JigsawGenerator jigsawGenerator; // 拼图生成器引用
    
    private List<PuzzlePiece> puzzlePieces = new List<PuzzlePiece>();
    private bool isGameStarted = false;
    
    void Start()
    {
        // 查找JigsawGenerator组件（如果未手动分配）
        if (jigsawGenerator == null)
        {
            jigsawGenerator = GetComponent<JigsawGenerator>();
        }
        
        if (jigsawGenerator == null)
        {
            Debug.LogError("PuzzleBoard需要JigsawGenerator组件！");
            return;
        }
        
        // 设置默认区域
        SetupDefaultAreas();
    }
    
    /// <summary>
    /// 设置默认区域
    /// </summary>
    void SetupDefaultAreas()
    {
        if (puzzleArea == null)
        {
            GameObject puzzleAreaObj = new GameObject("PuzzleArea");
            puzzleAreaObj.transform.parent = transform;
            puzzleAreaObj.transform.localPosition = Vector3.zero;
            puzzleArea = puzzleAreaObj.transform;
        }
        
        if (shuffleArea == null)
        {
            GameObject shuffleAreaObj = new GameObject("ShuffleArea");
            shuffleAreaObj.transform.parent = transform;
            shuffleAreaObj.transform.localPosition = new Vector3(gridSize * pieceSpacing + 3, 0, 0);
            shuffleArea = shuffleAreaObj.transform;
        }
    }
    
    /// <summary>
    /// 延迟生成拼图（等待JigsawGenerator完成）
    /// </summary>
    IEnumerator GeneratePuzzleWithDelay()
    {
        // 等待一帧，确保JigsawGenerator已经生成了拼图块
        yield return new WaitForEndOfFrame();
        
        // 查找所有生成的拼图块
        CollectPuzzlePieces();
        
        // 设置拼图块信息
        SetupPuzzlePieces();
        
        // 如果启用自动打乱，则打乱拼图块
        if (autoShuffle)
        {
            yield return new WaitForSeconds(0.5f);
            ShufflePieces();
        }
        
        isGameStarted = true;
    }
    
    /// <summary>
    /// 收集所有拼图块
    /// </summary>
    void CollectPuzzlePieces()
    {
        puzzlePieces.Clear();
        
        // 查找所有名称包含"Piece_"的游戏对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Piece_"))
            {
                // 添加PuzzlePiece组件（如果没有的话）
                PuzzlePiece piece = obj.GetComponent<PuzzlePiece>();
                if (piece == null)
                {
                    piece = obj.AddComponent<PuzzlePiece>();
                }
                
                puzzlePieces.Add(piece);
            }
        }
        
        Debug.Log($"找到 {puzzlePieces.Count} 个拼图块");
    }
    
    /// <summary>
    /// 初始化拼图（由游戏管理器调用）
    /// </summary>
    public void InitializePuzzle()
    {
        StartCoroutine(InitializePuzzleCoroutine());
    }
    
    /// <summary>
    /// 初始化拼图协程
    /// </summary>
    IEnumerator InitializePuzzleCoroutine()
    {
        // 等待一帧，确保JigsawGenerator已经生成了拼图块
        yield return new WaitForEndOfFrame();
        
        // 查找所有生成的拼图块
        CollectPuzzlePieces();
        
        // 设置拼图块信息
        SetupPuzzlePieces();
        
        // 如果启用自动打乱，则打乱拼图块
        if (autoShuffle)
        {
            yield return new WaitForSeconds(0.5f);
            ShufflePieces();
        }
        
        isGameStarted = true;
        Debug.Log("拼图初始化完成");
    }
    
    /// <summary>
    /// 设置拼图块信息
    /// </summary>
    void SetupPuzzlePieces()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            // 从名称中解析行列信息
            string[] nameParts = piece.name.Split('_');
            if (nameParts.Length >= 3)
            {
                int col = int.Parse(nameParts[1]);
                int row = int.Parse(nameParts[2]);
                
                // 计算正确位置
                Vector3 correctPos = CalculateCorrectPosition(row, col);
                
                // 设置拼图块信息
                piece.SetPieceInfo(row, col, correctPos);
                
                Debug.Log($"设置拼图块 {piece.name}: 行={row}, 列={col}, 正确位置={correctPos}");
            }
        }
    }
    
    /// <summary>
    /// 计算拼图块的正确位置
    /// </summary>
    Vector3 CalculateCorrectPosition(int row, int col)
    {
        float x = (col - (gridSize - 1) * 0.5f) * pieceSpacing;
        float y = ((gridSize - 1) * 0.5f - row) * pieceSpacing;
        
        return puzzleArea.position + new Vector3(x, y, 0);
    }
    
    /// <summary>
    /// 打乱拼图块
    /// </summary>
    public void ShufflePieces()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            // 生成随机位置
            Vector3 randomPos = GenerateRandomShufflePosition();
            piece.transform.position = randomPos;
            
            // 重置状态
            piece.isPlaced = false;
        }
        
        Debug.Log("拼图块已打乱");
    }
    
    /// <summary>
    /// 生成随机打乱位置
    /// </summary>
    Vector3 GenerateRandomShufflePosition()
    {
        float x = Random.Range(-shuffleAreaSize.x * 0.5f, shuffleAreaSize.x * 0.5f);
        float y = Random.Range(-shuffleAreaSize.y * 0.5f, shuffleAreaSize.y * 0.5f);
        
        return shuffleArea.position + new Vector3(x, y, 0);
    }
    
    /// <summary>
    /// 重置拼图
    /// </summary>
    public void ResetPuzzle()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            piece.ResetPosition();
        }
        
        if (autoShuffle)
        {
            StartCoroutine(ShuffleAfterDelay(0.5f));
        }
    }
    
    /// <summary>
    /// 延迟打乱
    /// </summary>
    IEnumerator ShuffleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShufflePieces();
    }
    
    /// <summary>
    /// 显示解决方案
    /// </summary>
    public void ShowSolution()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            piece.transform.position = piece.correctPosition;
            piece.isPlaced = true;
        }
        
        Debug.Log("显示解决方案");
    }
    
    /// <summary>
    /// 检查游戏是否完成
    /// </summary>
    public bool IsGameCompleted()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (!piece.isPlaced)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 获取游戏进度（0-1）
    /// </summary>
    public float GetGameProgress()
    {
        if (puzzlePieces.Count == 0) return 0f;
        
        int placedCount = 0;
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece.isPlaced)
            {
                placedCount++;
            }
        }
        
        return (float)placedCount / puzzlePieces.Count;
    }
    
    /// <summary>
    /// 设置拼图难度
    /// </summary>
    public void SetDifficulty(int newGridSize)
    {
        if (newGridSize < 2 || newGridSize > 10)
        {
            Debug.LogWarning("网格大小应该在2-10之间");
            return;
        }
        
        gridSize = newGridSize;
        
        // 清理现有拼图块
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece != null)
            {
                DestroyImmediate(piece.gameObject);
            }
        }
        puzzlePieces.Clear();
        
        // 重新生成拼图
        if (jigsawGenerator != null)
        {
            StartCoroutine(RegeneratePuzzle());
        }
    }
    
    /// <summary>
    /// 重新生成拼图
    /// </summary>
    IEnumerator RegeneratePuzzle()
    {
        // 调用JigsawGenerator重新生成
        jigsawGenerator.GeneratePuzzle();
        
        // 等待生成完成
        yield return new WaitForEndOfFrame();
        
        // 重新收集和设置拼图块
        CollectPuzzlePieces();
        SetupPuzzlePieces();
        
        if (autoShuffle)
        {
            yield return new WaitForSeconds(0.5f);
            ShufflePieces();
        }
    }
    
    // 可视化调试
    void OnDrawGizmosSelected()
    {
        // 绘制拼图区域
        if (puzzleArea != null)
        {
            Gizmos.color = Color.green;
            float areaSize = gridSize * pieceSpacing;
            Gizmos.DrawWireCube(puzzleArea.position, new Vector3(areaSize, areaSize, 0));
        }
        
        // 绘制打乱区域
        if (shuffleArea != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(shuffleArea.position, new Vector3(shuffleAreaSize.x, shuffleAreaSize.y, 0));
        }
    }
}