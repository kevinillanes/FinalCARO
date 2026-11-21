using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateJumpRopeScene
{
    private const string Path = "Assets/0_0Scenes/TiraYAflojaPisoCompleto.unity";
    private const string WhitePath = "Assets/Art/Tira2D/FormaBlanca.png";
    private const string BackgroundPath = "Assets/Art/Tira2D/FondoTiraYAfloja.png";

    [MenuItem("Tools/Upgrade Full Platform to Spinning Jump Rope")]
    public static void Create()
    {
        Scene scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Single);
        GameObject cameraObject = GameObject.Find("Main Camera");
        if (cameraObject == null) throw new System.InvalidOperationException("No se encontro Main Camera.");

        var roots = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects()) if (root != cameraObject) roots.Add(root);
        foreach (GameObject root in roots) Object.DestroyImmediate(root);

        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
        Sprite background = LoadTugBackground();
        Sprite p1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tira2D/Player_Celeste.png");
        Sprite p2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tira2D/Equipo_Azul.png");
        if (white == null || background == null || p1Sprite == null || p2Sprite == null)
            throw new System.InvalidOperationException("Faltan sprites base en Assets/Art/Tira2D.");

        var arena = new GameObject("Reto de la cuerda giratoria");
        Shape("Fondo del tira y afloja", background, Vector2.zero, Vector2.one*3.6f, Color.white, arena.transform, -100).transform.localPosition = new Vector3(0,2.7f,10);
        Shape("Plataforma flotante cerrada", white, new Vector2(0f, -0.25f), new Vector2(47f, 2f), new Color(0.17f, 0.22f, 0.3f), arena.transform, -20);
        Shape("Piso continuo", white, new Vector2(0f, 0.75f), new Vector2(46.8f, 0.25f), new Color(0.58f, 0.64f, 0.68f), arena.transform, -19);
        Shape("Borde dorado", white, new Vector2(0f, 0.92f), new Vector2(46.8f, 0.1f), new Color(0.95f, 0.73f, 0.35f), arena.transform, -18);
        for (int side = -1; side <= 1; side += 2)
            for (int rung = 0; rung < 7; rung++)
                Shape("Estructura del puente", white, new Vector2(side * (14f + rung * 1.6f), -0.6f), new Vector2(0.28f, 1.8f), new Color(0.29f, 0.35f, 0.43f), arena.transform, -10);

        var pivot = new GameObject("Cuerda giratoria");
        pivot.transform.SetParent(arena.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 3.2f, -0.4f);
        Shape("Cuerda larga", white, Vector2.zero, new Vector2(20f, 0.28f), new Color(0.38f, 0.22f, 0.13f), pivot.transform, 3);
        Shape("Punta roja cuerda", white, new Vector2(9.8f, 0f), new Vector2(0.65f, 0.6f), new Color(0.94f, 0.17f, 0.25f), pivot.transform, 4);

        GameObject playerOne = Character("Player 1", p1Sprite, new Vector2(-3.6f, 1.55f), new Color(0.28f, 0.84f, 0.97f), arena.transform);
        GameObject playerTwo = Character("Player 2", p2Sprite, new Vector2(3.6f, 1.55f), new Color(0.32f, 0.64f, 1f), arena.transform);
        var bots = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            float x = -19f + i * 7.6f;
            GameObject bot = Character("Participante bot " + (i + 1), p2Sprite, new Vector2(x, 1.52f), i % 2 == 0 ? new Color(0.88f, 0.31f, 0.42f) : new Color(0.98f, 0.73f, 0.31f), arena.transform);
            bot.transform.localScale *= 0.68f;
            bots[i] = bot.transform;
        }
        TextMesh text = MakeText("Instrucciones", new Vector3(0f, 9.3f, -1f), arena.transform);
        TextMesh cue = MakeText("Aviso de salto", new Vector3(0f, 6.5f, -1f), arena.transform);
        cue.characterSize = 0.4f;
        cue.fontSize = 48;

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.fieldOfView = 47f;
        camera.orthographicSize = 13f;
        cameraObject.transform.position = new Vector3(0f, 2.7f, -30f);
        cameraObject.transform.rotation = Quaternion.Euler(35.26439f,45f,0);
        var game = arena.AddComponent<JumpRopeGame>();
        var serialized = new SerializedObject(game);
        serialized.FindProperty("rope").objectReferenceValue = pivot.transform;
        serialized.FindProperty("playerOne").objectReferenceValue = playerOne.transform;
        serialized.FindProperty("playerTwo").objectReferenceValue = playerTwo.transform;
        SerializedProperty botsProperty = serialized.FindProperty("bots");
        botsProperty.arraySize = bots.Length;
        for (int i = 0; i < bots.Length; i++) botsProperty.GetArrayElementAtIndex(i).objectReferenceValue = bots[i];
        serialized.FindProperty("instructions").objectReferenceValue = text;
        serialized.FindProperty("timingCue").objectReferenceValue = cue;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Escena de plataforma completa convertida en un reto cooperativo de cuerda giratoria.");
    }

    private static GameObject Character(string name, Sprite sprite, Vector2 position, Color color, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, -0.7f);
        go.transform.localScale = Vector3.one * 1.25f;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 5;
        return go;
    }

    private static Sprite LoadTugBackground()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (sprite != null) return sprite;
        AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
    }

    private static GameObject Shape(string name, Sprite sprite, Vector2 pos, Vector2 size, Color color, Transform parent, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    private static TextMesh MakeText(string name, Vector3 pos, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = "";
        mesh.characterSize = 0.32f;
        mesh.fontSize = 42;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = Color.white;
        return mesh;
    }
}
