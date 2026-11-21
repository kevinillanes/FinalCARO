using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>A small, self-contained pixel survival game with local co-op and bot rivals.</summary>
public sealed partial class PixelSurvivalGame : MonoBehaviour
{
    private enum Screen { Title, Kidnap, Round, Result, Insider, Betrayal, Ending, GameOver, Vote, Interlude, Exit, Welcome, Practice, Victory }
    internal enum Trial { RedLight, Cookie, Carousel, Night, Tug, Bridge, Rope, Final }

    private static readonly string[] TrialNames =
    {
        "LUZ ROJA, LUZ VERDE", "LA GALLETA", "EL CARRUSEL", "NOCHE EN LAS LITERAS", "TIRA Y AFLOJA", "EL PUENTE DE CRISTAL", "LA CUERDA GIRATORIA", "EL SUELO QUE CAE"
    };
    private static readonly string[] StoryCards =
    {
        "Nerea: «Guarda el número de tu tarjeta. Aquí hasta tu nombre tiene precio».",
        "El vehículo se detiene. Los guardias te llevan a una arena desconocida.",
        "Al otro lado del cristal pasan luces blancas. El vehículo empieza a frenar.",
        "Dos guardias abren las puertas. Al fondo del pasillo se oye un altavoz."
    };
    private int rivalBots;
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
    private readonly bool[] bridgeRight = new bool[2];
    private int[] bridgeMistakes = new int[2];
    private bool[,] safeBridge = new bool[8, 2];
    private Vector2[] finalPosition = new Vector2[2];
    private Vector2[] botPosition = new Vector2[11];
    private bool[] playerAlive = new bool[2];
    private bool[] playerWon = new bool[2];
    private bool lightsGreen = true;
    private float lightClock;
    private readonly bool[] redHeld = new bool[2];
    private bool insiderTrusted;
    private bool insiderRouteRevealed;
    private bool insiderBetrayed;
    private string resultMessage = "";
    private Texture2D pixel;
    private float[] jumpTimer = new float[2];

    private bool[] playerMoving = new bool[2];
    private int shotTarget = -1;
    private int shotBotTarget = -1;
    private int killedPlayer = -1;
    private float shotTimer;

    private AudioSource cueSpeaker;
    private AudioClip cueTone;
    private AudioClip victoryTone, lossTone, impactTone;
    private GUIStyle titleStyle, bodyStyle, buttonStyle, smallStyle;
    private float uiScale = 1f;
    private CampaignStage25D stage25D;
    [SerializeField] private GameObject leftTree, rightTree;
    private Texture2D menuBackdrop;

    private void Awake()
    {
        cueSpeaker = gameObject.AddComponent<AudioSource>();
        cueSpeaker.playOnAwake = false;
        cueTone = AudioClip.Create("Señal de la arena", 4800, 1, 24000, false);
        float[] samples = new float[4800];
        for (int i=0;i<samples.Length;i++)
        {
            float t=i/24000f;
            samples[i]=Mathf.Sin(2*Mathf.PI*420*t)*Mathf.Clamp01(t*140)*Mathf.Exp(-t*22)*.22f;
        }
        cueTone.SetData(samples,0);
        victoryTone=ArenaCue("Paso libre",330,415,554);
        lossTone=ArenaCue("Cierre de puertas",247,196,147);
        impactTone=ArenaCue("Impacto",126,69);
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
        pixel.filterMode = FilterMode.Point;
        stage25D = GetComponent<CampaignStage25D>();
        if (stage25D == null) stage25D = gameObject.AddComponent<CampaignStage25D>();
        stage25D.SetFinishTrees(leftTree,rightTree);
        menuBackdrop=Resources.Load<Texture2D>("Backgrounds/Fondo_Cuartos");
    }

    private void OnDestroy()
    {
        if (pixel != null) Destroy(pixel);
        foreach(var texture in cookieFaces) if(texture!=null) Destroy(texture);
        if (cueTone != null) Destroy(cueTone);
        Destroy(victoryTone); Destroy(lossTone); Destroy(impactTone);
    }

