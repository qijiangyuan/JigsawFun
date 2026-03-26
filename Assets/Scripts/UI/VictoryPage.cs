using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using TMPro;

public class VictoryPage : BasePage
{
    [Header("胜利界面组件")]
    public TextMeshProUGUI congratulationsText;        // 恭喜文本
    public TextMeshProUGUI completionTimeText;         // 完成时间文本
    public TextMeshProUGUI difficultyText;             // 难度文本
    public TextMeshProUGUI statisticsText;             // 统计信息文本
    public Image puzzleImage;                          // 拼图图片显示

    [Header("按钮")]
    public Button playAgainButton;          // 再玩一次按钮
    public Button backToGalleryButton;      // 返回Gallery按钮

    [Header("动画效果")]
    public ParticleSystem celebrationParticles;  // 庆祝粒子效果
    public Image[] stars;                   // 星星评级
    public RectTransform congratsPanel;     // 恭喜面板

    [Header("音效")]
    public AudioSource audioSource;        // 音效播放器
    public AudioClip victorySound;          // 胜利音效
    public AudioClip starSound;             // 星星音效

    [Header("评级设置")]
    public float threeStarTime = 60f;       // 三星时间阈值
    public float twoStarTime = 120f;        // 二星时间阈值

    private float completionTime;
    private int difficulty;
    private int starRating;
    private static TMP_FontAsset sCjkFont;
    [SerializeField] private TMP_FontAsset cjkFontOverride;

    protected override void Awake()
    {
        showFooter = false;
        animationType = PageAnimationType.Scale;
        base.Awake();
        InitializeComponents();
        EnsureTopmost();
        EnsureCjkFont();
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        if (playAgainButton == null)
        {
            var btn = FindByNameInChildren<Button>("ReplaytButton");
            if (btn != null) playAgainButton = btn;
        }
        if (backToGalleryButton == null)
        {
            var btn = FindByNameInChildren<Button>("MainMenuButton");
            if (btn != null) backToGalleryButton = btn;
        }
        if (difficultyText == null)
        {
            var t = FindByNameInChildren<TextMeshProUGUI>("DifficultText");
            if (t != null) difficultyText = t;
        }

        // 设置按钮事件
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainButtonClicked);

        if (backToGalleryButton != null)
            backToGalleryButton.onClick.AddListener(OnBackToGalleryButtonClicked);

