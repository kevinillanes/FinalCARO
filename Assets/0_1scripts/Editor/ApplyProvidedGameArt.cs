using UnityEditor;
using UnityEngine;

public static class ApplyProvidedGameArt
{
    [MenuItem("Tools/Apply Provided Game Art")]
    public static void Apply()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        CreateTugOfWar2D.Create();
        CreateJumpRopeScene.Create();
        CreateGameRoom.Create();
        AssetDatabase.SaveAssets();
        Debug.Log("Se aplicaron los fondos y el sprite animado de la mu\u00f1eca a las escenas del juego.");
    }
}
