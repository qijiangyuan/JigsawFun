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
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        //if (pageTransform != null)
        //{
        //    switch (animationType)
        //    {
        //        case PageAnimationType.Scale:
        //            pageTransform.localScale = Vector3.zero;
        //            break;
        //        case PageAnimationType.Slide:
        //            pageTransform.anchoredPosition = new Vector2(Screen.width, 0);
        //            break;
        //    }
        //}
        
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 显示页面
    /// </summary>
    public virtual void ShowPage()
    {
        if (isVisible) return;
        
        gameObject.SetActive(true);
        isVisible = true;
        
        // 执行显示动画
        //PlayShowAnimation();
        
        // 调用子类的显示逻辑
        OnPageShow();
    }
    
    /// <summary>
    /// 隐藏页面
    /// </summary>
    /// <param name="withAnimation">是否播放动画</param>
    public virtual void HidePage(bool withAnimation = true)
    {
        if (!isVisible) return;
        
        isVisible = false;

        //if (withAnimation)
        //{
        //    PlayHideAnimation(() => {
        //        gameObject.SetActive(false);
        //    });
        //}
        //else
        //{
        //    gameObject.SetActive(false);
        //}

        gameObject.SetActive(false);

        // 调用子类的隐藏逻辑
        OnPageHide();
    }
    
    /// <summary>
    /// 播放显示动画
    /// </summary>
    protected virtual void PlayShowAnimation()
    {
        Sequence showSequence = DOTween.Sequence();
        
        // 设置CanvasGroup可交互
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            showSequence.Append(canvasGroup.DOFade(1f, animationDuration));
        }
        
        // 根据动画类型播放不同动画
        if (pageTransform != null)
        {
            switch (animationType)
            {
                case PageAnimationType.Scale:
                    pageTransform.localScale = Vector3.zero;
                    showSequence.Join(pageTransform.DOScale(Vector3.one, animationDuration).SetEase(showEase));
                    break;
                    
                case PageAnimationType.Slide:
                    pageTransform.anchoredPosition = new Vector2(Screen.width, 0);
                    showSequence.Join(pageTransform.DOAnchorPos(Vector2.zero, animationDuration).SetEase(showEase));
                    break;
                    
                case PageAnimationType.Fade:
                    // Fade动画已经通过CanvasGroup处理
                    break;
            }
        }
    }
    
    /// <summary>
    /// 播放隐藏动画
    /// </summary>
    /// <param name="onComplete">动画完成回调</param>
    protected virtual void PlayHideAnimation(System.Action onComplete = null)
    {
        Sequence hideSequence = DOTween.Sequence();
        
        // 设置CanvasGroup不可交互
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            hideSequence.Append(canvasGroup.DOFade(0f, animationDuration));
        }
        
        // 根据动画类型播放不同动画
        if (pageTransform != null)
        {
            switch (animationType)
            {
                case PageAnimationType.Scale:
                    hideSequence.Join(pageTransform.DOScale(Vector3.zero, animationDuration).SetEase(hideEase));
                    break;
                    
                case PageAnimationType.Slide:
                    hideSequence.Join(pageTransform.DOAnchorPos(new Vector2(Screen.width, 0), animationDuration).SetEase(hideEase));
                    break;
                    
                case PageAnimationType.Fade:
                    // Fade动画已经通过CanvasGroup处理
                    break;
            }
        }
        
        // 动画完成回调
        hideSequence.OnComplete(() => onComplete?.Invoke());
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