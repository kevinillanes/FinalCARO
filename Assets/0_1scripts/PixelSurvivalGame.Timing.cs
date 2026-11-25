using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class PixelSurvivalGame
{
    private const int CampaignBots=50;
    private readonly float[] bandCenter=new float[2];
    private readonly bool[] tugAttempted=new bool[2];
    private readonly string[] tugFeedback=new string[2];
    private float tugBeat;
    private int tugBeatNumber;

    private void PrepareTugTiming()
    {
        tugBeat=0;
        tugBeatNumber=0;
        for(int p=0;p<humanCount;p++)
        {
            PickBand(p);
            tugAttempted[p]=false;
            tugFeedback[p]="Prepara el tirón";
        }
    }

    private void PickBand(int p)
    {
        // A circular offset keeps consecutive targets visibly separated without retries.
        float t=(bandCenter[p]-.16f)/.68f;
        bandCenter[p]=.16f+.68f*Mathf.Repeat(t+Random.Range(.26f,.74f),1);
    }

    private float Needle(int p)=>Mathf.PingPong(pulse[p],1);
    private float Band(int p)=>bandCenter[p]+.035f*Mathf.Sin(trialTime*2.4f+p);
    private float TugWidth()=>Mathf.Lerp(.19f,.13f,Mathf.Clamp01(trialTime/32f));
    private bool TimingHit(int p,float width)=>Mathf.Abs(Needle(p)-Band(p))<=width*.5f;

    private void DrawBand(Rect bar,int p,float width)
    {
        Fill(bar,new Color(.065f,.085f,.12f));
        float center=bar.x+Band(p)*bar.width;
        Fill(new Rect(center-width*bar.width*.5f,bar.y,width*bar.width,bar.height),new Color(.15f,.73f,.38f));
        if((Trial)trialIndex==Trial.Tug)
            Fill(new Rect(center-width*bar.width*.17f,bar.y,width*bar.width*.34f,bar.height),new Color(.68f,1f,.61f));
        Fill(new Rect(bar.x+Needle(p)*bar.width-2,bar.y-3,4,bar.height+6),Color.white);
    }

    private void UpdateTug(Keyboard keys,float dt)
    {
        tugBeat+=dt/Mathf.Lerp(1.85f,1.20f,Mathf.Clamp01(trialTime/32f));
        if(tugBeat>=1)
        {
            tugBeat%=1;
            tugBeatNumber++;
            for(int p=0;p<humanCount;p++)
            {
                if(!tugAttempted[p]) { tugProgress-=3f/humanCount; tugFeedback[p]="Sin tirón"; }
                PickBand(p);
                tugAttempted[p]=false;
            }
        }
        for(int p=0;p<humanCount;p++)
        {
            pulse[p]=(tugBeatNumber+p)%2==0?tugBeat:1-tugBeat;
            bool press=p==0?Pressed(keys,Key.Space)||Pressed(keys,Key.A):Pressed(keys,Key.Enter)||Pressed(keys,Key.RightArrow);
            if(!press || tugAttempted[p]) continue;
            tugAttempted[p]=true;
            float width=TugWidth();
            bool hit=TimingHit(p,width),perfect=TimingHit(p,width*.34f);
            stage25D.PlayTugPull(p,true,perfect?1f:hit?.75f:.24f);
            tugProgress+=(perfect?10f:hit?6.5f:-6f)/humanCount;
            tugFeedback[p]=perfect?"¡Perfecto!":hit?"Buen tirón":"Fuera de tiempo";
            Chime(perfect?1.7f:hit?1.25f:.6f);
        }
        for(int i=0;i<rivalBots;i++)
        {
            if(!botRunnerAlive[i] || (botClock[i]-=dt)>0) continue;
            stage25D.PlayTugPull(i+2,tugAllies[i],.36f);
            tugProgress+=(tugAllies[i]?1:-1)*(.22f+.32f*nerve[i]);
            botClock[i]=1.4f+(1f-craft[i])*.8f;
        }
        tugProgress=Mathf.Clamp(tugProgress-dt*.45f,0,100);
        if(tugProgress<=0 || tugProgress>=100 || trialTime>=32)
        {
            bool won=tugProgress>=60;
            CompleteTrial(won,won?"El último tirón fue de tu equipo.":"El equipo rival se llevó la cuerda.");
        }
    }

    private void DrawTug()
    {
        Fill(new Rect(56,360,848,136),new Color(.035f,.055f,.085f,.96f));
        Label(new Rect(66,362,828,29),$"Un tirón por barrido · Verde claro da más fuerza · {Mathf.CeilToInt(32-trialTime)} s",smallStyle);
        for(int p=0;p<humanCount;p++)
        {
            float x=humanCount==1?330:104+p*450;
            DrawBand(new Rect(x,400,300,16),p,TugWidth());
            Label(new Rect(x-20,420,340,25),$"J{p+1} · {(p==0?"ESPACIO / A":"ENTER / →")} · {tugFeedback[p]}",smallStyle);
        }
        Fill(new Rect(230,453,500,9),Color.black);
        Fill(new Rect(230,453,tugProgress*5,9),new Color(.27f,.71f,.52f));
        Fill(new Rect(529,450,2,15),Color.white);
        Label(new Rect(135,465,690,29),$"Fuerza {Mathf.RoundToInt(tugProgress)} % · Llega a 100 % o acaba con 60 %",smallStyle);
        if(isPractice && GUI.Button(new Rect(12,12,122,31),"Volver al menú")) screen=Screen.Title;
    }
}
