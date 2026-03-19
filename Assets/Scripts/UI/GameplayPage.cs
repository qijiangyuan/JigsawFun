using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;
using JigsawFun.Ads;

public class GameplayPage : BasePage
{
    [Header("拼图区域")]
    public Transform puzzleContainer;       // 拼图容器
    public Image backgroundImage;           // 背景图片
    public RectTransform puzzleArea;        // 拼图区域

    [Header("操作按钮")]
    public Button pauseButton;              // 重置拼图按钮
    public Button hintButton;               // 提示原图按钮
    //public Button timerToggleButton;        // 计时器开关按钮
    public Button backButton;               // 返回按钮
    public Button startButton;              // 开始按钮
    public Button clearToTrayButton;

    [Header("UI显示")]
    public TextMeshProUGUI timerText;                  // 计时器文本
    //public Image hintOverlay;               // 提示覆盖层
    public TextMeshProUGUI difficultyText;             // 难度显示
    public TextMeshProUGUI progressText;               // 进度显示
    public TextMeshProUGUI hintRemainText;  // 提示次数显示

    [Header("音效")]
    public AudioSource audioSource;        // 音效播放器
    public AudioClip snapSound;             // 拼对音效
    public AudioClip completeSound;         // 完成音效

    [Header("设置")]
    //public float hintDisplayTime = 3f;      // 提示显示时间
    public Sprite woodTexture;              // 木质背景纹理
    public Sprite fabricTexture;            // 布面背景纹理

    [Header("广告区域")]
    public RectTransform bannerAdArea;      // Banner广告区域
    public Button watchAdButton;            // 观看广告获得提示按钮

    private GameData currentGameData;

    private float gameStartTime;
    private float currentGameTime;
    private bool isTimerEnabled = true;
    private bool isGameActive = false;
    private int completedPieces = 0;
    private int totalPieces = 0;

    private const int MaxHintsPerGame = 5;
    private int hintRemaining = MaxHintsPerGame;

    private bool timerStarted;

    //免费提示次数 
    private int freeHintCount = 3; // 初始免费提示次数
    private HintManager hintManager; // 提示管理器
    
    [Header("布局")]
    public float boardMargin = 1.0f; // 棋盘白边（像素）更窄

    private PuzzlePiece currentPiece; // 当前选中的拼图块

    protected override void Awake()
    {
        base.Awake();
        InitializeComponents();
        InitializeAdSystem();
    }

    private void OnEnable()
    {
        SubscribeToPuzzleEvents();

    }

    private void OnDisable()
    {
        UnsubscribeFromPuzzleEvents();

        // 取消订阅提示管理器事件
        if (hintManager != null)
        {
            //hintManager.OnHintCountChanged -= UpdateHintUI;
            //hintManager.OnHintRewardReceived -= OnHintRewardReceived;
        }
    }

    /// <summary>
    /// 订阅拼图事件
    /// </summary>
    private void SubscribeToPuzzleEvents()
    {
        // 这里需要与PuzzlePiece脚本配合，当拼图块正确放置时调用OnPieceCompleted
        // 当所有拼图块完成时调用OnPuzzleCompleted
        EventDispatcher.AddListener<PuzzlePiece>(EventNames.SELECT_PIECE, OnSelectPiece);
        EventDispatcher.AddListener<PuzzlePiece>(EventNames.DESELECT_PIECE, OnDeselectPiece);
        EventDispatcher.AddListener(EventNames.PUZZLE_COMPLETED, OnPuzzleCompleted);
        EventDispatcher.AddListener(EventNames.PUZZLE_GENERATEION_DONE, OnPuzzleGenerationDone);
    }

