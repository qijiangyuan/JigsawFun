using System.Collections.Generic;
using UnityEngine;

public static class RoundedRectSpriteCache
{
    private struct Key
    {
        public int size;
        public int radius;

        public Key(int size, int radius)
        {
            this.size = size;
            this.radius = radius;
        }
    }

    private class KeyComparer : IEqualityComparer<Key>
    {
        public bool Equals(Key x, Key y) => x.size == y.size && x.radius == y.radius;
        public int GetHashCode(Key obj) => (obj.size * 397) ^ obj.radius;
    }

    private static readonly Dictionary<Key, Sprite> cache = new Dictionary<Key, Sprite>(new KeyComparer());

    public static Sprite Get(int size = 64, int radius = 16, float pixelsPerUnit = 100f)
    {
        size = Mathf.Clamp(size, 16, 512);
        radius = Mathf.Clamp(radius, 1, size / 2 - 1);

        var key = new Key(size, radius);
        if (cache.TryGetValue(key, out var s) && s != null) return s;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.name = $"RoundedRect_{size}_{radius}";

        var pixels = new Color32[size * size];
        float r = radius;
        float rr = r * r;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);

                bool inside;
                if (dx >= radius || dy >= radius)
                {
                    inside = true;
                }
                else
                {
                    float cx = (radius - 0.5f) - dx;
                    float cy = (radius - 0.5f) - dy;
                    inside = (cx * cx + cy * cy) <= rr;
                }

                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        var border = new Vector4(radius, radius, radius, radius);
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        cache[key] = sprite;
        return sprite;
    }
}

