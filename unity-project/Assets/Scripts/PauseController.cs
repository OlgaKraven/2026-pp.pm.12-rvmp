using UnityEngine;

public class PauseController : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            GameManager.Instance.TogglePause();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) GameManager.Instance?.Pause();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) GameManager.Instance?.Pause();
        PlayerPrefs.Save();
    }

    // Возврат фокуса не возобновляет игру без действия игрока.
    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}
