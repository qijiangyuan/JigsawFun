using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PuzzleTrayItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject worldPiece;
    public ScrollRect scrollRect;
    
    private Image uiImage;
    private CanvasGroup canvasGroup;
    private TileMovement tileMovement;
    private bool isDragging = false;
    private bool isDraggingPiece = false; // Whether we are dragging the puzzle piece (true) or scrolling the list (false)

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
                // Destroy this UI item as the piece is now on the board
                Destroy(gameObject);
            }
            else
            {
                // Return to tray
                worldPiece.SetActive(false);
                uiImage.color = Color.white;
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
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0; // Ensure 2D
        worldPiece.transform.position = worldPos;
    }
}
