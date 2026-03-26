using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class GalleryPage : BasePage
{
    [Header("Gallery组件")]
    public ScrollRect categoryScrollRect;           // 类别滚动视图
    public Transform categoryToggleParent;          // 类别按钮父对象
    public Toggle categoryTogglePrefab;            // 类别Toggle预制体
    public Button cameraButton;                    // 相机按钮

    public ScrollRect imageScrollRect;             // 图片滚动视图
    public Transform imageGridParent;              // 图片网格父对象
    public Button imageButtonPrefab;              // 图片按钮预制体

    public TextMeshProUGUI titleText;
    public RectTransform titleBackground;
    public Vector2 titleBackgroundPadding = new Vector2(150f, 14f);
    public float titleBackgroundMinWidth = 0f;
    public TextMeshProUGUI InProgressText;
    public bool centerTitle = true;
    public bool centerCategoryToggles = true;

    private ImageCropperModal cropper;
    private Sprite roundedUiSprite;

    [Header("类别数据")]
    public List<CategoryData> categories = new List<CategoryData>();
    private string userCategoryName = "My Picture";
    private int userCategoryIndex = -1;
    private int inProgressIndex = -1;
    private int completedIndex = -1;

    private int currentCategoryIndex = 0;
    private List<Toggle> categoryToggles = new List<Toggle>();
    private List<Button> imageButtons = new List<Button>();
    private ToggleGroup categoryToggleGroup;
    private List<int> categoryIndexByToggle = new List<int>();

    private Toggle progressInProgressToggle;
    private Toggle progressCompletedToggle;
    private RectTransform progressTabBar;

    [Header("Footer Navigation")]
    public Toggle footerGalleryToggle;
    public Toggle footerMyPictureToggle;
    public Toggle footerInProgressToggle;
    public ToggleGroup footerToggleGroup;
    private int lastNormalCategoryIndex = 0;
    private bool suppressFooterCallback = false;
    private FooterTab lastFooterTab = FooterTab.Unknown;

    private enum FooterTab
    {
        Unknown = 0,
        Gallery = 1,
        MyPicture = 2,
        InProgress = 3
    }

    [System.Serializable]
    public class CategoryData
    {
        public string categoryName;        // 类别名称
        public List<Sprite> images;        // 该类别下的图片列表

        public CategoryData(string name)
        {
            categoryName = name;
            images = new List<Sprite>();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        InitializeCategories();
        EnsureTitleText();

        // 创建ToggleGroup组件
        categoryToggleGroup = categoryToggleParent.GetComponent<ToggleGroup>();
        if (categoryToggleGroup == null)
        {
            categoryToggleGroup = categoryToggleParent.gameObject.AddComponent<ToggleGroup>();
        }
        categoryToggleGroup.allowSwitchOff = true;

        BindFooterToggles();

        // 初始化相机按钮
        if (cameraButton != null)
        {
            cameraButton.onClick.AddListener(OnCameraButtonClicked);
            Debug.Log("cameraButton assigned via Inspector");
        }
        else
        {
            // 如果没有分配按钮，尝试在子物体中找一个名字叫 "CameraButton" 的按钮
            Transform camBtnTransform = transform.Find("CameraButton");
            if (camBtnTransform != null)
            {
                cameraButton = camBtnTransform.GetComponent<Button>();
                if (cameraButton != null)
                {
                    cameraButton.onClick.AddListener(OnCameraButtonClicked);
                    Debug.Log("cameraButton found via direct child Transform.Find(\"CameraButton\")");
                }
            }
            // 递归在子层级中寻找（如果不在直接子节点）
            if (cameraButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn.name == "CameraButton")
                    {
                        cameraButton = btn;
                        cameraButton.onClick.AddListener(OnCameraButtonClicked);
                        Debug.Log("cameraButton found via recursive search in children");
                        break;
                    }
                }
            }
            if (cameraButton == null)
            {
                Debug.LogWarning("cameraButton not found. Ensure a Button named 'CameraButton' exists under GalleryPage or assign it in Inspector.");
            }
        }
    }

    private void BindFooterToggles()
    {
        if (footerToggleGroup == null || footerGalleryToggle == null || footerMyPictureToggle == null || footerInProgressToggle == null)
        {
            Transform footer = transform.root != null ? transform.root.Find("Footer") : null;
            if (footer == null) footer = GameObject.Find("Footer") != null ? GameObject.Find("Footer").transform : null;
            if (footer != null)
            {
                if (footerToggleGroup == null)
                {
                    footerToggleGroup = footer.GetComponent<ToggleGroup>();
                    if (footerToggleGroup == null) footerToggleGroup = footer.gameObject.AddComponent<ToggleGroup>();
                }
                var toggles = footer.GetComponentsInChildren<Toggle>(true);
                for (int i = 0; i < toggles.Length; i++)
                {
                    var t = toggles[i];
                    if (t == null) continue;
                    if (footerGalleryToggle == null && t.name.IndexOf("Gallery", StringComparison.OrdinalIgnoreCase) >= 0) footerGalleryToggle = t;
                    if (footerMyPictureToggle == null && (t.name.IndexOf("MyPicture", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("My Picture", StringComparison.OrdinalIgnoreCase) >= 0)) footerMyPictureToggle = t;
                    if (footerInProgressToggle == null && t.name.IndexOf("InProgress", StringComparison.OrdinalIgnoreCase) >= 0) footerInProgressToggle = t;
                }

                // 若按名称未匹配成功，则按从左到右的顺序兜底绑定（常见：HOME / Camera / INPROGRESS）
                if (footerGalleryToggle == null || footerMyPictureToggle == null || footerInProgressToggle == null)
                {
                    var candidates = new List<Toggle>();
                    for (int i = 0; i < toggles.Length; i++)
                    {
                        var t = toggles[i];
                        if (t == null) continue;
                        if (t.transform == footer) continue;
                        candidates.Add(t);
                    }
                    candidates.Sort((a, b) =>
                    {
                        var ra = a.GetComponent<RectTransform>();
                        var rb = b.GetComponent<RectTransform>();
                        if (ra != null && rb != null)
                        {
                            return ra.anchoredPosition.x.CompareTo(rb.anchoredPosition.x);
                        }
                        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
                    });
                    if (candidates.Count >= 3)
                    {
                        if (footerGalleryToggle == null) footerGalleryToggle = candidates[0];
                        if (footerMyPictureToggle == null) footerMyPictureToggle = candidates[1];
                        if (footerInProgressToggle == null) footerInProgressToggle = candidates[candidates.Count - 1];
                    }
                }
            }
        }

        if (footerToggleGroup != null)
        {
            if (footerGalleryToggle != null) footerGalleryToggle.group = footerToggleGroup;
            if (footerMyPictureToggle != null) footerMyPictureToggle.group = footerToggleGroup;
            if (footerInProgressToggle != null) footerInProgressToggle.group = footerToggleGroup;
        }

        Debug.Log($"[Gallery] Footer bound: gallery={footerGalleryToggle?.name}, myPicture={footerMyPictureToggle?.name}, inProgress={footerInProgressToggle?.name}");

        if (footerGalleryToggle != null)
        {
            footerGalleryToggle.onValueChanged.RemoveAllListeners();
            footerGalleryToggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn || suppressFooterCallback) return;
                OpenGalleryFromFooter();
            });
        }
        if (footerMyPictureToggle != null)
        {
            footerMyPictureToggle.onValueChanged.RemoveAllListeners();
            footerMyPictureToggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn || suppressFooterCallback) return;
                OpenMyPictureFromFooter();
            });
        }
        if (footerInProgressToggle != null)
        {
            footerInProgressToggle.onValueChanged.RemoveAllListeners();
            footerInProgressToggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn || suppressFooterCallback) return;
                OpenInProgressFromFooter();
            });
        }
    }

    private void OpenGalleryFromFooter()
    {
        int idx = FindFirstNormalCategoryIndex();
        if (lastNormalCategoryIndex >= 0 && lastNormalCategoryIndex < categories.Count &&
            lastNormalCategoryIndex != userCategoryIndex && lastNormalCategoryIndex != inProgressIndex && lastNormalCategoryIndex != completedIndex)
        {
            idx = lastNormalCategoryIndex;
        }
        ShowCategory(idx);
    }

    private void OpenMyPictureFromFooter()
    {
        if (userCategoryIndex >= 0) ShowCategory(userCategoryIndex);
    }

    private void OpenInProgressFromFooter()
    {
        if (inProgressIndex >= 0) ShowCategory(inProgressIndex);
    }

    private int FindFirstNormalCategoryIndex()
    {
        for (int i = 0; i < categories.Count; i++)
        {
            if (i != userCategoryIndex && i != inProgressIndex && i != completedIndex) return i;
        }
        return 0;
    }

    private void UpdateFooterToggleStates()
    {
        suppressFooterCallback = true;
        bool isMyPicture = currentCategoryIndex == userCategoryIndex;
        bool isInProgress = currentCategoryIndex == inProgressIndex || currentCategoryIndex == completedIndex;
        bool isGallery = !isMyPicture && !isInProgress;
        if (footerGalleryToggle != null) footerGalleryToggle.SetIsOnWithoutNotify(isGallery);
        if (footerMyPictureToggle != null) footerMyPictureToggle.SetIsOnWithoutNotify(isMyPicture);
        if (footerInProgressToggle != null) footerInProgressToggle.SetIsOnWithoutNotify(isInProgress);
        suppressFooterCallback = false;
    }


    private void OnEnable()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.onPhotoCaptured += OnPhotoCaptured;
        }
    }

    private void OnDisable()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.onPhotoCaptured -= OnPhotoCaptured;
        }
    }

    private void OnCameraButtonClicked()
    {
        Debug.Log("CameraButton clicked");
        if (CameraManager.Instance == null)
        {
            Debug.Log("CameraManager not found, creating new instance");
            GameObject camMgrObj = new GameObject("CameraManager");
            var mgr = camMgrObj.AddComponent<CameraManager>();
            mgr.onPhotoCaptured += OnPhotoCaptured;
        }

        if (CameraManager.Instance != null)
        {
            Debug.Log("Invoking mobile choice or gallery fallback");
            CameraManager.Instance.onPhotoCaptured -= OnPhotoCaptured;
            CameraManager.Instance.onPhotoCaptured += OnPhotoCaptured;
#if UNITY_ANDROID || UNITY_IOS
            ShowCameraChoice();
#else
            CameraManager.Instance.PickImageFromGallery();
#endif
        }
        else
        {
            Debug.LogError("CameraManager Instance is still null after creation attempt!");
        }
    }

    GameObject cameraChoicePanel;
    void ShowCameraChoice()
    {
        if (cameraChoicePanel == null)
        {
            roundedUiSprite = RoundedRectSpriteCache.Get(64, 16);
            var panel = new GameObject("CameraChoicePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var bg = panel.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            var container = new GameObject("Container", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            container.transform.SetParent(panel.transform, false);
            var cRect = container.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(520, 240);
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.anchoredPosition = Vector2.zero;
            var cBg = container.GetComponent<Image>();
            cBg.color = new Color(252f / 255f, 246f / 255f, 227f / 255f, 1f);
            cBg.sprite = roundedUiSprite;
            cBg.type = Image.Type.Sliced;

            Button MakeBtn(string name, Vector2 pos, string text, Color buttonColor, UnityEngine.Events.UnityAction onClick)
            {
                var btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(container.transform, false);
                var r = btnObj.GetComponent<RectTransform>();
                r.sizeDelta = new Vector2(220, 80);
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = pos;
                var img = btnObj.GetComponent<Image>();
                img.color = buttonColor;
                img.sprite = roundedUiSprite;
                img.type = Image.Type.Sliced;
                var btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(onClick);
                var txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                txtObj.transform.SetParent(btnObj.transform, false);
                var tRect = txtObj.GetComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0, 0);
                tRect.anchorMax = new Vector2(1, 1);
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;
                var t = txtObj.GetComponent<Text>();
                t.text = text;
                t.alignment = TextAnchor.MiddleCenter;
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.color = Color.white;
                t.fontSize = 32;
                return btn;
            }

            var takeBtn = MakeBtn("TakePhotoButton", new Vector2(-120, 0), "Camera", new Color(4f / 255f, 193f / 255f, 195f / 255f, 1f), () =>
            {
                CloseCameraChoice();
                CameraManager.Instance.TakePhoto();
            });
            var pickBtn = MakeBtn("PickGalleryButton", new Vector2(120, 0), "Album", new Color(255f / 255f, 120f / 255f, 77f / 255f, 1f), () =>
            {
                CloseCameraChoice();
                CameraManager.Instance.PickImageFromGallery();
            });

            var closeBtn = panel.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(CloseCameraChoice);

            cameraChoicePanel = panel;
        }
        cameraChoicePanel.SetActive(true);
    }

    void CloseCameraChoice()
    {
        if (cameraChoicePanel != null) cameraChoicePanel.SetActive(false);
    }

    private void OnPhotoCaptured(Texture2D texture)
    {
        if (texture == null) return;
        if (!IsSquare(texture))
        {
            EnsureCropper();
            cropper.Show(texture, cropped =>
            {
                if (cropped == null) return;
                AddUserImageToMyPicture(cropped);
            });
            return;
        }
        AddUserImageToMyPicture(texture);
    }

    private bool IsSquare(Texture2D tex)
    {
        if (tex == null) return true;
        if (tex.width == tex.height) return true;
        float aspect = (float)tex.width / Mathf.Max(1f, tex.height);
        return Mathf.Abs(aspect - 1f) < 0.01f;
    }

    private void EnsureCropper()
    {
        if (cropper != null) return;
        var go = new GameObject("ImageCropperModal", typeof(RectTransform));
        Transform parent = transform.root != null ? transform.root : transform;
        go.transform.SetParent(parent, false);
        cropper = go.AddComponent<ImageCropperModal>();
        go.SetActive(false);
    }

    private void AddUserImageToMyPicture(Texture2D texture)
    {
        Sprite capturedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        capturedSprite.name = "CameraPhoto_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        if (userCategoryIndex < 0 || userCategoryIndex >= categories.Count)
        {
            CategoryData userCategory = new CategoryData(userCategoryName);
            categories.Add(userCategory);
            userCategoryIndex = categories.Count - 1;
            SetupCategoryButtons();
        }
        AddImageToCategory(userCategoryIndex, capturedSprite);
        ShowCategory(userCategoryIndex);
    }

    /// <summary>
    /// 初始化类别数据
    /// </summary>
    private void InitializeCategories()
    {
        // 清空现有类别
        categories.Clear();

        // 从ImageLoader获取可用类别
        List<string> availableCategories = ImageLoader.Instance.GetAvailableCategories();

        foreach (string categoryName in availableCategories)
        {
            CategoryData categoryData = new CategoryData(categoryName);

            // 从ImageLoader加载该类别的图片
            List<Sprite> categoryImages = ImageLoader.Instance.GetCategoryImages(categoryName);
            categoryData.images.AddRange(categoryImages);

            categories.Add(categoryData);
        }

        // 添加一个“我的图片”类别（运行时动态添加）
        CategoryData userCategory = new CategoryData(userCategoryName);
        categories.Add(userCategory);
        userCategoryIndex = categories.Count - 1;
        // 添加“InProgress”类别（紧随 My Picture）
        CategoryData inProgressCategory = new CategoryData("InProgress");
        categories.Add(inProgressCategory);
        inProgressIndex = categories.Count - 1;
        // 添加“Completed”类别（紧随 InProgress）
        CategoryData completedCategory = new CategoryData("Completed");
        categories.Add(completedCategory);
        completedIndex = categories.Count - 1;

        Debug.Log($"初始化完成，共加载 {categories.Count} 个类别");
    }

    // InProgress 列表的刷新在 ShowCategory 与页面显示时执行

    protected override void OnPageShow()
    {
        base.OnPageShow();
        BindFooterToggles();
        SetupCategoryButtons();
        FooterTab tab = GetCurrentFooterTab();
        if (lastFooterTab == FooterTab.Unknown || tab == FooterTab.Unknown)
        {
            suppressFooterCallback = true;
            if (footerGalleryToggle != null) footerGalleryToggle.SetIsOnWithoutNotify(true);
            if (footerMyPictureToggle != null) footerMyPictureToggle.SetIsOnWithoutNotify(false);
            if (footerInProgressToggle != null) footerInProgressToggle.SetIsOnWithoutNotify(false);
            suppressFooterCallback = false;
            tab = FooterTab.Gallery;
        }
        ApplyFooterTab(tab, true);
    }

    /// <summary>
    /// 设置类别Toggle
    /// </summary>
    private void SetupCategoryButtons()
    {
        ApplyCategoryToggleLayout();
        // 清除现有Toggle
        foreach (Toggle toggle in categoryToggles)
        {
            if (toggle != null)
                DestroyImmediate(toggle.gameObject);
        }
        categoryToggles.Clear();
        categoryIndexByToggle.Clear();

        // 创建类别Toggle
        for (int i = 0; i < categories.Count; i++)
        {
            if (i == userCategoryIndex || i == inProgressIndex || i == completedIndex)
            {
                continue;
            }
            int categoryIndex = i; // 闭包变量

            Toggle categoryToggle = Instantiate(categoryTogglePrefab, categoryToggleParent);
            categoryToggle.gameObject.SetActive(true);
            categoryToggle.group = categoryToggleGroup;

            // 设置Toggle文本
            TextMeshProUGUI toggleText = categoryToggle.GetComponentInChildren<TextMeshProUGUI>();
            if (toggleText != null)
                toggleText.text = categories[i].categoryName;

            // Toggle的图标在场景中直接设置，不需要代码处理

            // 添加值改变事件
            categoryToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn && EventSystem.current.currentSelectedGameObject != null)
                {
                    // 播放切换音效
                    if (JigsawFun.Audio.AudioManager.Instance != null)
                    {
                        AudioClip clickSound = Resources.Load<AudioClip>("Audio/bong_001");
                        if (clickSound != null)
                        {
                            JigsawFun.Audio.AudioManager.Instance.PlayUISound(clickSound);
                        }
                        else
                        {
                            Debug.LogWarning("无法加载音效: Audio/bong_001.ogg");
                        }
                    }
                    ShowCategory(categoryIndex);
                }
            });

            categoryToggles.Add(categoryToggle);
            categoryIndexByToggle.Add(categoryIndex);
        }

        // 默认选中第一个Toggle
        if (categoryToggles.Count > 0)
        {
            categoryToggles[0].SetIsOnWithoutNotify(true);
            if (categoryIndexByToggle.Count > 0)
            {
                lastNormalCategoryIndex = categoryIndexByToggle[0];
            }
        }
    }

    private void Update()
    {
        ApplyFooterTab(GetCurrentFooterTab(), false);
    }

    private FooterTab GetCurrentFooterTab()
    {
        if (footerMyPictureToggle != null && footerMyPictureToggle.isOn) return FooterTab.MyPicture;
        if (footerInProgressToggle != null && footerInProgressToggle.isOn) return FooterTab.InProgress;
        if (footerGalleryToggle != null && footerGalleryToggle.isOn) return FooterTab.Gallery;
        return FooterTab.Unknown;
    }

    private void ApplyFooterTab(FooterTab tab, bool force)
    {
        if (!force && tab == lastFooterTab) return;
        lastFooterTab = tab;
        UpdateTitleForFooterTab(tab);
        Debug.Log($"[Gallery] ApplyFooterTab tab={tab}, userIndex={userCategoryIndex}, inProgressIndex={inProgressIndex}, lastNormal={lastNormalCategoryIndex}");
        if (tab == FooterTab.MyPicture)
        {
            OpenMyPictureFromFooter();
            return;
        }
        if (tab == FooterTab.InProgress)
        {
            OpenInProgressFromFooter();
            return;
        }
        OpenGalleryFromFooter();
    }

    private void EnsureTitleText()
    {
        if (titleText != null) return;
        var t = transform.Find("GalleryPanel/Title");
        if (t != null)
        {
            titleText = t.GetComponent<TextMeshProUGUI>();
            if (titleText != null) return;
        }
        var any = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < any.Length; i++)
        {
            var txt = any[i];
            if (txt != null && string.Equals(txt.name, "Title", StringComparison.OrdinalIgnoreCase))
            {
                titleText = txt;
                return;
            }
        }
    }

    private void ApplyTitleCenterLayout()
    {
        if (!centerTitle) return;
        EnsureTitleText();
        EnsureTitleBackground();
        if (titleText == null) return;
        var rt = titleText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(0.5f, rt.anchorMax.y);
        rt.pivot = new Vector2(0.5f, rt.pivot.y);
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
        titleText.alignment = TextAlignmentOptions.Center;
        if (titleBackground != null)
        {
            titleBackground.anchorMin = new Vector2(0.5f, titleBackground.anchorMin.y);
            titleBackground.anchorMax = new Vector2(0.5f, titleBackground.anchorMax.y);
            titleBackground.pivot = new Vector2(0.5f, titleBackground.pivot.y);
            titleBackground.anchoredPosition = new Vector2(0f, titleBackground.anchoredPosition.y);
        }
    }

    private void ApplyCategoryToggleLayout()
    {
        if (!centerCategoryToggles) return;
        if (categoryToggleParent == null) return;
        var hlg = categoryToggleParent.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childAlignment = TextAnchor.MiddleCenter;
        }
    }

    private void EnsureTitleBackground()
    {
        if (titleBackground != null) return;
        if (titleText == null) return;
        var p = titleText.transform.parent;
        if (p != null)
        {
            var child = p.Find("TitleBackground");
            if (child != null)
            {
                titleBackground = child as RectTransform;
                if (titleBackground != null) return;
            }
            for (int i = 0; i < p.childCount; i++)
            {
                var c = p.GetChild(i) as RectTransform;
                if (c == null) continue;
                if (c == titleText.rectTransform) continue;
                if (c.name.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    titleBackground = c;
                    return;
                }
            }
        }
    }

    private void UpdateTitleBackgroundSize()
    {
        EnsureTitleText();
        EnsureTitleBackground();
        ApplyTitleCenterLayout();
        if (titleText == null || titleBackground == null) return;
        titleText.ForceMeshUpdate();
        float w = titleText.preferredWidth + titleBackgroundPadding.x * 2f;
        float h = titleText.preferredHeight + titleBackgroundPadding.y * 2f;
        if (titleBackgroundMinWidth > 0f && w < titleBackgroundMinWidth) w = titleBackgroundMinWidth;
        titleBackground.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        titleBackground.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
    }

    private void UpdateTitleForFooterTab(FooterTab tab)
    {
        EnsureTitleText();
        if (titleText == null) return;
        string text = "Gallery";
        if (tab == FooterTab.MyPicture) text = "MyPicture";
        else if (tab == FooterTab.InProgress)
        {
            text = currentCategoryIndex == completedIndex ? "Completed" : "InProgress";
        }
        titleText.text = text;
        UpdateTitleBackgroundSize();
    }

    /// <summary>
    /// 显示指定类别的图片
    /// </summary>
    /// <param name="categoryIndex">类别索引</param>
    public void ShowCategory(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Count)
            return;

        currentCategoryIndex = categoryIndex;
        Debug.Log($"[Gallery] ShowCategory index={categoryIndex}, name={categories[categoryIndex].categoryName}");
        if (currentCategoryIndex != userCategoryIndex && currentCategoryIndex != inProgressIndex && currentCategoryIndex != completedIndex)
        {
            lastNormalCategoryIndex = currentCategoryIndex;
        }
        UpdateCategoryButtonStates();
        UpdateTopCategoryVisibility();
        UpdateFooterToggleStates();
        SetupImageGrid();
    }

    private void UpdateTopCategoryVisibility()
    {
        bool inProgressTab = currentCategoryIndex == inProgressIndex || currentCategoryIndex == completedIndex;
        bool showTop = currentCategoryIndex != userCategoryIndex && !inProgressTab;
        // 只隐藏顶部分类按钮，不要隐藏包含图片网格的容器（避免误把整个内容区域隐藏掉）
        if (categoryToggleParent != null) categoryToggleParent.gameObject.SetActive(showTop);
        // 不要在这里 SetActive(categoryScrollRect)，因为在部分场景结构里它可能是更大的容器，会把图片网格一起隐藏

        for (int i = 0; i < categoryToggles.Count; i++)
        {
            if (categoryToggles[i] != null) categoryToggles[i].gameObject.SetActive(showTop);
        }
        EnsureProgressTabToggles();
        if (progressTabBar != null) progressTabBar.gameObject.SetActive(inProgressTab);
    }

    /// <summary>
    /// 更新类别Toggle状态
    /// </summary>
    private void UpdateCategoryButtonStates()
    {
        for (int i = 0; i < categoryToggles.Count; i++)
        {
            if (categoryToggles[i] != null)
            {
                // Toggle的选中状态由ToggleGroup自动管理
                int mapped = (i >= 0 && i < categoryIndexByToggle.Count) ? categoryIndexByToggle[i] : -1;
                categoryToggles[i].SetIsOnWithoutNotify(mapped == currentCategoryIndex);
            }
        }
        EnsureProgressTabToggles();
        if (progressInProgressToggle != null) progressInProgressToggle.SetIsOnWithoutNotify(currentCategoryIndex == inProgressIndex);
        if (progressCompletedToggle != null) progressCompletedToggle.SetIsOnWithoutNotify(currentCategoryIndex == completedIndex);
        if (progressInProgressToggle != null) progressInProgressToggle.gameObject.SetActive(currentCategoryIndex == inProgressIndex || currentCategoryIndex == completedIndex);
        if (progressCompletedToggle != null) progressCompletedToggle.gameObject.SetActive(currentCategoryIndex == inProgressIndex || currentCategoryIndex == completedIndex);
    }

    private void EnsureProgressTabToggles()
    {
        if (categoryTogglePrefab == null || categoryToggleGroup == null) return;

        if (progressTabBar == null)
        {
            var barGo = new GameObject("ProgressTabBar", typeof(RectTransform));
            barGo.transform.SetParent(transform, false);
            progressTabBar = barGo.GetComponent<RectTransform>();
            progressTabBar.anchorMin = new Vector2(0f, 1f);
            progressTabBar.anchorMax = new Vector2(1f, 1f);
            progressTabBar.pivot = new Vector2(0.5f, 1f);
            progressTabBar.anchoredPosition = new Vector2(0f, -267f);
            progressTabBar.sizeDelta = new Vector2(0f, 90f);
            var hlg = barGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 0, 0);
        }

        if (progressInProgressToggle != null && progressCompletedToggle != null) return;

        Toggle Make(string name, string text, int categoryIndex)
        {
            Toggle t = Instantiate(categoryTogglePrefab, progressTabBar);
            t.name = name;
            t.gameObject.SetActive(false);
            t.group = categoryToggleGroup;
            var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
            t.onValueChanged.RemoveAllListeners();
            t.onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;
                ShowCategory(categoryIndex);
            });
            return t;
        }

        if (progressInProgressToggle == null && inProgressIndex >= 0)
        {
            progressInProgressToggle = Make("ProgressTab_InProgress", "InProgress", inProgressIndex);
        }
        if (progressCompletedToggle == null && completedIndex >= 0)
        {
            progressCompletedToggle = Make("ProgressTab_Completed", "Completed", completedIndex);
        }
    }

    /// <summary>
    /// 设置图片网格
    /// </summary>
    private void SetupImageGrid()
    {
        Debug.Log($"[Gallery] SetupImageGrid begin: current={currentCategoryIndex}, userIndex={userCategoryIndex}, inProgressIndex={inProgressIndex}, imageGridParent={(imageGridParent != null ? imageGridParent.name : "NULL")}, imageButtonPrefab={(imageButtonPrefab != null ? imageButtonPrefab.name : "NULL")}, imageGridActive={(imageGridParent != null ? imageGridParent.gameObject.activeInHierarchy : false)}, imageScrollActive={(imageScrollRect != null ? imageScrollRect.gameObject.activeInHierarchy : false)}");
        if (imageScrollRect != null) imageScrollRect.gameObject.SetActive(true);
        if (imageGridParent != null) imageGridParent.gameObject.SetActive(true);
        int beforeChildren = imageGridParent != null ? imageGridParent.childCount : -1;
        var empty = imageGridParent != null ? imageGridParent.Find("InProgressEmptyText") : null;
        if (empty != null) DestroyImmediate(empty.gameObject);
        var emptyCompleted = imageGridParent != null ? imageGridParent.Find("CompletedEmptyText") : null;
        if (emptyCompleted != null) DestroyImmediate(emptyCompleted.gameObject);

        // 清除现有图片按钮
        foreach (Button btn in imageButtons)
        {
            if (btn != null)
                DestroyImmediate(btn.gameObject);
        }
        imageButtons.Clear();
        int afterClearChildren = imageGridParent != null ? imageGridParent.childCount : -1;
        Debug.Log($"[Gallery] SetupImageGrid cleared: beforeChildren={beforeChildren}, afterClearChildren={afterClearChildren}");

        // 获取当前类别的图片
        CategoryData currentCategory = categories[currentCategoryIndex];

        // InProgress 类别：用未完成列表填充
        if (currentCategoryIndex == inProgressIndex)
        {
            if (cameraButton != null) cameraButton.gameObject.SetActive(false);
            var ids = PlayPrefsManager.Instance.GetAllUnfinishedImageIds();
            if (ids == null || ids.Count == 0)
            {
                // 彻底清空所有旧子节点（防止布局组件残留）
                if (imageGridParent != null)
                {
                    for (int c = imageGridParent.childCount - 1; c >= 0; c--)
                    {
                        DestroyImmediate(imageGridParent.GetChild(c).gameObject);
                    }
                }
                // 创建空文案
                GameObject tObj = new GameObject("InProgressEmptyText");
                tObj.transform.SetParent(imageGridParent, false);
                var rt = tObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(680f, 80f);
                TextMeshProUGUI emptyText = tObj.AddComponent<TextMeshProUGUI>();
                emptyText.alignment = TextAlignmentOptions.Center;
                emptyText.fontSize = 34;
                emptyText.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                emptyText.text = "YouHaveNoPuzzleToComplete";
                Debug.Log("[Gallery] InProgress empty: show placeholder text");
                goto ResetScroll;
            }
            for (int i = 0; i < ids.Count; i++)
            {
                string imageId = ids[i];
                Sprite imageSprite = FindSpriteByName(imageId);
                if (imageSprite == null) continue;
                Button imageBtn = Instantiate(imageButtonPrefab, imageGridParent);
                imageBtn.gameObject.SetActive(true);
                Image btnImage = imageBtn.transform.Find("PuzzleImage")?.GetComponent<Image>();
                if (btnImage != null) btnImage.sprite = imageSprite;
                imageBtn.onClick.AddListener(() => OnInProgressItemClicked(imageId, imageSprite));
                imageButtons.Add(imageBtn);
            }
            goto ResetScroll;
        }

        if (currentCategoryIndex == completedIndex)
        {
            if (cameraButton != null) cameraButton.gameObject.SetActive(false);
            var list = PlayPrefsManager.Instance.GetCompletedPuzzles();
            if (list == null || list.Count == 0)
            {
                if (imageGridParent != null)
                {
                    for (int c = imageGridParent.childCount - 1; c >= 0; c--)
                    {
                        DestroyImmediate(imageGridParent.GetChild(c).gameObject);
                    }
                }
                GameObject tObj = new GameObject("CompletedEmptyText");
                tObj.transform.SetParent(imageGridParent, false);
                var rt = tObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(680f, 80f);
                TextMeshProUGUI emptyText = tObj.AddComponent<TextMeshProUGUI>();
                emptyText.alignment = TextAlignmentOptions.Center;
                emptyText.fontSize = 34;
                emptyText.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                emptyText.text = "No completed puzzles";
                goto ResetScroll;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry == null || string.IsNullOrEmpty(entry.imageId)) continue;

                Sprite imageSprite = FindSpriteByName(entry.imageId);
                if (imageSprite == null) imageSprite = PlayPrefsManager.Instance.LoadCompletedPreviewSprite(entry.imageId);
                if (imageSprite == null) continue;

                Button imageBtn = Instantiate(imageButtonPrefab, imageGridParent);
                imageBtn.gameObject.SetActive(true);
                Image btnImage = imageBtn.transform.Find("PuzzleImage")?.GetComponent<Image>();
                if (btnImage != null) btnImage.sprite = imageSprite;
                string id = entry.imageId;
                int diff = entry.difficulty;
                float time = entry.completionTimeSeconds;
                imageBtn.onClick.AddListener(() => ShowCompletedDetail(id, diff, time, imageSprite));
                imageButtons.Add(imageBtn);
            }
            goto ResetScroll;
        }

        // 在“我的图片”类别中插入一个“添加”卡片
        if (currentCategoryIndex == userCategoryIndex)
        {
            Button addBtn = Instantiate(imageButtonPrefab, imageGridParent);
            addBtn.gameObject.SetActive(true);
            Image btnImageAdd = addBtn.transform.Find("PuzzleImage").GetComponent<Image>();
            if (btnImageAdd != null)
                btnImageAdd.sprite = GetAddSprite();
            addBtn.onClick.AddListener(() => OnCameraButtonClicked());
            imageButtons.Add(addBtn);
            if (cameraButton != null) cameraButton.gameObject.SetActive(false);
            Debug.Log($"[Gallery] MyPicture add card created: activeInHierarchy={addBtn.gameObject.activeInHierarchy}, parentActive={imageGridParent.gameObject.activeInHierarchy}");
        }
        else
        {
            if (cameraButton != null) cameraButton.gameObject.SetActive(true);
        }

        // 创建图片按钮
        for (int i = 0; i < currentCategory.images.Count; i++)
        {
            Sprite imageSprite = currentCategory.images[i];
            if (imageSprite == null) continue;

            Button imageBtn = Instantiate(imageButtonPrefab, imageGridParent);
            imageBtn.gameObject.SetActive(true);

            // 设置图片
            Image btnImage = imageBtn.transform.Find("PuzzleImage").GetComponent<Image>();
            if (btnImage != null)
                btnImage.sprite = imageSprite;

            // 添加点击事件
            imageBtn.onClick.AddListener(() => OnImageSelected(imageSprite));

            imageButtons.Add(imageBtn);
        }

        // 重置滚动位置
        ResetScroll:
        if (imageScrollRect != null)
        {
            StartCoroutine(ResetScrollPosition());
        }
        Debug.Log($"[Gallery] SetupImageGrid end: gridChildren={(imageGridParent != null ? imageGridParent.childCount : -1)}, buttonsTracked={imageButtons.Count}");
    }

    private Sprite FindSpriteByName(string name)
    {
        for (int ci = 0; ci < categories.Count; ci++)
        {
            var cat = categories[ci];
            for (int i = 0; i < cat.images.Count; i++)
            {
                var s = cat.images[i];
                if (s != null && s.name == name) return s;
            }
        }
        return null;
    }

    private void OnInProgressItemClicked(string imageId, Sprite sprite)
    {
        Debug.Log($"[Gallery] InProgress item clicked: imageId={imageId}");
        var state = PlayPrefsManager.Instance.LoadPuzzleStateForImage(imageId);
        if (state == null || state.pieces == null || state.pieces.Length == 0)
        {
            // 存档无效，移除索引并刷新
            typeof(PlayPrefsManager).GetMethod("ClearPuzzleStateForImage")?.Invoke(PlayPrefsManager.Instance, new object[] { imageId });
            if (currentCategoryIndex == inProgressIndex)
            {
                SetupImageGrid();
            }
            return;
        }
        int placed = 0;
        for (int i = 0; i < state.pieces.Length; i++) if (state.pieces[i].isPlaced) placed++;
        Debug.Log($"[Gallery] Continue with save: grid={state.gridSize}, placed={placed}/{state.pieces.Length}");
        GameManager.Instance.StartNewGame(sprite, state.gridSize, true);
    }

    private GameObject completedDetailModal;
    private Image completedDetailPreview;
    private TextMeshProUGUI completedDetailText;
    private Button completedDetailDeleteButton;
    private string completedDetailCurrentImageId;

    private void EnsureCompletedDetailModal()
    {
        if (completedDetailModal != null) return;

        roundedUiSprite = RoundedRectSpriteCache.Get(64, 16);
        completedDetailModal = new GameObject("CompletedDetailModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        completedDetailModal.transform.SetParent(transform, false);
        var rt = completedDetailModal.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var bg = completedDetailModal.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(completedDetailModal.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(860f, 1200f);
        var pimg = panel.GetComponent<Image>();
        pimg.color = new Color(252f / 255f, 246f / 255f, 227f / 255f, 1f);
        pimg.sprite = roundedUiSprite;
        pimg.type = Image.Type.Sliced;

        var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        previewGo.transform.SetParent(panel.transform, false);
        completedDetailPreview = previewGo.GetComponent<Image>();
        var prevRt = previewGo.GetComponent<RectTransform>();
        prevRt.anchorMin = new Vector2(0.5f, 1f);
        prevRt.anchorMax = new Vector2(0.5f, 1f);
        prevRt.pivot = new Vector2(0.5f, 1f);
        prevRt.anchoredPosition = new Vector2(0f, -80f);
        prevRt.sizeDelta = new Vector2(720f, 720f);
        completedDetailPreview.preserveAspect = true;

        var textGo = new GameObject("InfoText", typeof(RectTransform), typeof(CanvasRenderer));
        textGo.transform.SetParent(panel.transform, false);
        completedDetailText = textGo.AddComponent<TextMeshProUGUI>();
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0f);
        trt.anchorMax = new Vector2(0.5f, 0f);
        trt.pivot = new Vector2(0.5f, 0f);
        trt.anchoredPosition = new Vector2(0f, 280f);
        trt.sizeDelta = new Vector2(760f, 100f);
        completedDetailText.alignment = TextAlignmentOptions.Center;
        completedDetailText.fontSize = 40;
        completedDetailText.color = new Color(0.15f, 0.15f, 0.2f, 1f);

        var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(panel.transform, false);
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0f, 40f);
        crt.sizeDelta = new Vector2(420f, 90f);
        var closeImg = closeGo.GetComponent<Image>();
        closeImg.color = new Color(0.15f, 0.75f, 0.78f, 1f);
        closeImg.sprite = roundedUiSprite;
        closeImg.type = Image.Type.Sliced;
        var btn = closeGo.GetComponent<Button>();
        btn.onClick.AddListener(() => completedDetailModal.SetActive(false));

        var closeTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        closeTextGo.transform.SetParent(closeGo.transform, false);
        var closeText = closeTextGo.AddComponent<TextMeshProUGUI>();
        closeText.text = "Close";
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.fontSize = 38;
        closeText.color = Color.white;
        var closeTextRt = closeText.rectTransform;
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.offsetMin = Vector2.zero;
        closeTextRt.offsetMax = Vector2.zero;

        var bgBtn = completedDetailModal.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(() => completedDetailModal.SetActive(false));

        var delGo = new GameObject("DeleteButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        delGo.transform.SetParent(panel.transform, false);
        var drt = delGo.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.5f, 0f);
        drt.anchorMax = new Vector2(0.5f, 0f);
        drt.pivot = new Vector2(0.5f, 0f);
        drt.anchoredPosition = new Vector2(0f, 150f);
        drt.sizeDelta = new Vector2(420f, 90f);
        var delImg = delGo.GetComponent<Image>();
        delImg.color = new Color(0.9f, 0.25f, 0.25f, 1f);
        delImg.sprite = roundedUiSprite;
        delImg.type = Image.Type.Sliced;
        completedDetailDeleteButton = delGo.GetComponent<Button>();
        completedDetailDeleteButton.onClick.AddListener(OnCompletedDeleteClicked);

        var delTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        delTextGo.transform.SetParent(delGo.transform, false);
        var delText = delTextGo.AddComponent<TextMeshProUGUI>();
        delText.text = "Delete";
        delText.alignment = TextAlignmentOptions.Center;
        delText.fontSize = 38;
        delText.color = Color.white;
        var delTextRt = delText.rectTransform;
        delTextRt.anchorMin = Vector2.zero;
        delTextRt.anchorMax = Vector2.one;
        delTextRt.offsetMin = Vector2.zero;
        delTextRt.offsetMax = Vector2.zero;

        completedDetailModal.SetActive(false);
    }

    private void ShowCompletedDetail(string imageId, int difficulty, float completionTimeSeconds, Sprite preview)
    {
        EnsureCompletedDetailModal();
        completedDetailCurrentImageId = imageId;
        if (completedDetailPreview != null) completedDetailPreview.sprite = preview;
        if (completedDetailText != null)
        {
            int minutes = Mathf.FloorToInt(completionTimeSeconds / 60f);
            int seconds = Mathf.FloorToInt(completionTimeSeconds % 60f);
            completedDetailText.text = $"Difficulty: {difficulty}x{difficulty}\nTime: {minutes:00}:{seconds:00}";
        }
        completedDetailModal.SetActive(true);
    }

    private void OnCompletedDeleteClicked()
    {
        if (string.IsNullOrEmpty(completedDetailCurrentImageId)) return;
        PlayPrefsManager.Instance.RemoveCompletedPuzzle(completedDetailCurrentImageId);
        completedDetailModal.SetActive(false);
        if (currentCategoryIndex == completedIndex)
        {
            SetupImageGrid();
        }
    }

    private static Sprite addSpriteCache;
    private Sprite GetAddSprite()
    {
        if (addSpriteCache != null) return addSpriteCache;
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color bg = new Color(0.92f, 0.92f, 0.95f, 1f);
        Color plus = new Color(0.2f, 0.2f, 0.25f, 1f);
        var pixels = tex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
        int th = size / 14;
        int len = size / 3;
        int cx = size / 2;
        int cy = size / 2;
        for (int x = cx - len; x <= cx + len; x++)
        {
            for (int t = -th; t <= th; t++)
            {
                int yy = cy + t;
                if (x >= 0 && x < size && yy >= 0 && yy < size) tex.SetPixel(x, yy, plus);
            }
        }
        for (int y = cy - len; y <= cy + len; y++)
        {
            for (int t = -th; t <= th; t++)
            {
                int xx = cx + t;
                if (xx >= 0 && xx < size && y >= 0 && y < size) tex.SetPixel(xx, y, plus);
            }
        }
        tex.Apply();
        addSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        addSpriteCache.name = "AddTileSprite";
        return addSpriteCache;
    }

    /// <summary>
    /// 重置滚动位置
    /// </summary>
    private IEnumerator ResetScrollPosition()
    {
        yield return new WaitForEndOfFrame();
        if (imageScrollRect != null)
        {
            imageScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// 图片被选中时调用
    /// </summary>
    /// <param name="selectedImage">选中的图片</param>
    private void OnImageSelected(Sprite selectedImage)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameManager.GameState.DifficultySelection);
        }
        UIManager.Instance.ShowPage<DifficultyPage>(page =>
        {
            page.SetSelectedImage(selectedImage);
        });
    }

    /// <summary>
    /// 添加图片到指定类别
    /// </summary>
    /// <param name="categoryIndex">类别索引</param>
    /// <param name="image">要添加的图片</param>
    public void AddImageToCategory(int categoryIndex, Sprite image)
    {
        if (categoryIndex >= 0 && categoryIndex < categories.Count && image != null)
        {
            categories[categoryIndex].images.Add(image);

            // 如果当前显示的就是这个类别，刷新显示
            if (categoryIndex == currentCategoryIndex)
            {
                SetupImageGrid();
            }
        }
    }



    /// <summary>
    /// 获取当前类别名称
    /// </summary>
    public string GetCurrentCategoryName()
    {
        if (currentCategoryIndex >= 0 && currentCategoryIndex < categories.Count)
            return categories[currentCategoryIndex].categoryName;
        return "";
    }

    public void UpdateHeaderForCurrentCategory()
    {
        // 在进入 InProgress 页面时显示 InProgressText，其他页面隐藏
        if (InProgressText != null)
        {
            bool show = currentCategoryIndex == inProgressIndex;
            InProgressText.gameObject.SetActive(show);
        }
        // 可选：输出当前类别日志，便于调试
        string categoryName = GetCurrentCategoryName();
        Debug.Log($"[Gallery] Current category: {categoryName} (index={currentCategoryIndex}, userIndex={userCategoryIndex}, inProgressIndex={inProgressIndex})");
    }
}
