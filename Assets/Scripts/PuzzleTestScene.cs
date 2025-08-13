using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 拼图测试场景脚本
/// 用于快速设置和测试拼图系统
/// </summary>
public class PuzzleTestScene : MonoBehaviour
{
    [Header("预制体引用")]
    public GameObject piecePrefab;
    
    [Header("UI预制体")]
    public GameObject canvasPrefab;
    
    [Header("测试设置")]
    public bool autoSetup = true;
    public int testGridSize = 4;
    public string testImagePath = "Images/test_image";
    
    void Start()
    {
        if (autoSetup)
        {
            SetupTestScene();
        }
    }
    
    /// <summary>
    /// 设置测试场景
    /// </summary>
    public void SetupTestScene()
    {
        Debug.Log("开始设置拼图测试场景...");
        
        // 创建主摄像机（如果不存在）
        SetupCamera();
        
        // 创建拼图生成器
        SetupJigsawGenerator();
        
        // 创建拼图盘
        SetupPuzzleBoard();
        
        // 创建游戏管理器
        SetupGameManager();
        
        // 创建UI系统
        SetupUI();
        
        Debug.Log("拼图测试场景设置完成！");
    }
    
    /// <summary>
    /// 设置摄像机
    /// </summary>
    void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        
        // 设置摄像机参数
        mainCam.transform.position = new Vector3(0, 0, -10);
        mainCam.orthographic = true;
        mainCam.orthographicSize = 8;
        mainCam.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 1f);
    }
    
    /// <summary>
    /// 设置拼图生成器
    /// </summary>
    void SetupJigsawGenerator()
    {
        GameObject generatorObj = new GameObject("JigsawGenerator");
        JigsawGenerator generator = generatorObj.AddComponent<JigsawGenerator>();
        
        // 设置基本参数
        generator.gridSize = testGridSize;
        generator.piecePrefab = piecePrefab;
        
        // 如果没有预制体，创建一个简单的
        if (generator.piecePrefab == null)
        {
            generator.piecePrefab = CreateSimplePiecePrefab();
        }
    }
    
    /// <summary>
    /// 设置拼图盘
    /// </summary>
    void SetupPuzzleBoard()
    {
        GameObject boardObj = new GameObject("PuzzleBoard");
        PuzzleBoard board = boardObj.AddComponent<PuzzleBoard>();
        
        // 设置基本参数
        board.gridSize = testGridSize;
        board.pieceSpacing = 1.2f;
        board.autoShuffle = true;
        board.shuffleAreaSize = new Vector2(10, 6);
        
        // 查找并设置JigsawGenerator引用
        board.jigsawGenerator = FindObjectOfType<JigsawGenerator>();
    }
    
    /// <summary>
    /// 设置游戏管理器
    /// </summary>
    void SetupGameManager()
    {
        GameObject managerObj = new GameObject("PuzzleGameManager");
        PuzzleGameManager manager = managerObj.AddComponent<PuzzleGameManager>();
        
        // 设置组件引用
        manager.jigsawGenerator = FindObjectOfType<JigsawGenerator>();
        manager.puzzleBoard = FindObjectOfType<PuzzleBoard>();
        
        // 设置基本参数
        manager.defaultGridSize = testGridSize;
        manager.autoStartGame = true;
        manager.imagePath = testImagePath;
        
        // 添加音频源
        AudioSource audioSource = managerObj.AddComponent<AudioSource>();
        manager.audioSource = audioSource;
    }
    
    /// <summary>
    /// 设置UI系统
    /// </summary>
    void SetupUI()
    {
        // 创建Canvas
        GameObject canvasObj;
        if (canvasPrefab != null)
        {
            canvasObj = Instantiate(canvasPrefab);
        }
        else
        {
            canvasObj = CreateSimpleCanvas();
        }
        
        // 添加PuzzleUI组件
        PuzzleUI puzzleUI = canvasObj.GetComponent<PuzzleUI>();
        if (puzzleUI == null)
        {
            puzzleUI = canvasObj.AddComponent<PuzzleUI>();
        }
        
        // 设置引用
        puzzleUI.puzzleBoard = FindObjectOfType<PuzzleBoard>();
        
        // 更新游戏管理器的UI引用
        PuzzleGameManager manager = FindObjectOfType<PuzzleGameManager>();
        if (manager != null)
        {
            manager.puzzleUI = puzzleUI;
        }
    }
    
    /// <summary>
    /// 创建简单的拼图块预制体
    /// </summary>
    GameObject CreateSimplePiecePrefab()
    {
        GameObject prefab = new GameObject("PuzzlePiece");
        
        // 添加SpriteRenderer
        SpriteRenderer sr = prefab.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        
        // 添加PolygonCollider2D
        PolygonCollider2D collider = prefab.AddComponent<PolygonCollider2D>();
        
        // 添加PuzzlePiece脚本
        PuzzlePiece puzzlePiece = prefab.AddComponent<PuzzlePiece>();
        
        // 设置标签
        prefab.tag = "PuzzlePiece";
        
        return prefab;
    }
    
    /// <summary>
    /// 创建简单的Canvas
    /// </summary>
    GameObject CreateSimpleCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        // 配置GraphicRaycaster，避免阻挡拼图块的鼠标事件
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        
        // 设置Canvas参数
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1; // 降低排序层级，避免阻挡拼图块
        
        // 设置CanvasScaler参数
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        // 创建EventSystem（如果不存在）
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // 创建基本UI元素
        CreateBasicUI(canvasObj);
        
        return canvasObj;
    }
    
    /// <summary>
    /// 创建基本UI元素
    /// </summary>
    void CreateBasicUI(GameObject canvas)
    {
        // 创建控制面板
        GameObject panel = CreateUIPanel(canvas, "ControlPanel", new Vector2(300, 400));
        panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-660, 0);
        
        // 创建按钮
        CreateUIButton(panel, "ShuffleButton", "打乱", new Vector2(0, 150));
        CreateUIButton(panel, "ResetButton", "重置", new Vector2(0, 100));
        CreateUIButton(panel, "SolutionButton", "显示答案", new Vector2(0, 50));
        CreateUIButton(panel, "NewGameButton", "新游戏", new Vector2(0, 0));
        
        // 创建难度滑块
        CreateUISlider(panel, "DifficultySlider", new Vector2(0, -50));
        
        // 创建文本显示
        CreateUIText(panel, "ProgressText", "进度: 0%", new Vector2(0, -100));
        CreateUIText(panel, "DifficultyText", "难度: 4x4", new Vector2(0, -80));
        
        // 创建完成面板
        GameObject completionPanel = CreateUIPanel(canvas, "CompletionPanel", new Vector2(400, 200));
        completionPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        completionPanel.SetActive(false);
        
        CreateUIText(completionPanel, "CompletionText", "恭喜完成！", Vector2.zero);
    }
    
    /// <summary>
    /// 创建UI面板
    /// </summary>
    GameObject CreateUIPanel(GameObject parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.7f);
        
        return panel;
    }
    
    /// <summary>
    /// 创建UI按钮
    /// </summary>
    GameObject CreateUIButton(GameObject parent, string name, string text, Vector2 position)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent.transform, false);
        
        RectTransform rect = button.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 40);
        rect.anchoredPosition = position;
        
        Image image = button.AddComponent<Image>();
        image.color = new Color(0.2f, 0.3f, 0.8f, 1f);
        
        Button btn = button.AddComponent<Button>();
        
        // 创建文本子对象
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 16;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        btn.targetGraphic = image;
        
        return button;
    }
    
    /// <summary>
    /// 创建UI滑块
    /// </summary>
    GameObject CreateUISlider(GameObject parent, string name, Vector2 position)
    {
        GameObject slider = new GameObject(name);
        slider.transform.SetParent(parent.transform, false);
        
        RectTransform rect = slider.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 20);
        rect.anchoredPosition = position;
        
        Slider sliderComponent = slider.AddComponent<Slider>();
        sliderComponent.minValue = 2;
        sliderComponent.maxValue = 8;
        sliderComponent.wholeNumbers = true;
        sliderComponent.value = 4;
        
        // 创建背景
        GameObject background = new GameObject("Background");
        background.transform.SetParent(slider.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        
        // 创建填充区域
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(slider.transform, false);
        RectTransform fillRect = fillArea.AddComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillImageRect = fill.AddComponent<RectTransform>();
        fillImageRect.sizeDelta = Vector2.zero;
        fillImageRect.anchorMin = Vector2.zero;
        fillImageRect.anchorMax = Vector2.one;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        
        // 创建滑块手柄
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(slider.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.sizeDelta = Vector2.zero;
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        
        // 设置滑块组件引用
        sliderComponent.fillRect = fillImageRect;
        sliderComponent.handleRect = handleRect;
        sliderComponent.targetGraphic = handleImage;
        
        return slider;
    }
    
    /// <summary>
    /// 创建UI文本
    /// </summary>
    GameObject CreateUIText(GameObject parent, string name, string text, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 30);
        rect.anchoredPosition = position;
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        return textObj;
    }
    
    /// <summary>
    /// 手动触发场景设置（用于编辑器测试）
    /// </summary>
    [ContextMenu("Setup Test Scene")]
    public void ManualSetupTestScene()
    {
        SetupTestScene();
    }
}