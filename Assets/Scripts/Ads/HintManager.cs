using UnityEngine;
using System;


namespace JigsawFun.Ads
{
    /// <summary>
    /// 提示管理器
    /// 负责管理免费提示次数和通过观看激励视频获得额外提示的机制
    /// </summary>
    public class HintManager : MonoBehaviour
    {
        public static HintManager Instance { get; private set; }
        [Header("提示系统配置")]
        [SerializeField] private int maxFreeHints = 3;
        [SerializeField] private int rewardedHintsPerAd = 2;
        [SerializeField] private float hintCooldownTime = 5f;

        private int currentFreeHints;
        private int currentRewardedHints;
        private bool isOnCooldown = false;
        private float lastHintTime;

        // 提示系统事件
        public event Action<int> OnFreeHintsChanged;
        public event Action<int> OnRewardedHintsChanged;
        public event Action OnHintUsed;
        public event Action OnNoHintsAvailable;
        public event Action OnCooldownStarted;
        public event Action OnCooldownEnded;

        /// <summary>
        /// 当前可用的免费提示次数
        /// </summary>
        public int CurrentFreeHints => currentFreeHints;

        /// <summary>
        /// 当前可用的奖励提示次数
        /// </summary>
        public int CurrentRewardedHints => currentRewardedHints;

        /// <summary>
        /// 总可用提示次数
        /// </summary>
        public int TotalAvailableHints => currentFreeHints + currentRewardedHints;

        /// <summary>
        /// 是否有可用提示
        /// </summary>
        public bool HasAvailableHints => TotalAvailableHints > 0 && !isOnCooldown;

        /// <summary>
        /// 是否在冷却中
        /// </summary>
        public bool IsOnCooldown => isOnCooldown;

        /// <summary>
        /// 剩余冷却时间
        /// </summary>
        public float RemainingCooldownTime
        {
            get
            {
                if (!isOnCooldown) return 0f;
                return Mathf.Max(0f, hintCooldownTime - (Time.time - lastHintTime));
            }
        }

        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
            }

            // 获取PlayPrefsManager实例
            playPrefsManager = PlayPrefsManager.Instance;

            // 初始化免费提示次数
            currentFreeHints = maxFreeHints;
            currentRewardedHints = 0;

