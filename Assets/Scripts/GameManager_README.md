# GameManager 使用说明

## 概述

GameManager是拼图游戏的核心管理类，负责控制整个游戏的流程，包括场景管理、状态管理、事件系统等。

## 主要功能

### 1. 单例模式
- GameManager采用单例模式，确保全局只有一个实例
- 自动设置为DontDestroyOnLoad，在场景切换时保持存在

### 2. 游戏状态管理
游戏包含以下状态：
- `MainMenu` - 主菜单
- `Gallery` - 图片库
- `DifficultySelection` - 难度选择
- `Playing` - 游戏进行中
- `Victory` - 胜利界面
- `Paused` - 暂停状态

### 3. 场景管理
- `LoadMainScene()` - 加载主场景
- `LoadGameScene()` - 加载游戏场景
- `LoadScene(sceneName, onComplete)` - 通用场景加载方法

### 4. 游戏流程控制
- `StartNewGame(image, difficulty, showBackground)` - 开始新游戏
- `CompleteGame(completionTime)` - 完成游戏
- `PauseGame()` - 暂停游戏
- `ResumeGame()` - 恢复游戏
- `ReturnToGallery()` - 返回图片库

### 5. 事件系统
提供以下事件：
- `OnGameStateChanged` - 游戏状态改变
- `OnGameStarted` - 游戏开始
- `OnGameCompleted` - 游戏完成
- `OnGamePaused` - 游戏暂停
- `OnGameResumed` - 游戏恢复

## 游戏数据结构

```csharp
[System.Serializable]
public class GameData
{
    public Sprite selectedImage;    // 选中的图片
    public int difficulty = 4;      // 难度等级
    public bool showBackground = true; // 是否显示背景
    public float completionTime;    // 完成时间
    public int starRating;         // 星级评分
    public string imageName;       // 图片名称
}
```

## 使用示例

### 1. 开始游戏
```csharp
// 在DifficultyPage中
GameManager.Instance.StartNewGame(selectedImage, difficulty, showBackground);
```

### 2. 完成游戏
```csharp
// 在GameplayPage中
GameManager.Instance.CompleteGame(completionTime);
```

### 3. 订阅事件
```csharp
private void Start()
{
    GameManager.OnGameStateChanged += OnGameStateChanged;
    GameManager.OnGameCompleted += OnGameCompleted;
}

private void OnDestroy()
{
    GameManager.OnGameStateChanged -= OnGameStateChanged;
    GameManager.OnGameCompleted -= OnGameCompleted;
}
```

### 4. 状态管理
```csharp
// 改变游戏状态
GameManager.Instance.ChangeGameState(GameManager.GameState.Gallery);

// 获取当前状态
var currentState = GameManager.Instance.CurrentState;
```

## 与其他系统的集成

### UIManager集成
- UIManager订阅GameManager的事件来响应状态变化
- 页面跳转通过GameManager的状态管理来协调

### 场景架构
- **Main场景**: 包含UIManager、Gallery、Difficulty、Victory页面
- **Game场景**: 包含GameplayPage、拼图逻辑、游戏对象

## 调试功能

### GameManagerTest脚本
提供了完整的测试功能：
- 按键1-5：测试各种游戏流程
- 按键G/D/P/V：测试状态切换
- GUI显示当前状态和游戏数据

### 调试模式
在GameManager中设置`debugMode = true`可以启用详细的调试日志。

## 最佳实践

1. **事件订阅**: 始终在OnDestroy中取消事件订阅，避免内存泄漏
2. **状态检查**: 在调用GameManager方法前检查Instance是否存在
3. **场景切换**: 使用GameManager的场景管理方法，而不是直接调用SceneManager
4. **数据持久化**: 游戏数据通过GameManager在场景间传递，无需额外的持久化

## 扩展建议

1. **保存系统**: 可以扩展GameData来支持游戏进度保存
2. **音效管理**: 可以在GameManager中集成音效管理
3. **设置系统**: 可以添加游戏设置的管理功能
4. **统计系统**: 可以添加游戏统计和成就系统

## 注意事项

1. GameManager必须在场景中存在才能正常工作
2. 确保场景名称常量与实际场景名称一致
3. 在构建时确保所有场景都添加到Build Settings中
4. 事件系统是静态的，注意内存管理