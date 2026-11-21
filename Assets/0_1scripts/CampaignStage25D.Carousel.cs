using UnityEngine;

public sealed partial class CampaignStage25D
{
    private Transform carouselPlatform;
    private Mesh carouselCanopy;
    private readonly Transform[] carouselShutters = new Transform[PixelSurvivalGame.CarouselDoors.Length];
    private readonly Renderer[] doorLamps = new Renderer[PixelSurvivalGame.CarouselDoors.Length];
    public bool HasCarousel => carouselPlatform != null;

    private void BuildCarousel()
    {
        sceneCamera.backgroundColor = new Color(.105f,.085f,.18f);
        Box("Pista de la feria",new Vector3(0,-.25f,0),new Vector3(22,.5f,18),floorMat);
        Cylinder("Cenefa de la pista",new Vector3(0,.014f,0),4.95f,.025f,goldMat);
        Cylinder("Anillo de la pista",new Vector3(0,.032f,0),4.84f,.025f,pinkMat);
        Cylinder("Borde interior",new Vector3(0,.05f,0),4.55f,.025f,whiteMat);
        Box("Pared del fondo",new Vector3(0,2.6f,8.2f),new Vector3(22,5.2f,.5f),wallMat);
        for (int side=-1;side<=1;side+=2)
            Box("Pared lateral",new Vector3(side*10.8f,side<0?.32f:2.2f,.8f),new Vector3(.4f,side<0?.64f:4.4f,15),wallMat);
        for (int stripe=0;stripe<19;stripe++)
        {
            Box("Franja del pabellón",new Vector3(stripe*1.12f-10,4.5f,7.89f),new Vector3(1.12f,1.45f,.12f),stripe%2==0?pinkMat:whiteMat);
            Sphere("Guirnalda",new Vector3(stripe*1.12f-10,3.78f+.13f*Mathf.Cos(stripe*Mathf.PI/3),7.77f),.20f,goldMat);
        }
        carouselPlatform = new GameObject("Plataforma giratoria").transform;
        carouselPlatform.SetParent(stageRoot,false);
        Cylinder("Borde dorado",new Vector3(0,.13f,0),4.2f,.26f,goldMat,carouselPlatform);
        Cylinder("Disco",new Vector3(0,.31f,0),4.02f,.14f,blueMat,carouselPlatform);
        Cylinder("Eje",new Vector3(0,1.7f,0),.42f,2.8f,pinkMat,carouselPlatform);
        Cylinder("Corona",new Vector3(0,3.17f,0),1.45f,.24f,goldMat,carouselPlatform);
        AddCarouselCanopy();
        for (int n=0;n<12;n++)
        {
            float a=n*Mathf.PI/6;
            Vector3 spoke=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
            Box("Radio del disco",spoke*2.1f+Vector3.up*.39f,new Vector3(.075f,.02f,3.4f),goldMat,carouselPlatform).transform.localRotation=Quaternion.Euler(0,n*30,0);
            Sphere("Bombilla",spoke*4.06f+Vector3.up*.28f,.12f,whiteMat,carouselPlatform);
            if (n%3!=0) continue;
            Vector3 basePoint=spoke*1.22f;
            Cylinder("Barra del caballito",basePoint+Vector3.up*1.75f,.055f,2.85f,goldMat,carouselPlatform);
            Transform horse = new GameObject("Caballito de feria").transform;
            horse.SetParent(carouselPlatform,false);
            horse.localPosition=basePoint+Vector3.up*(1.1f+.18f*(n%2));
            horse.localRotation=Quaternion.Euler(0,n*30f,0);
            Box("Lomo",Vector3.zero,new Vector3(.85f,.35f,.33f),whiteMat,horse);
            Box("Cabeza",new Vector3(.36f,.24f,0),new Vector3(.31f,.38f,.29f),whiteMat,horse);
            Box("Silla",new Vector3(-.03f,.20f,0),new Vector3(.39f,.10f,.35f),redMat,horse);
            for (int leg=-1;leg<=1;leg+=2)
                Box("Pata",new Vector3(leg*.26f,-.23f,0),new Vector3(.13f,.27f,.23f),whiteMat,horse);
        }
        for (int d=0;d<carouselShutters.Length;d++)
        {
            Vector2 entry=PixelSurvivalGame.CarouselDoors[d];
            Transform frame=new GameObject("Habitación "+(d+1)).transform;
            frame.SetParent(stageRoot,false);
            frame.localPosition=new Vector3(entry.x,0,entry.y);
            // Each side opening faces the middle and remains visible from the playing camera.
            frame.localRotation=Quaternion.Euler(0,entry.x < -8 ? -5 : entry.x > 8 ? 58 : 0,0);
            Material trim=d%3==0?pinkMat:d%3==1?blueMat:goldMat;
            Box("Interior",new Vector3(0,1.22f,1.85f),new Vector3(2.3f,2.45f,.27f),darkMat,frame);
            Box("Piso del cuarto",new Vector3(0,.07f,1.15f),new Vector3(2.22f,.12f,1.8f),floorMat,frame);
            for(int side=-1;side<=1;side+=2)
                Box("Tabique",new Vector3(side*1.1f,1.22f,1.15f),new Vector3(.15f,2.45f,1.7f),wallMat,frame);
            Box("Umbral",new Vector3(0,.05f,0),new Vector3(2.4f,.10f,1.3f),trim,frame);
            for (int dash=0;dash<3;dash++)
                Box("Camino al umbral",new Vector3(0,.033f,-.9f-dash*.44f),new Vector3(.58f-dash*.1f,.035f,.16f),whiteMat,frame);
            for(int side=-1;side<=1;side+=2)
                Box("Jamba",new Vector3(side*1.1f,1.30f,.38f),new Vector3(.22f,2.6f,.35f),trim,frame);
            Box("Dintel",new Vector3(0,2.60f,.38f),new Vector3(2.42f,.22f,.35f),trim,frame);
            // Fixed roofs stay intact when the camera cuts away taller side walls.
            int roofLayer=LayerMask.NameToLayer("Ignore Raycast");
            Box("Techo continuo",new Vector3(0,2.73f,1.12f),new Vector3(2.62f,.20f,2.32f),trim,frame).layer=roofLayer;
            Box("Cubierta",new Vector3(0,2.87f,1.15f),new Vector3(2.39f,.12f,2.03f),wallMat,frame).layer=roofLayer;
            for(int stripe=0;stripe<7;stripe++)
                Box("Cenefa",new Vector3((stripe-3)*.36f,2.61f,-.025f),new Vector3(.36f,.14f,.14f),stripe%2==0?trim:whiteMat,frame).layer=roofLayer;
            carouselShutters[d]=Box("Puerta",new Vector3(0,1.25f,.48f),new Vector3(1.96f,2.4f,.17f),trim,frame).transform;
            doorLamps[d]=Sphere("Indicador",new Vector3(0,2.84f,-.08f),.22f,goldMat,frame).GetComponent<Renderer>();
        }
        for (int i=0;i<rosterSlots;i++) actors.Add(Actor(false));
    }

