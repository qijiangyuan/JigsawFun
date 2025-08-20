using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GalleryPage : BasePage
{
    [Header("Gallery组件")]
    public ScrollRect categoryScrollRect;           // 类别滚动视图
    public Transform categoryToggleParent;          // 类别按钮父对象
    public Toggle categoryTogglePrefab;            // 类别Toggle预制体
    
    public ScrollRect imageScrollRect;             // 图片滚动视图
    public Transform imageGridParent;              // 图片网格父对象
    public Button imageButtonPrefab;              // 图片按钮预制体
    
    [Header("类别数据")]
    public List<CategoryData> categories = new List<CategoryData>();
    
    private int currentCategoryIndex = 0;
    private List<Toggle> categoryToggles = new List<Toggle>();
    private List<Button> imageButtons = new List<Button>();
    private ToggleGroup categoryToggleGroup;
    
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
        
        // 创建ToggleGroup组件
        categoryToggleGroup = categoryToggleParent.GetComponent<ToggleGroup>();
        if (categoryToggleGroup == null)
        {
            categoryToggleGroup = categoryToggleParent.gameObject.AddComponent<ToggleGroup>();
        }
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
        
        Debug.Log($"初始化完成，共加载 {categories.Count} 个类别");
    }
    
    protected override void OnPageShow()
    {
        base.OnPageShow();
        SetupCategoryButtons();
        ShowCategory(0); // 默认显示第一个类别
    }
    
    /// <summary>
    /// 设置类别Toggle
    /// </summary>
    private void SetupCategoryButtons()
    {
        // 清除现有Toggle
        foreach (Toggle toggle in categoryToggles)
        {
            if (toggle != null)
                DestroyImmediate(toggle.gameObject);
        }
        categoryToggles.Clear();
        
        // 创建类别Toggle
        for (int i = 0; i < categories.Count; i++)
        {
            int categoryIndex = i; // 闭包变量
            
            Toggle categoryToggle = Instantiate(categoryTogglePrefab, categoryToggleParent);
            categoryToggle.gameObject.SetActive(true);
            categoryToggle.group = categoryToggleGroup;
            
            // 设置Toggle文本
            Text toggleText = categoryToggle.GetComponentInChildren<Text>();
            if (toggleText != null)
                toggleText.text = categories[i].categoryName;
            
            // Toggle的图标在场景中直接设置，不需要代码处理
            
            // 添加值改变事件
            categoryToggle.onValueChanged.AddListener((isOn) => {
                if (isOn) ShowCategory(categoryIndex);
            });
            
            categoryToggles.Add(categoryToggle);
        }
        
        // 默认选中第一个Toggle
        if (categoryToggles.Count > 0)
        {
            categoryToggles[0].isOn = true;
        }
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
        UpdateCategoryButtonStates();
        SetupImageGrid();
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
                categoryToggles[i].isOn = (i == currentCategoryIndex);
            }
        }
    }
    
    /// <summary>
    /// 设置图片网格
    /// </summary>
    private void SetupImageGrid()
    {
        // 清除现有图片按钮
        foreach (Button btn in imageButtons)
        {
            if (btn != null)
                DestroyImmediate(btn.gameObject);
        }
        imageButtons.Clear();
        
        // 获取当前类别的图片
        CategoryData currentCategory = categories[currentCategoryIndex];
        
        // 创建图片按钮
        for (int i = 0; i < currentCategory.images.Count; i++)
        {
            Sprite imageSprite = currentCategory.images[i];
            if (imageSprite == null) continue;
            
            Button imageBtn = Instantiate(imageButtonPrefab, imageGridParent);
            imageBtn.gameObject.SetActive(true);
            
            // 设置图片
            Image btnImage = imageBtn.GetComponent<Image>();
            if (btnImage != null)
                btnImage.sprite = imageSprite;
            
            // 添加点击事件
            imageBtn.onClick.AddListener(() => OnImageSelected(imageSprite));
            
            imageButtons.Add(imageBtn);
        }
        
        // 重置滚动位置
        if (imageScrollRect != null)
        {
            StartCoroutine(ResetScrollPosition());
        }
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
        // 更新GameManager状态并跳转到难度选择页面
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameManager.GameState.DifficultySelection);
        }
        
        // 将选中的图片传递给UIManager，然后跳转到难度选择页面
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
}