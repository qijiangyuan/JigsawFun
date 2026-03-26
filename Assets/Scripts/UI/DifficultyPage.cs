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
    public Button continueButton;           // 继续按钮
    public Slider progressSlider;           // 进度条
    public TextMeshProUGUI progressText;    // 进度百分比文本
    public GameObject difficultySliderContainer;
    
    private int savedGridSize = -1;
    private bool hasSavedState = false;

    [Header("UI文本")]
    public Text titleText;                  // 标题文本
    public Text backgroundToggleLabel;      // 背景开关标签

    private Sprite selectedImage;           // 当前选中的图片
    private int currentDifficulty = 3;      // 当前难度 (默认3x3)
    private bool showBackground = true;     // 是否显示背景图片

    protected override void Awake()
    {
        showFooter = false;
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
        
        // 动态创建“继续”与进度条（若未在预制体中配置）
        if (continueButton == null)
        {
            GameObject btnObj = new GameObject("ContinueButton");
            btnObj.transform.SetParent(transform, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 140f);
            rt.sizeDelta = new Vector2(360f, 80f);
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.85f, 0.85f, 0.95f, 0.95f);
            var btn = btnObj.AddComponent<Button>();
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            txt.alignment = TextAlignmentOptions.Center;
            txt.text = "Continue";
            txt.fontSize = 32;
            continueButton = btn;
        }
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        continueButton.gameObject.SetActive(false);

        if (progressSlider == null)
        {
            GameObject sObj = new GameObject("ProgressSlider");
            sObj.transform.SetParent(transform, false);
            var rt = sObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 230f);
            rt.sizeDelta = new Vector2(420f, 30f);
            var bg = sObj.AddComponent<Image>();
            bg.color = new Color(0.92f, 0.92f, 0.95f, 1f);
            progressSlider = sObj.AddComponent<Slider>();
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;
        }
        if (progressText == null)
        {
            GameObject tObj = new GameObject("ProgressText");
            tObj.transform.SetParent(progressSlider.transform, false);
            progressText = tObj.AddComponent<TextMeshProUGUI>();
            var trt = progressText.rectTransform;
            trt.anchorMin = new Vector2(1f, 0.5f);
            trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(1f, 0.5f);
            trt.anchoredPosition = new Vector2(20f, 0f);
            trt.sizeDelta = new Vector2(120f, 30f);
            progressText.alignment = TextAlignmentOptions.MidlineRight;
            progressText.fontSize = 24;
        }
        progressSlider.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);

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
        UpdateContinueAndProgress();
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
        UpdatePrimaryButtons();
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
            // 若存在历史存档且用户选择了新的难度，则清除旧存档
            if (hasSavedState && !string.IsNullOrEmpty(selectedImage.name))
            {
                // 删除该图片对应的存档
                PlayPrefsManager.Instance.ClearPuzzleStateForImage(selectedImage.name);
                hasSavedState = false;
                savedGridSize = -1;
            }
            GameManager.Instance.StartNewGame(selectedImage, currentDifficulty, showBackground);
        }
        else
        {
            Debug.LogError("GameManager实例不存在！");
        }
    }
    
    private void OnContinueButtonClicked()
    {
        if (selectedImage == null) return;
        var state = PlayPrefsManager.Instance.LoadPuzzleStateForImage(selectedImage.name);
        if (state == null || state.pieces == null || state.pieces.Length == 0) return;
        currentDifficulty = state.gridSize;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame(selectedImage, currentDifficulty, showBackground);
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
    
    private void UpdateContinueAndProgress()
    {
        if (selectedImage == null)
        {
            continueButton.gameObject.SetActive(false);
            progressSlider.gameObject.SetActive(false);
            if (progressText != null) progressText.gameObject.SetActive(false);
            if (startButton != null) startButton.gameObject.SetActive(true);
            hasSavedState = false;
            savedGridSize = -1;
            UpdatePrimaryButtons();
            return;
        }
        Debug.Log($"[DifficultyPage] Checking saved state for image={selectedImage.name}");
        var state = PlayPrefsManager.Instance.LoadPuzzleStateForImage(selectedImage.name);
        if (state != null && state.pieces != null && state.pieces.Length > 0)
        {
            hasSavedState = true;
            savedGridSize = state.gridSize;
            int placed = 0;
            for (int i = 0; i < state.pieces.Length; i++)
            {
                if (state.pieces[i].isPlaced) placed++;
            }
            float progress = Mathf.Clamp01((float)placed / state.pieces.Length);
            Debug.Log($"[DifficultyPage] Found state grid={state.gridSize} placed={placed}/{state.pieces.Length} progress={progress}");
            // 有存档就显示继续；进度条显示 0% 也是有效反馈
            progressSlider.value = progress;
            progressSlider.gameObject.SetActive(true);
            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
                progressText.gameObject.SetActive(true);
            }
            // 保持滑条可见，主按钮根据难度决定
            continueButton.gameObject.SetActive(true); // 继续按钮会在 UpdatePrimaryButtons 中按需隐藏
            
            // 将难度调整为存档难度并更新 UI
            currentDifficulty = state.gridSize;
            if (difficultySlider != null) difficultySlider.value = currentDifficulty;
            UpdateUI();
            UpdatePrimaryButtons();
        }
        else
        {
            Debug.Log($"[DifficultyPage] No saved state for image={selectedImage.name}");
            hasSavedState = false;
            savedGridSize = -1;
            continueButton.gameObject.SetActive(false);
            progressSlider.gameObject.SetActive(false);
            if (progressText != null) progressText.gameObject.SetActive(false);
            if (startButton != null) startButton.gameObject.SetActive(true);
            UpdatePrimaryButtons();
        }
    }

    private GameObject GetDifficultySliderContainer()
    {
        if (difficultySliderContainer != null) return difficultySliderContainer;
        if (difficultySlider == null) return null;
        Transform t = difficultySlider.transform;
        Transform p = t.parent;
        if (p != null && p != transform)
        {
            var sliderInParent = p.GetComponentInChildren<Slider>(true);
            if (sliderInParent == difficultySlider && p.GetComponent<Slider>() == null)
            {
                return p.gameObject;
            }
        }
        return t.gameObject;
    }

    private void SetDifficultySliderVisible(bool visible)
    {
        var go = GetDifficultySliderContainer();
        if (go != null) go.SetActive(visible);
    }

    private void UpdatePrimaryButtons()
    {
        // 规则：
        // - 无存档：只显示 Start，隐藏 Continue
        // - 有存档：
        //   - 如果当前滑块等于存档难度：显示 Continue，隐藏 Start
        //   - 如果当前滑块不等于存档难度：显示 Start，隐藏 Continue
        if (!hasSavedState)
        {
            if (startButton != null) startButton.gameObject.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            return;
        }
        bool sameAsSaved = (currentDifficulty == savedGridSize);
        if (startButton != null) startButton.gameObject.SetActive(!sameAsSaved);
        if (continueButton != null) continueButton.gameObject.SetActive(sameAsSaved);
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
        UpdateContinueAndProgress();

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


