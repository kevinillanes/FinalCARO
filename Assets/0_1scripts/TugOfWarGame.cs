using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TugOfWarGame : MonoBehaviour
{
    [SerializeField] private Transform centerMarker;
    [SerializeField] private TextMesh instructionText;
    [SerializeField] private float winDistance = 7f;
    [SerializeField] private float pullPerTap = 0.65f;
    [SerializeField] private float opponentPullPerSecond = 0.3f;

    private float progress;
    private bool finished;
    private RopePracticeStage stage;

    private void Start()
    {
        stage=gameObject.AddComponent<RopePracticeStage>();
        stage.Build(false,centerMarker);
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(12,12,132,34),"Volver al menú"))
            UnityEngine.SceneManagement.SceneManager.LoadScene("LaUltimaRonda");
        if(instructionText!=null) GUI.Box(new Rect((Screen.width-530)*.5f,Screen.height-75,530,58),instructionText.text);
    }

    private void Update()
    {
        if (finished) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            progress += pullPerTap;

        progress -= opponentPullPerSecond * Time.deltaTime;
        progress = Mathf.Clamp(progress, -winDistance, winDistance);
        stage.DrawCable(0f,progress/winDistance);
        if (centerMarker != null)
            centerMarker.position = new Vector3(-progress, centerMarker.position.y, centerMarker.position.z);

        if (instructionText == null) return;
        if (progress >= winDistance)
        {
            instructionText.text = "¡GANASTE!";
            instructionText.color = new Color(0.35f, 1f, 0.62f);
            finished = true;
        }
        else if (progress <= -winDistance)
        {
            instructionText.text = "¡TE GANÓ EL OTRO EQUIPO!";
            instructionText.color = new Color(1f, 0.42f, 0.42f);
            finished = true;
        }
    }
}
