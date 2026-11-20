using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalizeNormal2DView
{
    private const string RoomPath = "Assets/0_0Scenes/CuartoDelJuego.unity";
    private const string StadiumPath = "Assets/0_0Scenes/Estadio.unity";

    [MenuItem("Tools/Remove Dormitory and Use Close 2D View")]
    public static void Apply()
    {
        Scene emptyRoom = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var roomCameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        roomCameraObject.tag = "MainCamera";
        Camera roomCamera = roomCameraObject.GetComponent<Camera>();
        roomCamera.orthographic = true;
        roomCamera.orthographicSize = 12f;
        roomCamera.clearFlags = CameraClearFlags.SolidColor;
        roomCamera.backgroundColor = new Color(0.025f, 0.035f, 0.07f);
        roomCameraObject.transform.position = new Vector3(0f, 0f, -10f);
        EditorSceneManager.SaveScene(emptyRoom, RoomPath);

        var buildScenes = new List<EditorBuildSettingsScene>();
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            if (entry.path != RoomPath) buildScenes.Add(entry);
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Scene stadium = EditorSceneManager.OpenScene(StadiumPath, OpenSceneMode.Single);
        GameObject player = GameObject.Find("Player");
        Camera camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        CinematicCamera2D intro = camera != null ? camera.GetComponent<CinematicCamera2D>() : null;
        if (player != null && intro != null)
        {
            var serialized = new SerializedObject(intro);
            serialized.FindProperty("overviewCenter").vector2Value = (Vector2)player.transform.position + new Vector2(0f, 3f);
            serialized.FindProperty("overviewSize").floatValue = 15f;
            serialized.FindProperty("followSize").floatValue = 15f;
            serialized.FindProperty("followOffset").vector2Value = new Vector2(0f, 3f);
            serialized.FindProperty("introDuration").floatValue = 0.15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            camera.orthographic = true;
            camera.orthographicSize = 15f;
        }
        EditorSceneManager.MarkSceneDirty(stadium);
        EditorSceneManager.SaveScene(stadium);
        AssetDatabase.SaveAssets();
        Debug.Log("Dormitorio retirado del juego; escena de estadio en cámara 2D cercana al Player.");
    }
}
