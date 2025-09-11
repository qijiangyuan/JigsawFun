using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public class UICameraController : MonoBehaviour
{
    private Camera uiCamera;

    void Awake()
    {
        uiCamera = GetComponent<Camera>();
        DontDestroyOnLoad(gameObject);

        // 初始进入 Main 场景 → UICamera 必须能独立渲染
        SetAsBase();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAttachToGameCamera();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // 如果 Game 场景被卸载 → UICamera 恢复 Base
        if (scene.name == "Game")
        {
            Debug.Log("Game 卸载，UICamera 切回 Base");
            SetAsBase();
        }
    }

    private void TryAttachToGameCamera()
    {
        // 查找 GameCamera
        var cameras = GameObject.FindObjectsOfType<Camera>();
        foreach (var cam in cameras)
        {
            if (cam == uiCamera) continue;

            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null && camData.renderType == CameraRenderType.Base)
            {
                // 找到 Base → 把 UICamera 挂过去
                var uiData = uiCamera.GetUniversalAdditionalCameraData();
                uiData.renderType = CameraRenderType.Overlay;

                if (!camData.cameraStack.Contains(uiCamera))
                {
                    camData.cameraStack.Add(uiCamera);
                    Debug.Log("UICamera 已加入 GameCamera Stack");
                }
                return;
            }
        }
    }

    private void SetAsBase()
    {
        var data = uiCamera.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Base;
    }
}
