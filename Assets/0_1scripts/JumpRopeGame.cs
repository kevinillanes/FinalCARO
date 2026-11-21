using UnityEngine;
using UnityEngine.SceneManagement;

// The standalone practice scene enters the same rules and arena used by the campaign.
public sealed class JumpRopeGame : MonoBehaviour
{
    private void Start()
    {
        SceneManager.sceneLoaded += OpenPractice;
        SceneManager.LoadScene("LaUltimaRonda");
    }

    private static void OpenPractice(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OpenPractice;
        FindFirstObjectByType<PixelSurvivalGame>().OpenRopePractice();
    }
}
