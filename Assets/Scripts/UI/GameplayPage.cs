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

    //免费提示次数 
    private int freeHintCount = 3; // 初始免费提示次数
    private HintManager hintManager; // 提示管理器

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
    }

    //取消订阅拼图事件
    private void UnsubscribeFromPuzzleEvents()
    {
        EventDispatcher.RemoveListener<PuzzlePiece>(EventNames.SELECT_PIECE, OnSelectPiece);
        EventDispatcher.RemoveListener<PuzzlePiece>(EventNames.DESELECT_PIECE, OnDeselectPiece);
        EventDispatcher.RemoveListener(EventNames.PUZZLE_COMPLETED, OnPuzzleCompleted);
    }

    private void OnSelectPiece(PuzzlePiece puzzlePiece)
    {
        hintButton.gameObject.SetActive(true);
        currentPiece = puzzlePiece;
    }

    private void OnDeselectPiece(PuzzlePiece puzzlePiece)
    {
        if (currentPiece != null && currentPiece == puzzlePiece)
        {
            currentPiece = null;
            hintButton.gameObject.SetActive(false);
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
            hintButton.gameObject.SetActive(false); // 初始隐藏提示按钮
        }

        if (watchAdButton != null)
        {
            watchAdButton.onClick.AddListener(OnWatchAdButtonClicked);
        }



        //if (timerToggleButton != null)
        //    timerToggleButton.onClick.AddListener(OnTimerToggleClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

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

    /// <summary>
    /// 开始游戏
    /// </summary>
    /// <param name="gameData">游戏数据</param>
    public void StartGame(GameData gameData)
    {
        currentGameData = gameData;

        // 设置背景
        SetupBackground();

        // 生成拼图
        GeneratePuzzle();

        // 初始化游戏状态
        gameStartTime = Time.time;
        currentGameTime = 0f;
        isGameActive = true;
        completedPieces = 0;
        totalPieces = gameData.difficulty * gameData.difficulty;

        // 更新UI
        UpdateUI();

        // 初始化提示UI
        UpdateHintUI();
    }

    public void ResetGame()
    {
        // 初始化游戏状态
        gameStartTime = Time.time;
        currentGameTime = 0f;
        isGameActive = true;
        isTimerEnabled = true;
        completedPieces = 0;
        //totalPieces = currentGameData.difficulty * currentGameData.difficulty;
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

        // 使用GameManager处理游戏完成
        if (GameManager.Instance != null)
        {
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

    /// <summary>
    /// 重置按钮点击事件
    /// </summary>
    private void OnPauseButtonClicked()
    {
        //暂停
        // 先使用GameManager返回到Gallery，后面会加一个暂停弹窗
        if (GameManager.Instance != null)
        {
            JigsawGenerator.Instance.ClearPuzzles();
            GameManager.Instance.ReturnToGallery();
        }
        else
        {
            Debug.LogError("GameManager实例不存在！");
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

        UpdateUI();
    }

    /// <summary>
    /// 提示按钮点击事件
    /// </summary>
    private void OnHintButtonClicked()
    {
        //if (hintManager != null && hintManager.CanUseHint())
        //{
        //    currentPiece?.SnapToCorrectPosition();
        //    hintManager.UseHint();
        //    UpdateHintUI();
        //}
        //else
        {
            // 如果没有免费提示次数，显示观看广告按钮
            Debug.Log("没有免费提示次数了，观看广告获得更多提示！");
            ShowWatchAdOption();
        }
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
            AdManager.Instance.BannerHandler.ShowBanner();

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
        if (hintRemainText != null && hintManager != null)
        {
            //hintRemainText.text = $"x{hintManager.CurrentHintCount}";
        }

        // 根据提示次数显示/隐藏观看广告按钮
        if (hintManager != null && !hintManager.UseHint())
        {
            ShowWatchAdOption();
        }
        else
        {
            HideWatchAdOption();
        }
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
        // 更新游戏时间
        if (isGameActive && isTimerEnabled)
        {
            currentGameTime = Time.time - gameStartTime;
            UpdateTimerText();
        }
    }

    protected override void OnPageShow()
    {
        base.OnPageShow();
        UpdateUI();
        UpdateHintUI();

        // 显示Banner广告
        if (AdManager.Instance != null)
        {
            AdManager.Instance.BannerHandler.ShowBanner();
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