using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace JigsawFun.GenerateNormals
{
    /// <summary>
    /// 法线贴图生成器 - 将SpriteLitAutoNormal_Border着色器的边缘检测算法移植到CPU预计算
    /// </summary>
    public class NormalMapGenerator : EditorWindow
    {
        [SerializeField] private Texture2D sourceTexture;
        [SerializeField] private float borderSize = 30f;
        [SerializeField] private float edgeStrength = 1f;
        [SerializeField] private float smoothingStrength = 1f;
        [SerializeField] private string outputPath = "Assets/Images/Generated/";
        [SerializeField] private string inputDirectory = "Assets/Resources/Images/mask";
        [SerializeField] private bool generateForAllTextures = false;
        
        [MenuItem("Tools/Generate Normal Maps")]
        public static void ShowWindow()
        {
            GetWindow<NormalMapGenerator>("Normal Map Generator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("法线贴图生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            sourceTexture = (Texture2D)EditorGUILayout.ObjectField("源纹理", sourceTexture, typeof(Texture2D), false);
            borderSize = EditorGUILayout.FloatField("边框大小 (像素)", borderSize);
            edgeStrength = EditorGUILayout.FloatField("边缘强度", edgeStrength);
            smoothingStrength = EditorGUILayout.Slider("平滑强度", smoothingStrength, 0f, 3f);
            outputPath = EditorGUILayout.TextField("输出路径", outputPath);
            
            EditorGUILayout.Space();
            generateForAllTextures = EditorGUILayout.Toggle("批量生成模式", generateForAllTextures);
            
            if (generateForAllTextures)
            {
                EditorGUI.indentLevel++;
                inputDirectory = EditorGUILayout.TextField("输入目录", inputDirectory);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("生成法线贴图"))
            {
                if (generateForAllTextures)
                {
                    GenerateForAllTextures();
                }
                else if (sourceTexture != null)
                {
                    GenerateNormalMap(sourceTexture);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请选择源纹理", "确定");
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("功能说明:\n" +
                                  "• 单个生成: 选择源纹理，设置参数后生成对应的法线贴图\n" +
                                  "• 批量生成: 指定输入目录，自动处理该目录下所有图片\n" +
                                  "\n平滑强度说明:\n" +
                                  "• 0: 使用锐利的3x3 Sobel算子，产生清晰的边缘效果\n" +
                                  "• >0: 使用弱化的5x5 Scharr算子和温和的后处理\n" +
                                  "• 1-3: 温和平滑，已优化以减少过度平滑，保持更多细节\n" +
                                  "\n批量生成特性:\n" +
                                  "• 支持取消操作，避免卡死\n" +
                                  "• 自动跳过已生成的文件\n" +
                                  "• 法线贴图保存在原图同目录，命名为 原名_Normal\n" +
                                  "• 内存管理优化，定期清理避免崩溃", MessageType.Info);
        }
        
        private void GenerateNormalMapSafe(Texture2D sourceTexture, string sourcePath)
        {
            if (sourceTexture == null)
            {
                throw new System.ArgumentNullException(nameof(sourceTexture), "源纹理不能为空");
            }
            
            // 获取源文件的目录和文件名
            string sourceDirectory = System.IO.Path.GetDirectoryName(sourcePath);
            string sourceFileName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
            string sourceExtension = System.IO.Path.GetExtension(sourcePath);
            
            // 生成法线贴图文件名
            string normalMapFileName = $"{sourceFileName}_Normal{sourceExtension}";
            string normalMapPath = System.IO.Path.Combine(sourceDirectory, normalMapFileName).Replace("\\", "/");
            
            // 临时设置sourceTexture用于生成
            var originalSourceTexture = this.sourceTexture;
            var originalOutputPath = this.outputPath;
            
            try
            {
                this.sourceTexture = sourceTexture;
                this.outputPath = normalMapPath;
                
                // 调用原有的生成方法
                GenerateNormalMap();
            }
            finally
            {
                // 恢复原始设置
                this.sourceTexture = originalSourceTexture;
                this.outputPath = originalOutputPath;
            }
        }
        
        private void GenerateForAllTextures()
        {
            if (string.IsNullOrEmpty(inputDirectory))
            {
                EditorUtility.DisplayDialog("错误", "请指定输入目录", "确定");
                return;
            }
            
            if (!AssetDatabase.IsValidFolder(inputDirectory))
            {
                EditorUtility.DisplayDialog("错误", $"输入目录不存在: {inputDirectory}", "确定");
                return;
            }
            
            // 查找指定目录下的所有纹理
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { inputDirectory });
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", $"在目录 {inputDirectory} 中未找到任何纹理文件", "确定");
                return;
            }
            
            // 过滤掉已生成的文件和无效文件
            var validTextures = new System.Collections.Generic.List<(string path, Texture2D texture)>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // 跳过Generated目录和法线贴图文件
                if (path.Contains("Generated") || path.Contains("_Normal"))
                    continue;
                    
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    validTextures.Add((path, texture));
                }
            }
            
            if (validTextures.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有找到需要处理的纹理文件", "确定");
                return;
            }
            
            // 开始批量处理
            int processed = 0;
            int failed = 0;
            var failedFiles = new System.Collections.Generic.List<string>();
            
            try
            {
                for (int i = 0; i < validTextures.Count; i++)
                {
                    var (path, texture) = validTextures[i];
                    
                    // 显示进度
                    float progress = (float)i / validTextures.Count;
                    bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "批量生成法线贴图", 
                        $"处理: {texture.name} ({i + 1}/{validTextures.Count})", 
                        progress);
                    
                    if (cancelled)
                    {
                        EditorUtility.DisplayDialog("已取消", $"已处理 {processed} 个文件，操作被用户取消", "确定");
                        break;
                    }
                    
                    try
                    {
                        // 生成法线贴图
                        GenerateNormalMapSafe(texture, path);
                        processed++;
                        
                        // 每处理5个文件刷新一次资源数据库，避免内存积累
                        if (processed % 5 == 0)
                        {
                            AssetDatabase.Refresh();
                            System.GC.Collect();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        failed++;
                        failedFiles.Add($"{texture.name}: {ex.Message}");
                        Debug.LogError($"生成法线贴图失败: {texture.name}, 错误: {ex.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
            
            // 显示结果
            string message = $"批量生成完成!\n成功: {processed} 个\n失败: {failed} 个";
            if (failedFiles.Count > 0)
            {
                message += "\n\n失败的文件:\n" + string.Join("\n", failedFiles.Take(5));
                if (failedFiles.Count > 5)
                {
                    message += $"\n... 还有 {failedFiles.Count - 5} 个文件失败";
                }
            }
            
            EditorUtility.DisplayDialog("完成", message, "确定");
        }
        
        private void GenerateNormalMap()
        {
            if (sourceTexture == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择源纹理", "确定");
                return;
            }
            
            GenerateNormalMap(sourceTexture);
        }
        
        private void GenerateNormalMap(Texture2D sourceTexture)
        {
            // 确保纹理可读
            string texturePath = AssetDatabase.GetAssetPath(sourceTexture);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            bool wasReadable = importer.isReadable;
            
            if (!wasReadable)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(texturePath);
            }
            
            try
            {
                // 获取源纹理数据
                Color[] sourcePixels = sourceTexture.GetPixels();
                int width = sourceTexture.width;
                int height = sourceTexture.height;
                
                // 创建法线贴图数据
                Color[] normalPixels = new Color[width * height];
                
                // 移植着色器算法到CPU
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        Color sourceColor = sourcePixels[index];
                        
                        if (sourceColor.a < 0.001f)
                        {
                            // 透明像素，设置默认法线
                            normalPixels[index] = new Color(0.5f, 0.5f, 1f, 0f);
                            continue;
                        }
                        
                        // 计算当前像素的高度
                        float height_current = CalculateHeight(x, y, width, height, sourcePixels);
                        
                        // 根据平滑强度选择梯度计算方法
                        Vector2 gradient;
                        if (smoothingStrength <= 0.001f)
                        {
                            // 平滑强度为0时，使用锐利的Sobel算子
                            gradient = CalculateSharpGradient(x, y, width, height, sourcePixels);
                        }
                        else
                        {
                            // 使用平滑的梯度计算
                            gradient = CalculateSmoothGradient(x, y, width, height, sourcePixels);
                        }
                        
                        // 应用边缘强度
                        gradient *= edgeStrength;
                        
                        // 计算法线
                        Vector3 normal = new Vector3(-gradient.x, -gradient.y, 1.0f).normalized;
                        
                        // 转换到0-1范围
                        normalPixels[index] = new Color(
                            normal.x * 0.5f + 0.5f,
                            normal.y * 0.5f + 0.5f,
                            normal.z * 0.5f + 0.5f,
                            height_current
                        );
                    }
                }
                
                // 应用后处理平滑
                if (smoothingStrength > 0f)
                {
                    normalPixels = ApplyNormalSmoothing(normalPixels, width, height, sourcePixels, smoothingStrength);
                }
                
                // 创建法线贴图纹理
                Texture2D normalMap = new Texture2D(width, height, TextureFormat.RGBA32, false);
                normalMap.SetPixels(normalPixels);
                normalMap.Apply();
                
                // 保存法线贴图
                SaveNormalMap(normalMap, sourceTexture.name);
                
                DestroyImmediate(normalMap);
            }
            finally
            {
                // 恢复原始设置
                if (!wasReadable)
                {
                    importer.isReadable = false;
                    AssetDatabase.ImportAsset(texturePath);
                }
            }
        }
        
        private float CalculateHeight(int x, int y, int width, int height, Color[] pixels)
        {
            // 边界检查
            if (x < 0 || x >= width || y < 0 || y >= height)
                return 0f;
                
            int index = y * width + x;
            Color pixel = pixels[index];
            
            if (pixel.a < 0.001f)
                return 0f;
                
            // 计算到UV边界的距离
            float distToUVEdge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(width - x - 1, height - y - 1));
            
            // 使用更精确的距离场算法计算到最近透明像素的距离
            float minDistanceToTransparent = CalculateDistanceField(x, y, width, height, pixels);
            
            // 取两种边缘距离的最小值
            float finalEdgeDistance = Mathf.Min(distToUVEdge, minDistanceToTransparent);
            
            // 计算高度 - 使用更平滑的曲线
            float heightValue = Mathf.Clamp01(finalEdgeDistance / borderSize);
            
            // 使用三次贝塞尔曲线进行更平滑的插值
            heightValue = SmoothCurve(heightValue);
            
            // 边缘范围检查
            if (finalEdgeDistance >= borderSize)
            {
                heightValue = 1f;
            }
            
            return heightValue * pixel.a;
        }
        
        /// <summary>
        /// 多级距离场算法 - 提高距离计算精度
        /// </summary>
        private float CalculateDistanceField(int centerX, int centerY, int width, int height, Color[] pixels)
        {
            float minDistance = float.MaxValue;
            int searchRadius = Mathf.RoundToInt(borderSize);
            
            // 第一级：粗略搜索，步长为2
            for (int y = centerY - searchRadius; y <= centerY + searchRadius; y += 2)
            {
                for (int x = centerX - searchRadius; x <= centerX + searchRadius; x += 2)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        int index = y * width + x;
                        if (pixels[index].a < 0.001f)
                        {
                            float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                            minDistance = Mathf.Min(minDistance, distance);
                        }
                    }
                }
            }
            
            // 第二级：在找到的最近距离附近进行精细搜索
            if (minDistance < float.MaxValue)
            {
                int fineSearchRadius = Mathf.Min(5, Mathf.RoundToInt(minDistance + 2));
                
                for (int y = centerY - fineSearchRadius; y <= centerY + fineSearchRadius; y++)
                {
                    for (int x = centerX - fineSearchRadius; x <= centerX + fineSearchRadius; x++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            // 使用亚像素精度计算
                            float preciseDistance = CalculatePreciseDistance(centerX, centerY, x, y, width, height, pixels);
                            if (preciseDistance >= 0)
                            {
                                minDistance = Mathf.Min(minDistance, preciseDistance);
                            }
                        }
                    }
                }
            }
            
            return minDistance == float.MaxValue ? borderSize : minDistance;
        }
        
        /// <summary>
        /// 计算亚像素精度的距离
        /// </summary>
        private float CalculatePreciseDistance(int centerX, int centerY, int x, int y, int width, int height, Color[] pixels)
        {
            // 检查当前像素是否透明
            if (GetAlphaAt(x, y, pixels, width, height) < 0.001f)
            {
                return Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
            }
            
            // 检查像素边缘，使用双线性插值寻找精确的透明边界
            float[] offsets = { -0.5f, 0.5f };
            float minEdgeDistance = float.MaxValue;
            
            foreach (float offsetX in offsets)
            {
                foreach (float offsetY in offsets)
                {
                    float sampleX = x + offsetX;
                    float sampleY = y + offsetY;
                    
                    float alpha = GetBilinearAlpha(sampleX, sampleY, pixels, width, height);
                    if (alpha < 0.001f)
                    {
                        float distance = Mathf.Sqrt((sampleX - centerX) * (sampleX - centerX) + (sampleY - centerY) * (sampleY - centerY));
                        minEdgeDistance = Mathf.Min(minEdgeDistance, distance);
                    }
                }
            }
            
            return minEdgeDistance == float.MaxValue ? -1f : minEdgeDistance;
        }
        
        /// <summary>
        /// 双线性插值获取alpha值
        /// </summary>
        private float GetBilinearAlpha(float x, float y, Color[] pixels, int width, int height)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;
            
            float fx = x - x0;
            float fy = y - y0;
            
            float a00 = GetAlphaAt(x0, y0, pixels, width, height);
            float a10 = GetAlphaAt(x1, y0, pixels, width, height);
            float a01 = GetAlphaAt(x0, y1, pixels, width, height);
            float a11 = GetAlphaAt(x1, y1, pixels, width, height);
            
            float a0 = Mathf.Lerp(a00, a10, fx);
            float a1 = Mathf.Lerp(a01, a11, fx);
            
            return Mathf.Lerp(a0, a1, fy);
        }
        
        /// <summary>
        /// 安全获取指定位置的alpha值
        /// </summary>
        private float GetAlphaAt(int x, int y, Color[] pixels, int width, int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return 0f;
            return pixels[y * width + x].a;
        }
        
        /// <summary>
        /// 应用法线平滑后处理
        /// </summary>
        private Color[] ApplyNormalSmoothing(Color[] normalPixels, int width, int height, Color[] sourcePixels, float smoothingStrength)
        {
            Color[] smoothedNormals = new Color[normalPixels.Length];
            
            // 高斯模糊核 3x3 - 减少平滑强度
            float[,] gaussianKernel = {
                { 0.05f, 0.1f, 0.05f },
                { 0.1f,  0.4f, 0.1f },  // 增加中心权重，减少周围权重
                { 0.05f, 0.1f, 0.05f }
            };
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color sourceColor = sourcePixels[index];
                    
                    // 只对不透明像素进行平滑
                    if (sourceColor.a < 0.001f)
                    {
                        smoothedNormals[index] = normalPixels[index];
                        continue;
                    }
                    
                    Vector3 smoothedNormal = Vector3.zero;
                    float totalWeight = 0f;
                    float heightSum = 0f;
                    
                    // 3x3邻域平滑
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int sampleX = x + dx;
                            int sampleY = y + dy;
                            
                            if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                            {
                                int sampleIndex = sampleY * width + sampleX;
                                Color sampleSource = sourcePixels[sampleIndex];
                                
                                // 只使用不透明像素进行平滑
                                if (sampleSource.a > 0.001f)
                                {
                                    Color sampleNormal = normalPixels[sampleIndex];
                                    float weight = gaussianKernel[dy + 1, dx + 1];
                                    
                                    // 计算边缘权重 - 距离边缘越近，平滑程度越高
                                     float edgeDistance = CalculateEdgeDistance(sampleX, sampleY, width, height, sourcePixels);
                                     float edgeWeight = Mathf.Clamp01(1f - edgeDistance / 8f); // 从5像素增加到8像素，减少边缘平滑范围
                                     weight *= (1f + edgeWeight * 1f * smoothingStrength); // 从2f减少到1f，降低边缘平滑强度
                                    
                                    // 转换法线到-1到1范围
                                    Vector3 normal = new Vector3(
                                        sampleNormal.r * 2f - 1f,
                                        sampleNormal.g * 2f - 1f,
                                        sampleNormal.b * 2f - 1f
                                    );
                                    
                                    smoothedNormal += normal * weight;
                                    heightSum += sampleNormal.a * weight;
                                    totalWeight += weight;
                                }
                            }
                        }
                    }
                    
                    if (totalWeight > 0)
                    {
                        smoothedNormal /= totalWeight;
                        heightSum /= totalWeight;
                        
                        // 归一化法线
                        smoothedNormal = smoothedNormal.normalized;
                        
                        // 根据平滑强度混合原始和平滑法线
                        Vector3 originalNormal = new Vector3(
                            normalPixels[index].r * 2f - 1f,
                            normalPixels[index].g * 2f - 1f,
                            normalPixels[index].b * 2f - 1f
                        );
                        
                        Vector3 finalNormal = Vector3.Lerp(originalNormal, smoothedNormal, smoothingStrength);
                        finalNormal = finalNormal.normalized;
                        
                        float finalHeight = Mathf.Lerp(normalPixels[index].a, heightSum, smoothingStrength);
                        
                        // 转换回0-1范围
                        smoothedNormals[index] = new Color(
                            finalNormal.x * 0.5f + 0.5f,
                            finalNormal.y * 0.5f + 0.5f,
                            finalNormal.z * 0.5f + 0.5f,
                            finalHeight
                        );
                    }
                    else
                    {
                        smoothedNormals[index] = normalPixels[index];
                    }
                }
            }
            
            return smoothedNormals;
        }
        
        /// <summary>
        /// 计算到边缘的距离
        /// </summary>
        private float CalculateEdgeDistance(int x, int y, int width, int height, Color[] sourcePixels)
        {
            float minDistance = float.MaxValue;
            int searchRadius = 10; // 搜索半径
            
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                for (int dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;
                    
                    if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                    {
                        int checkIndex = checkY * width + checkX;
                        if (sourcePixels[checkIndex].a < 0.001f)
                        {
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);
                            minDistance = Mathf.Min(minDistance, distance);
                        }
                    }
                }
            }
            
            return minDistance == float.MaxValue ? searchRadius : minDistance;
        }
        
        /// <summary>
        /// 计算锐利梯度 - 使用传统3x3 Sobel算子
        /// </summary>
        private Vector2 CalculateSharpGradient(int x, int y, int width, int height, Color[] pixels)
        {
            // 传统3x3 Sobel算子
            float[,] sobelX = {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };
            
            float[,] sobelY = {
                { -1, -2, -1 },
                {  0,  0,  0 },
                {  1,  2,  1 }
            };
            
            float gradX = 0f;
            float gradY = 0f;
            
            // 3x3采样
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    float heightValue = CalculateHeight(x + dx, y + dy, width, height, pixels);
                    float weightX = sobelX[dy + 1, dx + 1];
                    float weightY = sobelY[dy + 1, dx + 1];
                    
                    gradX += heightValue * weightX;
                    gradY += heightValue * weightY;
                }
            }
            
            // 归一化 - 使用更强的系数保持锐利度
            gradX /= 8.0f;
            gradY /= 8.0f;
            
            return new Vector2(gradX, gradY);
        }
        
        /// <summary>
        /// 计算平滑梯度 - 使用多点采样和加权平均
        /// </summary>
        private Vector2 CalculateSmoothGradient(int x, int y, int width, int height, Color[] pixels)
        {
            // 使用5x5 Scharr算子进行更精确的梯度计算
            float[,] scharrX = {
                { -1, -2,  0,  2,  1 },
                { -2, -4,  0,  4,  2 },
                { -3, -6,  0,  6,  3 },
                { -2, -4,  0,  4,  2 },
                { -1, -2,  0,  2,  1 }
            };
            
            float[,] scharrY = {
                { -1, -2, -3, -2, -1 },
                { -2, -4, -6, -4, -2 },
                {  0,  0,  0,  0,  0 },
                {  2,  4,  6,  4,  2 },
                {  1,  2,  3,  2,  1 }
            };
            
            float gradX = 0f;
            float gradY = 0f;
            float totalWeight = 0f;
            
            // 5x5采样
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    float heightValue = CalculateHeight(x + dx, y + dy, width, height, pixels);
                    float weightX = scharrX[dy + 2, dx + 2];
                    float weightY = scharrY[dy + 2, dx + 2];
                    
                    gradX += heightValue * weightX;
                    gradY += heightValue * weightY;
                    totalWeight += Mathf.Abs(weightX) + Mathf.Abs(weightY);
                }
            }
            
            // 归一化 - 减少平滑效果
            if (totalWeight > 0)
            {
                gradX /= totalWeight * 0.3f;  // 从0.5f减少到0.3f，增强梯度强度
                gradY /= totalWeight * 0.3f;
            }
            
            // 应用轻微的平滑滤波
            Vector2 gradient = new Vector2(gradX, gradY);
            
            // 减少梯度幅度限制，保持更多细节
            float magnitude = gradient.magnitude;
            if (magnitude > 3.0f)  // 从2.0f增加到3.0f，允许更强的梯度
            {
                gradient = gradient.normalized * (3.0f + (magnitude - 3.0f) * 0.5f);  // 增加保留系数
            }
            
            // 减少非线性增强，保持更多原始强度
            float enhancedMagnitude = Mathf.Pow(gradient.magnitude, 0.95f);  // 从0.9f增加到0.95f
            if (gradient.magnitude > 0)
            {
                gradient = gradient.normalized * enhancedMagnitude;
            }
            
            return gradient;
        }
        
        /// <summary>
        /// 三次贝塞尔曲线平滑函数
        /// </summary>
        private float SmoothCurve(float t)
        {
            // 使用S形三次贝塞尔曲线，比SmoothStep更平滑
            return t * t * (3f - 2f * t);
        }
        
        private void SaveNormalMap(Texture2D normalMap, string originalName)
        {
            string fullPath;
            
            // 检查outputPath是否是完整的文件路径
            if (outputPath.EndsWith(".png") || outputPath.EndsWith(".jpg") || outputPath.EndsWith(".jpeg"))
            {
                // outputPath是完整的文件路径，直接使用
                fullPath = outputPath;
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            else
            {
                // outputPath是目录路径，按原来的方式处理
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                
                string fileName = $"{originalName}_Normal.png";
                fullPath = Path.Combine(outputPath, fileName);
            }
            
            // 保存文件
            byte[] pngData = normalMap.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);
            
            // 刷新资源数据库
            AssetDatabase.Refresh();
            
            // 设置导入设置
            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                AssetDatabase.ImportAsset(fullPath);
            }
            
            Debug.Log($"法线贴图已保存: {fullPath}");
        }
    }
}