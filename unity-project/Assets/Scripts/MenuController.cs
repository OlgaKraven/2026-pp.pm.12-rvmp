using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MenuController : MonoBehaviour
{
    private GameManager game;
    private GameObject menu, hud, pause, result, settings;
    private Text progress, hint, resultTitle, resultText, volumeText;
    [SerializeField] private Button[] levels = new Button[3];
    private Button next;
    private Font font;
    private Transform canvas;

    private void Start()
    {
        game = GameManager.Instance;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("Canvas", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.transform;
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem),
                typeof(StandaloneInputModule));
        BuildMenu();
        BuildHud();
        BuildOverlays();
    }

    private void BuildMenu()
    {
        menu = Panel("MainMenuPanel", canvas, 0, 0, 860, 550);
        Label(menu.transform, "КРИСТАЛЬНЫЙ МАРШРУТ", 0, 205, 800, 50, 34);
        Label(menu.transform, "Три маршрута. Соберите кристаллы и найдите выход.",
            0, 140, 800, 40, 22);
        for (int i = 0; i < 3; i++)
        {
            int number = i + 1;
            levels[i] = Button(menu.transform, "Level" + number,
                "", 0, 60 - i * 85, 640, 66, () => game.StartLevel(number));
        }
        Button(menu.transform, "Settings", "Настройки звука", 0, -215,
            420, 55, () => ShowSettings(true));
    }

    private void BuildHud()
    {
        hud = new GameObject("GamePanel", typeof(RectTransform));
        hud.transform.SetParent(canvas, false);
        var bar = Panel("HUDPanel", hud.transform, 0, 305, 1230, 80);
        progress = Label(bar.transform, "", -70, 0, 1020, 55, 24);
        Button(bar.transform, "PauseButton", "Пауза", 525, 0, 160, 54, game.Pause);
        hint = Label(hud.transform, "", 120, -305, 820, 50, 21);
        Direction("Left", "←", -565, -295, Vector2.left);
        Direction("Down", "↓", -475, -295, Vector2.down);
        Direction("Right", "→", -385, -295, Vector2.right);
        Direction("Up", "↑", -475, -205, Vector2.up);
    }

    private void Direction(string name, string label, float x, float y, Vector2 d)
    {
        var button = Button(hud.transform, name, label, x, y, 80, 80, null);
        button.gameObject.AddComponent<HoldButton>().direction = d;
    }

    private void BuildOverlays()
    {
        pause = Panel("PausePanel", canvas, 0, 0, 700, 360);
        Label(pause.transform, "ПАУЗА", 0, 115, 620, 60, 36);
        Button(pause.transform, "Resume", "Продолжить", 0, 15, 420, 65,
            game.TogglePause);
        Button(pause.transform, "Menu", "В меню", 0, -80, 420, 65, game.ToMenu);
        result = Panel("ResultPanel", canvas, 0, 0, 780, 450);
        resultTitle = Label(result.transform, "", 0, 165, 740, 55, 32);
        resultText = Label(result.transform, "", 0, 95, 740, 50, 22);
        next = Button(result.transform, "Next", "Следующий уровень", 0, 10,
            480, 65, () => game.StartLevel(game.Levels.Current + 1));
        Button(result.transform, "Retry", "Повторить уровень", 0, -75,
            480, 65, () => game.StartLevel(game.Levels.Current));
        Button(result.transform, "Menu", "В меню", 0, -160, 480, 65, game.ToMenu);
        settings = Panel("SettingsPanel", canvas, 0, 0, 780, 450);
        Label(settings.transform, "НАСТРОЙКИ", 0, 160, 700, 60, 34);
        volumeText = Label(settings.transform, "", 0, 70, 700, 50, 28);
        Button(settings.transform, "Minus", "−", -140, -20, 120, 65,
            () => SaveService.SetVolume(SaveService.Volume - 0.1f));
        Button(settings.transform, "Plus", "+", 140, -20, 120, 65,
            () => SaveService.SetVolume(SaveService.Volume + 0.1f));
        Button(settings.transform, "Back", "Назад", 0, -140, 420, 65,
            () => ShowSettings(false));
        settings.SetActive(false);
    }

    private void ShowSettings(bool show)
    {
        settings.SetActive(show);
        menu.SetActive(!show);
    }

    private void Update()
    {
        if (game == null) return;
        var state = game.CurrentState;
        menu.SetActive(state == GameManager.State.Menu && !settings.activeSelf);
        hud.SetActive(state != GameManager.State.Menu);
        pause.SetActive(state == GameManager.State.Paused);
        bool ended = state == GameManager.State.Won || state == GameManager.State.Lost;
        result.SetActive(ended);
        if (state != GameManager.State.Menu) settings.SetActive(false);
        for (int i = 0; i < 3; i++)
        {
            bool unlocked = i + 1 <= SaveService.Unlocked;
            levels[i].interactable = unlocked;
            levels[i].GetComponentInChildren<Text>().text = unlocked
                ? $"Уровень {i + 1}  ·  рекорд {SaveService.Best(i + 1)}"
                : $"Уровень {i + 1}  ·  закрыт";
        }
        volumeText.text = $"Громкость: {Mathf.RoundToInt(SaveService.Volume * 100)}%";
        if (game.Levels.Current == 0) return;
        progress.text = $"Уровень {game.Levels.Current}   Кристаллы " +
            $"{game.Collected}/{game.Levels.Data.target}   Жизни {game.Hearts}" +
            $"   Время {Mathf.CeilToInt(game.Remaining)}";
        hint.text = game.Hint;
        resultTitle.text = state == GameManager.State.Won
            ? (game.Levels.Current == 3 ? "ИГРА ПРОЙДЕНА" : "УРОВЕНЬ ПРОЙДЕН")
            : "ПОПРОБУЙТЕ ЕЩЁ";
        resultText.text = game.Hint;
        next.gameObject.SetActive(state == GameManager.State.Won && game.Levels.Current < 3);
    }

    private GameObject Panel(string name, Transform parent,
        float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Place(go, parent, x, y, width, height);
        go.GetComponent<Image>().color = VisualTheme.Panel;
        return go;
    }

    private Text Label(Transform parent, string value, float x, float y,
        float width, float height, int size)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        Place(go, parent, x, y, width, height);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }

    private Button Button(Transform parent, string name, string label,
        float x, float y, float width, float height, UnityAction action)
    {
        var go = Panel(name, parent, x, y, width, height);
        go.GetComponent<Image>().color = VisualTheme.Accent;
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        if (action != null) button.onClick.AddListener(action);
        Label(go.transform, label, 0, 0, width - 12, height - 8, 24);
        return button;
    }

    private void Place(GameObject go, Transform parent,
        float x, float y, float width, float height)
    {
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);
    }
}
