using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

public class GameplayPage : BasePage
{
    [Header("拼图区域")]
    public Transform puzzleContainer;       // 拼图容器
    public Image backgroundImage;           // 背景图片
    public RectTransform puzzleArea;        // 拼图区域

    [Header("操作按钮")]
    public Button resetButton;              // 重置拼图按钮
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

    private GameData currentGameData;

    private float gameStartTime;
    private float currentGameTime;
    private bool isTimerEnabled = true;
    private bool isGameActive = false;
    private int completedPieces = 0;
    private int totalPieces = 0;

    //免费提示次数 
    private int freeHintCount = 3; // 初始免费提示次数

    private PuzzlePiece currentPiece; // 当前选中的拼图块

    protected override void Awake()
    {
        base.Awake();
        InitializeComponents();
    }

    private void OnEnable()
    {
        SubscribeToPuzzleEvents();

    }

    private void OnDisable()
    {
        UnsubscribeFromPuzzleEvents();
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
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        if (hintButton != null)
        {
            hintButton.onClick.AddListener(OnHintButtonClicked);
            hintButton.gameObject.SetActive(false); // 初始隐藏提示按钮
            hintRemainText.text = $"x{freeHintCount}";
        }



        //if (timerToggleButton != null)
        //    timerToggleButton.onClick.AddListener(OnTimerToggleClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

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
    }

    /// <summary>
    /// 重置按钮点击事件
    /// </summary>
    private void OnResetButtonClicked()
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
        if (freeHintCount > 0)
        {
            currentPiece?.SnapToCorrectPosition();
            ConsumeFreeHint();
        }
        else
        {
            // 如果没有免费提示次数，可以在这里处理收费提示逻辑
            Debug.Log("没有免费提示次数了，请充值！");
        }
    }

    //消耗免费提示次数
    private void ConsumeFreeHint()
    {
        if (freeHintCount > 0)
        {
            freeHintCount--;
            Debug.Log($"免费提示次数剩余: {freeHintCount}");
            // 可以在这里显示提示次数的UI更新
            hintRemainText.text = $"x{freeHintCount}";
        }
        else
        {
            Debug.Log("没有免费提示次数了，请充值！");
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
    }

    protected override void OnPageHide()
    {
        base.OnPageHide();

        // 停止游戏
        isGameActive = false;
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