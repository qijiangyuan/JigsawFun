using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 图片加载管理器
/// 负责从Resources文件夹动态加载图片资源
/// </summary>
public class ImageLoader : MonoBehaviour
{
    private static ImageLoader _instance;
    public static ImageLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ImageLoader");
                _instance = go.AddComponent<ImageLoader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("图片加载配置")]
    public string basePath = "Images/PuzzleImages"; // Resources下的基础路径

    // 缓存已加载的图片
    private Dictionary<string, List<Sprite>> categoryImageCache = new Dictionary<string, List<Sprite>>();

    // 支持的图片格式
    private readonly string[] supportedFormats = { ".jpg", ".jpeg", ".png", ".bmp", ".tga" };

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 获取指定类别的所有图片
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    /// <returns>该类别下的所有图片</returns>
    public List<Sprite> GetCategoryImages(string categoryName)
    {
        // 如果已经缓存，直接返回
        if (categoryImageCache.ContainsKey(categoryName))
        {
            return categoryImageCache[categoryName];
        }

        // 加载图片
        List<Sprite> images = LoadImagesFromCategory(categoryName);

        // 缓存结果
        categoryImageCache[categoryName] = images;

        return images;
    }

    /// <summary>
    /// 从指定类别文件夹加载所有图片
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    /// <returns>加载的图片列表</returns>
    private List<Sprite> LoadImagesFromCategory(string categoryName)
    {
        List<Sprite> images = new List<Sprite>();
        string categoryPath = $"{basePath}/{categoryName}";

        try
        {
            // 使用Resources.LoadAll加载指定文件夹下的所有Sprite
            Sprite[] loadedSprites = Resources.LoadAll<Sprite>(categoryPath);

            if (loadedSprites != null && loadedSprites.Length > 0)
            {
                images.AddRange(loadedSprites);
                Debug.Log($"成功加载类别 '{categoryName}' 下的 {loadedSprites.Length} 张图片");
            }
            else
            {
                Debug.LogWarning($"在路径 '{categoryPath}' 下未找到任何图片资源");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载类别 '{categoryName}' 的图片时发生错误: {e.Message}");
        }

        return images;
    }

    /// <summary>
    /// 获取所有可用的类别名称
    /// </summary>
    /// <returns>类别名称列表</returns>
    public List<string> GetAvailableCategories()
    {
        List<string> categories = new List<string>();

        // 这里我们使用预定义的类别列表，因为Resources.LoadAll无法直接获取文件夹列表
        // 如果需要动态获取，可以考虑使用StreamingAssets或其他方式
        string[] predefinedCategories = { "Nature", "Cartoon", "Mechanical", "Art", "Fantasy" };

        foreach (string category in predefinedCategories)
        {
            // 检查该类别是否有图片
            if (HasImagesInCategory(category))
            {
                categories.Add(category);
            }
        }

        return categories;
    }

    /// <summary>
    /// 检查指定类别是否包含图片
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    /// <returns>是否包含图片</returns>
    public bool HasImagesInCategory(string categoryName)
    {
        string categoryPath = $"{basePath}/{categoryName}";
        Sprite[] sprites = Resources.LoadAll<Sprite>(categoryPath);
        return sprites != null && sprites.Length > 0;
    }

    /// <summary>
    /// 清除指定类别的缓存
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    public void ClearCategoryCache(string categoryName)
    {
        if (categoryImageCache.ContainsKey(categoryName))
        {
            categoryImageCache.Remove(categoryName);
            Debug.Log($"已清除类别 '{categoryName}' 的缓存");
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearAllCache()
    {
        categoryImageCache.Clear();
        Debug.Log("已清除所有图片缓存");
    }

    /// <summary>
    /// 预加载所有类别的图片
    /// </summary>
    public void PreloadAllCategories()
    {
        List<string> categories = GetAvailableCategories();

        foreach (string category in categories)
        {
            GetCategoryImages(category);
        }

        Debug.Log($"已预加载 {categories.Count} 个类别的图片");
    }

    /// <summary>
    /// 获取缓存信息
    /// </summary>
    /// <returns>缓存信息字符串</returns>
    public string GetCacheInfo()
    {
        int totalImages = 0;
        foreach (var kvp in categoryImageCache)
        {
            totalImages += kvp.Value.Count;
        }

        return $"已缓存 {categoryImageCache.Count} 个类别，共 {totalImages} 张图片";
    }
}