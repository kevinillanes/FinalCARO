using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds the campaign arenas as small, perspective 3D sets while keeping play on a readable plane.</summary>
public sealed partial class CampaignStage25D : MonoBehaviour
{
    private const int Title = 0, Kidnap = 1, Round = 2, Result = 3, Insider = 4, Betrayal = 5, Ending = 6, GameOver = 7, Vote = 8, Interlude = 9;
    private const int RedLight = (int)PixelSurvivalGame.Trial.RedLight, Cookie = (int)PixelSurvivalGame.Trial.Cookie,
        Carousel = (int)PixelSurvivalGame.Trial.Carousel, Night = (int)PixelSurvivalGame.Trial.Night,
        Tug = (int)PixelSurvivalGame.Trial.Tug, Bridge = (int)PixelSurvivalGame.Trial.Bridge, Final = (int)PixelSurvivalGame.Trial.Final;
    private int rosterSlots = 31;
    private readonly List<GameObject> actors = new List<GameObject>();
    private Transform stageRoot;
    private Camera sceneCamera;
    private Material floorMat, roomFloorMat, wallMat, darkMat, greenMat, grassMat, redMat, pinkMat, blueMat, glassMat, goldMat, whiteMat, woodMat, skinMat;
    private Sprite playerSprite, playerWalkA, playerWalkB, guardSprite, armedGuardSprite;
    private Sprite[] dollFrames;
    private GameObject ropeMarker, doll, van;
    private ArenaCharacterArt characterArt;
    private readonly Dictionary<GameObject, ArenaCharacterPose> poses = new Dictionary<GameObject, ArenaCharacterPose>();
    private readonly HashSet<Renderer> cutaway = new HashSet<Renderer>();
    private readonly RaycastHit[] obstructionHits = new RaycastHit[64];
    private float sizeVelocity;
    private Renderer signalLamp;
    private GameObject leftTree,rightTree;
    private float tugEnd;
    public void SetFinishTrees(GameObject left,GameObject right){leftTree=left; rightTree=right;}
    private Vector3[] jumpStart, jumpEnd;
    private float[] jumpBlend;
    private Material sandstone, terracotta, trackSoil;
    private Texture2D aggregate, floorTiles;
    private SpriteRenderer dollRenderer;
    private LineRenderer shotTrail, tugRope, tugRopeFiberA, tugRopeFiberB;
    private readonly List<GameObject> bridgeLeft = new List<GameObject>();
    private readonly List<GameObject> bridgeRight = new List<GameObject>();
    private int lastScreen = -1, lastTrial = -1;
    private float animClock, ropeCenter, ropeSpeed, dollFrame;
    private Vector3 cameraVelocity;
    private bool cameraReady;
    private static readonly Color FieldGreen = new Color(.20f, .37f, .24f);
    private static readonly Color WarmLight = new Color(1f, .79f, .50f);

    private void Awake()
    {
        sceneCamera = Camera.main;
        if (sceneCamera == null) sceneCamera = FindAnyObjectByType<Camera>();
        if (sceneCamera != null)
        {
            sceneCamera.orthographic = true;
            sceneCamera.fieldOfView = 48f;
            sceneCamera.nearClipPlane = .1f;
            sceneCamera.farClipPlane = 160f;
            sceneCamera.backgroundColor = new Color(.035f, .045f, .09f);
            AimCamera(new Vector3(0f, 1.2f, 1.5f));
        }

        floorMat = MakeMaterial(new Color(.18f, .21f, .25f));
        roomFloorMat = MakeMaterial(new Color(.24f, .29f, .21f));
        wallMat = MakeMaterial(new Color(.18f, .20f, .27f));
        darkMat = MakeMaterial(new Color(.025f, .035f, .07f));
        greenMat = MakeMaterial(new Color(.03f, .34f, .23f));
        grassMat = MakeMaterial(FieldGreen);
        redMat = MakeMaterial(new Color(.78f, .035f, .045f));
        pinkMat = MakeMaterial(new Color(.90f, .17f, .33f));
        blueMat = MakeMaterial(new Color(.12f, .39f, .72f));
        glassMat = MakeGlassMaterial(new Color(.48f, .75f, .84f, .55f));
        goldMat = MakeMaterial(new Color(.98f, .70f, .24f));
        whiteMat = MakeMaterial(new Color(.82f, .87f, .90f));
        woodMat = MakeMaterial(new Color(.32f, .19f, .12f));
        skinMat = MakeMaterial(new Color(.91f, .69f, .48f));
        playerSprite = LoadSprite("Player_Idle_A");
        playerWalkA = LoadSprite("Player_Walk_A");
        playerWalkB = LoadSprite("Player_Walk_B");
        guardSprite = LoadSprite("Guard_Idle");
        armedGuardSprite = LoadSprite("Guard_Armed");
        characterArt = new ArenaCharacterArt();
        sandstone=MakeMaterial(new Color(.78f,.62f,.40f));
        terracotta=MakeMaterial(new Color(.57f,.24f,.16f));
        trackSoil=MakeMaterial(new Color(.85f,.71f,.5f));
        aggregate=SurfacePrint(false); floorTiles=SurfacePrint(true);
        trackSoil.SetTexture("_BaseMap",aggregate);
        trackSoil.SetTextureScale("_BaseMap",new Vector2(9,29));
        floorMat.SetTexture("_BaseMap",floorTiles);
        floorMat.SetTextureScale("_BaseMap",new Vector2(9,10));
        dollFrames = LoadDollFrames();
        if (stageRoot == null)
        {
            var root = new GameObject("2.5D Campaign Stage");
            stageRoot = root.transform;
        }
        BuildStage(Title, RedLight);
    }

