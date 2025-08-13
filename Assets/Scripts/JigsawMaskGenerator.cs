using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 用于JSON序列化的mask数据类
/// </summary>
[System.Serializable]
public class MaskPieceData
{
    public int row;
    public int col;
    public string fileName;
    public MaskPiece.EdgeType topEdge;
    public MaskPiece.EdgeType rightEdge;
    public MaskPiece.EdgeType bottomEdge;
    public MaskPiece.EdgeType leftEdge;
    
    public MaskPieceData(int row, int col, string fileName, MaskPiece maskPiece)
    {
        this.row = row;
        this.col = col;
        this.fileName = fileName;
        this.topEdge = maskPiece.TopEdge;
        this.rightEdge = maskPiece.RightEdge;
        this.bottomEdge = maskPiece.BottomEdge;
        this.leftEdge = maskPiece.LeftEdge;
    }
}

/// <summary>
/// 用于JSON序列化的拼图布局数据类
/// </summary>
[System.Serializable]
public class JigsawLayoutData
{
    public int gridSize;
    public List<MaskPieceData> pieces;
    
    public JigsawLayoutData(int gridSize)
    {
        this.gridSize = gridSize;
        this.pieces = new List<MaskPieceData>();
    }
}

public class JigsawMaskGenerator : MonoBehaviour
{
    public FilterMode filterMode = FilterMode.Bilinear; // 采样模式：双线性(性能好) vs 点采样(质量高)
    public int maskSize = 512; // 生成的mask尺寸
    public Texture2D semicircleTemplate;   // 半圆模板图片（圆弧在上，平边在下）
    

    void Start()
    {

    }
    
    /// <summary>
    /// 生成上边和右边凸出的测试Mask
    /// </summary>
    [ContextMenu("生成四面凸出测试Mask")]
    public void GenerateAllProtrudingTestMask()
    {
        Debug.Log("开始生成四面凸出的测试Mask...");
        
        // 检查半圆模板
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！请在Inspector中指定semicircleTemplate。");
            return;
        }
        else if (semicircleTemplate.width != maskSize || semicircleTemplate.height >= maskSize)
        {
            Debug.LogError($"半圆模板尺寸不匹配！当前: {semicircleTemplate.width}x{semicircleTemplate.height}, 期望: 宽度{maskSize}，高度小于{maskSize}，请提供正确尺寸的半圆模板。");
            return;
        }
        
        try
        {
            // 生成四面凸出的mask：[上, 右, 下, 左] = [true, true, true, true]
            Texture2D mask = GenerateAllProtrudingMask(maskSize, new bool[] { true, true, true, true }, semicircleTemplate);
            
            if (mask == null)
            {
                Debug.LogError("生成四面凸出Mask失败！");
                return;
            }
            
            // 保存为PNG文件
            byte[] pngData = mask.EncodeToPNG();
            string fileName = "TestMask_AllSidesProtrude.png";
            string filePath = System.IO.Path.Combine(Application.dataPath, "..", fileName);
            System.IO.File.WriteAllBytes(filePath, pngData);
            
            Debug.Log($"生成四面凸出测试Mask: {fileName}");
            
            // 清理临时纹理
            DestroyImmediate(mask);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成四面凸出Mask时发生错误: {e.Message}");
            Debug.LogError($"错误堆栈: {e.StackTrace}");
        }
        
