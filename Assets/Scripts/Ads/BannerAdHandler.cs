using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace JigsawFun.Ads
{
    /// <summary>
    /// Banner广告处理器
    /// 负责在拼图界面底部显示Banner广告
    /// </summary>
    public class BannerAdHandler : MonoBehaviour
    {
        [Header("Banner广告配置")]
        [SerializeField] private string bannerAdUnitId;
        [SerializeField] private LevelPlayBannerPosition bannerPosition = LevelPlayBannerPosition.BottomCenter;
        [SerializeField] private bool autoShow = true;

        private bool isInitialized = false;
        private bool isBannerLoaded = false;
        private bool isBannerVisible = false;
        private bool pendingShow = false;

        // Banner广告事件
        public event Action OnBannerLoaded;
        public event Action<LevelPlayAdError> OnBannerLoadFailed;
        public event Action OnBannerClicked;
        public event Action OnBannerDisplayed;
        public event Action<LevelPlayAdDisplayInfoError> OnBannerDisplayFailed;
        public event Action<bool> OnBannerVisibilityChanged;

        LevelPlayBannerAd bannerAds;

        /// <summary>
        /// 初始化Banner广告处理器
        /// </summary>
        /// <param name="config">广告配置</param>
        public void Initialize(AdConfig config)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[BannerAdHandler] 已经初始化过了");
                return;
            }

            bannerAdUnitId = config.BannerAdUnitId;

            bannerAds = new LevelPlayBannerAd(config.BannerAdUnitId);

            // 注册Banner广告事件
            RegisterBannerEvents();

            isInitialized = true;
            Debug.Log("[BannerAdHandler] 初始化完成");
        }

        /// <summary>
        /// 注册Banner广告事件监听
        /// </summary>
        private void RegisterBannerEvents()
        {
            bannerAds.OnAdLoaded += HandleBannerLoaded;
            bannerAds.OnAdLoadFailed += HandleBannerLoadFailed;
            bannerAds.OnAdClicked += HandleBannerClicked;
            bannerAds.OnAdDisplayed += HandleBannerDisplayed;
            bannerAds.OnAdDisplayFailed += HandleBannerDisplayFailed;
        }

        /// <summary>
        /// 注销Banner广告事件监听
        /// </summary>
        private void UnregisterBannerEvents()
        {
            bannerAds.OnAdLoaded -= HandleBannerLoaded;
            bannerAds.OnAdLoadFailed -= HandleBannerLoadFailed;
            bannerAds.OnAdClicked -= HandleBannerClicked;
            bannerAds.OnAdDisplayed -= HandleBannerDisplayed;
            bannerAds.OnAdDisplayFailed -= HandleBannerDisplayFailed;
        }

        /// <summary>
        /// 加载Banner广告
        /// </summary>
        public void LoadBanner()
        {
            if (!isInitialized)
            {
                Debug.LogError("[BannerAdHandler] 尚未初始化，无法加载Banner广告");
                return;
            }

            if (string.IsNullOrEmpty(bannerAdUnitId))
            {
                Debug.LogError("[BannerAdHandler] Banner广告单元ID为空");
                return;
            }

            if (isBannerLoaded)
            {
                Debug.Log("[BannerAdHandler] Banner广告已加载，跳过重复加载");
                return;
            }

            Debug.Log("[BannerAdHandler] 开始加载Banner广告");
            bannerAds.LoadAd();
        }

        /// <summary>
        /// 显示Banner广告
        /// </summary>
        public void ShowBanner()
        {
            Debug.Log($"[BannerAdHandler] ShowBanner called. isBannerLoaded={isBannerLoaded}, isBannerVisible={isBannerVisible}, pendingShow={pendingShow}");
            if (!isBannerLoaded)
            {
                Debug.Log("[BannerAdHandler] 尝试显示Banner，但广告未加载，等待加载完成后自动显示，立即发起一次加载");
                pendingShow = true;
                LoadBanner();
                return;
            }

            if (isBannerVisible)
            {
                return;
            }

            Debug.Log("[BannerAdHandler] 显示Banner广告");
            bannerAds.ShowAd();
        }

        /// <summary>
        /// 隐藏Banner广告
        /// </summary>
        public void HideBanner()
        {
            pendingShow = false;
            if (bannerAds != null)
            {
                if (isBannerVisible)
                {
                    Debug.Log("[BannerAdHandler] 隐藏Banner广告");
                }
                bannerAds.HideAd();
            }
            isBannerVisible = false;
            OnBannerVisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// 销毁Banner广告
        /// </summary>
        public void DestroyBanner()
        {
            if (isBannerVisible)
            {
                HideBanner();
            }

            Debug.Log("[BannerAdHandler] 销毁Banner广告");
            bannerAds.DestroyAd();
            isBannerLoaded = false;
            isBannerVisible = false;
            OnBannerVisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// 检查Banner广告是否已加载
        /// </summary>
        /// <returns>是否已加载</returns>
        public bool IsBannerLoaded()
        {
            return isBannerLoaded;
        }

        /// <summary>
        /// 检查Banner广告是否正在显示
        /// </summary>
        /// <returns>是否正在显示</returns>
        public bool IsBannerVisible()
        {
            return isBannerVisible;
        }

        #region Banner广告事件回调

        /// <summary>
        /// Banner广告加载成功回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void HandleBannerLoaded(LevelPlayAdInfo adInfo)
        {
            if (adInfo.AdUnitId != bannerAdUnitId) return;

            Debug.Log($"[BannerAdHandler] Banner广告加载成功: {adInfo.AdUnitId}");
            isBannerLoaded = true;

            OnBannerLoaded?.Invoke();

            if (pendingShow)
            {
                pendingShow = false;
                if (!isBannerVisible)
                {
                    ShowBanner();
                }
            }
        }

        /// <summary>
        /// Banner广告加载失败回调
        /// </summary>
        /// <param name="error">错误信息</param>
        private void HandleBannerLoadFailed(LevelPlayAdError error)
        {
            Debug.LogError($"[BannerAdHandler] Banner广告加载失败: {error.ErrorMessage}");
            isBannerLoaded = false;

            OnBannerLoadFailed?.Invoke(error);
            
            // 延迟重试加载
            Invoke(nameof(LoadBanner), 15f);
        }

        /// <summary>
        /// Banner广告点击回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void HandleBannerClicked(LevelPlayAdInfo adInfo)
        {
            if (adInfo.AdUnitId != bannerAdUnitId) return;

            Debug.Log($"[BannerAdHandler] Banner广告被点击: {adInfo.AdUnitId}");
            OnBannerClicked?.Invoke();
        }

        /// <summary>
        /// Banner广告显示成功回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        private void HandleBannerDisplayed(LevelPlayAdInfo adInfo)
        {
            if (adInfo.AdUnitId != bannerAdUnitId) return;

            Debug.Log($"[BannerAdHandler] Banner广告显示成功: {adInfo.AdUnitId}");
            isBannerVisible = true;

            OnBannerDisplayed?.Invoke();
            OnBannerVisibilityChanged?.Invoke(true);
        }

        /// <summary>
        /// Banner广告显示失败回调
        /// </summary>
        /// <param name="adInfo">广告信息</param>
        /// <param name="error">错误信息</param>
        private void HandleBannerDisplayFailed(LevelPlayAdDisplayInfoError error)
        {
            // if (adInfo.AdUnitId != bannerAdUnitId) return;

            Debug.LogError($"[BannerAdHandler] Banner广告显示失败: {error.LevelPlayError}");
            isBannerVisible = false;

            OnBannerDisplayFailed?.Invoke(error);
            OnBannerVisibilityChanged?.Invoke(false);
        }

        #endregion

        /// <summary>
        /// 组件销毁时清理资源
        /// </summary>
        private void OnDestroy()
        {
            if (isInitialized)
            {
                UnregisterBannerEvents();
                DestroyBanner();
            }
        }
    }
}
