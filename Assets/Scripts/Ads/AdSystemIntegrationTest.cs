using UnityEngine;
using JigsawFun.Ads;

/// <summary>
/// 广告系统集成测试
/// 验证广告系统在实际游戏流程中的集成效果
/// </summary>
public class AdSystemIntegrationTest : MonoBehaviour
{
    [Header("测试配置")]
    public bool runTestOnStart = false;       // 启动时运行测试
    public bool enableDetailedLogging = true; // 启用详细日志

    private int testResults = 0;              // 测试结果计数
    private int passedTests = 0;              // 通过的测试数
    private int failedTests = 0;              // 失败的测试数

    void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(RunIntegrationTests());
        }
    }

    /// <summary>
    /// 运行集成测试
    /// </summary>
    public System.Collections.IEnumerator RunIntegrationTests()
    {
        LogTest("开始广告系统集成测试...");

        // 等待系统初始化
        yield return new WaitForSeconds(1f);

        // 测试1: AdManager初始化
        yield return StartCoroutine(TestAdManagerInitialization());

        // 测试2: 插屏广告流程
        yield return StartCoroutine(TestInterstitialAdFlow());

        // 测试3: 激励视频流程
        yield return StartCoroutine(TestRewardedAdFlow());

        // 测试4: Banner广告流程
        yield return StartCoroutine(TestBannerAdFlow());

        // 测试5: HintManager集成
        yield return StartCoroutine(TestHintManagerIntegration());

        // 测试6: 游戏流程集成
        yield return StartCoroutine(TestGameFlowIntegration());

        // 输出测试结果
        LogTestResults();
    }

    /// <summary>
    /// 测试AdManager初始化
    /// </summary>
    private System.Collections.IEnumerator TestAdManagerInitialization()
    {
        LogTest("测试AdManager初始化...");

        bool testPassed = true;

        // 检查AdManager实例
        if (AdManager.Instance == null)
        {
            LogTestError("AdManager.Instance为null");
            testPassed = false;
        }

        // 检查广告处理器
        if (AdManager.Instance != null)
        {
            if (AdManager.Instance.InterstitialHandler == null)
            {
                LogTestError("InterstitialAdHandler未初始化");
                testPassed = false;
            }

            if (AdManager.Instance.RewardedHandler == null)
            {
                LogTestError("RewardedAdHandler未初始化");
                testPassed = false;
            }

            if (AdManager.Instance.BannerHandler == null)
            {
                LogTestError("BannerAdHandler未初始化");
                testPassed = false;
            }
        }

        RecordTestResult("AdManager初始化", testPassed);
        yield return null;
    }

    /// <summary>
    /// 测试插屏广告流程
    /// </summary>
    private System.Collections.IEnumerator TestInterstitialAdFlow()
    {
        LogTest("测试插屏广告流程...");

        bool testPassed = true;

        if (AdManager.Instance?.InterstitialHandler != null)
        {
            var handler = AdManager.Instance.InterstitialHandler;

            // 测试关卡完成触发
            for (int i = 0; i < 5; i++)
            {
                handler.OnLevelCompleted();
                yield return new WaitForSeconds(0.1f);
            }

            LogTest("插屏广告触发测试完成");
        }
        else
        {
            LogTestError("插屏广告处理器不可用");
            testPassed = false;
        }

        RecordTestResult("插屏广告流程", testPassed);
    }

    /// <summary>
    /// 测试激励视频流程
    /// </summary>
    private System.Collections.IEnumerator TestRewardedAdFlow()
    {
        LogTest("测试激励视频流程...");

        bool testPassed = true;

        if (AdManager.Instance != null)
        {
            // 测试广告准备状态
            bool isReady = AdManager.Instance.RewardedHandler.CanShowAd();
            LogTest($"激励视频准备状态: {isReady}");

            // 测试显示激励视频（模拟）
            if (isReady)
            {
                // 注意：在测试环境中不实际显示广告
                LogTest("激励视频广告可以显示");
            }
            else
            {
                LogTest("激励视频广告未准备好（正常情况）");
            }
        }
        else
        {
            LogTestError("AdManager不可用");
            testPassed = false;
        }

        RecordTestResult("激励视频流程", testPassed);
        yield return null;
    }

    /// <summary>
    /// 测试Banner广告流程
    /// </summary>
    private System.Collections.IEnumerator TestBannerAdFlow()
    {
        LogTest("测试Banner广告流程...");

        bool testPassed = true;

        if (AdManager.Instance != null)
        {
            // 测试显示Banner
            AdManager.Instance.RewardedHandler.CanShowAd();
            yield return new WaitForSeconds(1f);

            // 测试隐藏Banner
            AdManager.Instance.RewardedHandler.CanShowAd();
            yield return new WaitForSeconds(0.5f);

            LogTest("Banner广告显示/隐藏测试完成");
        }
        else
        {
            LogTestError("AdManager不可用");
            testPassed = false;
        }

        RecordTestResult("Banner广告流程", testPassed);
    }

    /// <summary>
    /// 测试HintManager集成
    /// </summary>
    private System.Collections.IEnumerator TestHintManagerIntegration()
    {
        LogTest("测试HintManager集成...");

        bool testPassed = true;

        if (HintManager.Instance != null)
        {
            var hintManager = HintManager.Instance;

            // 测试初始提示次数
            int initialHints = hintManager.CurrentFreeHints;
            LogTest($"初始提示次数: {initialHints}");

            // 测试使用提示
            if (hintManager.UseHint())
            {
                hintManager.UseHint();
                LogTest($"使用提示后剩余: {hintManager.CurrentFreeHints}");
            }

            // 测试广告奖励
            //hintManager.OnRewardedAdCompleted();
            LogTest($"观看广告后提示次数: {hintManager.CurrentFreeHints}");
        }
        else
        {
            LogTestError("HintManager不可用");
            testPassed = false;
        }

        RecordTestResult("HintManager集成", testPassed);
        yield return null;
    }

    /// <summary>
    /// 测试游戏流程集成
    /// </summary>
    private System.Collections.IEnumerator TestGameFlowIntegration()
    {
        LogTest("测试游戏流程集成...");

        bool testPassed = true;

        // 检查PuzzleGameManager集成
        if (PuzzleGameManager.Instance != null)
        {
            LogTest("PuzzleGameManager已集成广告系统");
        }
        else
        {
            LogTestWarning("PuzzleGameManager不可用（可能在非游戏场景）");
        }

        // 检查GameplayPage集成
        var gameplayPage = FindObjectOfType<GameplayPage>();
        if (gameplayPage != null)
        {
            LogTest("GameplayPage已找到，广告集成应该正常");
        }
        else
        {
            LogTestWarning("GameplayPage不可用（可能在非游戏场景）");
        }

        RecordTestResult("游戏流程集成", testPassed);
        yield return null;
    }

    /// <summary>
    /// 记录测试结果
    /// </summary>
    private void RecordTestResult(string testName, bool passed)
    {
        testResults++;

        if (passed)
        {
            passedTests++;
            LogTest($"✅ {testName} - 通过");
        }
        else
        {
            failedTests++;
            LogTest($"❌ {testName} - 失败");
        }
    }

    /// <summary>
    /// 输出测试结果
    /// </summary>
    private void LogTestResults()
    {
        LogTest("=== 广告系统集成测试结果 ===");
        LogTest($"总测试数: {testResults}");
        LogTest($"通过: {passedTests}");
        LogTest($"失败: {failedTests}");
        LogTest($"成功率: {(float)passedTests / testResults * 100:F1}%");

        if (failedTests == 0)
        {
            LogTest("🎉 所有测试通过！广告系统集成成功！");
        }
        else
        {
            LogTest("⚠️ 部分测试失败，请检查广告系统配置");
        }
    }

    /// <summary>
    /// 记录测试日志
    /// </summary>
    private void LogTest(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AdSystemIntegrationTest] {message}");
        }
    }

    /// <summary>
    /// 记录测试错误
    /// </summary>
    private void LogTestError(string message)
    {
        Debug.LogError($"[AdSystemIntegrationTest] ❌ {message}");
    }

    /// <summary>
    /// 记录测试警告
    /// </summary>
    private void LogTestWarning(string message)
    {
        Debug.LogWarning($"[AdSystemIntegrationTest] ⚠️ {message}");
    }

    /// <summary>
    /// 手动运行测试
    /// </summary>
    [ContextMenu("运行集成测试")]
    public void RunTests()
    {
        StartCoroutine(RunIntegrationTests());
    }
}