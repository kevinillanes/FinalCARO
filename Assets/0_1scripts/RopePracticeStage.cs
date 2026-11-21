using UnityEngine;

public sealed class RopePracticeStage : MonoBehaviour
{
    private Camera view;
    private Material steel, amber, enamel;
    private Sprite character;
    private LineRenderer cable;
    private bool jump;

    public void Build(bool jumping,Transform marker)
    {
        jump=jumping;
        view=Camera.main;
        view.orthographic=true;
        view.backgroundColor=new Color(.21f,.32f,.4f);
        view.transform.SetPositionAndRotation(Vector3.up-ArenaView.Rotation*Vector3.forward*39,ArenaView.Rotation);
        ArenaView.KeyLight(transform);
        steel=Metal(new Color(.14f,.23f,.28f));
        amber=Metal(new Color(.94f,.57f,.20f));
        enamel=Metal(new Color(.24f,.49f,.50f));
        var tex=Resources.Load<Texture2D>("Characters/Player_Idle_A");
        if(tex!=null) character=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.05f),70f);
        foreach(var sr in transform.root.GetComponentsInChildren<SpriteRenderer>())
        {
            string n=sr.name;
            if (n.StartsWith("Player") || n.StartsWith("NPC_") || n.StartsWith("Participante"))
            {
                sr.transform.rotation=Quaternion.Euler(0,ArenaView.Yaw,0);
                if(character!=null) sr.sprite=character;
                sr.color=Color.Lerp(Color.white,sr.color,.42f);
                sr.sortingOrder=0;
                sr.sortingLayerID=0;
                sr.spriteSortPoint=SpriteSortPoint.Pivot;
                Vector3 pos=sr.transform.position;
                sr.transform.position=new Vector3(pos.x,.92f,jumping?0f:-.27f);
            }
            else sr.enabled=false;
        }
        foreach(var text in transform.root.GetComponentsInChildren<TextMesh>())
            text.GetComponent<MeshRenderer>().enabled=false;
        for(int side=-1;side<=1;side+=2)
        {
            float middle=side*(jump?11.65f:13.5f),length=jump?23.3f:19f;
            Block("Tablero",new Vector3(middle,.42f,0),new Vector3(length,.85f,5),enamel);
            for(int edge=-1;edge<=1;edge+=2)
                Block("Viga de borde",new Vector3(middle,.26f,edge*2.46f),new Vector3(length,.35f,.13f),amber);
            for(int post=0;post<3;post++)
            {
                float x=middle-length*.39f+post*length*.39f;
                Block("Pilar",new Vector3(x,-3.7f,1.7f),new Vector3(.46f,8,.46f),steel);
                Block("Travesaño",new Vector3(x,-.25f,0),new Vector3(.26f,.35f,5.3f),steel);
            }
            Block("Soporte",new Vector3(side*21,1.9f,0),new Vector3(.35f,2.4f,.35f),amber);
        }
        for(int bay=0;bay<5;bay++)
        {
            float x=bay*12f-24f;
            Block("Pasillo de mantenimiento",new Vector3(x,-7.4f,9),new Vector3(11.9f,.38f,2.5f),steel);
            Block("Baranda de servicio",new Vector3(x,-6.25f,10.2f),new Vector3(12,.1f,.1f),amber);
            for(int side=-1;side<=1;side+=2)
            {
                Block("Montante",new Vector3(x+side*4,-6.82f,10.2f),new Vector3(.1f,1.2f,.1f),steel);
                var brace=Block("Riostra",new Vector3(x+side*1.7f,-3.7f,1.7f),new Vector3(.22f,7.15f,.22f),steel);
                brace.transform.rotation=Quaternion.Euler(0,0,side*32f);
            }
        }
        cable=new GameObject("Cuerda suspendida").AddComponent<LineRenderer>();
        cable.transform.SetParent(transform,false);
        cable.sharedMaterial=amber;
        cable.positionCount=41; cable.widthMultiplier=.18f;
        if(marker!=null && !jump)
        {
            var knot=Block("Testigo",Vector3.zero,new Vector3(.36f,.65f,.30f),amber);
            knot.transform.SetParent(marker,false);
            knot.transform.localScale=new Vector3(.36f/Mathf.Max(.01f,marker.lossyScale.x),.65f/Mathf.Max(.01f,marker.lossyScale.y),.3f);
        }
        DrawCable(0f,0f);
    }
    public void DrawCable(float phase,float tension)
    {
        float angle=(phase-.8f)*2*Mathf.PI;
        for(int i=0;i<cable.positionCount;i++)
        {
            float u=i/(float)(cable.positionCount-1), bow=4*u*(1-u);
            float height=jump?3.15f-2.22f*Mathf.Cos(angle)*bow:2.42f-(.45f-.18f*Mathf.Abs(tension))*bow;
            float depth=jump?2.22f*Mathf.Sin(angle)*bow:0;
            cable.SetPosition(i,new Vector3(Mathf.Lerp(-21,21,u),height,depth));
        }
    }
    private void LateUpdate()
    {
        if(view!=null) view.orthographicSize=Mathf.Max(13f,21f/view.aspect);
    }
    private GameObject Block(string label,Vector3 position,Vector3 size,Material mat)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name=label; part.transform.SetParent(transform,false);part.transform.position=position;
        part.transform.localScale=size;part.GetComponent<MeshRenderer>().sharedMaterial=mat;
        return part;
    }
    private static Material Metal(Color color)=>new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};
    private void OnDestroy(){Destroy(steel);Destroy(amber);Destroy(enamel);if(character!=null)Destroy(character);}
}
