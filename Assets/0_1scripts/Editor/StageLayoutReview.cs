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
    static void Tick()
    {
        if(!SessionState.GetBool("StageLayoutReview",false)||!Application.isPlaying||Time.frameCount<5) return;
        SessionState.SetBool("StageLayoutReview",false);
        try
        {
            var g=UnityEngine.Object.FindFirstObjectByType<PixelSurvivalGame>();
            Check(Read<Texture2D>(g,"menuBackdrop")!=null,"Missing title backdrop");
            var stage=Read<CampaignStage25D>(g,"stage25D");
            var root=Read<Transform>(stage,"stageRoot");
            Call(g,"StartGame",1); Call(g,"BeginTrial",0); Call(g,"LateUpdate");
            Check(root.Cast<Transform>().Count(t=>t.name.StartsWith("Arbolito_"))==2,"Missing trees");
            Check(Read<GameObject>(stage,"doll").transform.position.y<.1f,"Doll elevated");
            Call(g,"BeginTrial",1); Call(g,"LateUpdate");
            for(int n=0;n<=5;n++) Check(((Texture2D)Call(g,"CookieFace",n)).width==180,"Cookie");
            foreach(int people in new[]{1,2})
            foreach(int total in new[]{20,23,30})
            foreach(int size in new[]{2,3,4})
            {
                Call(g,"StartGame",people);
                var alive=Read<bool[]>(g,"botRunnerAlive");
                for(int i=0;i<alive.Length;i++) alive[i]=i<total-people;
                Call(g,"BeginTrial",2); Call(g,"LateUpdate");
                g.GetType().GetField("groupSize",Private).SetValue(g,size);
                Call(g,"OpenCarouselDoors");
                for(int n=0;n<320;n++){
                    if(n==70) for(int p=0;p<people;p++) Call(g,"SetCarouselRoom",p,p);
                    g.GetType().GetField("carouselClock",Private).SetValue(g,17f-n*.05f);
                    Call(g,"UpdateCarouselBots",.05f);
                }
                var rooms=Read<int[]>(g,"carouselRoom");
                var pos=Read<Vector2[]>(g,"botPosition");
                var counts=Read<int[]>(g,"roomCount");
                Check(counts.Sum()+Enumerable.Range(0,total-people).Count(i=>rooms[i+2]<0)==total,"Occupancy");
                Check(counts.All(count=>count<=size),"Overfull room");
                for(int i=0;i<total-people;i++) Check(rooms[i+2]>=0||pos[i].magnitude>4.1f,"Stranded on carousel: "+i);
                Check(counts.Sum()==Math.Min(total,9*size),"Unused places");
            }
            var frames=root.Cast<Transform>().Where(t=>t.name.StartsWith("Habitación ")).Take(9).ToArray();
            Check(frames.Length==9 && frames.All(t=>t.Find("Techo continuo")!=null),"Open roof");
            Call(g,"BeginTrial",4); Call(g,"LateUpdate");
            var actors=Read<System.Collections.Generic.List<GameObject>>(stage,"actors").Where(a=>a.activeSelf).ToArray();
            Check(actors.Length==30 && actors.Select(a=>a.transform.position.z).Distinct().Count()==1,"Tug lineup");
            var camera=Camera.main;
            foreach(var a in actors){
                var p=camera.WorldToViewportPoint(a.transform.position);
                Check(p.x>.01f && p.x<.99f && p.y>.02f && p.y<.98f,"Tug cropped");
            }
            Debug.Log("STAGE_LAYOUT_PASS 18 carousel configurations, tug roster, doll, title and cookies");
            EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogError("STAGE_LAYOUT_FAIL "+ex);EditorApplication.Exit(1);}
    }
}