    private void LateUpdate() { animClock += Time.deltaTime; }

    private void OnDestroy()
    {
        if (carouselCanopy != null) Destroy(carouselCanopy);
        if (stageRoot != null) Destroy(stageRoot.gameObject);
        characterArt?.Dispose();
        Destroy(aggregate); Destroy(floorTiles); Destroy(trackSoil);
        Material[] materials = { floorMat, roomFloorMat, wallMat, darkMat, greenMat, grassMat, redMat, pinkMat, blueMat, glassMat, goldMat, whiteMat, woodMat, skinMat, sandstone, terracotta };
        foreach (Material material in materials) if (material != null) Destroy(material);
        Sprite[] sprites = { playerSprite, playerWalkA, playerWalkB, guardSprite, armedGuardSprite };
        foreach (Sprite sprite in sprites) if (sprite != null) Destroy(sprite);
        if (dollFrames != null) foreach (Sprite sprite in dollFrames) if (sprite != null) Destroy(sprite);
    }

    public void Present(int screen, int trial, int humanCount, float phase, float progress, float progress2, bool greenLight,
        int shotVictim, float jumpOne, float jumpTwo, bool movingOne, bool movingTwo, Vector2 playerOne, Vector2 playerTwo, Vector2[] bots,
        bool[] playerAlive, bool[] botAlive, bool[] bridgeSide, bool[] nightHostile, bool[] knownGlass, bool[,] safeBridge)
    {
        trial = Mathf.Clamp(trial, 0, Final);
        rosterSlots = 2 + botAlive.Length;
        if (trial==lastTrial && IsTrialSet(screen) && IsTrialSet(lastScreen))
            lastScreen=screen;
        else if (screen != lastScreen || trial != lastTrial)
        {
            if(trial==Tug)
            {
                int left=humanCount,right=0;
                for(int i=0;i<botAlive.Length;i++) if(botAlive[i]) { if(i%2==0)left++;else right++; }
                tugEnd=Mathf.Max(6.3f,2.1f+1.28f*(Mathf.Max(left,right)-1));
            }
            BuildStage(screen, trial);
        }
        if (sceneCamera != null)
        {
            FrameArena(screen, trial, phase, humanCount, progress, progress2, playerOne, playerTwo);
        }
        UpdateActors(screen == Interlude ? Round : screen, trial, humanCount, phase, progress, progress2, greenLight, shotVictim, jumpOne, jumpTwo, movingOne, movingTwo,
            playerOne, playerTwo, bots, playerAlive, botAlive, bridgeSide, nightHostile, knownGlass, safeBridge);
    }

    private static bool IsTrialSet(int screen) => screen==Round || screen==Result || screen==GameOver || screen==Interlude;

    private void BuildStage(int screen, int trial)
    {
        lastScreen = screen;
        lastTrial = trial;
        carouselPlatform = null;
        cutaway.Clear();
        poses.Clear();
        signalLamp=null;
        cameraReady=false;
        foreach (Transform child in stageRoot) Destroy(child.gameObject);
        actors.Clear(); bridgeLeft.Clear(); bridgeRight.Clear(); ropeMarker = doll = van = null; dollRenderer = null;
        shotTrail = tugRope = tugRopeFiberA = tugRopeFiberB = null;
        ArenaView.KeyLight(stageRoot);
        AddLight("Stage Key Light", new Vector3(-5f, 11f, -7f), 1.15f, 15f);
        AddLight("Warm Fill", new Vector3(6f, 8f, 5f), .75f, 11f);
        if (screen == Kidnap) { BuildKidnap(); return; }
        if (screen == Round || screen == Interlude) BuildTrial(trial);
        else if (screen == Vote) BuildDormitory();
        else if (screen == Insider || screen == Betrayal) BuildNarrativeRoom(screen);
        else if (screen == Result || screen == GameOver) BuildTrial(trial);
        else if (screen == Ending) BuildFinal();
        else BuildLobby();

    }

    private void BuildTrial(int trial)
    {
        switch (trial)
        {
            case RedLight: BuildRedLight(); break;
            case Cookie: BuildCookieRoom(); break;
            case Carousel: BuildCarousel(); break;
            case Night: BuildDormitory(); break;
            case Tug: BuildTug(); break;
            case Bridge: BuildBridge(); break;
            default: BuildFinal(); break;
        }
    }

