using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Timing-based local co-op spinning rope challenge on one continuous floating deck.</summary>
public sealed class JumpRopeGame : MonoBehaviour
{
    private const string HelpText =
        "P1: ESPACIO o W  |  P2: ENTER o flecha ARRIBA\n" +
        "Salten cuando la cuerda se acerque a sus pies.\n" +
        "El equipo necesita 12 saltos a tiempo; 3 fallos eliminan.";

    [SerializeField] private Transform rope;
    [SerializeField] private Transform playerOne;
    [SerializeField] private Transform playerTwo;
    [SerializeField] private Transform[] bots;
    [SerializeField] private TextMesh instructions;
    [SerializeField] private TextMesh timingCue;
    [SerializeField] private int requiredJumps = 12;
    [SerializeField] private int missesAllowed = 3;
    private int jumps;
    private int misses;
    private float timer;
    private float period = 2.6f;
    private bool resolvedThisSpin;
    private float[] jumpUntil = new float[2];
    private float[] baseY = new float[2];
    private float[] botJumpUntil;
    private float[] botBaseY;
    private bool ended;
    private RopePracticeStage stage;

    private void Start()
    {
        stage=gameObject.AddComponent<RopePracticeStage>();
        stage.Build(true,rope);
        if (playerOne != null) baseY[0] = playerOne.position.y;
        if (playerTwo != null) baseY[1] = playerTwo.position.y;
        botJumpUntil = new float[bots == null ? 0 : bots.Length];
        botBaseY = new float[botJumpUntil.Length];
        for (int i = 0; i < botBaseY.Length; i++) if (bots[i] != null) botBaseY[i] = bots[i].position.y;
        UpdateText("Listos: 0/" + requiredJumps + " saltos  ·  Fallos: 0/" + missesAllowed);
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(12,12,132,34),"Volver al menú"))
            UnityEngine.SceneManagement.SceneManager.LoadScene("LaUltimaRonda");
        if(instructions!=null) GUI.Box(new Rect((Screen.width-500)*.5f,Screen.height-100,500,92),instructions.text);
        if(timingCue!=null && !ended) GUI.Box(new Rect((Screen.width-160)*.5f,20,160,32),timingCue.text);
    }

    private void Update()
    {
        if (ended) return;
        Keyboard k = Keyboard.current;
        if (k == null) return;

        float dt = Time.deltaTime;
        period = Mathf.Max(1.45f, 2.6f - jumps * 0.065f);
        timer += dt / period;
        if (timer >= 1f) { timer %= 1f; resolvedThisSpin = false; }
        float phase = timer * period;
        stage.DrawCable(timer,0f);

        bool danger = phase > period * 0.72f && phase < period * 0.88f;
        if (timingCue != null)
        {
            timingCue.text = danger ? "¡SALTA!" : "¡PREPÁRATE!";
            timingCue.color = danger ? new Color(1f, 0.18f, 0.22f) : new Color(1f, 0.82f, 0.32f);
        }
        bool p1Pressed = k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame;
        bool p2Pressed = k.enterKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame;
        if (p1Pressed && danger && jumpUntil[0] < Time.time)
        {
            jumpUntil[0] = Time.time + 0.7f;
            jumps++;
            resolvedThisSpin = true;
            UpdateText("¡P1 a tiempo! Saltos " + Mathf.Min(jumps, requiredJumps) + "/" + requiredJumps + "  ·  Fallos " + misses + "/" + missesAllowed);
        }
        if (p2Pressed && danger && jumpUntil[1] < Time.time)
        {
            jumpUntil[1] = Time.time + 0.7f;
            jumps++;
            resolvedThisSpin = true;
            UpdateText("¡P2 a tiempo! Saltos " + Mathf.Min(jumps, requiredJumps) + "/" + requiredJumps + "  ·  Fallos " + misses + "/" + missesAllowed);
        }

        for (int i = 0; i < botJumpUntil.Length; i++)
        {
            if (bots[i] == null) continue;
            if (danger && botJumpUntil[i] < Time.time && Random.value > 0.12f) botJumpUntil[i] = Time.time + 0.68f;
            float remaining = botJumpUntil[i] - Time.time;
            Vector3 botPos = bots[i].position;
            float height = remaining > 0f ? Mathf.Sin((0.68f - remaining) / 0.68f * Mathf.PI) * 1.5f : 0f;
            bots[i].position = new Vector3(botPos.x, botBaseY[i] + height, botPos.z);
        }

        if (playerOne != null) AnimateJump(playerOne, 0);
        if (playerTwo != null) AnimateJump(playerTwo, 1);

        if (phase > period * 0.9f && !resolvedThisSpin)
        {
            misses++;
            UpdateText("¡La cuerda los alcanzó! Fallos " + misses + "/" + missesAllowed + "  ·  Saltos " + jumps + "/" + requiredJumps);
            if (misses >= missesAllowed) Finish(false);
            resolvedThisSpin = true;
        }

        if (jumps >= requiredJumps) Finish(true);
    }

    private void AnimateJump(Transform player, int index)
    {
        float remaining = jumpUntil[index] - Time.time;
        float height = remaining > 0f ? Mathf.Sin((0.7f - remaining) / 0.7f * Mathf.PI) * 2f : 0f;
        Vector3 pos = player.position;
        player.position = new Vector3(pos.x, baseY[index] + height, pos.z);
    }

    private void Finish(bool won)
    {
        ended = true;
        UpdateText(won ? "¡GANARON! Completaron los saltos." : "FIN DE LA PARTIDA. La cuerda alcanzó al equipo.");
    }

    private void UpdateText(string value)
    {
        if (instructions != null) instructions.text = HelpText + "\n" + value;
    }
}
