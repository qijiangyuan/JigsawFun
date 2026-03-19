using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileMovement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Tile tile { get; set; }
    private Vector3 mOffset = new Vector3(0.0f, 0.0f, 0.0f);

    private SpriteRenderer mSpriteRenderer;

    public float snapratio = 0.5f; // 吸附距离
    [Header("Drag")]
    public float dragUpOffsetFactor = 0.5f;
    public AudioClip dragSound;
    public AudioClip snapSound;
    private bool placedNotified = false;
    private int lastDragStartFrame = -1;
    private static Vector3 ContentCenterOffset()
    {
        // 以内容区域中心为准（pivot 为左下角）
        return new Vector3(Tile.padding + Tile.tileSize * 0.5f, Tile.padding + Tile.tileSize * 0.5f, 0f);
    }

    public delegate void DelegateOnTileInPlace(TileMovement tm);
    public DelegateOnTileInPlace onTileInPlace;

    void Start()
    {
        mSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (mSpriteRenderer == null) mSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private Vector3 GetCorrectPosition()
    {
        return new Vector3(tile.xIndex * Tile.tileSize, tile.yIndex * Tile.tileSize, 0f);
    }

    private void OnMouseDown()
    {
        //if (!GameManager.Instance.TileMovementEnabled) return;
        mOffset = Vector3.zero; // 始终以拼图块内容中心对齐指针

        // For sorting of tiles.
        PieceSorting.SetDragging(mSpriteRenderer);
        TryPlayDragSound();
    }

    private void OnMouseDrag()
    {
        //if (!GameManager.Instance.TileMovementEnabled) return;
        var cam = Camera.main;
        float zDist = Mathf.Abs(transform.position.z - cam.transform.position.z);
        Vector3 mouseWp = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDist));
        mouseWp.z = 0f;
        Vector3 centerOffsetWorld = mSpriteRenderer.bounds.center - transform.position;
        transform.position = mouseWp - centerOffsetWorld;
    }

    public bool CheckAndSnap()
    {
        float dist = (transform.position - GetCorrectPosition()).magnitude;
        float snapThreshold = Mathf.Max(4f, Tile.tileSize * snapratio);
        if (dist < snapThreshold)
        {
            transform.position = GetCorrectPosition();
            NotifyPlaced();
            Physics2D.SyncTransforms();
            return true;
        }
        return false;
    }

    private void OnMouseUp()
    {
        //if (!GameManager.Instance.TileMovementEnabled) return;
        if (TryReturnToTray(Input.mousePosition)) return;
        float dist = (transform.position - GetCorrectPosition()).magnitude;
        // 贴合阈值按 tileSize 缩放（例如 20% 的边长），兼容不同 n/tileSize
        float snapThreshold = Mathf.Max(4f, Tile.tileSize * snapratio);
        if (dist < snapThreshold)
        {
            transform.position = GetCorrectPosition();
            NotifyPlaced();
        }
        Physics2D.SyncTransforms();
    }

    public void SnapToCorretPosition()
    {
        transform.DOMove(GetCorrectPosition(), 0.2f).SetEase(Ease.OutCubic);
        NotifyPlaced();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("按下");

        mOffset = Vector3.zero; // 始终以拼图块内容中心对齐指针

        // For sorting of tiles.
        PieceSorting.SetDragging(mSpriteRenderer);
        TryPlayDragSound();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("拖拽中");
        //transform.position = eventData.position; // 简单移动到鼠标/手指位置

        var cam = Camera.main;
        float zDist = Mathf.Abs(transform.position.z - cam.transform.position.z);
        Vector3 mouseWp = cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, zDist));
        mouseWp.z = 0f;
        Vector3 centerOffsetWorld = mSpriteRenderer.bounds.center - transform.position;
        transform.position = mouseWp - centerOffsetWorld;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("松开");
        if (TryReturnToTray(eventData.position)) return;
        float dist = (transform.position - GetCorrectPosition()).magnitude;
        // 贴合阈值按 tileSize 缩放（例如 20% 的边长），兼容不同 n/tileSize
        float snapThreshold = Mathf.Max(4f, Tile.tileSize * 0.2f);
        if (dist < snapThreshold)
        {
            transform.position = GetCorrectPosition();
            NotifyPlaced();
        }
        else
        {
            PieceSorting.SetLastReleased(mSpriteRenderer);
        }
    }

    private void NotifyPlaced()
    {
        if (placedNotified) return;
        placedNotified = true;
        TryPlaySnapSound();
        PieceSorting.SetPlaced(mSpriteRenderer);
        onTileInPlace?.Invoke(this);
    }

    private void TryPlayDragSound()
    {
        if (Time.frameCount == lastDragStartFrame) return;
        lastDragStartFrame = Time.frameCount;
        if (JigsawFun.Audio.AudioManager.Instance != null)
        {
            if (JigsawFun.Audio.AudioManager.Instance.HasSoundGroup("Drag"))
            {
                JigsawFun.Audio.AudioManager.Instance.PlayRandomSound("Drag");
                return;
            }
            if (dragSound != null)
            {
                JigsawFun.Audio.AudioManager.Instance.PlaySound(dragSound);
            }
        }
    }

    private void TryPlaySnapSound()
    {
        if (JigsawFun.Audio.AudioManager.Instance != null)
        {
            if (JigsawFun.Audio.AudioManager.Instance.HasSoundGroup("Snap"))
            {
                JigsawFun.Audio.AudioManager.Instance.PlayRandomSound("Snap");
                return;
            }
            if (snapSound != null)
            {
                JigsawFun.Audio.AudioManager.Instance.PlaySound(snapSound);
            }
        }
    }

    private bool TryReturnToTray(Vector2 screenPos)
    {
        if (placedNotified) return false;

        PuzzleScrollTray tray = FindObjectOfType<PuzzleScrollTray>(true);
        if (tray == null || tray.scrollRect == null) return false;

        RectTransform hitRect = tray.scrollRect.GetComponent<RectTransform>();
        if (hitRect == null) hitRect = tray.scrollRect.viewport;
        if (hitRect == null) return false;

        Canvas canvas = tray.scrollRect.GetComponentInParent<Canvas>();
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(hitRect, screenPos, uiCam)) return false;

        RemoveExistingTrayItem(tray);
        tray.AddPiece(gameObject);
        return true;
    }

    private void RemoveExistingTrayItem(PuzzleScrollTray tray)
    {
        if (tray == null || tray.contentContainer == null) return;
        for (int i = tray.contentContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = tray.contentContainer.GetChild(i);
            if (child == null) continue;
            PuzzleTrayItem item = child.GetComponent<PuzzleTrayItem>();
            if (item != null && item.worldPiece == gameObject)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
