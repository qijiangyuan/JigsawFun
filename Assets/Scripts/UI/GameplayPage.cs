using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class GameplayPage : BasePage
{
    [Header("拼图区域")]
    public Transform puzzleContainer;       // 拼图容器
    public Image backgroundImage;           // 背景图片
    public RectTransform puzzleArea;        // 拼图区域
    
    [Header("操作按钮")]
    public Button resetButton;              // 重置拼图按钮
    public Button hintButton;               // 提示原图按钮
    public Button timerToggleButton;        // 计时器开关按钮
    public Button backButton;               // 返回按钮
    
    [Header("UI显示")]
    public Text timerText;                  // 计时器文本
    public Image hintOverlay;               // 提示覆盖层
    public Text difficultyText;             // 难度显示
    public Text progressText;               // 进度显示
    
    [Header("音效")]
    public AudioSource audioSource;        // 音效播放器
    public AudioClip snapSound;             // 拼对音效
    public AudioClip completeSound;         // 完成音效
    
    [Header("设置")]
    public float hintDisplayTime = 3f;      // 提示显示时间
    public Sprite woodTexture;              // 木质背景纹理
    public Sprite fabricTexture;            // 布面背景纹理
    
    private GameSettings currentGameSettings;
    private JigsawGenerator jigsawGenerator;
    private float gameStartTime;
    private float currentGameTime;
    private bool isTimerEnabled = true;
    private bool isGameActive = false;
    private int completedPieces = 0;
    private int totalPieces = 0;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeComponents();
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
            hintButton.onClick.AddListener(OnHintButtonClicked);
            
        if (timerToggleButton != null)
            timerToggleButton.onClick.AddListener(OnTimerToggleClicked);
            
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
        
        // 获取拼图生成器
        jigsawGenerator = FindObjectOfType<JigsawGenerator>();
        
        // 初始化提示覆盖层
        if (hintOverlay != null)
        {
            hintOverlay.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 开始游戏
    /// </summary>
    /// <param name="gameSettings">游戏设置</param>
    public void StartGame(GameSettings gameSettings)
    {
        currentGameSettings = gameSettings;
        
        // 设置背景
        SetupBackground();
        
        // 生成拼图
        GeneratePuzzle();
        
        // 初始化游戏状态
        gameStartTime = Time.time;
        currentGameTime = 0f;
        isGameActive = true;
        completedPieces = 0;
        totalPieces = gameSettings.GetTotalPieces();
        
        // 更新UI
        UpdateUI();
    }
    
    /// <summary>
    /// 设置背景
    /// </summary>
    private void SetupBackground()
    {
        if (backgroundImage == null) return;
        
        if (currentGameSettings.showBackground)
        {
            // 显示选中的图片作为背景
            backgroundImage.sprite = currentGameSettings.selectedImage;
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
        if (jigsawGenerator == null || currentGameSettings.selectedImage == null)
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
    /// 订阅拼图事件
    /// </summary>
    private void SubscribeToPuzzleEvents()
    {
        // 这里需要与PuzzlePiece脚本配合，当拼图块正确放置时调用OnPieceCompleted
        // 当所有拼图块完成时调用OnPuzzleCompleted
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
        StartCoroutine(ShowHint());
    }
    
    /// <summary>
    /// 显示提示
    /// </summary>
    private IEnumerator ShowHint()
    {
        if (hintOverlay == null || currentGameSettings.selectedImage == null)
            yield break;
            
        // 设置提示图片
        hintOverlay.sprite = currentGameSettings.selectedImage;
        hintOverlay.gameObject.SetActive(true);
        
        // 淡入动画
        hintOverlay.color = new Color(1f, 1f, 1f, 0f);
        hintOverlay.DOFade(0.8f, 0.3f);
        
        // 等待指定时间
        yield return new WaitForSeconds(hintDisplayTime);
        
        // 淡出动画
        hintOverlay.DOFade(0f, 0.3f).OnComplete(() => {
            hintOverlay.gameObject.SetActive(false);
        });
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
        if (timerToggleButton == null) return;
        
        // 可以通过改变按钮颜色或文本来表示开关状态
        ColorBlock colors = timerToggleButton.colors;
        colors.normalColor = isTimerEnabled ? Color.green : Color.gray;
        timerToggleButton.colors = colors;
        
        // 更新计时器文本显示
        if (timerText != null)
        {
            timerText.gameObject.SetActive(isTimerEnabled);
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
        if (difficultyText != null && currentGameSettings != null)
        {
            difficultyText.text = $"难度: {currentGameSettings.GetDifficultyDescription()}";
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