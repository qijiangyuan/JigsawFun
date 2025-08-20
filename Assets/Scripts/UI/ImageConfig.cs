using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 图片配置文件方案
/// 使用ScriptableObject来配置图片资源
/// </summary>
[CreateAssetMenu(fileName = "ImageConfig", menuName = "Puzzle Game/Image Config")]
public class ImageConfig : ScriptableObject
{
    [Header("图片类别配置")]
    public List<CategoryConfig> categories = new List<CategoryConfig>();
    
    [System.Serializable]
    public class CategoryConfig
    {
        [Header("类别信息")]
        public string categoryName;        // 类别名称
        public Sprite categoryIcon;        // 类别图标
        public Color categoryColor = Color.white; // 类别主题色
        
        [Header("图片资源")]
        public List<Sprite> images = new List<Sprite>();  // 该类别下的图片列表
        
        [Header("加载设置")]
        public bool preloadImages = true;   // 是否预加载图片
        public int maxImagesCount = 50;     // 最大图片数量限制
        
        [Header("显示设置")]
        public bool isEnabled = true;       // 是否启用该类别
        public int sortOrder = 0;           // 排序顺序
    }
    
    /// <summary>
    /// 获取启用的类别列表
    /// </summary>
    /// <returns>启用的类别配置列表</returns>
    public List<CategoryConfig> GetEnabledCategories()
    {
        List<CategoryConfig> enabledCategories = new List<CategoryConfig>();
        
        foreach (CategoryConfig category in categories)
        {
            if (category.isEnabled && category.images.Count > 0)
            {
                enabledCategories.Add(category);
            }
        }
        
        // 按排序顺序排序
        enabledCategories.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        
        return enabledCategories;
    }
    
    /// <summary>
    /// 根据名称获取类别配置
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    /// <returns>类别配置，如果未找到返回null</returns>
    public CategoryConfig GetCategoryByName(string categoryName)
    {
        foreach (CategoryConfig category in categories)
        {
            if (category.categoryName == categoryName)
            {
                return category;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    /// <returns>验证结果</returns>
    public bool ValidateConfig()
    {
        bool isValid = true;
        
        // 检查是否有重复的类别名称
        HashSet<string> categoryNames = new HashSet<string>();
        foreach (CategoryConfig category in categories)
        {
            if (string.IsNullOrEmpty(category.categoryName))
            {
                Debug.LogError("发现空的类别名称");
                isValid = false;
                continue;
            }
            
            if (categoryNames.Contains(category.categoryName))
            {
                Debug.LogError($"发现重复的类别名称: {category.categoryName}");
                isValid = false;
            }
            else
            {
                categoryNames.Add(category.categoryName);
            }
            
            // 检查图片数量
            if (category.images.Count > category.maxImagesCount)
            {
                Debug.LogWarning($"类别 '{category.categoryName}' 的图片数量 ({category.images.Count}) 超过了最大限制 ({category.maxImagesCount})");
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 获取配置统计信息
    /// </summary>
    /// <returns>统计信息字符串</returns>
    public string GetConfigStats()
    {
        int totalCategories = categories.Count;
        int enabledCategories = 0;
        int totalImages = 0;
        
        foreach (CategoryConfig category in categories)
        {
            if (category.isEnabled)
            {
                enabledCategories++;
            }
            totalImages += category.images.Count;
        }
        
        return $"总类别: {totalCategories}, 启用类别: {enabledCategories}, 总图片: {totalImages}";
    }
}

/// <summary>
/// 配置文件管理器
/// 负责加载和管理ImageConfig
/// </summary>
public class ImageConfigManager : MonoBehaviour
{
    private static ImageConfigManager _instance;
    public static ImageConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ImageConfigManager");
                _instance = go.AddComponent<ImageConfigManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    [Header("配置文件")]
    public ImageConfig imageConfig;
    
    [Header("运行时设置")]
    public bool autoLoadConfig = true;
    public string configResourcePath = "ImageConfig"; // Resources文件夹下的配置文件路径
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (autoLoadConfig)
            {
                LoadConfig();
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 加载配置文件
    /// </summary>
    public void LoadConfig()
    {
        if (imageConfig == null)
        {
            imageConfig = Resources.Load<ImageConfig>(configResourcePath);
            
            if (imageConfig == null)
            {
                Debug.LogError($"无法在路径 '{configResourcePath}' 下找到ImageConfig配置文件");
                return;
            }
        }
        
        // 验证配置
        if (imageConfig.ValidateConfig())
        {
            Debug.Log($"配置文件加载成功: {imageConfig.GetConfigStats()}");
        }
        else
        {
            Debug.LogError("配置文件验证失败，请检查配置");
        }
    }
    
    /// <summary>
    /// 获取当前配置
    /// </summary>
    /// <returns>图片配置</returns>
    public ImageConfig GetConfig()
    {
        if (imageConfig == null)
        {
            LoadConfig();
        }
        return imageConfig;
    }
    
    /// <summary>
    /// 获取启用的类别列表
    /// </summary>
    /// <returns>启用的类别配置列表</returns>
    public List<ImageConfig.CategoryConfig> GetEnabledCategories()
    {
        ImageConfig config = GetConfig();
        return config != null ? config.GetEnabledCategories() : new List<ImageConfig.CategoryConfig>();
    }
}