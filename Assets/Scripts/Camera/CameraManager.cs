using UnityEngine;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 管理相机拍照功能的脚本，使用 NativeCamera 插件实现。
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public delegate void OnPhotoCaptured(Texture2D texture);
    public event OnPhotoCaptured onPhotoCaptured;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 调用相机拍照
    /// </summary>
    public void TakePhoto()
    {
#if UNITY_EDITOR
        Debug.Log("TakePhoto invoked in Editor");
        string path = EditorUtility.OpenFilePanel("选择图片模拟拍照", "", "png");
        if (string.IsNullOrEmpty(path))
        {
            path = EditorUtility.OpenFilePanel("选择图片模拟拍照（jpg）", "", "jpg");
        }
        if (string.IsNullOrEmpty(path))
        {
            path = EditorUtility.OpenFilePanel("选择图片模拟拍照（jpeg）", "", "jpeg");
        }
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("取消选择图片（编辑器模拟拍照）");
            return;
        }
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (texture.LoadImage(bytes))
        {
            string fileName = "EditorSim_Captured_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string savePath = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(savePath, texture.EncodeToPNG());
            Debug.Log("编辑器模拟拍照图片已保存到: " + savePath);
            onPhotoCaptured?.Invoke(texture);
        }
        else
        {
            Debug.LogError("无法加载所选图片（编辑器模拟拍照）: " + path);
        }
#else
#if (UNITY_ANDROID || UNITY_IOS)
        NativeCamera.TakePicture((path) =>
        {
            Debug.Log("照片保存路径: " + path);
            if (path != null)
            {
                Texture2D texture = NativeCamera.LoadImageAtPath(path, 2048, false);
                if (texture != null)
                {
                    Debug.Log("照片加载成功: " + texture.width + "x" + texture.height);
                    string fileName = "CapturedPhoto_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
                    string savePath = Path.Combine(Application.persistentDataPath, fileName);
                    byte[] bytes = texture.EncodeToPNG();
                    File.WriteAllBytes(savePath, bytes);
                    Debug.Log("照片已保存到: " + savePath);
                    onPhotoCaptured?.Invoke(texture);
                }
                else
                {
                    Debug.LogError("无法从路径加载照片: " + path);
                }
            }
        });
#else
        Debug.LogWarning("当前平台未集成原生相机插件，拍照功能不可用。");
#endif
#endif
    }

    /// <summary>
    /// 选择相册图片（若取消则回退为拍照）
    /// </summary>
    public void PickImageFromGallery()
    {
#if UNITY_EDITOR
        Debug.Log("PickImageFromGallery invoked in Editor");
        string path = EditorUtility.OpenFilePanel("选择相册图片（编辑器模拟）", "", "png");
        if (string.IsNullOrEmpty(path))
        {
            path = EditorUtility.OpenFilePanel("选择相册图片（jpg）", "", "jpg");
        }
        if (string.IsNullOrEmpty(path))
        {
            path = EditorUtility.OpenFilePanel("选择相册图片（jpeg）", "", "jpeg");
        }
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("取消选择相册图片（编辑器模拟）");
            return;
        }
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (texture.LoadImage(bytes))
        {
            string fileName = "EditorSim_Gallery_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string savePath = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(savePath, texture.EncodeToPNG());
            Debug.Log("编辑器模拟相册图片已保存到: " + savePath);
            onPhotoCaptured?.Invoke(texture);
        }
        else
        {
            Debug.LogError("无法加载所选相册图片（编辑器模拟）: " + path);
        }
#else
#if (UNITY_ANDROID || UNITY_IOS)
        Debug.Log("调用 NativeGallery.GetImageFromGallery（强类型）");
        NativeGallery.GetImageFromGallery((path) =>
        {
            Debug.Log($"NativeGallery 回调，path={path}");
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("用户取消相册选择");
                return;
            }
            Texture2D texture = NativeCamera.LoadImageAtPath(path, 2048, false);
            if (texture != null)
            {
                string fileName = "GalleryImage_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
                string savePath = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllBytes(savePath, texture.EncodeToPNG());
                Debug.Log("相册图片已保存到: " + savePath);
                onPhotoCaptured?.Invoke(texture);
            }
            else
            {
                Debug.LogError("无法从路径加载相册图片: " + path);
            }
        }, "选择图片", "image/*");
#else
        Debug.LogWarning("当前平台未集成原生相机插件，图片选择不可用。");
#endif
#endif
    }

    // 不再需要反射回调方法
}
