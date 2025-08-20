using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class VictoryPage : BasePage
{
    [Header("胜利界面组件")]
    public Text congratulationsText;        // 恭喜文本
    public Text completionTimeText;         // 完成时间文本
    public Text difficultyText;             // 难度文本
    public Text statisticsText;             // 统计信息文本
    
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
        
        // 计算星级评分
        CalculateStarRating();
        
        // 更新UI文本
        UpdateVictoryTexts();
        
        // 播放胜利音效
        PlayVictorySound();
        
        // 开始庆祝动画
        StartCoroutine(PlayVictoryAnimation());
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
            congratulationsText.text = "🎉 恭喜完成！";
        }
        
        // 完成时间文本
        if (completionTimeText != null)
        {
            int minutes = Mathf.FloorToInt(completionTime / 60f);
            int seconds = Mathf.FloorToInt(completionTime % 60f);
            completionTimeText.text = $"完成时间: {minutes:00}:{seconds:00}";
        }
        
        // 难度文本
        if (difficultyText != null)
        {
            difficultyText.text = $"难度: {difficulty}×{difficulty} ({difficulty * difficulty} 块)";
        }
        
        // 统计信息文本
        if (statisticsText != null)
        {
            string ratingText = GetRatingText();
            statisticsText.text = $"评级: {ratingText}\n{GetPerformanceText()}";
        }
    }
    
    /// <summary>
    /// 获取评级文本
    /// </summary>
    private string GetRatingText()
    {
        switch (starRating)
        {
            case 3: return "⭐⭐⭐ 完美！";
            case 2: return "⭐⭐ 很好！";
            case 1: return "⭐ 不错！";
            default: return "继续努力！";
        }
    }
    
    /// <summary>
    /// 获取表现文本
    /// </summary>
    private string GetPerformanceText()
    {
        switch (starRating)
        {
            case 3: return "你是拼图大师！";
            case 2: return "表现优秀！";
            case 1: return "继续加油！";
            default: return "多多练习！";
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
        if (playAgainButton != null)
        {
            CanvasGroup btnGroup = playAgainButton.GetComponent<CanvasGroup>();
            if (btnGroup == null) btnGroup = playAgainButton.gameObject.AddComponent<CanvasGroup>();
            btnGroup.alpha = 0f;
            btnGroup.DOFade(1f, 0.3f);
        }
        
        if (backToGalleryButton != null)
        {
            CanvasGroup btnGroup = backToGalleryButton.GetComponent<CanvasGroup>();
            if (btnGroup == null) btnGroup = backToGalleryButton.gameObject.AddComponent<CanvasGroup>();
            btnGroup.alpha = 0f;
            btnGroup.DOFade(1f, 0.3f).SetDelay(0.1f);
        }
    }
    
    /// <summary>
    /// 再玩一次按钮点击事件
    /// </summary>
    private void OnPlayAgainButtonClicked()
    {
        // 使用GameManager重新开始游戏
        if (GameManager.Instance != null)
        {
            var gameData = GameManager.Instance.currentGameData;
            GameManager.Instance.StartNewGame(gameData.selectedImage, gameData.difficulty, gameData.showBackground);
        }
        else
        {
            Debug.LogError("GameManager实例不存在！");
        }
    }
    
    /// <summary>
    /// 返回Gallery按钮点击事件
    /// </summary>
    private void OnBackToGalleryButtonClicked()
    {
        // 使用GameManager返回到Gallery
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToGallery();
        }
        else
        {
            Debug.LogError("GameManager实例不存在！");
        }
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