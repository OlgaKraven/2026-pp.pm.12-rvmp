using System.Collections.Generic;
using UnityEngine;

public class WorldBuilder : MonoBehaviour
{
    public PlayerController Player { get; private set; }
    private GameObject root;
    private readonly Dictionary<int, Sprite> sprites = new();
    private readonly Vector2[] crystals =
    {
        new(-5.7f, 2.8f), new(-2.5f, 0.8f), new(0.8f, 3),
        new(3.2f, -1.8f), new(5.7f, 2.2f), new(-1, -3), new(6, -3)
    };
    private readonly Vector2[] hazards =
    {
        new(-4, -1.8f), new(-0.6f, -0.2f), new(2.2f, 1.5f),
        new(5, -0.5f), new(-6, 0), new(0, -3.5f)
    };

    public void BuildCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 6.2f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = VisualTheme.Background;
        if (!cam.GetComponent<AudioListener>())
            cam.gameObject.AddComponent<AudioListener>();
    }

    public void Clear()
    {
        if (!root) return;
        root.SetActive(false);
        Destroy(root);
    }

    public void Build(LevelData data)
    {
        Clear();
        root = new GameObject("LevelObjects");
        Item("Field", Vector2.zero, new(17, 9.5f), VisualTheme.Surface, 0, -10);
        Wall("Top", new(0, 4.7f), new(17, 0.3f));
        Wall("Bottom", new(0, -4.7f), new(17, 0.3f));
        Wall("Left", new(-8.4f, 0), new(0.3f, 9.6f));
        Wall("Right", new(8.4f, 0), new(0.3f, 9.6f));
        Wall("A", new(-3.8f, 1.2f), new(0.35f, 3));
        if (data.target >= 5)
            Wall("B", new(1.4f, -2.2f), new(3.2f, 0.35f));
        if (data.target >= 7)
            Wall("C", new(4.4f, 2), new(0.35f, 2.5f));
        var player = Item("Player", new(-6.5f, -3.1f), new(0.7f, 0.7f),
            VisualTheme.Player, 1, 10);
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<CircleCollider2D>();
        Player = player.AddComponent<PlayerController>();
        for (int i = 0; i < Mathf.Clamp(data.target, 1, crystals.Length); i++)
        {
            var c = Item("Crystal" + (i + 1), crystals[i], new(0.6f, 0.8f),
                VisualTheme.Collectible, 2, 5);
            c.AddComponent<PolygonCollider2D>();
            c.AddComponent<CollectibleVisual>();
            c.AddComponent<Collectible>();
        }
        for (int i = 0; i < Mathf.Clamp(data.hazards, 1, hazards.Length); i++)
        {
            var h = Item("Hazard" + (i + 1), hazards[i], new(0.8f, 0.8f),
                VisualTheme.Danger, 3, 4);
            h.AddComponent<CircleCollider2D>();
            h.AddComponent<Hazard>();
        }
        var exit = Item("Exit", new(6.5f, 3.1f), new(1, 1),
            VisualTheme.Success, 4, 2);
        exit.AddComponent<BoxCollider2D>();
        exit.AddComponent<ExitZone>();
    }

    private void Wall(string name, Vector2 p, Vector2 scale)
    {
        Item("Wall" + name, p, scale, VisualTheme.Button, 0, 1)
            .AddComponent<BoxCollider2D>();
    }

    private GameObject Item(string name, Vector2 p, Vector2 scale,
        Color color, int shape, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform);
        go.transform.position = p;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite(shape);
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    private Sprite GetSprite(int shape)
    {
        if (sprites.TryGetValue(shape, out Sprite cached)) return cached;
        const int size = 64;
        var texture = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float a = (x + 0.5f) / size * 2 - 1;
            float b = (y + 0.5f) / size * 2 - 1;
            bool inside = shape switch
            {
                1 => a * a + b * b < 0.9f,
                2 => Mathf.Abs(a) + Mathf.Abs(b) < 0.92f,
                3 => Mathf.Abs(a - b) < 0.2f || Mathf.Abs(a + b) < 0.2f,
                4 => Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)) > 0.65f,
                _ => true
            };
            texture.SetPixel(x, y, inside ? Color.white : Color.clear);
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
        sprites[shape] = sprite;
        return sprite;
    }

    private void OnDestroy()
    {
        foreach (Sprite sprite in sprites.Values)
        {
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
