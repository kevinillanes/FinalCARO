using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateJumpRopeScene
{
    [MenuItem("Tools/Upgrade Full Platform to Spinning Jump Rope")]
    public static void Create()
    {
        const string path = "Assets/0_0Scenes/TiraYAflojaPisoCompleto.unity";
        var scene = EditorSceneManager.OpenScene(path);
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponent<Camera>() == null) Object.DestroyImmediate(root);
        new GameObject("Práctica de cuerda").AddComponent<JumpRopeGame>();
        EditorSceneManager.SaveScene(scene);
    }
}
