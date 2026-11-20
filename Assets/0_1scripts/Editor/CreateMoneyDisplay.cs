using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class CreateMoneyDisplay
{
    private const string ScenePath = "Assets/0_0Scenes/Estadio.unity";
    private const string PrefabPath = "Assets/0_2prefabs/CajaDeDinero.prefab";

    [MenuItem("Tools/Create Center Money Display")]
    public static void Create()
    {
        Material greenA = GetOrCreateMaterial("Assets/Art/Billetes_Verdes.mat", new Color(0.06f, 0.34f, 0.18f));
        Material greenB = GetOrCreateMaterial("Assets/Art/Billetes_Verdes_Claros.mat", new Color(0.09f, 0.48f, 0.24f));
        Material band = GetOrCreateMaterial("Assets/Art/Fajillo_Billetes.mat", new Color(0.88f, 0.78f, 0.46f));
        Material pink = GetOrCreateMaterial("Assets/Art/Marco_Premio.mat", new Color(0.9f, 0.08f, 0.34f));
        Material gold = GetOrCreateMaterial("Assets/Art/Detalles_Dorados.mat", new Color(1f, 0.67f, 0.12f));
        Material baseMaterial = GetOrCreateMaterial("Assets/Art/Base_Premio.mat", new Color(0.035f, 0.055f, 0.09f));
        Material glass = GetOrCreateGlassMaterial();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject previous = GameObject.Find("Caja de dinero");
        if (previous != null) Object.DestroyImmediate(previous);

        var root = new GameObject("Caja de dinero");
        BuildPedestal(root.transform, baseMaterial, pink, gold);
        BuildGlassCase(root.transform, glass, pink, gold);
        BuildCash(root.transform, greenA, greenB, band);
        BuildPlaque(root.transform, pink, gold);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Transform stadium = GameObject.Find("ESTADIO")?.transform;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stadium);
        instance.name = "Caja de dinero";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Caja de dinero añadida al centro del estadio.");
    }

    private static void BuildPedestal(Transform parent, Material dark, Material pink, Material gold)
    {
        Cube("Base inferior", new Vector3(0f, 0.35f, 0f), new Vector3(11f, 0.7f, 9f), dark, parent);
        Cube("Banda rosa inferior", new Vector3(0f, 0.74f, 0f), new Vector3(10.65f, 0.12f, 8.65f), pink, parent);
        Cube("Plataforma superior", new Vector3(0f, 0.93f, 0f), new Vector3(10.25f, 0.32f, 8.25f), gold, parent);
        Cube("Suelo interior", new Vector3(0f, 1.13f, 0f), new Vector3(8.15f, 0.12f, 6.45f), dark, parent);
        // Bright trim around the reward platform.
        Cube("Luz frontal", new Vector3(0f, 0.85f, -4.48f), new Vector3(9.8f, 0.08f, 0.06f), gold, parent);
        Cube("Luz trasera", new Vector3(0f, 0.85f, 4.48f), new Vector3(9.8f, 0.08f, 0.06f), gold, parent);
    }

    private static void BuildGlassCase(Transform parent, Material glass, Material frame, Material gold)
    {
        const float centerY = 3.65f;
        const float height = 4.9f;
        const float halfX = 4.05f;
        const float halfZ = 3.2f;
        const float thickness = 0.08f;

        Cube("Cristal frontal", new Vector3(0f, centerY, -halfZ), new Vector3(8.02f, height, thickness), glass, parent);
        Cube("Cristal trasero", new Vector3(0f, centerY, halfZ), new Vector3(8.02f, height, thickness), glass, parent);
        Cube("Cristal izquierdo", new Vector3(-halfX, centerY, 0f), new Vector3(thickness, height, 6.3f), glass, parent);
        Cube("Cristal derecho", new Vector3(halfX, centerY, 0f), new Vector3(thickness, height, 6.3f), glass, parent);

        foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                Cube("Columna rosa", new Vector3(x * halfX, centerY, z * halfZ), new Vector3(0.22f, height + 0.2f, 0.22f), frame, parent);

        Cube("Marco superior frontal", new Vector3(0f, 6.15f, -halfZ), new Vector3(8.35f, 0.24f, 0.24f), frame, parent);
        Cube("Marco superior trasero", new Vector3(0f, 6.15f, halfZ), new Vector3(8.35f, 0.24f, 0.24f), frame, parent);
        Cube("Marco superior izquierdo", new Vector3(-halfX, 6.15f, 0f), new Vector3(0.24f, 0.24f, 6.55f), frame, parent);
        Cube("Marco superior derecho", new Vector3(halfX, 6.15f, 0f), new Vector3(0.24f, 0.24f, 6.55f), frame, parent);
        Cube("Franja dorada frontal", new Vector3(0f, 6.31f, -halfZ), new Vector3(7.5f, 0.06f, 0.27f), gold, parent);
    }

    private static void BuildCash(Transform parent, Material greenA, Material greenB, Material band)
    {
        int number = 1;
        const float width = 0.77f;
        const float height = 0.40f;
        const float depth = 0.85f;
        for (int level = 0; level < 6; level++)
        {
            float y = 1.4f + level * 0.43f;
            for (int row = 0; row < 5; row++)
            {
                float z = -1.84f + row * 0.92f;
                for (int column = 0; column < 7; column++)
                {
                    float x = -2.52f + column * 0.84f;
                    var stack = new GameObject("Fajo " + (number++).ToString("000"));
                    stack.transform.SetParent(parent, false);
                    stack.transform.localPosition = new Vector3(x, y, z);
                    Material green = (level + row + column) % 2 == 0 ? greenA : greenB;
                    GameObject bills = Cube("Billetes", Vector3.zero, new Vector3(width, height, depth), green, stack.transform);
                    GameObject strap = Cube("Fajillo", new Vector3(0f, height * 0.5f + 0.012f, 0f), new Vector3(0.14f, 0.024f, depth + 0.01f), band, stack.transform);
                    Object.DestroyImmediate(bills.GetComponent<Collider>());
                    Object.DestroyImmediate(strap.GetComponent<Collider>());
                }
            }
        }
    }

    private static void BuildPlaque(Transform parent, Material pink, Material gold)
    {
        Cube("Placa del premio", new Vector3(0f, 6.78f, -3.45f), new Vector3(5.3f, 0.82f, 0.22f), pink, parent);
        Cube("Borde dorado de placa", new Vector3(0f, 7.22f, -3.58f), new Vector3(5.3f, 0.05f, 0.05f), gold, parent);
        var label = new GameObject("Texto del premio", typeof(TextMesh));
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, 6.69f, -3.58f);
        label.transform.localRotation = Quaternion.identity;
        var text = label.GetComponent<TextMesh>();
        text.text = "PREMIO FINAL";
        text.fontSize = 100;
        text.characterSize = 0.3f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(1f, 0.9f, 0.62f);
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material GetOrCreateGlassMaterial()
    {
        const string path = "Assets/Art/Cristal_Vitrina.mat";
        Material glass = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (glass == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            glass = new Material(shader);
            AssetDatabase.CreateAsset(glass, path);
        }

        Color tint = new Color(0.26f, 0.78f, 0.96f, 0.1f);
        glass.color = tint;
        if (glass.HasProperty("_Surface")) glass.SetFloat("_Surface", 1f);
        if (glass.HasProperty("_Blend")) glass.SetFloat("_Blend", 0f);
        if (glass.HasProperty("_SrcBlend")) glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (glass.HasProperty("_DstBlend")) glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (glass.HasProperty("_ZWrite")) glass.SetFloat("_ZWrite", 0f);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glass.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(glass);
        return glass;
    }
}
