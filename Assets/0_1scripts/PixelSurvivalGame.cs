using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>A small, self-contained pixel survival game with local co-op and bot rivals.</summary>
public sealed class PixelSurvivalGame : MonoBehaviour
{
    private enum Screen { Title, Kidnap, Round, Result, Insider, Betrayal, Ending, GameOver }
    private enum Trial { RedLight, Tug, Bridge, Final }

    private static readonly string[] TrialNames =
    {
        "LUZ ROJA, LUZ VERDE", "TIRA Y AFLOJA", "EL PUENTE DE CRISTAL", "EL JUEGO FINAL"
    };
    private static readonly string[] StoryCards =
    {
        "Una invitación misteriosa. Una última oportunidad.",
        "El vehículo se detiene. Los guardias te llevan a una arena desconocida.",
        "Despiertas junto a otros participantes. Los guardias anuncian la primera prueba.",
        "La muñeca se gira. La luz verde está por comenzar."
    };
    [SerializeField] private int rivalBots = 11;
    private Screen screen = Screen.Title;
    private int humanCount = 1;
    private int trialIndex;
    private float phaseTime;
    private float trialTime;
    private int redStrikes1;
    private int redStrikes2;
    private float[] runner = new float[2];
    private int[] botStrikes = new int[11];
    private bool[] botRunnerAlive = new bool[11];
    private float tugProgress;
    private int[] bridgeStep = new int[2];
    private int[] bridgeMistakes = new int[2];
    private bool[,] safeBridge = new bool[8, 2];
    private Vector2[] finalPosition = new Vector2[2];
    private Vector2[] botPosition = new Vector2[11];
    private bool[] playerAlive = new bool[2];
    private bool[] playerWon = new bool[2];
    private bool lightsGreen = true;
    private float lightClock;
    private bool insiderTrusted;
    private bool insiderRouteRevealed;
    private bool insiderBetrayed;
    private string resultMessage = "";
    private Texture2D pixel;
    private Texture2D playerIdleSprite;
    private Texture2D[] playerWalkSprites;
    private Texture2D guardIdleSprite;
    private Texture2D guardArmedSprite;
    private float[] jumpTimer = new float[2];
    private float[] playerDeathTime = new float[2];
    private bool[] playerMoving = new bool[2];
    private int shotTarget = -1;
    private int shotBotTarget = -1;
    private int killedPlayer = -1;
    private float shotTimer;
    private Vector2 shotStart;
    private Vector2 shotEnd;
    private GUIStyle titleStyle, bodyStyle, buttonStyle, smallStyle;
    private float uiScale = 1f;
    private CampaignStage25D stage25D;

    private void Awake()
    {
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
        pixel.filterMode = FilterMode.Point;
        playerIdleSprite = LoadCharacter("Player_Idle_A");
        playerWalkSprites = new[] { LoadCharacter("Player_Walk_A"), LoadCharacter("Player_Walk_B") };
        guardIdleSprite = LoadCharacter("Guard_Idle");
        guardArmedSprite = LoadCharacter("Guard_Armed");
        stage25D = GetComponent<CampaignStage25D>();
        if (stage25D == null) stage25D = gameObject.AddComponent<CampaignStage25D>();
    }

    private void OnDestroy()
    {
        if (pixel != null) Destroy(pixel);
    }

    private void LateUpdate()
    {
        if (stage25D == null) return;
        float stageProgress = trialIndex == (int)Trial.RedLight ? runner[0] : trialIndex == (int)Trial.Bridge ? bridgeStep[0] : finalPosition[0].x;
        float stageProgress2 = trialIndex == (int)Trial.RedLight ? runner[1] : trialIndex == (int)Trial.Bridge ? bridgeStep[1] : finalPosition[1].x;
        Vector2 visualPlayerOne = trialIndex == (int)Trial.RedLight ? new Vector2(runner[0], 0f) : finalPosition[0];
        Vector2 visualPlayerTwo = trialIndex == (int)Trial.RedLight ? new Vector2(runner[1], 0f) : finalPosition[1];
        int shotVictim = shotTarget >= 0 ? shotTarget : shotBotTarget >= 0 ? shotBotTarget + 2 : -1;
        stage25D.Present((int)screen, trialIndex, humanCount, phaseTime, stageProgress, stageProgress2, lightsGreen,
            shotVictim, jumpTimer[0], jumpTimer[1], playerMoving[0], playerMoving[1], visualPlayerOne, visualPlayerTwo, botPosition,
            playerAlive, botRunnerAlive, insiderTrusted, insiderBetrayed);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (screen == Screen.Kidnap)
        {
            phaseTime += dt;
            Keyboard cutsceneKeys = Keyboard.current;
            if (phaseTime >= 12f || (cutsceneKeys != null && cutsceneKeys.spaceKey.wasPressedThisFrame)) BeginTrial(0);
            return;
        }
        if (screen == Screen.Betrayal)
        {
            phaseTime += dt;
            if (phaseTime >= 6f) BeginTrial((int)Trial.Final);
            return;
        }
        if (screen == Screen.GameOver)
        {
            if (killedPlayer >= 0) phaseTime += dt;
            return;
        }
        if (screen != Screen.Round) return;

        trialTime += dt;
        Keyboard keys = Keyboard.current;
        if (keys == null) return;
        switch ((Trial)trialIndex)
        {
            case Trial.RedLight: UpdateRedLight(keys, dt); break;
            case Trial.Tug: UpdateTug(keys, dt); break;
            case Trial.Bridge: UpdateBridge(keys); break;
            case Trial.Final: UpdateFinal(keys, dt); break;
        }
    }

