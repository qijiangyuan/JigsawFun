# Unity LevelPlay 广告系统集成指南

## 概述

本项目已成功集成Unity LevelPlay广告系统，包括插屏广告、激励视频广告和Banner广告。系统设计遵循最佳用户体验实践，确保广告展示时机合理且不影响游戏流程。

## 系统架构

### 核心组件

1. **AdManager** - 广告系统核心管理器
   - 负责初始化Unity LevelPlay SDK
   - 管理所有广告处理器
   - 提供统一的广告接口

2. **InterstitialAdHandler** - 插屏广告处理器
   - 控制插屏广告显示频率（每2-3关显示一次）
   - 确保只在关卡完成后显示
   - 防止在游戏进行中打断用户

3. **RewardedAdHandler** - 激励视频广告处理器
   - 处理激励视频广告的加载和显示
   - 管理观看完成后的奖励发放
   - 与提示系统集成

4. **BannerAdHandler** - Banner广告处理器
   - 管理Banner广告的显示和隐藏
   - 控制广告位置（拼图界面底部）
   - 避免与游戏UI冲突

5. **HintManager** - 提示管理器
   - 管理免费提示次数
   - 集成激励视频奖励机制
   - 处理提示冷却时间

6. **AdConfig** - 广告配置类
   - 集中管理广告相关参数
   - 便于调整广告策略

## 广告展示策略

### 插屏广告 (Interstitial Ads)

**触发时机：**
- ✅ 完成关卡后，在过关动画结束后展示
- ✅ 每完成2-3关展示一次（可配置）
- ❌ 严禁在拼图进行过程中展示

**实现位置：**
- `PuzzleGameManager.OnGameCompleted()` - 关卡完成处理
- `GameplayPage.OnPuzzleCompleted()` - UI层面的完成处理

### 激励视频广告 (Rewarded Ads)

**触发条件：**
- 当用户免费提示次数用尽时
- 用户主动选择观看广告获得提示

**奖励机制：**
- 观看完整视频后获得额外提示次数
- 支持冷却时间机制

**实现位置：**
- `HintManager` - 提示系统核心逻辑
- `GameplayPage` - UI交互处理

### Banner广告 (Banner Ads)

**展示位置：**
- 拼图界面底部区域
- 不遮挡游戏核心区域

**显示时机：**
- 进入游戏界面时显示
- 离开游戏界面时隐藏

## 集成点说明

### 1. 游戏流程集成

```csharp
// PuzzleGameManager.cs
public void OnGameCompleted()
{
    completedLevels++;
    SaveLevelProgress();
    
    // 触发插屏广告逻辑
    TriggerInterstitialAd();
}
```

### 2. UI界面集成

```csharp
// GameplayPage.cs
private void OnPuzzleCompleted()
{
    // 游戏完成处理
    GameManager.Instance.CompleteGame(currentTime);
    
    // 通知PuzzleGameManager
    if (PuzzleGameManager.Instance != null)
    {
        PuzzleGameManager.Instance.OnGameCompleted();
    }
}
```

### 3. 提示系统集成

```csharp
// GameplayPage.cs
private void OnHintButtonClicked()
{
    if (hintManager.CanUseHint())
    {
        hintManager.UseHint();
        // 执行提示逻辑
    }
    else
    {
        ShowWatchAdOption();
    }
}
```

## 测试系统

### 1. AdSystemTester - 功能测试器

**功能：**
- 测试各类广告的基本功能
- 验证AdManager初始化
- 模拟用户操作流程

**使用方法：**
```csharp
// 在场景中添加AdSystemTester组件
// 通过UI按钮或代码调用测试方法
AdSystemTester tester = FindObjectOfType<AdSystemTester>();
tester.TestInterstitialAd();
tester.TestRewardedAd();
```

### 2. AdSystemIntegrationTest - 集成测试

**功能：**
- 验证广告系统与游戏流程的集成
- 检查各组件的初始化状态
- 自动化测试流程

**使用方法：**
```csharp
// 自动运行测试
AdSystemIntegrationTest integrationTest = FindObjectOfType<AdSystemIntegrationTest>();
StartCoroutine(integrationTest.RunIntegrationTests());
```

