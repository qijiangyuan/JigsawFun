using UnityEngine;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

namespace JigsawFun.Ads
{
    /// <summary>
    /// 插屏广告处理器，负责插屏广告的加载、显示和频率控制
    /// </summary>
    public class InterstitialAdHandler : MonoBehaviour
    {
        private AdConfig adConfig;
        private string adUnitId;
        private bool isInitialized;
        
        // 广告状态
        private bool isAdLoaded = false;
        private bool isAdShowing = false;
        private bool isLoading;
        private Coroutine retryCoroutine;
        private float nextAllowedLoadTime;
        
        // 频率控制
        private int completedLevels = 0;
        private float lastAdShowTime = 0f;
        
        // 事件
        public static event Action OnInterstitialAdShown;
        public static event Action OnInterstitialAdClosed;
        public static event Action<string> OnInterstitialAdFailed;

        LevelPlayInterstitialAd interstitialAd;

        /// <summary>
        /// 初始化插屏广告处理器
        /// </summary>
        /// <param name="config">广告配置</param>
        public void Initialize(AdConfig config)
        {
            adConfig = config;
            adUnitId = config.InterstitialAdUnitId;
            isInitialized = true;
            EnsureAdObject();
            
            Log("插屏广告处理器初始化完成");
        }
        
        /// <summary>
        /// 注册LevelPlay事件回调
        /// </summary>
        private void RegisterLevelPlayEvents()
        {
            interstitialAd.OnAdLoaded += OnAdLoaded;
            interstitialAd.OnAdLoadFailed += OnAdLoadFailed;
            interstitialAd.OnAdDisplayed += OnAdDisplayed;
            interstitialAd.OnAdDisplayFailed += OnAdDisplayFailed;
            interstitialAd.OnAdClicked += OnAdClicked;
            interstitialAd.OnAdClosed += OnAdClosed;
        }
        
        /// <summary>
        /// 注销LevelPlay事件回调
        /// </summary>
        private void UnregisterLevelPlayEvents()
        {
            interstitialAd.OnAdLoaded -= OnAdLoaded;
            interstitialAd.OnAdLoadFailed -= OnAdLoadFailed;
            interstitialAd.OnAdDisplayed -= OnAdDisplayed;
            interstitialAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            interstitialAd.OnAdClicked -= OnAdClicked;
            interstitialAd.OnAdClosed -= OnAdClosed;
        }
        
        /// <summary>
        /// 加载插屏广告
        /// </summary>
        public void LoadAd()
        {
            if (!CanLoadNow()) return;
            if (Time.unscaledTime < nextAllowedLoadTime) return;
            if (isLoading) return;

            if (isAdLoaded)
            {
                Log("插屏广告已加载，无需重复加载");
                return;
            }
            
            try
            {
                Log("开始加载插屏广告...");
                isLoading = true;
                interstitialAd.LoadAd();
            }
            catch (Exception e)
            {
                isLoading = false;
                LogError($"加载插屏广告失败: {e.Message}");
            }
        }

        private bool CanLoadNow()
        {
            if (!isInitialized) return false;
            if (AdManager.Instance != null && !AdManager.Instance.IsInitialized) return false;
            return EnsureAdObject();
        }

        private bool EnsureAdObject()
        {
            if (interstitialAd != null) return true;
            if (string.IsNullOrEmpty(adUnitId)) return false;
            interstitialAd = new LevelPlayInterstitialAd(adUnitId);
            RegisterLevelPlayEvents();
            return true;
        }
        
        /// <summary>
        /// 显示插屏广告
        /// </summary>
        /// <param name="placement">广告位置标识</param>
        public void ShowAd(string placement = "Level_Complete")
        {
            if (!CanShowAd())
            {
                Log("当前无法显示插屏广告");
                return;
            }
            
            try
            {
                Log($"显示插屏广告，位置: {placement}");
                interstitialAd.ShowAd( placement);
                isAdShowing = true;
            }
            catch (Exception e)
            {
                LogError($"显示插屏广告失败: {e.Message}");
                OnInterstitialAdFailed?.Invoke(e.Message);
            }
        }
        