        // 初始化星星为隐藏状态
        if (stars != null)
        {
            foreach (Image star in stars)
            {
                if (star != null)
                {
                    star.color = new Color(1f, 1f, 1f, 0.3f); // 半透明灰色
                }
            }
        }
        EnsureCjkFont();
    }

    /// <summary>
    /// 显示胜利界面
    /// </summary>
    /// <param name="time">完成时间</param>
    /// <param name="puzzleDifficulty">拼图难度</param>
    public void ShowVictory(float time, int puzzleDifficulty)
    {
        completionTime = time;
        difficulty = puzzleDifficulty;

        EnsureTopmost();
        transform.SetAsLastSibling();
        EnsureCjkFont();

        // 显示拼图图片
        ShowPuzzleImage();

        // 计算星级评分
        CalculateStarRating();

        // 更新UI文本
        UpdateVictoryTexts();

        // 播放胜利音效
        PlayVictorySound();

        // 开始庆祝动画
        StartCoroutine(PlayVictoryAnimation());
    }

    private void ShowPuzzleImage()
    {
        puzzleImage.sprite = GameManager.Instance.currentGameData.selectedImage;
    }

    /// <summary>
    /// 计算星级评分
    /// </summary>
    private void CalculateStarRating()
    {
        // 根据完成时间和难度计算星级
        float adjustedTime = completionTime / (difficulty * difficulty * 0.1f); // 根据难度调整时间

        if (adjustedTime <= threeStarTime)
            starRating = 3;
        else if (adjustedTime <= twoStarTime)
            starRating = 2;
        else
            starRating = 1;
    }

    /// <summary>
    /// 更新胜利界面文本
    /// </summary>
    private void UpdateVictoryTexts()
    {
        // 恭喜文本
        if (congratulationsText != null)
        {
            congratulationsText.text = "Puzzle Completed!";
        }

        // 完成时间文本
        if (completionTimeText != null)
        {
            int minutes = Mathf.FloorToInt(completionTime / 60f);
            int seconds = Mathf.FloorToInt(completionTime % 60f);
            completionTimeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        // 难度文本
        if (difficultyText != null)
        {
            difficultyText.text = $"Difficulty: {difficulty}×{difficulty} ({difficulty * difficulty} Pieces)";
        }

        // 统计信息文本
        if (statisticsText != null)
        {
            string ratingText = GetRatingText();
            statisticsText.text = $"Rating: {ratingText}\n{GetPerformanceText()}";
        }
    }

    private Canvas _selfCanvas;
    private GraphicRaycaster _graphicRaycaster;
    private CanvasGroup _canvasGroup;
    private void EnsureTopmost()
    {
        if (_selfCanvas == null) _selfCanvas = GetComponent<Canvas>();
        if (_selfCanvas == null) _selfCanvas = gameObject.AddComponent<Canvas>();
        _selfCanvas.overrideSorting = true;
        _selfCanvas.sortingOrder = 10000;

        if (_graphicRaycaster == null) _graphicRaycaster = GetComponent<GraphicRaycaster>();
        if (_graphicRaycaster == null) _graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private void EnsureCjkFont()
    {
        TMP_FontAsset fontToUse = cjkFontOverride;
        if (fontToUse == null)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (sCjkFont == null)
            {
                try
                {
                    string[] names = new[]
                    {
                        "Microsoft YaHei","SimHei","NSimSun","DengXian","Noto Sans CJK SC","Source Han Sans SC","PingFang SC"
                    };
                    Font f = Font.CreateDynamicFontFromOSFont(names, 32);
                    if (f != null)
                    {
                        sCjkFont = TMP_FontAsset.CreateFontAsset(f);
                        if (sCjkFont != null)
                        {
                            sCjkFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                            sCjkFont.name = "RuntimeCJK";
                        }
                    }
                }
                catch {}
            }
#endif
            fontToUse = sCjkFont;
        }
        if (fontToUse == null) return;
        var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] != null) tmps[i].font = fontToUse;
        }
    }

    /// <summary>
    /// 获取评级文本
    /// </summary>
    private string GetRatingText()
    {
        switch (starRating)
        {
            case 3: return "Perfect!";
            case 2: return "Great!";
            case 1: return "Nice!";
            default: return "Keep trying!";
        }
    }

    /// <summary>
    /// 获取表现文本
    /// </summary>
    private string GetPerformanceText()
    {
        switch (starRating)
        {
            case 3: return "You are a Puzzle Master!";
            case 2: return "Excellent Performance!";
            case 1: return "Keep Going!";
            default: return "Practice More!";
        }
    }

    /// <summary>
    /// 播放胜利动画
    /// </summary>
    private IEnumerator PlayVictoryAnimation()
    {
        // 面板缩放动画
        if (congratsPanel != null)
        {
            congratsPanel.localScale = Vector3.zero;
            congratsPanel.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(0.5f);

        // 启动粒子效果
        if (celebrationParticles != null)
        {
            celebrationParticles.Play();
        }

        // 逐个点亮星星
        if (stars != null)
        {
            for (int i = 0; i < starRating && i < stars.Length; i++)
            {
                if (stars[i] != null)
                {
                    // 星星闪烁动画
                    stars[i].DOColor(Color.yellow, 0.3f).SetEase(Ease.OutQuad);
                    stars[i].transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);

                    // 播放星星音效
                    PlayStarSound();

                    yield return new WaitForSeconds(0.2f);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 按钮淡入动画
        //if (playAgainButton != null)
        //{
        //    CanvasGroup btnGroup = playAgainButton.GetComponent<CanvasGroup>();
        //    if (btnGroup == null) btnGroup = playAgainButton.gameObject.AddComponent<CanvasGroup>();
        //    btnGroup.alpha = 0f;
        //    btnGroup.DOFade(1f, 0.3f);
        //}

        //if (backToGalleryButton != null)
        //{
        //    CanvasGroup btnGroup = backToGalleryButton.GetComponent<CanvasGroup>();
        //    if (btnGroup == null) btnGroup = backToGalleryButton.gameObject.AddComponent<CanvasGroup>();
        //    btnGroup.alpha = 0f;
        //    btnGroup.DOFade(1f, 0.3f).SetDelay(0.1f);
        //}
    }

    /// <summary>
    /// 再玩一次按钮点击事件
    /// </summary>
    private void OnPlayAgainButtonClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null in OnPlayAgainButtonClicked");
            return;
        }
        var gd = GameManager.Instance.currentGameData;
        UIManager.Instance.ShowPage<LoadingPage>();
        HidePage(false);
        GameManager.Instance.StartNewGame(gd.selectedImage, gd.difficulty, gd.showBackground);
    }

    /// <summary>
    /// 返回Gallery按钮点击事件
    /// </summary>
    private void OnBackToGalleryButtonClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null in OnBackToGalleryButtonClicked");
            return;
        }
        // 清理旧生成器（若存在）
        if (JigsawGenerator.Instance != null)
        {
            JigsawGenerator.Instance.ClearPuzzles();
        }
        if (GameManager.Instance != null && GameManager.Instance.currentGameData != null && GameManager.Instance.currentGameData.selectedImage != null)
        {
            var id = GameManager.Instance.currentGameData.selectedImage.name;
            PlayPrefsManager.Instance.ClearPuzzleStateForImage(id);
        }
        // BoardGen 不需要显式清理，场景卸载会销毁；直接返回 Gallery
        UIManager.Instance.HidePage<VictoryPage>();
        GameManager.Instance.ReturnToGallery();
    }

    /// <summary>
    /// 播放胜利音效
    /// </summary>
    private void PlayVictorySound()
    {
        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }
    }

    /// <summary>
    /// 播放星星音效
    /// </summary>
    private void PlayStarSound()
    {
        if (audioSource != null && starSound != null)
        {
            audioSource.PlayOneShot(starSound);
        }
    }

    protected override void OnPageShow()
    {
        base.OnPageShow();

        var tray = FindObjectOfType<PuzzleScrollTray>(true);
        if (tray != null && tray.scrollRect != null)
        {
            tray.scrollRect.gameObject.SetActive(false);
        }

        // 重置动画状态
        ResetAnimationState();
    }

    /// <summary>
    /// 重置动画状态
    /// </summary>
    private void ResetAnimationState()
    {
        // 重置面板缩放
        if (congratsPanel != null)
        {
            congratsPanel.localScale = Vector3.zero;
        }

        // 重置星星状态
        if (stars != null)
        {
            foreach (Image star in stars)
            {
                if (star != null)
                {
                    star.color = new Color(1f, 1f, 1f, 0.3f);
                    star.transform.localScale = Vector3.one;
                }
            }
        }

        // 重置按钮透明度
        ResetButtonAlpha(playAgainButton);
        ResetButtonAlpha(backToGalleryButton);
    }

    /// <summary>
    /// 重置按钮透明度
    /// </summary>
    private void ResetButtonAlpha(Button button)
    {
        if (button != null)
        {
            CanvasGroup btnGroup = button.GetComponent<CanvasGroup>();
            if (btnGroup != null)
            {
                btnGroup.alpha = 0f;
            }
        }
    }

    private T FindByNameInChildren<T>(string name) where T : Component
    {
        var comps = GetComponentsInChildren<T>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i].name == name) return comps[i];
        }
        return null;
    }

    /// <summary>
    /// 获取星级评分
    /// </summary>
    public int GetStarRating()
    {
        return starRating;
    }

    /// <summary>
    /// 获取完成时间
    /// </summary>
    public float GetCompletionTime()
    {
        return completionTime;
    }
}