    private void UpdateRedLight(Keyboard k, float dt)
    {
        playerMoving[0] = playerMoving[1] = false;
        if (shotTarget >= 0 || shotBotTarget >= 0)
        {
            shotTimer += dt;
            if (shotTimer >= 0.42f)
            {
                if (shotTarget >= 0)
                {
                    killedPlayer = shotTarget;
                    playerAlive[shotTarget] = false;
                    playerDeathTime[shotTarget] = Time.time;
                    shotTarget = -1;
                    CompleteTrial(false, "Un guardia te vio moverte y te eliminó.");
                }
                else
                {
                    botRunnerAlive[shotBotTarget] = false;
                    shotBotTarget = -1;
                    shotTimer = 0f;
                }
            }
            return;
        }
        lightClock += dt;
        if (lightClock >= (lightsGreen ? 3.2f : 2.4f)) { lightClock = 0f; lightsGreen = !lightsGreen; }
        MoveRunner(k, 0, Key.W, Key.A, Key.S, Key.D, dt);
        if (humanCount == 2) MoveRunner(k, 1, Key.UpArrow, Key.LeftArrow, Key.DownArrow, Key.RightArrow, dt);
        for (int i = 0; i < botRunnerAlive.Length; i++)
        {
            if (!botRunnerAlive[i]) continue;
            if (lightsGreen && lightClock < 2.35f)
                botPosition[i].x = Mathf.Min(760f, botPosition[i].x + (68f + (i % 4) * 6f) * dt);
            else if (!lightsGreen && Random.value < 0.00035f * (i % 3 + 1))
            {
                botStrikes[i]++;
                if (botStrikes[i] >= 3)
                {
                    if (shotTarget < 0 && shotBotTarget < 0)
                    {
                        shotBotTarget = i;
                        shotTimer = 0f;
                        shotEnd = new Vector2(62f + botPosition[i].x, 415f - (i + 2) * 25f);
                    }
                    else botRunnerAlive[i] = false;
                }
            }
        }
        if (runner[0] >= 760f) playerWon[0] = true;
        if (humanCount == 2 && runner[1] >= 760f) playerWon[1] = true;
        if (playerWon[0] && (humanCount == 1 || playerWon[1])) CompleteTrial(true, "¡Llegaron a la meta!");
        else if (!playerAlive[0] || (humanCount == 2 && !playerAlive[1]))
            CompleteTrial(false, "Un guardia te vio moverte y te eliminó.");
        else if (trialTime > 48f) CompleteTrial(playerWon[0] || (humanCount == 2 && playerWon[1]), "Se acabó el tiempo.");
    }

    private void MoveRunner(Keyboard k, int p, Key up, Key left, Key down, Key right, float dt)
    {
        float axis = ((k[right].isPressed || k[up].isPressed) ? 1f : 0f)
                   - ((k[left].isPressed || k[down].isPressed) ? 1f : 0f);
        if (Mathf.Abs(axis) < 0.01f) return;
        playerMoving[p] = true;
        if (!lightsGreen)
        {
            bool movedThisFrame = Pressed(k, up) || Pressed(k, left) || Pressed(k, down) || Pressed(k, right);
            if (movedThisFrame)
            {
                if (p == 0) redStrikes1++; else redStrikes2++;
                int strikes = p == 0 ? redStrikes1 : redStrikes2;
                if (strikes >= 3 && shotTarget < 0) StartGuardShot(p);
            }
            return;
        }
        runner[p] = Mathf.Clamp(runner[p] + axis * 96f * dt, 0f, 760f);
    }

    private void UpdateTug(Keyboard k, float dt)
    {
        int pulls = 0;
        if (Pressed(k, Key.Space) || Pressed(k, Key.A)) pulls++;
        if (humanCount == 2 && (Pressed(k, Key.Enter) || Pressed(k, Key.RightArrow))) pulls++;
        tugProgress += pulls * 3.1f;
        tugProgress -= (0.52f + Mathf.Min(0.4f, trialTime * 0.005f)) * dt;
        tugProgress = Mathf.Clamp(tugProgress, 0f, 100f);
        if (tugProgress >= 100f) CompleteTrial(true, "¡Tu equipo ganó el tira y afloja!");
        else if (trialTime >= 32f) CompleteTrial(tugProgress >= 46f, tugProgress >= 46f ? "El equipo aguantó unido." : "El equipo rival ganó terreno.");
    }

