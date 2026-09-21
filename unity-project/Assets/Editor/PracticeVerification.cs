using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Запуск из меню только в копии проекта для проверки.
[InitializeOnLoad]
public static class PracticeVerification
{
    private static int step;
    private static double next;
    private static int oldUnlocked;
    private static float oldVolume;
    private static readonly int[] oldBest = new int[3];
    private const string Prefix = "PP12.Crystal.v1.";

    static PracticeVerification()
    {
        if (SessionState.GetBool("PP12.Verify", false))
        {
            step = 0;
            next = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update += Tick;
        }
    }

    [MenuItem("Practice/Verify And Capture")]
    public static void Run()
    {
        Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/verification.txt", "RUNNING");
        SessionState.SetBool("PP12.Verify", true);
        oldUnlocked = SaveService.Unlocked;
        oldVolume = SaveService.Volume;
        for (int i = 0; i < 3; i++) oldBest[i] = SaveService.Best(i + 1);
        SessionState.SetInt("PP12.OldUnlocked", oldUnlocked);
        SessionState.SetFloat("PP12.OldVolume", oldVolume);
        for (int i = 0; i < 3; i++) SessionState.SetInt("PP12.OldBest" + i, oldBest[i]);
        PlayerPrefs.SetInt(Prefix + "Unlocked", 1);
        PlayerPrefs.Save();
        Application.runInBackground = true;
        PracticeProjectSetup.PrepareMainScene();
        EditorApplication.isPlaying = true;
        step = 0;
        next = EditorApplication.timeSinceStartup + 4;
        EditorApplication.update += Tick;
    }