        /// <summary>
        /// 检查是否可以显示插屏广告
        /// </summary>
        /// <returns>是否可以显示</returns>
        public bool CanShowAd()
        {
            // 检查广告是否已加载
            if (!isAdLoaded)
            {
                Log("插屏广告未加载");
                return false;
            }
            
            // 检查是否正在显示广告
            if (isAdShowing)
            {
                Log("插屏广告正在显示中");
                return false;
            }
            
            // 检查冷却时间
            if (Time.time - lastAdShowTime < adConfig.InterstitialCooldownTime)
            {
                Log($"插屏广告冷却中，剩余时间: {adConfig.InterstitialCooldownTime - (Time.time - lastAdShowTime):F1}秒");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查是否应该显示插屏广告（基于关卡数）
        /// </summary>
        /// <returns>是否应该显示</returns>
        public bool ShouldShowAd()
        {
            return CanShowAd() && completedLevels > 0 && completedLevels % adConfig.LevelsBetweenInterstitials == 0;
        }
        
        /// <summary>
        /// 记录关卡完成
        /// </summary>
        public void OnLevelCompleted()
        {
            completedLevels++;
            Log($"关卡完成计数: {completedLevels}");
            
            // 检查是否应该显示插屏广告
            if (ShouldShowAd())
            {
                // 延迟显示广告，确保关卡完成动画播放完毕
                StartCoroutine(DelayedShowAd());
            }
        }
        
        /// <summary>
        /// 延迟显示广告
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator DelayedShowAd()
        {
            // 等待1秒，确保关卡完成动画播放完毕
            yield return new WaitForSeconds(1f);
            
            if (CanShowAd())
            {
                ShowAd("Level_Complete");
            }
        }
        
        /// <summary>
        /// 获取状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public string GetStatusInfo()
        {
            return $"已加载: {isAdLoaded}, 显示中: {isAdShowing}, 完成关卡: {completedLevels}, 冷却剩余: {Mathf.Max(0, adConfig.InterstitialCooldownTime - (Time.time - lastAdShowTime)):F1}秒";
        }
        
        #region LevelPlay事件回调
        
        /// <summary>
        /// 广告加载成功回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void OnAdLoaded(LevelPlayAdInfo adInfo)
        {
            isAdLoaded = true;
            isLoading = false;
            nextAllowedLoadTime = Time.unscaledTime;
            if (retryCoroutine != null)
            {
                StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }
            Log($"插屏广告加载成功: {adInfo.AdUnitId}");
        }
        
        /// <summary>
        /// 广告加载失败回调
        /// </summary>
        /// <param name="error">错误信息</param>
        private void OnAdLoadFailed(LevelPlayAdError error)
        {
            isAdLoaded = false;
            string msg = error != null ? error.ErrorMessage : "unknown";
            LogError($"插屏广告加载失败: {msg}");

            if (!string.IsNullOrEmpty(msg) && msg.IndexOf("Load is already called", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                isLoading = true;
                return;
            }
            isLoading = false;
            
            // 延迟重试加载
            ScheduleRetry();
        }
        
        /// <summary>
        /// 广告显示成功回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void OnAdDisplayed(LevelPlayAdInfo adInfo)
        {
            Log($"插屏广告显示成功: {adInfo.AdUnitId}");
            OnInterstitialAdShown?.Invoke();
        }
        
        /// <summary>
        /// 广告显示失败回调
        /// </summary>
        /// <param name="error">错误信息</param>
        private void OnAdDisplayFailed(LevelPlayAdDisplayInfoError error)
        {
            isAdShowing = false;
            LogError($"插屏广告显示失败: {error}");
            OnInterstitialAdFailed?.Invoke(error.ToString());
            
            // 重新加载广告
            LoadAd();
        }
        
        /// <summary>
        /// 广告点击回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void OnAdClicked(LevelPlayAdInfo adInfo)
        {
            Log($"插屏广告被点击: {adInfo.AdUnitId}");
        }
        
        /// <summary>
        /// 广告关闭回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void OnAdClosed(LevelPlayAdInfo adInfo)
        {
            isAdShowing = false;
            isAdLoaded = false;
            isLoading = false;
            lastAdShowTime = Time.time;
            
            Log($"插屏广告关闭: {adInfo.AdUnitId}");
            OnInterstitialAdClosed?.Invoke();
            
            // 预加载下一个广告
            LoadAd();
        }
        
        #endregion
        
        /// <summary>
        /// 重试加载广告
        /// </summary>
        /// <returns>协程</returns>
        private void ScheduleRetry()
        {
            nextAllowedLoadTime = Mathf.Max(nextAllowedLoadTime, Time.unscaledTime + 5f);
            if (retryCoroutine != null)
            {
                StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }
            retryCoroutine = StartCoroutine(RetryLoadAd());
        }

        private IEnumerator RetryLoadAd()
        {
            float delay = Mathf.Max(0f, nextAllowedLoadTime - Time.unscaledTime);
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            retryCoroutine = null;
            LoadAd();
        }
        
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="message">日志信息</param>
        private void Log(string message)
        {
            if (adConfig != null && adConfig.EnableAdLogging)
            {
                Debug.Log($"[InterstitialAdHandler] {message}");
            }
        }
        
        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误信息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[InterstitialAdHandler] {message}");
        }
        
        private void OnDestroy()
        {
            UnregisterLevelPlayEvents();
            if (retryCoroutine != null)
            {
                StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }
        }
    }
}
