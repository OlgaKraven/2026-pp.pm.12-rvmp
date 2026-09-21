using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(LevelManager), typeof(WorldBuilder))]
public class GameManager : MonoBehaviour
{
    public enum State { Menu, Playing, Paused, Won, Lost }
    public static GameManager Instance { get; private set; }
    public State CurrentState { get; private set; } = State.Menu;
    public bool IsPlaying => CurrentState == State.Playing;
    public Vector2 MobileDirection { get; private set; }
    public int Collected { get; private set; }
    public int Hearts { get; private set; }
    public float Remaining { get; private set; }
    public string Hint { get; private set; } = "Выберите уровень";
    public LevelManager Levels { get; private set; }
    private WorldBuilder world;
    private AudioFeedback audioFeedback;
    private float immuneUntil;
    private float elapsed;

    private void Awake()
    {
        Instance = this;
        Levels = GetComponent<LevelManager>();
        world = GetComponent<WorldBuilder>();
        audioFeedback = gameObject.AddComponent<AudioFeedback>();
        gameObject.AddComponent<PauseController>();
        gameObject.AddComponent<MenuController>();
        Application.targetFrameRate = 60;
        AudioListener.volume = SaveService.Volume;
        Time.timeScale = 1;
        world.BuildCamera();
    }

    public void StartLevel(int number)
    {
        if (!Levels.Select(number)) return;
        Time.timeScale = 1;
        Collected = 0;
        Hearts = 3;
        Remaining = Levels.Data.seconds;
        elapsed = 0;
        immuneUntil = 0;
        MobileDirection = Vector2.zero;
        Hint = "Соберите все кристаллы и войдите в портал";
        world.Build(Levels.Data);
        CurrentState = State.Playing;
    }

    private void Update()
    {
        if (!IsPlaying) return;
        elapsed += Time.deltaTime;
        Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
        if (Remaining <= 0) Finish(false, "Время закончилось");
    }

    public void SetDirection(Vector2 value) => MobileDirection = value;

    public void Collect(Collectible item)
    {
        if (!IsPlaying || !item.gameObject.activeSelf) return;
        item.gameObject.SetActive(false);
        Collected++;
        audioFeedback.PlayCollect();
        Hint = Collected == Levels.Data.target
            ? "Все кристаллы собраны. Войдите в зелёный портал"
            : $"Собрано {Collected} из {Levels.Data.target}";
    }

    public void HitHazard()
    {
        if (!IsPlaying || Time.time < immuneUntil) return;
        Hearts--;
        immuneUntil = Time.time + 1;
        audioFeedback.PlayHazard();
        world.Player.ReturnToStart();
        Hint = "Потеряна жизнь. Измените маршрут";
        if (Hearts <= 0) Finish(false, "Жизни закончились");
    }

    public void TryFinishLevel()
    {
        if (!IsPlaying) return;
        if (Collected < Levels.Data.target)
        {
            Hint = $"Нужно ещё {Levels.Data.target - Collected} кристаллов";
            return;
        }
        int score = Mathf.Max(1, 1000 + Hearts * 100 -
            Mathf.RoundToInt(elapsed * 5));
        SaveService.Complete(Levels.Current, score);
        audioFeedback.PlaySuccess();
        Finish(true, $"Очки: {score}. Рекорд: {SaveService.Best(Levels.Current)}");
    }

    private void Finish(bool won, string message)
    {
        CurrentState = won ? State.Won : State.Lost;
        Hint = message;
        MobileDirection = Vector2.zero;
        Time.timeScale = 0;
    }

    public void Pause()
    {
        if (!IsPlaying) return;
        CurrentState = State.Paused;
        MobileDirection = Vector2.zero;
        Time.timeScale = 0;
        PlayerPrefs.Save();
    }

    public void TogglePause()
    {
        if (IsPlaying) Pause();
        else if (CurrentState == State.Paused)
        {
            CurrentState = State.Playing;
            Time.timeScale = 1;
        }
    }

    public void ToMenu()
    {
        CurrentState = State.Menu;
        MobileDirection = Vector2.zero;
        Time.timeScale = 1;
        world.Clear();
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1;
    }
}
