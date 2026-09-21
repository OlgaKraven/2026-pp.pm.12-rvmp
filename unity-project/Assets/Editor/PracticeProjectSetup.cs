using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

public static class PracticeProjectSetup
{
    [MenuItem("Practice/Prepare Main Scene")]
    public static void PrepareMainScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        var root = new GameObject("GameRoot");
        root.AddComponent<LevelManager>();
        root.AddComponent<WorldBuilder>();
        root.AddComponent<GameManager>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
        };
        AssetDatabase.SaveAssets();
    }
}
