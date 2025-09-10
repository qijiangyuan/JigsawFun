using UnityEngine;

namespace JigsawFun.Ads
{
    /// <summary>
    /// 广告配置类，定义Unity LevelPlay广告系统的相关参数
    /// </summary>
    [CreateAssetMenu(fileName = "AdConfig", menuName = "JigsawFun/Ad Config")]
    public class AdConfig : ScriptableObject
    {
        [Header("广告单元ID配置")]
        [SerializeField] private string androidAppKey = "YOUR_ANDROID_APP_KEY";
        [SerializeField] private string iosAppKey = "YOUR_IOS_APP_KEY";
        
        [Header("插屏广告配置")]
        [SerializeField] private string interstitialAdUnitId = "DefaultInterstitial";
        [SerializeField] private int levelsBetweenInterstitials = 3; // 每3关显示一次插屏广告
        [SerializeField] private float interstitialCooldownTime = 30f; // 插屏广告冷却时间（秒）
        
        [Header("激励视频广告配置")]
        [SerializeField] private string rewardedAdUnitId = "DefaultRewarded";
        [SerializeField] private int rewardHintCount = 3; // 观看激励视频获得的提示次数
        
        [Header("Banner广告配置")]
        [SerializeField] private string bannerAdUnitId = "DefaultBanner";
        [SerializeField] private bool enableBannerAds = true;
        [SerializeField] private float bannerRefreshInterval = 60f; // Banner刷新间隔（秒）
        
        [Header("提示系统配置")]
        [SerializeField] private int dailyFreeHints = 5; // 每日免费提示次数
        [SerializeField] private int maxStoredHints = 10; // 最大存储提示次数
        
        [Header("调试设置")]
        [SerializeField] private bool enableTestMode = false;
        [SerializeField] private bool enableAdLogging = true;
        
        /// <summary>
        /// 获取当前平台的应用密钥
        /// </summary>
        public string GetAppKey()
        {
#if UNITY_ANDROID
            return androidAppKey;
#elif UNITY_IOS
            return iosAppKey;
#else
            return androidAppKey; // 默认使用Android密钥
#endif
        }
        
        /// <summary>
        /// 插屏广告单元ID
        /// </summary>
        public string InterstitialAdUnitId => interstitialAdUnitId;
        
        /// <summary>
        /// 激励视频广告单元ID
        /// </summary>
        public string RewardedAdUnitId => rewardedAdUnitId;
        
        /// <summary>
        /// Banner广告单元ID
        /// </summary>
        public string BannerAdUnitId => bannerAdUnitId;
        
        /// <summary>
        /// 插屏广告显示间隔（关卡数）
        /// </summary>
        public int LevelsBetweenInterstitials => levelsBetweenInterstitials;
        
        /// <summary>
        /// 插屏广告冷却时间
        /// </summary>
        public float InterstitialCooldownTime => interstitialCooldownTime;
        
        /// <summary>
        /// 观看激励视频获得的提示次数
        /// </summary>
        public int RewardHintCount => rewardHintCount;
        
        /// <summary>
        /// 是否启用Banner广告
        /// </summary>
        public bool EnableBannerAds => enableBannerAds;
        
        /// <summary>
        /// Banner广告刷新间隔
        /// </summary>
        public float BannerRefreshInterval => bannerRefreshInterval;
        
        /// <summary>
        /// 每日免费提示次数
        /// </summary>
        public int DailyFreeHints => dailyFreeHints;
        
        /// <summary>
        /// 最大存储提示次数
        /// </summary>
        public int MaxStoredHints => maxStoredHints;
        
        /// <summary>
        /// 是否启用测试模式
        /// </summary>
        public bool EnableTestMode => enableTestMode;
        
        /// <summary>
        /// 是否启用广告日志
        /// </summary>
        public bool EnableAdLogging => enableAdLogging;
    }
}