    private void UpdateBridge(Keyboard k)
    {
        if (Pressed(k, Key.A) || (humanCount == 1 && Pressed(k, Key.LeftArrow))) ChooseBridge(0, false);
        if (Pressed(k, Key.D) || (humanCount == 1 && Pressed(k, Key.RightArrow))) ChooseBridge(0, true);
        if (humanCount == 2)
        {
            if (Pressed(k, Key.LeftArrow)) ChooseBridge(1, false);
            if (Pressed(k, Key.RightArrow)) ChooseBridge(1, true);
        }
        for (int i = 0; i < 3; i++) bridgeStep[2 + i] = Mathf.Min(8, Mathf.FloorToInt(trialTime * (0.13f + i * 0.025f)));
        if (bridgeStep[0] >= 8 && (humanCount == 1 || bridgeStep[1] >= 8)) CompleteTrial(true, "Cruzaron el puente.");
        else if (bridgeMistakes[0] >= 3 || (humanCount == 2 && bridgeMistakes[1] >= 3)) CompleteTrial(false, "Demasiados paneles se rompieron.");
        else if (trialTime > 65f) CompleteTrial(false, "El puente se cerró.");
    }

    private void ChooseBridge(int p, bool right)
    {
        if (p >= humanCount || bridgeStep[p] >= 8) return;
        if (safeBridge[bridgeStep[p], right ? 1 : 0]) bridgeStep[p]++;
        else bridgeMistakes[p]++;
    }

    private void UpdateFinal(Keyboard k, float dt)
    {
        playerMoving[0] = playerMoving[1] = false;
        MoveFinal(k, 0, Key.W, Key.A, Key.S, Key.D, dt);
        if (humanCount == 2) MoveFinal(k, 1, Key.UpArrow, Key.LeftArrow, Key.DownArrow, Key.RightArrow, dt);
        if (Pressed(k, Key.Space)) jumpTimer[0] = 0.58f;
        if (humanCount == 2 && Pressed(k, Key.Enter)) jumpTimer[1] = 0.58f;
        for (int p = 0; p < jumpTimer.Length; p++) jumpTimer[p] = Mathf.Max(0f, jumpTimer[p] - dt);
        for (int i = 0; i < botPosition.Length; i++)
        {
            int p = i % humanCount;
            Vector2 target = finalPosition[p];
            Vector2 direction = (target - botPosition[i]).normalized;
            float betrayalBoost = insiderBetrayed ? 1.45f : 1f;
            botPosition[i] += direction * (18f + (i % 3) * 3f) * betrayalBoost * dt;
        }
        for (int p = 0; p < humanCount; p++)
        {
            if (finalPosition[p].x >= 405f) playerWon[p] = true;
            for (int b = 0; b < botPosition.Length; b++)
                if (Vector2.Distance(finalPosition[p], botPosition[b]) < 22f)
                {
                    playerAlive[p] = false;
                    killedPlayer = p;
                    playerDeathTime[p] = Time.time;
                }
        }
        bool allWon = playerWon[0] && (humanCount == 1 || playerWon[1]);
        bool allLost = !playerAlive[0] && (humanCount == 1 || !playerAlive[1]);
        if (allWon) CompleteTrial(true, "¡Ganaste el juego final!");
        else if (allLost) CompleteTrial(false, "Los rivales llegaron primero.");
        else if (trialTime > 50f) CompleteTrial(playerWon[0] || (humanCount == 2 && playerWon[1]), "Terminó la última ronda.");
    }

    private void MoveFinal(Keyboard k, int p, Key up, Key left, Key down, Key right, float dt)
    {
        if (!playerAlive[p] || playerWon[p]) return;
        Vector2 move = new Vector2((k[right].isPressed ? 1f : 0f) - (k[left].isPressed ? 1f : 0f),
            (k[up].isPressed ? 1f : 0f) - (k[down].isPressed ? 1f : 0f));
        if (move.sqrMagnitude > 1f) move.Normalize();
        playerMoving[p] = move.sqrMagnitude > 0.01f;
        finalPosition[p] = Vector2.ClampMagnitude(finalPosition[p] + move * 125f * dt, 460f);
        finalPosition[p] = new Vector2(Mathf.Clamp(finalPosition[p].x, -440f, 430f), Mathf.Clamp(finalPosition[p].y, -175f, 175f));
    }

    private void StartKidnap()
    {
        screen = Screen.Kidnap;
        phaseTime = 0f;
    }

