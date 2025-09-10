using UnityEngine;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

namespace JigsawFun.Ads
{
    /// <summary>
    /// 激励视频广告处理器，负责激励视频广告的加载、显示和奖励发放
    /// </summary>
    public class RewardedAdHandler : MonoBehaviour
    {
        private AdConfig adConfig;
        private string adUnitId;

        // 广告状态
        private bool isAdLoaded = false;
        private bool isAdShowing = false;

        // 奖励回调
        private Action currentRewardCallback;

        // 事件
        public event Action OnRewardedAdShown;
        public event Action OnRewardedAdClosed;
        public event Action OnRewardedAdRewarded;
        public event Action<string> OnRewardedAdFailed;

        LevelPlayRewardedAd rewardedVideoAd;

        /// <summary>
        /// 初始化激励视频广告处理器
        /// </summary>
        /// <param name="config">广告配置</param>
        public void Initialize(AdConfig config)
        {
            adConfig = config;
            adUnitId = config.RewardedAdUnitId;

            rewardedVideoAd = new LevelPlayRewardedAd(config.RewardedAdUnitId);

            // 注册LevelPlay事件
            RegisterLevelPlayEvents();

            // 预加载广告
            LoadAd();

            Log("激励视频广告处理器初始化完成");
        }

        /// <summary>
        /// 注册LevelPlay事件回调
        /// </summary>
        private void RegisterLevelPlayEvents()
        {
            rewardedVideoAd.OnAdLoaded += OnAdLoaded;
            rewardedVideoAd.OnAdLoadFailed += OnAdLoadFailed;
            rewardedVideoAd.OnAdDisplayed += OnAdDisplayed;
            rewardedVideoAd.OnAdDisplayFailed += OnAdDisplayFailed;
            rewardedVideoAd.OnAdRewarded += OnAdRewarded;
            rewardedVideoAd.OnAdClicked += OnAdClicked;
            rewardedVideoAd.OnAdClosed += OnAdClosed;
        }

        /// <summary>
        /// 注销LevelPlay事件回调
        /// </summary>
        private void UnregisterLevelPlayEvents()
        {
            rewardedVideoAd.OnAdLoaded -= OnAdLoaded;
            rewardedVideoAd.OnAdLoadFailed -= OnAdLoadFailed;
            rewardedVideoAd.OnAdDisplayed -= OnAdDisplayed;
            rewardedVideoAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            rewardedVideoAd.OnAdRewarded -= OnAdRewarded;
            rewardedVideoAd.OnAdClicked -= OnAdClicked;
            rewardedVideoAd.OnAdClosed -= OnAdClosed;
        }

        /// <summary>
        /// 加载激励视频广告
        /// </summary>
        public void LoadAd()
        {
            if (isAdLoaded)
            {
                Log("激励视频广告已加载，无需重复加载");
                return;
            }

            try
            {
                Log("开始加载激励视频广告...");
                rewardedVideoAd.LoadAd();
            }
            catch (Exception e)
            {
                LogError($"加载激励视频广告失败: {e.Message}");
            }
        }

        /// <summary>
        /// 显示激励视频广告
        /// </summary>
        /// <param name="placement">广告位置标识</param>
        /// <param name="onRewarded">奖励回调</param>
        public void ShowAd(string placement = "hint_reward", Action onRewarded = null)
        {
            if (!CanShowAd())
            {
                Log("当前无法显示激励视频广告");
                OnRewardedAdFailed?.Invoke("广告未准备好");
                return;
            }

            try
            {
                Log($"显示激励视频广告，位置: {placement}");
                currentRewardCallback = onRewarded;
                rewardedVideoAd.ShowAd(placement);
                isAdShowing = true;
            }
            catch (Exception e)
            {
                LogError($"显示激励视频广告失败: {e.Message}");
                OnRewardedAdFailed?.Invoke(e.Message);
                currentRewardCallback = null;
            }
        }

