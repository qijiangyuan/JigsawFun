using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class PuzzleTrayItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject worldPiece;
    public ScrollRect scrollRect;
    
    private Image uiImage;
    private CanvasGroup canvasGroup;
    private TileMovement tileMovement;
    private bool isDragging = false;
    private bool isDraggingPiece = false; // Whether we are dragging the puzzle piece (true) or scrolling the list (false)
    private Vector3 worldOriginalScale;
    public float dragScaleMultiplier = 1.0f;
    public float dragScaleInDuration = 0.2f;
    public float dragScaleOutDuration = 0.2f;

    void Awake()
    {
        uiImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(GameObject piece, ScrollRect scroll)
    {
        if (uiImage == null) uiImage = GetComponent<Image>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        worldPiece = piece;
        scrollRect = scroll;
        tileMovement = worldPiece.GetComponent<TileMovement>();
        worldOriginalScale = worldPiece != null ? worldPiece.transform.localScale : Vector3.one;
        
        // Create sprite from piece texture
        SpriteRenderer sr = worldPiece.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            uiImage.sprite = sr.sprite;
            uiImage.color = Color.white;
            // Preserve aspect ratio
            uiImage.preserveAspect = true;
        }
        else
        {
            Debug.LogError($"Puzzle piece {piece.name} has no sprite!");
            uiImage.color = Color.red; // Visual debug
        }
        
        // Hide world piece initially
        worldPiece.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        
        // Direction check: Horizontal for Scroll, Vertical for Pull Out
        // Since ScrollRect is Horizontal, we check if vertical movement dominates
        if (Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x))
        {
            isDraggingPiece = true;
            
            // Start dragging piece
            // Dim UI
            uiImage.color = new Color(1, 1, 1, 0f); // Invisible but present to hold layout
            
            // Show World Piece
            worldPiece.SetActive(true);
            worldPiece.transform.DOKill();
            worldPiece.transform.localScale = worldOriginalScale;
            worldPiece.transform.localScale = worldOriginalScale * 0.5f;
            worldPiece.transform.DOScale(worldOriginalScale * dragScaleMultiplier, dragScaleInDuration).SetEase(Ease.OutBack);
            UpdateWorldPosition(eventData);
            
            // Bring to top
            SpriteRenderer sr = worldPiece.GetComponent<SpriteRenderer>();
            if (sr != null) Tile.tilesSorting.BringToTop(sr);
        }
        else
        {
            isDraggingPiece = false;
            // Hand over to ScrollRect
            if (scrollRect != null) scrollRect.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            if (isDraggingPiece)
            {
                UpdateWorldPosition(eventData);
            }
            else
            {
                if (scrollRect != null) scrollRect.OnDrag(eventData);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        if (isDraggingPiece)
        {
            // End dragging piece
            isDraggingPiece = false;
            
            // Check if placed
            bool placed = false;
            if (tileMovement != null)
            {
                placed = tileMovement.CheckAndSnap();
            }
            
            if (placed)
            {
                worldPiece.transform.DOKill();
                worldPiece.transform.DOScale(worldOriginalScale, dragScaleOutDuration).SetEase(Ease.OutSine);
                // Destroy this UI item as the piece is now on the board
                Destroy(gameObject);
            }
            else
            {
                // 留在玩家松手的位置（不自动放回托盘）
                worldPiece.transform.DOKill();
                worldPiece.transform.DOScale(worldOriginalScale, dragScaleOutDuration).SetEase(Ease.OutSine);
                Destroy(gameObject);
            }
        }
        else
        {
            // End scrolling
            if (scrollRect != null) scrollRect.OnEndDrag(eventData);
        }
    }
    
    private void UpdateWorldPosition(PointerEventData eventData)
    {
        var cam = Camera.main;
        float zDist = Mathf.Abs(worldPiece.transform.position.z - cam.transform.position.z);
        Vector3 mouseWp = cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, zDist));
        mouseWp.z = 0f;
        var sr = worldPiece.GetComponent<SpriteRenderer>();
        var tm = worldPiece.GetComponent<TileMovement>();
        if (sr != null)
        {
            Vector3 centerOffset = sr.bounds.center - worldPiece.transform.position;
            float factor = tm != null ? tm.dragUpOffsetFactor : 0.2f;
            float up = sr.bounds.size.y * factor;
            worldPiece.transform.position = mouseWp - centerOffset + new Vector3(0f, up, 0f);
        }
        else
        {
            worldPiece.transform.position = mouseWp;
        }
    }
}
