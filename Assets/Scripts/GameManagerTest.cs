using UnityEngine;

/// <summary>
/// GameManager测试脚本
/// 用于测试GameManager的各种功能
/// </summary>
public class GameManagerTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool enableDebugKeys = true;
    public Sprite testImage;
    
    private void Start()
    {
        // 订阅GameManager事件进行测试
        if (GameManager.Instance != null)
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
            GameManager.OnGameStarted += OnGameStarted;
            GameManager.OnGameCompleted += OnGameCompleted;
            GameManager.OnGamePaused += OnGamePaused;
            GameManager.OnGameResumed += OnGameResumed;
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (GameManager.Instance != null)
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            GameManager.OnGameStarted -= OnGameStarted;
            GameManager.OnGameCompleted -= OnGameCompleted;
            GameManager.OnGamePaused -= OnGamePaused;
            GameManager.OnGameResumed -= OnGameResumed;
        }
    }
    
    private void Update()
    {
        if (!enableDebugKeys || GameManager.Instance == null) return;
        
        // 测试按键
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 测试开始游戏
            if (testImage != null)
            {
                Debug.Log("[GameManagerTest] 测试开始游戏");
                GameManager.Instance.StartNewGame(testImage, 4, true);
            }
            else
            {
                Debug.LogWarning("[GameManagerTest] 测试图片未设置！");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // 测试完成游戏
            Debug.Log("[GameManagerTest] 测试完成游戏");
            GameManager.Instance.CompleteGame(120.5f);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // 测试暂停游戏
            Debug.Log("[GameManagerTest] 测试暂停游戏");
            GameManager.Instance.PauseGame();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // 测试恢复游戏
            Debug.Log("[GameManagerTest] 测试恢复游戏");
            GameManager.Instance.ResumeGame();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // 测试返回Gallery
            Debug.Log("[GameManagerTest] 测试返回Gallery");
            GameManager.Instance.ReturnToGallery();
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            // 测试状态切换到Gallery
            Debug.Log("[GameManagerTest] 切换到Gallery状态");
            GameManager.Instance.ChangeGameState(GameManager.GameState.Gallery);
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            // 测试状态切换到DifficultySelection
            Debug.Log("[GameManagerTest] 切换到DifficultySelection状态");
            GameManager.Instance.ChangeGameState(GameManager.GameState.DifficultySelection);
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            // 测试状态切换到Playing
            Debug.Log("[GameManagerTest] 切换到Playing状态");
            GameManager.Instance.ChangeGameState(GameManager.GameState.Playing);
        }
        
        if (Input.GetKeyDown(KeyCode.V))
        {
            // 测试状态切换到Victory
            Debug.Log("[GameManagerTest] 切换到Victory状态");
            GameManager.Instance.ChangeGameState(GameManager.GameState.Victory);
        }
    }
    
    #region Event Handlers
    
    private void OnGameStateChanged(GameManager.GameState newState)
    {
        Debug.Log($"[GameManagerTest] 游戏状态改变: {newState}");
    }
    
    private void OnGameStarted(Sprite selectedImage, int difficulty)
    {
        Debug.Log($"[GameManagerTest] 游戏开始 - 图片: {(selectedImage ? selectedImage.name : "null")}, 难度: {difficulty}");
    }
    
    private void OnGameCompleted(float completionTime, int starRating)
    {
        Debug.Log($"[GameManagerTest] 游戏完成 - 时间: {completionTime:F2}秒, 星级: {starRating}");
    }
    
    private void OnGamePaused()
    {
        Debug.Log("[GameManagerTest] 游戏暂停");
    }
    
    private void OnGameResumed()
    {
        Debug.Log("[GameManagerTest] 游戏恢复");
    }
    
    #endregion
    
    #region GUI Debug Info
    
    private void OnGUI()
    {
        if (!enableDebugKeys || GameManager.Instance == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("GameManager 测试控制台", GUI.skin.box);
        GUILayout.Space(10);
        
        GUILayout.Label($"当前状态: {GameManager.Instance.CurrentState}");
        GUILayout.Space(5);
        
        if (GameManager.Instance.currentGameData != null)
        {
            var data = GameManager.Instance.currentGameData;
            GUILayout.Label($"选中图片: {(data.selectedImage ? data.selectedImage.name : "无")}");
            GUILayout.Label($"难度: {data.difficulty}");
            GUILayout.Label($"显示背景: {data.showBackground}");
            GUILayout.Label($"完成时间: {data.completionTime:F2}秒");
            GUILayout.Label($"星级: {data.starRating}");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("测试按键:");
        GUILayout.Label("1 - 开始游戏");
        GUILayout.Label("2 - 完成游戏");
        GUILayout.Label("3 - 暂停游戏");
        GUILayout.Label("4 - 恢复游戏");
        GUILayout.Label("5 - 返回Gallery");
        GUILayout.Label("G - Gallery状态");
        GUILayout.Label("D - 难度选择状态");
        GUILayout.Label("P - 游戏中状态");
        GUILayout.Label("V - 胜利状态");
        
        GUILayout.EndArea();
    }
    
    #endregion
}