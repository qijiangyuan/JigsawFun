using UnityEngine;
using System.IO;

namespace JigsawFun.GenerateNormals
{
    /// <summary>
    /// 预计算法线贴图辅助类 - 运行时自动设置法线贴图
    /// </summary>
    public static class PrecomputedNormalHelper
    {
        private const string NORMAL_SUFFIX = "_Normal";
        private const string GENERATED_PATH = "Assets/Images/Generated/";
        
        /// <summary>
        /// 为材质自动设置对应的法线贴图
        /// </summary>
        /// <param name="material">要设置的材质</param>
        /// <param name="mainTexture">主纹理</param>
        /// <returns>是否成功设置法线贴图</returns>
        public static bool SetNormalMapForMaterial(Material material, Texture2D mainTexture)
        {
            if (material == null || mainTexture == null)
                return false;
                
            // 查找对应的法线贴图
            Texture2D normalMap = FindNormalMap(mainTexture.name);
            
            if (normalMap != null)
            {
                material.SetTexture("_NormalMap", normalMap);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 根据纹理名称查找对应的法线贴图
        /// </summary>
        /// <param name="textureName">纹理名称</param>
        /// <returns>法线贴图，如果未找到返回null</returns>
        public static Texture2D FindNormalMap(string textureName)
        {
            string normalMapName = textureName + NORMAL_SUFFIX;
            
            // 首先在Generated目录中查找
            string generatedPath = Path.Combine(GENERATED_PATH, normalMapName + ".png");
            Texture2D normalMap = Resources.Load<Texture2D>(generatedPath);
            
            if (normalMap == null)
            {
                // 在整个项目中搜索
                string[] guids = UnityEditor.AssetDatabase.FindAssets(normalMapName + " t:Texture2D");
                
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    normalMap = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
            
            return normalMap;
        }
        
        /// <summary>
        /// 检查是否存在对应的法线贴图
        /// </summary>
        /// <param name="textureName">纹理名称</param>
        /// <returns>是否存在法线贴图</returns>
        public static bool HasNormalMap(string textureName)
        {
            return FindNormalMap(textureName) != null;
        }
        
        /// <summary>
        /// 为SpriteRenderer自动设置优化材质和法线贴图
        /// </summary>
        /// <param name="spriteRenderer">SpriteRenderer组件</param>
        /// <param name="precomputedMaterial">预计算材质模板</param>
        /// <returns>是否成功设置</returns>
        public static bool SetupSpriteRenderer(SpriteRenderer spriteRenderer, Material precomputedMaterial)
        {
            if (spriteRenderer == null || precomputedMaterial == null || spriteRenderer.sprite == null)
                return false;
                
            // 创建材质实例
            Material materialInstance = new Material(precomputedMaterial);
            
            // 设置主纹理
            materialInstance.SetTexture("_MainTex", spriteRenderer.sprite.texture);
            
            // 设置法线贴图
            bool hasNormalMap = SetNormalMapForMaterial(materialInstance, spriteRenderer.sprite.texture);
            
            if (hasNormalMap)
            {
                spriteRenderer.material = materialInstance;
                return true;
            }
            else
            {
                Debug.LogWarning($"未找到纹理 {spriteRenderer.sprite.texture.name} 对应的法线贴图");
                Object.DestroyImmediate(materialInstance);
                return false;
            }
        }
    }
    
    /// <summary>
    /// 自动设置预计算法线贴图的组件
    /// </summary>
    [System.Serializable]
    public class AutoPrecomputedNormal : MonoBehaviour
    {
        [Header("预计算法线贴图设置")]
        [SerializeField] private Material precomputedMaterialTemplate;
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool logResults = true;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupNormalMap();
            }
        }
        
        [ContextMenu("设置法线贴图")]
        public void SetupNormalMap()
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (spriteRenderer == null)
            {
                if (logResults)
                    Debug.LogError("未找到SpriteRenderer组件", this);
                return;
            }
            
            if (precomputedMaterialTemplate == null)
            {
                if (logResults)
                    Debug.LogError("未设置预计算材质模板", this);
                return;
            }
            
            bool success = PrecomputedNormalHelper.SetupSpriteRenderer(spriteRenderer, precomputedMaterialTemplate);
            
            if (logResults)
            {
                if (success)
                {
                    Debug.Log($"成功为 {gameObject.name} 设置预计算法线贴图", this);
                }
                else
                {
                    Debug.LogWarning($"无法为 {gameObject.name} 设置预计算法线贴图", this);
                }
            }
        }
        
        /// <summary>
        /// 检查是否可以使用预计算法线贴图
        /// </summary>
        /// <returns>是否可以使用</returns>
        public bool CanUsePrecomputedNormal()
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                return false;
                
            return PrecomputedNormalHelper.HasNormalMap(spriteRenderer.sprite.texture.name);
        }
    }
}