    private void StartGuardShot(int player)
    {
        shotTarget = player;
        shotBotTarget = -1;
        shotTimer = 0f;
        shotStart = new Vector2(824f, 352f);
        shotEnd = new Vector2(62f + runner[player], 415f - player * 25f);
    }

    private void BeginTrial(int index)
    {
        trialIndex = index;
        screen = Screen.Round;
        trialTime = 0f;
        phaseTime = 0f;
        runner = new float[2];
        botPosition = new Vector2[rivalBots];
        botStrikes = new int[rivalBots];
        botRunnerAlive = new bool[rivalBots];
        for (int i = 0; i < rivalBots; i++) botRunnerAlive[i] = true;
        redStrikes1 = redStrikes2 = 0;
        shotTarget = shotBotTarget = killedPlayer = -1;
        shotTimer = 0f;
        jumpTimer = new float[2];
        playerDeathTime = new float[2];
        playerMoving = new bool[2];
        tugProgress = 15f;
        bridgeStep = new int[5]; bridgeMistakes = new int[2];
        for (int i = 0; i < 8; i++)
        {
            bool safeRight = insiderTrusted ? (i == 0 || i == 3 || i == 5 || i == 6) : Random.value > 0.5f;
            safeBridge[i, 0] = !safeRight;
            safeBridge[i, 1] = safeRight;
        }
        insiderRouteRevealed = index == (int)Trial.Bridge && insiderTrusted;
        finalPosition = new[] { new Vector2(-420f, 0f), new Vector2(-420f, -45f) };
        botPosition = new Vector2[rivalBots];
        for (int i = 0; i < botPosition.Length; i++)
        {
            float x = trialIndex == (int)Trial.Final ? 25f + (i % 4) * 90f : Random.Range(0f, 6f);
            float y = trialIndex == (int)Trial.Final ? -130f + (i / 4) * 82f : 0f;
            botPosition[i] = new Vector2(x, y);
        }
        playerAlive = new[] { true, true };
        playerWon = new bool[2];
        lightsGreen = true; lightClock = 0f;
    }

    private void CompleteTrial(bool success, string message)
    {
        if (screen != Screen.Round) return;
        resultMessage = message;
        screen = success
            ? trialIndex == (int)Trial.Final ? Screen.Ending
            : trialIndex == (int)Trial.Tug ? Screen.Insider
            : trialIndex == (int)Trial.Bridge ? Screen.Betrayal
            : Screen.Result
            : Screen.GameOver;
        if (screen == Screen.Betrayal)
        {
            insiderBetrayed = insiderTrusted;
            phaseTime = 0f;
        }
    }

    private void Advance()
    {
        if (trialIndex + 1 >= TrialNames.Length) { screen = Screen.Ending; return; }
        BeginTrial(trialIndex + 1);
    }

    private void StartGame(int players)
    {
        humanCount = players;
        insiderTrusted = false;
        insiderRouteRevealed = false;
        insiderBetrayed = false;
        StartKidnap();
    }

    private void ResolveInsiderChoice(bool trust)
    {
        insiderTrusted = trust;
        resultMessage = trust
            ? "Nerea revela los paneles seguros del puente y te ayudará a descubrir a los guardias."
            : "Nerea se marcha. No sabes si era una aliada o si formaba parte de otra trampa.";
        screen = Screen.Result;
    }