    private void AddCarouselCanopy()
    {
        if (carouselCanopy==null)
        {
            const int sectors=16;
            var vertices=new Vector3[sectors*3];
            var rose=new int[sectors/2*3];
            var ivory=new int[rose.Length];
            for (int i=0;i<sectors;i++)
            {
                float a=i*2*Mathf.PI/sectors,b=(i+1)*2*Mathf.PI/sectors;
                vertices[3*i]=new Vector3(0,4.15f,0);
                vertices[3*i+1]=new Vector3(Mathf.Sin(a)*1.87f,3.19f,Mathf.Cos(a)*1.87f);
                vertices[3*i+2]=new Vector3(Mathf.Sin(b)*1.87f,3.19f,Mathf.Cos(b)*1.87f);
                int[] band=i%2==0?rose:ivory;
                for (int j=0;j<3;j++) band[i/2*3+j]=3*i+j;
            }
            carouselCanopy=new Mesh {name="Lona a rayas",vertices=vertices,subMeshCount=2};
            carouselCanopy.SetTriangles(rose,0);
            carouselCanopy.SetTriangles(ivory,1);
            carouselCanopy.RecalculateNormals();
            carouselCanopy.RecalculateBounds();
        }
        var canopy=new GameObject("Toldo del carrusel",typeof(MeshFilter),typeof(MeshRenderer));
        canopy.transform.SetParent(carouselPlatform,false);
        canopy.GetComponent<MeshFilter>().sharedMesh=carouselCanopy;
        canopy.AddComponent<MeshCollider>().sharedMesh=carouselCanopy;
        canopy.GetComponent<MeshRenderer>().sharedMaterials=new[]{pinkMat,whiteMat};
        Cylinder("Alero",new Vector3(0,3.17f,0),1.87f,.12f,goldMat,carouselPlatform);
        Sphere("Remate",new Vector3(0,4.18f,0),.32f,goldMat,carouselPlatform);
    }

