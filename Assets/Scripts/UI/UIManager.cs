using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.EventSystems;
using JigsawFun.Ads;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("页面引用")]
    //public BasePage galleryPage;
    //public BasePage difficultyPage;
    //public BasePage gameplayPage;
    //public BasePage victoryPage;
    public List<BasePage> pages;
    //public LoadingPage loadingPage; // 加载页面引用

    public GameObject EventSystem;
    public GameObject Canvas;

    private readonly Stack<BasePage> pageStack = new Stack<BasePage>();
    private BasePage currentPage;
    private bool isLoadingPageActive = false;
    [SerializeField] private float bannerHeightDpPhone = 250f;
    [SerializeField] private float bannerHeightDpTablet = 90f;
    private RectTransform cachedFooterRect;
    private Vector2 cachedFooterAnchoredPos;
    private bool cachedFooterPosValid;
    private bool footerOffsetApplied;
    [SerializeField] private bool debugUseSimulatedBannerVisibility;
    [SerializeField] private bool debugSimulatedBannerVisible;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(EventSystem);
            DontDestroyOnLoad(Canvas);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始化所有页面为隐藏状态
        HideAllPages();

        // 订阅GameManager事件
        SubscribeToGameEvents();
        AdManager.OnAdManagerInitialized -= HandleAdManagerInitialized;
        AdManager.OnAdManagerInitialized += HandleAdManagerInitialized;
        TryHookBannerEvents();
        UpdateFooterForBanner();

        // 显示Gallery页面作为首页
        ShowPage<GalleryPage>();
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        UnsubscribeFromGameEvents();
        AdManager.OnAdManagerInitialized -= HandleAdManagerInitialized;
        UnhookBannerEvents();
    }

    /// <summary>
    /// 通用显示页面方法
    /// </summary>
    /// <typeparam name="T">页面类型</typeparam>
    /// <param name="setupAction">页面设置回调</param>
    public void ShowPage<T>(Action<T> setupAction = null) where T : BasePage
    {
        T targetPage = null;
        foreach (BasePage page in pages)
        {
            if (page is T)
            {
                targetPage = page as T;
                break;
            }
        }


        if (targetPage == null)
        {
            Debug.LogError($"Page of type {typeof(T).Name} not found!");
            return;
        }

        ShowPageInternal(targetPage, true, setupAction);
    }

    public void HidePage<T>() where T : BasePage
    {
        T targetPage = null;
        foreach (BasePage page in pages)
        {
            if (page is T)
            {
                targetPage = page as T;
                break;
            }
        }

        if (targetPage == null)
        {
            Debug.LogError($"Page of type {typeof(T).Name} not found!");
            return;
        }

        targetPage.HidePage((targetPage is DifficultyPage) || (targetPage is VictoryPage));
    }


    /// <summary>
    /// 显示指定页面实例
    /// </summary>
    /// <param name="page">要显示的页面</param>
    /// <param name="addToStack">是否添加到页面栈中</param>
    public void ShowPage(BasePage page, bool addToStack = true)
    {
        ShowPageInternal(page, addToStack, (Action<BasePage>)null);
    }

    private void ShowPageInternal<T>(T page, bool addToStack, Action<T> setupAction) where T : BasePage
    {
        if (page == null) return;
        if (currentPage == page)
        {
            bool useAnim = (page is DifficultyPage) || (page is VictoryPage);
            bool setupBeforeShow = page is DifficultyPage;
            if (setupBeforeShow) setupAction?.Invoke(page);
            if (page is LoadingPage)
            {
                page.ShowPage(false);
                page.transform.SetAsLastSibling();
                setupAction?.Invoke(page);
                SetFooterVisible(false);
                UpdateBannerForPage(page);
                return;
            }
            page.ShowPage(useAnim, () =>
            {
                if (!setupBeforeShow) setupAction?.Invoke(page);
                SetFooterVisible(page.showFooter && !(page is LoadingPage));
                UpdateBannerForPage(page);
            });
            return;
        }

        BasePage previous = currentPage;
        if (addToStack && previous != null)
        {
            pageStack.Push(previous);
        }

        currentPage = page;

        bool useAnimation = (page is DifficultyPage) || (page is VictoryPage);
        bool setupBefore = page is DifficultyPage;
        if (setupBefore) setupAction?.Invoke(page);
        page.ShowPage(useAnimation, () =>
        {
            if (!setupBefore) setupAction?.Invoke(page);
            SetFooterVisible(page.showFooter && !(page is LoadingPage));
            UpdateBannerForPage(page);

            if (useAnimation && previous != null && previous != page && !(previous is LoadingPage))
            {
                previous.HidePage((previous is DifficultyPage) || (previous is VictoryPage));
            }
        });

        if (!useAnimation && previous != null && previous != page && !(previous is LoadingPage))
        {
            previous.HidePage((previous is DifficultyPage) || (previous is VictoryPage));
        }

    }



    /// <summary>
    /// 返回上一页
    /// </summary>
    public void GoBack()
    {
        if (pageStack.Count > 0)
        {
            BasePage previousPage = pageStack.Pop();
            ShowPage(previousPage, false);
            return;
        }

        if (currentPage != null)
        {
            currentPage.HidePage((currentPage is DifficultyPage) || (currentPage is VictoryPage));
            currentPage = null;
        }
    }

    ///// <summary>
    ///// 清空页面栈
    ///// </summary>
    //public void ClearPageStack()
    //{
    //    pageStack.Clear();
    //}

    /// <summary>
    /// 隐藏所有页面
    /// </summary>
    private void HideAllPages()
    {
        pageStack.Clear();
        currentPage = null;
        foreach (BasePage page in pages)
        {
            if (page != null)
            {
                page.HidePage(false);
            }
        }
        SetFooterVisible(true);
        UpdateBannerForPage(null);
    }

    private void KeepFooterOnTop()
    {
        Transform root = Canvas != null ? Canvas.transform : transform.root;
        if (root == null) return;
        Transform footer = FindChildByName(root, "Footer");
        if (footer != null) footer.SetAsLastSibling();
    }

    private void SetFooterVisible(bool visible)
    {
        Transform root = Canvas != null ? Canvas.transform : transform.root;
        if (root == null) return;
        Transform footer = FindChildByName(root, "Footer");
        if (footer != null)
        {
            footer.gameObject.SetActive(visible);
            if (visible) footer.SetAsLastSibling();
        }
        UpdateFooterForBanner();
    }

    private Transform FindChildByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t != null && t.name == name) return t;
        }
        return null;
    }

    private void HandleAdManagerInitialized()
    {
        TryHookBannerEvents();
        UpdateFooterForBanner();
    }

    private void TryHookBannerEvents()
    {
        var ad = AdManager.Instance != null ? AdManager.Instance : FindObjectOfType<AdManager>(true);
        if (ad == null || ad.BannerHandler == null) return;
        ad.BannerHandler.OnBannerVisibilityChanged -= HandleBannerVisibilityChanged;
        ad.BannerHandler.OnBannerVisibilityChanged += HandleBannerVisibilityChanged;
    }

    private void UnhookBannerEvents()
    {
        var ad = AdManager.Instance;
        if (ad == null || ad.BannerHandler == null) return;
        ad.BannerHandler.OnBannerVisibilityChanged -= HandleBannerVisibilityChanged;
    }

    private void HandleBannerVisibilityChanged(bool _)
    {
        UpdateFooterForBanner();
    }

    private void UpdateBannerForPage(BasePage page)
    {
        if (page == null) return;
        if (debugUseSimulatedBannerVisibility) return;
        var ad = AdManager.Instance != null ? AdManager.Instance : FindObjectOfType<AdManager>(true);
        if (ad == null) 
        {
            Debug.Log("[UIManager] AdManager is null, cannot show/hide banner");
            return;
        }
        if (ad.BannerHandler == null)
        {
            Debug.Log("[UIManager] BannerHandler is null, cannot show/hide banner");
            return;
        }

        bool shouldShow = page is LoadingPage || page is GameplayPage || page is VictoryPage;
        if (shouldShow)
        {
            Debug.Log($"[UIManager] 请求显示Banner，因为当前页面是: {page.GetType().Name}");
            ad.BannerHandler.ShowBanner();
        }
        else
        {
            Debug.Log($"[UIManager] 请求隐藏Banner，因为当前页面是: {page.GetType().Name}");
            ad.BannerHandler.HideBanner();
        }
    }

    private void UpdateFooterForBanner()
    {
        Transform root = Canvas != null ? Canvas.transform : transform.root;
        if (root == null) return;
        var footer = FindChildByName(root, "Footer") as RectTransform;
        if (footer == null) return;

        if (cachedFooterRect != footer)
        {
            cachedFooterRect = footer;
            cachedFooterPosValid = false;
            footerOffsetApplied = false;
        }

        if (!cachedFooterPosValid)
        {
            cachedFooterAnchoredPos = footer.anchoredPosition;
            cachedFooterPosValid = true;
        }

        bool footerVisible = footer.gameObject.activeInHierarchy;
        bool bannerVisible = debugUseSimulatedBannerVisibility
            ? debugSimulatedBannerVisible
            : (AdManager.Instance != null && AdManager.Instance.BannerHandler != null && AdManager.Instance.BannerHandler.IsBannerVisible());

        if (!footerVisible || !bannerVisible)
        {
            if (footerOffsetApplied)
            {
                footer.anchoredPosition = cachedFooterAnchoredPos;
                footerOffsetApplied = false;
            }
            return;
        }

        float pixels = GetBannerHeightPixels();
        float scaleFactor = 1f;
        var canvasComp = Canvas != null ? Canvas.GetComponent<UnityEngine.Canvas>() : null;
        if (canvasComp != null && canvasComp.scaleFactor > 0.01f) scaleFactor = canvasComp.scaleFactor;
        float offset = pixels / scaleFactor;

        footer.anchoredPosition = new Vector2(cachedFooterAnchoredPos.x, cachedFooterAnchoredPos.y + offset);
        footerOffsetApplied = true;
    }

    public void DebugBannerVisibleOn()
    {
        debugUseSimulatedBannerVisibility = true;
        debugSimulatedBannerVisible = true;
        UpdateFooterForBanner();
    }

    public void DebugBannerVisibleOff()
    {
        debugUseSimulatedBannerVisibility = true;
        debugSimulatedBannerVisible = false;
        UpdateFooterForBanner();
    }

    public void DebugBannerVisibleToggle()
    {
        debugUseSimulatedBannerVisibility = true;
        debugSimulatedBannerVisible = !debugSimulatedBannerVisible;
        UpdateFooterForBanner();
    }

    public void DebugBannerUseRealVisibility()
    {
        debugUseSimulatedBannerVisibility = false;
        UpdateFooterForBanner();
    }

    private float GetBannerHeightPixels()
    {
        float dpi = Screen.dpi;
        if (dpi <= 0f) dpi = 160f;
        float minInches = Mathf.Min(Screen.width, Screen.height) / dpi;
        bool tablet = minInches >= 6.5f;
        float dp = tablet ? bannerHeightDpTablet : bannerHeightDpPhone;
        return dp * (dpi / 160f);
    }

    //// 也隐藏加载页面
    //if (loadingPage != null)
    //{
    //    loadingPage.HidePage();
    //}

    ///// <summary>
    ///// 显示加载页面并开始加载
    ///// </summary>
    ///// <param name="targetSceneName">目标场景名称</param>
    ///// <param name="onLoadingComplete">加载完成回调</param>
    //public void ShowLoadingPage(string targetSceneName, System.Action onLoadingComplete = null)
    //{
    //    if (loadingPage == null)
    //    {
    //        Debug.LogError("LoadingPage reference is null! Please assign it in the inspector.");
    //        return;
    //    }

    //    if (isLoadingPageActive)
    //    {
    //        Debug.LogWarning("Loading page is already active!");
    //        return;
    //    }

    //    isLoadingPageActive = true;

    //    // 隐藏当前页面但不添加到栈中
    //    if (currentPage != null)
    //    {
    //        currentPage.HidePage();
    //    }

    //    // 显示加载页面
    //    loadingPage.ShowPage();

    //    // 开始加载
    //    loadingPage.StartLoading(targetSceneName, () =>
    //    {
    //        isLoadingPageActive = false;
    //        onLoadingComplete?.Invoke();
    //    });
    //}

    /// <summary>
    /// 检查加载页面是否激活
    /// </summary>
    public bool IsLoadingPageActive => isLoadingPageActive;

    /// <summary>
    /// 获取加载页面引用
    /// </summary>
    //public LoadingPage GetLoadingPage() => loadingPage;

    #region GameManager Event Handling

    private void SubscribeToGameEvents()
    {
        GameManager.OnGameStateChanged += OnGameStateChanged;
        GameManager.OnGameStarted += OnGameStarted;
        GameManager.OnGameCompleted += OnGameCompleted;
    }

    private void UnsubscribeFromGameEvents()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        GameManager.OnGameStarted -= OnGameStarted;
        GameManager.OnGameCompleted -= OnGameCompleted;
    }

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Gallery:
                ShowPage<GalleryPage>();
                break;
            case GameManager.GameState.DifficultySelection:
                // DifficultyPage会通过其他方式显示
                break;
            case GameManager.GameState.Playing:
                // GameplayPage会在游戏场景中显示

                break;
            case GameManager.GameState.Victory:
                ShowPage<VictoryPage>(page =>
                {
                    if (GameManager.Instance != null)
                    {
                        page.ShowVictory(GameManager.Instance.currentGameData.completionTime,
                                       GameManager.Instance.currentGameData.difficulty);
                    }
                });
                break;
        }
    }

    private void OnGameStarted(Sprite selectedImage, int difficulty)
    {
        ShowPage<GameplayPage>(page =>
        {
            page.StartGame(new GameManager.GameData
            {
                selectedImage = selectedImage,
                difficulty = difficulty,
                showBackground = GameManager.Instance.currentGameData.showBackground
            });
        });


    }

    private void OnGameCompleted(float completionTime, int starRating)
    {
        // 游戏完成时的UI处理
        // VictoryPage会通过状态变化事件显示
    }

    #endregion

}
