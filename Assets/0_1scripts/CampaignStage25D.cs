using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds the campaign arenas as small, perspective 3D sets while keeping play on a readable plane.</summary>
public sealed class CampaignStage25D : MonoBehaviour
{
    private const int Title = 0, Kidnap = 1, Round = 2, Result = 3, Insider = 4, Betrayal = 5, Ending = 6, GameOver = 7;
    private const int RedLight = 0, Tug = 1, Bridge = 2, Final = 3;
    private readonly List<GameObject> actors = new List<GameObject>();
    private Transform stageRoot;
    private Camera sceneCamera;
    private Material floorMat, roomFloorMat, wallMat, darkMat, greenMat, grassMat, redMat, pinkMat, blueMat, glassMat, goldMat, whiteMat, woodMat, skinMat;
    private Sprite playerSprite, playerWalkA, playerWalkB, guardSprite, armedGuardSprite;
    private Sprite dollSprite;
    private GameObject ropeMarker, doll, van;
    private LineRenderer shotTrail, tugRope, tugRopeFiberA, tugRopeFiberB;
    private readonly List<GameObject> bridgeLeft = new List<GameObject>();
    private readonly List<GameObject> bridgeRight = new List<GameObject>();
    private int lastScreen = -1, lastTrial = -1;
    private float animClock;
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
            sceneCamera.orthographic = false;
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
        if (stageRoot != null) Destroy(stageRoot.gameObject);
        Material[] materials = { floorMat, roomFloorMat, wallMat, darkMat, greenMat, grassMat, redMat, pinkMat, blueMat, glassMat, goldMat, whiteMat, woodMat, skinMat };
        foreach (Material material in materials) if (material != null) Destroy(material);
        Sprite[] sprites = { playerSprite, playerWalkA, playerWalkB, guardSprite, armedGuardSprite, dollSprite };
        foreach (Sprite sprite in sprites) if (sprite != null) Destroy(sprite);
    }

    public void Present(int screen, int trial, int humanCount, float phase, float progress, float progress2, bool greenLight,
        int shotVictim, float jumpOne, float jumpTwo, bool movingOne, bool movingTwo, Vector2 playerOne, Vector2 playerTwo, Vector2[] bots,
        bool[] playerAlive, bool[] botAlive, bool trust, bool betrayed)
    {
        trial = Mathf.Clamp(trial, 0, 3);
        if (screen != lastScreen || trial != lastTrial) BuildStage(screen, trial);
        if (sceneCamera != null)
        {
            Vector3 target = trial == Tug ? new Vector3(0f, 2.4f, 1.5f) : new Vector3(0f, 1.3f, 1.5f);
            bool followPlayer = screen == Round && (trial == RedLight || trial == Final);
            if (followPlayer && trial == RedLight)
                target.x = (-8.2f + Mathf.Clamp01(progress / 760f) * 16.4f) * .62f;
            else if (followPlayer && trial == Final)
            {
                target.x = Mathf.Clamp(playerOne.x / 55f, -8.3f, 8.1f) * .62f;
                target.z += Mathf.Clamp(playerOne.y / 20f, -5.8f, 7f) * .55f;
            }
            AimCamera(target, followPlayer);
        }
        UpdateActors(screen, trial, humanCount, phase, progress, progress2, greenLight, shotVictim, jumpOne, jumpTwo, movingOne, movingTwo,
            playerOne, playerTwo, bots, playerAlive, botAlive, trust, betrayed);
    }

    private void BuildStage(int screen, int trial)
    {
        lastScreen = screen;
        lastTrial = trial;
        foreach (Transform child in stageRoot) Destroy(child.gameObject);
        actors.Clear(); bridgeLeft.Clear(); bridgeRight.Clear(); ropeMarker = doll = van = null;
        shotTrail = tugRope = tugRopeFiberA = tugRopeFiberB = null;
        AddLight("Stage Key Light", new Vector3(-5f, 11f, -7f), 1.15f, 15f);
        AddLight("Warm Fill", new Vector3(6f, 8f, 5f), .75f, 11f);
        if (screen == Kidnap) { BuildKidnap(); return; }
        if (screen == Round)
        {
            switch (trial)
            {
                case RedLight: BuildRedLight(); break;
                case Tug: BuildTug(); break;
                case Bridge: BuildBridge(); break;
                default: BuildFinal(); break;
            }
        }
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
        Box("Left Wall", new Vector3(-9.8f, 2.8f, 1f), new Vector3(.5f, 6f, 22f), wallMat);
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
        Box("Long Grass Field", new Vector3(0f, -.35f, 2.2f), new Vector3(22f, .7f, 24f), grassMat);
        for (int side = 0; side < 2; side++)
            for (int i = 0; i < 7; i++)
            {
                if (side == 1 && i == 3) continue;
                float x = side == 0 ? -10f : 10f;
                float z = -7f + i * 3f;
                Cylinder("Tree Trunk", new Vector3(x, .9f, z), .26f, 2.5f, woodMat);
                Sphere("Tree Canopy", new Vector3(x, 2.5f, z), 1.35f, i % 2 == 0 ? greenMat : grassMat);
            }
        Box("Start Line", new Vector3(-8.3f, .03f, 1.5f), new Vector3(.14f, .06f, 19f), whiteMat);
        Box("Finish Line", new Vector3(8.3f, .03f, 1.5f), new Vector3(.18f, .06f, 19f), whiteMat);
        Box("Finish Arch", new Vector3(8.55f, 3.5f, 1.5f), new Vector3(.3f, .28f, 9f), pinkMat);
        Box("Finish Arch Left Post", new Vector3(8.55f, 1.8f, -2.8f), new Vector3(.3f, 3.5f, .3f), pinkMat);
        Box("Finish Arch Right Post", new Vector3(8.55f, 1.8f, 5.8f), new Vector3(.3f, 3.5f, .3f), pinkMat);
        Box("Doll Stand", new Vector3(9.65f, .45f, 1.5f), new Vector3(1.5f, .9f, 1.1f), woodMat);
        BuildDoll();
        for (int i = 0; i < 13; i++) actors.Add(Actor(false));
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
            float x = i == 0 ? -5f : 5f;
            Box("Floating Team Platform", new Vector3(x, 2.4f, 1.5f), new Vector3(8.2f, .65f, 7.8f), i == 0 ? blueMat : redMat);
            for (int p = 0; p < 4; p++)
                Cylinder("Platform Support", new Vector3(x - 2.8f + p * 1.85f, -1.1f, i == 0 ? -1f : 4f), .16f, 6f, whiteMat);
        }
        tugRope = CreateRopeLine("Tug Rope", .19f, new Color(.34f, .19f, .12f), woodMat);
        tugRopeFiberA = CreateRopeLine("Tug Rope Twist A", .045f, new Color(.61f, .39f, .23f), woodMat);
        tugRopeFiberB = CreateRopeLine("Tug Rope Twist B", .045f, new Color(.20f, .11f, .08f), woodMat);
        ropeMarker = Box("Rope Marker", new Vector3(0f, 4.15f, 1.5f), new Vector3(.34f, .52f, .38f), goldMat);
        Box("Left Anchor", new Vector3(-9.2f, 3.6f, 1.5f), new Vector3(.6f, 3f, .7f), whiteMat);
        Box("Right Anchor", new Vector3(9.2f, 3.6f, 1.5f), new Vector3(.6f, 3f, .7f), whiteMat);
        for (int i = 0; i < 6; i++) actors.Add(Actor(false));
        for (int i = 0; i < 6; i++) actors.Add(Actor(false));
    }

    private void BuildBridge()
    {
        sceneCamera.backgroundColor = new Color(.012f, .018f, .055f);
        Box("Bridge Void Base", new Vector3(0f, -5f, 3f), new Vector3(22f, .4f, 30f), darkMat);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 5; i++)
                Cylinder("Bridge Suspension Cable", new Vector3(side * 2.6f, 4.8f, -6f + i * 3.6f), .045f, 11f, glassMat);
        for (int i = 0; i < 8; i++)
        {
            float z = -6f + i * 2.05f;
            GameObject left = Box("Left Glass Panel " + i, new Vector3(-1.05f, 2.4f, z), new Vector3(1.9f, .22f, 1.78f), glassMat);
            GameObject right = Box("Right Glass Panel " + i, new Vector3(1.05f, 2.4f, z), new Vector3(1.9f, .22f, 1.78f), whiteMat);
            bridgeLeft.Add(left); bridgeRight.Add(right);
        }
        actors.Add(Actor(false)); actors.Add(Actor(false)); actors.Add(Actor(true)); actors.Add(Actor(true));
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
        Box("Goal Beam", new Vector3(6.9f, 5.2f, -6.5f), new Vector3(2.5f, .25f, .4f), goldMat);
        for (int i = 0; i < 13; i++) actors.Add(Actor(false));
    }

    private void BuildNarrativeRoom(int screen)
    {
        sceneCamera.backgroundColor = new Color(.018f, .024f, .05f);
        Box("Briefing Room Floor", new Vector3(0f, -.3f, 1f), new Vector3(19f, .6f, 21f), darkMat);
        Box("Briefing Wall", new Vector3(0f, 3.3f, 9.6f), new Vector3(19f, 7f, .5f), wallMat);
        Box("Door Frame", new Vector3(6f, 2f, 9.1f), new Vector3(2f, 4f, .5f), pinkMat);
        Box("Door Opening", new Vector3(6f, 1.8f, 8.8f), new Vector3(1.5f, 3.6f, .15f), darkMat);
        actors.Add(Actor(false)); actors.Add(Actor(true)); actors.Add(Actor(true));
    }

    private void UpdateActors(int screen, int trial, int humanCount, float phase, float progress, float progress2, bool greenLight,
        int shotVictim, float jumpOne, float jumpTwo, bool movingOne, bool movingTwo, Vector2 playerOne, Vector2 playerTwo, Vector2[] bots,
        bool[] playerAlive, bool[] botAlive, bool trust, bool betrayed)
    {
        if (screen == Kidnap && van != null)
        {
            van.transform.position = new Vector3(Mathf.Lerp(-9f, 0f, Mathf.Clamp01(phase / 5f)), 0f, 2f);
            for (int i = 0; i < actors.Count; i++) PlaceActor(actors[i], new Vector3(-2.6f + i * 1.05f, .04f, 0f + (i % 2) * 1.2f), i >= 4, false, 0f);
            return;
        }
        if ((screen == Round || screen == Result || screen == GameOver) && trial == RedLight)
        {
            if (doll != null) doll.transform.localRotation = Quaternion.Euler(0f, greenLight ? 0f : 180f, 0f);
            if (actors.Count > 0) PlaceActor(actors[0], new Vector3(-8.2f + Mathf.Clamp01(progress / 760f) * 16.4f, .05f, -4f), false, movingOne, 0f, IsAlive(playerAlive, 0), true);
            if (actors.Count > 1) PlaceActor(actors[1], new Vector3(-8.2f + Mathf.Clamp01(progress2 / 760f) * 16.4f, .05f, -2.5f), false, movingTwo, 0f, IsAlive(playerAlive, 1), true);
            if (humanCount < 2 && actors.Count > 1) actors[1].SetActive(false);
            for (int i = 2; i < 13 && i < actors.Count; i++)
            {
                int botIndex = i - 2;
                float botX = bots != null && botIndex < bots.Length ? bots[botIndex].x : 0f;
                float x = -8.2f + Mathf.Clamp01(botX / 760f) * 16.4f;
                PlaceActor(actors[i], new Vector3(x, .05f, -1.35f + (i % 3) * .62f), false, greenLight, 0f, IsAlive(botAlive, botIndex));
            }
            for (int i = 13; i < actors.Count; i++) PlaceActor(actors[i], new Vector3(-5f + (i - 13) * 5f, .05f, 7.5f), true, shotVictim >= 0, 0f);
            if (shotTrail != null)
            {
                shotTrail.enabled = shotVictim >= 0;
                if (shotVictim >= 0)
                {
                    shotTrail.SetPosition(0, new Vector3(0f, 1.4f, 7.2f));
                    if (shotVictim == 1) shotTrail.SetPosition(1, new Vector3(-8.2f + Mathf.Clamp01(progress2 / 760f) * 16.4f, .9f, -2.5f));
                    else if (shotVictim >= 2 && bots != null && shotVictim - 2 < bots.Length)
                    {
                        int bot = shotVictim - 2;
                        float x = -8.2f + Mathf.Clamp01(bots[bot].x / 760f) * 16.4f;
                        shotTrail.SetPosition(1, new Vector3(x, .9f, -1.35f + ((bot + 2) % 3) * .62f));
                    }
                    else shotTrail.SetPosition(1, new Vector3(-8.2f + Mathf.Clamp01(progress / 760f) * 16.4f, .9f, -4f));
                }
            }
        }
        else if ((screen == Round || screen == Result || screen == GameOver) && trial == Tug)
        {
            float markerT = Mathf.Lerp(.12f, .88f, Mathf.Clamp01(progress / 100f));
            float sag = .12f + Mathf.Clamp01(1f - Mathf.Abs(progress - 50f) / 50f) * .15f;
            float markerX = Mathf.Lerp(-4.9f, 4.9f, markerT);
            float markerY = RopeHeight(markerT, sag, animClock);
            if (ropeMarker != null) ropeMarker.transform.localPosition = new Vector3(markerX, markerY + .24f, 1.5f);
            UpdateRope(tugRope, sag, animClock, 0f, 0f);
            UpdateRope(tugRopeFiberA, sag, animClock, .085f, 1.2f);
            UpdateRope(tugRopeFiberB, sag, animClock, -.085f, 3.4f);
            for (int i = 0; i < actors.Count; i++)
            {
                bool left = i < 6; int slot = left ? i : i - 6;
                float lean = Mathf.Sin(animClock * 7f + slot * .4f) * .09f;
                PlaceActor(actors[i], new Vector3(left ? -7f + slot * .9f : 7f - slot * .9f, 2.8f + lean, .5f + slot * .35f), false, Mathf.Sin(animClock * 7f + slot) > 0f, left ? -8f : 8f);
            }
        }
        else if ((screen == Round || screen == Result || screen == GameOver) && trial == Bridge)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                int step = i == 0 ? Mathf.Clamp(Mathf.RoundToInt(progress), 0, 8) : i == 1 ? Mathf.Clamp(Mathf.RoundToInt(progress2), 0, 8) : Mathf.Min(8, Mathf.FloorToInt(phase * .12f));
                PlaceActor(actors[i], new Vector3(i % 2 == 0 ? -1.05f : 1.05f, 2.57f, -6f + step * 2.05f), false, i >= 2, 0f);
            }
        }
        else if ((screen == Round || screen == Result || screen == GameOver || screen == Ending) && trial == Final)
        {
            float leapOne = jumpOne > 0f ? Mathf.Sin(Mathf.Clamp01(1f - jumpOne / .58f) * Mathf.PI) * 1.25f : 0f;
            float leapTwo = jumpTwo > 0f ? Mathf.Sin(Mathf.Clamp01(1f - jumpTwo / .58f) * Mathf.PI) * 1.25f : 0f;
            if (actors.Count > 0) PlaceActor(actors[0], new Vector3(Mathf.Clamp(playerOne.x / 55f, -8.3f, 8.1f), .05f + leapOne, Mathf.Clamp(playerOne.y / 20f, -5.8f, 7f)), false, movingOne, 0f, IsAlive(playerAlive, 0), true);
            if (actors.Count > 1) PlaceActor(actors[1], new Vector3(Mathf.Clamp(playerTwo.x / 55f, -8.3f, 8.1f), .05f + leapTwo, Mathf.Clamp(playerTwo.y / 20f, -5.8f, 7f)), false, movingTwo, 0f, IsAlive(playerAlive, 1), true);
            if (humanCount < 2 && actors.Count > 1) actors[1].SetActive(false);
            for (int i = 2; i < actors.Count; i++)
            {
                Vector2 pos = bots != null && bots.Length > 0 ? bots[(i - 2) % bots.Length] : Vector2.zero;
                PlaceActor(actors[i], new Vector3(Mathf.Clamp(pos.x / 55f, -8.3f, 8.1f), .05f, Mathf.Clamp(pos.y / 20f, -5.8f, 7f)), false, true, 0f);
            }
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
                bridgeLeft[i].transform.localScale = new Vector3(1.9f, .22f, 1.78f);
                bridgeRight[i].transform.localScale = new Vector3(1.9f, .22f, 1.78f);
                if (i < Mathf.Max(progress, progress2))
                {
                    SetMaterial(bridgeLeft[i], greenMat);
                    SetMaterial(bridgeRight[i], greenMat);
                }
            }
        }
    }

    private void BuildDoll()
    {
        var pivot = new GameObject("Doll Turning Pivot");
        pivot.transform.SetParent(stageRoot, false); pivot.transform.localPosition = new Vector3(9.65f, 0f, 1.5f);
        doll = pivot;
        Box("Doll Red Dress", new Vector3(0f, 1.3f, 0f), new Vector3(1.25f, 1.9f, .75f), pinkMat, pivot.transform);
        Sphere("Doll Face", new Vector3(0f, 2.65f, -.05f), .82f, skinMat, pivot.transform);
        Sphere("Doll Hair", new Vector3(0f, 3.03f, 0f), .73f, woodMat, pivot.transform);
        Box("Doll Hair Back", new Vector3(0f, 2.46f, .4f), new Vector3(1.1f, 1.5f, .28f), woodMat, pivot.transform);
        Box("Doll Eye L", new Vector3(-.16f, 2.68f, -.48f), new Vector3(.09f, .1f, .035f), darkMat, pivot.transform);
        Box("Doll Eye R", new Vector3(.16f, 2.68f, -.48f), new Vector3(.09f, .1f, .035f), darkMat, pivot.transform);
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
        actor.transform.localPosition = position;
        if (alive && !walking) actor.transform.localPosition += Vector3.up * Mathf.Sin(animClock * 2.6f + position.x * .7f) * .018f;
        actor.transform.localRotation = Quaternion.Euler(0f, yaw, alive ? 0f : 78f);
        var renderer = actor.GetComponent<SpriteRenderer>();
        if (renderer == null) return;
        renderer.sprite = guard ? armedGuardSprite : walking ? ((Mathf.FloorToInt(animClock * 9f) & 1) == 0 ? playerWalkA : playerWalkB) : playerSprite;
        renderer.color = alive ? Color.white : new Color(.52f, .52f, .56f);
        renderer.sortingOrder = Mathf.RoundToInt((20f - position.z) * 2f);
    }

    private static bool IsAlive(bool[] states, int index) => states == null || index >= states.Length || states[index];

    private void AimCamera(Vector3 target, bool smooth = false)
    {
        Vector3 desiredPosition = target + new Vector3(0f, 9.2f, -21f);
        if (smooth && cameraReady)
            sceneCamera.transform.position = Vector3.SmoothDamp(sceneCamera.transform.position, desiredPosition, ref cameraVelocity, .32f);
        else
        {
            sceneCamera.transform.position = desiredPosition;
            cameraVelocity = Vector3.zero;
        }
        sceneCamera.transform.rotation = Quaternion.LookRotation(target - sceneCamera.transform.position, Vector3.up);
        cameraReady = true;
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
        return 4.3f - bow * sag + Mathf.Sin(t * Mathf.PI * 3f + clock * 5f) * .025f * bow;
    }

    private static void UpdateRope(LineRenderer line, float sag, float clock, float strandOffset, float strandPhase)
    {
        if (line == null) return;
        const int points = 33;
        for (int i = 0; i < points; i++)
        {
            float t = i / (float)(points - 1);
            float x = Mathf.Lerp(-4.9f, 4.9f, t);
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

    private static void SetMaterial(GameObject item, Material material)
    {
        var renderer = item.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material;
    }

}
