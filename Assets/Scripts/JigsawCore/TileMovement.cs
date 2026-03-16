using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileMovement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Tile tile { get; set; }
    private Vector3 mOffset = new Vector3(0.0f, 0.0f, 0.0f);

    private SpriteRenderer mSpriteRenderer;
    [Header("Drag")]
    public float dragUpOffsetFactor = 0.5f;
    private bool placedNotified = false;
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
        Tile.tilesSorting.BringToTop(mSpriteRenderer);
    }

    private void OnMouseDrag()
    {
        //if (!GameManager.Instance.TileMovementEnabled) return;
        var cam = Camera.main;
        float zDist = Mathf.Abs(transform.position.z - cam.transform.position.z);
        Vector3 mouseWp = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDist));
        mouseWp.z = 0f;
        Vector3 centerOffsetWorld = mSpriteRenderer.bounds.center - transform.position;
        float up = mSpriteRenderer.bounds.size.y * dragUpOffsetFactor;
        transform.position = mouseWp - centerOffsetWorld + new Vector3(0f, up, 0f);
    }

    public bool CheckAndSnap()
    {
        float dist = (transform.position - GetCorrectPosition()).magnitude;
        float snapThreshold = Mathf.Max(4f, Tile.tileSize * 0.2f);
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
        float dist = (transform.position - GetCorrectPosition()).magnitude;
        // 贴合阈值按 tileSize 缩放（例如 20% 的边长），兼容不同 n/tileSize
        float snapThreshold = Mathf.Max(4f, Tile.tileSize * 0.2f);
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
        Tile.tilesSorting.BringToTop(mSpriteRenderer);
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
        float up = mSpriteRenderer.bounds.size.y * dragUpOffsetFactor;
        transform.position = mouseWp - centerOffsetWorld + new Vector3(0f, up, 0f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("松开");
        float dist = (transform.position - GetCorrectPosition()).magnitude;
        // 贴合阈值按 tileSize 缩放（例如 20% 的边长），兼容不同 n/tileSize
        float snapThreshold = Mathf.Max(4f, Tile.tileSize * 0.2f);
        if (dist < snapThreshold)
        {
            transform.position = GetCorrectPosition();
            NotifyPlaced();
        }
    }

    private void NotifyPlaced()
    {
        if (placedNotified) return;
        placedNotified = true;
        onTileInPlace?.Invoke(this);
    }
}
