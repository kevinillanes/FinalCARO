using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateTugOfWarContinuous2D
{
    private const string SourcePath = "Assets/0_0Scenes/TiraYAflojaAlturas.unity";
    private const string ScenePath = "Assets/0_0Scenes/TiraYAflojaPisoCompleto.unity";
    private const string WhiteSpritePath = "Assets/Art/Tira2D/FormaBlanca.png";

    [MenuItem("Tools/Create 2D Tug of War with Full Platform")]
    public static void Create()
    {
        Scene scene = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new System.InvalidOperationException("No se pudo duplicar la escena de tira y afloja.");

        Transform arena = GameObject.Find("Tira y afloja sobre las nubes").transform;
        var removeNames = new HashSet<string>
        {
            "Plataforma elevada",
            "Cubierta de plataforma",
            "Borde de plataforma"
        };
        var toRemove = new List<GameObject>();
        foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (removeNames.Contains(candidate.name)) toRemove.Add(candidate);
        foreach (GameObject oldPlatformPart in toRemove)
            Object.DestroyImmediate(oldPlatformPart);

        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        if (white == null) throw new System.InvalidOperationException("No se encontró el sprite base de la plataforma.");
        AddPlatformLayer("Piso flotante continuo", white, new Vector3(0f, -0.05f, 0f),
            new Vector3(47f, 1.3f, 1f), new Color(0.13f, 0.18f, 0.27f), -20, arena);
        AddPlatformLayer("Cubierta continua", white, new Vector3(0f, 0.68f, -0.02f),
            new Vector3(46.8f, 0.22f, 1f), new Color(0.34f, 0.42f, 0.53f), -19, arena);
        AddPlatformLayer("Borde continuo", white, new Vector3(0f, 0.84f, -0.03f),
            new Vector3(46.8f, 0.08f, 1f), new Color(0.9f, 0.74f, 0.38f), -18, arena);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("TiraYAflojaPisoCompleto creado con una plataforma flotante continua.");
    }

    private static GameObject AddPlatformLayer(string name, Sprite sprite, Vector3 position, Vector3 scale,
        Color color, int sortingOrder, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return obj;
    }

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene scene in scenes)
            if (scene.path == path) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