        /// <summary>
        /// 检查是否可以显示激励视频广告
        /// </summary>
        /// <returns>是否可以显示</returns>
        public bool CanShowAd()
        {
            // 检查广告是否已加载
            if (!isAdLoaded)
            {
                Log("激励视频广告未加载");
                return false;
            }

            // 检查是否正在显示广告
            if (isAdShowing)
            {
                Log("激励视频广告正在显示中");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查激励视频广告是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        public bool IsAdAvailable()
        {
            try
            {
                return rewardedVideoAd.IsAdReady();
            }
            catch (Exception e)
            {
                LogError($"检查激励视频广告可用性失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public string GetStatusInfo()
        {
            return $"已加载: {isAdLoaded}, 显示中: {isAdShowing}, SDK可用: {IsAdAvailable()}";
        }

        #region LevelPlay事件回调

        /// <summary>
        /// 广告加载成功回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void OnAdLoaded(LevelPlayAdInfo adInfo)
        {
            isAdLoaded = true;
            Log($"激励视频广告加载成功: {adInfo.AdUnitId}");
        }

        /// <summary>
        /// 广告加载失败回调
        /// </summary>
        /// <param name="error">错误信息</param>
        private void OnAdLoadFailed(LevelPlayAdError error)
        {
            isAdLoaded = false;
            LogError($"激励视频广告加载失败: {error.ErrorMessage}");

            // 延迟重试加载
            StartCoroutine(RetryLoadAd());
        }

        /// <summary>
        /// 广告显示成功回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void OnAdDisplayed(LevelPlayAdInfo adInfo)
        {
            Log($"激励视频广告显示成功: {adInfo.AdUnitId}");
            OnRewardedAdShown?.Invoke();
        }

        /// <summary>
        /// 广告显示失败回调
        /// </summary>
        /// <param name="error">错误信息</param>
        private void OnAdDisplayFailed(LevelPlayAdDisplayInfoError error)
        {
            isAdShowing = false;
            LogError($"激励视频广告显示失败: {error.LevelPlayError}");
            OnRewardedAdFailed?.Invoke(error.LevelPlayError.ToString());

            // 清除奖励回调
            currentRewardCallback = null;

            // 重新加载广告
            LoadAd();
        }

        /// <summary>
        /// 广告奖励回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        /// <param name="reward">奖励信息</param>
        private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            Log($"激励视频广告奖励发放: {adInfo.AdUnitId}, 奖励: {reward.Name} x{reward.Amount}");

            // 发放奖励
            GrantReward();

            OnRewardedAdRewarded?.Invoke();
        }

        /// <summary>
        /// 广告点击回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void OnAdClicked(LevelPlayAdInfo adInfo)
        {
            Log($"激励视频广告被点击: {adInfo.AdUnitId}");
        }

        /// <summary>
        /// 广告关闭回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void OnAdClosed(LevelPlayAdInfo adInfo)
        {
            isAdShowing = false;
            isAdLoaded = false;

            Log($"激励视频广告关闭: {adInfo.AdUnitId}");
            OnRewardedAdClosed?.Invoke();

            // 清除奖励回调
            currentRewardCallback = null;

            // 预加载下一个广告
            LoadAd();
        }

        #endregion

        /// <summary>
        /// 发放奖励
        /// </summary>
        private void GrantReward()
        {
            try
            {
                // 调用奖励回调
                currentRewardCallback?.Invoke();

                // 通过HintManager发放提示奖励
                if (HintManager.Instance != null)
                {
                    HintManager.Instance.AddRewardedHints(adConfig.RewardHintCount);
                    Log($"发放提示奖励: {adConfig.RewardHintCount}个");
                }
                else
                {
                    LogError("HintManager实例不存在，无法发放提示奖励");
                }
            }
            catch (Exception e)
            {
                LogError($"发放奖励失败: {e.Message}");
            }
        }

        /// <summary>
        /// 重试加载广告
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator RetryLoadAd()
        {
            yield return new WaitForSeconds(5f); // 等待5秒后重试
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
                Debug.Log($"[RewardedAdHandler] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误信息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[RewardedAdHandler] {message}");
        }

        private void OnDestroy()
        {
            UnregisterLevelPlayEvents();
        }
    }
}