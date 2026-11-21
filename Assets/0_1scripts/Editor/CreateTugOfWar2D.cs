using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateTugOfWar2D
{
    private const string ScenePath = "Assets/0_0Scenes/TiraYAflojaAlturas.unity";
    private const string SpriteFolder = "Assets/Art/Tira2D";
    private const string PrefabFolder = "Assets/0_2prefabs/Tira2D";

    [MenuItem("Tools/Create 2D Tug of War at Heights")]
    public static void Create()
    {
        EnsureFolder("Assets/Art", "Tira2D");
        EnsureFolder("Assets/0_2prefabs", "Tira2D");
        Sprite white = MakeWhiteSprite();
        Sprite background = LoadTugBackground();
        Sprite playerSprite = MakePersonSprite("Player_Celeste.png", new Color32(44, 205, 241, 255));
        Sprite blueSprite = MakePersonSprite("Equipo_Azul.png", new Color32(47, 119, 232, 255));
        Sprite redSprite = MakePersonSprite("Equipo_Coral.png", new Color32(224, 72, 79, 255));

        GameObject playerPrefab = MakeCharacterPrefab("PlayerTira2D", playerSprite, "Assets/0_2prefabs/Tira2D/PlayerTira2D.prefab");
        GameObject bluePrefab = MakeCharacterPrefab("NPC_Azul_2D", blueSprite, "Assets/0_2prefabs/Tira2D/NPC_Azul_2D.prefab");
        GameObject redPrefab = MakeCharacterPrefab("NPC_Coral_2D", redSprite, "Assets/0_2prefabs/Tira2D/NPC_Coral_2D.prefab");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var arena = new GameObject("Tira y afloja sobre las nubes");
        BuildBackground(arena.transform, background, white);
        BuildPlatforms(arena.transform, white);
        BuildRope(arena.transform, white);
        BuildTeams(arena.transform, playerPrefab, bluePrefab, redPrefab);
        BuildLabels(arena.transform);
        BuildCamera();
        BuildGameManager(arena.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("TiraYAflojaAlturas creado como escena 2D. Pulsa ESPACIO, A o flecha izquierda para tirar.");
    }

    private static void BuildBackground(Transform parent, Sprite sky, Sprite white)
    {
        if (sky != null)
        {
            SpriteObject("Fondo del tira y afloja", sky, new Vector3(0f, 2.5f, 10f), new Vector3(3.6f,3.6f,1f), Color.white, parent, -100);
            return;
        }
        SpriteObject("Cielo", sky, new Vector3(0f, 2.5f, 10f), new Vector3(48f, 30f, 1f), new Color(1f, 1f, 1f, 1f), parent, -100);
        Color cloud = new Color(1f, 1f, 1f, 0.74f);
        for (int row = 0; row < 3; row++)
        {
            float y = -5.8f + row * 1.25f;
            for (int i = 0; i < 9; i++)
            {
                float x = -23f + i * 5.7f + (row % 2 == 0 ? 0f : 2.7f);
                float width = 5.2f + (i % 3) * 0.6f;
                SpriteObject("Nube", white, new Vector3(x, y, 8f + row * 0.1f), new Vector3(width, 1.05f + (i % 2) * 0.3f, 1f), cloud, parent, -80);
                SpriteObject("Nube", white, new Vector3(x + 1.5f, y + 0.45f, 7.9f + row * 0.1f), new Vector3(width * 0.58f, 0.8f, 1f), cloud, parent, -79);
            }
        }
        // The towers and cables dropping into the cloud layer make the height legible.
        Color tower = new Color(0.18f, 0.24f, 0.33f);
        foreach (float x in new[] { -13.5f, 13.5f })
        {
            SpriteObject("Torre colgante", white, new Vector3(x, -5.4f, 4f), new Vector3(1.1f, 9.2f, 1f), tower, parent, -30);
            for (int y = -9; y < -1; y += 2)
                SpriteObject("Travesaño de torre", white, new Vector3(x, y, 3.8f), new Vector3(3.4f, 0.28f, 1f), new Color(0.34f, 0.4f, 0.5f), parent, -29);
        }
    }

    private static Sprite LoadTugBackground()
    {
        const string path = SpriteFolder + "/FondoTiraYAfloja.png";
        if (!File.Exists(path)) return MakeSkySprite();
        return ImportSprite(path, 100f);
    }

    private static void BuildPlatforms(Transform parent, Sprite white)
    {
        Color darkSide = new Color(0.13f, 0.18f, 0.27f);
        Color deck = new Color(0.34f, 0.42f, 0.53f);
        Color trim = new Color(0.9f, 0.74f, 0.38f);
        foreach (int side in new[] { -1, 1 })
        {
            float x = side * 13.5f;
            SpriteObject("Plataforma elevada", white, new Vector3(x, -0.05f, 0f), new Vector3(19f, 1.3f, 1f), darkSide, parent, -20);
            SpriteObject("Cubierta de plataforma", white, new Vector3(x, 0.68f, -0.02f), new Vector3(18.8f, 0.22f, 1f), deck, parent, -19);
            SpriteObject("Borde de plataforma", white, new Vector3(x, 0.84f, -0.03f), new Vector3(18.8f, 0.08f, 1f), trim, parent, -18);
            // Outer safety posts frame the decks while leaving the gap open.
            float outerEdge = side * 23f;
            SpriteObject("Poste exterior", white, new Vector3(outerEdge, 1.55f, 0f), new Vector3(0.28f, 1.35f, 1f), trim, parent, -5);
            SpriteObject("Baranda exterior", white, new Vector3(side * 18f, 2.1f, 0f), new Vector3(10f, 0.16f, 1f), trim, parent, -5);
        }
    }

    private static void BuildRope(Transform parent, Sprite white)
    {
        SpriteObject("Cuerda de tira y afloja", white, new Vector3(0f, 2.4f, -0.15f), new Vector3(43f, 0.34f, 1f),
            new Color(0.36f, 0.2f, 0.12f), parent, 0);
        for (int i = 0; i < 80; i++)
        {
            float x = -21f + i * 0.53f;
            SpriteObject("Trenza de cuerda", white, new Vector3(x, 2.4f, -0.2f), new Vector3(0.28f, 0.12f, 1f),
                i % 2 == 0 ? new Color(0.68f, 0.46f, 0.27f) : new Color(0.19f, 0.12f, 0.09f), parent, 1,
                Quaternion.Euler(0f, 0f, i % 2 == 0 ? 24f : -24f));
        }

        SpriteObject("Marca azul izquierda", white, new Vector3(-2.6f, 2.4f, -0.35f), new Vector3(0.8f, 0.48f, 1f),
            new Color(0.35f, 0.68f, 1f), parent, 3);
        SpriteObject("Marca azul derecha", white, new Vector3(2.6f, 2.4f, -0.35f), new Vector3(0.8f, 0.48f, 1f),
            new Color(0.35f, 0.68f, 1f), parent, 3);

        GameObject marker = new GameObject("Marca roja móvil");
        marker.transform.SetParent(parent, false);
        marker.transform.position = new Vector3(0f, 3f, -0.5f);
        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = white;
        renderer.color = new Color(0.95f, 0.12f, 0.2f);
        renderer.sortingOrder = 8;
        marker.transform.localScale = new Vector3(0.48f, 1.15f, 1f);
        SpriteObject("Punta de la marca", white, new Vector3(0f, -0.58f, -0.02f), new Vector3(0.75f, 0.22f, 1f),
            new Color(0.95f, 0.12f, 0.2f), marker.transform, 9, Quaternion.Euler(0f, 0f, 45f));
    }

    private static void BuildTeams(Transform parent, GameObject playerPrefab, GameObject bluePrefab, GameObject redPrefab)
    {
        float[] slots = { 21f, 17f, 13f, 9f, 5.2f };
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject left = (GameObject)PrefabUtility.InstantiatePrefab(i == slots.Length - 1 ? playerPrefab : bluePrefab, parent);
            left.name = i == slots.Length - 1 ? "Player_Tira2D" : "NPC_Azul_" + (i + 1).ToString("00");
            left.transform.localPosition = new Vector3(-slots[i], 0.82f, -0.75f);
            left.GetComponent<SpriteRenderer>().flipX = false;

            GameObject right = (GameObject)PrefabUtility.InstantiatePrefab(redPrefab, parent);
            right.name = "NPC_Coral_" + (i + 1).ToString("00");
            right.transform.localPosition = new Vector3(slots[i], 0.82f, -0.75f);
            right.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    private static void BuildLabels(Transform parent)
    {
        Text("EQUIPO CELESTE", new Vector3(-13.5f, 4.65f, -0.2f), new Color(0.56f, 0.91f, 1f), 0.3f, parent);
        Text("EQUIPO CORAL", new Vector3(13.5f, 4.65f, -0.2f), new Color(1f, 0.67f, 0.65f), 0.3f, parent);
        Text("TIRA Y AFLOJA · A GRAN ALTURA", new Vector3(0f, 10.35f, -0.3f), Color.white, 0.4f, parent);
        Text("P1: ESPACIO/A o FLECHA IZQUIERDA para tirar.\nEl rival tira automáticamente: lleva la marca roja al lado azul.",
            new Vector3(0f, 8.95f, -0.3f), new Color(1f, 0.93f, 0.72f), 0.27f, parent, "Instrucciones del juego");
        Text("¡No sueltes la cuerda!", new Vector3(0f, -2.3f, -0.3f), new Color(0.13f, 0.17f, 0.23f), 0.3f, parent);
    }

    private static void BuildCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 2.6f, -30f);
        cameraObject.transform.rotation = Quaternion.Euler(35.26439f,45f,0);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.fieldOfView = 47f;
        camera.orthographicSize = 13f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
    }

    private static void BuildGameManager(Transform parent)
    {
        var manager = new GameObject("Control del tira y afloja");
        manager.transform.SetParent(parent, false);
        manager.AddComponent<TugOfWarGame>();
    }

    private static GameObject MakeCharacterPrefab(string name, Sprite sprite, string path)
    {
        var root = new GameObject(name);
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 5;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Sprite MakeWhiteSprite()
    {
        const string path = SpriteFolder + "/FormaBlanca.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color32[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        return ImportSprite(path, 8f);
    }

    private static Sprite MakeSkySprite()
    {
        const string path = SpriteFolder + "/CieloAlturas.png";
        if (!File.Exists(path))
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Color32 top = new Color32(53, 137, 226, 255);
            Color32 bottom = new Color32(194, 230, 247, 255);
            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                Color32 color = Color32.Lerp(bottom, top, t);
                for (int x = 0; x < size; x++) pixels[y * size + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        return ImportSprite(path, 256f);
    }

    private static Sprite MakePersonSprite(string filename, Color32 shirt)
    {
        string path = SpriteFolder + "/" + filename;
        if (!File.Exists(path))
        {
            const int width = 96;
            const int height = 144;
            var pixels = new Color32[width * height];
            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32 skin = new Color32(242, 192, 153, 255);
            Color32 pants = new Color32(32, 48, 70, 255);
            Color32 shoes = new Color32(20, 26, 39, 255);
            Color32 glove = new Color32(244, 225, 186, 255);
            Color32 hair = new Color32(44, 34, 34, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

            // Pixel-art figure: separated legs, torso, outstretched arms and a readable face.
            Rect(pixels, width, height, 37, 4, 46, 34, pants);
            Rect(pixels, width, height, 25, 2, 43, 9, shoes);
            Rect(pixels, width, height, 49, 2, 67, 9, shoes);
            Rect(pixels, width, height, 34, 35, 62, 88, shirt);
            Rect(pixels, width, height, 31, 78, 65, 87, new Color32(238, 232, 214, 255));
            ThickLine(pixels, width, height, 45, 77, 12, 91, 7, shirt);
            ThickLine(pixels, width, height, 51, 77, 85, 86, 7, shirt);
            Circle(pixels, width, height, 18, 83, 4, glove);
            Circle(pixels, width, height, 78, 85, 4, glove);
            Circle(pixels, width, height, 48, 111, 17, skin);
            Rect(pixels, width, height, 34, 119, 61, 132, hair);
            Rect(pixels, width, height, 54, 111, 60, 114, hair);
            Circle(pixels, width, height, 60, 110, 2, new Color32(25, 27, 31, 255));

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        return ImportSprite(path, 48f, true);
    }

    private static void Rect(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
    {
        for (int y = Mathf.Max(0, y0); y <= Mathf.Min(height - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(width - 1, x1); x++) pixels[y * width + x] = color;
    }

    private static void Circle(Color32[] pixels, int width, int height, int cx, int cy, int radius, Color32 color)
    {
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                if (x * x + y * y <= radius * radius)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < width && py >= 0 && py < height) pixels[py * width + px] = color;
                }
    }

    private static void ThickLine(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, int radius, Color32 color)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : i / (float)steps;
            Circle(pixels, width, height, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), radius, color);
        }
    }

    private static Sprite ImportSprite(string path, float pixelsPerUnit, bool feetAtPivot = false)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        if (feetAtPivot)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
        }
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject SpriteObject(string name, Sprite sprite, Vector3 position, Vector3 scale, Color color,
        Transform parent, int order, Quaternion? rotation = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        if (rotation.HasValue) obj.transform.localRotation = rotation.Value;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return obj;
    }

    private static void Text(string content, Vector3 position, Color color, float size, Transform parent, string objectName = null)
    {
        var obj = new GameObject(objectName ?? content, typeof(TextMesh));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        TextMesh mesh = obj.GetComponent<TextMesh>();
        mesh.text = content;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        obj.transform.localRotation = Quaternion.identity;
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 30;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
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
