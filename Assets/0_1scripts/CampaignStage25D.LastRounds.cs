using UnityEngine;

public sealed partial class CampaignStage25D
{
    private Sprite vipSprite;
    private Material bankGlass;
    private LineRenderer swingRope;
    private GameObject[,] floorTiles3D;
    private int ropeParticipants;
    private Transform exitLeft, exitRight;

    private void BuildWinnerExit()
    {
        sceneCamera.backgroundColor=new Color(.055f,.09f,.14f);
        Box("Galería de salida",new Vector3(0,-.22f,0),new Vector3(11,.44f,17),floorMat);
        Box("Alfombra",new Vector3(0,.02f,0),new Vector3(3.6f,.04f,15),terracotta);
        for(int side=-1;side<=1;side+=2)
        {
            Box("Borde de la alfombra",new Vector3(side*1.82f,.04f,0),new Vector3(.09f,.04f,15),goldMat);
            Box("Jamba de salida",new Vector3(side*2.12f,2.15f,3),new Vector3(.42f,4.3f,.55f),goldMat);
            for(int i=0;i<4;i++)
            {
                float z=-5+i*3;
                Cylinder("Pedestal",new Vector3(side*3.5f,.82f,z),.30f,1.64f,wallMat);
                Sphere("Luz de la galería",new Vector3(side*3.5f,1.75f,z),.45f,goldMat);
            }
        }
        Box("Dintel",new Vector3(0,4.28f,3),new Vector3(4.66f,.32f,.56f),goldMat);
        exitLeft=Box("Hoja izquierda",new Vector3(-1,2,3),new Vector3(2,4,.25f),blueMat).transform;
        exitRight=Box("Hoja derecha",new Vector3(1,2,3),new Vector3(2,4,.25f),blueMat).transform;
        actors.Add(Actor(false)); actors.Add(Actor(false));
    }

