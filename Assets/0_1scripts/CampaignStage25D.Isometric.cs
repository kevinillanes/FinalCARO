using UnityEngine;

public sealed partial class CampaignStage25D
{
    private static Texture2D SurfacePrint(bool tiles)
    {
        const int size=64;
        var texture=new Texture2D(size,size,TextureFormat.RGB24,true){name=tiles?"Baldosa":"Albero",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
        var random=new System.Random(tiles?13:52);
        var pixels=new Color32[size*size];
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            int n=random.Next(12);
            int value=tiles ? ((x==0 || y==0)?168:(x==1 || y==1)?248:230+n) : 227+n;
            if(!tiles && n==0) value-=17;
            pixels[x+size*y]=new Color32((byte)value,(byte)value,(byte)value,255);
        }
        texture.SetPixels32(pixels); texture.Apply(true,true);
        return texture;
    }

    private void FrameArena(int screen,int trial,float phase,int people,float p1,float p2,Vector2 one,Vector2 two)
    {
        float aspect=Mathf.Max(.35f,sceneCamera.aspect);
        float size=Mathf.Max(10.8f,17.4f/aspect);
        Vector3 focus=new Vector3(0,1.1f,1);
        bool stage=screen==Round || screen==Result || screen==GameOver || screen==Interlude;
        bool follow=screen==Round;
        if (stage && trial==RedLight)
        {
            float first=RedLightTrackZ(p1),second=people==2?RedLightTrackZ(p2):first;
            float spread=Mathf.Abs(first-second);
            focus=new Vector3(0,1f,(first+second)*.5f+2.3f);
            size=Mathf.Max(8.3f+.22f*spread,(11f+.38f*spread)/aspect);
            if(screen==Round && phase<3f)
            {
                float opening=Mathf.SmoothStep(0,1,Mathf.Clamp01((phase-.7f)/2.3f));
                focus=Vector3.Lerp(new Vector3(0,1,9),focus,opening);
                size=Mathf.Lerp(Mathf.Max(18,31/aspect),size,opening);
            }
        }
        if (stage && trial==Final && follow)
        {
            Vector2 pos=people==2?(one+two)*.5f:one;
            focus=new Vector3(pos.x/55f*.60f,1,pos.y/26f*.50f);
            size=Mathf.Max(8.8f,14.2f/aspect);
        }
        if (stage && trial==Tug)
        {
            focus=new Vector3(0,2.8f,1.5f);
            size=Mathf.Max(5.3f+tugEnd*.25f,(tugEnd*.8f+2.6f)/aspect);
        }
        if (HasCarousel)
        {
            focus=new Vector3(0,1.4f,1.5f);
            size=Mathf.Max(8.6f,12.9f/aspect);
        }
        if(stage && trial==Cookie) size=Mathf.Max(9.3f,14.6f/aspect);
        if (screen==Interlude) focus+=Vector3.right*Mathf.Lerp(-1.5f,0,Mathf.SmoothStep(0,1,phase/6));
        AimCamera(focus,follow,size);
    }