    private void BuildLobby()
    {
        sceneCamera.backgroundColor = new Color(.025f, .035f, .075f);
        Box("Lobby Floor", new Vector3(0f, -.32f, 1f), new Vector3(20f, .6f, 25f), floorMat);
        Box("Back Wall", new Vector3(0f, 3.2f, 11.5f), new Vector3(20f, 7f, .6f), wallMat);
        PaintWall("Dormitory Background", new Vector3(0f, 3.2f, 11.08f), new Vector2(19.5f, 6.6f));
        Box("Left Wall", new Vector3(-9.8f, .25f, 1f), new Vector3(.5f,.5f,22f), wallMat);
        Box("Right Wall", new Vector3(9.8f, 2.8f, 1f), new Vector3(.5f, 6f, 22f), wallMat);
        for (int i = 0; i < 8; i++)
        {
            Box("Wall Light " + i, new Vector3(-7.8f + i * 2.2f, 4.8f, 11.12f), new Vector3(.16f, 2.3f, .13f), i % 2 == 0 ? pinkMat : blueMat);
            Box("Floor Stripe " + i, new Vector3(-8.7f + i * 2.5f, .015f, -5f), new Vector3(.035f, .025f, 13f), goldMat);
        }
        for (int i = 0; i < 7; i++) actors.Add(Actor(false));
        actors.Add(Actor(true)); actors.Add(Actor(true));
    }

    private void BuildKidnap()
    {
        sceneCamera.backgroundColor = new Color(.012f, .02f, .045f);
        Box("Night Road", new Vector3(0f, -.3f, 1f), new Vector3(25f, .5f, 32f), darkMat);
        for (int i = 0; i < 9; i++)
        {
            Box("Road Mark " + i, new Vector3(-10f + i * 2.5f, -.02f, -7f + (i % 2) * 15f), new Vector3(.08f, .025f, 3.2f), whiteMat);
            Box("Street Lamp " + i, new Vector3(-10f + i * 2.5f, 2.9f, 8f), new Vector3(.12f, 5.8f, .12f), wallMat);
            Sphere("Lamp Glow " + i, new Vector3(-10f + i * 2.5f, 5.8f, 8f), .23f, goldMat);
        }
        var vanRoot = new GameObject("Guard Van"); vanRoot.transform.SetParent(stageRoot, false); van = vanRoot;
        Box("Van Body", new Vector3(0f, .85f, 3f), new Vector3(4.4f, 1.5f, 2.3f), whiteMat, vanRoot.transform);
        Box("Van Cabin", new Vector3(.8f, 1.7f, 3f), new Vector3(2f, .9f, 2.25f), blueMat, vanRoot.transform);
        for (int i = 0; i < 4; i++) Cylinder("Van Wheel " + i, new Vector3(-1.4f + (i % 2) * 2.7f, .35f, 2f + (i / 2) * 2f), .37f, .28f, darkMat, vanRoot.transform, new Vector3(90f, 0f, 0f));
        for (int i = 0; i < 4; i++) actors.Add(Actor(i == 0));
        actors.Add(Actor(true)); actors.Add(Actor(true));
    }

    private void BuildRedLight()
    {
        sceneCamera.backgroundColor = new Color(.47f, .64f, .71f);
        BuildCourtyard();
        Box("Pista de albero", new Vector3(0f, -.35f, 10f), new Vector3(18f, .7f, 58f), trackSoil);
        Box("Left Track Boundary", new Vector3(-8.4f, .035f, 10f), new Vector3(.08f, .035f, 56f), whiteMat);
        Box("Right Track Boundary", new Vector3(8.4f, .035f, 10f), new Vector3(.08f, .035f, 56f), whiteMat);
        for (int lane = -1; lane <= 1; lane++)
            Box("Track Lane Divider " + lane, new Vector3(lane * 4.2f, .025f, 10f), new Vector3(.035f, .025f, 48f), whiteMat);
        Box("Start Line", new Vector3(0f, .04f, -12f), new Vector3(16.5f, .07f, .18f), whiteMat);
        Box("Finish Line", new Vector3(0f, .04f, 31f), new Vector3(16.5f, .07f, .2f), whiteMat);
        if(leftTree!=null) Instantiate(leftTree,new Vector3(-3.6f,.0f,35.6f),Quaternion.identity,stageRoot);
        if(rightTree!=null) Instantiate(rightTree,new Vector3(3.6f,.0f,36.6f),Quaternion.Euler(0,37f,0),stageRoot);
        BuildDoll();
        doll.transform.localRotation=Quaternion.Euler(0,ArenaView.Yaw,0);
        Cylinder("Poste de señal",new Vector3(7.8f,.81f,31.2f),.10f,1.62f,darkMat);
        signalLamp=Sphere("Semáforo de meta",new Vector3(7.8f,1.77f,31.2f),.4f,greenMat).GetComponent<Renderer>();
        for (int i = 0; i < rosterSlots; i++) actors.Add(Actor(false));
        actors.Add(Actor(true)); actors.Add(Actor(true)); actors.Add(Actor(true));
        var trailObject = new GameObject("Guard Shot Trail"); trailObject.transform.SetParent(stageRoot, false);
        shotTrail = trailObject.AddComponent<LineRenderer>();
        shotTrail.positionCount = 2; shotTrail.startWidth = .055f; shotTrail.endWidth = .025f;
        shotTrail.material = goldMat; shotTrail.startColor = new Color(1f, .9f, .55f); shotTrail.endColor = new Color(1f, .34f, .1f);
        shotTrail.enabled = false;
    }

