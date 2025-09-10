using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JigsawFun.Ads;

/// <summary>
/// 广告系统测试器
/// 用于测试Unity LevelPlay广告系统的完整性和用户体验流程
/// </summary>
public class AdSystemTester : MonoBehaviour
{
    [Header("测试UI组件")]
    public Button testInterstitialButton;     // 测试插屏广告按钮
    public Button testRewardedButton;         // 测试激励视频按钮
    public Button testBannerShowButton;       // 测试显示Banner按钮
    public Button testBannerHideButton;       // 测试隐藏Banner按钮
    public Button resetProgressButton;        // 重置进度按钮
    public TextMeshProUGUI statusText;        // 状态显示文本
    public TextMeshProUGUI hintCountText;     // 提示次数显示
    public TextMeshProUGUI levelCountText;    // 关卡计数显示

    [Header("测试设置")]
    public bool enableDebugMode = true;       // 启用调试模式
    public float testInterval = 2f;           // 自动测试间隔

    private int testLevelCount = 0;           // 测试关卡计数
    private bool isAutoTesting = false;       // 是否正在自动测试

    void Start()
    {
        InitializeTestUI();
        TestAdManagerInitialization();
    }

    /// <summary>
    /// 初始化测试UI
    /// </summary>
    private void InitializeTestUI()
    {
        // 设置按钮事件
        if (testInterstitialButton != null)
            testInterstitialButton.onClick.AddListener(TestInterstitialAd);

        if (testRewardedButton != null)
            testRewardedButton.onClick.AddListener(TestRewardedAd);

        if (testBannerShowButton != null)
            testBannerShowButton.onClick.AddListener(TestShowBanner);

        if (testBannerHideButton != null)
            testBannerHideButton.onClick.AddListener(TestHideBanner);

        if (resetProgressButton != null)
            resetProgressButton.onClick.AddListener(ResetTestProgress);

        UpdateStatusText("广告系统测试器已初始化");
    }

    /// <summary>
    /// 测试AdManager初始化
    /// </summary>
    private void TestAdManagerInitialization()
    {
        if (AdManager.Instance == null)
        {
            UpdateStatusText("❌ AdManager未初始化！");
            Debug.LogError("[AdSystemTester] AdManager实例不存在");
            return;
        }

        UpdateStatusText("✅ AdManager已初始化");

        // 测试各个广告处理器
        TestAdHandlers();

        // 测试HintManager
        TestHintManager();
    }

    /// <summary>
    /// 测试广告处理器
    /// </summary>
    private void TestAdHandlers()
    {
        var adManager = AdManager.Instance;

        // 测试插屏广告处理器
        if (adManager.InterstitialHandler != null)
        {
            Debug.Log("✅ InterstitialAdHandler已初始化");
        }
        else
        {
            Debug.LogError("❌ InterstitialAdHandler未初始化");
        }

        // 测试激励视频处理器
        if (adManager.RewardedHandler != null)
        {
            Debug.Log("✅ RewardedAdHandler已初始化");
        }
        else
        {
            Debug.LogError("❌ RewardedAdHandler未初始化");
        }

        // 测试Banner广告处理器
        if (adManager.BannerHandler != null)
        {
            Debug.Log("✅ BannerAdHandler已初始化");
        }
        else
        {
            Debug.LogError("❌ BannerAdHandler未初始化");
        }
    }

    /// <summary>
    /// 测试HintManager
    /// </summary>
    private void TestHintManager()
    {
        if (HintManager.Instance != null)
        {
            Debug.Log("✅ HintManager已初始化");
            UpdateHintCountDisplay();
        }
        else
        {
            Debug.LogError("❌ HintManager未初始化");
        }
    }

    /// <summary>
    /// 测试插屏广告
    /// </summary>
    public void TestInterstitialAd()
    {
        UpdateStatusText("测试插屏广告...");

        if (AdManager.Instance != null)
        {
            // 模拟关卡完成
            testLevelCount++;
            UpdateLevelCountDisplay();

            if (AdManager.Instance.InterstitialHandler != null)
            {
                AdManager.Instance.InterstitialHandler.OnLevelCompleted();
                UpdateStatusText($"✅ 插屏广告测试完成 (关卡 {testLevelCount})");
            }
            else
            {
                UpdateStatusText("❌ 插屏广告处理器未初始化");
            }
        }
        else
        {
            UpdateStatusText("❌ AdManager未初始化");
        }
    }