    //取消订阅拼图事件
    private void UnsubscribeFromPuzzleEvents()
    {
        EventDispatcher.RemoveListener<PuzzlePiece>(EventNames.SELECT_PIECE, OnSelectPiece);
        EventDispatcher.RemoveListener<PuzzlePiece>(EventNames.DESELECT_PIECE, OnDeselectPiece);
        EventDispatcher.RemoveListener(EventNames.PUZZLE_COMPLETED, OnPuzzleCompleted);
        EventDispatcher.RemoveListener(EventNames.PUZZLE_GENERATEION_DONE, OnPuzzleGenerationDone);
    }

    private void OnPuzzleGenerationDone()
    {
        startButton?.gameObject.SetActive(true);
        SetTopControlsVisible(true);
        UpdateUI();
        UpdateHintUI();
    }

    private void SetTopControlsVisible(bool visible)
    {
        if (pauseButton != null) pauseButton.gameObject.SetActive(visible);
        if (hintButton != null) hintButton.gameObject.SetActive(visible);
        if (backButton != null) backButton.gameObject.SetActive(visible);
        if (clearToTrayButton != null) clearToTrayButton.gameObject.SetActive(visible);
        if (timerText != null) timerText.gameObject.SetActive(visible);
        if (difficultyText != null) difficultyText.gameObject.SetActive(visible);
        if (progressText != null) progressText.gameObject.SetActive(visible);
        if (hintRemainText != null) hintRemainText.gameObject.SetActive(visible);
    }

    private void OnSelectPiece(PuzzlePiece puzzlePiece)
    {
        currentPiece = puzzlePiece;
    }