    public void ShowCarousel(float angle,int[] rooms,int[] counts,int[] capacity,int humans,Vector2[] players,
        Vector2[] bots,bool[] alive,bool open,bool[] moving)
    {
        carouselPlatform.localRotation=Quaternion.Euler(0,angle,0);
        for (int d=0;d<carouselShutters.Length;d++)
        {
            Transform shutter=carouselShutters[d];
            Vector3 scale=shutter.localScale;
            scale.y=Mathf.MoveTowards(scale.y,open?.10f:2.4f,Time.deltaTime*9f);
            shutter.localScale=scale;
            shutter.localPosition=new Vector3(0,2.45f-scale.y*.5f,.48f);
            doorLamps[d].sharedMaterial=!open?whiteMat:counts[d]==capacity[d]?greenMat:counts[d]>capacity[d]?redMat:goldMat;
        }
        for (int i=0;i<actors.Count;i++)
        {
            Vector2 pos=i<2?players[i]:bots[i-2];
            bool visible=(i<2?i<humans:alive[i-2]) && rooms[i]<0;
            float height=pos.sqrMagnitude<4.0f*4.0f?.4f:.06f;
            PlaceActor(actors[i],new Vector3(pos.x,height,pos.y),false,open && (i>=2 || moving[i]),0,visible);
            if (i<humans && visible) actors[i].GetComponent<SpriteRenderer>().color=i==0?new Color(.76f,1f,1f):new Color(1,.94f,.72f);
        }
    }

    private Vector2 CarouselLabel(Vector3 world)
    {
        Vector3 screenPoint=sceneCamera.WorldToScreenPoint(world);
        float scale=Mathf.Min(Screen.width/960f,Screen.height/540f);
        return new Vector2((screenPoint.x-Screen.width*.5f)/scale+480f,(Screen.height*.5f-screenPoint.y)/scale+270f);
    }

    public void DrawCarouselMarkers(int[] counts,int[] capacity,GUIStyle style,int humans,int[] rooms)
    {
        if (!HasCarousel) return;
        for (int d=0;d<counts.Length;d++)
        {
            Vector2 entry=PixelSurvivalGame.CarouselDoors[d];
            Vector2 point=CarouselLabel(new Vector3(entry.x,3f,entry.y));
            bool one=rooms[0]==d, two=humans==2 && rooms[1]==d;
            string marker=one && two?"J1 + J2":one?"J1":two?"J2":"";
            string count=counts[d]+"/"+capacity[d];
            Rect badge=new Rect(point.x-29,point.y-13,58,24);
            GUI.color=counts[d]==capacity[d]?new Color(.06f,.36f,.22f,.96f):counts[d]>capacity[d]?new Color(.6f,.08f,.16f,.96f):new Color(.06f,.06f,.14f,.94f);
            GUI.DrawTexture(badge,Texture2D.whiteTexture);
            GUI.color=Color.white;
            GUI.Label(badge,(d+1)+" · "+count,style);
            if (marker.Length>0) GUI.Label(new Rect(point.x-42,point.y+12,84,24),marker,style);
        }
        for (int p=0;p<humans;p++)
        {
            if (rooms[p]>=0) continue;
            Vector2 head=CarouselLabel(actors[p].transform.position+Vector3.up*1.8f);
            GUI.color=p==0?Color.cyan:new Color(1,.86f,.4f);
            GUI.Label(new Rect(head.x-22,head.y-12,44,24),"J"+(p+1),style);
            GUI.color=Color.white;
        }
    }
}
