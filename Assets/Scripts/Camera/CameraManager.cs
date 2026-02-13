using UnityEngine;
using System.IO;
using System;

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
        Debug.Log("在编辑器中无法调用相机，模拟拍照成功。");
        // 编辑器中模拟：随机选择一个现有图片（如果有的话）
        // 这里只是为了演示，实际应该提示用户在真机测试
#endif

        // NativeCamera API 调用
        NativeCamera.Permission permission = NativeCamera.TakePicture((path) =>
        {
            Debug.Log("照片保存路径: " + path);
            if (path != null)
            {
                // 加载拍摄的照片为 Texture2D
                Texture2D texture = NativeCamera.LoadImageAtPath(path, 2048, false);
                if (texture != null)
                {
                    Debug.Log("照片加载成功: " + texture.width + "x" + texture.height);
                    
                    // 保存到 persistentDataPath
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

        Debug.Log("相机权限状态: " + permission);
    }
}
