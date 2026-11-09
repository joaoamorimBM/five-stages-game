using System.Collections.Generic;
using UnityEngine;

// Sprites de retângulo arredondado gerados por código (9-slice), pra não depender de arte.
// Use com Image.type = Sliced: os cantos ficam fixos e o meio estica.
public static class UiShapes
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Rounded(int radius, int border, Color fill, Color line)
    {
        string key = radius + "/" + border + "/" + fill + "/" + line;
        if (Cache.TryGetValue(key, out Sprite s)) return s;

        int size = radius * 2 + 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] px = new Color[size * size];
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Distância até a borda do retângulo arredondado (negativa = dentro)
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                float outside = Mathf.Clamp01(dist + 0.5f);             // antisserrilhado da borda externa
                float inLine = Mathf.Clamp01(dist + border + 0.5f);      // 1 = dentro da faixa da linha
                Color c = Color.Lerp(fill, line, border > 0 ? inLine : 0f);
                c.a *= 1f - outside;
                px[y * size + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply(false, true);

        float b = radius + 1;
        s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        s.name = "arredondado_" + radius;
        Cache[key] = s;
        return s;
    }
}
