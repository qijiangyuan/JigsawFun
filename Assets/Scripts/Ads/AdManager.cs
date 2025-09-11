using UnityEngine;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using System;
using System.Threading.Tasks;

namespace JigsawFun.Ads
{
    /// <summary>
    /// 广告管理器，负责Unity LevelPlay SDK的初始化和广告系统的统一管理
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private AdConfig adConfig;
        
        [Header("广告处理器")]
        [SerializeField] private InterstitialAdHandler interstitialHandler;
        [SerializeField] private RewardedAdHandler rewardedHandler;
        [SerializeField] private BannerAdHandler bannerHandler;
        
        // 单例实例
        public static AdManager Instance { get; private set; }
        
        // 初始化状态
        public bool IsInitialized { get; private set; }
        
        // 事件
        public static event Action OnAdManagerInitialized;
        public static event Action<string> OnAdManagerInitializationFailed;
        
        /// <summary>
        /// 获取广告配置
        /// </summary>
        public AdConfig Config => adConfig;
        
        /// <summary>
        /// 获取插屏广告处理器
        /// </summary>
        public InterstitialAdHandler InterstitialHandler => interstitialHandler;
        
        /// <summary>
        /// 获取激励视频广告处理器
        /// </summary>
        public RewardedAdHandler RewardedHandler => rewardedHandler;
        
        /// <summary>
        /// 获取Banner广告处理器
        /// </summary>
        public BannerAdHandler BannerHandler => bannerHandler;
        
        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAdSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 初始化广告系统
        /// </summary>
        private async void InitializeAdSystem()
        {
            try
            {
                if (adConfig == null)
                {
                    LogError("AdConfig未设置，无法初始化广告系统");
                    OnAdManagerInitializationFailed?.Invoke("AdConfig未设置");
                    return;
                }
                
                Log("开始初始化Unity Services...");
                // To enable the test suite in your app, call the following before initializing the SDK:
                LevelPlay.SetMetaData("is_test_suite", "enable");
                // 初始化Unity Services
                await UnityServices.InitializeAsync();
                
                Log("Unity Services初始化完成，开始初始化LevelPlay...");
                
                // 初始化LevelPlay
                string appKey = adConfig.GetAppKey();
                if (string.IsNullOrEmpty(appKey) || appKey.Contains("YOUR_"))
                {
                    LogError("应用密钥未正确配置，请在AdConfig中设置正确的App Key");
                    OnAdManagerInitializationFailed?.Invoke("应用密钥未配置");
                    return;
                }

                // 设置LevelPlay配置
                //LevelPlayConfiguration levelPlayConfig = new LevelPlayConfiguration.Builder(appKey)
                //    .WithLogLevel(adConfig.EnableAdLogging ? LevelPlayLogLevel.Debug : LevelPlayLogLevel.Error)
                //    .Build();

                //await LevelPlay.InitializeAsync(levelPlayConfig);

                //After successfully initializing LevelPlay, launch the test suite by calling the following method:
                LevelPlay.LaunchTestSuite();

                Log("LevelPlay初始化完成，开始初始化广告处理器...");
                
                // 初始化广告处理器
                InitializeAdHandlers();
                
                IsInitialized = true;
                Log("广告系统初始化完成");
                OnAdManagerInitialized?.Invoke();
            }
            catch (Exception e)
            {
                LogError($"广告系统初始化失败: {e.Message}");
                OnAdManagerInitializationFailed?.Invoke(e.Message);
            }
        }
        
        /// <summary>
        /// 初始化广告处理器
        /// </summary>
        private void InitializeAdHandlers()
        {
            // 获取或创建广告处理器组件
            if (interstitialHandler == null)
                interstitialHandler = GetComponent<InterstitialAdHandler>() ?? gameObject.AddComponent<InterstitialAdHandler>();
            
            if (rewardedHandler == null)
                rewardedHandler = GetComponent<RewardedAdHandler>() ?? gameObject.AddComponent<RewardedAdHandler>();
            
            if (bannerHandler == null && adConfig.EnableBannerAds)
                bannerHandler = GetComponent<BannerAdHandler>() ?? gameObject.AddComponent<BannerAdHandler>();
            
            // 初始化各个处理器
            interstitialHandler?.Initialize(adConfig);
            rewardedHandler?.Initialize(adConfig);
            bannerHandler?.Initialize(adConfig);
            
            Log("广告处理器初始化完成");
        }
        
        /// <summary>
        /// 显示插屏广告
        /// </summary>
        /// <param name="placement">广告位置标识</param>
        public void ShowInterstitial(string placement = "level_complete")
        {
            if (!IsInitialized)
            {
                LogError("广告系统未初始化，无法显示插屏广告");
                return;
            }
            
            interstitialHandler?.ShowAd(placement);
        }
        
        /// <summary>
        /// 显示激励视频广告
        /// </summary>
        /// <param name="placement">广告位置标识</param>
        /// <param name="onRewarded">奖励回调</param>
        public void ShowRewardedVideo(string placement = "hint_reward", Action onRewarded = null)
        {
            if (!IsInitialized)
            {
                LogError("广告系统未初始化，无法显示激励视频");
                return;
            }
            
            rewardedHandler?.ShowAd(placement, onRewarded);
        }
        
        /// <summary>
        /// 显示Banner广告
        /// </summary>
        public void ShowBanner()
        {
            if (!IsInitialized || !adConfig.EnableBannerAds)
            {
                LogError("广告系统未初始化或Banner广告未启用");
                return;
            }
            
            bannerHandler?.ShowBanner();
        }
        
        /// <summary>
        /// 隐藏Banner广告
        /// </summary>
        public void HideBanner()
        {
            bannerHandler?.HideBanner();
        }
        
        /// <summary>
        /// 检查插屏广告是否可以显示
        /// </summary>
        /// <returns>是否可以显示</returns>
        public bool CanShowInterstitial()
        {
            return IsInitialized && interstitialHandler != null && interstitialHandler.CanShowAd();
        }
        
        /// <summary>
        /// 检查激励视频是否可以显示
        /// </summary>
        /// <returns>是否可以显示</returns>
        public bool CanShowRewardedVideo()
        {
            return IsInitialized && rewardedHandler != null && rewardedHandler.CanShowAd();
        }
        
        /// <summary>
        /// 记录关卡完成，用于插屏广告频率控制
        /// </summary>
        public void OnLevelCompleted()
        {
            interstitialHandler?.OnLevelCompleted();
        }
        
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="message">日志信息</param>
        private void Log(string message)
        {
            if (adConfig != null && adConfig.EnableAdLogging)
            {
                Debug.Log($"[AdManager] {message}");
            }
        }
        
        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误信息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[AdManager] {message}");
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // 应用暂停时处理广告状态
            if (pauseStatus)
            {
                //bannerHandler?.OnApplicationPause();
            }
            else
            {
                //bannerHandler?.OnApplicationResume();
            }
        }
    }
}