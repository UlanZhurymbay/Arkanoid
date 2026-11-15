using UnityEngine;

/// <summary>
/// Создаёт простые спрайты (квадрат и круг) прямо из кода,
/// чтобы игра работала без картинок. Позже их можно заменить на свои.
/// </summary>
public static class SpriteFactory
{
    private static Sprite _square;
    private static Sprite _circle;
    private static Material _material;

    /// <summary>Белый квадрат 1×1 единицу. Размер задаём через Transform.localScale.</summary>
    public static Sprite Square
    {
        get
        {
            if (_square == null)
            {
                var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                _square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            }
            return _square;
        }
    }

    /// <summary>Белый круг диаметром 1 единицу со сглаженным краем.</summary>
    public static Sprite Circle
    {
        get
        {
            if (_circle == null)
            {
                const int size = 64;
                var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                        float alpha = Mathf.Clamp01(r - d); // мягкий край в 1 пиксель
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                }
                tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }
            return _circle;
        }
    }

    /// <summary>Неосвещаемый материал — цвета видны как есть, без источников света.</summary>
    private static Material Material
    {
        get
        {
            if (_material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                _material = new Material(shader);
            }
            return _material;
        }
    }

    public static SpriteRenderer AddRenderer(GameObject go, Sprite sprite, Color color, int order = 0)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sharedMaterial = Material;
        sr.sortingOrder = order;
        return sr;
    }
}
