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

    private void Update()
    {
        if (finished) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            progress += pullPerTap;

        progress -= opponentPullPerSecond * Time.deltaTime;
        progress = Mathf.Clamp(progress, -winDistance, winDistance);
        if (centerMarker != null)
            centerMarker.position = new Vector3(progress, centerMarker.position.y, centerMarker.position.z);

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