    private void OnDeselectPiece(PuzzlePiece puzzlePiece)
    {
        if (currentPiece != null && currentPiece == puzzlePiece)
        {
            currentPiece = null;
        }
    }


    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        // 设置按钮事件
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseButtonClicked);

        if (hintButton != null)
        {
            hintButton.onClick.AddListener(OnHintButtonClicked);
            hintButton.gameObject.SetActive(true);
        }

        if (watchAdButton != null)
        {
            watchAdButton.onClick.AddListener(OnWatchAdButtonClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            startButton.gameObject.SetActive(false); // 初始隐藏提示按钮
        }

        if (clearToTrayButton != null)
        {
            clearToTrayButton.onClick.AddListener(OnClearToTrayClicked);
        }


        //if (timerToggleButton != null)
        //    timerToggleButton.onClick.AddListener(OnTimerToggleClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
        else
        {
            // 动态创建“Back to Main Menu”按钮（若未在Prefab中指定）
            GameObject btnObj = new GameObject("BackToMainMenuButton");
            btnObj.transform.SetParent(transform, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -20f);
            rt.sizeDelta = new Vector2(180f, 60f);
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.95f, 0.95f, 0.98f, 0.85f);
            var btn = btnObj.AddComponent<Button>();
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            txt.alignment = TextAlignmentOptions.Center;
            txt.text = "Main Menu";
            txt.fontSize = 28;
            backButton = btn;
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // 初始化提示管理器
        if (hintManager == null)
        {
            hintManager = HintManager.Instance;
        }

        // 获取拼图生成器
        //jigsawGenerator = FindObjectOfType<JigsawGenerator>();

        // 初始化提示覆盖层
        //if (hintOverlay != null)
        //{
        //    hintOverlay.gameObject.SetActive(false);
        //}
    }

    private void OnStartButtonClicked()
    {
        BoardGen boardGen = FindObjectOfType<BoardGen>();

        if (boardGen != null)
        {
            boardGen.ShuffleTiles();
        }

        isTimerEnabled = true;
        timerStarted = true;
        startButton?.gameObject.SetActive(false);
    }

    private void OnClearToTrayClicked()
    {
        BoardGen boardGen = FindObjectOfType<BoardGen>();
        if (boardGen != null)
        {
            boardGen.MoveUnplacedTilesToTray();
        }
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    /// <param name="gameData">游戏数据</param>
    public void StartGame(GameData gameData)
    {
        currentGameData = gameData;

        SetTopControlsVisible(false);
        startButton?.gameObject.SetActive(false);

        // 设置背景
        SetupBackground();
        FitPuzzleFrameMargin();

        // 生成拼图
        GeneratePuzzle();

        // 初始化游戏状态
        gameStartTime = Time.time;
        float saved = 0f;
        if (GameManager.Instance != null && GameManager.Instance.currentGameData != null && GameManager.Instance.currentGameData.selectedImage != null)
        {
            var state = PlayPrefsManager.Instance.LoadPuzzleStateForImage(GameManager.Instance.currentGameData.selectedImage.name);
            if (state != null) saved = Mathf.Max(0f, state.elapsedSeconds);
        }
        currentGameTime = saved;
        isGameActive = true;
        isTimerEnabled = true;
        completedPieces = 0;
        totalPieces = gameData.difficulty * gameData.difficulty;
        hintRemaining = MaxHintsPerGame;
        timerStarted = false;

        // 更新UI
        UpdateUI();
        UpdateTimerText();

        // 初始化提示UI
        UpdateHintUI();
    }

    private void FitPuzzleFrameMargin()
    {
        if (backgroundImage != null)
        {
            var rt = backgroundImage.rectTransform;
            // 让背景图充满拼图区域但留出窄边距
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(boardMargin, boardMargin);
            rt.offsetMax = new Vector2(-boardMargin, -boardMargin);
            backgroundImage.preserveAspect = true;
        }
    }

    public void ResetGame()
    {
        // 初始化游戏状态
        gameStartTime = Time.time;
        currentGameTime = 0f;
        isGameActive = true;
        isTimerEnabled = true;
        completedPieces = 0;
        hintRemaining = MaxHintsPerGame;
        //totalPieces = currentGameData.difficulty * currentGameData.difficulty;
        timerStarted = false;
        UpdateTimerText();
        SetTopControlsVisible(false);
        startButton?.gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置背景
    /// </summary>
    private void SetupBackground()
    {
        if (backgroundImage == null) return;

        if (currentGameData.showBackground)
        {
            // 显示选中的图片作为背景
            backgroundImage.sprite = currentGameData.selectedImage;
            backgroundImage.color = new Color(1f, 1f, 1f, 0.3f); // 半透明
        }
        else
        {
            // 显示木质或布面纹理
            Sprite textureSprite = Random.value > 0.5f ? woodTexture : fabricTexture;
            backgroundImage.sprite = textureSprite;
            backgroundImage.color = Color.white;
        }
    }

    /// <summary>
    /// 生成拼图
    /// </summary>
    private void GeneratePuzzle()
    {
        if (currentGameData.selectedImage == null)
        {
            Debug.LogError("拼图生成器或选中图片为空！");
            return;
        }

        //// 设置拼图参数
        //jigsawGenerator.puzzleImage = currentGameSettings.selectedImage;
        //jigsawGenerator.gridSize = currentGameSettings.difficulty;

        //// 生成拼图
        //jigsawGenerator.GenerateJigsaw();

        // 订阅拼图完成事件
        SubscribeToPuzzleEvents();
    }



    /// <summary>
    /// 拼图块完成事件
    /// </summary>
    public void OnPieceCompleted()
    {
        completedPieces++;

        // 播放拼对音效
        PlaySnapSound();

        // 更新进度
        UpdateProgressText();

        // 检查是否完成
        if (completedPieces >= totalPieces)
        {
            OnPuzzleCompleted();
        }
    }

    /// <summary>
    /// 拼图完成事件
    /// </summary>
    private void OnPuzzleCompleted()
    {
        isGameActive = false;

        // 播放完成音效
        PlayCompleteSound();

        // 计算游戏时间
        float completionTime = currentGameTime;

        //停止计时器
        isTimerEnabled = false;
        timerStarted = false;

        // 使用GameManager处理游戏完成
        if (GameManager.Instance != null)
        {
            string imageId = GameManager.Instance.currentGameData != null && GameManager.Instance.currentGameData.selectedImage != null
                ? GameManager.Instance.currentGameData.selectedImage.name
                : GameManager.Instance.currentGameData != null ? GameManager.Instance.currentGameData.imageName : null;
            if (!string.IsNullOrEmpty(imageId))
            {
                PlayPrefsManager.Instance.AddCompletedPuzzle(imageId, GameManager.Instance.currentGameData.difficulty, completionTime);
                PlayPrefsManager.Instance.ClearPuzzleStateForImage(imageId);
                PlayPrefsManager.Instance.SaveCompletedPreview(imageId, SpriteToPreviewTexture(GameManager.Instance.currentGameData.selectedImage));
            }
            GameManager.Instance.CompleteGame(completionTime);
        }
        else
        {
            Debug.LogError("GameManager实例不存在！");
        }

        // 通知PuzzleGameManager游戏完成，触发插屏广告逻辑
        if (PuzzleGameManager.Instance != null)
        {
            PuzzleGameManager.Instance.OnGameCompleted();
        }
        else
        {
            Debug.LogWarning("PuzzleGameManager实例不存在，无法触发插屏广告逻辑！");
        }
    }

    private Texture2D SpriteToPreviewTexture(Sprite sprite)
    {
        if (sprite == null) return null;
        var srcTex = sprite.texture;
        if (srcTex == null) return null;

        Rect tr = sprite.textureRect;
        int w = Mathf.Max(1, Mathf.RoundToInt(tr.width));
        int h = Mathf.Max(1, Mathf.RoundToInt(tr.height));
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        Rect uv = new Rect(tr.x / srcTex.width, tr.y / srcTex.height, tr.width / srcTex.width, tr.height / srcTex.height);
        Graphics.DrawTexture(new Rect(0, 0, w, h), srcTex, uv, 0, 0, 0, 0);
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    /// <summary>
    /// 重置按钮点击事件
    /// </summary>
    private void OnPauseButtonClicked()
    {
        //暂停
        // 先使用GameManager返回到Gallery，后面会加一个暂停弹窗
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.currentGameData != null && GameManager.Instance.currentGameData.selectedImage != null)
            {
                PlayPrefsManager.Instance.SaveCurrentSceneState(
                    GameManager.Instance.currentGameData.selectedImage.name,
                    GameManager.Instance.currentGameData.difficulty,
                    currentGameTime
                );
            }
            GameManager.Instance.ReturnToGallery();
        }
        else
        {
            Debug.LogError("GameManager实例不存在！");
        }
    }

    /// <summary>
    /// 返回主菜单按钮点击
    /// </summary>
    private void OnBackButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.currentGameData != null && GameManager.Instance.currentGameData.selectedImage != null)
            {
                PlayPrefsManager.Instance.SaveCurrentSceneState(
                    GameManager.Instance.currentGameData.selectedImage.name,
                    GameManager.Instance.currentGameData.difficulty,
                    currentGameTime
                );
            }
            GameManager.Instance.ReturnToGallery();
        }
    }

    private void Reset()
    {
        // 重新生成拼图
        GeneratePuzzle();

        // 重置游戏状态
        gameStartTime = Time.time;
        currentGameTime = 0f;
        completedPieces = 0;
        timerStarted = false;

        UpdateUI();
        UpdateTimerText();
    }

    /// <summary>
    /// 提示按钮点击事件
    /// </summary>
    private void OnHintButtonClicked()
    {
        if (hintRemaining <= 0)
        {
            // 当提示数量为0时，触发 Startup placement 的激励视频广告
            if (AdManager.Instance != null && AdManager.Instance.CanShowRewardedVideo())
            {
                AdManager.Instance.ShowRewardedVideo("Startup", () => 
                {
                    // 广告观看完成后的回调，增加提示次数
                    hintRemaining += 3; // 假设每次观看广告获得3个提示，您可以根据需要调整
                    UpdateHintUI();
                    Debug.Log("观看了激励视频，获得了提示奖励");
                });
            }
            else
            {
                Debug.LogWarning("提示次数不足，且激励视频广告未准备好。尝试重新加载...");
                if (AdManager.Instance != null && AdManager.Instance.RewardedHandler != null)
                {
                    AdManager.Instance.RewardedHandler.LoadAd();
                }
            }
            return;
        }

        var tm = PickHintTargetTileMovement();
        if (tm == null) return;

        hintRemaining = Mathf.Max(0, hintRemaining - 1);
        UpdateHintUI();

        tm.enabled = true;
        tm.gameObject.SetActive(true);
        tm.SnapToCorretPosition();
    }

    /// <summary>
    /// 观看广告按钮点击事件
    /// </summary>
    private void OnWatchAdButtonClicked()
    {
        //if (AdManager.Instance != null && AdManager.Instance.IsRewardedAdReady())
        //{
        //    AdManager.Instance.ShowRewardedAd();
        //}
        //else
        {
            Debug.LogWarning("激励视频广告未准备好");
        }
    }

    /// <summary>
    /// 显示观看广告选项
    /// </summary>
    private void ShowWatchAdOption()
    {
        if (watchAdButton != null)
        {
            watchAdButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏观看广告选项
    /// </summary>
    private void HideWatchAdOption()
    {
        if (watchAdButton != null)
        {
            watchAdButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 初始化广告系统
    /// </summary>
    private void InitializeAdSystem()
    {
        // 确保AdManager已初始化
        if (AdManager.Instance != null)
        {
            // 显示Banner广告
            //AdManager.Instance.BannerHandler.ShowBanner();

            // 订阅提示奖励事件
            if (hintManager != null)
            {
                //hintManager.OnHintCountChanged += UpdateHintUI;
                // hintManager.OnHintRewardReceived += OnHintRewardReceived;
            }
        }
        else
        {
            Debug.LogWarning("AdManager未初始化，无法显示Banner广告");
        }
    }

    /// <summary>
    /// 提示奖励接收事件
    /// </summary>
    private void OnHintRewardReceived(int rewardCount)
    {
        Debug.Log($"获得 {rewardCount} 个提示奖励！");
        HideWatchAdOption();
        UpdateHintUI();
    }

    /// <summary>
    /// 更新提示UI显示
    /// </summary>
    private void UpdateHintUI()
    {
        if (hintRemainText != null)
        {
            hintRemainText.text = $"x{hintRemaining}";
            hintRemainText.gameObject.SetActive(true);
        }
        if (hintButton != null)
        {
            // 当提示次数为0时，如果广告准备好了，也可以点击（触发广告）
            bool canUseHintOrWatchAd = (hintRemaining > 0 || (AdManager.Instance != null && AdManager.Instance.CanShowRewardedVideo()));
            //hintButton.interactable = canUseHintOrWatchAd && HasAnyUnplacedTiles();
        }
    }

    private bool HasAnyUnplacedTiles()
    {
        var tray = FindObjectOfType<PuzzleScrollTray>(true);
        if (tray != null && tray.contentContainer != null && tray.contentContainer.childCount > 0) return true;
        var tiles = FindObjectsOfType<TileMovement>(true);
        return tiles != null && tiles.Length > 0;
    }

    private TileMovement PickHintTargetTileMovement()
    {
        var tray = FindObjectOfType<PuzzleScrollTray>(true);
        if (tray != null && tray.contentContainer != null)
        {
            for (int i = 0; i < tray.contentContainer.childCount; i++)
            {
                var child = tray.contentContainer.GetChild(i);
                if (child == null) continue;
                var item = child.GetComponent<PuzzleTrayItem>();
                if (item == null || item.worldPiece == null) continue;
                var tm = item.worldPiece.GetComponent<TileMovement>();
                if (tm == null) continue;
                Destroy(child.gameObject);
                return tm;
            }
        }

        var tiles = FindObjectsOfType<TileMovement>(true);
        if (tiles == null || tiles.Length == 0) return null;
        TileMovement best = null;
        float bestDist = float.PositiveInfinity;
        for (int i = 0; i < tiles.Length; i++)
        {
            var tm = tiles[i];
            if (tm == null || tm.tile == null) continue;
            Vector3 correct = new Vector3(tm.tile.xIndex * Tile.tileSize, tm.tile.yIndex * Tile.tileSize, 0f);
            float d = (tm.transform.position - correct).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = tm;
            }
        }
        return best;
    }


    /// <summary>
    /// 计时器开关点击事件
    /// </summary>
    private void OnTimerToggleClicked()
    {
        isTimerEnabled = !isTimerEnabled;
        UpdateTimerButton();
    }

    /// <summary>
    /// 更新计时器按钮显示
    /// </summary>
    private void UpdateTimerButton()
    {
        //if (timerToggleButton == null) return;

        // 可以通过改变按钮颜色或文本来表示开关状态
        //ColorBlock colors = timerToggleButton.colors;
        //colors.normalColor = isTimerEnabled ? Color.green : Color.gray;
        //timerToggleButton.colors = colors;

        // 更新计时器文本显示
        if (timerText != null)
        {
            //timerText.gameObject.SetActive(isTimerEnabled);
        }
    }

    /// <summary>
    /// 播放拼对音效
    /// </summary>
    private void PlaySnapSound()
    {
        if (audioSource != null && snapSound != null)
        {
            audioSource.PlayOneShot(snapSound);
        }
    }

    /// <summary>
    /// 播放完成音效
    /// </summary>
    private void PlayCompleteSound()
    {
        if (audioSource != null && completeSound != null)
        {
            audioSource.PlayOneShot(completeSound);
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        UpdateDifficultyText();
        UpdateProgressText();
        UpdateTimerButton();
    }

    /// <summary>
    /// 更新难度显示
    /// </summary>
    private void UpdateDifficultyText()
    {
        if (difficultyText != null && currentGameData != null)
        {
            difficultyText.text = $"难度: {currentGameData.difficulty}";
        }
    }

    /// <summary>
    /// 更新进度显示
    /// </summary>
    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            progressText.text = $"进度: {completedPieces}/{totalPieces}";
        }
    }

    /// <summary>
    /// 更新计时器显示
    /// </summary>
    private void UpdateTimerText()
    {
        if (timerText != null && isTimerEnabled)
        {
            int minutes = Mathf.FloorToInt(currentGameTime / 60f);
            int seconds = Mathf.FloorToInt(currentGameTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void Update()
    {
        if (isGameActive && isTimerEnabled && timerStarted && (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing))
        {
            currentGameTime += Time.unscaledDeltaTime;
            UpdateTimerText();
        }
    }

    public bool IsTimerStarted()
    {
        return timerStarted;
    }

    protected override void OnPageShow()
    {
        base.OnPageShow();
        UpdateUI();
        UpdateHintUI();

        // 显示Banner广告
        if (AdManager.Instance != null)
        {
            //AdManager.Instance.BannerHandler.ShowBanner();
        }
    }

    protected override void OnPageHide()
    {
        base.OnPageHide();

        // 停止游戏
        isGameActive = false;

        // 隐藏Banner广告
        if (AdManager.Instance != null)
        {
            AdManager.Instance.BannerHandler.HideBanner();
        }
    }

    /// <summary>
    /// 获取当前游戏时间
    /// </summary>
    public float GetCurrentGameTime()
    {
        return currentGameTime;
    }

    /// <summary>
    /// 获取完成进度
    /// </summary>
    public float GetCompletionProgress()
    {
        return totalPieces > 0 ? (float)completedPieces / totalPieces : 0f;
    }
}
