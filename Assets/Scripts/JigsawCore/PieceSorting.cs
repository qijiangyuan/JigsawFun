using UnityEngine;

public static class PieceSorting
{
    public const int LayerPlaced = 0;
    public const int LayerIdleBase = 1000;
    public const int LayerLastReleasedBase = 2000;
    public const int LayerDraggingBase = 3000;

    private static int seq = 0;
    private static SpriteRenderer lastReleased;

    public static void SetPlaced(SpriteRenderer sr)
    {
        if (sr == null) return;
        Apply(sr, LayerPlaced);
    }

    public static void SetIdle(SpriteRenderer sr)
    {
        if (sr == null) return;
        seq++;
        Apply(sr, LayerIdleBase + (seq % 512));
    }

    public static void SetDragging(SpriteRenderer sr)
    {
        if (sr == null) return;
        seq++;
        Apply(sr, LayerDraggingBase + (seq % 512));
    }

    public static void SetLastReleased(SpriteRenderer sr)
    {
        if (sr == null) return;
        if (lastReleased != null && lastReleased != sr)
        {
            SetIdle(lastReleased);
        }
        lastReleased = sr;
        seq++;
        Apply(sr, LayerLastReleasedBase + (seq % 512));
    }

    private static void Apply(SpriteRenderer sr, int order)
    {
        sr.sortingOrder = order;
        var p = sr.transform.position;
        p.z = -order / 1000.0f;
        sr.transform.position = p;

        var shadow = sr.transform.Find("PieceShadow");
        if (shadow != null)
        {
            var srs = shadow.GetComponent<SpriteRenderer>();
            if (srs != null)
            {
                srs.sortingOrder = order;
                var sp = shadow.position;
                sp.z = -order / 1000.0f;
                shadow.position = sp;
            }
        }
    }
}

