using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class PixelSurvivalGame
{
    private enum CarouselPhase { Briefing, Spinning, Doors, Count }
    private const int CarouselRounds = 3;
    private const float DoorSeconds = 17f;
    private const float Reach = 1.65f;
    // Ordered from the left foreground, across the back wall, to the right foreground.
    internal static readonly Vector2[] CarouselDoors = {
        new Vector2(-8.8f,-4), new Vector2(-8.8f,1), new Vector2(-7.6f,6),
        new Vector2(-3.8f,6), new Vector2(0,6), new Vector2(3.8f,6),
        new Vector2(7.6f,6), new Vector2(8.8f,1), new Vector2(8.8f,-4)
    };
    private CarouselPhase carouselPhase;
    private int carouselRound;
    private readonly int[] capacity = new int[CarouselDoors.Length];
    private float carouselClock, carouselAngle, nextNote, groupsSettled;
    private int[] carouselRoom, destination;
    private readonly int[] roomCount = new int[CarouselDoors.Length], expected = new int[CarouselDoors.Length];
    private readonly string[] doorNotice = new string[2];
    private readonly float[] noticeUntil = new float[2];
    private string carouselReport;

    private void PrepareCarousel()
    {
        carouselRoom = new int[2+rivalBots];
        destination = new int[rivalBots];
        carouselRound = 0;
        carouselAngle = 0;
        StartCarouselSpin();
        carouselPhase = CarouselPhase.Briefing;
    }

    private void StartCarouselSpin()
    {
        carouselRound++;
        int living = humanCount + RemainingRivals();
        // Scatter all four sizes before drawing the remaining rooms.
        for (int d=0; d<capacity.Length; d++)
            capacity[d]=Mathf.Min(living,d<4 ? d+1 : Random.Range(1,5));
        for (int d=capacity.Length-1; d>0; d--)
        {
            int swap=Random.Range(0,d+1);
            (capacity[d],capacity[swap])=(capacity[swap],capacity[d]);
        }
        System.Array.Fill(carouselRoom,-1);
        System.Array.Fill(destination,-1);
        System.Array.Clear(roomCount,0,roomCount.Length);
        System.Array.Clear(noticeUntil,0,noticeUntil.Length);
        carouselPhase = CarouselPhase.Spinning;
        carouselClock = 4f;
        nextNote = 0f;
        PlaceCarouselRiders();
    }

    private void PlaceCarouselRiders()
    {
        int index = 0, living = humanCount + RemainingRivals();
        for (int slot=0; slot<carouselRoom.Length; slot++)
        {
            if (slot==1 && humanCount==1 || slot>=2 && !botRunnerAlive[slot-2]) continue;
            int rings=living>32 ? 3 : 2;
            int ring=index%rings;
            int inRing=(living+rings-1-ring)/rings;
            float angle = (carouselAngle + 360f*(index/rings + ring*.5f)/inRing)*Mathf.Deg2Rad;
            float radius=3.65f-ring*1.05f;
            Vector2 pos = new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*radius;
            if (slot<2) finalPosition[slot]=pos;
            else botPosition[slot-2]=pos;
            index++;
        }
    }

    private void UpdateCarousel(Keyboard k, float dt)
    {
        if (carouselPhase==CarouselPhase.Briefing)
        {
            if (Pressed(k,Key.Space) || Pressed(k,Key.Enter)) carouselPhase=CarouselPhase.Spinning;
            return;
        }
        int previousSecond=Mathf.CeilToInt(carouselClock);
        carouselClock -= dt;
        if (carouselPhase==CarouselPhase.Count)
        {
            if (carouselClock<=0) StartCarouselSpin();
            return;
        }
        if (carouselPhase==CarouselPhase.Spinning)
        {
            carouselAngle = Mathf.Repeat(carouselAngle + dt*58f*Mathf.Clamp01(carouselClock),360f);
            PlaceCarouselRiders();
            nextNote -= dt;
            if (nextNote<=0) { Chime(1f + .16f*(Mathf.CeilToInt(carouselClock*2)%3)); nextNote=.5f; }
            if (carouselClock<=0) OpenCarouselDoors();
            return;
        }
        if (carouselClock<=0) { CountCarouselRooms(); return; }
        if (carouselClock<=3 && Mathf.CeilToInt(carouselClock)<previousSecond) Chime(.8f);
        for (int p=0; p<humanCount; p++)
        {
            Vector2 motion = p==0
                ? new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0))
                : new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
            playerMoving[p] = carouselRoom[p]<0 && motion.sqrMagnitude>0;
            if (playerMoving[p])
            {
                Vector2 moved=finalPosition[p]+ArenaView.GroundInput(motion)*dt*5.6f;
                finalPosition[p]=ArenaView.SlidePastColumn(finalPosition[p],new Vector2(Mathf.Clamp(moved.x,-9,9),Mathf.Clamp(moved.y,-6,6.2f)),false);
            }
            if (Pressed(k,p==0?Key.Space:Key.Enter)) UseCarouselDoor(p);
        }
        UpdateCarouselBots(dt);
        // Keep the full time while someone is choosing. Once the groups have
        // settled, give the humans a final chance to leave, then begin the count.
        if (CarouselGroupsReady()) groupsSettled+=dt;
        else groupsSettled=0;
        if (groupsSettled>=2f) CountCarouselRooms();
    }

    private void OpenCarouselDoors()
    {
        carouselPhase=CarouselPhase.Doors;
        carouselClock=DoorSeconds;
        groupsSettled=0;
        for (int i=0;i<rivalBots;i++)
        {
            if (!botRunnerAlive[i]) continue;
            botClock[i] = .12f + (1f-craft[i])*.55f;
        }
        Chime(.65f);
    }

    private int NearestCarouselDoor(Vector2 position)
    {
        int nearest=-1;
        float best=Reach*Reach;
        for (int d=0;d<CarouselDoors.Length;d++)
        {
            float distance=(position-CarouselDoors[d]).sqrMagnitude;
            if (distance<=best) { nearest=d; best=distance; }
        }
        return nearest;
    }

    private bool CarouselGroupsReady()
    {
        for(int p=0;p<humanCount;p++)
            if(carouselRoom[p]<0 || roomCount[carouselRoom[p]]!=capacity[carouselRoom[p]]) return false;
        int inside=0, places=0;
        for(int d=0;d<capacity.Length;d++)
        {
            if(roomCount[d]!=0 && roomCount[d]!=capacity[d]) return false;
            inside+=roomCount[d];
            places+=capacity[d];
        }
        return inside==Mathf.Min(humanCount+RemainingRivals(),places);
    }

    private bool SetCarouselRoom(int slot,int room)
    {
        int old=carouselRoom[slot];
        if(old==room) return true;
        if(room>=0 && roomCount[room]>=capacity[room]) return false;
        if(old>=0) roomCount[old]--;
        carouselRoom[slot]=room;
        if(room>=0) roomCount[room]++;
        return true;
    }

    private void UseCarouselDoor(int p)
    {
        int old=carouselRoom[p];
        if (old>=0)
        {
            SetCarouselRoom(p,-1);
            finalPosition[p]=Vector2.MoveTowards(CarouselDoors[old],Vector2.zero,.85f);
            Chime(.9f);
            return;
        }
        int room=NearestCarouselDoor(finalPosition[p]);
        if (room>=0 && SetCarouselRoom(p,room)) { Chime(1.4f); return; }
        doorNotice[p]=room<0?"Acércate al umbral iluminado":"Puerta llena · busca otra";
        noticeUntil[p]=trialTime+1.25f;
    }

    private void UpdateCarouselBots(float dt)
    {
        System.Array.Copy(roomCount,expected,roomCount.Length);
        for (int i=0;i<rivalBots;i++)
            if (botRunnerAlive[i] && carouselRoom[i+2]<0 && destination[i]>=0) expected[destination[i]]++;
        for (int i=0;i<rivalBots;i++)
        {
            if (!botRunnerAlive[i]) continue;
            botClock[i]-=dt;
            int slot=i+2, current=carouselRoom[slot];
            if (current>=0)
            {
                if (botClock[i]>0 || roomCount[current]==capacity[current]) continue;
                if (roomCount[current]<capacity[current])
                {
                    // An incomplete group may still have riders on their way. Only abandon it
                    // late and for an occupied room the bot can reach before the doors close.
                    if (carouselClock>6 || expected[current]>=capacity[current] || HasHumanInside(current)) continue;
                    int other=ChooseCarouselDoor(i,current);
                    if (other<0 || other==current || roomCount[other]==0 ||
                        !HasHumanInside(other) && capacity[other]-expected[other]>=capacity[current]-expected[current] ||
                        Vector2.Distance(CarouselDoors[current],CarouselDoors[other])>(2.6f+1.7f*nerve[i])*Mathf.Max(0,carouselClock-.7f)) continue;
                    destination[i]=other;
                    expected[other]++;
                }
                expected[current]--;
                SetCarouselRoom(slot,-1);
                botPosition[i]=CarouselDoors[current];
            }
            int target=destination[i];
            if (botClock[i]<=0)
            {
                if (target>=0) expected[target]--;
                int candidate=ChooseCarouselDoor(i);
                if(target<0 || expected[target]>=capacity[target] ||
                    (candidate>=0 && HasHumanInside(candidate) && !HasHumanInside(target)))
                    target=candidate;
                destination[i]=target;
                if (target>=0) expected[target]++;
                botClock[i]=.55f + (1f-craft[i])*.6f;
            }
            Vector2 threshold;
            if(target>=0) threshold=CarouselDoors[target];
            else
            {
                // Even when every place is claimed, approach the doors and watch for a vacancy.
                int nearest=0;
                for(int d=1;d<CarouselDoors.Length;d++)
                    if((botPosition[i]-CarouselDoors[d]).sqrMagnitude<(botPosition[i]-CarouselDoors[nearest]).sqrMagnitude) nearest=d;
                Vector2 inward=-CarouselDoors[nearest].normalized;
                threshold=CarouselDoors[nearest]+inward*(1.15f+.22f*(i%3))+new Vector2(-inward.y,inward.x)*((i%5)-2)*.31f;
            }
            Vector2 start=botPosition[i],path=threshold-start;
            float along=Mathf.Clamp01(-Vector2.Dot(start,path)/Mathf.Max(.0001f,path.sqrMagnitude));
            Vector2 waypoint=threshold;
            if((start+path*along).sqrMagnitude<1.32f*1.32f)
            {
                Vector2 side=new Vector2(-threshold.y,threshold.x).normalized;
                waypoint=side*(Vector2.Dot(start,side)<0?-1.8f:1.8f);
            }
            botPosition[i]=ArenaView.SlidePastColumn(start,Vector2.MoveTowards(start,waypoint,dt*(2.6f+1.7f*nerve[i])),true);
            if (target<0 || (botPosition[i]-threshold).sqrMagnitude>.35f) continue;
            if (roomCount[target]>=capacity[target])
            {
                expected[target]--;
                destination[i]=-1;
                botClock[i]=0f;
                continue;
            }
            SetCarouselRoom(slot,target);
            destination[i]=-1;
            botClock[i]=1f;
        }
    }

    private int ChooseCarouselDoor(int bot,int exclude=-1)
    {
        int best=-1;
        float bestScore=float.NegativeInfinity;
        for (int d=0;d<CarouselDoors.Length;d++)
        {
            if (d==exclude || expected[d]>=capacity[d]) continue;
            float score=(HasHumanInside(d)?18f:0f) + expected[d]*3f - Vector2.Distance(botPosition[bot],CarouselDoors[d]);
            if (score>bestScore) { best=d; bestScore=score; }
        }
        return best;
    }

    private bool HasHumanInside(int room)
    {
        return carouselRoom[0]==room || humanCount==2 && carouselRoom[1]==room;
    }

    private void CountCarouselRooms()
    {
        string failure=null;
        for (int p=0;p<humanCount;p++)
        {
            int door=carouselRoom[p];
            if (door<0) failure=$"J{p+1} quedó fuera cuando se cerraron las puertas.";
            else if (roomCount[door]!=capacity[door])
                failure=$"Puerta {door+1}: había {roomCount[door]}. Se necesitaban exactamente {capacity[door]}.";
        }
        int before=RemainingRivals();
        for (int i=0;i<rivalBots;i++)
        {
            int d=carouselRoom[i+2];
            if (botRunnerAlive[i] && (d<0 || roomCount[d]!=capacity[d])) botRunnerAlive[i]=false;
        }
        if (ally>=0 && !botRunnerAlive[ally]) { ally=-1; share=doubleCross=false; }
        carouselPhase=CarouselPhase.Count;
        carouselReport=$"Grupos comprobados · {before-RemainingRivals()} rivales eliminados";
        if (failure!=null) { CompleteTrial(false,failure); return; }
        if (carouselRound==CarouselRounds) { CompleteTrial(true,"Tres vueltas superadas. Se abre el pasillo hacia las literas."); return; }
        carouselClock=3f;
        Chime(1.7f);
    }

    private void DrawCarousel()
    {
        Color panel=new Color(.035f,.045f,.09f,.94f);
        if (carouselPhase==CarouselPhase.Briefing)
        {
            Fill(new Rect(124,122,712,304),panel);
            Label(new Rect(144,135,672,48),"CUANDO PARE LA MÚSICA",titleStyle);
            Label(new Rect(157,190,646,93),"Cada puerta sortea su propio cupo de 1 a 4. Un cartel 2/3 tiene una plaza libre. Entra antes del cierre: solo sobreviven los cuartos exactos. Una puerta llena rechaza tu entrada.",bodyStyle);
            Label(new Rect(146,285,668,68),humanCount==1?"J1 · WASD para moverte · ESPACIO para entrar o salir":"J1 · WASD + ESPACIO     J2 · Flechas + ENTER\nPueden elegir puertas distintas. Ambos deben quedar en grupos exactos.",smallStyle);
            Label(new Rect(144,363,672,44),"Pulsa ESPACIO o ENTER cuando estés listo",bodyStyle);
            return;
        }
        if (carouselPhase==CarouselPhase.Doors) stage25D.DrawCarouselMarkers(roomCount,capacity,smallStyle,humanCount,carouselRoom);
        Fill(new Rect(20,8,920,45),panel);
        Label(new Rect(22,8,290,44),$"CARRUSEL · VUELTA {carouselRound}/{CarouselRounds}\n{humanCount+RemainingRivals()} participantes",smallStyle);
        string call=carouselPhase==CarouselPhase.Doors ? (groupsSettled>0 ? "GRUPOS LISTOS · CIERRE" : $"BUSCA UNA PLAZA    {Mathf.CeilToInt(carouselClock)} s")
            : carouselPhase==CarouselPhase.Count ? "GRUPO CORRECTO" : "EL CARRUSEL ESTÁ GIRANDO";
        Label(new Rect(316,8,615,44),call,titleStyle);
        if (carouselPhase!=CarouselPhase.Doors)
        {
            Fill(new Rect(165,433,630,52),panel);
            Label(new Rect(172,436,616,45),carouselPhase==CarouselPhase.Count?carouselReport:"Escucha la música. Las puertas se abrirán al parar.",bodyStyle);
            return;
        }
        for (int p=0;p<humanCount;p++)
        {
            int door=carouselRoom[p];
            string key=p==0?"ESPACIO":"ENTER";
            int near=NearestCarouselDoor(finalPosition[p]);
            string action;
            if (door>=0)
            {
                int missing=capacity[door]-roomCount[door];
                string status=missing==0?"Grupo exacto":missing>0?$"Faltan {missing}":$"Sobran {-missing}";
                action=$"Puerta {door+1} · {status} · {key} para salir";
            }
            else if (noticeUntil[p]>trialTime) action=doorNotice[p];
            else if(near>=0 && roomCount[near]>=capacity[near]) action="Puerta llena · busca otra";
            else action=near>=0?$"{key} para entrar por la puerta {near+1}":(p==0?"WASD":"Flechas")+" · Acércate a una puerta";
            float x=humanCount==1?250:20+p*468;
            Fill(new Rect(x,463,440,35),panel);
            Label(new Rect(x+4,463,432,35),$"J{p+1}   {action}",smallStyle);
        }
    }
}
