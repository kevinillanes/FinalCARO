using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class PixelSurvivalGame
{
    internal const int FloorRows = 8, FloorLanes = 3, RopeTurns = 12;
    internal struct RopeRider
    {
        public int place;
        public bool jumped;
        public float height, jumpAt, decision;
    }
    internal struct FloorRider
    {
        public int row, lane, fromLane;
        public float travel, nextMove;
        public bool airborne;
    }

    private float[] bridgeFallAt, riderOutAt;
    private RopeRider[] ropeSlots;
    private FloorRider[] floorRiders;
    private float[,] tileDeadline;
    private int ropeTurn;
    private float ropePhase;
    private bool ropeChecked, isPractice;
    private Trial practiceTrial;
    private const float JumpWindowStart = .60f, RopeContact = .83f;

    private bool RiderAlive(int slot) => slot < 2 ? slot < humanCount && playerAlive[slot] : botRunnerAlive[slot-2];

    private void EliminateRider(int slot)
    {
        riderOutAt[slot]=Time.time;
        if (slot < 2) { playerAlive[slot] = false; Chime(.56f); }
        else botRunnerAlive[slot-2] = false;
    }

    public void OpenRopePractice() { practiceTrial=Trial.Rope; screen=Screen.Practice; }
    public void OpenTugPractice() { practiceTrial=Trial.Tug; screen=Screen.Practice; }

    private void DrawPracticeEntry()
    {
        Fill(new Rect(155,110,650,320),new Color(.04f,.07f,.12f,.96f));
        Label(new Rect(170,135,620,54),TrialNames[(int)practiceTrial],titleStyle);
        Label(new Rect(180,205,600,60),practiceTrial==Trial.Rope?"Doce vueltas. Cada participante responde por su salto.":"Una pulsación por barrido. Tira dentro del verde.",bodyStyle);
        for (int n=1;n<=2;n++)
            if (GUI.Button(new Rect(185+(n-1)*305,285,285,55),n==1?"UN JUGADOR":"DOS JUGADORES",buttonStyle))
            {
                StartGame(n);
                isPractice=true;
                for(int i=6;i<rivalBots;i++) botRunnerAlive[i]=false;
                BeginTrial((int)practiceTrial);
            }
        if (GUI.Button(new Rect(390,365,180,38),"Volver al menú",buttonStyle)) screen=Screen.Title;
    }

    private void PrepareRopeRound()
    {
        ResetEliminations();
        ropeTurn=0;
        ropeSlots=new RopeRider[2+rivalBots];
        int place=0;
        for(int slot=0;slot<ropeSlots.Length;slot++)
            if(RiderAlive(slot)) ropeSlots[slot].place=place++;
        ResetRotation();
    }

    private void ResetRotation()
    {
        ropePhase=0;
        ropeChecked=false;
        for(int slot=0;slot<ropeSlots.Length;slot++)
        {
            ropeSlots[slot].jumped=false;
            ropeSlots[slot].height=0;
            if(slot>=2)
                ropeSlots[slot].decision=Random.value < .94f+.045f*craft[slot-2]
                    ? Random.Range(.64f,.78f) : .95f;
        }
    }

    private void UpdateRopeRound(Keyboard keys,float dt)
    {
        float period=Mathf.Max(1.52f,2.65f-.09f*ropeTurn);
        float previous=ropePhase;
        ropePhase+=dt/period;
        if(previous<JumpWindowStart && ropePhase>=JumpWindowStart) Chime(1.5f);
        if(!ropeChecked)
        {
            for(int slot=0;slot<ropeSlots.Length;slot++)
            {
                if(!RiderAlive(slot) || ropeSlots[slot].jumped) continue;
                bool press=slot==0 ? Pressed(keys,Key.Space)||Pressed(keys,Key.W)
                    : slot==1 ? Pressed(keys,Key.Enter)||Pressed(keys,Key.UpArrow)
                    : ropePhase>=ropeSlots[slot].decision;
                if(!press) continue;
                if(ropePhase<JumpWindowStart || previous>=RopeContact) { EliminateRider(slot); continue; }
                ropeSlots[slot].jumped=true;
                ropeSlots[slot].jumpAt=ropePhase;
            }
            if(ropePhase>=RopeContact)
            {
                for(int slot=0;slot<ropeSlots.Length;slot++)
                    if(RiderAlive(slot) && !ropeSlots[slot].jumped) EliminateRider(slot);
                ropeChecked=true;
            }
        }
        for(int i=0;i<ropeSlots.Length;i++)
            if(ropeSlots[i].jumped)
            {
                float t=Mathf.InverseLerp(ropeSlots[i].jumpAt,1f,ropePhase);
                ropeSlots[i].height=1.5f*Mathf.Sin(Mathf.PI*t);
            }
        if(!playerAlive[0] && !playerAlive[1]) { CompleteTrial(false,"La cuerda alcanzó al último jugador."); return; }
        if(ropePhase<1f) return;
        if(++ropeTurn==RopeTurns) CompleteTrial(true,"Doce vueltas superadas. Los supervivientes dejan la pasarela.");
        else ResetRotation();
    }

    private void DrawRopeRound()
    {
        bool jump=ropePhase>=JumpWindowStart && ropePhase<RopeContact;
        Fill(new Rect(296,99,368,46),jump?new Color(.05f,.48f,.29f):new Color(.05f,.08f,.15f,.94f));
        Label(new Rect(300,100,360,44),jump?"¡SALTA AHORA!":ropeChecked?"PASÓ LA CUERDA":"PREPÁRATE",bodyStyle);
        string status=humanCount==2 ? $"   ·   J1 {(playerAlive[0]?"en juego":"fuera")} / J2 {(playerAlive[1]?"en juego":"fuera")}" : "";
        Fill(new Rect(58,422,844,70),new Color(.03f,.06f,.10f,.94f));
        Label(new Rect(68,424,824,64),$"Vuelta {ropeTurn+1}/{RopeTurns}{status}\nSalta al aviso verde. Un fallo te elimina.  J1 ESPACIO o W"+(humanCount==2?"  ·  J2 ENTER o ↑":""),smallStyle);
        if(isPractice && GUI.Button(new Rect(12,12,122,31),"Volver al menú")) screen=Screen.Title;
    }

    private void ResetEliminations()
    {
        riderOutAt=new float[2+rivalBots];
        System.Array.Fill(riderOutAt,float.NegativeInfinity);
    }

    private void PrepareFloor()
    {
        ResetEliminations();
        tileDeadline=new float[FloorRows,FloorLanes];
        for(int row=0;row<FloorRows;row++)
            for(int lane=0;lane<FloorLanes;lane++) tileDeadline[row,lane]=float.PositiveInfinity;
        // Each broken row is approached from a complete row, so every lane has a reachable landing.
        for(int row=1;row<FloorRows;row+=2) tileDeadline[row,row==3?1:Random.Range(0,FloorLanes)]=-10f;
        floorRiders=new FloorRider[2+rivalBots];
        for(int i=0;i<floorRiders.Length;i++)
        {
            floorRiders[i].row=-1;
            floorRiders[i].lane=i==0?1:i%3;
            floorRiders[i].nextMove=Random.Range(1.9f,3.7f);
        }
    }

    private const float FlightSeconds=.52f, TileLife=1.75f;

    private void LaunchFloorJump(int slot,int direction)
    {
        ref FloorRider rider=ref floorRiders[slot];
        rider.fromLane=rider.lane;
        rider.lane=Mathf.Clamp(rider.lane+direction,0,FloorLanes-1);
        rider.row++;
        rider.airborne=true;
        rider.travel=0;
    }

    private void UpdateFloor(Keyboard keys,float dt)
    {
        for(int slot=0;slot<floorRiders.Length;slot++)
        {
            ref FloorRider rider=ref floorRiders[slot];
            if(!RiderAlive(slot)) continue;
            if(rider.airborne)
            {
                rider.travel=Mathf.Min(1,rider.travel+dt/FlightSeconds);
                if(rider.travel<1) continue;
                rider.airborne=false;
                if(rider.row==FloorRows) { if(slot<2) { playerWon[slot]=true; Chime(1.6f); } continue; }
                ref float deadline=ref tileDeadline[rider.row,rider.lane];
                if(deadline>trialTime)
                {
                    deadline=Mathf.Min(deadline,trialTime+TileLife);
                    if(slot<2) Chime(1.08f+.035f*rider.row);
                }
                rider.nextMove=trialTime+Random.Range(.38f,1.23f);
            }
            if(rider.row==FloorRows) continue;
            if(rider.row>=0 && tileDeadline[rider.row,rider.lane]<=trialTime) { EliminateRider(slot); continue; }
            if(slot<2)
            {
                if(!Pressed(keys,slot==0?Key.Space:Key.Enter)) continue;
                int dir=(keys[slot==0?Key.D:Key.RightArrow].isPressed?1:0)-(keys[slot==0?Key.A:Key.LeftArrow].isPressed?1:0);
                LaunchFloorJump(slot,dir);
            }
            else if(trialTime>=rider.nextMove)
            {
                int best=rider.lane;
                float bestScore=float.NegativeInfinity;
                for(int lane=Mathf.Max(0,rider.lane-1);lane<=Mathf.Min(FloorLanes-1,rider.lane+1);lane++)
                {
                    float left=rider.row+1==FloorRows?5f:tileDeadline[rider.row+1,lane]-trialTime;
                    float score=Mathf.Min(4f,left)+Random.value;
                    if(score>bestScore) { bestScore=score; best=lane; }
                }
                LaunchFloorJump(slot,best-rider.lane);
            }
        }
        bool finished=true,won=false;
        for(int p=0;p<humanCount;p++) { finished &= !playerAlive[p] || playerWon[p]; won |= playerWon[p]; }
        if(finished || trialTime>=38f) CompleteTrial(won,won?"Llegaste a la plataforma dorada.":"Nadie llegó a la salida antes de que cayera el suelo.");
    }

    private void DrawFloor()
    {
        Fill(new Rect(42,413,876,84),new Color(.035f,.05f,.09f,.95f));
        Label(new Rect(52,414,856,77),$"Alcanza la salida dorada · {Mathf.CeilToInt(38f-trialTime)} s\nSalta recto o mantén una dirección al saltar. La loseta naranja está por caer.\nJ1 ESPACIO + A/D"+(humanCount==2?"     J2 ENTER + ←/→":""),smallStyle);
    }

    private void DrawWelcome()
    {
        string line=phaseTime<4f ? $"Guardia: «Bienvenidos. Hoy entran {humanCount+rivalBots} participantes. Ocho pruebas. Miren arriba»."
            : phaseTime<8f ? "Las luces se encienden dentro de la alcancía. Nerea susurra: «¿Cuánto vale que uno de nosotros no vuelva?»"
            : phaseTime<12f ? "Guardia: «El premio espera a quienes lleguen al final. Tras la primera prueba podrán votar si continúan»."
            : "Se abre el dormitorio. «Primera prueba: avancen con luz verde. En cuanto la muñeca se gire, quédense quietos».";
        Fill(new Rect(54,385,852,115),new Color(.025f,.035f,.065f,.94f));
        Label(new Rect(78,390,804,75),line,bodyStyle);
        Label(new Rect(80,462,800,27),"DORMITORIO   ·   ESPACIO para saltar la cinemática",smallStyle);
    }

    private string WinnerTitle()=>humanCount==2 && playerWon[0] && playerWon[1] ? "J1 Y J2 · GANADORES"
        : playerWon[1] ? "J2 · GANADOR" : "J1 · GANADOR";

    private void DrawVictory()
    {
        Fill(new Rect(0,0,960,43),new Color(.02f,.03f,.045f,.96f));
        Fill(new Rect(0,421,960,119),new Color(.02f,.03f,.045f,.96f));
        string line=phaseTime<3 ? "El último panel cae. En el palco se apagan las luces."
            : phaseTime<6 ? "Se abre la salida. Esta vez nadie da la orden de volver."
            : WinnerTitle();
        Label(new Rect(50,430,860,57),line,phaseTime<6?bodyStyle:titleStyle);
        Label(new Rect(70,490,820,30),"ESPACIO para pasar al desenlace",smallStyle);
    }
}
