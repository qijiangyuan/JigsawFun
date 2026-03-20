using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    [Header("Game Settings")]
    public bool debugMode = false;

    public bool TileMovementEnabled { get; set; } = false;

    public int TotalTilesInCorrectPosition = 0;

    public int SecondsSinceStart = 0;

    public Camera MainCamera;

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

    private GameObject footerRoot;

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

    private bool autoPausedByFocus;

    // 场景名称常量
    public const string MAIN_SCENE = "Main";
    public const string GAME_SCENE = "Game";
    public const string JIGSAW_SCENE = "Scene_JigsawGame";

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(MainCamera);
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

        UpdateFooterVisibility(newState);
        OnGameStateChanged?.Invoke(newState);
    }

    private void UpdateFooterVisibility(GameState state)
    {
        if (footerRoot == null)
        {
            var go = GameObject.Find("Footer");
            if (go != null) footerRoot = go;
        }
        if (footerRoot == null) return;

        bool show = state == GameState.MainMenu || state == GameState.Gallery || state == GameState.DifficultySelection;
        if (footerRoot.activeSelf != show)
        {
            footerRoot.SetActive(show);
            if (debugMode)
            {
                Debug.Log($"[GameManager] Footer visibility: {show}");
            }
        }
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
            UIManager.Instance.HidePage<VictoryPage>();
            UIManager.Instance.HidePage<GameplayPage>();
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
            //page.StartLoading(GAME_SCENE, () =>
            //{
            //    ChangeGameState(GameState.Playing);
            //    OnGameStarted?.Invoke(currentGameData.selectedImage, currentGameData.difficulty);
            //});

            page.StartLoadScene(JIGSAW_SCENE, () =>
            {
                Debug.Log("JIGSAW_SCENE 加载成功");
                ChangeGameState(GameState.Playing);
                EventDispatcher.Dispatch(EventNames.PUZZLE_GENERATEION_START);
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

        HidePuzzleWorldForVictory();
        ChangeGameState(GameState.Victory);
        OnGameCompleted?.Invoke(completionTime, currentGameData.starRating);
    }

    private void HidePuzzleWorldForVictory()
    {
        var board = FindObjectOfType<BoardGen>(true);
        if (board != null)
        {
            board.SetPuzzleVisible(false);
            return;
        }

        var renderers = GameObject.FindObjectsOfType<SpriteRenderer>(true);
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            var sr = renderers[i];
            if (sr == null) continue;
            var go = sr.gameObject;
            if (go == null) continue;
            if (go.name.StartsWith("TileGameObe_") || go.name.StartsWith("Piece_"))
            {
                go.SetActive(false);
            }
        }
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
        string imageId = null;
        if (currentGameData != null)
        {
            imageId = currentGameData.selectedImage != null ? currentGameData.selectedImage.name : currentGameData.imageName;
        }
        if (!string.IsNullOrEmpty(imageId))
        {
            DebugLog($"Saving progress before returning to gallery: imageId={imageId}, grid={currentGameData.difficulty}");
            float elapsed = 0f;
            var gp = FindObjectOfType<GameplayPage>(true);
            if (gp != null) elapsed = Mathf.Max(0f, gp.GetCurrentGameTime());
            PlayPrefsManager.Instance.SaveCurrentSceneState(imageId, currentGameData.difficulty, elapsed);
        }
        // 恢复 UI 到相册页面
        if (UIManager.Instance != null)
        {
            CleanUpGameplayUI();
            UIManager.Instance.HidePage<VictoryPage>();
            UIManager.Instance.HidePage<GameplayPage>();
            UIManager.Instance.ShowPage<GalleryPage>();
        }
        ChangeGameState(GameState.Gallery);

        // 尝试卸载可能加载的游戏相关场景（兼容不同名称）
        TryUnloadSceneSafely(JIGSAW_SCENE);
        TryUnloadSceneSafely(GAME_SCENE);
    }

    /// <summary>
    /// 清理游戏过程中动态创建并挂在主 Canvas 下的 UI（例如 PuzzleScrollTray）
    /// 避免返回主菜单后仍然遮挡按钮
    /// </summary>
    private void CleanUpGameplayUI()
    {
        // 销毁所有拼图托盘
        var trays = GameObject.FindObjectsOfType<PuzzleScrollTray>(true);
        for (int i = 0; i < trays.Length; i++)
        {
            if (trays[i] != null)
            {
                UnityEngine.Object.Destroy(trays[i].gameObject);
            }
        }
    }

    private void TryUnloadSceneSafely(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        var sc = SceneManager.GetSceneByName(sceneName);
        if (sc.IsValid() && sc.isLoaded)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }
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
            if (currentGameData != null && currentGameData.selectedImage != null)
            {
                var id = currentGameData.selectedImage.name;
                float elapsed = 0f;
                var gp = FindObjectOfType<GameplayPage>(true);
                if (gp != null) elapsed = Mathf.Max(0f, gp.GetCurrentGameTime());
                PlayPrefsManager.Instance.SaveCurrentSceneState(id, currentGameData.difficulty, elapsed);
            }
            autoPausedByFocus = true;
            PauseGame();
        }
        else if (!pauseStatus && autoPausedByFocus && currentState == GameState.Paused)
        {
            autoPausedByFocus = false;
            ResumeGame();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && currentState == GameState.Playing)
        {
            autoPausedByFocus = true;
            PauseGame();
        }
        else if (hasFocus && autoPausedByFocus && currentState == GameState.Paused)
        {
            autoPausedByFocus = false;
            ResumeGame();
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
