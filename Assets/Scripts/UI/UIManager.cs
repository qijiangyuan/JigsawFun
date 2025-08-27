using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.EventSystems;

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

    //private Stack<BasePage> pageStack = new Stack<BasePage>();
    private BasePage currentPage;
    private bool isLoadingPageActive = false;

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

        // 显示Gallery页面作为首页
        ShowPage<GalleryPage>();
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        UnsubscribeFromGameEvents();
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



        ShowPage(targetPage);

        // 执行页面设置
        setupAction?.Invoke(targetPage);
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

        targetPage.HidePage();
    }


    /// <summary>
    /// 显示指定页面实例
    /// </summary>
    /// <param name="page">要显示的页面</param>
    /// <param name="addToStack">是否添加到页面栈中</param>
    public void ShowPage(BasePage page, bool addToStack = true)
    {
        if (page == null) return;

        // 如果当前有页面，先隐藏
        //if (currentPage != null)
        //{
        //    currentPage.HidePage();
        //    if (addToStack)
        //    {
        //        pageStack.Push(currentPage);
        //    }
        //}

        currentPage = page;
        page.ShowPage();
    }



    /// <summary>
    /// 返回上一页
    /// </summary>
    public void GoBack()
    {
        //if (pageStack.Count > 0)
        //{
        //    BasePage previousPage = pageStack.Pop();
        //    ShowPage(previousPage, false);
        //}

        currentPage.HidePage();
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
        foreach (BasePage page in pages)
        {
            if (page != null)
            {
                page.HidePage();
            }
        }

        //// 也隐藏加载页面
        //if (loadingPage != null)
        //{
        //    loadingPage.HidePage();
        //}
    }

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