using UnityEngine;

[System.Serializable]
public class LevelData
{
    public string title;
    [Range(1, 7)] public int target;
    [Range(1, 6)] public int hazards;
    [Min(20)] public float seconds;
}

public class LevelManager : MonoBehaviour
{
    public LevelData[] levels =
    {
        new() { title = "Первый маршрут", target = 3,
            hazards = 2, seconds = 90 },
        new() { title = "Опасный поворот", target = 5,
            hazards = 4, seconds = 80 },
        new() { title = "Последний портал", target = 7,
            hazards = 6, seconds = 70 }
    };
    public int Current { get; private set; }
    public LevelData Data => levels[Current - 1];

    public bool Select(int number)
    {
        if (number < 1 || number > levels.Length ||
            number > SaveService.Unlocked) return false;
        Current = number;
        return true;
    }
}
