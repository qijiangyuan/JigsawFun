# 拼图游戏系统

这是一个完整的Unity拼图游戏系统，支持动态生成拼图块、拖放交互、自动吸附和游戏管理功能。

## 系统组件

### 核心脚本

1. **JigsawGenerator.cs** - 拼图生成器
   - 负责从图片生成拼图块
   - 支持自定义网格大小
   - 根据JSON布局文件生成凹凸形状
   - 自动添加PuzzlePiece组件

2. **PuzzlePiece.cs** - 拼图块控制器
   - 处理拼图块的拖放交互
   - 实现自动吸附功能
   - 管理拼图块状态和位置
   - 提供视觉反馈效果

3. **PuzzleBoard.cs** - 拼图盘管理器
   - 管理拼图区域和打乱区域
   - 控制拼图块的布局和状态
   - 提供重置、打乱、显示答案等功能

4. **PuzzleGameManager.cs** - 游戏管理器
   - 协调各组件之间的通信
   - 管理游戏流程和状态
   - 处理音效播放
   - 提供单例访问接口

5. **PuzzleUI.cs** - 用户界面控制器
   - 管理游戏UI界面
   - 处理按钮事件
   - 显示游戏进度和状态
   - 提供难度调节功能

6. **PuzzleTestScene.cs** - 测试场景脚本
   - 快速设置测试环境
   - 自动创建必要的游戏对象
   - 配置组件引用关系

### 数据结构

- **MaskPieceData** - 拼图块数据（在JigsawMaskGenerator.cs中定义）
- **JigsawLayoutData** - 拼图布局数据（在JigsawMaskGenerator.cs中定义）
- **MaskPiece.EdgeType** - 边缘类型枚举（凹、凸、平）

## 快速开始

### 方法1：使用测试场景脚本（推荐）

1. 在Unity中创建一个空的GameObject
2. 添加`PuzzleTestScene`组件
3. 运行游戏，系统会自动设置所有必要的组件

### 方法2：手动设置

1. **创建拼图生成器**
   ```csharp
   GameObject generatorObj = new GameObject("JigsawGenerator");
   JigsawGenerator generator = generatorObj.AddComponent<JigsawGenerator>();
   generator.gridSize = 4; // 设置网格大小
   generator.piecePrefab = yourPiecePrefab; // 设置拼图块预制体
   ```

2. **创建拼图盘**
   ```csharp
   GameObject boardObj = new GameObject("PuzzleBoard");
   PuzzleBoard board = boardObj.AddComponent<PuzzleBoard>();
   board.jigsawGenerator = generator;
   board.gridSize = 4;
   ```

3. **创建游戏管理器**
   ```csharp
   GameObject managerObj = new GameObject("PuzzleGameManager");
   PuzzleGameManager manager = managerObj.AddComponent<PuzzleGameManager>();
   manager.jigsawGenerator = generator;
   manager.puzzleBoard = board;
   ```

4. **创建UI系统**
   - 创建Canvas和UI元素
   - 添加`PuzzleUI`组件
   - 设置按钮引用和事件

## 使用说明

### 基本功能

- **拖放拼图块**：鼠标点击并拖动拼图块
- **自动吸附**：拼图块靠近正确位置时会自动吸附
- **打乱拼图**：点击"打乱"按钮重新排列拼图块
- **重置拼图**：点击"重置"按钮将拼图块放回正确位置
- **显示答案**：点击"显示答案"按钮查看完整图片
- **调节难度**：使用滑块调节拼图网格大小（2x2到8x8）

### 高级功能

- **音效支持**：拼图块吸附和完成时播放音效
- **进度显示**：实时显示拼图完成进度
- **完成检测**：自动检测拼图是否完成
- **视觉反馈**：拼图块拖动时的高亮和层级变化

## 配置选项

### JigsawGenerator设置
- `gridSize`：网格大小（2-10）
- `piecePrefab`：拼图块预制体
- `imagePath`：源图片路径

### PuzzleBoard设置
- `pieceSpacing`：拼图块间距
- `autoShuffle`：是否自动打乱
- `shuffleAreaSize`：打乱区域大小
- `shuffleForce`：打乱力度

### PuzzlePiece设置
- `snapDistance`：吸附距离
- `dragSensitivity`：拖拽灵敏度
- `highlightColor`：高亮颜色
- `snapForce`：吸附力度

## 扩展功能

### 添加新的拼图形状
1. 修改`MaskPiece.EdgeType`枚举
2. 更新`JigsawGenerator`中的形状生成逻辑
3. 调整`PuzzlePiece`中的吸附检测

### 自定义UI主题
1. 修改`PuzzleUI`中的颜色和样式设置
2. 替换按钮和面板的图片资源
3. 调整布局和动画效果

### 添加新的游戏模式
1. 在`PuzzleGameManager`中添加新的游戏状态
2. 实现对应的游戏逻辑
3. 更新UI界面以支持新模式

## 注意事项

1. **预制体设置**：确保拼图块预制体包含SpriteRenderer和Collider2D组件
2. **图层设置**：建议为拼图块设置专门的图层以优化性能
3. **标签设置**：拼图块必须使用"PuzzlePiece"标签
4. **摄像机设置**：建议使用正交摄像机以获得最佳视觉效果
5. **性能优化**：大网格拼图（8x8以上）可能需要额外的性能优化

## 故障排除

### 常见问题

1. **拼图块无法拖动**
   - 检查是否添加了PuzzlePiece组件
   - 确认Collider2D组件正常工作
   - 检查摄像机设置

2. **拼图块无法吸附**
   - 调整snapDistance参数
   - 检查正确位置是否设置正确
   - 确认吸附逻辑没有被其他代码干扰

3. **UI按钮无响应**
   - 检查EventSystem是否存在
   - 确认按钮事件绑定正确
   - 检查Canvas设置

4. **游戏管理器初始化失败**
   - 确认所有组件引用都已正确设置
   - 检查初始化顺序
   - 查看控制台错误信息

## 版本信息

- Unity版本：2021.3 LTS或更高
- 支持平台：Windows, Mac, Linux, WebGL
- 依赖项：Unity UI系统

## 许可证

本项目采用MIT许可证，可自由使用和修改。