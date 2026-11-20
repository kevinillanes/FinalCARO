using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class BuildStadiumScene
{
    private static readonly Color Yellow = new Color(1f, 0.72f, 0.08f);
    private static readonly Color Blue = new Color(0.08f, 0.78f, 1f);
    private static readonly Color Turf = new Color(0.08f, 0.34f, 0.19f);
    private static readonly Color SeatA = new Color(0.1f, 0.16f, 0.23f);
    private static readonly Color SeatB = new Color(0.13f, 0.22f, 0.3f);
    private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

    [MenuItem("Tools/Build Stadium Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.55f, 0.59f, 0.66f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.12f, 0.18f, 0.23f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.006f;

        var stadium = new GameObject("ESTADIO");
        var field = new GameObject("Cancha"); field.transform.SetParent(stadium.transform);
        Cube("Patio exterior", new Vector3(0, -0.8f, 0), new Vector3(170, 0.8f, 140), new Color(0.11f, 0.15f, 0.17f), stadium.transform);
        Cube("Paseo alrededor de la cancha", new Vector3(0, -0.12f, 0), new Vector3(104, 0.24f, 74), new Color(0.24f, 0.28f, 0.29f), stadium.transform);
        Cube("Cancha verde", new Vector3(0, 0, 0), new Vector3(90, 0.4f, 60), Turf, field.transform);
        Cube("Borde perimetral", new Vector3(0, -0.42f, 0), new Vector3(98, 0.35f, 68), new Color(0.16f, 0.2f, 0.2f), stadium.transform);
        // Put the playing surface above its foundation.
        GameObject.Find("Cancha verde").transform.position = new Vector3(0, 0, 0);
        Line("Línea lateral norte", new Vector3(0, 0.212f, 29.5f), new Vector3(89, 0.025f, 0.15f), Color.white, field.transform);
        Line("Línea lateral sur", new Vector3(0, 0.212f, -29.5f), new Vector3(89, 0.025f, 0.15f), Color.white, field.transform);
        Line("Línea de fondo este", new Vector3(44.5f, 0.212f, 0), new Vector3(0.15f, 0.025f, 59), Color.white, field.transform);
        Line("Línea de fondo oeste", new Vector3(-44.5f, 0.212f, 0), new Vector3(0.15f, 0.025f, 59), Color.white, field.transform);
        var center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        center.name = "Círculo central"; center.transform.SetParent(field.transform);
        center.transform.SetPositionAndRotation(new Vector3(0, 0.218f, 0), Quaternion.identity);
        center.transform.localScale = new Vector3(14, 0.012f, 14); SetColor(center, Color.white);
        var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disk.name = "Interior del círculo"; disk.transform.SetParent(field.transform);
        disk.transform.SetPositionAndRotation(new Vector3(0, 0.24f, 0), Quaternion.identity);
        disk.transform.localScale = new Vector3(13.8f, 0.014f, 13.8f); SetColor(disk, Turf);
        Line("Línea de medio campo", new Vector3(0, 0.212f, 0), new Vector3(0.15f, 0.025f, 59), Color.white, field.transform);

        BuildGoal(stadium.transform, -44.5f, 0f);
        BuildGoal(stadium.transform, 44.5f, 180f);
        BuildStands(stadium.transform);
        BuildLights(stadium.transform);

        // A well-spaced 6 by 5 formation leaves ample room for the player.
        int npcNumber = 1;
        for (int row = 0; row < 5; row++)
            for (int column = 0; column < 6; column++)
                Character("NPC_Cancha_" + (npcNumber++).ToString("00"),
                    new Vector3(-30f + column * 12f, 0, 16f - row * 8f), Yellow, stadium.transform);
        Character("Player", new Vector3(0, 0, -26), Blue, stadium.transform, 1.25f);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 59, -127);
        cameraObject.transform.LookAt(new Vector3(0, 4, 1));
        var camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = 54f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 450f;
        camera.clearFlags = CameraClearFlags.Skybox;
        var sun = new GameObject("Luz principal", typeof(Light));
        sun.transform.rotation = Quaternion.Euler(48, -28, 0);
        sun.GetComponent<Light>().type = LightType.Directional;
        sun.GetComponent<Light>().intensity = 1.35f;
        sun.GetComponent<Light>().color = new Color(1f, 0.91f, 0.76f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/0_0Scenes/Estadio.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/0_0Scenes/Estadio.unity", true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Estadio creado en Assets/0_0Scenes/Estadio.unity");
    }

    private static void BuildStands(Transform parent)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            for (int tier = 0; tier < 8; tier++)
            {
                float z = side * (35.8f + tier * 2.2f);
                Cube("Grada lateral " + side + " nivel " + tier, new Vector3(0, 1.05f + tier * 0.95f, z),
                    new Vector3(138 - tier * 2f, 1.35f, 2.35f), tier % 2 == 0 ? SeatA : SeatB, parent);
                for (int i = 0; i < 30; i++)
                {
                    float x = -55f + i * 3.8f;
                    Character("NPC_Tribuna_" + (side > 0 ? "N" : "S") + "_" + tier + "_" + i,
                        new Vector3(x, 1.62f + tier * 0.95f, z - side * 0.75f), Yellow, parent, 0.62f);
                }
            }
        }
        for (int end = -1; end <= 1; end += 2)
        {
            for (int tier = 0; tier < 7; tier++)
            {
                float x = end * (48f + tier * 2.2f);
                Cube("Grada cabecera " + end + " nivel " + tier, new Vector3(x, 1.05f + tier * 0.95f, 0),
                    new Vector3(2.35f, 1.35f, 88 - tier * 2f), tier % 2 == 0 ? SeatA : SeatB, parent);
                for (int i = 0; i < 20; i++)
                    Character("NPC_Cabecera_" + end + "_" + tier + "_" + i,
                        new Vector3(x - end * 0.75f, 1.62f + tier * 0.95f, -36f + i * 3.8f), Yellow, parent, 0.62f);
            }
        }
        Cube("Viga superior norte", new Vector3(0, 10.2f, 67), new Vector3(150, 0.8f, 1.2f), new Color(0.07f, 0.1f, 0.15f), parent);
        Cube("Viga superior sur", new Vector3(0, 10.2f, -67), new Vector3(150, 0.8f, 1.2f), new Color(0.07f, 0.1f, 0.15f), parent);
        Cube("Viga superior este", new Vector3(75, 10.2f, 0), new Vector3(1.2f, 0.8f, 134), new Color(0.07f, 0.1f, 0.15f), parent);
        Cube("Viga superior oeste", new Vector3(-75, 10.2f, 0), new Vector3(1.2f, 0.8f, 134), new Color(0.07f, 0.1f, 0.15f), parent);
        Cube("Marcador", new Vector3(0, 15.2f, 67.5f), new Vector3(15, 4f, 0.7f), new Color(0.03f, 0.08f, 0.12f), parent);
        Text3D("ESTADIO · 90 MIN", new Vector3(0, 15.1f, 67.1f), 1.2f, new Color(0.35f, 0.9f, 1f), parent);
    }

    private static void BuildGoal(Transform parent, float x, float yaw)
    {
        var goal = new GameObject("Arco"); goal.transform.SetParent(parent);
        goal.transform.position = new Vector3(x, 0, 0); goal.transform.rotation = Quaternion.Euler(0, yaw, 0);
        Cube("Poste izquierdo", new Vector3(-0.04f, 1.8f, -5f), new Vector3(0.22f, 3.6f, 0.22f), Color.white, goal.transform);
        Cube("Poste derecho", new Vector3(-0.04f, 1.8f, 5f), new Vector3(0.22f, 3.6f, 0.22f), Color.white, goal.transform);
        Cube("Travesaño", new Vector3(-0.04f, 3.58f, 0), new Vector3(0.22f, 0.22f, 10f), Color.white, goal.transform);
    }

    private static void BuildLights(Transform parent)
    {
        foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
        {
            Cube("Torre de iluminación", new Vector3(x * 77, 12, z * 63), new Vector3(0.55f, 24, 0.55f), new Color(0.2f, 0.25f, 0.29f), parent);
            Cube("Batería de focos", new Vector3(x * 77, 24, z * 63), new Vector3(3.8f, 1.2f, 1.8f), new Color(1f, 0.83f, 0.48f), parent);
        }
    }

    private static void Character(string name, Vector3 ground, Color color, Transform parent, float scale = 1f)
    {
        var root = new GameObject(name); root.transform.SetParent(parent); root.transform.position = ground;
        // Compact stylized figures made only from Unity primitives.
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Cuerpo"; body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.55f * scale, 0);
        body.transform.localScale = new Vector3(0.52f * scale, 0.55f * scale, 0.52f * scale); SetColor(body, color);
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Cabeza"; head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 1.2f * scale, 0);
        head.transform.localScale = Vector3.one * (0.38f * scale); SetColor(head, new Color(0.88f, 0.67f, 0.48f));
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 size, Color color, Transform parent)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
        obj.transform.SetParent(parent); obj.transform.position = position; obj.transform.localScale = size;
        SetColor(obj, color); return obj;
    }

    private static GameObject Line(string name, Vector3 position, Vector3 size, Color color, Transform parent) => Cube(name, position, size, color, parent);

    private static void Text3D(string value, Vector3 position, float size, Color color, Transform parent)
    {
        var go = new GameObject("Texto marcador", typeof(TextMesh)); go.transform.SetParent(parent); go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0, 180, 0);
        var text = go.GetComponent<TextMesh>(); text.text = value; text.fontSize = 100; text.characterSize = size;
        text.anchor = TextAnchor.MiddleCenter; text.color = color;
    }

    private static void SetColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (!Materials.TryGetValue(color, out var material))
        {
            material = new Material(shader); material.color = color; Materials[color] = material;
        }
        renderer.sharedMaterial = material;
    }
}
