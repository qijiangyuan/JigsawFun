using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DifficultyPage : BasePage
{
    [Header("难度选择组件")]
    public Image previewImage;              // 图片预览
    public Image gridImage;               // 网格图片 (用于显示拼图网格)
    public Slider difficultySlider;         // 难度滑块 (2-10)
    public TextMeshProUGUI difficultyText;             // 难度显示文本
    public Toggle backgroundToggle;         // 背景图片开关
    public Button startButton;              // 开始拼图按钮
    public Button backButton;               // 返回按钮

    [Header("UI文本")]
    public Text titleText;                  // 标题文本
    public Text backgroundToggleLabel;      // 背景开关标签

    private Sprite selectedImage;           // 当前选中的图片
    private int currentDifficulty = 3;      // 当前难度 (默认3x3)
    private bool showBackground = true;     // 是否显示背景图片

    protected override void Awake()
    {
        base.Awake();
        InitializeComponents();
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        // 设置滑块范围
        if (difficultySlider != null)
        {
            difficultySlider.minValue = 2;
            difficultySlider.maxValue = 10;
            difficultySlider.wholeNumbers = true;
            difficultySlider.value = currentDifficulty;
            difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
        }

        // 设置背景开关
        if (backgroundToggle != null)
        {
            backgroundToggle.isOn = showBackground;
            backgroundToggle.onValueChanged.AddListener(OnBackgroundToggleChanged);
        }

        // 设置按钮事件
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        // 设置默认文本
        UpdateUI();
    }

    /// <summary>
    /// 设置选中的图片
    /// </summary>
    /// <param name="image">选中的图片</param>
    public void SetSelectedImage(Sprite image)
    {
        selectedImage = image;

        if (previewImage != null)
        {
            previewImage.sprite = selectedImage;
            previewImage.gameObject.SetActive(selectedImage != null);
        }

        if (gridImage != null)
        {
            gridImage.sprite = selectedImage;
            gridImage.gameObject.SetActive(selectedImage != null);
        }

        UpdateUI();
    }

    /// <summary>
    /// 难度滑块值改变事件
    /// </summary>
    /// <param name="value">滑块值</param>
    private void OnDifficultyChanged(float value)
    {
        // 0~1 映射到 2~10
        currentDifficulty = Mathf.RoundToInt(value);

        // 网格数量
        gridImage.materialForRendering.SetFloat("_GridCount", currentDifficulty);

        // 线性插值：difficulty=2时为0.004，difficulty=10时为0.001
        float lineWidth = Mathf.Lerp(0.004f, 0.0018f, (currentDifficulty - 2f) / 8f);
        gridImage.materialForRendering.SetFloat("_LineWidth", lineWidth);


        if (difficultyText != null)
        {
            difficultyText.text = $"{currentDifficulty}x{currentDifficulty}";
        }
    }

    /// <summary>
    /// 背景开关改变事件
    /// </summary>
    /// <param name="isOn">开关状态</param>
    private void OnBackgroundToggleChanged(bool isOn)
    {
        showBackground = isOn;
    }

    /// <summary>
    /// 开始拼图按钮点击事件
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (selectedImage == null)
        {
            Debug.LogWarning("没有选中图片！");
            return;
        }

        // 使用GameManager启动游戏
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame(selectedImage, currentDifficulty, showBackground);
        }
        else
        {
            Debug.LogError("GameManager实例不存在！");
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        UpdateGridMaterial();
        UpdateDifficultyText();
        UpdateTitleText();
        UpdateLabels();

    }

    /// <summary>
    /// 更新PuzzleGrid材质属性  
    /// </summary>
    private void UpdateGridMaterial()
    {
        if (gridImage != null && gridImage.material != null)
        {
            gridImage.materialForRendering.SetFloat("_GridCount", currentDifficulty);
            float lineWidth = Mathf.Lerp(0.004f, 0.0018f, (currentDifficulty - 2f) / 8f);
            gridImage.materialForRendering.SetFloat("_LineWidth", lineWidth);


        }
    }

    /// <summary>
    /// 更新难度显示文本
    /// </summary>
    private void UpdateDifficultyText()
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"{currentDifficulty}x{currentDifficulty}";
        }
    }

    /// <summary>
    /// 更新标题文本
    /// </summary>
    private void UpdateTitleText()
    {
        if (titleText != null)
        {
            titleText.text = "选择难度";
        }
    }

    /// <summary>
    /// 更新标签文本
    /// </summary>
    private void UpdateLabels()
    {
        if (backgroundToggleLabel != null)
        {
            backgroundToggleLabel.text = "显示背景图片";
        }
    }

    ///// <summary>
    ///// 获取当前游戏设置
    ///// </summary>
    ///// <returns>游戏设置</returns>
    //public GameSettings GetGameSettings()
    //{
    //    return new GameSettings
    //    {
    //        selectedImage = selectedImage,
    //        difficulty = currentDifficulty,
    //        showBackground = showBackground
    //    };
    //}

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public void ResetToDefault()
    {
        currentDifficulty = 3;
        showBackground = true;

        if (difficultySlider != null)
            difficultySlider.value = currentDifficulty;

        if (backgroundToggle != null)
            backgroundToggle.isOn = showBackground;

        UpdateUI();
    }

    protected override void OnPageShow()
    {
        base.OnPageShow();

        // 确保UI是最新的
        UpdateUI();

        //// 如果没有选中图片，禁用开始按钮
        //if (startButton != null)
        //{
        //    startButton.interactable = selectedImage != null;
        //}
    }
}

///// <summary>
///// 游戏设置数据类
///// </summary>
//[System.Serializable]
//public class GameSettings
//{
//    public Sprite selectedImage;    // 选中的图片
//    public int difficulty;          // 难度 (n×n)
//    public bool showBackground;     // 是否显示背景图片

//    /// <summary>
//    /// 获取拼图块总数
//    /// </summary>
//    public int GetTotalPieces()
//    {
//        return difficulty * difficulty;
//    }

//    /// <summary>
//    /// 获取难度描述
//    /// </summary>
//    public string GetDifficultyDescription()
//    {
//        return $"{difficulty}×{difficulty}";
//    }
//}