    private void BuildTug()
    {
        sceneCamera.backgroundColor = new Color(.28f, .49f, .71f);
        for (int i = 0; i < 7; i++)
        {
            float x = -10f + (i % 4) * 6.4f;
            float z = 8f + (i / 4) * 5f;
            Sphere("High Cloud", new Vector3(x, 8f + (i % 3) * .8f, z), 2.1f, whiteMat).transform.localScale = new Vector3(3.2f, 1.1f, 1.5f);
        }
        for (int i = 0; i < 2; i++)
        {
            float x = (i==0?-1:1)*(tugEnd+1.6f)*.5f;
            Box("Floating Team Platform", new Vector3(x,2.4f,1.5f),new Vector3(tugEnd-.2f,.65f,4.6f),i==0?blueMat:redMat);
            for (int p = 0; p < 4; p++)
                Cylinder("Platform Support", new Vector3(x - 2.8f + p * 1.85f, -1.1f, i == 0 ? -1f : 4f), .16f, 6f, whiteMat);
        }
        ropeCenter = .5f; ropeSpeed = 0f;
        tugRope = CreateRopeLine("Tug Rope", .19f, new Color(.34f, .19f, .12f), woodMat);
        tugRopeFiberA = CreateRopeLine("Tug Rope Twist A", .045f, new Color(.61f, .39f, .23f), woodMat);
        tugRopeFiberB = CreateRopeLine("Tug Rope Twist B", .045f, new Color(.20f, .11f, .08f), woodMat);
        ropeMarker = Box("Rope Marker", new Vector3(0f, 4.15f, 1.5f), new Vector3(.34f, .52f, .38f), goldMat);
        for (int i=0;i<rosterSlots;i++) actors.Add(Actor(false));
    }

