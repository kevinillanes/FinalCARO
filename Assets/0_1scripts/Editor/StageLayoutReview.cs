using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
    public static void Validate(PixelSurvivalGame g)
    {
        UnityEngine.Random.InitState(3079);
        Check(Read<Texture2D>(g,"menuBackdrop")!=null,"Missing title backdrop");
        var stage=Read<CampaignStage25D>(g,"stage25D");
        var root=Read<Transform>(stage,"stageRoot");
        Call(g,"StartGame",1); Call(g,"BeginTrial",0); Call(g,"LateUpdate");
        Check(root.Cast<Transform>().Count(t=>t.name.StartsWith("Arbolito_"))==2,"Missing trees");
        Check(Read<GameObject>(stage,"doll").transform.position.y<.1f,"Doll elevated");
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
        var camera=Camera.main;
        foreach(var a in actors){
            var p=camera.WorldToViewportPoint(a.transform.position);
            Check(p.x>.01f && p.x<.99f && p.y>.02f && p.y<.98f,"Tug cropped");
        }
    }
}
