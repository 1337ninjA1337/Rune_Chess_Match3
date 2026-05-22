using RuneChess.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RuneChess.Editor;

public static class CreateMainScene
{
    [MenuItem("Rune Chess/Create Main Scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var bootstrap = new GameObject("RuneChessBootstrap");
        bootstrap.AddComponent<PortraitGameBootstrap>();

        const string scenePath = "Assets/Scenes/Main.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
    }
}
