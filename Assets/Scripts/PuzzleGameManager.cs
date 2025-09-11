using UnityEngine;
using System.Collections;
using JigsawFun.Ads;

/// <summary>
/// 拼图游戏管理器
/// 负责协调游戏的整体流程和各组件之间的通信
/// </summary>
public class PuzzleGameManager : MonoBehaviour
{
    [Header("游戏组件")]
    public JigsawGenerator jigsawGenerator;
    public PuzzleBoard puzzleBoard;
    public PuzzleUI puzzleUI;
    
    [Header("游戏设置")]
    public string imagePath = "Images/puzzle_image";
    public int defaultGridSize = 4;
    public bool autoStartGame = true;
    
    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip snapSound;
    public AudioClip completeSound;
    public AudioClip errorSound;
    
    private bool gameInitialized = false;
    private bool gameInProgress = false;
    private int completedLevels = 0; // 已完成关卡数
    private PlayPrefsManager playPrefsManager;
    private bool hasSavedState = false; // 是否有保存的状态
    
    public static PuzzleGameManager Instance { get; private set; }
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        InitializeGame();
        
        // 加载已完成关卡数
        completedLevels = PlayerPrefs.GetInt("CompletedLevels", 0);
        
        if (autoStartGame)
        {
            StartCoroutine(StartGameAfterDelay(0.5f));
        }
    }
    
    /// <summary>
    /// 初始化游戏
    /// </summary>
    void InitializeGame()
    {
        // 查找组件
        FindGameComponents();
        
        // 设置组件引用
        SetupComponentReferences();
        
        // 初始化音效
        SetupAudio();
        
        // 初始化进度管理器
        playPrefsManager = PlayPrefsManager.Instance;
        // 加载已完成关卡数
        completedLevels = playPrefsManager.GetLevelProgress(0) ? 1 : 0;
        for (int i = 1; playPrefsManager.GetLevelProgress(i); i++)
        {
            completedLevels = i + 1;
        }

        // 检查是否有保存的拼图状态
        var savedState = playPrefsManager.LoadPuzzleState();
        hasSavedState = savedState != null;
        
        gameInitialized = true;
        Debug.Log("拼图游戏初始化完成");

        // 如果有保存的状态，恢复拼图
        if (hasSavedState)
        {
            RestorePuzzleState(savedState);
        }
    }
    
    /// <summary>
    /// 查找游戏组件
    /// </summary>
    void FindGameComponents()
    {
        if (jigsawGenerator == null)
        {
            jigsawGenerator = FindObjectOfType<JigsawGenerator>();
        }
        
        if (puzzleBoard == null)
        {
            puzzleBoard = FindObjectOfType<PuzzleBoard>();
        }
        
        if (puzzleUI == null)
        {
            puzzleUI = FindObjectOfType<PuzzleUI>();
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    /// <summary>
    /// 设置组件之间的引用关系
    /// </summary>
    void SetupComponentReferences()
    {
        if (puzzleUI != null && puzzleBoard != null)
        {
            puzzleUI.puzzleBoard = puzzleBoard;
        }
        
        if (puzzleBoard != null && jigsawGenerator != null)
        {
            puzzleBoard.jigsawGenerator = jigsawGenerator;
        }
    }
    
    /// <summary>
    /// 设置音效
    /// </summary>
    void SetupAudio()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
        }
    }
    
    /// <summary>
    /// 延迟启动游戏
    /// </summary>
    IEnumerator StartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNewGame(defaultGridSize);
    }
    
    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartNewGame(int gridSize = 4)
    {
        if (!gameInitialized)
        {
            Debug.LogWarning("游戏尚未初始化完成");
            return;
        }
        
        StartCoroutine(StartNewGameCoroutine(gridSize));
    }
    
    /// <summary>
    /// 开始新游戏协程
    /// </summary>
    IEnumerator StartNewGameCoroutine(int gridSize)
    {
        gameInProgress = false;
        
        // 禁用UI交互
        if (puzzleUI != null)
        {
            puzzleUI.SetButtonsInteractable(false);
        }
        
        // 清理现有拼图块
        ClearExistingPieces();
        
        yield return new WaitForSeconds(0.1f);
        
        // 生成新拼图
        if (jigsawGenerator != null)
        {
            jigsawGenerator.gridSize = gridSize;
            jigsawGenerator.GeneratePuzzle();
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // 初始化拼图盘
        if (puzzleBoard != null)
        {
            puzzleBoard.gridSize = gridSize;
            puzzleBoard.InitializePuzzle();
        }
        
        yield return new WaitForSeconds(0.2f);
        
        // 启用UI交互
        if (puzzleUI != null)
        {
            puzzleUI.SetButtonsInteractable(true);
        }
        
        gameInProgress = true;
        Debug.Log($"新游戏开始，难度: {gridSize}x{gridSize}");
    }
    
    /// <summary>
    /// 清理现有拼图块
    /// </summary>
    void ClearExistingPieces()
    {
        GameObject[] pieces = GameObject.FindGameObjectsWithTag("PuzzlePiece");
        foreach (GameObject piece in pieces)
        {
            DestroyImmediate(piece);
        }
        // 清除保存的拼图状态
        playPrefsManager.ClearPuzzleState();
        hasSavedState = false;
    }

    /// <summary>
    /// 保存当前拼图状态
    /// </summary>
    public void SavePuzzleState()
    {
        if (!gameInProgress) return;

        PuzzlePiece[] pieces = FindObjectsOfType<PuzzlePiece>();
        if (pieces != null && pieces.Length > 0)
        {
            playPrefsManager.SavePuzzleState(puzzleBoard.gridSize, pieces);
            Debug.Log("[PuzzleGameManager] 已保存拼图状态");
        }
    }

    /// <summary>
    /// 恢复拼图状态
    /// </summary>
    private void RestorePuzzleState(PlayPrefsManager.PuzzleStateData stateData)
    {
        if (stateData == null) return;

        // 设置拼图板网格大小
        puzzleBoard.gridSize = stateData.gridSize;

        // 等待一帧确保拼图块已生成
        StartCoroutine(RestorePuzzleStateCoroutine(stateData));
    }

    private IEnumerator RestorePuzzleStateCoroutine(PlayPrefsManager.PuzzleStateData stateData)
    {
        yield return new WaitForEndOfFrame();

        // 获取所有拼图块
        PuzzlePiece[] pieces = FindObjectsOfType<PuzzlePiece>();
        if (pieces.Length != stateData.pieces.Length)
        {
            Debug.LogError("[PuzzleGameManager] 拼图块数量不匹配，无法恢复状态");
            yield break;
        }

        // 恢复每个拼图块的状态
        for (int i = 0; i < pieces.Length; i++)
        {
            var pieceData = stateData.pieces[i];
            var piece = pieces[i];

            piece.row = pieceData.row;
            piece.col = pieceData.col;
            piece.correctPosition = pieceData.correctPosition;
            piece.isPlaced = pieceData.isPlaced;
            piece.transform.position = pieceData.currentPosition;
        }

        gameInProgress = true;
        Debug.Log("[PuzzleGameManager] 已恢复拼图状态");
    }

    /// <summary>
    /// 在应用退出时保存状态
    /// </summary>
    private void OnApplicationQuit()
    {
        SavePuzzleState();
    }

    /// <summary>
    /// 在应用暂停时保存状态
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SavePuzzleState();
        }
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// 播放吸附音效
    /// </summary>
    public void PlaySnapSound()
    {
        PlaySound(snapSound);
    }
    
    /// <summary>
    /// 播放完成音效
    /// </summary>
    public void PlayCompleteSound()
    {
        PlaySound(completeSound);
    }
    
    /// <summary>
    /// 播放错误音效
    /// </summary>
    public void PlayErrorSound()
    {
        PlaySound(errorSound);
    }
    
    /// <summary>
    /// 游戏暂停
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = 0f;
        gameInProgress = false;
    }
    
    /// <summary>
    /// 游戏恢复
    /// </summary>
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        gameInProgress = true;
    }
    
    /// <summary>
    /// 获取游戏状态
    /// </summary>
    public bool IsGameInProgress()
    {
        return gameInProgress && gameInitialized;
    }
    
    /// <summary>
    /// 获取当前难度
    /// </summary>
    public int GetCurrentDifficulty()
    {
        return puzzleBoard != null ? puzzleBoard.gridSize : defaultGridSize;
    }
    
    /// <summary>
    /// 设置图片路径
    /// </summary>
    public void SetImagePath(string newImagePath)
    {
        imagePath = newImagePath;
        if (jigsawGenerator != null)
        {
            // 这里可以添加设置图片路径的逻辑
            Debug.Log($"图片路径设置为: {imagePath}");
        }
    }
    
    /// <summary>
    /// 重新开始当前游戏
    /// </summary>
    public void RestartCurrentGame()
    {
        if (puzzleBoard != null)
        {
            StartNewGame(puzzleBoard.gridSize);
        }
        else
        {
            StartNewGame(defaultGridSize);
        }
    }
    
    /// <summary>
    /// 游戏完成处理
    /// </summary>
    public void OnGameCompleted()
    {
        if (!gameInProgress) return;
        
        gameInProgress = false;
        completedLevels++;
        
        Debug.Log($"[PuzzleGameManager] 关卡完成，已完成关卡数: {completedLevels}");
        
        // 播放完成音效
        PlayCompleteSound();
        
        // 触发插屏广告逻辑
        TriggerInterstitialAd();
        
        // 保存关卡进度
        playPrefsManager.SaveLevelProgress(completedLevels, true);
    }
    
    /// <summary>
    /// 触发插屏广告
    /// </summary>
    private void TriggerInterstitialAd()
    {
        // 确保AdManager已初始化
        if (AdManager.Instance == null)
        {
            Debug.LogWarning("[PuzzleGameManager] AdManager未初始化，无法显示插屏广告");
            return;
        }
        
        // 通过AdManager的插屏广告处理器记录关卡完成
        if (AdManager.Instance.InterstitialHandler != null)
        {
            AdManager.Instance.InterstitialHandler.OnLevelCompleted();
        }
        else
        {
            Debug.LogWarning("[PuzzleGameManager] InterstitialAdHandler未初始化");
        }
    }
    
    /// <summary>
    /// 获取已完成关卡数
    /// </summary>
    /// <returns>已完成关卡数</returns>
    public int GetCompletedLevels()
    {
        return completedLevels;
    }
    
    /// <summary>
    /// 重置关卡计数（用于测试或重置游戏进度）
    /// </summary>
    public void ResetLevelProgress()
    {
        completedLevels = 0;
        playPrefsManager.ClearAllProgress();
        Debug.Log("[PuzzleGameManager] 关卡进度已重置");
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}