    private void SetupStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 34, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.82f, 0.36f) } };
        bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, wordWrap = true, normal = { textColor = Color.white } };
        smallStyle = new GUIStyle(bodyStyle) { fontSize = 16 };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, hover = { textColor = Color.yellow } };
    }

    private void OnGUI()
    {
        if (pixel == null) return;
        if (titleStyle == null) SetupStyles();
        uiScale = Mathf.Min(UnityEngine.Screen.width / 960f, UnityEngine.Screen.height / 540f);
        float ox = (UnityEngine.Screen.width - 960f * uiScale) * 0.5f, oy = (UnityEngine.Screen.height - 540f * uiScale) * 0.5f;
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousBackgroundColor = GUI.backgroundColor;
        GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0f), Quaternion.identity, Vector3.one * uiScale);
        GUI.backgroundColor = new Color(0.78f, 0.16f, 0.34f);
        if (screen == Screen.Title) DrawTitle();
        else if (screen == Screen.Kidnap) DrawKidnap();
        else if (screen == Screen.Round) DrawRound();
        else if (screen == Screen.Result) DrawResult();
        else if (screen == Screen.Insider) DrawInsider();
        else if (screen == Screen.Betrayal) DrawBetrayal();
        else if (screen == Screen.GameOver) DrawGameOver();
        else DrawEnding();
        GUI.backgroundColor = previousBackgroundColor;
        GUI.matrix = previousMatrix;
    }

    private void DrawTitle()
    {
        Fill(new Rect(0, 0, 960, 540), new Color(0.035f, 0.045f, 0.09f, 0.24f));
        Fill(new Rect(80, 78, 800, 390), new Color(0.07f, 0.09f, 0.16f, 0.72f));
        Fill(new Rect(0, 48, 960, 8), new Color(0.92f, 0.18f, 0.39f));
        Label(new Rect(100, 105, 760, 65), "LA ÚLTIMA RONDA", titleStyle);
        Label(new Rect(160, 175, 640, 65), "Aventura 2.5D de supervivencia en pixel art", bodyStyle);
        Label(new Rect(170, 270, 620, 35), "Elige cuántos jugadores participarán", smallStyle);
        if (GUI.Button(new Rect(270, 320, 180, 65), "1 JUGADOR", buttonStyle)) StartGame(1);
        if (GUI.Button(new Rect(510, 320, 180, 65), "2 JUGADORES", buttonStyle)) StartGame(2);
        Label(new Rect(120, 395, 720, 32), "P1: WASD  ·  P2: FLECHAS", smallStyle);
        Label(new Rect(120, 427, 720, 32), "Cada prueba explica sus reglas y controles.", smallStyle);
        Label(new Rect(100, 485, 760, 30), "Historia original inspirada en juegos de supervivencia", smallStyle);
    }

    private void DrawKidnap()
    {
        Fill(new Rect(0, 0, 960, 540), new Color(0.025f, 0.035f, 0.07f, 0.2f));
        Fill(new Rect(70, 65, 820, 108), new Color(0f, 0f, 0f, 0.82f));
        string card = phaseTime < 3f ? StoryCards[0] : phaseTime < 6f ? StoryCards[1] : phaseTime < 9f ? StoryCards[2] : StoryCards[3];
        Label(new Rect(100, 78, 760, 78), card, bodyStyle);
        Label(new Rect(350, 475, 260, 28), "CINEMÁTICA  ·  " + Mathf.CeilToInt(12f - phaseTime), smallStyle);
    }

    private void DrawRound()
    {
        DrawBackdrop();
        Label(new Rect(35, 15, 890, 45), TrialNames[trialIndex], titleStyle);
        Label(new Rect(15, 65, 930, 32), "PRUEBA " + (trialIndex + 1) + "/" + TrialNames.Length + "     RIVALES RESTANTES: " + RemainingRivals(), smallStyle);
        switch ((Trial)trialIndex)
        {
            case Trial.RedLight: DrawRedLight(); break;
            case Trial.Tug: DrawTug(); break;
            case Trial.Bridge: DrawBridge(); break;
            case Trial.Final: DrawFinal(); break;
        }
        if (trialIndex == (int)Trial.Bridge && insiderRouteRevealed) DrawBridgeHint();
        Label(new Rect(25, 500, 910, 24), "P1: WASD  ·  P2: FLECHAS  ·  Consulta las reglas de la prueba", smallStyle);
    }

    private void DrawBackdrop()
    {
        // The camera renders the current 3D arena; OnGUI is reserved for instructions and controls.
    }

    private void DrawRedLight()
    {
        Fill(new Rect(55, 140, 850, 82), new Color(0.12f, 0.17f, 0.24f, 0.72f));
        Label(new Rect(35, 144, 890, 38), "Con luz verde, avanza con WASD o las flechas. Con luz roja, detente.", smallStyle);
        Label(new Rect(35, 186, 890, 32), "Si te mueves 3 veces en rojo, el guardia te eliminará. P1: WASD  ·  P2: FLECHAS", smallStyle);
        Label(new Rect(340, 98, 280, 38), lightsGreen ? "¡LUZ VERDE!" : "¡LUZ ROJA!", titleStyle);
        Label(new Rect(30, 460, 450, 30), "Faltas P1: " + redStrikes1 + "/3" + (humanCount == 2 ? "   P2: " + redStrikes2 + "/3" : ""), smallStyle);
        Fill(new Rect(700, 462, 220, 16), new Color(0.04f, 0.07f, 0.1f, 0.82f));
        Fill(new Rect(704, 466, 212f * Mathf.Clamp01(runner[0] / 760f), 8), new Color(0.35f, 0.86f, 0.69f));
    }

    private void DrawTug()
    {
        Fill(new Rect(150, 435, 660, 28), new Color(0.07f, 0.08f, 0.12f));
        Fill(new Rect(154, 439, tugProgress * 6.52f, 20), new Color(0.24f, 0.77f, 0.56f));
        Fill(new Rect(155, 135, 650, 104), new Color(0.09f, 0.12f, 0.19f, 0.72f));
        Label(new Rect(125, 143, 710, 42), "¡Pulsa para tirar! Cada pulsación aumenta la fuerza de tu equipo.", bodyStyle);
        Label(new Rect(175, 185, 610, 46), humanCount == 1
            ? "P1: ESPACIO o A  ·  Gana con 100% de fuerza o si terminas con 46% o más."
            : "P1: ESPACIO/A  ·  P2: ENTER/→  ·  Gana con 100% o termina con 46% o más.", smallStyle);
        Label(new Rect(340, 465, 280, 25), "FUERZA " + Mathf.RoundToInt(tugProgress) + "%", smallStyle);
    }

    private void DrawBridge()
    {
        Fill(new Rect(150, 115, 660, 56), new Color(0.035f, 0.055f, 0.12f, 0.7f));
        Label(new Rect(160, 120, 640, 46), humanCount == 1
            ? "Elige un panel por paso: A/← izquierda o D/→ derecha.\nTres errores te eliminan. Sigue la pista de Nerea si la tienes."
            : "Elijan un panel por paso. P1: A/D  ·  P2: ←/→\nTres errores eliminan al jugador. Usen la pista de Nerea.", smallStyle);
        Label(new Rect(180, 179, 600, 30), "P1 paso " + bridgeStep[0] + "/8  ·  errores " + bridgeMistakes[0] + "/3" + (humanCount == 2 ? "   P2 paso " + bridgeStep[1] + "/8  ·  errores " + bridgeMistakes[1] + "/3" : ""), smallStyle);
    }

    private void DrawBridgeHint()
    {
        string route = "PISTA DE NEREA: ";
        for (int i = 0; i < 8; i++) route += safeBridge[i, 1] ? "D  " : "I  ";
        Label(new Rect(250, 218, 460, 28), route, smallStyle);
    }

    private void DrawFinal()
    {
        Fill(new Rect(770, 210, 160, 12), new Color(0.04f, 0.07f, 0.1f, 0.82f));
        Fill(new Rect(774, 214, 152f * Mathf.Clamp01((finalPosition[0].x + 440f) / 845f), 4), C.yellow);
        Label(new Rect(110, 130, 740, 52), "Llega al otro lado y evita a los rivales", bodyStyle);
        Label(new Rect(150, 190, 660, 35), humanCount == 1 ? "P1 WASD  ·  ESPACIO para saltar" : "P1 WASD/ESPACIO  ·  P2 FLECHAS/ENTER para saltar", smallStyle);
        Label(new Rect(150, 425, 660, 28), "META →", smallStyle);
    }

    private void DrawResult()
    {
        DrawBackdrop();
        Fill(new Rect(120, 120, 720, 310), new Color(0.03f, 0.05f, 0.1f, 0.78f));
        Label(new Rect(170, 155, 620, 55), "PRUEBA SUPERADA", titleStyle);
        Label(new Rect(190, 225, 580, 60), resultMessage, bodyStyle);
        Label(new Rect(170, 300, 620, 42), "Rivales restantes: " + Mathf.Max(1, rivalBots - 1 - trialIndex), smallStyle);
        if (GUI.Button(new Rect(350, 365, 260, 55), "SIGUIENTE PRUEBA", buttonStyle)) Advance();
    }

    private void DrawInsider()
    {
        Fill(new Rect(0, 0, 960, 540), new Color(0.035f, 0.045f, 0.08f, 0.28f));
        Fill(new Rect(75, 70, 810, 400), new Color(0.12f, 0.15f, 0.22f, 0.72f));
        Fill(new Rect(105, 100, 750, 340), new Color(0.06f, 0.08f, 0.13f, 0.64f));
        Label(new Rect(165, 115, 630, 52), "EL GUARDIA ENMASCARADO", titleStyle);
        Label(new Rect(175, 190, 610, 95), "Soy Nerea. Estoy infiltrada entre los guardias para sacar pruebas.\nPuedo revelarte qué paneles del puente son seguros.", bodyStyle);
        Label(new Rect(170, 300, 620, 42), "¿Confías en ella?", bodyStyle);
        if (GUI.Button(new Rect(220, 365, 220, 60), "CONFIAR", buttonStyle)) ResolveInsiderChoice(true);
        if (GUI.Button(new Rect(520, 365, 220, 60), "NO CONFIAR", buttonStyle)) ResolveInsiderChoice(false);
        Label(new Rect(180, 440, 600, 26), "Tu decisión cambia la siguiente prueba.", smallStyle);
    }

    private void DrawBetrayal()
    {
        Fill(new Rect(0, 0, 960, 540), new Color(0.025f, 0.025f, 0.06f, 0.22f));
        Fill(new Rect(0, 365, 960, 175), new Color(0.11f, 0.13f, 0.19f, 0.5f));
        Fill(new Rect(70, 70, 820, 116), new Color(0f, 0f, 0f, 0.72f));
        string text = insiderBetrayed
            ? "Nerea abre una puerta para ustedes y activa la alarma. Era informante de los organizadores. ¡Los bots vienen más rápido!"
            : "La infiltrada se queda atrás para distraer a los guardias. La puerta del juego final se abre.";
        Label(new Rect(110, 84, 740, 90), text, bodyStyle);
        Label(new Rect(335, 475, 290, 28), "SIGUIENTE PRUEBA EN " + Mathf.CeilToInt(6f - phaseTime), smallStyle);
    }

    private void DrawGameOver()
    {
        Fill(new Rect(0, 0, 960, 540), new Color(0.04f, 0.025f, 0.06f, 0.25f));
        Label(new Rect(100, 115, 760, 65), "FIN DE LA PARTIDA", titleStyle);
        Label(new Rect(160, 215, 640, 75), resultMessage, bodyStyle);
        Label(new Rect(150, 305, 660, 45), "Llegaron hasta: " + TrialNames[Mathf.Clamp(trialIndex, 0, TrialNames.Length - 1)], smallStyle);
        if (GUI.Button(new Rect(365, 375, 230, 55), "VOLVER AL MENÚ", buttonStyle)) screen = Screen.Title;
    }

    private void DrawEnding()
    {
        Fill(new Rect(0, 0, 960, 540), new Color(0.035f, 0.06f, 0.11f, 0.25f));
        Fill(new Rect(0, 405, 960, 135), new Color(0.12f, 0.2f, 0.2f, 0.48f));
        Label(new Rect(100, 110, 760, 68), "SOBREVIVISTE", titleStyle);
        Label(new Rect(160, 215, 640, 72), "El juego terminó. La decisión de qué hacer ahora es tuya.", bodyStyle);
        if (GUI.Button(new Rect(365, 345, 230, 60), "JUGAR DE NUEVO", buttonStyle)) screen = Screen.Title;
    }

    private void DrawTrackRunner(float x, int lane, Color color)
    {
        Vector2 pos = new Vector2(62 + Mathf.Clamp(x, 0f, 820f), 415 - lane * 25);
        if (lane == 0 || (humanCount == 2 && lane == 1))
        {
            int player = lane;
            bool moving = Keyboard.current != null && (player == 0
                ? Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed || Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed
                : Keyboard.current.upArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed);
            DrawPlayerSprite(pos, 0.68f, moving, 0f);
        }
        else DrawPlayerSprite(pos, 0.6f, ((lane + Mathf.FloorToInt(Time.time * 8f)) & 1) == 0, 0f);
    }

    private void DrawFinalPlayer(Vector2 pos, Color color)
    {
        int player = color == C.cyan ? 0 : color == C.yellow ? 1 : -1;
        Vector2 center = new Vector2(480 + pos.x, 350 - pos.y * 0.42f);
        if (player >= 0)
        {
            float jump = jumpTimer[player] > 0f ? Mathf.Sin((1f - jumpTimer[player] / 0.58f) * Mathf.PI) : 0f;
            if (!playerAlive[player])
                DrawPlayerSprite(center, 0.72f, false, Mathf.Lerp(0f, 88f, Mathf.Clamp01((Time.time - playerDeathTime[player]) / 0.45f)));
            else if (jump > 0.05f)
                DrawPlayerSprite(center + Vector2.up * (jump * 34f), 0.82f + jump * 0.12f, false, 0f);
            else DrawPlayerSprite(center, 0.82f, playerMoving[player], 0f);
        }
        else DrawPlayerSprite(center, 0.72f, true, 0f);
    }

    private void PixelDoll()
    {
        PixelChar(new Vector2(480, 315), new Color(1f, 0.56f, 0.2f), 1.5f);
        Fill(new Rect(lightsGreen ? 464 : 475, 275, 32, 8), Color.black);
        Fill(new Rect(lightsGreen ? 470 : 488, 277, 7, 4), new Color(0.95f, 0.1f, 0.18f));
    }

    private void PixelTree(float x, float y, float scale)
    {
        Fill(new Rect(x + 22 * scale, y + 35 * scale, 14 * scale, 60 * scale), new Color(0.37f, 0.23f, 0.15f));
        Fill(new Rect(x, y, 58 * scale, 48 * scale), new Color(0.16f, 0.37f, 0.2f));
        Fill(new Rect(x + 9 * scale, y - 13 * scale, 40 * scale, 14 * scale), new Color(0.22f, 0.45f, 0.24f));
    }

    private void PixelChar(Vector2 center, Color shirt, float scale)
    {
        float s = Mathf.Max(2f, 12f * scale);
        Fill(new Rect(center.x - 8 * scale, center.y - 6 * scale, 16 * scale, 15 * scale), new Color(0.88f, 0.65f, 0.48f));
        Fill(new Rect(center.x - 10 * scale, center.y + 9 * scale, 20 * scale, 28 * scale), shirt);
        Fill(new Rect(center.x - 10 * scale, center.y + 35 * scale, 8 * scale, 20 * scale), new Color(0.12f, 0.15f, 0.22f));
        Fill(new Rect(center.x + 2 * scale, center.y + 35 * scale, 8 * scale, 20 * scale), new Color(0.12f, 0.15f, 0.22f));
        Fill(new Rect(center.x - 11 * scale, center.y - 9 * scale, 22 * scale, 6 * scale), new Color(0.16f, 0.1f, 0.09f));
        Fill(new Rect(center.x - 5 * scale, center.y - 1 * scale, 3 * scale, 3 * scale), Color.black);
        Fill(new Rect(center.x + 2 * scale, center.y - 1 * scale, 3 * scale, 3 * scale), Color.black);
    }

    private static Texture2D LoadCharacter(string assetName)
    {
        Texture2D texture = Resources.Load<Texture2D>("Characters/" + assetName);
        if (texture != null) { texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp; }
        return texture;
    }

    private void DrawPlayerSprite(Vector2 center, float scale, bool walking, float rotation)
    {
        if (playerIdleSprite == null) playerIdleSprite = LoadCharacter("Player_Idle_A");
        if (playerWalkSprites == null || playerWalkSprites.Length == 0)
            playerWalkSprites = new[] { LoadCharacter("Player_Walk_A"), LoadCharacter("Player_Walk_B") };
        Texture2D texture = walking && playerWalkSprites != null
            ? playerWalkSprites[Mathf.FloorToInt(Time.time * 10f) % playerWalkSprites.Length]
            : playerIdleSprite;
        if (texture == null)
        {
            PixelChar(center, new Color(0.015f, 0.34f, 0.24f), scale);
            return;
        }
        if (!walking) center.y += Mathf.Sin(Time.time * 2.6f + center.x * 0.015f) * 1.2f;
        DrawCharacter(texture, center, new Vector2(52f * scale, 54f * scale), Color.white, rotation);
    }

    private void DrawGuardSprite(Vector2 center, float scale, bool armed)
    {
        if (guardIdleSprite == null) guardIdleSprite = LoadCharacter("Guard_Idle");
        if (guardArmedSprite == null) guardArmedSprite = LoadCharacter("Guard_Armed");
        Texture2D texture = armed && guardArmedSprite != null ? guardArmedSprite : guardIdleSprite;
        if (texture == null) PixelChar(center, new Color(0.82f, 0.04f, 0.04f), scale);
        else DrawCharacter(texture, center, new Vector2(58f * scale, 54f * scale), Color.white, 0f);
    }

    private void DrawCharacter(Texture2D texture, Vector2 center, Vector2 size, Color tint, float rotation)
    {
        if (texture == null) return;
        Matrix4x4 previous = GUI.matrix;
        if (Mathf.Abs(rotation) > 0.01f) GUIUtility.RotateAroundPivot(rotation, center);
        GUI.color = tint;
        GUI.DrawTexture(new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y), texture, ScaleMode.ScaleToFit, true);
        GUI.color = Color.white;
        GUI.matrix = previous;
    }

    private void DrawBulletShot()
    {
        float progress = Mathf.Clamp01(shotTimer / 0.42f);
        Vector2 bullet = Vector2.Lerp(shotStart, shotEnd, progress);
        Vector2 segment = bullet - shotStart;
        float angle = Mathf.Atan2(segment.y, segment.x) * Mathf.Rad2Deg;
        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, shotStart);
        Fill(new Rect(shotStart.x, shotStart.y - 1.5f, segment.magnitude, 3f), new Color(1f, 0.82f, 0.34f, 0.9f));
        GUI.matrix = previous;
        Fill(new Rect(bullet.x - 5f, bullet.y - 3f, 10f, 6f), new Color(1f, 0.96f, 0.62f));
        if (shotTimer < 0.1f) Fill(new Rect(shotStart.x - 3f, shotStart.y - 8f, 12f, 16f), new Color(1f, 0.76f, 0.22f, 0.95f));
    }

    private void Fill(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = Color.white;
    }

    private void Label(Rect rect, string text, GUIStyle style)
    {
        GUI.Label(rect, text, style);
    }

    private static bool Pressed(Keyboard k, Key key) => k[key].wasPressedThisFrame;

    private int RemainingRivals()
    {
        if (trialIndex != (int)Trial.RedLight || botRunnerAlive == null) return rivalBots;
        int remaining = 0;
        for (int i = 0; i < botRunnerAlive.Length; i++) if (botRunnerAlive[i]) remaining++;
        return remaining;
    }

    private static class C
    {
        public static readonly Color cyan = new Color(0.22f, 0.82f, 0.94f);
        public static readonly Color pink = new Color(0.93f, 0.22f, 0.43f);
        public static readonly Color yellow = new Color(0.97f, 0.77f, 0.28f);
    }
}
