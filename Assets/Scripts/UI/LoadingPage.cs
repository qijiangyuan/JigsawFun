using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

public class LoadingPage : BasePage
{
    [Header("加载界面组件")]
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI puzzleProgressText;

    [Header("加载设置")]
    public float minLoadingTime = 2f; // 最小加载时间

    private float sceneLoadProgress = 0f;
    private float puzzleGenerateProgress = 0f;
    private AsyncOperation sceneLoad; // 场景加载操作
    private float totalProgress = 0f;
    private bool isLoading = false;
    private string targetSceneName;
    private Action onLoadingComplete;

    protected override void Awake()
    {
        base.Awake();

        // 确保组件引用
        if (progressBar == null)
            progressBar = GetComponentInChildren<Slider>();
        if (progressText == null)
            progressText = transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
        if (statusText == null)
            statusText = transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        if (puzzleProgressText == null)
            puzzleProgressText = transform.Find("PuzzleProgressText")?.GetComponent<TextMeshProUGUI>();

    }


    protected override void OnPageShow()
    {
        base.OnPageShow();
        InitializeLoadingUI();
    }

    /// <summary>
    /// 初始化加载界面UI
    /// </summary>
    private void InitializeLoadingUI()
    {
        if (progressBar != null)
            progressBar.value = 0f;

        if (progressText != null)
            progressText.text = "0%";

        if (statusText != null)
            statusText.text = "Loading...";

        if (puzzleProgressText != null)
            puzzleProgressText.text = "Preparing to generate puzzle...";

        sceneLoadProgress = 0f;
        puzzleGenerateProgress = 0f;
        totalProgress = 0f;
    }

    /// <summary>
    /// 开始加载场景和拼图
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    /// <param name="onComplete">加载完成回调</param>
    public void StartLoading(string sceneName, Action onComplete = null)
    {
        if (isLoading) return;

        targetSceneName = sceneName;
        onLoadingComplete = onComplete;
        isLoading = true;

        StartCoroutine(LoadSceneAndPuzzle());
    }

    public void StartLoadScene(string sceneName, Action onComplete = null)
    {
        StartCoroutine(LoadScene(sceneName, onComplete));
    }

    public IEnumerator LoadScene(string sceneName, Action onComplete = null)
    {
        //使用Scene_JigsawGame场景
        isLoading = true;
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (asyncOp == null)
        {
            Debug.LogError("LoadSceneAsync 返回 null! 检查 Build Settings 和场景名字");
            yield break;
        }
        asyncOp.allowSceneActivation = false; // 默认就是 true，可以省略
                                              // 等待加载完成
        while (!asyncOp.isDone)
        {
            // 可以在这里处理进度条 asyncOp.progress (0~0.9f 之间才更新)
            Debug.Log($"Loading {sceneName}... {asyncOp.progress}");
            progressText.text = asyncOp.progress * 100 + "%";
            progressBar.value = asyncOp.progress;
            if (asyncOp.progress >= 0.9f)
            {
                asyncOp.allowSceneActivation = true;
            }
            yield return null;
        }



        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        Debug.Log("加载完成: " + sceneName);

        // 加载完成后回调
        onComplete?.Invoke();
        // 隐藏加载页面
        isLoading = false;
        HidePage();
    }

    /// <summary>
    /// 加载场景和拼图的协程
    /// </summary>
    private IEnumerator LoadSceneAndPuzzle()
    {
        float startTime = Time.time;

        // 开始异步加载场景
        sceneLoad = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        sceneLoad.allowSceneActivation = false; // 先不激活场景

        // 更新状态文本
        if (statusText != null)
            statusText.text = "Loading Scene...";

        // 监听场景加载进度
        while (sceneLoad.progress < 0.9f)
        {
            sceneLoadProgress = sceneLoad.progress;
            UpdateProgress();
            yield return null;
        }

        // 场景加载完成（但未激活）
        sceneLoadProgress = 0.9f;
        UpdateProgress();

        // 更新状态文本
        if (statusText != null)
            statusText.text = "Generating Puzzle...";

        // 监听拼图生成进度
        yield return StartCoroutine(WaitForPuzzleGeneration());

        // 确保最小加载时间
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minLoadingTime)
        {
            yield return new WaitForSeconds(minLoadingTime - elapsedTime);
        }

