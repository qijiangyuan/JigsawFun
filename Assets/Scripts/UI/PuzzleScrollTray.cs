using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleScrollTray : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Transform contentContainer;
    public GameObject trayItemPrefab;

    public Color trayBackgroundColor = new Color(252f / 255f, 246f / 255f, 227f / 255f, 1f);
    [Range(0.5f, 1f)]
    public float trayBackgroundDarkenFactor = 0.95f;
    
    // Create the UI structure programmatically if not assigned
    public void Initialize()
    {
        if (scrollRect == null)
        {
            // Create Scroll View
            GameObject svObj = new GameObject("PuzzleScrollView");
            svObj.transform.SetParent(transform, false);
            
            RectTransform svRect = svObj.AddComponent<RectTransform>();
            svRect.anchorMin = new Vector2(0, 0);
            svRect.anchorMax = new Vector2(1, 0.20f); // Bottom 15%
            svRect.offsetMin = Vector2.zero;
            svRect.offsetMax = Vector2.zero;
            
            Image bg = svObj.AddComponent<Image>();
            bg.color = GetTrayBackgroundColor();
            
            scrollRect = svObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(svObj.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            
            // Use RectMask2D for better performance and simplicity
            viewport.AddComponent<RectMask2D>();
            
            scrollRect.viewport = vpRect;
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 0.5f);
            cRect.anchorMax = new Vector2(0, 0.5f);
            cRect.pivot = new Vector2(0, 0.5f);
            
            HorizontalLayoutGroup hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.spacing = 10;
            hlg.padding = new RectOffset(10, 10, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            
            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = cRect;
            contentContainer = content.transform;
        }
        ApplyTrayBackgroundColor();
    }

    private void ApplyTrayBackgroundColor()
    {
        if (scrollRect == null) return;
        var img = scrollRect.GetComponent<Image>();
        if (img == null) return;
        img.color = GetTrayBackgroundColor();
    }

    private Color GetTrayBackgroundColor()
    {
        float f = Mathf.Clamp01(trayBackgroundDarkenFactor);
        return new Color(
            Mathf.Clamp01(trayBackgroundColor.r * f),
            Mathf.Clamp01(trayBackgroundColor.g * f),
            Mathf.Clamp01(trayBackgroundColor.b * f),
            trayBackgroundColor.a
        );
    }
    
    public void AddPiece(GameObject piece)
    {
        if (trayItemPrefab == null)
        {
            CreateDefaultItemPrefab();
        }
        
        GameObject itemObj = Instantiate(trayItemPrefab, contentContainer);
        itemObj.SetActive(true);
        
        // Reset scale and position to ensure visibility
        RectTransform rt = itemObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0);
        rt.sizeDelta = new Vector2(150, 150); // Fixed size for icons
        
        PuzzleTrayItem item = itemObj.GetComponent<PuzzleTrayItem>();
        item.Setup(piece, scrollRect);
    }
    
    public void Clear()
    {
        if (contentContainer != null)
        {
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void CreateDefaultItemPrefab()
    {
        GameObject prefabObj = new GameObject("TrayItemPrefab");
        prefabObj.AddComponent<RectTransform>();
        prefabObj.AddComponent<Image>();
        prefabObj.AddComponent<PuzzleTrayItem>();
        
        // Keep it as a template, don't destroy, but we can't save as asset at runtime easily.
        // So we just keep it in memory or create on fly.
        // Actually, let's just create on fly in AddPiece if prefab is null.
        trayItemPrefab = prefabObj;
        prefabObj.SetActive(false); // Hide template
        prefabObj.transform.SetParent(transform);
    }
}
