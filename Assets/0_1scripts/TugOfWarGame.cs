using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TugOfWarGame : MonoBehaviour
{
    private void Start()
    {
        SceneManager.sceneLoaded += OpenPractice;
        SceneManager.LoadScene("LaUltimaRonda");
    }

    private static void OpenPractice(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OpenPractice;
        FindAnyObjectByType<PixelSurvivalGame>().OpenTugPractice();
    }
}
