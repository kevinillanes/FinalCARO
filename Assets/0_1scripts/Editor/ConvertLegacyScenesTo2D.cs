using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConvertLegacyScenesTo2D
{
    private const string ArtFolder = "Assets/Art/2DWorld";
    private const string PrefabFolder = "Assets/0_2prefabs/2DWorld";
    private const string StadiumScenePath = "Assets/0_0Scenes/Estadio.unity";
    private const string RoomScenePath = "Assets/0_0Scenes/CuartoDelJuego.unity";

    [MenuItem("Tools/Convert Stadium and Room Scenes to 2D")]
    public static void Convert()
    {
        EnsureFolder("Assets/Art", "2DWorld");
        EnsureFolder("Assets/0_2prefabs", "2DWorld");
        Sprite white = LoadOrMakeWhite();
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tira2D/Player_Celeste.png");
        if (playerSprite == null) throw new System.InvalidOperationException("No se encontró el sprite celeste de Player.");
        Sprite npcSprite = MakePersonSprite("Assets/Art/2DWorld/NPC_Amarillo.png", new Color32(244, 183, 34, 255));
        Sprite circleSprite = MakeCircleSprite();
        Sprite[] houseSprites =
        {
            MakeHouseSprite("Casita_Coral.png", new Color32(228, 86, 91, 255), new Color32(50, 106, 218, 255)),
            MakeHouseSprite("Casita_Amarilla.png", new Color32(244, 188, 54, 255), new Color32(107, 67, 165, 255)),
            MakeHouseSprite("Casita_Menta.png", new Color32(69, 176, 136, 255), new Color32(224, 82, 88, 255))
        };
        Sprite[] treeSprites =
        {
            MakeTreeSprite("Arbolito_Copa.png", new Color32(58, 157, 76, 255)),
            MakeTreeSprite("Arbolito_Olivo.png", new Color32(102, 169, 67, 255)),
            MakeTreeSprite("Arbolito_Oscuro.png", new Color32(36, 120, 71, 255))
        };

        GameObject playerPrefab = MakePrefab("Player_2D", playerSprite, PrefabFolder + "/Player_2D.prefab");
        GameObject npcPrefab = MakePrefab("NPC_Amarillo_2D", npcSprite, PrefabFolder + "/NPC_Amarillo_2D.prefab");
        GameObject[] houses = new GameObject[houseSprites.Length];
        GameObject[] trees = new GameObject[treeSprites.Length];
        for (int i = 0; i < houses.Length; i++)
            houses[i] = MakePrefab("Casita_2D_" + (i + 1), houseSprites[i], PrefabFolder + "/Casita_2D_" + (i + 1) + ".prefab");
        for (int i = 0; i < trees.Length; i++)
            trees[i] = MakePrefab("Arbolito_2D_" + (i + 1), treeSprites[i], PrefabFolder + "/Arbolito_2D_" + (i + 1) + ".prefab");

        BuildStadium2D(white, circleSprite, npcPrefab, playerPrefab);
        AssetDatabase.SaveAssets();
    }

    private static void BuildStadium2D(Sprite white, Sprite circle, GameObject npcPrefab, GameObject playerPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Estadio 2D");
        Shape("Fondo exterior", white, Vector2.zero, new Vector2(170f, 112f), new Color(0.055f, 0.085f, 0.12f), root.transform, -100);

        // Four banks of stands and a large crowd frame the pitch.
        Shape("Grada norte", white, new Vector2(0f, 35f), new Vector2(145f, 16f), new Color(0.105f, 0.15f, 0.22f), root.transform, -20);
        Shape("Grada sur", white, new Vector2(0f, -35f), new Vector2(145f, 16f), new Color(0.105f, 0.15f, 0.22f), root.transform, -20);
        Shape("Grada oeste", white, new Vector2(-53f, 0f), new Vector2(16f, 58f), new Color(0.13f, 0.19f, 0.27f), root.transform, -20);
        Shape("Grada este", white, new Vector2(53f, 0f), new Vector2(16f, 58f), new Color(0.13f, 0.19f, 0.27f), root.transform, -20);
        for (int tier = 0; tier < 8; tier++)
        {
            float y = 28.4f + tier * 1.72f;
            Shape("Fila de asientos norte", white, new Vector2(0f, y), new Vector2(139f, 0.18f), tier % 2 == 0 ? new Color(0.25f, 0.34f, 0.45f) : new Color(0.19f, 0.28f, 0.39f), root.transform, -15);
            Shape("Fila de asientos sur", white, new Vector2(0f, -y), new Vector2(139f, 0.18f), tier % 2 == 0 ? new Color(0.25f, 0.34f, 0.45f) : new Color(0.19f, 0.28f, 0.39f), root.transform, -15);
            for (int i = 0; i < 34; i++)
            {
                float x = -66f + i * 4f;
                // Deliberately leave the stands empty: this room has guards, not spectators.
            }
        }
        for (int tier = 0; tier < 6; tier++)
        {
            float x = 47.5f + tier * 1.8f;
            Shape("Fila de asientos oeste", white, new Vector2(-x, 0f), new Vector2(0.18f, 56f), tier % 2 == 0 ? new Color(0.25f, 0.34f, 0.45f) : new Color(0.19f, 0.28f, 0.39f), root.transform, -15);
            Shape("Fila de asientos este", white, new Vector2(x, 0f), new Vector2(0.18f, 56f), tier % 2 == 0 ? new Color(0.25f, 0.34f, 0.45f) : new Color(0.19f, 0.28f, 0.39f), root.transform, -15);
            for (int i = 0; i < 18; i++)
            {
                float y = -26f + i * 3.05f;
                // Deliberately empty stands.
            }
        }

        Shape("Cancha de fútbol", white, Vector2.zero, new Vector2(90f, 58f), new Color(0.07f, 0.35f, 0.19f), root.transform, -10);
        Shape("Límite cancha superior", white, new Vector2(0f, 28f), new Vector2(88f, 0.22f), Color.white, root.transform, -5);
        Shape("Límite cancha inferior", white, new Vector2(0f, -28f), new Vector2(88f, 0.22f), Color.white, root.transform, -5);
        Shape("Límite cancha izquierdo", white, new Vector2(-43f, 0f), new Vector2(0.22f, 56f), Color.white, root.transform, -5);
        Shape("Límite cancha derecho", white, new Vector2(43f, 0f), new Vector2(0.22f, 56f), Color.white, root.transform, -5);
        Shape("Línea central", white, Vector2.zero, new Vector2(0.18f, 56f), Color.white, root.transform, -4);
        Shape("Círculo central", circle, Vector2.zero, new Vector2(14f, 14f), Color.white, root.transform, -4);
        Shape("Interior del círculo", circle, Vector2.zero, new Vector2(13.6f, 13.6f), new Color(0.07f, 0.35f, 0.19f), root.transform, -3);
        Shape("Arco oeste", white, new Vector2(-43.4f, 0f), new Vector2(3f, 12f), new Color(0.92f, 0.94f, 0.88f), root.transform, -4);
        Shape("Arco este", white, new Vector2(43.4f, 0f), new Vector2(3f, 12f), new Color(0.92f, 0.94f, 0.88f), root.transform, -4);

        // The prize display is drawn as a bright 2D glass case filled with stacks of bills.
        Shape("Base de la caja de premio", white, new Vector2(0f, 0f), new Vector2(11f, 8.5f), new Color(0.89f, 0.07f, 0.3f), root.transform, 0);
        Shape("Vitrina 2D", white, new Vector2(0f, 0f), new Vector2(10.2f, 7.7f), new Color(0.23f, 0.72f, 0.82f, 0.55f), root.transform, 1);
        for (int row = 0; row < 5; row++)
            for (int col = 0; col < 8; col++)
            {
                float x = -3.6f + col * 1.02f;
                float y = -2.35f + row * 1.16f;
                Color money = (row + col) % 2 == 0 ? new Color(0.09f, 0.55f, 0.24f) : new Color(0.14f, 0.68f, 0.32f);
                Shape("Fajo de billetes", white, new Vector2(x, y), new Vector2(0.78f, 0.4f), money, root.transform, 2);
                Shape("Banda del fajo", white, new Vector2(x, y), new Vector2(0.13f, 0.42f), new Color(0.93f, 0.8f, 0.46f), root.transform, 3);
            }
        Shape("Marco superior de vitrina", white, new Vector2(0f, 3.9f), new Vector2(11.1f, 0.22f), new Color(1f, 0.7f, 0.16f), root.transform, 4);

        int npcIndex = 1;
        for (int row = 0; row < 5; row++)
            for (int col = 0; col < 6; col++)
            {
                GameObject npc = AddPerson(npcPrefab, "NPC_Cancha_" + (npcIndex++).ToString("00"), new Vector2(-30f + col * 12f, -16f + row * 8f), 0.62f, 6, root.transform);
                AddWander(npc, new Vector2(-41f, -26f), new Vector2(41f, 26f), 2.1f);
            }
        for (int guard = 0; guard < 16; guard++)
        {
            float angle = guard * Mathf.PI * 2f / 16f;
            Vector2 at = new Vector2(Mathf.Cos(angle) * 47f, Mathf.Sin(angle) * 31f);
            GameObject watchman = AddPerson(npcPrefab, "Guardia_Rojo_" + (guard + 1).ToString("00"), at, 0.72f, 9, root.transform);
            watchman.GetComponent<SpriteRenderer>().color = new Color(0.92f, 0.14f, 0.27f);
        }
        GameObject player = AddPerson(playerPrefab, "Player", new Vector2(0f, -25f), 1f, 12, root.transform);
        AddPlayerMovement(player, -42f, 42f, -27f, 27f);

        CreateCamera(root.transform, new Vector2(0f, -22f), 15f, 15f, new Vector2(0f, 3f));
        SaveScene(scene, StadiumScenePath);
    }

    private static void BuildRoom2D(Sprite white, GameObject npcPrefab, GameObject playerPrefab, GameObject[] houses, GameObject[] trees)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Dormitorio de participantes");
        Shape("Fondo dormitorio", white, Vector2.zero, new Vector2(128f, 70f), new Color(0.035f, 0.05f, 0.1f), root.transform, -100);
        Shape("Pared principal", white, Vector2.zero, new Vector2(116f, 54f), new Color(0.74f, 0.78f, 0.81f), root.transform, -30);
        Shape("Suelo", white, new Vector2(0f, -22f), new Vector2(116f, 4f), new Color(0.32f, 0.37f, 0.43f), root.transform, -10);
        Shape("Techo azul", white, new Vector2(0f, 26f), new Vector2(116f, 4f), new Color(0.08f, 0.3f, 0.85f), root.transform, -10);
        Shape("Muro izquierdo", white, new Vector2(-57f, 1f), new Vector2(3f, 50f), new Color(0.48f, 0.53f, 0.59f), root.transform, -8);
        Shape("Muro derecho", white, new Vector2(57f, 1f), new Vector2(3f, 50f), new Color(0.48f, 0.53f, 0.59f), root.transform, -8);

        for (int row = 0; row < 4; row++)
        {
            float y = -17f + row * 11f;
            for (int col = 0; col < 8; col++)
            {
                float x = -49f + col * 14f;
                Shape("Marco litera", white, new Vector2(x, y + 2f), new Vector2(10.5f, 0.42f), new Color(0.15f, 0.2f, 0.28f), root.transform, 1);
                Shape("Colchon inferior", white, new Vector2(x, y + 0.6f), new Vector2(9.8f, 1.7f), new Color(0.13f, 0.4f, 0.62f), root.transform, 2);
                Shape("Colchon superior", white, new Vector2(x, y + 4.5f), new Vector2(9.8f, 1.7f), new Color(0.13f, 0.4f, 0.62f), root.transform, 2);
                Shape("Almohada inferior", white, new Vector2(x - 3.5f, y + 0.7f), new Vector2(1.8f, 1.2f), new Color(0.82f, 0.84f, 0.83f), root.transform, 3);
                Shape("Almohada superior", white, new Vector2(x - 3.5f, y + 4.6f), new Vector2(1.8f, 1.2f), new Color(0.82f, 0.84f, 0.83f), root.transform, 3);
                Shape("Escalera litera", white, new Vector2(x + 4.6f, y + 3.2f), new Vector2(0.32f, 6.5f), new Color(0.25f, 0.28f, 0.32f), root.transform, 3);
            }
        }
        Shape("Puerta vigilada", white, new Vector2(0f, -19f), new Vector2(10f, 7f), new Color(0.22f, 0.17f, 0.18f), root.transform, 5);

        for (int i = 0; i < 36; i++)
        {
            float x = -51f + (i % 12) * 9f;
            float y = -10.5f + (i / 12) * 11f;
            GameObject npc = AddPerson(npcPrefab, "Participante_Dormitorio_" + (i + 1).ToString("00"), new Vector2(x, y), 0.55f, 6, root.transform);
            AddWander(npc, new Vector2(-53f, y - 0.7f), new Vector2(53f, y + 0.7f), 1.25f);
        }
        for (int i = 0; i < 4; i++)
        {
            GameObject guard = AddPerson(npcPrefab, "Guardia_Rojo_Dormitorio_" + (i + 1), new Vector2(-9f + i * 6f, -15f), 0.8f, 10, root.transform);
            guard.GetComponent<SpriteRenderer>().color = new Color(0.92f, 0.14f, 0.27f);
        }
        GameObject player = AddPerson(playerPrefab, "Player", new Vector2(-51f, -18f), 1f, 12, root.transform);
        AddPlayerMovement(player, -53f, 53f, -19f, 19f);
        CreateCamera(root.transform, new Vector2(0f, 1f), 36f, 17f, new Vector2(0f, 3f));
        SaveScene(scene, RoomScenePath);
    }

    private static void BuildOldPlayRoom2D(Sprite white, GameObject npcPrefab, GameObject playerPrefab, GameObject[] houses, GameObject[] trees)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Cuarto del juego 2D");
        Sprite sky = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tira2D/CieloAlturas.png");
        if (sky != null) Shape("Fondo", sky, Vector2.zero, new Vector2(128f, 70f), Color.white, root.transform, -100);
        Shape("Pared interior", white, Vector2.zero, new Vector2(116f, 54f), new Color(0.75f, 0.29f, 0.36f), root.transform, -30);
        Shape("Muro lateral izquierdo", white, new Vector2(-57f, 2f), new Vector2(4f, 52f), new Color(0.84f, 0.72f, 0.61f), root.transform, -20);
        Shape("Muro lateral derecho", white, new Vector2(57f, 2f), new Vector2(4f, 52f), new Color(0.55f, 0.22f, 0.31f), root.transform, -20);
        Shape("Techo azul", white, new Vector2(0f, 28f), new Vector2(120f, 4f), new Color(0.09f, 0.32f, 0.89f), root.transform, -10);
        Shape("Viga del techo", white, new Vector2(0f, 25.5f), new Vector2(118f, 0.55f), new Color(0.12f, 0.17f, 0.26f), root.transform, -9);
        Shape("Piso de la sala", white, new Vector2(0f, -22f), new Vector2(120f, 3f), new Color(0.37f, 0.43f, 0.41f), root.transform, -8);
        Shape("Área rectangular de casitas", white, new Vector2(-23f, -12f), new Vector2(54f, 16f), new Color(0.38f, 0.57f, 0.4f), root.transform, -6);

        float[] houseX = { -44f, -35f, -26f, -17f, -8f, 1f };
        for (int i = 0; i < houseX.Length; i++)
        {
            GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(houses[i % houses.Length], root.transform);
            house.name = "Casita_2D_" + (i + 1).ToString("00");
            house.transform.localPosition = new Vector3(houseX[i], -19f, -0.2f);
            house.transform.localScale = Vector3.one * 1.8f;
            house.GetComponent<SpriteRenderer>().sortingOrder = 2;
        }
        Shape("Línea blanca después de las casitas", white, new Vector2(8f, -11f), new Vector2(0.45f, 18f), Color.white, root.transform, 3);

        float[] treeX = { 21f, 27f, 33f, 39f, 45f, 23f, 30f, 37f, 44f, 50f };
        float[] treeY = { -20f, -20f, -20f, -20f, -20f, -15f, -15f, -15f, -15f, -15f };
        for (int i = 0; i < treeX.Length; i++)
        {
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(trees[i % trees.Length], root.transform);
            tree.name = "Arbolito_2D_" + (i + 1).ToString("00");
            tree.transform.localPosition = new Vector3(treeX[i], treeY[i], -0.3f);
            tree.transform.localScale = Vector3.one * (i < 5 ? 1f : 0.72f);
            tree.GetComponent<SpriteRenderer>().sortingOrder = i < 5 ? 2 : 1;
        }

        GameObject giant = AddPerson(npcPrefab, "Personaje Gigante x3", new Vector2(34f, -20.5f), 3f, 5, root.transform);
        GameObject player = AddPerson(playerPrefab, "Player", new Vector2(-53f, -20.5f), 1f, 8, root.transform);
        AddPlayerMovement(player, -54f, 54f, -20.5f, 19f);
        Shape("Luz superior", white, new Vector2(0f, 22.5f), new Vector2(22f, 0.22f), new Color(1f, 0.82f, 0.5f), root.transform, -5);

        CreateCamera(root.transform, new Vector2(0f, 1f), 37f, 12f, new Vector2(0f, 3f));
        SaveScene(scene, RoomScenePath);
    }

    private static void CreateCamera(Transform parent, Vector2 overviewCenter, float overviewSize, float followSize, Vector2 followOffset)
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(overviewCenter.x, overviewCenter.y, -20f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = overviewSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.13f, 0.2f);

        CinematicCamera2D intro = cameraObject.AddComponent<CinematicCamera2D>();
        var serialized = new SerializedObject(intro);
        serialized.FindProperty("overviewCenter").vector2Value = overviewCenter;
        serialized.FindProperty("overviewSize").floatValue = overviewSize;
        serialized.FindProperty("followSize").floatValue = followSize;
        serialized.FindProperty("followOffset").vector2Value = followOffset;
        serialized.FindProperty("introDuration").floatValue = 6f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddPlayerMovement(GameObject player, float minX, float maxX, float minY, float maxY)
    {
        PlayerMovement2D movement = player.AddComponent<PlayerMovement2D>();
        var serialized = new SerializedObject(movement);
        serialized.FindProperty("minX").floatValue = minX;
        serialized.FindProperty("maxX").floatValue = maxX;
        serialized.FindProperty("minY").floatValue = minY;
        serialized.FindProperty("maxY").floatValue = maxY;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddWander(GameObject npc, Vector2 min, Vector2 max, float speed)
    {
        NPCWander2D wander = npc.AddComponent<NPCWander2D>();
        var serialized = new SerializedObject(wander);
        serialized.FindProperty("minBounds").vector2Value = min;
        serialized.FindProperty("maxBounds").vector2Value = max;
        serialized.FindProperty("speed").floatValue = speed;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject AddPerson(GameObject prefab, string name, Vector2 position, float scale, int order, Transform parent)
    {
        GameObject person = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        person.name = name;
        person.transform.localPosition = new Vector3(position.x, position.y, -0.5f);
        person.transform.localScale = Vector3.one * scale;
        person.GetComponent<SpriteRenderer>().sortingOrder = order;
        return person;
    }

    private static GameObject MakePrefab(string name, Sprite sprite, string path)
    {
        var root = new GameObject(name);
        root.AddComponent<SpriteRenderer>().sprite = sprite;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject Shape(string name, Sprite sprite, Vector2 position, Vector2 size, Color color, Transform parent, int order)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(position.x, position.y, 0f);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return obj;
    }

    private static Sprite LoadOrMakeWhite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tira2D/FormaBlanca.png");
        if (sprite == null)
        {
            var pixels = new Color32[8 * 8];
            FillRect(pixels, 8, 8, 0, 0, 7, 7, Color.white);
            sprite = SaveSprite("FormaBlanca.png", 8, 8, 4f, false, pixels);
        }
        return sprite;
    }

    private static Sprite MakePersonSprite(string path, Color32 shirt)
    {
        const int width = 96;
        const int height = 144;
        var pixels = Transparent(width, height);
        Color32 skin = new Color32(242, 192, 153, 255);
        Color32 pants = new Color32(37, 49, 72, 255);
        Color32 shoe = new Color32(22, 26, 35, 255);
        Color32 glove = new Color32(240, 222, 187, 255);
        Color32 hair = new Color32(45, 34, 33, 255);
        FillRect(pixels, width, height, 37, 4, 46, 34, pants);
        FillRect(pixels, width, height, 25, 2, 43, 9, shoe);
        FillRect(pixels, width, height, 49, 2, 67, 9, shoe);
        FillRect(pixels, width, height, 34, 35, 62, 88, shirt);
        FillRect(pixels, width, height, 31, 78, 65, 87, new Color32(238, 232, 214, 255));
        ThickLine(pixels, width, height, 45, 77, 12, 91, 7, shirt);
        ThickLine(pixels, width, height, 51, 77, 85, 86, 7, shirt);
        Circle(pixels, width, height, 18, 83, 4, glove);
        Circle(pixels, width, height, 78, 85, 4, glove);
        Circle(pixels, width, height, 48, 111, 17, skin);
        FillRect(pixels, width, height, 34, 119, 61, 132, hair);
        Circle(pixels, width, height, 60, 110, 2, new Color32(25, 27, 31, 255));
        return SaveSprite(Path.GetFileName(path), width, height, 48f, true, pixels);
    }

    private static Sprite MakeHouseSprite(string filename, Color32 body, Color32 roof)
    {
        const int size = 128;
        var pixels = Transparent(size, size);
        FillRect(pixels, size, size, 11, 4, 117, 73, body);
        // Gabled roof silhouette with two bright roof halves.
        for (int y = 68; y <= 123; y++)
        {
            int halfWidth = Mathf.RoundToInt((123 - y) * 1.02f);
            FillRect(pixels, size, size, 64 - halfWidth, y, 64 + halfWidth, y, roof);
        }
        FillRect(pixels, size, size, 52, 4, 76, 49, new Color32(91, 61, 48, 255));
        FillRect(pixels, size, size, 23, 39, 43, 58, new Color32(102, 217, 231, 255));
        FillRect(pixels, size, size, 85, 39, 105, 58, new Color32(102, 217, 231, 255));
        FillRect(pixels, size, size, 31, 47, 35, 51, Color.white);
        FillRect(pixels, size, size, 93, 47, 97, 51, Color.white);
        return SaveSprite(filename, size, size, 16f, true, pixels);
    }

    private static Sprite MakeTreeSprite(string filename, Color32 leaves)
    {
        const int width = 80;
        const int height = 144;
        var pixels = Transparent(width, height);
        Color32 trunk = new Color32(110, 67, 39, 255);
        FillRect(pixels, width, height, 34, 0, 46, 71, trunk);
        Circle(pixels, width, height, 40, 91, 30, leaves);
        Circle(pixels, width, height, 24, 103, 20, Color32.Lerp(leaves, new Color32(38, 112, 55, 255), 0.35f));
        Circle(pixels, width, height, 55, 107, 21, Color32.Lerp(leaves, new Color32(135, 202, 83, 255), 0.3f));
        Circle(pixels, width, height, 40, 123, 18, Color32.Lerp(leaves, Color.white, 0.18f));
        return SaveSprite(filename, width, height, 32f, true, pixels);
    }

    private static Sprite MakeCircleSprite()
    {
        const int size = 128;
        string path = ArtFolder + "/CircleRing.png";
        if (!File.Exists(AbsoluteAssetPath(path)))
        {
            var pixels = Transparent(size, size);
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (distance <= 62f && distance >= 57f) pixels[y * size + x] = new Color32(255, 255, 255, 255);
                }
            SaveSprite("CircleRing.png", size, size, 64f, false, pixels);
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite SaveSprite(string filename, int width, int height, float ppu, bool bottomPivot, Color32[] pixels)
    {
        string path = ArtFolder + "/" + filename;
        string absolute = AbsoluteAssetPath(path);
        if (!File.Exists(absolute))
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = ppu;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (bottomPivot)
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
        }
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static string AbsoluteAssetPath(string assetPath)
    {
        return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
    }

    private static Color32[] Transparent(int width, int height)
    {
        return new Color32[width * height];
    }

    private static void FillRect(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
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

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private static void SaveScene(Scene scene, string path)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path)) throw new System.InvalidOperationException("No se pudo guardar " + path);
    }
}