        Debug.Log("四面凸出测试Mask生成完成！");
    }
    
    [ContextMenu("生成四面凹陷测试Mask")]
    public void GenerateFourSideConcaveTestMask()
    {
        Debug.Log("开始生成四面凹陷的测试Mask...");
        
        // 检查半圆模板
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！请在Inspector中指定semicircleTemplate。");
            return;
        }
        else if (semicircleTemplate.width != maskSize || semicircleTemplate.height >= maskSize)
        {
            Debug.LogError($"半圆模板尺寸不匹配！当前: {semicircleTemplate.width}x{semicircleTemplate.height}, 期望: 宽度{maskSize}，高度小于{maskSize}，请提供正确尺寸的半圆模板。");
            return;
        }
        
        try
        {
            // 生成四面凹陷的mask
            Texture2D mask = GenerateFourSideConcaveMask(maskSize, semicircleTemplate);
            
            if (mask == null)
            {
                Debug.LogError("生成四面凹陷Mask失败！");
                return;
            }
            
            // 保存为PNG文件
            byte[] pngData = mask.EncodeToPNG();
            string fileName = "TestMask_FourSideConcave.png";
            string filePath = System.IO.Path.Combine(Application.dataPath, "..", fileName);
            System.IO.File.WriteAllBytes(filePath, pngData);
            
            Debug.Log($"生成四面凹陷测试Mask: {fileName}，尺寸: {mask.width}x{mask.height}");
            
            // 清理临时纹理
            DestroyImmediate(mask);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成四面凹陷Mask时发生错误: {e.Message}");
            Debug.LogError($"错误堆栈: {e.StackTrace}");
        }
        
        Debug.Log("四面凹陷测试Mask生成完成！");
    }

    [ContextMenu("生成2x2拼图Masks")]
    public void Generate2x2JigsawMasks()
    {
        Debug.Log("开始生成2x2拼图Masks（使用新的GenerateMaskPieces方法）");
        
        // 直接调用新的GenerateMaskPieces方法
        GenerateMaskPieces(2);
        
        Debug.Log("2x2拼图masks生成完成！");
    }

    [ContextMenu("测试单个拼图块凹凸")]
    public void TestSinglePieceProtrusion()
    {
        Debug.Log("开始测试单个拼图块凹凸效果");
        
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板未设置！");
            return;
        }
        
        // 测试(0,0)块：右边凸出，下边凹进
        GenerateTestPieceMask(0, 0, false, true, false, false, "test_0_0_right_protrude_down_concave");
        
        // 测试(1,0)块：左边凹进，下边凸出
        GenerateTestPieceMask(1, 0, false, false, true, false, "test_1_0_left_concave_down_protrude");
        
        Debug.Log("测试完成！");
    }
    
    [ContextMenu("测试单个方向凹凸")]
    public void TestSingleDirectionEffect()
    {
        Debug.Log("开始测试单个方向凹凸效果");
        
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板未设置！");
            return;
        }
        
        // 测试1：只有右边凸出
        GenerateSimpleTestMask("only_right_protrude", 1, true);
        
        // 测试2：只有下边凹进
        GenerateSimpleTestMask("only_down_concave", 2, false);
        
        // 测试3：只有上边凹陷
        GenerateSimpleTestMask("only_top_concave", 0, false);
        
        // 测试4：右边凸出 + 下边凹进（旧版本 - 固定尺寸）
        GenerateComplexTestMask("right_protrude_down_concave");
        
        // 测试5：复合效果V2（新版本 - 动态尺寸）
        GenerateComplexTestMaskV2("complex_v2_top_concave_right_protrude", true, true, false, false);
        GenerateComplexTestMaskV2("complex_v2_right_protrude_bottom_concave", false, true, true, false);
        GenerateComplexTestMaskV2("complex_v2_all_effects", true, true, true, true);
        
        Debug.Log("单个方向测试完成！");
    }
    
    [ContextMenu("调试ApplySemicircleV2Dynamic")]
    public void DebugApplySemicircleV2Dynamic()
    {
        Debug.Log("开始调试ApplySemicircleV2Dynamic方法");
        
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板未设置！");
            return;
        }
        
        Debug.Log($"半圆模板尺寸: {semicircleTemplate.width}x{semicircleTemplate.height}");
        
        // 测试1：右边凹陷效果（固定尺寸）
        Debug.Log("测试右边凹陷效果...");
        Texture2D testMask = new Texture2D(maskSize, maskSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[maskSize * maskSize];
        
        // 初始化为白色
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        // 测试右边凹陷 (direction=1, isProtrude=false)
        ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 1, false, 0, 0, maskSize);
        
        testMask.SetPixels(pixels);
        testMask.Apply();
        
        // 保存测试结果
        byte[] pngData = testMask.EncodeToPNG();
        string targetDir = System.IO.Path.Combine(Application.dataPath, "Images", "2x2");
        if (!System.IO.Directory.Exists(targetDir))
        {
            System.IO.Directory.CreateDirectory(targetDir);
        }
        string filePath = System.IO.Path.Combine(targetDir, "debug_right_concave.png");
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"调试图片已保存: {filePath}");
        DestroyImmediate(testMask);
        
        // 测试2：下边凸出效果（动态尺寸）
        Debug.Log("测试下边凸出效果（动态尺寸）...");
        
        // 为下边凸出计算动态尺寸
        int dynamicWidth = maskSize;
        int dynamicHeight = maskSize + semicircleTemplate.height; // 增加半圆模板的高度
        int baseStartX = 0;
        int baseStartY = semicircleTemplate.height; // 基础正方形上移，为下边凸出留空间
        
        Debug.Log($"动态mask尺寸: {dynamicWidth}x{dynamicHeight}");
        Debug.Log($"基础正方形位置: ({baseStartX}, {baseStartY})");
        
        testMask = new Texture2D(dynamicWidth, dynamicHeight, TextureFormat.RGBA32, false);
        Color[] dynamicPixels = new Color[dynamicWidth * dynamicHeight];
        
        // 初始化为透明
        for (int i = 0; i < dynamicPixels.Length; i++)
        {
            dynamicPixels[i] = Color.clear;
        }
        
        // 绘制基础正方形（512x512）在正确位置
        for (int y = baseStartY; y < baseStartY + maskSize; y++)
        {
            for (int x = baseStartX; x < baseStartX + maskSize; x++)
            {
                if (x >= 0 && x < dynamicWidth && y >= 0 && y < dynamicHeight)
                {
                    int index = y * dynamicWidth + x;
                    dynamicPixels[index] = Color.white;
                }
            }
        }
        
        // 测试下边凸出 (direction=2, isProtrude=true)
        ApplySemicircleV2Dynamic(dynamicPixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, 2, true, baseStartX, baseStartY, maskSize);
        
        testMask.SetPixels(dynamicPixels);
        testMask.Apply();
        
        // 保存测试结果
        pngData = testMask.EncodeToPNG();
        filePath = System.IO.Path.Combine(targetDir, "debug_bottom_protrude.png");
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"调试图片已保存: {filePath} (尺寸: {dynamicWidth}x{dynamicHeight})");
        DestroyImmediate(testMask);
        
        // 测试3：右边凸出效果（动态尺寸）
        Debug.Log("测试右边凸出效果（动态尺寸）...");
        
        // 为右边凸出计算动态尺寸
        dynamicWidth = maskSize + semicircleTemplate.height; // 增加半圆模板的高度
        dynamicHeight = maskSize;
        baseStartX = 0; // 基础正方形位置不变，凸出部分在右边
        baseStartY = 0;
        
        Debug.Log($"动态mask尺寸: {dynamicWidth}x{dynamicHeight}");
        Debug.Log($"基础正方形位置: ({baseStartX}, {baseStartY})");
        
        testMask = new Texture2D(dynamicWidth, dynamicHeight, TextureFormat.RGBA32, false);
        dynamicPixels = new Color[dynamicWidth * dynamicHeight];
        
        // 初始化为透明
        for (int i = 0; i < dynamicPixels.Length; i++)
        {
            dynamicPixels[i] = Color.clear;
        }
        
        // 绘制基础正方形（512x512）在正确位置
        for (int y = baseStartY; y < baseStartY + maskSize; y++)
        {
            for (int x = baseStartX; x < baseStartX + maskSize; x++)
            {
                if (x >= 0 && x < dynamicWidth && y >= 0 && y < dynamicHeight)
                {
                    int index = y * dynamicWidth + x;
                    dynamicPixels[index] = Color.white;
                }
            }
        }
        
        // 测试右边凸出 (direction=1, isProtrude=true)
        ApplySemicircleV2Dynamic(dynamicPixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, 1, true, baseStartX, baseStartY, maskSize);
        
        testMask.SetPixels(dynamicPixels);
        testMask.Apply();
        
        // 保存测试结果
        pngData = testMask.EncodeToPNG();
        filePath = System.IO.Path.Combine(targetDir, "debug_right_protrude.png");
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"调试图片已保存: {filePath} (尺寸: {dynamicWidth}x{dynamicHeight})");
        DestroyImmediate(testMask);
        
        Debug.Log("ApplySemicircleV2Dynamic调试完成！");
    }
    
    void GenerateSimpleTestMask(string fileName, int direction, bool isProtrude)
    {
        string dirName = direction == 0 ? "上" : direction == 1 ? "右" : direction == 2 ? "下" : "左";
        string effectName = isProtrude ? "凸出" : "凹进";
        Debug.Log($"生成测试：{dirName}边{effectName}");
        
        // 计算动态尺寸 - 如果是凸出，需要增加半圆模板的尺寸
        // 半圆模板尺寸：512x258（宽度512，高度258）
        int dynamicWidth = maskSize;
        int dynamicHeight = maskSize;
        
        // 计算基础正方形在动态mask中的起始位置
        int baseStartX = 0;
        int baseStartY = 0;
        
        if (isProtrude)
        {
            if (direction == 1) // 右边凸出
            {
                dynamicWidth += semicircleTemplate.height;  // 增加半圆模板的高度（258）
                // 基础正方形位置不变，凸出部分在右边
            }
            else if (direction == 2) // 下边凸出
            {
                dynamicHeight += semicircleTemplate.height; // 增加半圆模板的高度（258）
                // 基础正方形位置不变，凸出部分在下边
            }
            else if (direction == 0) // 上边凸出
            {
                dynamicHeight += semicircleTemplate.height; // 增加半圆模板的高度（258）
                baseStartY = semicircleTemplate.height; // 基础正方形下移，为上边凸出留空间
            }
            else if (direction == 3) // 左边凸出
            {
                dynamicWidth += semicircleTemplate.height;  // 增加半圆模板的高度（258）
                baseStartX = semicircleTemplate.height; // 基础正方形右移，为左边凸出留空间
            }
        }
        // 凹陷效果不需要扩展mask尺寸，在基础正方形内部创建凹陷
        // 所以对于凹陷，dynamicWidth和dynamicHeight保持为maskSize，baseStartX和baseStartY保持为0
        
        Debug.Log($"动态mask尺寸: {dynamicWidth}x{dynamicHeight}");
        Debug.Log($"基础正方形位置: ({baseStartX}, {baseStartY})");
        
        Texture2D mask = new Texture2D(dynamicWidth, dynamicHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[dynamicWidth * dynamicHeight];
        
        // 初始化为透明
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // 绘制基础正方形（512x512）在正确位置
        for (int y = baseStartY; y < baseStartY + maskSize; y++)
        {
            for (int x = baseStartX; x < baseStartX + maskSize; x++)
            {
                if (x >= 0 && x < dynamicWidth && y >= 0 && y < dynamicHeight)
                {
                    int index = y * dynamicWidth + x;
                    pixels[index] = Color.white;
                }
            }
        }
        
        // 应用单个方向的效果
        ApplySemicircleV2Dynamic(pixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, direction, isProtrude, baseStartX, baseStartY, maskSize);
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        // 保存测试文件
        byte[] pngData = mask.EncodeToPNG();
        string targetDir = System.IO.Path.Combine(Application.dataPath, "Images", "2x2");
        string filePath = System.IO.Path.Combine(targetDir, fileName + ".png");
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"保存测试文件: {filePath} (尺寸: {dynamicWidth}x{dynamicHeight})");
        
        DestroyImmediate(mask);
    }
    
    void GenerateComplexTestMask(string fileName)
    {
        Debug.Log($"生成复合测试：右边凸出 + 下边凹进");
        
        // 计算动态尺寸 - 右边凸出需要增加宽度
        int dynamicWidth = maskSize + semicircleTemplate.height;  // 增加半圆模板的高度（258）
        int dynamicHeight = maskSize;
        
        // 基础正方形位置不变，凸出部分在右边
        int baseStartX = 0;
        int baseStartY = 0;
        
        Debug.Log($"动态mask尺寸: {dynamicWidth}x{dynamicHeight}");
        Debug.Log($"基础正方形位置: ({baseStartX}, {baseStartY})");
        
        Texture2D mask = new Texture2D(dynamicWidth, dynamicHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[dynamicWidth * dynamicHeight];
        
        // 初始化为透明
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // 绘制基础正方形（512x512）在正确位置
        for (int y = baseStartY; y < baseStartY + maskSize; y++)
        {
            for (int x = baseStartX; x < baseStartX + maskSize; x++)
            {
                if (x >= 0 && x < dynamicWidth && y >= 0 && y < dynamicHeight)
                {
                    int index = y * dynamicWidth + x;
                    pixels[index] = Color.white;
                }
            }
        }
        
        // 先应用右边凸出
        Debug.Log("应用右边凸出...");
        ApplySemicircleV2Dynamic(pixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, 1, true, baseStartX, baseStartY, maskSize);
        
        // 再应用下边凹进
        Debug.Log("应用下边凹进...");
        ApplySemicircleV2Dynamic(pixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, 2, false, baseStartX, baseStartY, maskSize);
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        // 保存测试文件
        byte[] pngData = mask.EncodeToPNG();
        string targetDir = System.IO.Path.Combine(Application.dataPath, "Images", "2x2");
        string filePath = System.IO.Path.Combine(targetDir, fileName + ".png");
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"保存复合测试文件: {filePath} (尺寸: {dynamicWidth}x{dynamicHeight})");
        
        DestroyImmediate(mask);
    }
    
    /// <summary>
    /// 生成复合效果测试Mask（使用动态尺寸）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="topConcave">上边是否凹陷</param>
    /// <param name="rightProtrude">右边是否凸出</param>
    /// <param name="bottomConcave">下边是否凹陷</param>
    /// <param name="leftProtrude">左边是否凸出</param>
    void GenerateComplexTestMaskV2(string fileName, bool topConcave, bool rightProtrude, bool bottomConcave, bool leftProtrude)
    {
        Debug.Log($"生成复合测试V2：上边凹陷={topConcave}, 右边凸出={rightProtrude}, 下边凹陷={bottomConcave}, 左边凸出={leftProtrude}");
        
        // 计算动态尺寸
        int dynamicWidth = maskSize;
        int dynamicHeight = maskSize;
        
        // 根据凸出方向增加尺寸
        if (rightProtrude) dynamicWidth += semicircleTemplate.height;  // 右边凸出增加宽度
        if (leftProtrude) dynamicWidth += semicircleTemplate.height;   // 左边凸出增加宽度
        if (topConcave) { /* 上边凹陷不需要额外空间 */ }
        if (bottomConcave) { /* 下边凹陷不需要额外空间 */ }
        
        // 计算基础正方形在动态mask中的起始位置
        int baseStartX = leftProtrude ? semicircleTemplate.height : 0;  // 如果左边凸出，基础正方形右移
        int baseStartY = 0;  // 上下凹陷不影响基础正方形位置
        
        Debug.Log($"动态mask尺寸: {dynamicWidth}x{dynamicHeight}");
        Debug.Log($"基础正方形位置: ({baseStartX}, {baseStartY})");
        
        Texture2D mask = new Texture2D(dynamicWidth, dynamicHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[dynamicWidth * dynamicHeight];
        
        // 初始化为透明
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // 绘制基础正方形（512x512）在正确位置
        for (int y = baseStartY; y < baseStartY + maskSize; y++)
        {
            for (int x = baseStartX; x < baseStartX + maskSize; x++)
            {
                if (x >= 0 && x < dynamicWidth && y >= 0 && y < dynamicHeight)
                {
                    int index = y * dynamicWidth + x;
                    pixels[index] = Color.white;
                }
            }
        }
        
        // 应用各个方向的效果
        if (topConcave)
        {
            Debug.Log("应用上边凹陷...");
            ApplySemicircleV2Dynamic(pixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, 0, false, baseStartX, baseStartY, maskSize);
        }
        
        if (rightProtrude)
        {
            Debug.Log("应用右边凸出...");
            ApplySemicircleV2Dynamic(pixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, 1, true, baseStartX, baseStartY, maskSize);
        }
        
        if (bottomConcave)
        {
            Debug.Log("应用下边凹陷...");
            ApplySemicircleV2Dynamic(pixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, 2, false, baseStartX, baseStartY, maskSize);
        }
        
        if (leftProtrude)
        {
            Debug.Log("应用左边凸出...");
            ApplySemicircleV2Dynamic(pixels, dynamicWidth, dynamicHeight, semicircleTemplate.width, semicircleTemplate.height, 3, true, baseStartX, baseStartY, maskSize);
        }
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        // 保存测试文件
        byte[] pngData = mask.EncodeToPNG();
        string targetDir = System.IO.Path.Combine(Application.dataPath, "Images", "2x2");
        string filePath = System.IO.Path.Combine(targetDir, fileName + ".png");
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"保存复合测试文件V2: {filePath} (尺寸: {dynamicWidth}x{dynamicHeight})");
        
        DestroyImmediate(mask);
    }
    
    void GenerateTestPieceMask(int x, int y, bool protrudeUp, bool protrudeRight, bool protrudeDown, bool protrudeLeft, string fileName)
    {
        Debug.Log($"生成测试拼图块({x},{y}) - 上:{protrudeUp}, 右:{protrudeRight}, 下:{protrudeDown}, 左:{protrudeLeft}");
        
        Texture2D mask = new Texture2D(maskSize, maskSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[maskSize * maskSize];
        
        // 初始化为白色（基础正方形）
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        // 仅应用内部边缘的凹凸
        if (protrudeRight) // 右边凸出
        {
            Debug.Log($"应用右边凸出效果");
            ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 1, true, 0, 0, maskSize);
        }
        
        if (protrudeDown) // 下边凸出
        {
            Debug.Log($"应用下边凸出效果");
            ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 2, true, 0, 0, maskSize);
        }
        
        if (!protrudeRight && x < 1) // 右边凹进（仅当不是右边界时）
        {
            Debug.Log($"应用右边凹进效果");
            ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 1, false, 0, 0, maskSize);
        }
        
        if (!protrudeDown && y < 1) // 下边凹进（仅当不是下边界时）
        {
            Debug.Log($"应用下边凹进效果");
            ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 2, false, 0, 0, maskSize);
        }
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        // 保存测试文件
        byte[] pngData = mask.EncodeToPNG();
        string targetDir = System.IO.Path.Combine(Application.dataPath, "Images", "2x2");
        string filePath = System.IO.Path.Combine(targetDir, fileName + ".png");
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"保存测试文件: {filePath}");
        
        DestroyImmediate(mask);
    }

    /// <summary>
    /// 生成指定规格的拼图masks
    /// </summary>
    /// <param name="cols">列数</param>
    /// <param name="rows">行数</param>
    void GenerateJigsawMasks(int cols, int rows)
    {
        Debug.Log($"生成{cols}x{rows}拼图masks");
        
        // 为每个拼图块生成mask
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Debug.Log($"生成拼图块({x},{y})");
                
                // 确定每个方向是否突出
                bool protrudeUp = ShouldProtrude(x, y, 0, cols, rows);
                bool protrudeRight = ShouldProtrude(x, y, 1, cols, rows);
                bool protrudeDown = ShouldProtrude(x, y, 2, cols, rows);
                bool protrudeLeft = ShouldProtrude(x, y, 3, cols, rows);
                
                Debug.Log($"拼图块({x},{y}) - 上:{protrudeUp}, 右:{protrudeRight}, 下:{protrudeDown}, 左:{protrudeLeft}");
                
                // 生成该块的mask
                GenerateJigsawPieceMask(x, y, cols, rows, protrudeUp, protrudeRight, protrudeDown, protrudeLeft);
            }
        }
        
        Debug.Log($"{cols}x{rows}拼图masks生成完成！");
    }

    /// <summary>
    /// 生成单个拼图块的mask
    /// </summary>
    /// <param name="x">拼图块列索引</param>
    /// <param name="y">拼图块行索引</param>
    /// <param name="cols">总列数</param>
    /// <param name="rows">总行数</param>
    /// <param name="protrudeUp">上边是否突出</param>
    /// <param name="protrudeRight">右边是否突出</param>
    /// <param name="protrudeDown">下边是否突出</param>
    /// <param name="protrudeLeft">左边是否突出</param>
    void GenerateJigsawPieceMask(int x, int y, int cols, int rows, bool protrudeUp, bool protrudeRight, bool protrudeDown, bool protrudeLeft)
    {
        Debug.Log($"生成拼图块({x},{y})的mask");
        
        Texture2D mask = new Texture2D(maskSize, maskSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[maskSize * maskSize];
        
        // 初始化为白色（基础正方形）
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        // 判断每个边是否为边界
        bool isTopBoundary = (y == 0);
        bool isRightBoundary = (x == cols - 1);
        bool isBottomBoundary = (y == rows - 1);
        bool isLeftBoundary = (x == 0);
        
        // 应用四个方向的凹凸
        // 上边 - 只有当不是边界时才应用凹凸
        if (!isTopBoundary)
        {
            ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 0, protrudeUp, 0, 0, maskSize);
            Debug.Log($"拼图块({x},{y}) 应用上边凹凸: {protrudeUp}");
        }
        else
        {
            Debug.Log($"拼图块({x},{y}) 跳过上边凹凸: 边界保持平整");
        }
        
        // 右边 - 只有当不是边界时才应用凹凸
        if (!isRightBoundary)
        {
            ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 1, protrudeRight, 0, 0, maskSize);
            Debug.Log($"拼图块({x},{y}) 应用右边凹凸: {protrudeRight}");
        }
        else
        {
            Debug.Log($"拼图块({x},{y}) 跳过右边凹凸: 边界保持平整");
        }
        
        // 下边 - 只有当不是边界时才应用凹凸
        if (!isBottomBoundary)
        {
            ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 2, protrudeDown, 0, 0, maskSize);
            Debug.Log($"拼图块({x},{y}) 应用下边凹凸: {protrudeDown}");
        }
        else
        {
            Debug.Log($"拼图块({x},{y}) 跳过下边凹凸: 边界保持平整");
        }
        
        // 左边 - 只有当不是边界时才应用凹凸
        if (!isLeftBoundary)
        {
            ApplySemicircleV2Dynamic(pixels, maskSize, maskSize, semicircleTemplate.width, semicircleTemplate.height, 3, protrudeLeft, 0, 0, maskSize);
            Debug.Log($"拼图块({x},{y}) 应用左边凹凸: {protrudeLeft}");
        }
        else
        {
            Debug.Log($"拼图块({x},{y}) 跳过左边凹凸: 边界保持平整");
        }
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        // 保存为PNG文件到Assets/Images/2x2文件夹
        byte[] pngData = mask.EncodeToPNG();
        string fileName = $"jigsaw_{x}_{y}.png";
        
        // 确保目标文件夹存在
        string targetDir = System.IO.Path.Combine(Application.dataPath, "Images", "2x2");
        if (!System.IO.Directory.Exists(targetDir))
        {
            System.IO.Directory.CreateDirectory(targetDir);
            Debug.Log($"创建文件夹: {targetDir}");
        }
        
        string filePath = System.IO.Path.Combine(targetDir, fileName);
        System.IO.File.WriteAllBytes(filePath, pngData);
        
        Debug.Log($"保存拼图块({x},{y})mask: {filePath}，尺寸: {mask.width}x{mask.height}");
        
        // 清理临时纹理
        DestroyImmediate(mask);
    }
    
    /// <summary>
    /// 生成四面凹陷的Mask
    /// </summary>
    /// <param name="baseSize">基础正方形尺寸</param>
    /// <param name="semicircleTemplate">半圆模板</param>
    /// <returns>生成的Mask纹理</returns>
    public Texture2D GenerateFourSideConcaveMask(int baseSize, Texture2D semicircleTemplate)
    {
        if (semicircleTemplate == null)
        {
            Debug.LogError("semicircleTemplate 不能为空");
            return null;
        }
        
        Debug.Log($"生成四面凹陷Mask，基础尺寸: {baseSize}x{baseSize}");
        Debug.Log($"半圆模板: {semicircleTemplate.width}x{semicircleTemplate.height}");
        
        Texture2D mask = new Texture2D(baseSize, baseSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[baseSize * baseSize];
        
        // 初始化为白色（基础正方形）
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        // 应用四个方向的凹陷
        Debug.Log("开始应用四个方向的凹陷...");
        
        // 上边凹陷
        Debug.Log("应用上边凹陷...");
        ApplySemicircleV2Dynamic(pixels, baseSize, baseSize, semicircleTemplate.width, semicircleTemplate.height, 0, false, 0, 0, baseSize);
        
        // 右边凹陷
        Debug.Log("应用右边凹陷...");
        ApplySemicircleV2Dynamic(pixels, baseSize, baseSize, semicircleTemplate.width, semicircleTemplate.height, 1, false, 0, 0, baseSize);
        
        // 下边凹陷
        Debug.Log("应用下边凹陷...");
        ApplySemicircleV2Dynamic(pixels, baseSize, baseSize, semicircleTemplate.width, semicircleTemplate.height, 2, false, 0, 0, baseSize);
        
        // 左边凹陷
        Debug.Log("应用左边凹陷...");
        ApplySemicircleV2Dynamic(pixels, baseSize, baseSize, semicircleTemplate.width, semicircleTemplate.height, 3, false, 0, 0, baseSize);
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        Debug.Log($"生成四面凹陷Mask完成，尺寸: {baseSize}x{baseSize}");
        return mask;
    }
    
    /// <summary>
    /// 生成单边凸出的测试Mask
    /// </summary>
    /// <param name="baseSize">基础正方形尺寸</param>
    /// <param name="direction">凸出方向：0=上，1=右，2=下，3=左</param>
    /// <returns>单边凸出的Mask纹理</returns>
    Texture2D GenerateSingleProtrudingMask(int baseSize, int direction)
    {
        // 计算半圆模板的凸出高度
        int semicircleHeight = semicircleTemplate.height; // 应该是 258
        int protrudeHeight = semicircleHeight; // 直接使用半圆模板的高度作为凸出高度
        
        // 根据凸出方向计算最终尺寸
        int finalWidth = baseSize;
        int finalHeight = baseSize;
        
        switch (direction)
        {
            case 0: // 上边凸出
            case 2: // 下边凸出
                finalHeight = baseSize + protrudeHeight;
                break;
            case 1: // 右边凸出
            case 3: // 左边凸出
                finalWidth = baseSize + protrudeHeight;
                break;
        }
        
        Debug.Log($"基础正方形: {baseSize}x{baseSize}");
        Debug.Log($"半圆模板: {semicircleTemplate.width}x{semicircleTemplate.height}");
        Debug.Log($"凸出高度: {protrudeHeight}");
        Debug.Log($"最终Mask尺寸: {finalWidth}x{finalHeight}");
        
        Texture2D mask = new Texture2D(finalWidth, finalHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[finalWidth * finalHeight];
        
        // 初始化为透明
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // 绘制基础正方形（白色）
        int baseStartX = 0;
        int baseStartY = 0;
        
        switch (direction)
        {
            case 0: // 上边凸出：基础正方形在下方
                baseStartX = 0;
                baseStartY = 0;
                break;
            case 1: // 右边凸出：基础正方形在左方
                baseStartX = 0;
                baseStartY = 0;
                break;
            case 2: // 下边凸出：基础正方形在上方
                baseStartX = 0;
                baseStartY = protrudeHeight;
                break;
            case 3: // 左边凸出：基础正方形在右方
                baseStartX = protrudeHeight;
                baseStartY = 0;
                break;
        }
        
        // 绘制基础正方形
        for (int y = baseStartY; y < baseStartY + baseSize; y++)
        {
            for (int x = baseStartX; x < baseStartX + baseSize; x++)
            {
                if (x >= 0 && x < finalWidth && y >= 0 && y < finalHeight)
                {
                    pixels[y * finalWidth + x] = Color.white;
                }
            }
        }
        
        // 添加指定方向的凸出部分
        AddSingleProtrudingPart(pixels, finalWidth, finalHeight, baseSize, protrudeHeight, direction);
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        Debug.Log($"生成单边凸出Mask，最终尺寸: {finalWidth}x{finalHeight}");
        return mask;
    }
    
    /// <summary>
    /// 添加单个方向的凸出部分
    /// </summary>
    /// <param name="pixels">像素数组</param>
    /// <param name="maskWidth">Mask宽度</param>
    /// <param name="maskHeight">Mask高度</param>
    /// <param name="baseSize">基础正方形尺寸</param>
    /// <param name="protrudeHeight">凸出高度</param>
    /// <param name="direction">方向：0=上，1=右，2=下，3=左</param>
    void AddSingleProtrudingPart(Color[] pixels, int maskWidth, int maskHeight, int baseSize, int protrudeHeight, int direction)
    {
        // 获取半圆模板的像素
        Color[] semicirclePixels = semicircleTemplate.GetPixels();
        int semicircleWidth = semicircleTemplate.width;
        int semicircleHeight = semicircleTemplate.height;
        
        // 计算半圆模板在最终Mask中的起始位置
        int startX = 0;
        int startY = 0;
        
        switch (direction)
        {
            case 0: // 上边凸出：半圆模板放在基础正方形上方
                startX = 0;
                startY = baseSize;
                break;
            case 1: // 右边凸出：半圆模板放在基础正方形右方
                startX = baseSize;
                startY = 0;
                break;
            case 2: // 下边凸出：半圆模板放在基础正方形下方
                startX = 0;
                startY = 0;
                break;
            case 3: // 左边凸出：半圆模板放在基础正方形左方
                startX = 0;
                startY = 0;
                break;
        }
        
        // 根据方向复制半圆模板像素
        for (int sy = 0; sy < semicircleHeight; sy++)
        {
            for (int sx = 0; sx < semicircleWidth; sx++)
            {
                Color semicircleColor = semicirclePixels[sy * semicircleWidth + sx];
                
                int targetX, targetY;
                
                switch (direction)
                {
                    case 0: // 上边凸出：翻转Y坐标，圆弧朝上
                        targetX = startX + sx;
                        targetY = startY + (semicircleHeight - 1 - sy);
                        break;
                    case 1: // 右边凸出：旋转90度顺时针，圆弧朝右
                        targetX = startX + sy;
                        targetY = startY + (semicircleWidth - 1 - sx);
                        break;
                    case 2: // 下边凸出：直接复制，圆弧朝下
                        targetX = startX + sx;
                        targetY = startY + sy;
                        break;
                    case 3: // 左边凸出：旋转90度逆时针，圆弧朝左
                        targetX = startX + (semicircleHeight - 1 - sy);
                        targetY = startY + sx;
                        break;
                    default:
                        continue;
                }
                
                // 检查边界并复制像素（包括透明像素）
                if (targetX >= 0 && targetX < maskWidth && targetY >= 0 && targetY < maskHeight)
                {
                    pixels[targetY * maskWidth + targetX] = semicircleColor;
                }
            }
        }
    }
    
    [ContextMenu("生成上边凹陷测试Mask")]
    public void GenerateTopProtrudingTestMask()
    {
        Debug.Log("开始生成上边凹陷的测试Mask...");
        
        // 检查半圆模板
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！请在Inspector中指定semicircleTemplate。");
            return;
        }
        else if (semicircleTemplate.width != maskSize || semicircleTemplate.height >= maskSize)
        {
            Debug.LogError($"半圆模板尺寸不匹配！当前: {semicircleTemplate.width}x{semicircleTemplate.height}, 期望: 宽度{maskSize}，高度小于{maskSize}，请提供正确尺寸的半圆模板。");
            return;
        }
        
        try
        {
            // 生成上边凹陷的mask
            Texture2D mask = GenerateTopConcaveMask(maskSize);
            
            if (mask == null)
            {
                Debug.LogError("生成上边凹陷Mask失败！");
                return;
            }
            
            // 保存为PNG文件
            byte[] pngData = mask.EncodeToPNG();
            string fileName = "TestMask_TopConcave.png";
            string filePath = System.IO.Path.Combine(Application.dataPath, "..", fileName);
            System.IO.File.WriteAllBytes(filePath, pngData);
            
            Debug.Log($"生成上边凹陷测试Mask: {fileName}");
            
            // 清理临时纹理
            DestroyImmediate(mask);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成上边凹陷Mask时发生错误: {e.Message}");
            Debug.LogError($"错误堆栈: {e.StackTrace}");
        }
        
        Debug.Log("上边凹陷测试Mask生成完成！");
    }
    
    /// <summary>
    /// 生成上边凹陷的测试Mask
    /// </summary>
    /// <param name="baseSize">基础正方形尺寸</param>
    /// <returns>上边凹陷的Mask纹理</returns>
    Texture2D GenerateTopConcaveMask(int baseSize)
    {
        Debug.Log($"生成上边凹陷Mask，基础尺寸: {baseSize}x{baseSize}");
        Debug.Log($"半圆模板尺寸: {semicircleTemplate.width}x{semicircleTemplate.height}");
        
        Texture2D mask = new Texture2D(baseSize, baseSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[baseSize * baseSize];
        
        // 初始化为白色（基础正方形）
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        // 应用上边凹陷的半圆模板
        // 方向0=上，isProtrude=false表示凹陷
        ApplySemicircleV2Dynamic(pixels, baseSize, baseSize, semicircleTemplate.width, semicircleTemplate.height, 0, false, 0, 0, baseSize);
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        Debug.Log($"生成上边凹陷Mask完成，尺寸: {baseSize}x{baseSize}");
        return mask;
    }
    
    [ContextMenu("生成四面凸出测试Mask")]
    public void GenerateFourSideProtrudingTestMask()
    {
        Debug.Log("开始生成四面凸出的测试Mask...");
        
        // 检查半圆模板
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！请在Inspector中指定semicircleTemplate。");
            return;
        }
        else if (semicircleTemplate.width != maskSize || semicircleTemplate.height >= maskSize)
        {
            Debug.LogError($"半圆模板尺寸不匹配！当前: {semicircleTemplate.width}x{semicircleTemplate.height}, 期望: 宽度{maskSize}，高度小于{maskSize}，请提供正确尺寸的半圆模板。");
            return;
        }
        
        try
        {
            // 生成四面凸出的mask
            Texture2D mask = GenerateAllProtrudingMask(maskSize, new bool[] { true, true, true, true }, semicircleTemplate);
            
            if (mask == null)
            {
                Debug.LogError("生成四面凸出Mask失败！");
                return;
            }
            
            // 保存为PNG文件
            byte[] pngData = mask.EncodeToPNG();
            string fileName = "TestMask_FourSideProtrude.png";
            string filePath = System.IO.Path.Combine(Application.dataPath, "..", fileName);
            System.IO.File.WriteAllBytes(filePath, pngData);
            
            Debug.Log($"生成四面凸出测试Mask: {fileName}，尺寸: {mask.width}x{mask.height}");
            
            // 清理临时纹理
            DestroyImmediate(mask);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成四面凸出Mask时发生错误: {e.Message}");
            Debug.LogError($"错误堆栈: {e.StackTrace}");
        }
        
        Debug.Log("四面凸出测试Mask生成完成！");
    }
    
    /// <summary>
    /// 生成指定方向凸出的Mask
    /// </summary>
    /// <param name="baseSize">基础正方形尺寸</param>
    /// <param name="protrudingDirections">四个方向的凸出状态：[上, 右, 下, 左]</param>
    /// <param name="semicircleTemplate">半圆模板</param>
    /// <returns>生成的Mask纹理</returns>
    public Texture2D GenerateAllProtrudingMask(int baseSize, bool[] protrudingDirections, Texture2D semicircleTemplate)
    {
        if (protrudingDirections == null || protrudingDirections.Length != 4)
        {
            Debug.LogError("protrudingDirections 必须是长度为4的布尔数组：[上, 右, 下, 左]");
            return null;
        }
        
        if (semicircleTemplate == null)
        {
            Debug.LogError("semicircleTemplate 不能为空");
            return null;
        }
        
        // 计算半圆模板的凸出高度
        int semicircleHeight = semicircleTemplate.height;
        
        // 计算最终尺寸
        int finalWidth = baseSize;
        int finalHeight = baseSize;
        
        // 根据凸出方向计算最终尺寸
        if (protrudingDirections[1]) // 右凸出
            finalWidth += semicircleHeight;
        if (protrudingDirections[3]) // 左凸出
            finalWidth += semicircleHeight;
        if (protrudingDirections[0]) // 上凸出
            finalHeight += semicircleHeight;
        if (protrudingDirections[2]) // 下凸出
            finalHeight += semicircleHeight;
        
        Debug.Log($"基础正方形: {baseSize}x{baseSize}");
        Debug.Log($"半圆模板: {semicircleTemplate.width}x{semicircleTemplate.height}");
        Debug.Log($"凸出方向: 上={protrudingDirections[0]}, 右={protrudingDirections[1]}, 下={protrudingDirections[2]}, 左={protrudingDirections[3]}");
        Debug.Log($"最终Mask尺寸: {finalWidth}x{finalHeight}");
        
        Texture2D mask = new Texture2D(finalWidth, finalHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[finalWidth * finalHeight];
        
        // 初始化为透明
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // 计算基础正方形在最终mask中的位置
        int baseStartX = protrudingDirections[3] ? semicircleHeight : 0; // 如果左边凸出，基础正方形右移
        int baseStartY = protrudingDirections[2] ? semicircleHeight : 0; // 如果下边凸出，基础正方形上移
        
        // 绘制中心的基础正方形（白色）
        for (int y = baseStartY; y < baseStartY + baseSize; y++)
        {
            for (int x = baseStartX; x < baseStartX + baseSize; x++)
            {
                if (x >= 0 && x < finalWidth && y >= 0 && y < finalHeight)
                {
                    pixels[y * finalWidth + x] = Color.white;
                }
            }
        }
        
        // 添加指定方向的凸出部分
        for (int direction = 0; direction < 4; direction++)
        {
            if (protrudingDirections[direction])
            {
                ApplySemicircleV2Dynamic(pixels, finalWidth, finalHeight, semicircleTemplate.width, semicircleTemplate.height, direction, true, baseStartX, baseStartY, baseSize);
            }
        }
        
        mask.SetPixels(pixels);
        mask.Apply();
        
        Debug.Log($"生成凸出Mask，最终尺寸: {finalWidth}x{finalHeight}");
        return mask;
    }
    
    /// <summary>
    /// 添加单个方向的凸出部分到指定的mask像素数组
    /// </summary>
    /// <param name="pixels">像素数组</param>
    /// <param name="maskWidth">Mask宽度</param>
    /// <param name="maskHeight">Mask高度</param>
    /// <param name="baseSize">基础正方形尺寸</param>
    /// <param name="baseStartX">基础正方形在mask中的起始X坐标</param>
    /// <param name="baseStartY">基础正方形在mask中的起始Y坐标</param>
    /// <param name="direction">方向：0=上，1=右，2=下，3=左</param>
    /// <param name="semicircleTemplate">半圆模板</param>
    static void AddSingleProtrudingPartToMask(Color[] pixels, int maskWidth, int maskHeight, int baseSize, int baseStartX, int baseStartY, int direction, Texture2D semicircleTemplate)
    {
        // 获取半圆模板的像素
        Color[] semicirclePixels = semicircleTemplate.GetPixels();
        int semicircleWidth = semicircleTemplate.width;
        int semicircleHeight = semicircleTemplate.height;
        
        // 计算半圆模板在最终Mask中的起始位置
        int startX = 0;
        int startY = 0;
        
        switch (direction)
        {
            case 0: // 上边凸出：半圆模板放在基础正方形上方
                startX = baseStartX;
                startY = baseStartY + baseSize;
                break;
            case 1: // 右边凸出：半圆模板放在基础正方形右方
                startX = baseStartX + baseSize;
                startY = baseStartY;
                break;
            case 2: // 下边凸出：半圆模板放在基础正方形下方
                startX = baseStartX;
                startY = baseStartY - semicircleHeight;
                break;
            case 3: // 左边凸出：半圆模板放在基础正方形左方
                startX = baseStartX - semicircleHeight;
                startY = baseStartY;
                break;
        }
        
        // 根据方向复制半圆模板像素
        for (int sy = 0; sy < semicircleHeight; sy++)
        {
            for (int sx = 0; sx < semicircleWidth; sx++)
            {
                Color semicircleColor = semicirclePixels[sy * semicircleWidth + sx];
                
                int targetX, targetY;
                
                switch (direction)
                {
                    case 0: // 上边凸出：翻转Y坐标，圆弧朝上
                        targetX = startX + sx;
                        targetY = startY + (semicircleHeight - 1 - sy);
                        break;
                    case 1: // 右边凸出：旋转90度，圆弧朝右
                        targetX = startX + (semicircleHeight - 1 - sy);
                        targetY = startY + sx;
                        break;
                    case 2: // 下边凸出：直接复制，圆弧朝下
                        targetX = startX + sx;
                        targetY = startY + sy;
                        break;
                    case 3: // 左边凸出：旋转270度，圆弧朝左
                        targetX = startX + sy;
                        targetY = startY + (semicircleWidth - 1 - sx);
                        break;
                    default:
                        continue;
                }
                
                // 检查边界并复制像素（包括透明像素）
                if (targetX >= 0 && targetX < maskWidth && targetY >= 0 && targetY < maskHeight)
                {
                    pixels[targetY * maskWidth + targetX] = semicircleColor;
                }
            }
        }
    }
    

   

   

    /// <summary>
    /// 程序化生成拼图mask
    /// </summary>
    /// <param name="x">拼图块的列索引</param>
    /// <param name="y">拼图块的行索引</param>
    /// <param name="cols">总列数</param>
    /// <param name="rows">总行数</param>
    /// <param name="size">mask纹理尺寸</param>
    /// <returns>生成的mask纹理</returns>
    Texture2D GeneratePuzzleMask(int x, int y, int cols, int rows, int size)
    {
        // 半圆模板尺寸
        int semicircleW = semicircleTemplate.width;   // 512
        int semicircleH = semicircleTemplate.height;  // 258

        // 初始化为白色正方形
        Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

        // 四个方向
        for (int dir = 0; dir < 4; dir++)
        {
            bool protrude = ShouldProtrude(x, y, dir, cols, rows);
            ApplySemicircleV2Dynamic(pixels, size, size, semicircleW, semicircleH, dir, protrude, 0, 0, size);
        }

        mask.SetPixels(pixels);
        mask.Apply();
        return mask;
    }

    /// <summary>
    /// 统一的半圆应用方法 - 支持凸出和凹进，处理所有半圆效果
    /// </summary>
    /// <param name="pixels">目标像素数组</param>
    /// <param name="maskWidth">mask宽度</param>
    /// <param name="maskHeight">mask高度</param>
    /// <param name="semicircleW">半圆模板宽度</param>
    /// <param name="semicircleH">半圆模板高度</param>
    /// <param name="direction">方向：0=上，1=右，2=下，3=左</param>
    /// <param name="isProtrude">true=凸出，false=凹进</param>
    /// <param name="baseStartX">基础正方形在mask中的起始X位置</param>
    /// <param name="baseStartY">基础正方形在mask中的起始Y位置</param>
    /// <param name="baseSize">基础正方形尺寸</param>
    /// <param name="semicircleTemplate">半圆模板纹理</param>
    static void ApplySemicircleV2Dynamic(Color[] pixels, int maskWidth, int maskHeight, int semicircleW, int semicircleH, int direction, bool isProtrude, int baseStartX, int baseStartY, int baseSize, Texture2D semicircleTemplate)
    {
        string directionName = direction == 0 ? "上" : direction == 1 ? "右" : direction == 2 ? "下" : "左";
        Debug.Log($"ApplySemicircleV2Dynamic: 方向={directionName}({direction}), 突出={isProtrude}, mask尺寸={maskWidth}x{maskHeight}, 半圆={semicircleW}x{semicircleH}, 基础位置=({baseStartX},{baseStartY})");
        
        Color[] semicirclePixels = semicircleTemplate.GetPixels();
        
        // 计算半圆模板在动态mask中的起始位置，基于基础正方形的位置
        int startX = 0, startY = 0;
        switch (direction)
        {
            case 0: // 上边：半圆模板在基础正方形上边缘（在像素数组中，上边对应较大的Y值）
                startX = baseStartX + (baseSize - semicircleW) / 2;
                if (isProtrude)
                {
                    startY = baseStartY + baseSize; // 凸出时向上延伸（在像素数组中向Y增大方向）
                }
                else
                {
                    startY = baseStartY + baseSize - semicircleH; // 凹陷时在基础正方形内部，从上边缘向内
                }
                break;
            case 1: // 右边：半圆模板在基础正方形右边缘
                startY = baseStartY + (baseSize - semicircleW) / 2;
                if (isProtrude)
                {
                    startX = baseStartX + baseSize; // 凸出时向右延伸
                }
                else
                {
                    startX = baseStartX + baseSize - semicircleH; // 凹陷时在基础正方形内部，从右边缘向内
                }
                break;
            case 2: // 下边：半圆模板在基础正方形下边缘（在像素数组中，下边对应较小的Y值）
                startX = baseStartX + (baseSize - semicircleW) / 2;
                if (isProtrude)
                {
                    startY = baseStartY - semicircleH; // 凸出时向下延伸（在像素数组中向Y减小方向）
                }
                else
                {
                    startY = baseStartY; // 凹陷时在基础正方形内部，从下边缘开始
                }
                break;
            case 3: // 左边：半圆模板在基础正方形左边缘
                startY = baseStartY + (baseSize - semicircleW) / 2;
                if (isProtrude)
                {
                    startX = baseStartX - semicircleH; // 凸出时向左延伸
                }
                else
                {
                    startX = baseStartX; // 凹陷时在基础正方形内部，从左边缘开始
                }
                break;
        }
        
        Debug.Log($"ApplySemicircleV2: 半圆起始位置=({startX},{startY})");
        
        int whitePixelCount = 0;
        int transparentPixelCount = 0;
        int otherPixelCount = 0;
        
        // 统一的像素处理逻辑，支持凸出和凹陷
        for (int sy = 0; sy < semicircleH; sy++)
        {
            for (int sx = 0; sx < semicircleW; sx++)
            {
                int srcIndex = sy * semicircleW + sx;
                Color srcPixel = semicirclePixels[srcIndex];
                
                int targetX, targetY;
                
                // 根据方向应用旋转变换（统一的旋转逻辑）
                switch (direction)
                {
                    case 0: // 上边
                        if (isProtrude)
                        {
                            // 凸出时：翻转Y轴，圆弧朝上（向外凸）
                            targetX = sx;
                            targetY = semicircleH - 1 - sy;
                        }
                        else
                        {
                            // 凹陷时：直接复制，圆弧朝下（向内凹）
                            targetX = sx;
                            targetY = sy;
                        }
                        break;
                    case 1: // 右边
                        if (isProtrude)
                        {
                            // 凸出时：顺时针旋转90度，圆弧朝右（向外凸）
                            targetX = semicircleH - 1 - sy;
                            targetY = sx;
                        }
                        else
                        {
                            // 凹陷时：逆时针旋转90度，圆弧朝左（向内凹）
                            targetX = sy;
                            targetY = semicircleW - 1 - sx;
                        }
                        break;
                    case 2: // 下边
                        if (isProtrude)
                        {
                            // 凸出时：直接复制，圆弧朝下（向外凸）
                            targetX = sx;
                            targetY = sy;
                        }
                        else
                        {
                            // 凹陷时：翻转Y轴，圆弧朝上（向内凹）
                            targetX = sx;
                            targetY = semicircleH - 1 - sy;
                        }
                        break;
                    case 3: // 左边
                        if (isProtrude)
                        {
                            // 凸出时：旋转270度，圆弧朝左（向外凸）
                            targetX = sy;
                            targetY = semicircleW - 1 - sx;
                        }
                        else
                        {
                            // 凹陷时：顺时针旋转90度，圆弧朝右（向内凹）
                            targetX = semicircleH - 1 - sy;
                            targetY = sx;
                        }
                        break;
                    default:
                        continue;
                }
                
                // 计算在mask中的最终位置
                int finalX = startX + targetX;
                int finalY = startY + targetY;
                
                // 边界检查
                if (finalX < 0 || finalX >= maskWidth || finalY < 0 || finalY >= maskHeight)
                    continue;
                
                int maskIndex = finalY * maskWidth + finalX;
                
                // 根据凹凸状态设置像素
                if (isProtrude)
                {
                    // 凸出：复制所有像素（包括透明像素）
                    pixels[maskIndex] = srcPixel;
                    if (srcPixel.a >= 0.5f)
                    {
                        if (srcPixel.r > 0.5f && srcPixel.g > 0.5f && srcPixel.b > 0.5f)
                            whitePixelCount++;
                        else
                            otherPixelCount++;
                    }
                    else
                    {
                        transparentPixelCount++;
                    }
                }
                else
                {
                    // 凹陷：只处理非透明像素
                    if (srcPixel.a >= 0.5f)
                    {
                        if (srcPixel.r > 0.5f && srcPixel.g > 0.5f && srcPixel.b > 0.5f)
                        {
                            // 白色像素设为透明（挖空）
                            pixels[maskIndex] = Color.clear;
                            transparentPixelCount++;
                        }
                        otherPixelCount++;
                    }
                }
            }
        }
        
        Debug.Log($"ApplySemicircleV2完成: 处理了{otherPixelCount}个像素，白色像素={whitePixelCount}，透明像素={transparentPixelCount}");
    }
    
    /// <summary>
    /// ApplySemicircleV2Dynamic 的重载版本，用于向后兼容
    /// </summary>
    void ApplySemicircleV2Dynamic(Color[] pixels, int maskWidth, int maskHeight, int semicircleW, int semicircleH, int direction, bool isProtrude, int baseStartX, int baseStartY, int baseSize = 512)
    {
        ApplySemicircleV2Dynamic(pixels, maskWidth, maskHeight, semicircleW, semicircleH, direction, isProtrude, baseStartX, baseStartY, baseSize, semicircleTemplate);
    }
    


    /// <summary>
    /// 判断指定方向是否应该突出
    /// </summary>
    /// <param name="x">拼图块列索引</param>
    /// <param name="y">拼图块行索引</param>
    /// <param name="direction">方向：0=上, 1=右, 2=下, 3=左</param>
    /// <param name="cols">总列数</param>
    /// <param name="rows">总行数</param>
    /// <returns>true=突出，false=凹进</returns>
    bool ShouldProtrude(int x, int y, int direction, int cols, int rows)
    {
        string directionName = direction == 0 ? "上" : direction == 1 ? "右" : direction == 2 ? "下" : "左";
        
        // 边界检查：拼图外边缘不突出，保持平直
        if (direction == 0 && y == 0) 
        {
            Debug.Log($"拼图块({x},{y}) {directionName}边 - 边界，不突出");
            return false;           // 上边界
        }
        if (direction == 1 && x == cols - 1) 
        {
            Debug.Log($"拼图块({x},{y}) {directionName}边 - 边界，不突出");
            return false;    // 右边界  
        }
        if (direction == 2 && y == rows - 1) 
        {
            Debug.Log($"拼图块({x},{y}) {directionName}边 - 边界，不突出");
            return false;    // 下边界
        }
        if (direction == 3 && x == 0) 
        {
            Debug.Log($"拼图块({x},{y}) {directionName}边 - 边界，不突出");
            return false;           // 左边界
        }
        
        // 对于内部边缘，使用边缘的唯一标识来决定凹凸
        // 这样可以确保相邻拼图块的凹凸互补
        int edgeId = GetEdgeId(x, y, direction, cols, rows);
        if (edgeId == -1) 
        {
            Debug.Log($"拼图块({x},{y}) {directionName}边 - edgeId=-1，不突出");
            return false; // 边界情况
        }
        
        System.Random rand = new System.Random(5 + edgeId);
        bool result = rand.Next(2) == 0;
        Debug.Log($"拼图块({x},{y}) {directionName}边 - edgeId={edgeId}, 随机结果={result}");
        return result;
    }
    
    /// <summary>
    /// 获取边缘的唯一标识，确保相邻块使用相同的ID
    /// </summary>
    /// <param name="x">拼图块列索引</param>
    /// <param name="y">拼图块行索引</param>
    /// <param name="direction">方向：0=上, 1=右, 2=下, 3=左</param>
    /// <param name="cols">总列数</param>
    /// <param name="rows">总行数</param>
    /// <returns>边缘唯一ID，-1表示边界</returns>
    int GetEdgeId(int x, int y, int direction, int cols, int rows)
    {
        switch (direction)
        {
            case 0: // 上边
                if (y == 0) return -1; // 边界
                // 水平边缘ID：行号 * cols + 列号
                return (y - 1) * cols + x + 10000; // 加偏移避免与垂直边缘冲突
                
            case 1: // 右边
                if (x == cols - 1) return -1; // 边界
                // 垂直边缘ID：行号 * (cols-1) + 列号
                return y * (cols - 1) + x + 20000; // 加偏移避免与水平边缘冲突
                
            case 2: // 下边
                if (y == rows - 1) return -1; // 边界
                // 水平边缘ID：行号 * cols + 列号
                return y * cols + x + 10000;
                
            case 3: // 左边
                if (x == 0) return -1; // 边界
                // 垂直边缘ID：行号 * (cols-1) + 列号
                return y * (cols - 1) + (x - 1) + 20000;
        }
        return -1;
    }

    /// <summary>
    /// 应用半圆模板到指定方向
    /// </summary>
    /// <param name="pixels">目标像素数组</param>
    /// <param name="size">目标纹理尺寸</param>
    /// <param name="direction">方向：0=上, 1=右, 2=下, 3=左</param>
    /// <param name="isProtrude">true=凸出，false=凹进</param>
    void ApplySemicircle(Color[] pixels, int size, int direction, bool isProtrude)
    {
        // 安全检查
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！请在Inspector中设置semicircleTemplate。");
            return;
        }
        
        Debug.Log($"ApplySemicircle: 目标尺寸={size}, 半圆模板尺寸={semicircleTemplate.width}x{semicircleTemplate.height}, 方向={direction}, 凸出={isProtrude}");
        
        // 获取旋转后的半圆像素
        Color[] semicirclePixels = GetRotatedSemicircle(direction);
        if (semicirclePixels == null || semicirclePixels.Length == 0)
        {
            Debug.LogError("获取半圆像素失败！");
            return;
        }
        
        int semicircleSize = semicircleTemplate.width; // 假设半圆模板是正方形
        
        Debug.Log($"ApplySemicircle: 半圆像素数组长度={semicirclePixels.Length}, 期望长度={semicircleSize * semicircleSize}");
        
        // 验证半圆像素数组大小
        if (semicirclePixels.Length != semicircleSize * semicircleSize)
        {
            Debug.LogError($"半圆像素数组大小不匹配！期望: {semicircleSize * semicircleSize}, 实际: {semicirclePixels.Length}");
            Debug.LogError($"半圆模板实际尺寸: {semicircleTemplate.width}x{semicircleTemplate.height}");
            Debug.LogError($"maskSize: {maskSize}");
            return;
        }
        
        // 计算半圆在目标纹理中的位置
        int offsetX, offsetY;
        GetSemicircleOffset(size, semicircleSize, direction, out offsetX, out offsetY);
        
        Debug.Log($"ApplySemicircle: 半圆偏移位置=({offsetX}, {offsetY})");
        
        // 应用半圆到目标像素
        for (int y = 0; y < semicircleSize; y++)
        {
            for (int x = 0; x < semicircleSize; x++)
            {
                int targetX = offsetX + x;
                int targetY = offsetY + y;
                
                // 检查边界
                if (targetX < 0 || targetX >= size || targetY < 0 || targetY >= size)
                    continue;
                
                Color semicircleColor = semicirclePixels[y * semicircleSize + x];
                int targetIndex = targetY * size + targetX;
                
                // 检查半圆模板中是否有白色像素
                bool hasWhitePixel = semicircleColor.r > 0.5f && semicircleColor.g > 0.5f && semicircleColor.b > 0.5f && semicircleColor.a > 0.5f;
                
                if (hasWhitePixel) // 半圆模板中有白色像素的部分
                {
                    if (isProtrude)
                    {
                        // 凸出：半圆模板的白色像素部分添加到正方形上（保持白色）
                        pixels[targetIndex] = Color.white;
                    }
                    else
                    {
                        // 凹进：半圆模板的白色像素部分从正方形中挖掉（设为透明）
                        pixels[targetIndex] = Color.clear;
                    }
                }
                // 如果半圆模板中没有白色像素，保持原来的状态（正方形基础保持白色）
            }
        }
    }
    

    
    /// <summary>
    /// 获取旋转后的半圆像素
    /// </summary>
    /// <param name="direction">方向：0=上, 1=右, 2=下, 3=左</param>
    /// <returns>旋转后的像素数组</returns>
    Color[] GetRotatedSemicircle(int direction)
    {
        // 安全检查
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！");
            return new Color[0];
        }
        
        Color[] originalPixels = semicircleTemplate.GetPixels();
        int size = semicircleTemplate.width;
        
        Debug.Log($"GetRotatedSemicircle: 半圆模板尺寸={semicircleTemplate.width}x{semicircleTemplate.height}, 原始像素数组长度={originalPixels.Length}, 方向={direction}");
        
        if (direction == 0) // 上边，不需要旋转（默认圆弧在上）
        {
            Debug.Log($"GetRotatedSemicircle: 返回原始像素数组，长度={originalPixels.Length}");
            return originalPixels;
        }
        
        Color[] rotatedPixels = new Color[originalPixels.Length];
        Debug.Log($"GetRotatedSemicircle: 创建旋转像素数组，长度={rotatedPixels.Length}");
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int originalIndex = y * size + x;
                int newX, newY;
                
                switch (direction)
                {
                    case 1: // 右边：顺时针旋转90度
                        newX = size - 1 - y;
                        newY = x;
                        break;
                    case 2: // 下边：旋转180度
                        newX = size - 1 - x;
                        newY = size - 1 - y;
                        break;
                    case 3: // 左边：逆时针旋转90度
                        newX = y;
                        newY = size - 1 - x;
                        break;
                    default:
                        newX = x;
                        newY = y;
                        break;
                }
                
                int newIndex = newY * size + newX;
                rotatedPixels[newIndex] = originalPixels[originalIndex];
            }
        }
        
        return rotatedPixels;
    }
    
    /// <summary>
    /// 计算半圆在目标纹理中的偏移位置
    /// </summary>
    /// <param name="targetSize">目标纹理尺寸</param>
    /// <param name="semicircleSize">半圆模板尺寸</param>
    /// <param name="direction">方向：0=上, 1=右, 2=下, 3=左</param>
    /// <param name="offsetX">输出X偏移</param>
    /// <param name="offsetY">输出Y偏移</param>
    void GetSemicircleOffset(int targetSize, int semicircleSize, int direction, out int offsetX, out int offsetY)
    {
        switch (direction)
        {
            case 0: // 上边
                offsetX = (targetSize - semicircleSize) / 2;
                offsetY = 0;
                break;
            case 1: // 右边
                offsetX = targetSize - semicircleSize;
                offsetY = (targetSize - semicircleSize) / 2;
                break;
            case 2: // 下边
                offsetX = (targetSize - semicircleSize) / 2;
                offsetY = targetSize - semicircleSize;
                break;
            case 3: // 左边
                offsetX = 0;
                offsetY = (targetSize - semicircleSize) / 2;
                break;
            default:
                offsetX = 0;
                offsetY = 0;
                break;
        }
    }

    /// <summary>
    /// 使用MaskPiece类生成指定形状的mask图片
    /// 生成一个左边缘，右凸，上边缘，下凹的mask
    /// </summary>
    [ContextMenu("生成MaskPiece测试Mask")]
    public void GenerateMaskPieceTestMask()
    {
        Debug.Log("开始使用MaskPiece生成测试Mask...");
        
        // 检查半圆模板
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！请在Inspector中指定semicircleTemplate。");
            return;
        }
        
        try
        {
            // 创建MaskPiece实例：左边缘，右凸，上边缘，下凹
            MaskPiece maskPiece = new MaskPiece(
                MaskPiece.EdgeType.Edge,      // 上边：边缘
                MaskPiece.EdgeType.Protrude,  // 右边：凸出
                MaskPiece.EdgeType.Concave,   // 下边：凹陷
                MaskPiece.EdgeType.Edge       // 左边：边缘
            );
            
            Debug.Log($"创建MaskPiece: {maskPiece}");
            
            // 生成mask图片
            Texture2D mask = GenerateMaskFromMaskPiece(maskPiece);
            
            if (mask == null)
            {
                Debug.LogError("生成MaskPiece Mask失败！");
                return;
            }
            
            // 保存为PNG文件
            byte[] pngData = mask.EncodeToPNG();
            string fileName = "TestMask_MaskPiece_LeftEdge_RightProtrude_TopEdge_BottomConcave.png";
            
            // 创建目标目录路径
            string targetDirectory = System.IO.Path.Combine(Application.dataPath, "Images", "mask");
            
            // 确保目录存在
            if (!System.IO.Directory.Exists(targetDirectory))
            {
                System.IO.Directory.CreateDirectory(targetDirectory);
                Debug.Log($"创建目录: {targetDirectory}");
            }
            
            string filePath = System.IO.Path.Combine(targetDirectory, fileName);
            System.IO.File.WriteAllBytes(filePath, pngData);
            
            Debug.Log($"生成MaskPiece测试Mask: {fileName}");
            Debug.Log($"Mask属性: {maskPiece}");
            Debug.Log($"是否为边缘块: {maskPiece.IsEdgePiece()}");
            Debug.Log($"是否为角块: {maskPiece.IsCornerPiece()}");
            
            // 清理临时纹理
            DestroyImmediate(mask);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成MaskPiece Mask时发生错误: {e.Message}");
            Debug.LogError($"错误堆栈: {e.StackTrace}");
        }
        
        Debug.Log("MaskPiece测试Mask生成完成！");
    }

    /// <summary>
    /// 根据MaskPiece生成mask纹理
    /// </summary>
    /// <param name="maskPiece">拼图块定义</param>
    /// <returns>生成的mask纹理</returns>
    private Texture2D GenerateMaskFromMaskPiece(MaskPiece maskPiece)
    {
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！");
            return null;
        }
        
        int semicircleH = semicircleTemplate.height;
        
        // 计算需要的画布尺寸，为凸出的边预留空间
        int canvasWidth = maskSize;
        int canvasHeight = maskSize;
        
        // 检查左右边是否有凸出，如果有则增加宽度
        if (maskPiece.LeftEdge == MaskPiece.EdgeType.Protrude)
            canvasWidth += semicircleH;
        if (maskPiece.RightEdge == MaskPiece.EdgeType.Protrude)
            canvasWidth += semicircleH;
            
        // 检查上下边是否有凸出，如果有则增加高度
        if (maskPiece.TopEdge == MaskPiece.EdgeType.Protrude)
            canvasHeight += semicircleH;
        if (maskPiece.BottomEdge == MaskPiece.EdgeType.Protrude)
            canvasHeight += semicircleH;
        
        Debug.Log($"画布尺寸: {canvasWidth}x{canvasHeight} (基础: {maskSize}x{maskSize}, 半圆高度: {semicircleH})");
        
        // 创建扩展后的纹理
        Texture2D mask = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[canvasWidth * canvasHeight];
        
        // 初始化为透明
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // 计算基础正方形在画布中的起始位置
        int baseStartX = (maskPiece.LeftEdge == MaskPiece.EdgeType.Protrude) ? semicircleH : 0;
        int baseStartY = (maskPiece.BottomEdge == MaskPiece.EdgeType.Protrude) ? semicircleH : 0;
        
        Debug.Log($"基础正方形起始位置: ({baseStartX}, {baseStartY})");
        
        // 绘制基础正方形
        for (int y = baseStartY; y < baseStartY + maskSize; y++)
        {
            for (int x = baseStartX; x < baseStartX + maskSize; x++)
            {
                if (x >= 0 && x < canvasWidth && y >= 0 && y < canvasHeight)
                {
                    pixels[y * canvasWidth + x] = Color.white;
                }
            }
        }
        
        // 根据MaskPiece的每个边应用相应的效果
        for (int direction = 0; direction < 4; direction++)
        {
            MaskPiece.EdgeType edgeType = maskPiece.GetEdge(direction);
            
            switch (edgeType)
            {
                case MaskPiece.EdgeType.Edge:
                    // 边缘：不做任何处理，保持平直
                    Debug.Log($"方向{direction}({GetDirectionName(direction)})：边缘，保持平直");
                    break;
                    
                case MaskPiece.EdgeType.Protrude:
                    // 凸出：应用凸出的半圆
                    Debug.Log($"方向{direction}({GetDirectionName(direction)})：凸出");
                    ApplySemicircleV2Dynamic(pixels, canvasWidth, canvasHeight, 
                        semicircleTemplate.width, semicircleTemplate.height, 
                        direction, true, baseStartX, baseStartY, maskSize);
                    break;
                    
                case MaskPiece.EdgeType.Concave:
                    // 凹陷：应用凹陷的半圆
                    Debug.Log($"方向{direction}({GetDirectionName(direction)})：凹陷");
                    ApplySemicircleV2Dynamic(pixels, canvasWidth, canvasHeight, 
                        semicircleTemplate.width, semicircleTemplate.height, 
                        direction, false, baseStartX, baseStartY, maskSize);
                    break;
            }
        }
        
        // 应用像素到纹理
        mask.SetPixels(pixels);
        mask.Apply();
        
        return mask;
    }

    /// <summary>
    /// 获取方向名称（用于调试输出）
    /// </summary>
    /// <param name="direction">方向编号</param>
    /// <returns>方向名称</returns>
    private string GetDirectionName(int direction)
    {
        switch (direction)
        {
            case 0: return "上";
            case 1: return "右";
            case 2: return "下";
            case 3: return "左";
            default: return "未知";
        }
    }

    /// <summary>
    /// 生成指定行列数的拼图块mask图片
    /// </summary>
    /// <param name="gridSize">行列数（如2表示2x2，3表示3x3）</param>
    public void GenerateMaskPieces(int gridSize)
    {
        if (gridSize < 2)
        {
            Debug.LogError("拼图行列数必须至少为2！");
            return;
        }
        
        Debug.Log($"开始生成{gridSize}x{gridSize}拼图块Masks...");
        
        // 检查半圆模板
        if (semicircleTemplate == null)
        {
            Debug.LogError("半圆模板图片未设置！请在Inspector中指定semicircleTemplate。");
            return;
        }
        
        try
        {
            // 创建拼图布局：存储每个位置的边类型
            MaskPiece.EdgeType[,] horizontalEdges = new MaskPiece.EdgeType[gridSize + 1, gridSize]; // 水平边
            MaskPiece.EdgeType[,] verticalEdges = new MaskPiece.EdgeType[gridSize, gridSize + 1];   // 垂直边
            
            // 创建用于存储所有mask信息的拼图布局数据
            JigsawLayoutData layoutData = new JigsawLayoutData(gridSize);
            
            // 初始化边界为边缘
            for (int i = 0; i <= gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    if (i == 0 || i == gridSize)
                    {
                        horizontalEdges[i, j] = MaskPiece.EdgeType.Edge; // 上下边界
                    }
                }
            }
            
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j <= gridSize; j++)
                {
                    if (j == 0 || j == gridSize)
                    {
                        verticalEdges[i, j] = MaskPiece.EdgeType.Edge; // 左右边界
                    }
                }
            }
            
            // 随机生成内部边的类型（凸出或凹陷）
            System.Random random = new System.Random();
            
            // 生成水平内部边
            for (int i = 1; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    // 随机选择凸出或凹陷
                    horizontalEdges[i, j] = (random.Next(2) == 0) ? 
                        MaskPiece.EdgeType.Protrude : MaskPiece.EdgeType.Concave;
                }
            }
            
            // 生成垂直内部边
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 1; j < gridSize; j++)
                {
                    // 随机选择凸出或凹陷
                    verticalEdges[i, j] = (random.Next(2) == 0) ? 
                        MaskPiece.EdgeType.Protrude : MaskPiece.EdgeType.Concave;
                }
            }
            
            Debug.Log("拼图布局生成完成，开始生成每个拼图块的mask...");
            
            // 为每个拼图块生成mask
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    // 确定当前拼图块的四个边
                    MaskPiece.EdgeType topEdge = horizontalEdges[row, col];
                    MaskPiece.EdgeType bottomEdge = horizontalEdges[row + 1, col];
                    MaskPiece.EdgeType leftEdge = verticalEdges[row, col];
                    MaskPiece.EdgeType rightEdge = verticalEdges[row, col + 1];
                    
                    // 对于相邻的拼图块，需要确保边的互补性
                    // 如果上方拼图块的下边是凸出，那么当前拼图块的上边应该是凹陷
                    if (row > 0)
                    {
                        topEdge = GetComplementaryEdge(horizontalEdges[row, col]);
                    }
                    
                    // 如果左方拼图块的右边是凸出，那么当前拼图块的左边应该是凹陷
                    if (col > 0)
                    {
                        leftEdge = GetComplementaryEdge(verticalEdges[row, col]);
                    }
                    
                    // 创建MaskPiece
                    MaskPiece maskPiece = new MaskPiece(topEdge, rightEdge, bottomEdge, leftEdge);
                    
                    Debug.Log($"拼图块[{row},{col}]: {maskPiece}");
                    
                    // 生成mask图片
                    Texture2D mask = GenerateMaskFromMaskPiece(maskPiece);
                    
                    if (mask == null)
                    {
                        Debug.LogError($"生成拼图块[{row},{col}]的mask失败！");
                        continue;
                    }
                    
                    // 保存为PNG文件
                    byte[] pngData = mask.EncodeToPNG();
                    
                    // 使用简洁的数字命名格式：{row}_{col}.png
                    string fileName = $"{row}_{col}.png";
                    
                    // 创建目标目录路径 - 保存到Resources文件夹
                    string targetDirectory = System.IO.Path.Combine(Application.dataPath, "Resources", "Images", "mask", $"{gridSize}x{gridSize}");
                    
                    // 确保目录存在
                    if (!System.IO.Directory.Exists(targetDirectory))
                    {
                        System.IO.Directory.CreateDirectory(targetDirectory);
                        Debug.Log($"创建目录: {targetDirectory}");
                    }
                    
                    string filePath = System.IO.Path.Combine(targetDirectory, fileName);
                    System.IO.File.WriteAllBytes(filePath, pngData);
                    
                    Debug.Log($"生成拼图块[{row},{col}]mask: {fileName}");
                    
                    // 将mask信息添加到布局数据中
                    MaskPieceData pieceData = new MaskPieceData(row, col, fileName, maskPiece);
                    layoutData.pieces.Add(pieceData);
                    
                    // 清理临时纹理
                    DestroyImmediate(mask);
                }
            }
            
            // 生成JSON文件保存mask信息
            string jsonData = JsonUtility.ToJson(layoutData, true);
            string jsonFileName = $"{gridSize}x{gridSize}_layout.json";
            string jsonFilePath = System.IO.Path.Combine(Application.dataPath, "Resources", "Images", "mask", $"{gridSize}x{gridSize}", jsonFileName);
            System.IO.File.WriteAllText(jsonFilePath, jsonData);
            
            Debug.Log($"{gridSize}x{gridSize}拼图块Masks生成完成！");
            Debug.Log($"生成JSON布局文件: {jsonFileName}");
            Debug.Log($"JSON文件包含{layoutData.pieces.Count}个拼图块的边缘信息");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成拼图块Masks时发生错误: {e.Message}");
            Debug.LogError($"错误堆栈: {e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 获取互补的边类型（凸出对应凹陷，凹陷对应凸出，边缘保持边缘）
    /// </summary>
    /// <param name="edgeType">原始边类型</param>
    /// <returns>互补的边类型</returns>
    private MaskPiece.EdgeType GetComplementaryEdge(MaskPiece.EdgeType edgeType)
    {
        switch (edgeType)
        {
            case MaskPiece.EdgeType.Protrude:
                return MaskPiece.EdgeType.Concave;
            case MaskPiece.EdgeType.Concave:
                return MaskPiece.EdgeType.Protrude;
            case MaskPiece.EdgeType.Edge:
            default:
                return MaskPiece.EdgeType.Edge;
        }
    }
    
    /// <summary>
    /// 获取边类型的字符串表示（用于文件名）
    /// </summary>
    /// <param name="edgeType">边类型</param>
    /// <returns>字符串表示</returns>
    private string GetEdgeString(MaskPiece.EdgeType edgeType)
    {
        switch (edgeType)
        {
            case MaskPiece.EdgeType.Edge:
                return "E";
            case MaskPiece.EdgeType.Protrude:
                return "P";
            case MaskPiece.EdgeType.Concave:
                return "C";
            default:
                return "U";
        }
    }
    
    /// <summary>
    /// 批量生成2x2到10x10的所有拼图mask图片
    /// </summary>
    [ContextMenu("批量生成MaskPieceMask")]
    public void GenerateAllJigsawMasks()
    {
        Debug.Log("开始批量生成2x2到10x10的拼图mask图片...");
        
        // 记录开始时间
        System.DateTime startTime = System.DateTime.Now;
        
        try
        {
            // 生成2x2到10x10的拼图
            for (int gridSize = 2; gridSize <= 10; gridSize++)
            {
                Debug.Log($"正在生成{gridSize}x{gridSize}拼图mask...");
                
                // 调用单个尺寸的生成方法
                GenerateMaskPieces(gridSize);
                
                Debug.Log($"{gridSize}x{gridSize}拼图mask生成完成！");
            }
            
            // 计算总耗时
            System.TimeSpan totalTime = System.DateTime.Now - startTime;
            
            Debug.Log($"批量生成完成！总共生成了9种尺寸的拼图mask（2x2到10x10）");
            Debug.Log($"总耗时: {totalTime.TotalSeconds:F2}秒");
            
            // 统计生成的文件数量
            int totalMaskFiles = 0;
            int totalJsonFiles = 0;
            for (int gridSize = 2; gridSize <= 10; gridSize++)
            {
                totalMaskFiles += gridSize * gridSize; // 每个尺寸有gridSize*gridSize个mask文件
                totalJsonFiles += 1; // 每个尺寸有1个JSON布局文件
            }
            
            Debug.Log($"总共生成了{totalMaskFiles}个mask图片文件和{totalJsonFiles}个JSON布局文件");
            Debug.Log("文件保存位置: Assets/Resources/Images/mask/{尺寸}x{尺寸}/");
            Debug.Log("JSON文件包含每个mask的四边凹凸信息，可用于拼图逻辑判断");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"批量生成拼图mask时发生错误: {e.Message}");
            Debug.LogError($"错误堆栈: {e.StackTrace}");
        }
    }
}