        // 完成加载
        sceneLoadProgress = 1f;
        puzzleGenerateProgress = 1f;
        UpdateProgress();

        if (statusText != null)
            statusText.text = "Loading Complete！";

        //yield return new WaitForSeconds(0.5f);

        // 激活场景
        sceneLoad.allowSceneActivation = true;

        // 等待场景完全激活
        yield return new WaitUntil(() => sceneLoad.isDone);

        // 调用完成回调
        onLoadingComplete?.Invoke();

        // 隐藏加载页面
        isLoading = false;
        HidePage();
    }

    /// <summary>
    /// 等待拼图生成完成
    /// </summary>
    private IEnumerator WaitForPuzzleGeneration()
    {
        // 等待场景激活
        sceneLoad.allowSceneActivation = true;
        yield return new WaitUntil(() => sceneLoad.isDone);

        // 查找JigsawGenerator
        JigsawGenerator jigsawGenerator = FindObjectOfType<JigsawGenerator>();
        if (jigsawGenerator == null)
        {
            Debug.LogError("未找到JigsawGenerator，使用模拟进度");
            yield return StartCoroutine(SimulatePuzzleGeneration());
            yield break;
        }
        else
        {
            jigsawGenerator.GeneratePuzzleFromGameData();
        }

        // 订阅拼图生成进度事件
        bool generationComplete = false;
        jigsawGenerator.OnGenerationProgress += OnPuzzleGenerationProgress;
        jigsawGenerator.OnGenerationComplete += () =>
        {
            generationComplete = true;
            //UIManager.Instance.ShowPage<GameplayPage>();
            //UIManager.Instance.HidePage<LoadingPage>();
        };

        // 等待拼图生成完成
        yield return new WaitUntil(() => generationComplete);

        // 取消订阅
        jigsawGenerator.OnGenerationProgress -= OnPuzzleGenerationProgress;
    }

    /// <summary>
    /// 拼图生成进度回调
    /// </summary>
    private void OnPuzzleGenerationProgress(float progress)
    {
        puzzleGenerateProgress = progress;

        if (puzzleProgressText != null)
        {
            int percentage = Mathf.RoundToInt(puzzleGenerateProgress * 100);
            puzzleProgressText.text = $"Puzzle Generation Progress: {percentage}%";
        }

        UpdateProgress();
    }

    /// <summary>
    /// 模拟拼图生成进度（备用方案）
    /// </summary>
    private IEnumerator SimulatePuzzleGeneration()
    {
        float simulatedProgress = 0f;

        while (simulatedProgress < 1f)
        {
            simulatedProgress += Time.deltaTime * 2f; // 模拟生成速度
            puzzleGenerateProgress = Mathf.Clamp01(simulatedProgress);

            // 更新拼图进度文本
            if (puzzleProgressText != null)
            {
                int percentage = Mathf.RoundToInt(puzzleGenerateProgress * 100);
                puzzleProgressText.text = $"Puzzle Generation Progress: {percentage}%";
            }

            UpdateProgress();
            yield return null;
        }
    }

    /// <summary>
    /// 更新总体进度
    /// </summary>
    private void UpdateProgress()
    {
        // 场景加载占50%，拼图生成占50%
        totalProgress = (sceneLoadProgress * 0.5f) + (puzzleGenerateProgress * 0.5f);

        if (progressBar != null)
            progressBar.value = totalProgress;

        if (progressText != null)
        {
            int percentage = Mathf.RoundToInt(totalProgress * 100);
            progressText.text = $"{percentage}%";
        }
    }

    /// <summary>
    /// 设置拼图生成进度（供外部调用）
    /// </summary>
    /// <param name="progress">进度值 0-1</param>
    public void SetPuzzleGenerationProgress(float progress)
    {
        puzzleGenerateProgress = Mathf.Clamp01(progress);

        if (puzzleProgressText != null)
        {
            int percentage = Mathf.RoundToInt(puzzleGenerateProgress * 100);
            puzzleProgressText.text = $"Puzzle Generation Progress: {percentage}%";
        }

        UpdateProgress();
    }

    /// <summary>
    /// 设置状态文本
    /// </summary>
    /// <param name="status">状态文本</param>
    public void SetStatusText(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    /// <summary>
    /// 检查是否正在加载
    /// </summary>
    public bool IsLoading => isLoading;
}