            // 从PlayPrefsManager加载保存的数据
            LoadHintData();
        }

        private void Start()
        {
            // 注册激励视频广告完成事件
            if (AdManager.Instance != null && AdManager.Instance.RewardedHandler != null)
            {
                //AdManager.Instance.RewardedHandler. += HandleRewardEarned;
            }
        }

        private void Update()
        {
            // 检查冷却时间
            if (isOnCooldown && Time.time - lastHintTime >= hintCooldownTime)
            {
                EndCooldown();
            }
        }

        /// <summary>
        /// 使用提示
        /// </summary>
        /// <returns>是否成功使用提示</returns>
        public bool UseHint()
        {
            if (!HasAvailableHints)
            {
                Debug.LogWarning("[HintManager] 没有可用的提示或正在冷却中");
                OnNoHintsAvailable?.Invoke();
                return false;
            }

            // 优先使用奖励提示
            if (currentRewardedHints > 0)
            {
                currentRewardedHints--;
                OnRewardedHintsChanged?.Invoke(currentRewardedHints);
                Debug.Log($"[HintManager] 使用奖励提示，剩余: {currentRewardedHints}");
            }
            else if (currentFreeHints > 0)
            {
                currentFreeHints--;
                OnFreeHintsChanged?.Invoke(currentFreeHints);
                Debug.Log($"[HintManager] 使用免费提示，剩余: {currentFreeHints}");
            }

            // 开始冷却
            StartCooldown();

            // 保存数据
            SaveHintData();

            OnHintUsed?.Invoke();

            // 触发游戏事件
            //EventManager.TriggerEvent("HintUsed");

            return true;
        }

        /// <summary>
        /// 请求观看激励视频获得提示
        /// </summary>
        public void RequestRewardedHints()
        {
            if (AdManager.Instance == null || AdManager.Instance.RewardedHandler == null)
            {
                Debug.LogError("[HintManager] AdManager或RewardedAdHandler未初始化");
                return;
            }

            if (!AdManager.Instance.RewardedHandler.CanShowAd())
            {
                Debug.LogWarning("[HintManager] 激励视频广告尚未准备好");
                return;
            }

            Debug.Log("[HintManager] 请求观看激励视频获得提示");
            AdManager.Instance.RewardedHandler.ShowAd();
        }

        /// <summary>
        /// 重置免费提示次数（新关卡开始时调用）
        /// </summary>
        public void ResetFreeHints()
        {
            currentFreeHints = maxFreeHints;
            OnFreeHintsChanged?.Invoke(currentFreeHints);
            SaveHintData();

            Debug.Log($"[HintManager] 重置免费提示次数: {currentFreeHints}");
        }

        /// <summary>
        /// 添加奖励提示次数
        /// </summary>
        /// <param name="amount">添加的数量</param>
        public void AddRewardedHints(int amount)
        {
            currentRewardedHints += amount;
            OnRewardedHintsChanged?.Invoke(currentRewardedHints);
            SaveHintData();

            Debug.Log($"[HintManager] 添加奖励提示: +{amount}，总计: {currentRewardedHints}");
        }

        /// <summary>
        /// 开始冷却
        /// </summary>
        private void StartCooldown()
        {
            isOnCooldown = true;
            lastHintTime = Time.time;
            OnCooldownStarted?.Invoke();

            Debug.Log($"[HintManager] 开始提示冷却，时长: {hintCooldownTime}秒");
        }

        /// <summary>
        /// 结束冷却
        /// </summary>
        private void EndCooldown()
        {
            isOnCooldown = false;
            OnCooldownEnded?.Invoke();

            Debug.Log("[HintManager] 提示冷却结束");
        }

        /// <summary>
        /// 处理激励视频奖励
        /// </summary>
        private void HandleRewardEarned()
        {
            AddRewardedHints(rewardedHintsPerAd);
            Debug.Log($"[HintManager] 观看激励视频获得 {rewardedHintsPerAd} 个提示");
        }

        /// <summary>
        /// 保存提示数据到PlayerPrefs
        /// </summary>
        private PlayPrefsManager playPrefsManager;

        private void SaveHintData()
        {
            playPrefsManager.SaveHintData(currentFreeHints, currentRewardedHints, lastHintTime, isOnCooldown);
        }

        /// <summary>
        /// 从PlayerPrefs加载提示数据
        /// </summary>
        private void LoadHintData()
        {
            var hintData = playPrefsManager.LoadHintData(maxFreeHints);
            currentFreeHints = hintData.freeHints;
            currentRewardedHints = hintData.rewardedHints;
            lastHintTime = hintData.lastHintTime;
            isOnCooldown = hintData.isOnCooldown;

            // 检查是否需要结束冷却
            if (isOnCooldown && Time.time - lastHintTime >= hintCooldownTime)
            {
                isOnCooldown = false;
            }

            Debug.Log($"[HintManager] 加载提示数据 - 免费: {currentFreeHints}, 奖励: {currentRewardedHints}, 冷却: {isOnCooldown}");
        }

        /// <summary>
        /// 获取提示状态信息（用于UI显示）
        /// </summary>
        /// <returns>提示状态信息</returns>
        public string GetHintStatusText()
        {
            if (isOnCooldown)
            {
                return $"冷却中 ({RemainingCooldownTime:F1}s)";
            }

            if (TotalAvailableHints == 0)
            {
                return "观看广告获得提示";
            }

            string text = "";
            if (currentFreeHints > 0)
            {
                text += $"免费: {currentFreeHints}";
            }

            if (currentRewardedHints > 0)
            {
                if (!string.IsNullOrEmpty(text)) text += " | ";
                text += $"奖励: {currentRewardedHints}";
            }

            return text;
        }

        private void OnDestroy()
        {
            // 注销事件监听
            if (AdManager.Instance != null && AdManager.Instance.RewardedHandler != null)
            {
                AdManager.Instance.RewardedHandler.OnRewardedAdRewarded -= HandleRewardEarned;
            }

            // 保存数据
            SaveHintData();
        }
    }
}