    private void BuildBridge()
    {
        jumpStart=new Vector3[rosterSlots]; jumpEnd=new Vector3[rosterSlots]; jumpBlend=new float[rosterSlots];
        sceneCamera.backgroundColor = new Color(.012f, .018f, .055f);
        Box("Bridge Departure",new Vector3(0,2.4f,-8.05f),new Vector3(5,.3f,2.1f),floorMat);
        Box("Bridge Void Base", new Vector3(0f, -5f, 3f), new Vector3(22f, .4f, 30f), darkMat);
        Box("Desembarco",new Vector3(0,2.4f,10.0f),new Vector3(5,.4f,4),floorMat);
        for(int lane=-1;lane<=1;lane+=2)
            Box("Viga longitudinal",new Vector3(lane*2.12f,2.15f,1.1f),new Vector3(.16f,.18f,20),goldMat);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 5; i++)
                Cylinder("Bridge Suspension Cable", new Vector3(side * 2.6f, 4.8f, -6f + i * 3.6f), .045f, 11f, glassMat);
        for (int i = 0; i < 8; i++)
        {
            float z = -6f + i * 2.05f;
            GameObject left = Box("Left Glass Panel " + i, new Vector3(-1.05f, 2.4f, z), new Vector3(1.9f, .22f, 1.78f), glassMat);
            GameObject right = Box("Right Glass Panel " + i, new Vector3(1.05f, 2.4f, z), new Vector3(1.9f, .22f, 1.78f), glassMat);
            bridgeLeft.Add(left); bridgeRight.Add(right);
        }
        for (int i=0;i<rosterSlots;i++) actors.Add(Actor(false));
    }

    private void BuildCookieRoom()
    {
        BuildNarrativeRoom(Round);
        for (int row = 0; row < 4; row++)
        {
            float z = -4.8f + row * 3.1f;
            Box("Mesa de azúcar", new Vector3(0,.65f,z), new Vector3(15,.35f,1.4f), woodMat);
            for (int seat = 0; seat < 8; seat++)
                Cylinder("Lata de galleta", new Vector3(-6.3f + seat*1.8f,.88f,z), .4f,.1f,goldMat);
        }
        for (int i = 0; i < rosterSlots; i++) actors.Add(Actor(false));
        actors.Add(Actor(true));
        actors.Add(Actor(true));
    }

    private void BuildDormitory()
    {
        sceneCamera.backgroundColor = new Color(.08f,.10f,.18f);
        Box("Dormitorio",new Vector3(0,-.3f,1),new Vector3(21,.6f,20),floorMat);
        PaintWall("Mural",new Vector3(0,2.7f,10.02f),new Vector2(20,4.65f));
        for (int side=-1;side<=1;side+=2)
            for (int row=0;row<4;row++)
            {
                float x=8.5f*side,z=-5+row*4;
                for (int level=0;level<2;level++)
                    Box("Colchón",new Vector3(x,.5f+level*1.6f,z),new Vector3(1.8f,.24f,3.5f),blueMat);
                for (int corner=0;corner<4;corner++)
                    Box("Poste",new Vector3(x+(corner%2-.5f)*1.8f,1.3f,z+(corner/2-.5f)*3.5f),new Vector3(.11f,2.6f,.11f),whiteMat);
            }
        BuildWardDetails();
        for(int i=0;i<rosterSlots;i++) actors.Add(Actor(false));
    }

    private void BuildFinal()
    {
        sceneCamera.backgroundColor = new Color(.13f, .19f, .28f);
        Box("Final Arena Floor", new Vector3(0f, -.3f, 1f), new Vector3(21f, .6f, 24f), floorMat);
        for (int i = 0; i < 9; i++)
        {
            float x = -9.5f + i * 2.375f;
            Box("Arena Wall Section", new Vector3(x, 2.9f, 10.3f), new Vector3(2.3f, 6f, .5f), i % 2 == 0 ? wallMat : darkMat);
            Box("Arena Banner", new Vector3(x, 5.5f, 10f), new Vector3(1.3f, .15f, .2f), i % 2 == 0 ? pinkMat : blueMat);
        }
        Box("Goal Gate", new Vector3(8f, 2.7f, -6.5f), new Vector3(.32f, 5.4f, .4f), goldMat);
        Box("Meta",new Vector3(7.36f,.055f,.1f),new Vector3(.25f,.065f,15.6f),goldMat);
        Box("Pasarela de salida",new Vector3(9.3f,.035f,.2f),new Vector3(3,.065f,16),woodMat);
        for(int lane=0;lane<5;lane++)
            Box("Banda de salida",new Vector3(-3,.025f,-6+lane*3),new Vector3(7,.02f,.09f),whiteMat);
        for (int i = 0; i < rosterSlots; i++) actors.Add(Actor(false));
    }

    private void BuildNarrativeRoom(int screen)
    {
        sceneCamera.backgroundColor = new Color(.018f, .024f, .05f);
        Box("Briefing Room Floor", new Vector3(0f, -.3f, 1f), new Vector3(19f, .6f, 21f), darkMat);
        Box("Muro de obrador",new Vector3(-2.5f,2.5f,9.6f),new Vector3(14,5,.5f),wallMat);
        PaintWall("Mural del obrador",new Vector3(-2.5f,2.5f,9.32f),new Vector2(13.6f,4.65f));
        Box("Pavimento del corredor",new Vector3(6,.01f,11.2f),new Vector3(3.2f,.1f,5.6f),floorMat);
        for(int side=-1;side<=1;side+=2)
            Box("Jamba del corredor",new Vector3(6+side*1.55f,1.9f,11.2f),new Vector3(.25f,3.8f,5.6f),wallMat);
        Box("Luz del acceso",new Vector3(6,3.7f,8.7f),new Vector3(3.35f,.2f,.3f),pinkMat);
        if (screen != Round) { actors.Add(Actor(false)); actors.Add(Actor(true)); actors.Add(Actor(true)); }
    }

    private void UpdateActors(int screen, int trial, int humanCount, float phase, float progress, float progress2, bool greenLight,
        int shotVictim, float jumpOne, float jumpTwo, bool movingOne, bool movingTwo, Vector2 playerOne, Vector2 playerTwo, Vector2[] bots,
        bool[] playerAlive, bool[] botAlive, bool[] bridgeSide, bool[] nightHostile, bool[] knownGlass, bool[,] safeBridge)
    {
        if (HasCarousel) return;
        if (screen == Kidnap && van != null)
        {
            van.transform.position = new Vector3(Mathf.Lerp(-9f, 0f, Mathf.Clamp01(phase / 5f)), 0f, 2f);
            for (int i = 0; i < actors.Count; i++) PlaceActor(actors[i], new Vector3(-2.6f + i * 1.05f, .04f, 0f + (i % 2) * 1.2f), i >= 4, false, 0f);
            return;
        }
        if ((screen == Round || screen == Result || screen == GameOver) && trial == RedLight)
        {
            if (signalLamp!=null) signalLamp.sharedMaterial=greenLight?greenMat:redMat;
            if (dollRenderer != null && dollFrames != null && dollFrames.Length > 0)
            {
                // Halfway through the sheet the doll has its back to the contestants.
                float targetFrame = greenLight ? dollFrames.Length / 2 : 0;
                dollFrame = Mathf.MoveTowards(dollFrame, targetFrame, Time.deltaTime * 24f);
                dollRenderer.sprite = dollFrames[Mathf.RoundToInt(dollFrame)];
            }
            if (actors.Count > 0) PlaceActor(actors[0], new Vector3(-2.3f, .05f, RedLightTrackZ(progress)), false, movingOne, 0f, IsAlive(playerAlive, 0), true);
            if (actors.Count > 1) PlaceActor(actors[1], new Vector3(2.3f, .05f, RedLightTrackZ(progress2)), false, movingTwo, 0f, IsAlive(playerAlive, 1), true);
            if (humanCount < 2 && actors.Count > 1) actors[1].SetActive(false);
            actors[0].GetComponent<SpriteRenderer>().color = IsAlive(playerAlive,0)?new Color(.76f,1f,1f):Color.gray;
            for (int i = 2; i < rosterSlots && i < actors.Count; i++)
            {
                int botIndex = i - 2;
                float botProgress = bots != null && botIndex < bots.Length ? bots[botIndex].x : 0f;
                float x = RedLightBotLane(botIndex);
                float z = Mathf.Lerp(-12f - (botIndex / 8)*1.4f,31f,botProgress/760f);
                PlaceActor(actors[i], new Vector3(x, .05f, z), false, greenLight, 0f, IsAlive(botAlive, botIndex),true);
            }
            for (int i = rosterSlots; i < actors.Count; i++) PlaceActor(actors[i], new Vector3(-5f + (i - rosterSlots) * 5f, .05f, 29.5f), true, shotVictim >= 0, 0f);
            if (shotTrail != null)
            {
                shotTrail.enabled = shotVictim >= 0;
                if (shotVictim >= 0)
                {
                    shotTrail.SetPosition(0, new Vector3(0f, 1.4f, 29.5f));
                    if (shotVictim == 1) shotTrail.SetPosition(1, new Vector3(2.3f, .9f, RedLightTrackZ(progress2)));
                    else if (shotVictim >= 2 && bots != null && shotVictim - 2 < bots.Length)
                    {
                        int bot = shotVictim - 2;
                        shotTrail.SetPosition(1, new Vector3(RedLightBotLane(bot), .9f, actors[shotVictim].transform.position.z));
                    }
                    else shotTrail.SetPosition(1, new Vector3(-2.3f, .9f, RedLightTrackZ(progress)));
                }
            }
        }
        else if ((screen == Round || screen == Result || screen == GameOver) && trial == Night)
        {
            for (int i=0;i<actors.Count;i++)
            {
                Vector2 pos=i==0?playerOne:i==1?playerTwo:bots[i-2];
                PlaceActor(actors[i],new Vector3(pos.x,.05f,pos.y),false,i==0?movingOne:i==1?movingTwo:true,0f,
                    i<2?i<humanCount:IsAlive(botAlive,i-2));
                if(i<humanCount) actors[i].GetComponent<SpriteRenderer>().color=i==0?new Color(.76f,1f,1f):new Color(1,.94f,.72f);
                else if(i>=2 && nightHostile[i-2]) actors[i].GetComponent<SpriteRenderer>().color=new Color(1,.78f,.50f);
            }
        }
        else if ((screen == Round || screen == Result || screen == GameOver) && trial == Cookie)
        {
            for (int i = 0; i < rosterSlots; i++)
            {
                int seat = i < 2 ? i + 3 : i < 5 ? i - 2 : i;
                Vector3 position = new Vector3(-6.3f + seat%8*1.8f,.05f,-3.8f + seat/8*3.1f);
                PlaceActor(actors[i], position, false, false, 0, i < 2 ? i < humanCount : IsAlive(botAlive,i-2));
            }
            PlaceActor(actors[rosterSlots], new Vector3(-8.2f,.05f,0), true, false, 0);
            PlaceActor(actors[rosterSlots+1], new Vector3(8.2f,.05f,0), true, false, 0);
        }
        else if ((screen == Round || screen == Result || screen == GameOver) && trial == Tug)
        {
            float pullTarget = Mathf.Lerp(.88f,.12f,Mathf.Clamp01(progress/100f));
            float markerT = ropeCenter = Mathf.SmoothDamp(ropeCenter,pullTarget,ref ropeSpeed,.17f);
            float sag = .12f + Mathf.Clamp01(1f - Mathf.Abs(progress - 50f) / 50f) * .15f + Mathf.Min(.15f,Mathf.Abs(ropeSpeed)*.25f);
            float markerX = Mathf.Lerp(-tugEnd,tugEnd,markerT);
            float markerY = RopeHeight(markerT, sag, animClock);
            if (ropeMarker != null) ropeMarker.transform.localPosition = new Vector3(markerX, markerY + .24f, 1.5f);
            UpdateRope(tugRope, sag, animClock, 0f, 0f);
            UpdateRope(tugRopeFiberA, sag, animClock, .085f, 1.2f);
            UpdateRope(tugRopeFiberB, sag, animClock, -.085f, 3.4f);
            int leftRank=0,rightRank=0;
            for (int i = 0; i < actors.Count; i++)
            {
                bool left=i<2 || i%2==0;
                if(i<2?i>=humanCount:!IsAlive(botAlive,i-2)){actors[i].SetActive(false);continue;}
                int rank=left?leftRank++:rightRank++;
                float effort=Mathf.Sin(animClock*5.5f-rank*.12f);
                float x=(1.65f+rank*1.28f+effort*.075f)*(left?-1:1);
                PlaceActor(actors[i],new Vector3(x,2.76f,1.63f),false,false,0);
                var sr=actors[i].GetComponent<SpriteRenderer>();
                sr.sprite=effort>0?playerWalkA:playerWalkB;
                sr.flipX=!left;
            }
        }
        else if ((screen == Round || screen == Result || screen == GameOver) && trial == Bridge)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                int step = Mathf.Clamp(Mathf.RoundToInt(i==0 ? progress : i==1 ? progress2 : bots[i-2].x),0,8);
                float side = i<2 ? (bridgeSide[i]?1:-1) : bots[i-2].y>.5f?1:-1;
                float offset = i<2?0f:((i-2)%5-2)*.23f;
                Vector3 landing=new Vector3(side+offset,2.57f,-8.05f+step*2.05f);
                if(jumpEnd[i]!=landing)
                {
                    bool first=jumpEnd[i]==Vector3.zero;
                    jumpStart[i]=first?landing:actors[i].transform.position;
                    jumpEnd[i]=landing;
                    jumpBlend[i]=first?1f:0f;
                }
                jumpBlend[i]=Mathf.MoveTowards(jumpBlend[i],1f,Time.deltaTime*3.7f);
                float t=jumpBlend[i];
                var feet=Vector3.Lerp(jumpStart[i],landing,t)+Vector3.up*(3.8f*t*(1-t));
                PlaceActor(actors[i],feet,false,t<1,0f,i<2?i<humanCount:IsAlive(botAlive,i-2));
            }
        }
        else if ((screen == Round || screen == Result || screen == GameOver || screen == Ending) && trial == Final)
        {
            float leapOne = jumpOne > 0f ? Mathf.Sin(Mathf.Clamp01(1f - jumpOne / .58f) * Mathf.PI) * 1.25f : 0f;
            float leapTwo = jumpTwo > 0f ? Mathf.Sin(Mathf.Clamp01(1f - jumpTwo / .58f) * Mathf.PI) * 1.25f : 0f;
            if (actors.Count > 0) PlaceActor(actors[0], new Vector3(Mathf.Clamp(playerOne.x / 55f, -8.3f, 8.1f), .05f + leapOne, Mathf.Clamp(playerOne.y / 26f, -6.75f, 6.75f)), false, movingOne, 0f, IsAlive(playerAlive, 0), true);
            if (actors.Count > 1) PlaceActor(actors[1], new Vector3(Mathf.Clamp(playerTwo.x / 55f, -8.3f, 8.1f), .05f + leapTwo, Mathf.Clamp(playerTwo.y / 26f, -6.75f, 6.75f)), false, movingTwo, 0f, IsAlive(playerAlive, 1), true);
            if (humanCount < 2 && actors.Count > 1) actors[1].SetActive(false);
            actors[0].GetComponent<SpriteRenderer>().color = IsAlive(playerAlive,0)?new Color(.76f,1f,1f):Color.gray;
            for (int i = 2; i < actors.Count; i++)
            {
                Vector2 pos = bots != null && bots.Length > 0 ? bots[(i - 2) % bots.Length] : Vector2.zero;
                PlaceActor(actors[i], new Vector3(Mathf.Clamp(pos.x / 55f, -8.3f, 8.1f), .05f, Mathf.Clamp(pos.y / 26f, -6.75f, 6.75f)), false, true, 0f, IsAlive(botAlive,i-2));
            }
        }
        else if (screen == Vote)
        {
            for(int i=0;i<actors.Count;i++) PlaceActor(actors[i],new Vector3(-6f+(i%8)*1.7f,.05f,-4f+(i/8)*2.3f),false,false,0,i<2?i<humanCount:IsAlive(botAlive,i-2));
        }
        else if (screen != Round)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                float x = -6.8f + (i % 7) * 2.2f;
                float z = -3f + (i / 7) * 2.8f;
                PlaceActor(actors[i], new Vector3(x, .05f, z), i == actors.Count - 1, false, 0f);
            }
        }
        if (screen == Round && trial == Bridge)
        {
            for (int i = 0; i < bridgeLeft.Count; i++)
            {
                if (!knownGlass[i]) continue;
                GameObject safe = safeBridge[i, 0] ? bridgeLeft[i] : bridgeRight[i];
                GameObject cracked = safeBridge[i, 0] ? bridgeRight[i] : bridgeLeft[i];
                SetMaterial(safe, greenMat);
                SetMaterial(cracked, redMat);
            }
        }
    }

    private void BuildDoll()
    {
        var pivot = new GameObject("Doll Turning Pivot");
        pivot.transform.SetParent(stageRoot,false);pivot.transform.localPosition=new Vector3(0,.04f,34.6f);
        doll = pivot;
        pivot.transform.localScale = Vector3.one * 1.5f;
        dollRenderer = pivot.AddComponent<SpriteRenderer>();
        if (dollFrames != null && dollFrames.Length > 0)
        {
            dollFrame = dollFrames.Length / 2;
            dollRenderer.sprite = dollFrames[(int)dollFrame];
        }
        dollRenderer.sortingOrder = 24;
    }

    private GameObject Actor(bool guard)
    {
        GameObject actor = new GameObject(guard ? "Red Guard Sprite" : "Green Player Sprite");
        actor.transform.SetParent(stageRoot, false);
        SpriteRenderer renderer = actor.AddComponent<SpriteRenderer>();
        renderer.sprite = guard ? guardSprite : playerSprite;
        renderer.sortingOrder = 10;
        actor.transform.localScale = Vector3.one * .72f;
        return actor;
    }

    private GameObject SpriteActor(bool guard, Vector3 position, float height)
    {
        var actor = new GameObject(guard ? "Guard" : "Player");
        actor.transform.SetParent(stageRoot, false);
        actor.transform.localPosition = position;
        actor.transform.localScale = Vector3.one * (height / 2.1f);
        var renderer = actor.AddComponent<SpriteRenderer>();
        renderer.sprite = guard ? guardSprite : playerSprite;
        renderer.sortingOrder = 12;
        return actor;
    }

    private void PlaceActor(GameObject actor, Vector3 position, bool guard, bool walking, float yaw, bool alive = true, bool showDead = false)
    {
        if (actor == null) return;
        actor.SetActive(alive || showDead);
        if (!alive && !showDead) return;
        if (!poses.TryGetValue(actor,out var pose))
        {
            pose=new ArenaCharacterPose();
            poses.Add(actor,pose);
        }
        float floor=lastTrial==Final?.05f:lastTrial==Bridge?2.57f:position.y;
        pose.Draw(actor.transform,position,floor,alive,walking,guard,yaw,Time.deltaTime,characterArt,playerSprite,playerWalkA,playerWalkB,armedGuardSprite);
    }

    private static bool IsAlive(bool[] states, int index) => states == null || index >= states.Length || states[index];

    private static float RedLightTrackZ(float progress) => Mathf.Lerp(-12f, 31f, Mathf.Clamp01(progress / 760f));

    private static float RedLightBotLane(int botIndex) => ((botIndex % 8) - 3.5f) * 2.1f;

    private void AimCamera(Vector3 target, bool smooth = false, float framing = 1f)
    {
        var dest=target-ArenaView.Rotation*Vector3.forward*36f;
        sceneCamera.transform.SetPositionAndRotation(smooth && cameraReady ?
            Vector3.SmoothDamp(sceneCamera.transform.position,dest,ref cameraVelocity,.20f) : dest,ArenaView.Rotation);
        sceneCamera.orthographicSize=smooth && cameraReady ? Mathf.SmoothDamp(sceneCamera.orthographicSize,framing,ref sizeVelocity,.22f) : framing;
        sceneCamera.transparencySortMode=TransparencySortMode.Orthographic;
        cameraReady=true;
    }

    private LineRenderer CreateRopeLine(string objectName, float width, Color color, Material material)
    {
        var ropeObject = new GameObject(objectName);
        ropeObject.transform.SetParent(stageRoot, false);
        var line = ropeObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 33;
        line.startWidth = width;
        line.endWidth = width * .82f;
        line.numCapVertices = 5;
        line.numCornerVertices = 3;
        line.material = material;
        line.startColor = color;
        line.endColor = color;
        return line;
    }

    private static float RopeHeight(float t, float sag, float clock)
    {
        float bow = 4f * t * (1f - t);
        return 3.57f - bow * sag + Mathf.Sin(t * Mathf.PI * 3f + clock * 5f) * .025f * bow;
    }

    private void UpdateRope(LineRenderer line, float sag, float clock, float strandOffset, float strandPhase)
    {
        if (line == null) return;
        const int points = 33;
        for (int i = 0; i < points; i++)
        {
            float t = i / (float)(points - 1);
            float x = Mathf.Lerp(-tugEnd,tugEnd,t);
            float y = RopeHeight(t, sag, clock) + Mathf.Sin(t * Mathf.PI * 24f + clock * 4f + strandPhase) * strandOffset * .5f;
            float z = 1.5f + Mathf.Cos(t * Mathf.PI * 24f + clock * 4f + strandPhase) * strandOffset;
            line.SetPosition(i, new Vector3(x, y, z));
        }
    }

    private GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name; item.transform.SetParent(parent != null ? parent : stageRoot, false);
        item.transform.localPosition = position; item.transform.localScale = size;
        SetMaterial(item, material);
        return item;
    }

    private GameObject Cylinder(string name, Vector3 position, float radius, float height, Material material, Transform parent = null, Vector3? rotation = null)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        item.name = name; item.transform.SetParent(parent != null ? parent : stageRoot, false);
        item.transform.localPosition = position; item.transform.localScale = new Vector3(radius * 2f, height * .5f, radius * 2f);
        if (rotation.HasValue) item.transform.localRotation = Quaternion.Euler(rotation.Value);
        SetMaterial(item, material);
        return item;
    }

    private GameObject Sphere(string name, Vector3 position, float size, Material material, Transform parent = null)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = name; item.transform.SetParent(parent != null ? parent : stageRoot, false); item.transform.localPosition = position; item.transform.localScale = Vector3.one * size;
        SetMaterial(item, material); return item;
    }

    private void AddLight(string name, Vector3 position, float intensity, float range)
    {
        var go = new GameObject(name); go.transform.SetParent(stageRoot, false); go.transform.position = position;
        var light = go.AddComponent<Light>(); light.type = LightType.Point; light.color = WarmLight; light.intensity = intensity; light.range = range;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        return material;
    }

    private static Material MakeGlassMaterial(Color color)
    {
        Material material = MakeMaterial(color);
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
        return material;
    }

    private static Sprite LoadSprite(string name)
    {
        Texture2D texture = Resources.Load<Texture2D>("Characters/" + name);
        return texture != null ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .05f), 100f) : null;
    }

    private static Sprite[] LoadDollFrames()
    {
        var frames = new List<Sprite>();
        for (int i = 0; i < 32; i++)
        {
            Texture2D texture = Resources.Load<Texture2D>("Characters/Doll/Doll_" + i.ToString("D2"));
            if (texture == null) break;
            texture.filterMode = FilterMode.Point;
            frames.Add(Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .025f), 64f));
        }
        return frames.ToArray();
    }

    private static void SetMaterial(GameObject item, Material material)
    {
        var renderer = item.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material;
    }

}