    public void RevealContestants(int count)
    {
        foreach(var r in cutaway) if(r!=null) r.forceRenderingOff=false;
        cutaway.Clear();
        if (lastScreen!=Round || actors.Count<count) return;
        for(int i=0;i<count;i++)
        {
            if(!actors[i].activeSelf) continue;
            Vector3 head=actors[i].transform.position+Vector3.up*.85f;
            Vector3 direction=-(sceneCamera.transform.forward);
            int hits=Physics.RaycastNonAlloc(head,direction,obstructionHits,45f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            for(int h=0;h<hits;h++)
            {
                var col=obstructionHits[h].collider;
                if(!col.transform.IsChildOf(stageRoot) || col.bounds.max.y<head.y) continue;
                var renderer=col.GetComponent<MeshRenderer>();
                if(renderer!=null) {renderer.forceRenderingOff=true; cutaway.Add(renderer);}
            }
        }
    }

    private void BuildCourtyard()
    {
        Box("Campiña",new Vector3(0,-1.05f,10),new Vector3(180,.45f,180),grassMat);
        Box("Camino de servicio",new Vector3(0,-.79f,-38),new Vector3(4.4f,.12f,25f),sandstone);
        Box("Terreno exterior",new Vector3(0,-.64f,10),new Vector3(42,.45f,72),roomFloorMat);
        for (int side=-1;side<=1;side+=2)
        {
            float x=side*10;
            Box("Camino de ronda",new Vector3(x,-.06f,10),new Vector3(2,.14f,58),terracotta);
            Box("Muro de recinto",new Vector3(x+side*1.3f,side>0?1.55f:.3f,10),new Vector3(.45f,side>0?3.1f:.6f,59),sandstone);
            for(int i=0;i<8;i++)
            {
                float z=-15+i*7.5f;
                Box("Contrafuerte",new Vector3(x+side*1.3f,side>0?1.65f:.36f,z),new Vector3(.75f,side>0?3.3f:.72f,.72f),terracotta);
                if (side>0 && i%2==0)
                {
                    Box("Caseta",new Vector3(14.5f,1.65f,z),new Vector3(4,3.5f,4.6f),sandstone);
                    Box("Alero de caseta",new Vector3(14.5f,3.45f,z),new Vector3(4.7f,.35f,5.2f),terracotta);
                    for(int slope=-1;slope<=1;slope+=2)
                        Box("Tejado de teja",new Vector3(14.5f+slope*1.12f,3.96f,z),new Vector3(2.56f,.24f,5.2f),terracotta).transform.rotation=Quaternion.Euler(0,0,-slope*23f);
                    Box("Ventana",new Vector3(12.47f,1.9f,z),new Vector3(.08f,1.05f,1.35f),blueMat);
                }
                Box("Banda de distancia",new Vector3(x-side*2,.045f,z),new Vector3(1.1f,.04f,.2f),goldMat);
                if(side==1 && i%2==1)
                {
                    Box("Jardinera de ronda",new Vector3(12.5f,.15f,z),new Vector3(2.2f,.6f,3.5f),terracotta);
                    for(int shrub=0;shrub<3;shrub++)
                        Sphere("Seto",new Vector3(12.5f,.65f,z+shrub-1f),1f,greenMat).transform.localScale=new Vector3(1.45f,.82f,1.22f);
                }
            }
        }
        {
            const float z=-24;
            Box("Suelo del pasaje",new Vector3(0,.02f,z),new Vector3(5,.14f,7.2f),terracotta);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Galería",new Vector3(side*6.7f,1.4f,z),new Vector3(8,2.8f,2.8f),sandstone);
                Box("Mocheta",new Vector3(side*2.58f,1.7f,z),new Vector3(.34f,3.4f,5.5f),terracotta);
            }
            Box("Dintel del pasaje",new Vector3(0,3.45f,z),new Vector3(5.5f,.35f,4.8f),terracotta);
        }
    }

    private void PaintWall(string label,Vector3 center,Vector2 size)
    {
        // Painted panels sit against the actual elevation: all marks share the wall's plane.
        var panel=Box(label,center,new Vector3(size.x,size.y,.09f),wallMat);
        float z=panel.transform.position.z-.085f;
        for(int section=-1;section<=1;section++)
        {
            float x=center.x+section*size.x*.29f,y=center.y+.05f*size.y,r=size.y*.18f;
            int corners=section==-1?18:section==0?3:4;
            float start=section==1?Mathf.PI/4:Mathf.PI*.5f;
            for(int i=0;i<corners;i++)
            {
                float a=start+i*2*Mathf.PI/corners,b=start+(i+1)*2*Mathf.PI/corners;
                var first=new Vector3(x+r*Mathf.Cos(a),y+r*Mathf.Sin(a),z);
                var second=new Vector3(x+r*Mathf.Cos(b),y+r*Mathf.Sin(b),z);
                var edge=Box("Esmalte", (first+second)*.5f,new Vector3((first-second).magnitude+.02f,.087f,.07f),section==0?pinkMat:goldMat);
                edge.transform.right=(second-first).normalized;
            }
        }
        Box("Zócalo",new Vector3(center.x,center.y-size.y*.30f,z),new Vector3(size.x,.19f,.1f),blueMat);
    }

    private void BuildWardDetails()
    {
        Box("Testero",new Vector3(0,2.5f,10.4f),new Vector3(21,5,.45f),wallMat);
        for(int row=0;row<4;row++)
        {
            float z=-5+4*row;
            for(int side=-1;side<=1;side+=2)
            {
                float x=8.5f*side;
                for(int level=0;level<2;level++)
                    Box("Almohada",new Vector3(x,.68f+level*1.6f,z+1.14f),new Vector3(1.35f,.16f,.65f),whiteMat);
                for(int rung=0;rung<4;rung++)
                    Box("Peldaño",new Vector3(x-side*.9f,.35f+rung*.48f,z+.45f),new Vector3(.10f,.085f,.64f),goldMat);
            }
            Box("Junta del suelo",new Vector3(0,.015f,z),new Vector3(20,.018f,.035f),wallMat);
        }
    }
}
