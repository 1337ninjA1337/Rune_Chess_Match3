using RuneChess.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RuneChess.Editor
{
    public static class CreateMainScene
    {
        [MenuItem("Rune Chess/Create Main Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.078f, 0.086f, 0.098f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;

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
}
