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

        // ��ʼ���� Main ���� �� UICamera �����ܶ�����Ⱦ
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
        // ��� Game ������ж�� �� UICamera �ָ� Base
        if (scene.name == "Game")
        {
            Debug.Log("Game ж�أ�UICamera �л� Base");
            SetAsBase();
        }
    }

    private void TryAttachToGameCamera()
    {
        // ���� GameCamera
        var cameras = GameObject.FindObjectsOfType<Camera>();
        foreach (var cam in cameras)
        {
            if (cam == uiCamera) continue;

            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null && camData.renderType == CameraRenderType.Base)
            {
                // �ҵ� Base �� �� UICamera �ҹ�ȥ
                var uiData = uiCamera.GetUniversalAdditionalCameraData();
                uiData.renderType = CameraRenderType.Overlay;

                if (!camData.cameraStack.Contains(uiCamera))
                {
                    camData.cameraStack.Add(uiCamera);
                    Debug.Log("UICamera �Ѽ��� GameCamera Stack");
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
