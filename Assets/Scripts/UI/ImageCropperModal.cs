using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImageCropperModal : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform root;
    private RectTransform cropArea;
    private RawImage rawImage;
    private RectTransform rawRect;
    private Slider zoomSlider;
    private Button cancelButton;
    private Button okButton;
    private TMP_Text title;
    private Canvas canvas;

    private Texture2D sourceTexture;
    private Action<Texture2D> onDone;

    private Vector2 dragStartLocal;
    private Vector2 rawStartAnchored;
    private bool dragging;

    public float maxZoomMultiplier = 3f;
    public Vector2 padding = new Vector2(32f, 32f);

    public void Show(Texture2D texture, Action<Texture2D> onDoneCallback)
    {
        EnsureUI();
        onDone = onDoneCallback;
        sourceTexture = GetReadableTexture(texture);
        rawImage.texture = sourceTexture;
        rawImage.color = Color.white;
        FitToCropArea();
        gameObject.SetActive(true);
    }

    private void EnsureUI()
    {
        if (root != null) return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        root = gameObject.GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var bg = gameObject.GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        bg.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(900f, 1400f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(252f / 255f, 246f / 255f, 227f / 255f, 1f);
        panelImg.sprite = RoundedRectSpriteCache.Get(64, 16);
        panelImg.type = Image.Type.Sliced;

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panelRt, false);
        title = titleGo.AddComponent<TextMeshProUGUI>();
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -36f);
        titleRt.sizeDelta = new Vector2(860f, 60f);
        title.text = "Crop";
        title.fontSize = 40;
        title.alignment = TextAlignmentOptions.Center;

        var cropGo = new GameObject("CropArea", typeof(RectTransform));
        cropGo.transform.SetParent(panelRt, false);
        cropArea = cropGo.GetComponent<RectTransform>();
        cropArea.anchorMin = new Vector2(0.5f, 0.5f);
        cropArea.anchorMax = new Vector2(0.5f, 0.5f);
        cropArea.pivot = new Vector2(0.5f, 0.5f);
        cropArea.anchoredPosition = new Vector2(0f, 120f);
        cropArea.sizeDelta = new Vector2(820f, 820f);
        var cropBg = cropGo.AddComponent<Image>();
        cropBg.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        cropBg.raycastTarget = true;

        var mask = cropGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var rawGo = new GameObject("RawImage", typeof(RectTransform));
        rawGo.transform.SetParent(cropArea, false);
        rawImage = rawGo.AddComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawRect = rawImage.rectTransform;
        rawRect.anchorMin = new Vector2(0.5f, 0.5f);
        rawRect.anchorMax = new Vector2(0.5f, 0.5f);
        rawRect.pivot = new Vector2(0.5f, 0.5f);
        rawRect.anchoredPosition = Vector2.zero;

        var dragCatcher = new GameObject("DragCatcher", typeof(RectTransform));
        dragCatcher.transform.SetParent(cropArea, false);
        var dragRt = dragCatcher.GetComponent<RectTransform>();
        dragRt.anchorMin = Vector2.zero;
        dragRt.anchorMax = Vector2.one;
        dragRt.offsetMin = Vector2.zero;
        dragRt.offsetMax = Vector2.zero;
        var dragImg = dragCatcher.AddComponent<Image>();
        dragImg.color = new Color(1f, 1f, 1f, 0f);
        dragImg.raycastTarget = true;
        var proxy = dragCatcher.AddComponent<DragProxy>();
        proxy.owner = this;

        var zoomGo = new GameObject("ZoomSlider", typeof(RectTransform));
        zoomGo.transform.SetParent(panelRt, false);
        var zoomRt = zoomGo.GetComponent<RectTransform>();
        zoomRt.anchorMin = new Vector2(0.5f, 0f);
        zoomRt.anchorMax = new Vector2(0.5f, 0f);
        zoomRt.pivot = new Vector2(0.5f, 0f);
        zoomRt.anchoredPosition = new Vector2(0f, 220f);
        zoomRt.sizeDelta = new Vector2(760f, 24f);
        var zoomBg = zoomGo.AddComponent<Image>();
        zoomBg.color = new Color(0.8f, 0.8f, 0.85f, 1f);
        zoomSlider = zoomGo.AddComponent<Slider>();
        zoomSlider.direction = Slider.Direction.LeftToRight;
        zoomSlider.wholeNumbers = false;
        zoomSlider.onValueChanged.AddListener(_ => ApplyZoom());
        var handleGo = new GameObject("Handle", typeof(RectTransform));
        handleGo.transform.SetParent(zoomRt, false);
        var handleRt = handleGo.GetComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0.5f);
        handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(36f, 36f);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        zoomSlider.targetGraphic = handleImg;
        zoomSlider.handleRect = handleRt;

        var buttonsGo = new GameObject("Buttons", typeof(RectTransform));
        buttonsGo.transform.SetParent(panelRt, false);
        var buttonsRt = buttonsGo.GetComponent<RectTransform>();
        buttonsRt.anchorMin = new Vector2(0.5f, 0f);
        buttonsRt.anchorMax = new Vector2(0.5f, 0f);
        buttonsRt.pivot = new Vector2(0.5f, 0f);
        buttonsRt.anchoredPosition = new Vector2(0f, 70f);
        buttonsRt.sizeDelta = new Vector2(820f, 100f);
        var hlg = buttonsGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.spacing = 30;
        hlg.padding = new RectOffset(20, 20, 0, 0);

        cancelButton = CreateButton(buttonsRt, "CancelButton", "Cancel", new Color(0.75f, 0.75f, 0.78f, 1f));
        okButton = CreateButton(buttonsRt, "OkButton", "OK", new Color(0.15f, 0.75f, 0.78f, 1f));

        cancelButton.onClick.AddListener(() => Close(null));
        okButton.onClick.AddListener(() => Close(CropToTexture()));

        gameObject.SetActive(false);
    }

    private Button CreateButton(RectTransform parent, string name, string text, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340f, 90f);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = RoundedRectSpriteCache.Get(64, 16);
        img.type = Image.Type.Sliced;
        var btn = go.AddComponent<Button>();
        var tGo = new GameObject("Text", typeof(RectTransform));
        tGo.transform.SetParent(rt, false);
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 34;
        tmp.alignment = TextAlignmentOptions.Center;
        var trt = tmp.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return btn;
    }

    private void FitToCropArea()
    {
        if (sourceTexture == null || cropArea == null || rawRect == null) return;
        rawRect.sizeDelta = new Vector2(sourceTexture.width, sourceTexture.height);
        rawRect.anchoredPosition = Vector2.zero;

        float cropW = cropArea.rect.width;
        float cropH = cropArea.rect.height;
        float sx = cropW / Mathf.Max(1f, sourceTexture.width);
        float sy = cropH / Mathf.Max(1f, sourceTexture.height);
        float baseScale = Mathf.Max(sx, sy);

        zoomSlider.minValue = baseScale;
        zoomSlider.maxValue = baseScale * Mathf.Max(1.01f, maxZoomMultiplier);
        zoomSlider.value = baseScale;
        ApplyZoom();
        ClampRawPosition();
    }

    private void ApplyZoom()
    {
        if (rawRect == null || zoomSlider == null) return;
        float s = zoomSlider.value;
        rawRect.localScale = new Vector3(s, s, 1f);
        ClampRawPosition();
    }

    private void ClampRawPosition()
    {
        if (rawRect == null || cropArea == null) return;
        float cropW = cropArea.rect.width;
        float cropH = cropArea.rect.height;
        Vector2 rawSize = rawRect.rect.size;
        float scaledW = rawSize.x * rawRect.localScale.x;
        float scaledH = rawSize.y * rawRect.localScale.y;
        float limitX = Mathf.Max(0f, (scaledW - cropW) * 0.5f);
        float limitY = Mathf.Max(0f, (scaledH - cropH) * 0.5f);
        Vector2 p = rawRect.anchoredPosition;
        p.x = Mathf.Clamp(p.x, -limitX, limitX);
        p.y = Mathf.Clamp(p.y, -limitY, limitY);
        rawRect.anchoredPosition = p;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cropArea == null || rawRect == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(cropArea, eventData.position, eventData.pressEventCamera, out dragStartLocal))
        {
            dragging = false;
            return;
        }
        rawStartAnchored = rawRect.anchoredPosition;
        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || cropArea == null || rawRect == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(cropArea, eventData.position, eventData.pressEventCamera, out var curLocal))
        {
            return;
        }
        Vector2 delta = curLocal - dragStartLocal;
        rawRect.anchoredPosition = rawStartAnchored + delta;
        ClampRawPosition();
    }

    private Texture2D CropToTexture()
    {
        if (sourceTexture == null || rawRect == null || cropArea == null) return null;

        Camera cam = GetEventCamera();
        var corners = new Vector3[4];
        cropArea.GetWorldCorners(corners);
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rawRect, screen, cam, out var local))
            {
                if (local.x < minX) minX = local.x;
                if (local.x > maxX) maxX = local.x;
                if (local.y < minY) minY = local.y;
                if (local.y > maxY) maxY = local.y;
            }
        }

        Rect r = rawRect.rect;
        float u0 = (minX - r.xMin) / Mathf.Max(1f, r.width);
        float u1 = (maxX - r.xMin) / Mathf.Max(1f, r.width);
        float v0 = (minY - r.yMin) / Mathf.Max(1f, r.height);
        float v1 = (maxY - r.yMin) / Mathf.Max(1f, r.height);
        u0 = Mathf.Clamp01(u0); v0 = Mathf.Clamp01(v0);
        u1 = Mathf.Clamp01(u1); v1 = Mathf.Clamp01(v1);

        int texW = sourceTexture.width;
        int texH = sourceTexture.height;
        int x0 = Mathf.FloorToInt(u0 * texW);
        int x1 = Mathf.CeilToInt(u1 * texW);
        int y0 = Mathf.FloorToInt(v0 * texH);
        int y1 = Mathf.CeilToInt(v1 * texH);
        int w = Mathf.Clamp(x1 - x0, 1, texW);
        int h = Mathf.Clamp(y1 - y0, 1, texH);
        int size = Mathf.Clamp(Mathf.Min(w, h), 1, Mathf.Min(texW, texH));
        int cx = x0 + w / 2;
        int cy = y0 + h / 2;
        int x = Mathf.Clamp(cx - size / 2, 0, texW - size);
        int y = Mathf.Clamp(cy - size / 2, 0, texH - size);

        Color32[] src = sourceTexture.GetPixels32();
        Color32[] dst = new Color32[size * size];
        for (int iy = 0; iy < size; iy++)
        {
            int srcRow = (y + iy) * texW + x;
            int dstRow = iy * size;
            for (int ix = 0; ix < size; ix++)
            {
                dst[dstRow + ix] = src[srcRow + ix];
            }
        }

        Texture2D outTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        outTex.SetPixels32(dst);
        outTex.Apply();
        outTex.filterMode = FilterMode.Bilinear;
        outTex.wrapMode = TextureWrapMode.Clamp;
        outTex.name = sourceTexture.name + "_Cropped";
        return outTex;
    }

    private void Close(Texture2D result)
    {
        gameObject.SetActive(false);
        var cb = onDone;
        onDone = null;
        cb?.Invoke(result);
    }

    private Texture2D GetReadableTexture(Texture2D source)
    {
        if (source == null) return null;
        if (source.isReadable) return source;

        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        Graphics.Blit(source, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        tex.name = source.name + "_Readable";
        return tex;
    }

    private Camera GetEventCamera()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }

    private class DragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ImageCropperModal owner;

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null) owner.dragging = false;
        }
    }
}
