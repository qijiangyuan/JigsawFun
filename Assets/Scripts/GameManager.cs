using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public bool debugMode = false;

    // 游戏状态
    public enum GameState
    {
        MainMenu,
        Gallery,
        DifficultySelection,
        Playing,
        Victory,
        Paused
    }

    [SerializeField] private GameState currentState = GameState.MainMenu;
    public GameState CurrentState => currentState;

    // 游戏数据
    [System.Serializable]
    public class GameData
    {
        public Sprite selectedImage;
        public int difficulty = 4;
        public bool showBackground = true;
        public float completionTime;
        public int starRating;
        public string imageName;
    }

    public GameData currentGameData = new GameData();

    // 事件系统
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<Sprite, int> OnGameStarted;
    public static event Action<float, int> OnGameCompleted;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    // 场景名称常量
    public const string MAIN_SCENE = "Main";
    public const string GAME_SCENE = "Game";

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGame()
    {
        // 设置目标帧率
        Application.targetFrameRate = 60;

        // 初始化游戏状态
        ChangeGameState(GameState.MainMenu);

        if (debugMode)
        {
            Debug.Log("[GameManager] Game initialized successfully");
        }
    }

    #region Game State Management

    public void ChangeGameState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;

        if (debugMode)
        {
            Debug.Log($"[GameManager] State changed from {previousState} to {newState}");
        }

        OnGameStateChanged?.Invoke(newState);
    }

    #endregion

    #region Scene Management

    public void LoadScene(string sceneName, Action onComplete = null)
    {
        StartCoroutine(LoadSceneAsync(sceneName, onComplete));
    }

    private IEnumerator LoadSceneAsync(string sceneName, Action onComplete = null)
    {
        if (debugMode)
        {
            Debug.Log($"[GameManager] Loading scene: {sceneName}");
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (debugMode)
        {
            Debug.Log($"[GameManager] Scene loaded: {sceneName}");
        }

        onComplete?.Invoke();
    }

    public void LoadMainScene()
    {
        LoadScene(MAIN_SCENE, () =>
        {
            ChangeGameState(GameState.Gallery);
        });
    }

    public void LoadGameScene()
    {
        // 使用UIManager显示加载页面
        //if (UIManager.Instance != null)
        //{
        //    UIManager.Instance.ShowLoadingPage(GAME_SCENE, () =>
        //    {
        //        ChangeGameState(GameState.Playing);
        //        OnGameStarted?.Invoke(currentGameData.selectedImage, currentGameData.difficulty);
        //    });
        //}
        //else
        //{
        //    // 如果UIManager不存在，回退到原来的方法
        //    LoadScene(GAME_SCENE, () =>
        //    {
        //        ChangeGameState(GameState.Playing);
        //        OnGameStarted?.Invoke(currentGameData.selectedImage, currentGameData.difficulty);
        //    });
        //}
        //使用UIManager通用方法
        UIManager.Instance.ShowPage<LoadingPage>(page =>
        {
            page.StartLoading(GAME_SCENE, () =>
            {
                Debug.LogError("GAME_SCENE load complete");
                ChangeGameState(GameState.Playing);
                OnGameStarted?.Invoke(currentGameData.selectedImage, currentGameData.difficulty);
            });
        });

        UIManager.Instance.HidePage<GalleryPage>();
        UIManager.Instance.HidePage<DifficultyPage>();
    }

    #endregion

    #region Game Flow Control

    public void StartNewGame(Sprite selectedImage, int difficulty, bool showBackground = true)
    {
        // 保存游戏数据
        currentGameData.selectedImage = selectedImage;
        currentGameData.difficulty = difficulty;
        currentGameData.showBackground = showBackground;
        currentGameData.imageName = selectedImage ? selectedImage.name : "Unknown";
        currentGameData.completionTime = 0f;
        currentGameData.starRating = 0;

        if (debugMode)
        {
            Debug.Log($"[GameManager] Starting new game - Image: {currentGameData.imageName}, Difficulty: {difficulty}");
        }

        // 跳转到游戏场景
        LoadGameScene();
    }

    public void CompleteGame(float completionTime)
    {
        currentGameData.completionTime = completionTime;
        currentGameData.starRating = CalculateStarRating(completionTime, currentGameData.difficulty);

        if (debugMode)
        {
            Debug.Log($"[GameManager] Game completed - Time: {completionTime:F2}s, Stars: {currentGameData.starRating}");
        }

        ChangeGameState(GameState.Victory);
        OnGameCompleted?.Invoke(completionTime, currentGameData.starRating);
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            ChangeGameState(GameState.Paused);
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();

            if (debugMode)
            {
                Debug.Log("[GameManager] Game paused");
            }
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ChangeGameState(GameState.Playing);
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();

            if (debugMode)
            {
                Debug.Log("[GameManager] Game resumed");
            }
        }
    }

    public void ReturnToGallery()
    {
        Time.timeScale = 1f; // 确保时间缩放恢复正常
        LoadMainScene();
    }

    public void QuitGame()
    {
        if (debugMode)
        {
            Debug.Log("[GameManager] Quitting game");
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    #endregion

    #region Utility Methods

    private int CalculateStarRating(float completionTime, int difficulty)
    {
        // 根据完成时间和难度计算星级
        float baseTime = difficulty * difficulty * 10f; // 基础时间

        if (completionTime <= baseTime * 0.5f)
            return 3; // 3星
        else if (completionTime <= baseTime * 0.8f)
            return 2; // 2星
        else
            return 1; // 1星
    }

    public void SetDontDestroyOnLoad(GameObject obj)
    {
        if (obj != null)
        {
            DontDestroyOnLoad(obj);

            if (debugMode)
            {
                Debug.Log($"[GameManager] Set DontDestroyOnLoad for: {obj.name}");
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && currentState == GameState.Playing)
        {
            PauseGame();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && currentState == GameState.Playing)
        {
            PauseGame();
        }
    }

    #endregion

    #region Debug Methods

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GameManager] {message}");
        }
    }

    #endregion
}