### 3. UserExperienceValidator - 用户体验验证

**功能：**
- 验证广告展示时机的合理性
- 检查广告频率控制
- 评估用户体验质量

**使用方法：**
```csharp
// 验证用户体验
UserExperienceValidator validator = FindObjectOfType<UserExperienceValidator>();
StartCoroutine(validator.ValidateUserExperience());
```

## 配置说明

### AdConfig 参数配置

```csharp
public class AdConfig
{
    // 插屏广告配置
    public int InterstitialFrequency = 3;        // 每N关显示一次
    public float InterstitialCooldown = 30f;     // 冷却时间（秒）
    
    // 激励视频配置
    public int RewardedHintCount = 2;            // 观看广告获得的提示数
    
    // Banner广告配置
    public bool EnableBannerAd = true;           // 是否启用Banner
    public BannerPosition BannerPosition = BannerPosition.Bottom;
    
    // 提示系统配置
    public int MaxFreeHints = 3;                 // 最大免费提示数
    public float HintCooldownDuration = 180f;    // 提示冷却时间
}
```

## 最佳实践

### 1. 广告展示时机

- ✅ **正确时机：** 关卡完成后、菜单界面、暂停界面
- ❌ **错误时机：** 游戏进行中、用户操作时、加载过程中

### 2. 频率控制

- 插屏广告：每2-3关显示一次
- 避免连续显示多个广告
- 考虑用户的游戏时长

### 3. 用户体验

- 提供明确的广告关闭按钮
- 确保广告加载不影响游戏性能
- 处理广告加载失败的情况

### 4. 错误处理

```csharp
// 示例：广告加载失败处理
public void OnAdLoadFailed(string error)
{
    Debug.LogWarning($"广告加载失败: {error}");
    // 继续游戏流程，不阻塞用户
    ContinueGameFlow();
}
```

## 调试和监控

### 1. 日志系统

所有广告相关操作都有详细的日志记录：

```csharp
Debug.Log("[AdManager] 插屏广告显示成功");
Debug.LogWarning("[AdManager] 激励视频未准备好");
Debug.LogError("[AdManager] Banner广告加载失败");
```

### 2. 性能监控

- 监控广告加载时间
- 检查内存使用情况
- 记录广告显示成功率

### 3. 用户行为分析

- 记录广告观看完成率
- 统计用户跳过广告的频率
- 分析广告对用户留存的影响

## 常见问题解决

### 1. 广告不显示

**可能原因：**
- Unity LevelPlay SDK未正确初始化
- 广告ID配置错误
- 网络连接问题

**解决方法：**
```csharp
// 检查AdManager初始化状态
if (AdManager.Instance == null)
{
    Debug.LogError("AdManager未初始化");
}

// 检查广告准备状态
if (!AdManager.Instance.IsInterstitialAdReady())
{
    Debug.LogWarning("插屏广告未准备好");
}
```

### 2. 广告频率过高

**解决方法：**
- 调整 `AdConfig.InterstitialFrequency` 参数
- 增加广告冷却时间
- 检查关卡计数逻辑

### 3. 提示系统异常

**解决方法：**
- 检查 `HintManager` 初始化
- 验证激励视频回调处理
- 确认数据持久化正常

## 部署注意事项

### 1. Unity LevelPlay 配置

- 确保在Unity Dashboard中正确配置广告单元ID
- 设置正确的测试设备ID
- 配置适当的广告填充率

### 2. 平台特定设置

- **Android：** 配置网络权限和广告权限
- **iOS：** 设置App Transport Security
- **通用：** 处理GDPR和隐私政策

### 3. 发布前检查

- [ ] 所有测试用例通过
- [ ] 用户体验验证通过
- [ ] 广告展示时机正确
- [ ] 错误处理机制完善
- [ ] 性能表现良好

## 联系和支持

如有问题或需要技术支持，请参考：

- Unity LevelPlay 官方文档
- Unity Ads 开发者指南
- 项目内的测试脚本和示例代码

---

**版本：** 1.0.0  
**最后更新：** 2024年1月  
**兼容性：** Unity 2022.3+ / Unity LevelPlay SDK