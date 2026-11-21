using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateGameRoom
{
    private const string ScenePath = "Assets/0_0Scenes/CuartoDelJuego.unity";
    private const string PrefabFolder = "Assets/0_2prefabs";

    [MenuItem("Tools/Create Large Game Room")]
    public static void Create()
    {
        Material floor = Mat("Assets/Art/Sala_Piso.mat", new Color(0.55f, 0.57f, 0.53f));
        Material blueCeiling = CeilingMat();
        Material wallA = Mat("Assets/Art/Sala_Muros_Coral.mat", new Color(0.77f, 0.28f, 0.36f));
        Material wallB = Mat("Assets/Art/Sala_Muros_Crema.mat", new Color(0.91f, 0.77f, 0.63f));
        Material border = Mat("Assets/Art/Sala_Marcos.mat", new Color(0.13f, 0.15f, 0.21f));
        Material lamp = Mat("Assets/Art/Sala_Lamparas.mat", new Color(1f, 0.83f, 0.54f));
        Material houseFloor = Mat("Assets/Art/Zona_Casitas.mat", new Color(0.39f, 0.57f, 0.39f));
        Material white = Mat("Assets/Art/Linea_Blanca_Sala.mat", new Color(0.96f, 0.96f, 0.9f));
        Material houseDoor = Mat("Assets/Art/Puerta_Casitas.mat", new Color(0.12f, 0.4f, 0.76f));
        Material houseWindow = Mat("Assets/Art/Ventana_Casitas.mat", new Color(0.24f, 0.82f, 0.91f));
        Sprite roomBackdrop = LoadRoomBackdrop();
        Material playerColor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Player_Celeste.mat");
        if (playerColor == null) playerColor = Mat("Assets/Art/Player_Celeste.mat", new Color(0.05f, 0.82f, 1f));
        Material skin = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Personaje_Piel.mat");
        if (skin == null) skin = Mat("Assets/Art/Personaje_Piel.mat", new Color(0.88f, 0.67f, 0.48f));

        GameObject[] trees = CreateTreePrefabs();
        GameObject[] houses = CreateHousePrefabs(houseDoor, houseWindow);
        GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/NPC_Cancha.prefab");
        if (npcPrefab == null) throw new System.InvalidOperationException("No se encontró el prefab NPC_Cancha.");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var room = new GameObject("Sala cerrada del juego");
        BuildRoomShell(room.transform, floor, blueCeiling, wallA, wallB, border, lamp, roomBackdrop);
        BuildHouseArea(room.transform, houses, houseFloor, white);
        BuildGiantAndTrees(room.transform, npcPrefab, trees);
        GameObject player = CreatePlayer(room.transform, playerColor, skin);
        CreateCamera(player.transform);
        CreateLighting(room.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("CuartoDelJuego creado: sala cerrada, techo azul, casitas, línea, gigante y árboles.");
    }

    [MenuItem("Tools/Apply Background to Existing Game Room")]
    public static void ApplyBackdropToExistingRoom()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject room = GameObject.Find("Sala cerrada del juego");
        if (room == null) throw new System.InvalidOperationException("No se encontró la sala cerrada del juego.");
        Transform oldBackdrop = room.transform.Find("Fondo de los cuartos");
        if (oldBackdrop != null) Object.DestroyImmediate(oldBackdrop.gameObject);
        Sprite roomBackdrop = LoadRoomBackdrop();
        if (roomBackdrop == null) throw new System.InvalidOperationException("No se pudo importar el fondo de los cuartos.");
        var backdrop = new GameObject("Fondo de los cuartos");
        backdrop.transform.SetParent(room.transform, false);
        backdrop.transform.localPosition = new Vector3(0f, 12f, 64.35f);
        backdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        backdrop.transform.localScale = new Vector3(108f, 56f, 1f);
        var renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = roomBackdrop;
        renderer.sortingOrder = -100;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void BuildRoomShell(Transform parent, Material floor, Material ceiling, Material wallA, Material wallB, Material trim, Material lamp, Sprite roomBackdrop)
    {
        Cube("Piso continuo", new Vector3(0f, -0.5f, 0f), new Vector3(110f, 1f, 130f), floor, parent);
        Cube("Muro este", new Vector3(55f, 12f, 0f), new Vector3(1f, 24f, 130f), wallA, parent);
        Cube("Muro oeste", new Vector3(-55f, 12f, 0f), new Vector3(1f, 24f, 130f), wallB, parent);
        Cube("Muro de entrada", new Vector3(0f, 12f, -65f), new Vector3(110f, 24f, 1f), wallB, parent);
        Cube("Muro del fondo", new Vector3(0f, 12f, 65f), new Vector3(110f, 24f, 1f), wallA, parent);
        Cube("Techo azul", new Vector3(0f, 24.5f, 0f), new Vector3(112f, 1f, 132f), ceiling, parent);
        var interiorCeiling = GameObject.CreatePrimitive(PrimitiveType.Quad);
        interiorCeiling.name = "Revestimiento azul interior";
        interiorCeiling.transform.SetParent(parent, false);
        interiorCeiling.transform.localPosition = new Vector3(0f, 23.98f, 0f);
        interiorCeiling.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        interiorCeiling.transform.localScale = new Vector3(110f, 130f, 1f);
        interiorCeiling.GetComponent<Renderer>().sharedMaterial = ceiling;
        Collider ceilingCollider = interiorCeiling.GetComponent<Collider>();
        if (ceilingCollider != null) Object.DestroyImmediate(ceilingCollider);

        for (int x = -2; x <= 2; x++)
        {
            Cube("Pilar interior este", new Vector3(54f, 12f, x * 24f), new Vector3(1.6f, 24f, 1.6f), trim, parent);
            Cube("Pilar interior oeste", new Vector3(-54f, 12f, x * 24f), new Vector3(1.6f, 24f, 1.6f), trim, parent);
        }
        Cube("Viga perimetral superior", new Vector3(0f, 22.8f, 0f), new Vector3(108f, 0.8f, 128f), trim, parent);
        Cube("Lámpara central", new Vector3(0f, 23.7f, 0f), new Vector3(25f, 0.16f, 4f), lamp, parent);
        if (roomBackdrop != null)
        {
            var backdrop = new GameObject("Fondo de los cuartos");
            backdrop.transform.SetParent(parent, false);
            backdrop.transform.localPosition = new Vector3(0f, 12f, 64.35f);
            backdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            backdrop.transform.localScale = new Vector3(108f, 56f, 1f);
            var renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = roomBackdrop;
            renderer.sortingOrder = -100;
        }
    }

    private static Sprite LoadRoomBackdrop()
    {
        const string path = "Assets/Resources/Backgrounds/Fondo_Cuartos.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
        if (changed) importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void BuildHouseArea(Transform parent, GameObject[] housePrefabs, Material area, Material line)
    {
        Cube("Rectángulo de casitas", new Vector3(0f, 0.04f, 26f), new Vector3(48f, 0.08f, 40f), area, parent);
        float[] xPositions = { -16f, 0f, 16f };
        float[] zPositions = { 13f, 34f };
        int count = 0;
        foreach (float z in zPositions)
            foreach (float x in xPositions)
            {
                GameObject prefab = housePrefabs[count % housePrefabs.Length];
                GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                house.name = "Casita_" + (count + 1).ToString("00");
                house.transform.localPosition = new Vector3(x, 0.1f, z);
                house.transform.localRotation = Quaternion.identity;
                house.transform.localScale = Vector3.one;
                count++;
            }

        Cube("Línea blanca después de las casitas", new Vector3(0f, 0.1f, 43f), new Vector3(56f, 0.12f, 0.42f), line, parent);
        Cube("Marca lateral izquierda", new Vector3(-28f, 0.08f, 27f), new Vector3(0.3f, 0.1f, 44f), line, parent);
        Cube("Marca lateral derecha", new Vector3(28f, 0.08f, 27f), new Vector3(0.3f, 0.1f, 44f), line, parent);
    }

    private static void BuildGiantAndTrees(Transform parent, GameObject npcPrefab, GameObject[] treePrefabs)
    {
        GameObject giant = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab, parent);
        giant.name = "Personaje Gigante x3";
        giant.transform.localPosition = new Vector3(0f, 1f, -26f);
        giant.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        giant.transform.localScale = Vector3.one * 3f;

        var platform = Cube("Base del personaje gigante", new Vector3(0f, 0.45f, -26f), new Vector3(9f, 0.9f, 7f),
            Mat("Assets/Art/Base_Gigante.mat", new Color(0.12f, 0.14f, 0.21f)), parent);
        Object.DestroyImmediate(platform.GetComponent<Collider>());
        Cube("Anillo de la base", new Vector3(0f, 0.92f, -26f), new Vector3(9.2f, 0.12f, 7.2f),
            Mat("Assets/Art/Marco_Premio.mat", new Color(0.9f, 0.08f, 0.34f)), parent);

        Vector3[] positions =
        {
            new Vector3(-23f, 0f, -19f), new Vector3(-31f, 0f, -30f), new Vector3(-23f, 0f, -41f),
            new Vector3(-31f, 0f, -52f), new Vector3(-13f, 0f, -49f),
            new Vector3(23f, 0f, -19f), new Vector3(31f, 0f, -30f), new Vector3(23f, 0f, -41f),
            new Vector3(31f, 0f, -52f), new Vector3(13f, 0f, -49f)
        };
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefabs[i % treePrefabs.Length], parent);
            tree.name = "Arbolito_" + (i + 1).ToString("00");
            tree.transform.localPosition = positions[i];
            tree.transform.localRotation = Quaternion.Euler(0f, (i * 37) % 360, 0f);
            tree.transform.localScale = Vector3.one * (0.8f + (i % 3) * 0.12f);
        }
    }

    private static GameObject CreatePlayer(Transform parent, Material bodyMaterial, Material skin)
    {
        var player = new GameObject("Player");
        player.transform.SetParent(parent, false);
        player.transform.localPosition = new Vector3(0f, 0f, -44f);
        AddBodyPart(player.transform, "Cuerpo", PrimitiveType.Capsule, new Vector3(0f, 0.69f, 0f),
            new Vector3(0.65f, 0.69f, 0.65f), bodyMaterial);
        AddBodyPart(player.transform, "Cabeza", PrimitiveType.Sphere, new Vector3(0f, 1.5f, 0f),
            Vector3.one * 0.48f, skin);

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.36f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.minMoveDistance = 0f;
        var movement = player.AddComponent<PlayerMovement>();
        var serializedMovement = new SerializedObject(movement);
        serializedMovement.FindProperty("fieldHalfWidth").floatValue = 49f;
        serializedMovement.FindProperty("fieldHalfLength").floatValue = 59f;
        serializedMovement.ApplyModifiedPropertiesWithoutUndo();
        return player;
    }

    private static void CreateCamera(Transform player)
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 21f, -59f);
        cameraObject.transform.LookAt(new Vector3(0f, 7f, 0f));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic=true;
        camera.orthographicSize=58;
        camera.farClipPlane = 350f;

        cameraObject.AddComponent<StadiumIntroCamera>();
    }

    private static void CreateLighting(Transform parent)
    {
        var light = new GameObject("Luz de sala", typeof(Light));
        light.transform.SetParent(parent, false);
        light.transform.rotation = Quaternion.Euler(52f, -25f, 0f);
        Light directional = light.GetComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 1.15f;
        directional.color = new Color(1f, 0.91f, 0.78f);

        foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
            {
                var pointObject = new GameObject("Luz de techo", typeof(Light));
                pointObject.transform.SetParent(parent, false);
                pointObject.transform.localPosition = new Vector3(x * 35f, 22f, z * 43f);
                Light point = pointObject.GetComponent<Light>();
                point.type = LightType.Point;
                point.range = 55f;
                point.intensity = 12f;
                point.color = new Color(0.68f, 0.8f, 1f);
            }
    }

    private static GameObject[] CreateTreePrefabs()
    {
        Material trunk = Mat("Assets/Art/Arbol_Tronco.mat", new Color(0.32f, 0.17f, 0.09f));
        Material leafA = Mat("Assets/Art/Arbol_Hojas_A.mat", new Color(0.18f, 0.48f, 0.25f));
        Material leafB = Mat("Assets/Art/Arbol_Hojas_B.mat", new Color(0.3f, 0.64f, 0.31f));
        Material leafC = Mat("Assets/Art/Arbol_Hojas_C.mat", new Color(0.1f, 0.34f, 0.2f));
        var prefabs = new GameObject[3];
        for (int type = 0; type < prefabs.Length; type++)
        {
            var tree = new GameObject("Arbolito_" + (type + 1));
            Cylinder("Tronco", new Vector3(0f, 0.9f, 0f), new Vector3(0.42f, 0.9f, 0.42f), trunk, tree.transform);
            if (type == 0)
            {
                Sphere("Copa baja", new Vector3(0f, 2.05f, 0f), new Vector3(2.1f, 1.45f, 2.1f), leafA, tree.transform);
                Sphere("Copa alta", new Vector3(0.2f, 2.85f, 0.1f), new Vector3(1.6f, 1.55f, 1.6f), leafB, tree.transform);
            }
            else if (type == 1)
            {
                Sphere("Copa inferior", new Vector3(0f, 1.9f, 0f), new Vector3(2.35f, 1.55f, 2.35f), leafC, tree.transform);
                Sphere("Copa media", new Vector3(0f, 2.7f, 0f), new Vector3(1.75f, 1.45f, 1.75f), leafA, tree.transform);
                Sphere("Copa alta", new Vector3(0f, 3.45f, 0f), new Vector3(1.05f, 1.3f, 1.05f), leafB, tree.transform);
            }
            else
            {
                Sphere("Copa izquierda", new Vector3(-0.65f, 2.25f, 0f), new Vector3(1.65f, 1.55f, 1.65f), leafA, tree.transform);
                Sphere("Copa derecha", new Vector3(0.65f, 2.25f, 0.1f), new Vector3(1.65f, 1.55f, 1.65f), leafB, tree.transform);
                Sphere("Copa central", new Vector3(0f, 3f, 0f), new Vector3(1.6f, 1.7f, 1.6f), leafC, tree.transform);
            }
            prefabs[type] = PrefabUtility.SaveAsPrefabAsset(tree, PrefabFolder + "/Arbolito_" + (type + 1) + ".prefab");
            Object.DestroyImmediate(tree);
        }
        return prefabs;
    }

    private static GameObject[] CreateHousePrefabs(Material door, Material window)
    {
        Material[] bodies =
        {
            Mat("Assets/Art/Casita_Coral.mat", new Color(0.9f, 0.31f, 0.35f)),
            Mat("Assets/Art/Casita_Amarilla.mat", new Color(0.95f, 0.68f, 0.19f)),
            Mat("Assets/Art/Casita_Menta.mat", new Color(0.25f, 0.69f, 0.55f))
        };
        Material[] roofs =
        {
            Mat("Assets/Art/Techo_Azul.mat", new Color(0.13f, 0.33f, 0.73f)),
            Mat("Assets/Art/Techo_Violeta.mat", new Color(0.4f, 0.25f, 0.62f)),
            Mat("Assets/Art/Techo_Coral.mat", new Color(0.83f, 0.32f, 0.33f))
        };
        Material wall = Mat("Assets/Art/Marco_Casita.mat", new Color(0.94f, 0.86f, 0.7f));
        var prefabs = new GameObject[3];
        for (int i = 0; i < prefabs.Length; i++)
        {
            var house = new GameObject("Casita_" + (i + 1));
            Cube("Cuerpo de casita", new Vector3(0f, 1.55f, 0f), new Vector3(7f, 3f, 6.4f), bodies[i], house.transform);
            Cube("Techo izquierdo", new Vector3(-1.48f, 3.35f, 0f), new Vector3(4.25f, 0.36f, 7f), roofs[i], house.transform).transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            Cube("Techo derecho", new Vector3(1.48f, 3.35f, 0f), new Vector3(4.25f, 0.36f, 7f), roofs[i], house.transform).transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            Cube("Puerta", new Vector3(0f, 1f, -3.25f), new Vector3(1.45f, 2f, 0.12f), door, house.transform);
            Cube("Ventana izquierda", new Vector3(-2.3f, 2.05f, -3.28f), new Vector3(1.05f, 0.9f, 0.1f), window, house.transform);
            Cube("Ventana derecha", new Vector3(2.3f, 2.05f, -3.28f), new Vector3(1.05f, 0.9f, 0.1f), window, house.transform);
            Cube("Marco de puerta", new Vector3(0f, 2.08f, -3.34f), new Vector3(1.7f, 0.12f, 0.12f), wall, house.transform);
            prefabs[i] = PrefabUtility.SaveAsPrefabAsset(house, PrefabFolder + "/Casita_" + (i + 1) + ".prefab");
            Object.DestroyImmediate(house);
        }
        return prefabs;
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static GameObject Cylinder(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static GameObject Sphere(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    private static void AddBodyPart(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    private static Material Mat(string path, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material CeilingMat()
    {
        const string path = "Assets/Art/Sala_Techo_Azul.mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material ceiling = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (ceiling == null)
        {
            ceiling = new Material(shader);
            AssetDatabase.CreateAsset(ceiling, path);
        }
        else if (ceiling.shader != shader)
        {
            ceiling.shader = shader;
        }
        ceiling.color = new Color(0.06f, 0.28f, 0.96f);
        EditorUtility.SetDirty(ceiling);
        return ceiling;
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
