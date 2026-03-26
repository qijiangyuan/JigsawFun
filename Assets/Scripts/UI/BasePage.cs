using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public abstract class BasePage : MonoBehaviour
{
    [Header("页面设置")]
    public CanvasGroup canvasGroup;
    public RectTransform pageTransform;
    
    [Header("动画设置")]
    public float animationDuration = 0.3f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;
    
    [Header("动画类型")]
    public PageAnimationType animationType = PageAnimationType.Scale;
    
    protected bool isVisible = false;
    private Sequence activeSequence;
    [Header("Footer")]
    public bool showFooter = true;
    
    public enum PageAnimationType
    {
        Scale,      // 缩放动画
        Slide,      // 滑动动画
        Fade        // 淡入淡出
    }
    
    protected virtual void Awake()
    {
        // 如果没有指定CanvasGroup，尝试获取
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        // 如果没有指定RectTransform，使用当前对象的
        if (pageTransform == null)
            pageTransform = GetComponent<RectTransform>();
            
        // 初始化页面为隐藏状态
        InitializePage();
    }
    
    /// <summary>
    /// 初始化页面状态
    /// </summary>
    protected virtual void InitializePage()
    {
        KillActiveTweens();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        if (pageTransform != null)
        {
            switch (animationType)
            {
                case PageAnimationType.Scale:
                    pageTransform.localScale = Vector3.zero;
                    break;
                case PageAnimationType.Slide:
                    pageTransform.anchoredPosition = new Vector2(Screen.width, 0);
                    break;
            }
        }
        
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 显示页面
    /// </summary>
    public virtual void ShowPage()
    {
        ShowPage(true, null);
    }

    public virtual void ShowPage(bool withAnimation)
    {
        ShowPage(withAnimation, null);
    }

    public virtual void ShowPage(bool withAnimation, System.Action onComplete)
    {
        if (isVisible) return;
        
        gameObject.SetActive(true);
        isVisible = true;
        
        // 调用子类的显示逻辑
        OnPageShow();

        if (withAnimation)
        {
            PlayShowAnimation(onComplete);
        }
        else
        {
            SetVisibleInstant(true);
            onComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// 隐藏页面
    /// </summary>
    /// <param name="withAnimation">是否播放动画</param>
    public virtual void HidePage(bool withAnimation = true)
    {
        HidePage(withAnimation, null);
    }

    public virtual void HidePage(bool withAnimation, System.Action onComplete)
    {
        if (!isVisible) return;
        
        isVisible = false;

        if (withAnimation)
        {
            PlayHideAnimation(() => {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }
        else
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        // 调用子类的隐藏逻辑
        OnPageHide();
    }
    
    /// <summary>
    /// 播放显示动画
    /// </summary>
    protected virtual void PlayShowAnimation(System.Action onComplete = null)
    {
        KillActiveTweens();
        activeSequence = DOTween.Sequence();
        
        // 设置CanvasGroup可交互
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            activeSequence.Append(canvasGroup.DOFade(1f, animationDuration));
        }
        
        // 根据动画类型播放不同动画
        if (pageTransform != null)
        {
            switch (animationType)
            {
                case PageAnimationType.Scale:
                    pageTransform.localScale = Vector3.zero;
                    activeSequence.Join(pageTransform.DOScale(Vector3.one, animationDuration).SetEase(showEase));
                    break;
                    
                case PageAnimationType.Slide:
                    pageTransform.anchoredPosition = new Vector2(Screen.width, 0);
                    activeSequence.Join(pageTransform.DOAnchorPos(Vector2.zero, animationDuration).SetEase(showEase));
                    break;
                    
                case PageAnimationType.Fade:
                    // Fade动画已经通过CanvasGroup处理
                    break;
            }
        }
        
        activeSequence.OnComplete(() => onComplete?.Invoke());
    }
    
    /// <summary>
    /// 播放隐藏动画
    /// </summary>
    /// <param name="onComplete">动画完成回调</param>
    protected virtual void PlayHideAnimation(System.Action onComplete = null)
    {
        KillActiveTweens();
        activeSequence = DOTween.Sequence();
        
        // 设置CanvasGroup不可交互
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            activeSequence.Append(canvasGroup.DOFade(0f, animationDuration));
        }
        
        // 根据动画类型播放不同动画
        if (pageTransform != null)
        {
            switch (animationType)
            {
                case PageAnimationType.Scale:
                    activeSequence.Join(pageTransform.DOScale(Vector3.zero, animationDuration).SetEase(hideEase));
                    break;
                    
                case PageAnimationType.Slide:
                    // Slide 隐藏不移动位置，避免过渡中露出背景，只做淡出
                    break;
                    
                case PageAnimationType.Fade:
                    // Fade动画已经通过CanvasGroup处理
                    break;
            }
        }
        
        // 动画完成回调
        activeSequence.OnComplete(() => onComplete?.Invoke());
    }

    private void SetVisibleInstant(bool visible)
    {
        KillActiveTweens();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (pageTransform != null)
        {
            switch (animationType)
            {
                case PageAnimationType.Scale:
                    pageTransform.localScale = visible ? Vector3.one : Vector3.zero;
                    break;
                case PageAnimationType.Slide:
                    pageTransform.anchoredPosition = visible ? Vector2.zero : Vector2.zero;
                    break;
            }
        }
    }

    private void KillActiveTweens()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill(false);
            activeSequence = null;
        }
        if (canvasGroup != null) DOTween.Kill(canvasGroup, false);
        if (pageTransform != null) DOTween.Kill(pageTransform, false);
    }
    
    /// <summary>
    /// 页面显示时调用（子类重写）
    /// </summary>
    protected virtual void OnPageShow() { }
    
    /// <summary>
    /// 页面隐藏时调用（子类重写）
    /// </summary>
    protected virtual void OnPageHide() { }
    
    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    public virtual void OnBackButtonClicked()
    {
        UIManager.Instance.GoBack();
    }
}