    public void ShowWinners(float seconds,bool[] won,int humans)
    {
        float open=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,3.1f,seconds));
        float walk=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.4f,7.8f,seconds));
        exitLeft.localPosition=new Vector3(-1-2*open,2,3);
        exitRight.localPosition=new Vector3(1+2*open,2,3);
        float z=Mathf.Lerp(4.6f,-3.4f,walk);
        bool pair=humans==2 && won[0] && won[1];
        for(int p=0;p<2;p++)
        {
            PlaceActor(actors[p],new Vector3(pair?(p==0?-.82f:.82f):0,.055f,z),false,walk>0 && walk<1,0,p<humans && won[p]);
            actors[p].GetComponent<SpriteRenderer>().color=p==0?new Color(.76f,1,1):new Color(1,.94f,.72f);
        }
        float zoom=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.8f,8.5f,seconds));
        AimCamera(new Vector3(0,1.15f,Mathf.Lerp(1.5f,z,zoom)),false,
            Mathf.Lerp(7.8f,Mathf.Max(2.8f,3.8f/sceneCamera.aspect),zoom));
    }

    private void BuildWelcome()
    {
        var bank=new GameObject("Alcancía suspendida").transform;
        bank.SetParent(stageRoot,false);
        bank.localPosition=new Vector3(0,4.5f,4.8f);
        bank.localRotation=Quaternion.Euler(0,92,0);
        if(bankGlass==null) bankGlass=MakeGlassMaterial(new Color(.81f,.95f,1f,.23f));
        var body=Sphere("Vientre de cristal",Vector3.zero,1,bankGlass,bank);
        body.transform.localScale=new Vector3(4.2f,2.55f,2.5f);
        for(int side=-1;side<=1;side+=2)
        {
            Cylinder("Cable de suspensión",new Vector3(side*1.15f,2.65f,0),.035f,3.5f,goldMat,bank);
            var ear=Sphere("Oreja",new Vector3(1.13f,1f,side*.55f),.58f,goldMat,bank);
            ear.transform.localScale=new Vector3(.42f,.82f,.3f);
            Sphere("Ojo",new Vector3(1.58f,.40f,side*.76f),.17f,darkMat,bank);
            for(int leg=-1;leg<=1;leg+=2)
                Cylinder("Pata",new Vector3(leg*1.08f,-.96f,side*.56f),.20f,.5f,goldMat,bank);
        }
        var nose=Sphere("Hocico",new Vector3(1.97f,.04f,0),1,goldMat,bank);
        nose.transform.localScale=new Vector3(.58f,.69f,1.07f);
        for(int side=-1;side<=1;side+=2)
            Sphere("Orificio",new Vector3(2.23f,.08f,side*.21f),.12f,darkMat,bank);
        Box("Ranura",new Vector3(0,1.27f,0),new Vector3(.84f,.06f,.16f),darkMat,bank);
        for(int i=0;i<18;i++)
        {
            var bundle=new GameObject("Fajo de billetes").transform;
            bundle.SetParent(bank,false);
            bundle.localPosition=new Vector3((i%3-1)*.69f,-.67f+(i/6)*.23f,(i/3%2-.5f)*.63f);
            bundle.localRotation=Quaternion.Euler(0,(i*37)%25-12,0);
            Box("Billetes",Vector3.zero,new Vector3(.7f,.2f,.53f),grassMat,bundle);
            Box("Faja",Vector3.zero,new Vector3(.16f,.22f,.55f),goldMat,bundle);
        }
        for(int i=-1;i<=1;i++)
            SpriteActor(true,new Vector3(i*3,.04f,7.2f),1.9f).transform.rotation=Quaternion.Euler(0,ArenaView.Yaw,0);
    }

    private void BuildSwingDeck()
    {
        ropeParticipants=0;
        sceneCamera.backgroundColor=new Color(.20f,.32f,.41f);
        Box("Pasarela continua",new Vector3(0,.48f,0),new Vector3(21,.84f,5.2f),greenMat);
        for(int side=-1;side<=1;side+=2)
        {
            Box("Viga dorada",new Vector3(0,.46f,side*2.56f),new Vector3(21,.22f,.12f),goldMat);
            for(int p=-1;p<=1;p++)
                Box("Pilar inferior",new Vector3(p*8,-3.4f,side*1.8f),new Vector3(.42f,7,.42f),wallMat);
            var figure=new GameObject("Muñeca de la cuerda").AddComponent<SpriteRenderer>();
            figure.transform.SetParent(stageRoot,false);
            figure.transform.SetLocalPositionAndRotation(new Vector3(side*9.75f,.91f,0),Quaternion.Euler(0,ArenaView.Yaw,0));
            if(dollFrames.Length>0) figure.sprite=dollFrames[0];
            figure.transform.localScale=Vector3.one*2.0f;
            figure.flipX=side>0;
        }
        swingRope=CreateRopeLine("Cuerda giratoria",.13f,new Color(.92f,.58f,.24f),goldMat);
        for(int i=0;i<rosterSlots;i++) actors.Add(Actor(false));
    }

    internal void ShowRopeRound(float phase,PixelSurvivalGame.RopeRider[] slots,int humans,bool[] players,bool[] bots)
    {
        if(swingRope==null || slots==null) return;
        float angle=(phase-.83f)*2*Mathf.PI;
        // The central span sweeps the deck together, while the tips stay in the dolls' hands.
        for(int i=0;i<swingRope.positionCount;i++)
        {
            float u=i/(float)(swingRope.positionCount-1);
            float bow=Mathf.SmoothStep(0,1,Mathf.Min(u,1-u)*8f);
            swingRope.SetPosition(i,new Vector3(Mathf.Lerp(-9.7f,9.7f,u),2.64f-1.65f*Mathf.Cos(angle)*bow,1.65f*Mathf.Sin(angle)*bow));
        }
        if(phase==0 || ropeParticipants==0)
        {
            ropeParticipants=1;
            foreach(var rider in slots) ropeParticipants=Mathf.Max(ropeParticipants,1+rider.place);
        }
        int columns=Mathf.Min(10,ropeParticipants);
        for(int i=0;i<slots.Length;i++)
        {
            int place=slots[i].place;
            float x=columns==1?0:Mathf.Lerp(-7.25f,7.25f,(place%columns)/(float)(columns-1));
            var feet=new Vector3(x,.94f+slots[i].height,place/columns*.74f);
            PlaceActor(actors[i],feet,false,false,0,i<2?i<humans && players[i]:bots[i-2]);
            if(i<humans) actors[i].GetComponent<SpriteRenderer>().color=i==0?Color.cyan:new Color(1,.88f,.55f);
        }
    }

    private static Vector3 FloorPoint(int row,int lane) => new Vector3((row-3.5f)*1.92f,.05f,(1-lane)*2.15f);

    private void BuildFinal()
    {
        sceneCamera.backgroundColor=new Color(.065f,.055f,.12f);
        Box("Fondo del pozo",new Vector3(0,-7,0),new Vector3(22,.4f,11),darkMat);
        floorTiles3D=new GameObject[PixelSurvivalGame.FloorRows,PixelSurvivalGame.FloorLanes];
        for(int row=0;row<PixelSurvivalGame.FloorRows;row++)
            for(int lane=0;lane<PixelSurvivalGame.FloorLanes;lane++)
                floorTiles3D[row,lane]=Box("Loseta "+(row+1)+" · "+(lane+1),FloorPoint(row,lane)-Vector3.up*.24f,new Vector3(1.78f,.4f,2.0f),blueMat);
        for(int side=-1;side<=1;side+=2)
            Box(side<0?"Salida de jugadores":"Meta dorada",new Vector3(side*8.82f,-.21f,0),new Vector3(2.3f,.50f,6.7f),side<0?wallMat:goldMat);
        Box("Palco de los VIP",new Vector3(0,1.62f,6.5f),new Vector3(10.3f,.54f,2.8f),woodMat);
        Box("Terciopelo del palco",new Vector3(0,3.16f,7.87f),new Vector3(10.3f,3.2f,.28f),terracotta);
        Box("Balaustrada dorada",new Vector3(0,2.35f,5.18f),new Vector3(10.3f,.11f,.12f),goldMat);
        if(vipSprite==null) vipSprite=LoadSprite("VIP_Negro");
        for(int i=0;i<4;i++)
        {
            float x=-3.6f+i*2.4f;
            Sphere("Lámpara de palco",new Vector3(x,4.22f,7.66f),.27f,goldMat);
            Box("Asiento",new Vector3(x,2.13f,6.65f),new Vector3(1.24f,.45f,1.1f),redMat);
            var vip=SpriteActor(false,new Vector3(x,2.32f,6.47f),2.35f);
            vip.name="VIP de traje negro";
            vip.GetComponent<SpriteRenderer>().sprite=vipSprite;
            vip.transform.rotation=Quaternion.Euler(0,ArenaView.Yaw,0);
        }
        for(int i=0;i<rosterSlots;i++) actors.Add(Actor(false));
    }

    internal void ShowFloor(PixelSurvivalGame.FloorRider[] riders,float[,] deadlines,float now,int humans,bool[] players,bool[] bots)
    {
        if(riders==null || floorTiles3D==null) return;
        for(int r=0;r<PixelSurvivalGame.FloorRows;r++)
            for(int l=0;l<PixelSurvivalGame.FloorLanes;l++)
            {
                var tile=floorTiles3D[r,l];
                float age=now-deadlines[r,l];
                bool falling=age>=0;
                tile.SetActive(age<1.2f);
                if(!tile.activeSelf) continue;
                bool touched=!float.IsInfinity(deadlines[r,l]);
                SetMaterial(tile,touched?(age>-.55f?redMat:goldMat):blueMat);
                float offset=falling?Mathf.Min(12,age*age*12):0;
                tile.transform.localPosition=FloorPoint(r,l)-Vector3.up*(.24f+offset);
                tile.transform.localRotation=Quaternion.Euler(touched?(falling?age*35:Mathf.Sin(now*29)*1.8f):0,0,0);
            }
        for(int i=0;i<riders.Length;i++)
        {
            var rider=riders[i];
            Vector3 feet=FloorPoint(rider.row,rider.lane);
            if(rider.airborne)
                feet=Vector3.Lerp(FloorPoint(rider.row-1,rider.fromLane),feet,rider.travel)+Vector3.up*(1.4f*Mathf.Sin(Mathf.PI*rider.travel));
            feet.x+=i<2?-.18f:(i%3)*.16f;
            PlaceActor(actors[i],feet,false,rider.airborne,0,i<2?i<humans && players[i]:bots[i-2]);
            if(i<humans) actors[i].GetComponent<SpriteRenderer>().color=i==0?Color.cyan:new Color(1,.88f,.55f);
        }
    }

    public void ShowGlassFalls(float[] falls,float now)
    {
        for(int b=0;b<falls.Length;b++)
        {
            float age=now-falls[b];
            if(age<0 || age>.85f) continue;
            int slot=b+2;
            Vector3 feet=Vector3.Lerp(jumpStart[slot],jumpEnd[slot],Mathf.Clamp01(age/.28f));
            feet.y+=Mathf.Sin(Mathf.PI*Mathf.Clamp01(age/.28f))*.75f-Mathf.Pow(Mathf.Max(0,age-.24f),2)*17f;
            PlaceActor(actors[slot],feet,false,false,0,false,true);
        }
    }
}
