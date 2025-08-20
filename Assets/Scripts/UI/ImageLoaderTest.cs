using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ImageLoader测试脚本
/// 用于测试和验证ImageLoader的功能
/// </summary>
public class ImageLoaderTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool testOnStart = true;
    public bool showDetailedLog = true;
    
    [Header("测试结果显示")]
    public UnityEngine.UI.Text resultText; // 可选：用于显示测试结果的UI文本
    
    private void Start()
    {
        if (testOnStart)
        {
            StartCoroutine(TestImageLoader());
        }
    }
    
    private void Update()
    {
        // 按键测试
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(TestImageLoader());
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            TestClearCache();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            TestPreloadAll();
        }
        
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowCacheInfo();
        }
    }
    
    /// <summary>
    /// 测试ImageLoader的主要功能
    /// </summary>
    private System.Collections.IEnumerator TestImageLoader()
    {
        Debug.Log("=== ImageLoader 功能测试开始 ===");
        
        // 等待一帧确保ImageLoader初始化完成
        yield return null;
        
        // 测试1：获取可用类别
        TestGetAvailableCategories();
        
        yield return new WaitForSeconds(0.1f);
        
        // 测试2：加载各个类别的图片
        TestLoadCategoryImages();
        
        yield return new WaitForSeconds(0.1f);
        
        // 测试3：测试缓存功能
        TestCacheFunction();
        
        yield return new WaitForSeconds(0.1f);
        
        // 测试4：显示统计信息
        ShowCacheInfo();
        
        Debug.Log("=== ImageLoader 功能测试完成 ===");
        
        // 更新UI显示
        UpdateResultDisplay();
    }
    
    /// <summary>
    /// 测试获取可用类别
    /// </summary>
    private void TestGetAvailableCategories()
    {
        Debug.Log("--- 测试：获取可用类别 ---");
        
        List<string> categories = ImageLoader.Instance.GetAvailableCategories();
        
        Debug.Log($"找到 {categories.Count} 个可用类别:");
        
        foreach (string category in categories)
        {
            Debug.Log($"  - {category}");
        }
        
        if (categories.Count == 0)
        {
            Debug.LogWarning("未找到任何可用类别，请检查Resources/Images/PuzzleImages文件夹结构");
        }
    }
    
    /// <summary>
    /// 测试加载类别图片
    /// </summary>
    private void TestLoadCategoryImages()
    {
        Debug.Log("--- 测试：加载类别图片 ---");
        
        List<string> categories = ImageLoader.Instance.GetAvailableCategories();
        
        foreach (string category in categories)
        {
            List<Sprite> images = ImageLoader.Instance.GetCategoryImages(category);
            
            if (showDetailedLog)
            {
                Debug.Log($"类别 '{category}': {images.Count} 张图片");
                
                if (images.Count > 0)
                {
                    for (int i = 0; i < Mathf.Min(images.Count, 3); i++) // 只显示前3张图片的信息
                    {
                        if (images[i] != null)
                        {
                            Debug.Log($"  - 图片 {i + 1}: {images[i].name} ({images[i].texture.width}x{images[i].texture.height})");
                        }
                    }
                    
                    if (images.Count > 3)
                    {
                        Debug.Log($"  - ... 还有 {images.Count - 3} 张图片");
                    }
                }
            }
            else
            {
                Debug.Log($"类别 '{category}': {images.Count} 张图片");
            }
        }
    }
    
    /// <summary>
    /// 测试缓存功能
    /// </summary>
    private void TestCacheFunction()
    {
        Debug.Log("--- 测试：缓存功能 ---");
        
        List<string> categories = ImageLoader.Instance.GetAvailableCategories();
        
        if (categories.Count > 0)
        {
            string testCategory = categories[0];
            
            // 第一次加载（应该从Resources加载）
            float startTime = Time.realtimeSinceStartup;
            List<Sprite> images1 = ImageLoader.Instance.GetCategoryImages(testCategory);
            float loadTime1 = Time.realtimeSinceStartup - startTime;
            
            // 第二次加载（应该从缓存加载）
            startTime = Time.realtimeSinceStartup;
            List<Sprite> images2 = ImageLoader.Instance.GetCategoryImages(testCategory);
            float loadTime2 = Time.realtimeSinceStartup - startTime;
            
            Debug.Log($"缓存测试结果:");
            Debug.Log($"  首次加载时间: {loadTime1 * 1000:F2}ms");
            Debug.Log($"  缓存加载时间: {loadTime2 * 1000:F2}ms");
            Debug.Log($"  性能提升: {(loadTime1 / loadTime2):F1}x");
            Debug.Log($"  图片数量一致: {images1.Count == images2.Count}");
        }
    }
    
    /// <summary>
    /// 测试清除缓存
    /// </summary>
    private void TestClearCache()
    {
        Debug.Log("--- 测试：清除缓存 ---");
        
        ImageLoader.Instance.ClearAllCache();
        Debug.Log("已清除所有缓存");
        
        ShowCacheInfo();
    }
    
    /// <summary>
    /// 测试预加载所有类别
    /// </summary>
    private void TestPreloadAll()
    {
        Debug.Log("--- 测试：预加载所有类别 ---");
        
        float startTime = Time.realtimeSinceStartup;
        ImageLoader.Instance.PreloadAllCategories();
        float loadTime = Time.realtimeSinceStartup - startTime;
        
        Debug.Log($"预加载完成，耗时: {loadTime * 1000:F2}ms");
        
        ShowCacheInfo();
    }
    
    /// <summary>
    /// 显示缓存信息
    /// </summary>
    private void ShowCacheInfo()
    {
        string cacheInfo = ImageLoader.Instance.GetCacheInfo();
        Debug.Log($"缓存信息: {cacheInfo}");
    }
    
    /// <summary>
    /// 更新结果显示
    /// </summary>
    private void UpdateResultDisplay()
    {
        if (resultText != null)
        {
            List<string> categories = ImageLoader.Instance.GetAvailableCategories();
            string cacheInfo = ImageLoader.Instance.GetCacheInfo();
            
            resultText.text = $"ImageLoader 测试结果:\n" +
                             $"可用类别: {categories.Count}\n" +
                             $"{cacheInfo}\n\n" +
                             $"按键说明:\n" +
                             $"T - 运行测试\n" +
                             $"C - 清除缓存\n" +
                             $"P - 预加载所有\n" +
                             $"I - 显示缓存信息";
        }
    }
    
    /// <summary>
    /// 在Inspector中显示帮助信息
    /// </summary>
    private void OnValidate()
    {
        // 这个方法在Inspector中值改变时调用，可以用来显示帮助信息
    }
    
    private void OnGUI()
    {
        // 在屏幕上显示简单的测试按钮（仅在开发模式下）
        if (Application.isEditor || Debug.isDebugBuild)
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 150));
            GUILayout.Label("ImageLoader 测试");
            
            if (GUILayout.Button("运行测试 (T)"))
            {
                StartCoroutine(TestImageLoader());
            }
            
            if (GUILayout.Button("清除缓存 (C)"))
            {
                TestClearCache();
            }
            
            if (GUILayout.Button("预加载所有 (P)"))
            {
                TestPreloadAll();
            }
            
            if (GUILayout.Button("显示缓存信息 (I)"))
            {
                ShowCacheInfo();
            }
            
            GUILayout.EndArea();
        }
    }
}