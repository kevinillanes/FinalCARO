using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class StageLayoutReview
{
    static StageLayoutReview(){ EditorApplication.update+=Tick; }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/LaUltimaRonda.unity");
        SessionState.SetBool("StageLayoutReview",true);
        EditorApplication.EnterPlaymode();
    }
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,Private).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Private).Invoke(o,a);
    static void Check(bool ok,string msg){if(!ok) throw new Exception(msg);}
    static readonly int[] PreviewTrials = {0,2,4};
    static readonly string[] PreviewNames = {"Meta","Carrusel","Cuerda"};
    static int preview = -1, captureAfter;

    static void PreparePreview(PixelSurvivalGame game)
    {
        Call(game,"StartGame",2);
        var alive = Read<bool[]>(game,"botRunnerAlive");
        // Deliberately leave only even roster indices alive to check team assignment.
        for (int i=0;i<alive.Length;i++) alive[i] = i%2==0;
        Call(game,"BeginTrial",PreviewTrials[preview]);
        if (preview==0)
        {
            var runner=Read<float[]>(game,"runner");
            runner[0]=runner[1]=730;
            game.GetType().GetField("trialTime",Private).SetValue(game,8f);
        }
        if (preview==1)
        {
            Call(game,"OpenCarouselDoors");
            for(int step=0;step<22;step++) Call(game,"UpdateCarouselBots",.05f);
        }
        if (preview==2)
        {
            int allies=2+Read<bool[]>(game,"tugAllies").Count(a=>a);
            Check(Math.Abs(2*allies-(2+alive.Count(a=>a)))<=1,"Uneven tug teams after eliminations");
        }
        Call(game,"LateUpdate");
        if(preview==2) Read<CampaignStage25D>(game,"stage25D").PlayTugPull(0,true,1);
        captureAfter=Time.frameCount+6;
    }

    static void CaptureArena(string name)
    {
        var camera=Camera.main;
        var oldTarget=camera.targetTexture;
        var oldActive=RenderTexture.active;
        var target=RenderTexture.GetTemporary(960,540,24);
        var texture=new Texture2D(960,540,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;
            camera.Render();
            RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,960,540),0,0);
            texture.Apply();
            System.IO.File.WriteAllBytes("Logs/Actual_"+name+".png",texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=oldTarget;
            RenderTexture.active=oldActive;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.Destroy(texture);
        }
    }
    static void Tick()
    {
        if(!Application.isPlaying||Time.frameCount<5) return;
        if(preview<0 && !SessionState.GetBool("StageLayoutReview",false)) return;
        SessionState.SetBool("StageLayoutReview",false);
        try
        {
            var g=UnityEngine.Object.FindFirstObjectByType<PixelSurvivalGame>();
            if(preview>=0)
            {
                if(Time.frameCount<captureAfter) return;
                CaptureArena(PreviewNames[preview]);
                if(++preview<PreviewTrials.Length) PreparePreview(g);
                else
                {
                    preview=-1;
                    Debug.Log("STAGE_LAYOUT_PASS 18 carousel configurations, balanced tug, doll, menu, cookies and camera previews");
                    EditorApplication.Exit(0);
                }
                return;
            }
            Validate(g);
            preview=0;
            PreparePreview(g);
        }
        catch(Exception ex){Debug.LogError("STAGE_LAYOUT_FAIL "+ex);EditorApplication.Exit(1);}
    }
    static void ValidateCookieTracing(PixelSurvivalGame game)
    {
        var previousMouse = Mouse.current;
        var focusRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
        var background = InputSystem.settings.backgroundBehavior;
        // A batch-mode Play session has no focused Game view to receive pointer events.
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var mouse = InputSystem.AddDevice<Mouse>();
        void Move(Vector2 point, bool held, float dt = .03f)
        {
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            Vector2 center = new Vector2(Screen.width, Screen.height) * .5f;
            Vector2 screenPoint = center + new Vector2(point.x - 480f, 270f - point.y) * scale;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPoint }.WithButton(MouseButton.Left, held));
            InputSystem.Update();
            Call(game, "UpdateCookie", dt);
        }
        try
        {
            int shapes = 0;
            for (int round = 0; round < 4; round++)
            {
                int players = 1 + round % 2;
                Call(game, "StartGame", players);
                Call(game, "BeginTrial", 1);
                shapes |= 1 << Read<int>(game, "cookieShape");
                var path = Read<Vector2[]>(game, "cookiePath");
                for (int p = 0; p < players; p++)
                {
                    Check(Read<int>(game, "cookiePlayer") == p, "Cookie turn handoff");
                    var fractures = Read<Vector2[][]>(game, "cookieFractures");
                    Check(fractures.All(f => f == null), "Cracks carried into a fresh cookie");
                    Move(path[0], false, 60f);
                    Check(Read<float>(game, "cookieTimeLeft") == 42f, "Clock ran before pickup");
                    Move(path[path.Length / 2], true);
                    Check(!Read<bool>(game, "cookieStarted"), "Started away from the marked point");
                    Move(path[0], false);
                    Move(path[0], true);
                    Check(Read<bool>(game, "cookieTracing"),
                        $"Mouse missed start {path[0]} at {mouse.position.ReadValue()}, screen {Screen.width}x{Screen.height}, current {Mouse.current == mouse}, held {mouse.leftButton.isPressed}, edge {mouse.leftButton.wasPressedThisFrame}");
                    Move(path[0], true);
                    Check(Read<int>(game, "cookieIndex") == 0, "Stationary mouse advanced");
                    Move(path[path.Length / 2], true);
                    Move(path[path.Length / 2], true);
                    Check(Read<int[]>(game, "cracks")[p] == 1, "Shortcut should cause exactly one crack");
                    Check(fractures[0]!=null && Vector2.Distance(fractures[0][0],path[0])<25f &&
                          Vector2.Distance(fractures[0][0],fractures[0][^1])>20f, "Crack did not spread from the slip");
                    int checkpoint = Read<int>(game, "cookieIndex");
                    Move(path[checkpoint], false);
                    Move(path[checkpoint], true);
                    for (int i = checkpoint + 1; i < path.Length; i++)
                    {
                        if (i == path.Length / 2)
                        {
                            for (int back = i - 1; back >= i - 10; back--) Move(path[back], true);
                            for (int forward = i - 9; forward < i; forward++) Move(path[forward], true);
                            Check(Read<int[]>(game, "cracks")[p] == 1, "Retracing the groove caused a crack");
                            Move(path[i - 1], false);
                            Move(path[0], false);
                            Check(Read<int>(game, "cookieIndex") == i - 1, "Lift changed progress");
                            Move(path[i - 1], true);
                        }
                        Move(path[i], true);
                    }
                    if (p + 1 < players)
                    {
                        Move(path[0], true);
                        Check(Read<object>(game, "screen").ToString() == "Round" && !Read<bool>(game, "cookieStarted"),
                            "Held button started the next player's clock");
                    }
                }
                Check(Read<object>(game, "screen").ToString() == "Result", "Full contour did not clear round");
            }
            Check(shapes == 15, "Missing cookie shape");
            Call(game, "BeginTrial", 1);
            var edge = Read<Vector2[]>(game, "cookiePath");
            for (int crack = 0; crack < 3; crack++)
            {
                Vector2 resume = edge[Read<int>(game, "cookieIndex")];
                Move(resume, false);
                Move(resume, true);
                Move(edge[edge.Length / 2], true);
            }
            var finalCrack = Read<Vector2[][]>(game,"cookieFractures")[2];
            Check(finalCrack!=null && Vector2.Distance(finalCrack[0],finalCrack[^1])>175f,"Final crack did not cross the cookie");
            Move(edge[0], false, .4f);
            Check(Read<object>(game,"screen").ToString()=="Round", "Game over covered the fracture animation");
            Move(edge[0], true, .5f);
            Check(Read<object>(game, "screen").ToString() == "GameOver", "Third crack should break the cookie");
            Call(game, "BeginTrial", 1);
            Vector2 start = Read<Vector2[]>(game, "cookiePath")[0];
            Move(start, false);
            Move(start, true);
            Move(start, false, 43f);
            Check(Read<object>(game, "screen").ToString() == "GameOver", "Clock stopped on release");
            Debug.Log("COOKIE_TRACE_PASS four shapes, mouse input, shortcuts, resume, handoff, cracks and timeout");
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            if (previousMouse != null) previousMouse.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = focusRouting;
        }
    }

    public static void Validate(PixelSurvivalGame g)
    {
        UnityEngine.Random.InitState(3079);
        Check(Read<Texture2D>(g,"menuBackdrop")!=null,"Missing title backdrop");
        var stage=Read<CampaignStage25D>(g,"stage25D");
        var root=Read<Transform>(stage,"stageRoot");
        Call(g,"StartGame",1); Call(g,"BeginTrial",0); Call(g,"LateUpdate");
        var cast=Read<System.Collections.Generic.List<GameObject>>(stage,"actors");
        Check(cast[0].GetComponent<SpriteRenderer>().sprite.texture.name=="Green_Unarmed","Missing contestant");
        var contestants = cast.Where(a => a.activeSelf && a.transform.Find("Dorsal") != null).ToArray();
        string Badge(GameObject actor)=>actor.transform.Find("Dorsal").GetComponent<SpriteRenderer>().sprite.name;
        Check(contestants.Length == 51 && contestants.Select(Badge).Distinct().Count()==51,"Duplicate solo participant numbers");
        Check(Badge(cast[0])=="001" && Badge(cast[2])=="002","Solo roster numbering");
        Check(contestants.Select(a=>a.GetComponent<SpriteRenderer>().sprite.texture).Distinct().Count()==7,"Hair palettes missing");
        Check(cast[cast.Count-1].transform.Find("Dorsal")==null,"Guard has a contestant badge");
        Check(cast[cast.Count-1].GetComponent<SpriteRenderer>().sprite.texture.name=="Red_Rifle","Missing guard");
        var playerImage=cast[0].GetComponent<SpriteRenderer>();
        Read<bool[]>(g,"playerMoving")[0]=true;
        Read<float[]>(g,"runner")[0]=10;
        Call(g,"LateUpdate");
        Check(playerImage.sprite.name.Contains("_Walk_"),"Directional walk");
        Read<bool[]>(g,"playerAlive")[0]=false;
        Call(g,"LateUpdate");
        Check(playerImage.sprite.name.Contains("_Death_"),"Missing elimination pose");
        var pose=((System.Collections.IDictionary)Read<object>(stage,"poses"))[cast[0]];
        pose.GetType().GetField("deathStarted",Private).SetValue(pose,Time.time-.62f);
        Call(g,"LateUpdate");
        Check(playerImage.sprite.name.EndsWith("_6"),"Elimination must hold final frame");
        Check(root.Cast<Transform>().Count(t=>t.name.StartsWith("Arbolito_"))==2,"Missing trees");
        Check(Read<GameObject>(stage,"doll").transform.position.y<.1f,"Doll elevated");
        ValidateCookieTracing(g);
        Call(g,"BeginTrial",1); Call(g,"LateUpdate");
        for(int n=0;n<=5;n++) Check(((Texture2D)Call(g,"CookieFace",n)).width==180,"Cookie");
        foreach(int people in new[]{1,2})
        foreach(int total in new[]{20,23,50+people})
        for(int layout=0;layout<3;layout++)
        {
            Call(g,"StartGame",people);
            var alive=Read<bool[]>(g,"botRunnerAlive");
            for(int i=0;i<alive.Length;i++) alive[i]=i<total-people;
            Call(g,"BeginTrial",2); Call(g,"LateUpdate");
            var capacities=Read<int[]>(g,"capacity");
            Check(capacities.Distinct().Count()==4,"Missing room sizes");
            Call(g,"OpenCarouselDoors");
            for(int n=0;n<340;n++){
                if(n==0) for(int p=0;p<people;p++) Call(g,"SetCarouselRoom",p,p);
                g.GetType().GetField("carouselClock",Private).SetValue(g,17f-n*.05f);
                Call(g,"UpdateCarouselBots",.05f);
            }
            var rooms=Read<int[]>(g,"carouselRoom");
            var pos=Read<Vector2[]>(g,"botPosition");
            var counts=Read<int[]>(g,"roomCount");
            Check(counts.Sum()+Enumerable.Range(0,total-people).Count(i=>rooms[i+2]<0)==total,"Occupancy");
            Check(counts.Select((count,d)=>count<=capacities[d]).All(valid=>valid),"Overfull room");
            for(int i=0;i<total-people;i++) Check(rooms[i+2]>=0||pos[i].magnitude>4.1f,"Stranded on carousel: "+i);
            Check(counts.Sum()==Math.Min(total,capacities.Sum()),$"Unused places ({people} humans, {total} riders, layout {layout}): {string.Join(",",counts)} / {string.Join(",",capacities)}");
        }
        var frames=root.Cast<Transform>().Where(t=>t.name.StartsWith("Habitación ")).Take(9).ToArray();
        Check(frames.Length==9 && frames.All(t=>t.Find("Techo continuo")!=null),"Open roof");
        // A human takes the last place while a bot with a reservation is at the threshold.
        Call(g,"BeginTrial",2);
        Read<int[]>(g,"capacity")[0]=2;
        Call(g,"OpenCarouselDoors");
        Call(g,"SetCarouselRoom",0,0);
        Call(g,"SetCarouselRoom",1,0);
        Read<int[]>(g,"destination")[0]=0;
        var doors=(Vector2[])typeof(PixelSurvivalGame).GetField("CarouselDoors",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        Read<Vector2[]>(g,"botPosition")[0]=doors[0];
        Read<float[]>(g,"botClock")[0]=1;
        Call(g,"UpdateCarouselBots",.05f);
        Check(Read<int[]>(g,"roomCount")[0]==2 && Read<int[]>(g,"destination")[0]!=0,"Bot entered a full room");
        Call(g,"BeginTrial",4); Call(g,"LateUpdate");
        var actors=Read<System.Collections.Generic.List<GameObject>>(stage,"actors").Where(a=>a.activeSelf).ToArray();
        Check(actors.Length==52 && actors.Select(a=>a.transform.position.z).Distinct().Count()==1,"Tug lineup");
        Check(Badge(actors[1])=="002" && Badge(actors[2])=="003" && Badge(actors[^1])=="052","Co-op roster numbering");
        Check(actors.Select(a=>a.GetComponent<SpriteRenderer>().sprite.texture).Distinct().Count()==7,"Hair lost in pull animation");
        Check(actors.All(a=>a.GetComponent<SpriteRenderer>().sprite.name.Contains("_Pull_")),"Missing grip pose");
        stage.PlayTugPull(0,true,1);
        Read<float[]>(stage,"tugPullAt")[0]=Time.time-.26f;
        Call(g,"LateUpdate");
        Check(actors[0].GetComponent<SpriteRenderer>().sprite.name.EndsWith("_3"),"Missing accented pull");
        var camera=Camera.main;
        foreach(var a in actors){
            var p=camera.WorldToViewportPoint(a.transform.position);
            Check(p.x>.01f && p.x<.99f && p.y>.02f && p.y<.98f,"Tug cropped");
        }
        Call(g,"BeginTrial",3); Call(g,"LateUpdate");
        Check(cast.Any(a=>a.GetComponent<SpriteRenderer>().sprite.texture.name=="Green_Flashlight"),"Missing flashlight");
        Call(g,"BeginTrial",7); Call(g,"LaunchFloorJump",0,0); Call(g,"LateUpdate");
        Check(cast[0].GetComponent<SpriteRenderer>().sprite.name.Contains("_Jump_"),"Missing airborne pose");
        var vipSkins=root.Cast<Transform>().Where(t=>t.name=="VIP de traje negro" && t.gameObject.activeSelf)
            .Select(t=>t.GetComponent<SpriteRenderer>().sprite.texture.name).Distinct().ToArray();
        Check(vipSkins.Contains("Black_Unarmed") && vipSkins.Contains("Black_Knife"),"Missing VIP variants");
    }
}