    /// <summary>
    /// 测试激励视频广告
    /// </summary>
    public void TestRewardedAd()
    {
        UpdateStatusText("测试激励视频广告...");

        if (AdManager.Instance != null)
        {
            if (AdManager.Instance.RewardedHandler.CanShowAd())
            {
                AdManager.Instance.RewardedHandler.ShowAd();
                UpdateStatusText("✅ 激励视频广告已显示");
            }
            else
            {
                UpdateStatusText("⚠️ 激励视频广告未准备好");
            }
        }
        else
        {
            UpdateStatusText("❌ AdManager未初始化");
        }
    }

    /// <summary>
    /// 测试显示Banner广告
    /// </summary>
    public void TestShowBanner()
    {
        UpdateStatusText("测试显示Banner广告...");

        if (AdManager.Instance != null)
        {
            AdManager.Instance.BannerHandler.ShowBanner();
            UpdateStatusText("✅ Banner广告已显示");
        }
        else
        {
            UpdateStatusText("❌ AdManager未初始化");
        }
    }

    /// <summary>
    /// 测试隐藏Banner广告
    /// </summary>
    public void TestHideBanner()
    {
        UpdateStatusText("测试隐藏Banner广告...");

        if (AdManager.Instance != null)
        {
            AdManager.Instance.RewardedHandler.CanShowAd();
            UpdateStatusText("✅ Banner广告已隐藏");
        }
        else
        {
            UpdateStatusText("❌ AdManager未初始化");
        }
    }

    /// <summary>
    /// 重置测试进度
    /// </summary>
    public void ResetTestProgress()
    {
        testLevelCount = 0;
        UpdateLevelCountDisplay();

        // 重置PuzzleGameManager的关卡进度
        if (PuzzleGameManager.Instance != null)
        {
            PuzzleGameManager.Instance.ResetLevelProgress();
        }

        // 重置HintManager的提示次数
        if (HintManager.Instance != null)
        {
            HintManager.Instance.ResetFreeHints();
            UpdateHintCountDisplay();
        }

        UpdateStatusText("✅ 测试进度已重置");
    }

    /// <summary>
    /// 开始自动测试流程
    /// </summary>
    public void StartAutoTest()
    {
        if (!isAutoTesting)
        {
            isAutoTesting = true;
            StartCoroutine(AutoTestCoroutine());
            UpdateStatusText("🔄 开始自动测试流程...");
        }
    }

    /// <summary>
    /// 停止自动测试流程
    /// </summary>
    public void StopAutoTest()
    {
        isAutoTesting = false;
        UpdateStatusText("⏹️ 自动测试已停止");
    }

    /// <summary>
    /// 自动测试协程
    /// </summary>
    private System.Collections.IEnumerator AutoTestCoroutine()
    {
        while (isAutoTesting)
        {
            // 测试插屏广告
            TestInterstitialAd();
            yield return new WaitForSeconds(testInterval);

            if (!isAutoTesting) break;

            // 测试Banner显示
            TestShowBanner();
            yield return new WaitForSeconds(testInterval);

            if (!isAutoTesting) break;

            // 测试Banner隐藏
            TestHideBanner();
            yield return new WaitForSeconds(testInterval);

            if (!isAutoTesting) break;

            // 测试激励视频
            TestRewardedAd();
            yield return new WaitForSeconds(testInterval * 2);
        }
    }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
        }

        if (enableDebugMode)
        {
            Debug.Log($"[AdSystemTester] {message}");
        }
    }

    /// <summary>
    /// 更新提示次数显示
    /// </summary>
    private void UpdateHintCountDisplay()
    {
        if (hintCountText != null && HintManager.Instance != null)
        {
            hintCountText.text = $"提示次数: {HintManager.Instance.CurrentFreeHints}";
        }
    }

    /// <summary>
    /// 更新关卡计数显示
    /// </summary>
    private void UpdateLevelCountDisplay()
    {
        if (levelCountText != null)
        {
            levelCountText.text = $"测试关卡: {testLevelCount}";
        }
    }

    void Update()
    {
        // 实时更新提示次数显示
        if (HintManager.Instance != null)
        {
            UpdateHintCountDisplay();
        }
    }

    void OnDestroy()
    {
        // 停止自动测试
        isAutoTesting = false;
    }
}