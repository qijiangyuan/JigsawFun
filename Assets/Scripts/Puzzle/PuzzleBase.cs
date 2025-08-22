using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 拼图底座脚本，支持摄像机拖拽移动和滚轮缩放
/// 使用EventSystem接口，支持多平台（鼠标和触摸）
/// </summary>
public class PuzzleBase : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("摄像机控制设置")]
    public Camera targetCamera; // 目标摄像机，如果为空则使用主摄像机
    public float dragSensitivity = 1.0f; // 拖拽灵敏度
    public float zoomSensitivity = 2.0f; // 缩放灵敏度
    public float minZoom = 3.0f; // 最小缩放值（透视摄像机的距离或正交摄像机的size）
    public float maxZoom = 50.0f; // 最大缩放值
    public LayerMask puzzleBaseLayer = -1; // 拼图底座图层
    
    private bool isDragging = false;
    private Vector3 lastDragPosition;
    private Vector3 dragStartCameraPosition;
    
    void Start()
    {
        // 如果没有指定摄像机，使用主摄像机
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera == null)
        {
            Debug.LogError("PuzzleBase: 未找到可用的摄像机！");
        }
    }

    void Update()
    {
        HandleCameraControls();
    }
    
    /// <summary>
    /// 处理摄像机控制
    /// </summary>
    void HandleCameraControls()
    {
        if (targetCamera == null) return;
        
        // 处理滚轮缩放
        HandleZoom();
        
        // 拖拽移动现在通过EventSystem接口处理
    }
    
    /// <summary>
    /// 处理滚轮缩放
    /// </summary>
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 获取鼠标在世界坐标中的位置作为缩放焦点
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            
            if (targetCamera.orthographic)
            {
                // 正交摄像机缩放
                ZoomOrthographic(scroll, mouseWorldPos);
            }
            else
            {
                // 透视摄像机缩放
                ZoomPerspective(scroll, mouseWorldPos);
            }
        }
    }
    
    /// <summary>
    /// 正交摄像机缩放
    /// </summary>
    void ZoomOrthographic(float scroll, Vector3 focusPoint)
    {
        float currentSize = targetCamera.orthographicSize;
        float newSize = currentSize - scroll * zoomSensitivity;
        newSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        
        // 计算缩放前后焦点的偏移
        Vector3 worldPosBefore = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        targetCamera.orthographicSize = newSize;
        Vector3 worldPosAfter = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        
        // 调整摄像机位置以保持焦点不变
        Vector3 offset = worldPosBefore - worldPosAfter;
        targetCamera.transform.position += offset;
    }
    
    /// <summary>
    /// 透视摄像机缩放
    /// </summary>
    void ZoomPerspective(float scroll, Vector3 focusPoint)
    {
        // 计算缩放前鼠标在世界坐标的位置
        Vector3 worldPosBefore = targetCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, targetCamera.transform.position.z));
        
        // 计算新的摄像机位置（沿着Z轴移动）
        Vector3 currentPos = targetCamera.transform.position;
        float zoomAmount = scroll * zoomSensitivity;
        float newZ = currentPos.z + zoomAmount;
        
        // 限制缩放范围（Z轴距离）
        newZ = Mathf.Clamp(newZ, -maxZoom, -minZoom);
        
        // 更新摄像机位置
        targetCamera.transform.position = new Vector3(currentPos.x, currentPos.y, newZ);
        
        // 计算缩放后鼠标在世界坐标的位置
        Vector3 worldPosAfter = targetCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, targetCamera.transform.position.z));
        
        // 调整摄像机位置以保持焦点不变
        Vector3 offset = worldPosBefore - worldPosAfter;
        targetCamera.transform.position += new Vector3(offset.x, offset.y, 0);
    }
    
    /// <summary>
    /// 开始拖拽事件（EventSystem接口）
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (targetCamera == null) return;
        
        isDragging = true;
        lastDragPosition = eventData.position;
        dragStartCameraPosition = targetCamera.transform.position;
    }
    
    /// <summary>
    /// 拖拽中事件（EventSystem接口）
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (targetCamera == null || !isDragging) return;
        
        Vector3 currentDragPosition = eventData.position;
        Vector3 deltaDragPosition = currentDragPosition - lastDragPosition;
        
        // 将屏幕坐标的移动转换为世界坐标的移动
        Vector3 worldDelta = ScreenToWorldDelta(deltaDragPosition);
        
        // 移动摄像机（方向相反，因为是移动视角）
        targetCamera.transform.position -= worldDelta * dragSensitivity;
        
        lastDragPosition = currentDragPosition;
    }
    
    /// <summary>
    /// 结束拖拽事件（EventSystem接口）
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
    

    
    /// <summary>
    /// 将屏幕坐标的移动转换为世界坐标的移动
    /// </summary>
    Vector3 ScreenToWorldDelta(Vector3 screenDelta)
    {
        if (targetCamera.orthographic)
        {
            // 正交摄像机
            float pixelsPerUnit = Screen.height / (2.0f * targetCamera.orthographicSize);
            return new Vector3(screenDelta.x / pixelsPerUnit, screenDelta.y / pixelsPerUnit, 0);
        }
        else
        {
            // 透视摄像机
            float distance = Mathf.Abs(targetCamera.transform.position.z);
            float frustumHeight = 2.0f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * targetCamera.aspect;
            
            return new Vector3(
                screenDelta.x * frustumWidth / Screen.width,
                screenDelta.y * frustumHeight / Screen.height,
                0
            );
        }
    }
    
    /// <summary>
    /// 获取鼠标在世界坐标中的位置
    /// </summary>
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        
        if (targetCamera.orthographic)
        {
            mouseScreenPos.z = targetCamera.nearClipPlane;
        }
        else
        {
            mouseScreenPos.z = Mathf.Abs(targetCamera.transform.position.z);
        }
        
        return targetCamera.ScreenToWorldPoint(mouseScreenPos);
    }
    
    /// <summary>
    /// 重置摄像机位置和缩放
    /// </summary>
    public void ResetCamera()
    {
        if (targetCamera == null) return;
        
        targetCamera.transform.position = new Vector3(0, 0, targetCamera.transform.position.z);
        
        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = (minZoom + maxZoom) / 2.0f;
        }
        else
        {
            targetCamera.transform.position = new Vector3(0, 0, -(minZoom + maxZoom) / 2.0f);
        }
    }
}
