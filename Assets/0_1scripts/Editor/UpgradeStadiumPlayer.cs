using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class UpgradeStadiumPlayer
{
    private const string ScenePath = "Assets/0_0Scenes/Estadio.unity";
    private const string PrefabFolder = "Assets/0_2prefabs";
    private const string PrefabPath = PrefabFolder + "/NPC_Cancha.prefab";

    [MenuItem("Tools/Upgrade Stadium Player and NPC Prefabs")]
    public static void Upgrade()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "0_2prefabs");

        Material npcMaterial = GetOrCreateMaterial("Assets/Art/NPC_Amarillo.mat", new Color(1f, 0.72f, 0.08f));
        Material playerMaterial = GetOrCreateMaterial("Assets/Art/Player_Celeste.mat", new Color(0.05f, 0.82f, 1f));
        GameObject template = CreateNpcTemplate(npcMaterial);
        GameObject npcPrefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        Object.DestroyImmediate(template);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int converted = 0;
        var targets = new List<GameObject>();
        foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (candidate.name.StartsWith("NPC_Cancha_")) targets.Add(candidate);
        foreach (GameObject existing in targets)
        {
            Vector3 position = existing.transform.position;
            Quaternion rotation = existing.transform.rotation;
            Transform parent = existing.transform.parent;
            string objectName = existing.name;
            Object.DestroyImmediate(existing);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab, parent);
            instance.name = objectName;
            instance.transform.SetPositionAndRotation(position, rotation);
            converted++;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null) throw new System.InvalidOperationException("No se encontró el objeto Player en la escena.");

        // Keep the Player clearly distinct from the yellow field NPCs.
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
        {
            if (renderer.gameObject.name == "Cuerpo")
            {
                renderer.sharedMaterial = playerMaterial;
            }
        }

        foreach (Collider childCollider in player.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(childCollider);
        var controller = player.GetComponent<CharacterController>();
        if (controller == null) controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.36f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.25f;
        controller.minMoveDistance = 0f;
        if (player.GetComponent<PlayerMovement>() == null)
            player.AddComponent<PlayerMovement>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Player listo para moverse con WASD y flechas. NPC de cancha convertidos a prefab: " + converted);
    }

    private static GameObject CreateNpcTemplate(Material yellowMaterial)
    {
        var root = new GameObject("NPC_Cancha");
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Cuerpo";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        body.transform.localScale = new Vector3(0.52f, 0.55f, 0.52f);
        body.GetComponent<Renderer>().sharedMaterial = yellowMaterial;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Cabeza";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        head.transform.localScale = Vector3.one * 0.38f;
        head.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Assets/Art/Personaje_Piel.mat", new Color(0.88f, 0.67f, 0.48f));
        Object.DestroyImmediate(head.GetComponent<Collider>());
        return root;
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
}