    [MenuItem("Practice/Verify UI And Settings")]
    public static void CheckUI()
    {
        var game = GameManager.Instance;
        if (game == null) return;
        game.ToMenu();
        var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var button in buttons)
            if (button.name == "Settings") button.onClick.Invoke();
        SaveService.SetVolume(0.4f);
        EditorApplication.delayCall += () => Capture("settings");
        Debug.Log("PASS Settings button callback");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Debug.Log("PASS " + message);
    }

    private static void Capture(string name)
    {
        Directory.CreateDirectory("Documentation/Screenshots");
        ScreenCapture.CaptureScreenshot("Documentation/Screenshots/pp12-" + name + ".png");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || GameManager.Instance == null ||
            EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 1.2;
        var game = GameManager.Instance;
        try
        {
            switch (step++)
            {
                case 0:
                    Check(!game.Levels.Select(2), "Closed level rejected");
                    Capture("menu"); break;
                case 1: game.StartLevel(1); break;
                case 2:
                    Check(game.IsPlaying && game.Hearts == 3, "Level starts");
                    game.TryFinishLevel();
                    Check(game.IsPlaying, "Early exit blocked");
                    Capture("level1"); break;
                case 3: game.Pause(); break;
                case 4:
                    Check(Time.timeScale == 0 && !game.IsPlaying, "Pause freezes play");
                    Capture("pause"); break;
                case 5:
                    game.TogglePause();
                    Check(game.IsPlaying && Time.timeScale == 1, "Resume");
                    foreach (var c in UnityEngine.Object.FindObjectsByType<Collectible>(
                        FindObjectsSortMode.None)) game.Collect(c);
                    game.TryFinishLevel(); break;
                case 6:
                    Check(game.CurrentState == GameManager.State.Won &&
                        SaveService.Unlocked == 2, "Win unlocks next level");
                    Capture("won"); break;
                case 7: game.ToMenu(); break;
                case 8: Capture("unlocked"); break;
                case 9: game.StartLevel(2); break;
                case 10: Capture("level2"); game.HitHazard(); break;
                case 11: game.HitHazard(); break;
                case 12: game.HitHazard(); break;
                case 13:
                    Check(game.CurrentState == GameManager.State.Lost,
                        "Three hazards cause loss");
                    Check(SaveService.Unlocked == 2, "Loss does not unlock");
                    Capture("lost"); break;
                case 14:
                    game.StartLevel(2);
                    Check(game.Collected == 0 && game.Hearts == 3, "Retry resets run");
                    SaveService.SetVolume(0.4f);
                    Check(Mathf.Approximately(SaveService.Volume, 0.4f), "Volume saved");
                    foreach (var c in UnityEngine.Object.FindObjectsByType<Collectible>(
                        FindObjectsSortMode.None)) game.Collect(c);
                    game.TryFinishLevel(); break;
                case 15: game.StartLevel(3); break;
                case 16: Capture("level3"); break;
                case 17:
                    foreach (var c in UnityEngine.Object.FindObjectsByType<Collectible>(
                        FindObjectsSortMode.None)) game.Collect(c);
                    game.TryFinishLevel(); break;
                case 18:
                    Check(SaveService.Unlocked == 3, "Final level bounded at three");
                    Capture("completed"); break;
                case 19: game.ToMenu(); break;
                case 20:
                    ClickButton("Settings"); break;
                case 21:
                    Capture("settings"); break;
                case 22:
                    ClickButton("Back"); break;
                case 23: ClickButton("Level1"); break;
                case 24:
                    Check(game.IsPlaying, "Level button starts game");
                    var right = FindButton("Right").GetComponent<HoldButton>();
                    right.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(
                        UnityEngine.EventSystems.EventSystem.current));
                    Check(game.MobileDirection == Vector2.right, "Hold sets direction");
                    right.OnPointerUp(null);
                    Check(game.MobileDirection == Vector2.zero, "Release clears direction");
                    ClickButton("PauseButton"); break;
                case 25:
                    Check(game.CurrentState == GameManager.State.Paused,
                        "Pause button pauses");
                    ClickButton("Resume"); break;
                case 26:
                    Check(game.IsPlaying, "Resume button resumes");
                    game.SendMessage("OnApplicationFocus", false,
                        SendMessageOptions.DontRequireReceiver);
                    Check(game.CurrentState == GameManager.State.Paused,
                        "Focus loss pauses");
                    game.ToMenu();
                    File.WriteAllText("Documentation/verification.txt",
                        "PASS: start, closed level, early exit, pause, resume, win, " +
                        "unlock, loss, retry, volume, final level, UI raycasts, button callbacks, " +
                        "hold/release, focus-loss callback.\n" +
                        "These checks call game methods; manual input tests are separate.\n");
                    Finish(); break;
            }
        }
        catch (Exception error)
        {
            File.WriteAllText("Documentation/verification.txt", "FAIL: " + error.Message);
            Debug.LogException(error);
            Finish();
        }
    }

    private static UnityEngine.UI.Button FindButton(string name)
    {
        foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(
            FindObjectsSortMode.None))
            if (button.name == name) return button;
        throw new Exception("Active button not found: " + name);
    }

    private static void ClickButton(string name)
    {
        Canvas.ForceUpdateCanvases();
        var button = FindButton(name);
        var rect = (RectTransform)button.transform;
        var point = RectTransformUtility.WorldToScreenPoint(null,
            rect.TransformPoint(rect.rect.center));
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(
            new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current) { position = point }, hits);
        Check(hits.Count > 0 && hits[0].gameObject == button.gameObject,
            "Raycast reaches " + name + " (hits: " +
            string.Join(",", hits.ConvertAll(h => h.gameObject.name)) + ")");
        Check(button.interactable, "Button enabled: " + name);
        button.onClick.Invoke();
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("PP12.Verify", false);
        oldUnlocked = SessionState.GetInt("PP12.OldUnlocked", 1);
        oldVolume = SessionState.GetFloat("PP12.OldVolume", 0.7f);
        for (int i = 0; i < 3; i++) oldBest[i] = SessionState.GetInt("PP12.OldBest" + i, 0);
        PlayerPrefs.SetInt(Prefix + "Unlocked", oldUnlocked);
        for (int i = 0; i < 3; i++) PlayerPrefs.SetInt(Prefix + "Best" + (i + 1), oldBest[i]);
        SaveService.SetVolume(oldVolume);
    }
}