    private void Chime(float pitch)
    {
        cueSpeaker.pitch = pitch;
        cueSpeaker.PlayOneShot(cueTone);
    }

    private static AudioClip ArenaCue(string label,params int[] notes)
    {
        const int rate=24000, beat=3000;
        float[] sound=new float[notes.Length*beat];
        for(int i=0;i<sound.Length;i++)
        {
            float t=(i%beat)/(float)rate;
            float a=notes[i/beat]*2*Mathf.PI*t;
            sound[i]=(.19f*Mathf.Sin(a)+.045f*Mathf.Sin(a*2))*Mathf.Min(1,t*180)*Mathf.Exp(-t*40);
        }
        var clip=AudioClip.Create(label,sound.Length,1,rate,false);
        clip.SetData(sound,0);
        return clip;
    }

    private void LateUpdate()
    {
        if (stage25D == null) return;
        float stageProgress = trialIndex == (int)Trial.Tug ? tugProgress : trialIndex == (int)Trial.RedLight ? runner[0] : trialIndex == (int)Trial.Bridge ? bridgeStep[0] : finalPosition[0].x;
        float stageProgress2 = trialIndex == (int)Trial.RedLight ? runner[1] : trialIndex == (int)Trial.Bridge ? bridgeStep[1] : finalPosition[1].x;
        Vector2 visualPlayerOne = trialIndex == (int)Trial.RedLight ? new Vector2(runner[0], 0f) : finalPosition[0];
        Vector2 visualPlayerTwo = trialIndex == (int)Trial.RedLight ? new Vector2(runner[1], 0f) : finalPosition[1];
        int shotVictim = shotTarget >= 0 ? shotTarget : shotBotTarget >= 0 ? shotBotTarget + 2 : -1;
        stage25D.Present((int)screen, trialIndex, humanCount, screen == Screen.Round ? trialTime : phaseTime, stageProgress, stageProgress2, lightsGreen,
            shotVictim, jumpTimer[0], jumpTimer[1], playerMoving[0], playerMoving[1], visualPlayerOne, visualPlayerTwo, botPosition,
            playerAlive, botRunnerAlive, bridgeRight, nightHostile, knownGlass, safeBridge, tugAllies);
        if ((Trial)trialIndex == Trial.Carousel && stage25D.HasCarousel)
            stage25D.ShowCarousel(carouselAngle, carouselRoom, roomCount, capacity, humanCount,
                finalPosition, botPosition, botRunnerAlive, carouselPhase == CarouselPhase.Doors, playerMoving);
        if (screen == Screen.Round || screen == Screen.Interlude || screen == Screen.GameOver || screen == Screen.Result)
        {
            if ((Trial)trialIndex == Trial.Rope) stage25D.ShowRopeRound(ropePhase, ropeSlots, humanCount, playerAlive, botRunnerAlive);
            if ((Trial)trialIndex == Trial.Final) stage25D.ShowFloor(floorRiders, tileDeadline, trialTime, humanCount, playerAlive, botRunnerAlive);
            if ((Trial)trialIndex == Trial.Bridge) stage25D.ShowGlassFalls(bridgeFallAt, trialTime);
        }
        if(screen==Screen.Victory || screen==Screen.Ending) stage25D.ShowWinners(screen==Screen.Ending ? 9f : phaseTime,playerWon,humanCount);
        stage25D.RevealContestants(humanCount);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if(screen==Screen.Victory)
        {
            phaseTime+=dt;
            if(phaseTime>=9f || (phaseTime>1.5f && Keyboard.current?.spaceKey.wasPressedThisFrame==true)) screen=Screen.Ending;
            return;
        }
        if (screen == Screen.Interlude)
        {
            phaseTime += dt;
            if (phaseTime >= 6f || (phaseTime > .5f && Keyboard.current?.spaceKey.wasPressedThisFrame == true)) { screen = Screen.Round; phaseTime = 0f; }
            return;
        }
        if (screen == Screen.Kidnap || screen == Screen.Welcome)
        {
            phaseTime += dt;
            Keyboard cutsceneKeys = Keyboard.current;
            float duration = screen == Screen.Welcome ? 16f : 12f;
            if (phaseTime >= duration || (phaseTime > .5f && cutsceneKeys?.spaceKey.wasPressedThisFrame == true))
            {
                if (screen == Screen.Welcome) BeginTrial(0);
                else { screen = Screen.Welcome; phaseTime = 0f; }
            }
            return;
        }
        if (screen == Screen.Betrayal)
        {
            phaseTime += dt;
            if (phaseTime >= 6f) Advance();
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
            case Trial.Cookie: UpdateCookie(keys, dt); break;
            case Trial.Carousel: UpdateCarousel(keys, dt); break;
            case Trial.Night: UpdateNight(keys, dt); break;
            case Trial.Tug: UpdateTug(keys, dt); break;
            case Trial.Bridge: UpdateBridge(keys); break;
            case Trial.Rope: UpdateRopeRound(keys, dt); break;
            case Trial.Final: UpdateFloor(keys, dt); break;
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
        if (lightClock >= (lightsGreen ? 3.2f : 2.4f)) { lightClock = 0f; lightsGreen = !lightsGreen; Chime(lightsGreen?1.5f:.62f); }
        MoveRunner(k, 0, Key.W, Key.A, Key.S, Key.D, dt);
        if (humanCount == 2) MoveRunner(k, 1, Key.UpArrow, Key.LeftArrow, Key.DownArrow, Key.RightArrow, dt);
        for (int i = 0; i < botRunnerAlive.Length; i++)
        {
            if (!botRunnerAlive[i]) continue;
            if (lightsGreen && lightClock < 2.15f + nerve[i] * .95f)
                botPosition[i].x = Mathf.Min(760f, botPosition[i].x + (68f + (i % 4) * 6f) * dt);
            else if (!lightsGreen && botPosition[i].x < 760f && Random.value < dt * nerve[i] * .28f)
            {
                botStrikes[i]++;
                if (botStrikes[i] >= 3)
                {
                    if (shotTarget < 0 && shotBotTarget < 0)
                    {
                        shotBotTarget = i;
                        shotTimer = 0f;

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
        else if (trialTime > 48f) CompleteTrial(false, "El portón se cerró antes de que el equipo cruzara.");
    }

    private void MoveRunner(Keyboard k, int p, Key up, Key left, Key down, Key right, float dt)
    {
        if (playerWon[p]) return;
        float axis = ((k[right].isPressed || k[up].isPressed) ? 1f : 0f)
                   - ((k[left].isPressed || k[down].isPressed) ? 1f : 0f);
        if (Mathf.Abs(axis) < 0.01f) { redHeld[p] = false; return; }
        if (lightsGreen) redHeld[p] = false;
        playerMoving[p] = true;
        if (!lightsGreen)
        {
            if (lightClock < .22f) return;
            bool movedThisFrame = !redHeld[p];
            redHeld[p] = true;
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

    private void UpdateBridge(Keyboard k)
    {
        if (Pressed(k, Key.A) || (humanCount == 1 && Pressed(k, Key.LeftArrow))) ChooseBridge(0, false);
        if (Pressed(k, Key.D) || (humanCount == 1 && Pressed(k, Key.RightArrow))) ChooseBridge(0, true);
        if (humanCount == 2)
        {
            if (Pressed(k, Key.LeftArrow)) ChooseBridge(1, false);
            if (Pressed(k, Key.RightArrow)) ChooseBridge(1, true);
        }
        bool humansAcross=bridgeStep[0]>=8 && (humanCount==1 || bridgeStep[1]>=8);
        bool botsCrossing=false;
        for (int i=0;i<rivalBots;i++)
        {
            int step=botStep[i];
            if (!botRunnerAlive[i] || step>=8) continue;
            if (trialTime>=65f) { botRunnerAlive[i]=false; continue; }
            botsCrossing=true;
            if ((botClock[i]-=Time.deltaTime*(humansAcross?2.6f:1f))>0) continue;
            bool right = knownGlass[step] && Random.value < .82f + .15f*craft[i]
                ? safeBridge[step,1] : Random.value < .5f;
            bool safe = safeBridge[step,right ? 1 : 0];
            knownGlass[step]=true;
            if (!safe) botRunnerAlive[i]=false;
            else botStep[i]++;
            botPosition[i]=new Vector2(step+1,right?1:0);
            if (!safe) { bridgeFallAt[i]=trialTime; Chime(.6f); }
            botClock[i]=1.8f+2f*(1f-nerve[i]);
        }
        if (humansAcross && !botsCrossing) CompleteTrial(true, "El recuento terminó. Los supervivientes cruzaron el puente.");
        else if (bridgeMistakes[0] >= 3 || (humanCount == 2 && bridgeMistakes[1] >= 3)) CompleteTrial(false, "Demasiados paneles se rompieron.");
        else if (trialTime > 65f) CompleteTrial(false, "El puente se cerró.");
    }

    private void ChooseBridge(int p, bool right)
    {
        if (p >= humanCount || bridgeStep[p] >= 8) return;
        knownGlass[bridgeStep[p]] = true;
        if (safeBridge[bridgeStep[p], right ? 1 : 0]) { bridgeRight[p]=right; bridgeStep[p]++; Chime(1.3f); }
        else { bridgeMistakes[p]++; Chime(.58f); }
    }

    private void StartKidnap()
    {
        screen = Screen.Kidnap;
        phaseTime = 0f;
    }

    private void StartGuardShot(int player)
    {
        cueSpeaker.pitch=1; cueSpeaker.PlayOneShot(impactTone);
        shotTarget = player;
        shotBotTarget = -1;
        shotTimer = 0f;
    }

    private void BeginTrial(int index)
    {
        trialIndex = index;
        screen = Screen.Round;
        trialTime = 0f;
        phaseTime = 0f;
        runner = new float[2];
        botStrikes = new int[rivalBots];
        redStrikes1 = redStrikes2 = 0;
        shotTarget = shotBotTarget = killedPlayer = -1;
        shotTimer = 0f;
        jumpTimer = new float[2];

        playerMoving = new bool[2];
        tugProgress = 42f;
        bridgeRight[0] = false; bridgeRight[1] = true;
        bridgeStep = new int[2]; bridgeMistakes = new int[2];
        for (int i = 0; i < 8; i++)
        {
            bool safeRight = insiderTrusted ? (i == 0 || i == 3 || i == 5 || i == 6) : Random.value > 0.5f;
            safeBridge[i, 0] = !safeRight;
            safeBridge[i, 1] = safeRight;
        }
        insiderRouteRevealed = index == (int)Trial.Bridge && insiderTrusted;
        finalPosition = new Vector2[2];
        botPosition = new Vector2[rivalBots];
        if ((Trial)index == Trial.RedLight)
            for (int i=0;i<rivalBots;i++) botPosition[i].x=Random.Range(0f,6f);
        if ((Trial)index != Trial.Final) playerAlive = new[] { true, humanCount == 2 };
        playerWon = new bool[2];
        lightsGreen = true; lightClock = 0f;
        redHeld[0] = redHeld[1] = false;
        PrepareContest();
        bridgeFallAt = new float[rivalBots];
        System.Array.Fill(bridgeFallAt, float.NegativeInfinity);
        if ((Trial)index == Trial.Rope) PrepareRopeRound();
        if ((Trial)index == Trial.Final) PrepareFloor();
        if ((Trial)index == Trial.Carousel) PrepareCarousel();
    }

    private void CompleteTrial(bool success, string message)
    {
        if (screen != Screen.Round) return;
        cueSpeaker.pitch=1;
        cueSpeaker.PlayOneShot(success?victoryTone:lossTone);
        resultMessage = message;
        if (success) ResolveRoster();
        screen = success
            ? isPractice ? Screen.Result
            : trialIndex == (int)Trial.Final ? Screen.Victory
            : trialIndex == (int)Trial.RedLight ? Screen.Vote
            : trialIndex == (int)Trial.Tug ? Screen.Insider
            : trialIndex == (int)Trial.Bridge ? Screen.Betrayal
            : Screen.Result
            : Screen.GameOver;
        if(screen==Screen.Victory) phaseTime=0;
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
        phaseTime = 0f;
        screen = Screen.Interlude;
    }

    private void StartGame(int players)
    {
        isPractice = false;
        humanCount = players;
        trialIndex = 0;
        CreateRoster();
        insiderTrusted = false;
        insiderRouteRevealed = false;
        insiderBetrayed = false;
        StartKidnap();
    }

    private void ResolveInsiderChoice(bool trust)
    {
        insiderTrusted = trust;
        resultMessage = trust
            ? "Nerea dibuja ocho huellas en tu manga. Al retirarse, vuelve a encender su radio."
            : "Nerea se marcha. No sabes si era una aliada o si formaba parte de otra trampa.";
        screen = Screen.Result;
    }

    private void SetupStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 34, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.82f, 0.36f) } };
        bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, wordWrap = true, normal = { textColor = Color.white } };
        smallStyle = new GUIStyle(bodyStyle) { fontSize = 16 };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, hover = { textColor = Color.yellow } };
        buttonStyle.normal.background=pixel;
        buttonStyle.hover.background=pixel;
        buttonStyle.active.background=pixel;
        buttonStyle.border=new RectOffset();
        buttonStyle.hover.textColor=new Color(1,.86f,.52f);
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
        else if (screen == Screen.Welcome) DrawWelcome();
        else if (screen == Screen.Victory) DrawVictory();
        else if (screen == Screen.Practice) DrawPracticeEntry();
        else if (screen == Screen.Round) DrawRound();
        else if (screen == Screen.Result) DrawResult();
        else if (screen == Screen.Insider) DrawInsider();
        else if (screen == Screen.Betrayal) DrawBetrayal();
        else if (screen == Screen.GameOver) DrawGameOver();
        else if (screen == Screen.Vote || screen == Screen.Exit) DrawVote();
        else if (screen == Screen.Interlude) DrawInterlude();
        else DrawEnding();
        GUI.backgroundColor = previousBackgroundColor;
        GUI.matrix = previousMatrix;
    }

    private void DrawTitle()
    {
        if(menuBackdrop!=null)
        {
            float w=UnityEngine.Screen.width/uiScale,h=UnityEngine.Screen.height/uiScale;
            var full=new Rect((960-w)/2,(540-h)/2,w,h);
            GUI.DrawTexture(full,menuBackdrop,ScaleMode.ScaleAndCrop);
            Fill(full,new Color(.025f,.04f,.075f,.20f));
        }
        Fill(new Rect(28,36,381,465),new Color(.035f,.058f,.085f,.96f));
        Fill(new Rect(28,36,381,5),new Color(.98f,.67f,.27f));
        Label(new Rect(36,68,365,61),"LA ÚLTIMA RONDA",titleStyle);
        Label(new Rect(43,129,352,45),$"{CampaignBots} rivales  ·  {TrialNames.Length} pruebas",bodyStyle);
        if (GUI.Button(new Rect(55,193,327,48),"CAMPAÑA INDIVIDUAL",buttonStyle)) StartGame(1);
        if (GUI.Button(new Rect(55,251,327,48),"COOPERATIVO LOCAL",buttonStyle)) StartGame(2);
        Label(new Rect(46,302,347,37),"J1 · WASD      J2 · FLECHAS",smallStyle);
        Fill(new Rect(55,353,327,1),new Color(.34f,.44f,.47f));
        Label(new Rect(53,361,329,28),"PRACTICAR",smallStyle);
        if (GUI.Button(new Rect(55,397,327,36),"Tira y afloja",buttonStyle))
            OpenTugPractice();
        if (GUI.Button(new Rect(55,442,327,36),"Saltar la cuerda",buttonStyle))
            OpenRopePractice();
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
        if ((Trial)trialIndex != Trial.Carousel)
        {
            Label(new Rect(35, 15, 890, 45), TrialNames[trialIndex], titleStyle);
            Label(new Rect(15, 65, 930, 32), "PRUEBA " + (trialIndex + 1) + "/" + TrialNames.Length + "     RIVALES RESTANTES: " + RemainingRivals(), smallStyle);
        }
        switch ((Trial)trialIndex)
        {
            case Trial.RedLight: DrawRedLight(); break;
            case Trial.Cookie: DrawCookie(); break;
            case Trial.Carousel: DrawCarousel(); break;
            case Trial.Night: DrawNight(); break;
            case Trial.Tug: DrawTug(); break;
            case Trial.Bridge: DrawBridge(); break;
            case Trial.Rope: DrawRopeRound(); break;
            case Trial.Final: DrawFloor(); break;
        }
        if (trialIndex == (int)Trial.Bridge) DrawBridgeHint();
        Label(new Rect(25, 500, 910, 24), humanCount == 2 ? "COOPERATIVO LOCAL   ·   J1 y J2 comparten el resultado" : "CAMPAÑA INDIVIDUAL", smallStyle);
    }

    private void DrawRedLight()
    {
        Fill(new Rect(205,95,550,47),new Color(.04f,.07f,.10f,.88f));
        Fill(new Rect(205,95,7,47),lightsGreen?new Color(.26f,.89f,.56f):new Color(1,.28f,.33f));
        string signal = !lightsGreen ? "ROJA · DETENTE" : lightClock > 2.55f ? "ATENCIÓN · VA A GIRAR" : "VERDE · AVANZA";
        Label(new Rect(205,94,550,46),signal,titleStyle);
        Label(new Rect(100,416,760,34),humanCount==1 ? "W o D para avanzar. Suelta al girar la muñeca. Tres faltas te eliminan." : "J1 avanza con W. J2 con ↑. Suelten al girar la muñeca. Tres faltas eliminan al equipo.",smallStyle);
        Label(new Rect(30, 460, 450, 30), "Faltas P1: " + redStrikes1 + "/3" + (humanCount == 2 ? "   P2: " + redStrikes2 + "/3" : ""), smallStyle);
        Fill(new Rect(700, 462, 220, 16), new Color(0.04f, 0.07f, 0.1f, 0.82f));
        Fill(new Rect(704, 466, 212f * Mathf.Clamp01(runner[0] / 760f), 8), new Color(0.35f, 0.86f, 0.69f));
    }

    private void DrawBridge()
    {
        Fill(new Rect(66,396,828,101),new Color(.03f,.05f,.10f,.93f));
        string hint=bridgeStep[0]>=8 && (humanCount==1 || bridgeStep[1]>=8) ? "Ya cruzaron. Los últimos rivales están saltando."
            : humanCount==1 ? "A / ← izquierda   ·   D / → derecha   ·   Tres errores te eliminan."
            : "J1 A/D   ·   J2 ←/→   ·   Elijan un cristal por fila. Tres errores eliminan.";
        Label(new Rect(74,399,812,29),hint,smallStyle);
        Label(new Rect(74,430,812,26),$"J1 · Paso {bridgeStep[0]}/8 · Errores {bridgeMistakes[0]}/3"+
            (humanCount==2?$"      J2 · Paso {bridgeStep[1]}/8 · Errores {bridgeMistakes[1]}/3":""),smallStyle);
    }

    private void DrawBridgeHint()
    {
        string route = insiderRouteRevealed ? "PISTA DE NEREA: " : "PASOS OBSERVADOS: ";
        for (int i = 0; i < 8; i++) route += !insiderRouteRevealed && !knownGlass[i] ? "·   " : safeBridge[i, 1] ? "D  " : "I  ";
        Label(new Rect(110,461,740,28), route, smallStyle);
    }

    private void DrawResult()
    {
        Fill(new Rect(120, 120, 720, 310), new Color(0.03f, 0.05f, 0.1f, 0.78f));
        Label(new Rect(170, 155, 620, 55), "PRUEBA SUPERADA", titleStyle);
        Label(new Rect(190, 225, 580, 60), resultMessage, bodyStyle);
        if ((Trial)trialIndex != Trial.Cookie || ally < 0) Label(new Rect(170, 300, 620, 42), "Rivales restantes: " + RemainingRivals(), smallStyle);
        if ((Trial)trialIndex == Trial.Cookie && ally >= 0)
        {
            Label(new Rect(150,290,660,58), $"Participante {ally+humanCount+1:00}: «Comparte tu ración y esta noche te cubro».", bodyStyle);
            if (GUI.Button(new Rect(185,365,275,55),"COMPARTIR",buttonStyle))
            {
                share = true;
                doubleCross = nerve[ally] > .58f;
                Advance();
            }
            else if (GUI.Button(new Rect(500,365,275,55),"GUARDAR RACIÓN",buttonStyle)) Advance();
            return;
        }
        if (GUI.Button(new Rect(350, 365, 260, 55), isPractice ? "VOLVER AL MENÚ" : "SIGUIENTE PRUEBA", buttonStyle))
        {
            if (isPractice) screen = Screen.Title;
            else Advance();
        }
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
            ? "Nerea pulsa la alarma. «Te di ocho pasos seguros para que llegaras hasta mi cámara». Los VIP ya te esperan al otro lado de la cuerda."
            : "La infiltrada se queda atrás para distraer a los guardias. Dos muñecas esperan en los extremos de la pasarela.";
        Label(new Rect(110, 84, 740, 90), text, bodyStyle);
        Label(new Rect(335, 475, 290, 28), "SIGUIENTE PRUEBA EN " + Mathf.CeilToInt(6f - phaseTime), smallStyle);
    }

    private void DrawGameOver()
    {
        Fill(new Rect(0, 0, 960, 540), new Color(0.04f, 0.025f, 0.06f, 0.25f));
        Fill(new Rect(131,100,698,347),new Color(.055f,.065f,.09f,.95f));
        Fill(new Rect(131,100,698,5),new Color(.92f,.22f,.35f));
        Label(new Rect(100, 115, 760, 65), "FIN DE LA PARTIDA", titleStyle);
        Label(new Rect(160, 215, 640, 75), resultMessage, bodyStyle);
        Label(new Rect(150, 305, 660, 45), "Llegaron hasta: " + TrialNames[Mathf.Clamp(trialIndex, 0, TrialNames.Length - 1)], smallStyle);
        if (GUI.Button(new Rect(365, 375, 230, 55), "VOLVER AL MENÚ", buttonStyle)) screen = Screen.Title;
    }

    private void DrawEnding()
    {
        Fill(new Rect(0, 0, 960, 540), new Color(0.035f, 0.06f, 0.11f, 0.25f));
        Fill(new Rect(131,94,698,336),new Color(.045f,.105f,.12f,.95f));
        Fill(new Rect(131,94,698,5),new Color(.98f,.73f,.35f));
        Label(new Rect(100, 110, 760, 68), WinnerTitle(), titleStyle);
        Label(new Rect(160, 215, 640, 72), insiderBetrayed ? "La cinta de la alarma lleva la firma de Nerea. Al salir, te llevas la grabación como prueba." : "La puerta se cierra detrás de ti. En tu bolsillo queda una tarjeta de Nerea: «Ahora puedes contarlo».", bodyStyle);
        if (GUI.Button(new Rect(365, 345, 230, 60), "JUGAR DE NUEVO", buttonStyle)) screen = Screen.Title;
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
        if (botRunnerAlive == null) return 0;
        int remaining = 0;
        for (int i = 0; i < botRunnerAlive.Length; i++) if (botRunnerAlive[i]) remaining++;
        return remaining;
    }


}
