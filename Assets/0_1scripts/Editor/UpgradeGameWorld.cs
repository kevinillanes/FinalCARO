using UnityEditor;

public static class UpgradeGameWorld
{
    [MenuItem("Tools/Apply Game World Improvements")]
    public static void Apply()
    {
        ConvertLegacyScenesTo2D.Convert();
        CreateJumpRopeScene.Create();
    }
}
