using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreatePixelSurvivalGame
{
    private const string ScenePath = "Assets/0_0Scenes/LaUltimaRonda.unity";

    [MenuItem("Tools/Create Pixel Survival Adventure")]
    public static void Create()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 270f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.09f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraObject.AddComponent<PixelSurvivalGame>();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new System.InvalidOperationException("No se pudo guardar la escena de la aventura pixel art.");

        var scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            if (existing.path != ScenePath) scenes.Add(existing);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Aventura pixel art creada. Está lista como primera escena del juego.");
    }
}
