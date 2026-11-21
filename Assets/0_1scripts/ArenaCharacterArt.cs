using UnityEngine;

// Four newly drawn views share the palette and pixel density of the supplied
// three-quarter artwork. One atlas serves every contestant in the arena.
internal sealed class ArenaCharacterArt
{
    public const int Front=0, Profile=1, RearQuarter=2, Rear=3;
    private readonly Sprite[,] frames=new Sprite[4,3];
    private readonly Texture2D atlas;
    public Sprite Shadow { get; }
    public Sprite Frame(int view,int step)=>frames[view,step];

    public ArenaCharacterArt()
    {
        const int w=22,h=24;
        atlas=new Texture2D(w*5,h*3,TextureFormat.RGBA32,false) {name="Participantes · vistas",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color32[atlas.width*atlas.height];
        Color32 ink=new Color32(14,19,20,255), hair=new Color32(49,31,24,255),skin=new Color32(242,189,128,255),
            shade=new Color32(173,119,77,255), jacket=new Color32(7,85,65,255),green=new Color32(3,53,45,255),
            trim=new Color32(193,211,199,255),sole=new Color32(135,146,143,255);
        for(int view=0;view<4;view++)
        for(int phase=0;phase<3;phase++)
        {
            void Block(int x,int y,int width,int height,Color32 color)
            {
                for(int j=y;j<y+height;j++)
                    for(int i=x;i<x+width;i++) pixels[(phase*h+h-1-j)*atlas.width+view*w+i]=color;
            }
            bool slim=view==Profile, back=view>=RearQuarter;
            int l=slim?7:5,r=slim?14:16;
            int stride=phase==1?-1:phase==2?1:0;
            for(int side=-1;side<=1;side+=2)
            {
                int foot=(side<0?l+1:r-3)+side*stride, lift=side*stride>0?1:0;
                Block(foot,16,4,6-lift,ink);
                Block(foot+1,17,3,4-lift,green);
                Block(foot-1,21-lift,6,2,ink);
                Block(foot,21-lift,4,1,sole);
            }
            Block(l,10,r-l+1,8,ink); Block(l+1,11,r-l-1,6,jacket);
            Block(l+1,16,r-l-1,1,green);
            if(back){Block(9,12,5,2,trim);Block(10,12,1,1,green);Block(12,12,1,1,green);}
            else Block(slim?13:10,12,1,5,green);
            for(int side=-1;side<=1;side+=2)
            {
                int arm=(side<0?l-2:r-1)+(slim?stride*side:0), y=11+side*stride;
                Block(arm,y,4,7,ink);Block(arm+1,y,2,4,jacket);Block(arm+1,y,2,1,trim);Block(arm+1,y+4,2,2,skin);
            }
            int head=slim?8:6;
            Block(head,2,11,8,ink);Block(head+1,1,9,9,hair);
            Block(head+2,9,7,2,shade);
            if(!back)
            {
                Block(head+(slim?4:1),4,slim?7:9,5,skin);
                Block(head+8,5,2,2,ink);
                if(!slim)Block(head+2,5,2,2,ink);
                if(slim)Block(head+10,6,2,2,skin);
                Block(head+5,8,3,1,shade);
            }
            else
            {
                Block(head+1,3,9,6,hair);
                Block(head+1,7,7,2,ink);
                if(view==RearQuarter)Block(head+9,5,2,3,skin);
            }
            frames[view,phase]=Sprite.Create(atlas,new Rect(view*w,phase*h,w,h),new Vector2(.5f,.045f),11.5f,0,SpriteMeshType.FullRect);
        }
        for(int y=0;y<h;y++)
            for(int x=0;x<w;x++)
                if(Mathf.Pow((x-10.5f)/10f,2)+Mathf.Pow((y-11.5f)/9f,2)<1)
                    pixels[y*atlas.width+4*w+x]=new Color32(14,19,29,100);
        atlas.SetPixels32(pixels); atlas.Apply(false,true);
        Shadow=Sprite.Create(atlas,new Rect(4*w,0,w,h),new Vector2(.5f,.5f),22,0,SpriteMeshType.FullRect);
    }
    public void Dispose()
    {
        foreach(var sprite in frames) Object.Destroy(sprite);
        Object.Destroy(Shadow); Object.Destroy(atlas);
    }
}

internal sealed class ArenaCharacterPose
{
    private SpriteRenderer image;
    private Transform shadow;
    private Vector3 previous;
    private Vector2 facing=new Vector2(.7f,-.7f);
    private float stride,death,breath,pace;

    public void Draw(Transform actor,Vector3 feet,float floor,bool alive,bool moving,bool guard,float yaw,float dt,ArenaCharacterArt art,
        Sprite idle,Sprite walkA,Sprite walkB,Sprite armed)
    {
        if(image==null)
        {
            image=actor.GetComponent<SpriteRenderer>();
            previous=feet;
            breath=feet.x+feet.z;
            shadow=new GameObject("Sombra de contacto").transform;
            shadow.SetParent(actor,false);
            shadow.gameObject.AddComponent<SpriteRenderer>().sprite=art.Shadow;
        }
        Vector2 delta=new Vector2(feet.x-previous.x,feet.z-previous.z);
        previous=feet;
        float distance=delta.magnitude;
        bool stepping=alive && moving && distance>.0001f && distance<2.5f;
        if(stepping)
        {
            Vector2 travel=delta/distance;
            facing=new Vector2(Vector2.Dot(travel,ArenaView.Right),Vector2.Dot(travel,ArenaView.Forward));
            stride+=distance*5.5f;
        }
        pace=Mathf.MoveTowards(pace,stepping?Mathf.Clamp01(distance/Mathf.Max(dt,.001f)/4f):0,dt*9f);
        breath+=dt*2.6f;
        death=Mathf.MoveTowards(death,alive?0:1,dt*3.8f);
        float footfall=Mathf.Sin(stride);
        int step=pace<.06f?0:footfall>0?1:2;
        float side=Mathf.Abs(facing.x);
        if(guard) image.sprite=armed;
        else if(facing.y>.38f) image.sprite=art.Frame(side<.38f?ArenaCharacterArt.Rear:ArenaCharacterArt.RearQuarter,step);
        else if(Mathf.Abs(facing.y)<.38f) image.sprite=art.Frame(ArenaCharacterArt.Profile,step);
        else if(side<.38f) image.sprite=art.Frame(ArenaCharacterArt.Front,step);
        else image.sprite=step==0?idle:step==1?walkA:walkB;
        image.flipX=!guard && facing.x<-.08f;
        image.spriteSortPoint=SpriteSortPoint.Pivot;
        image.sortingOrder=0;
        image.color=alive?Color.white:new Color(.56f,.58f,.63f);
        float lift=(Mathf.Abs(footfall)*.075f*pace+Mathf.Sin(breath)*.009f)*(1-death);
        actor.SetPositionAndRotation(feet+Vector3.up*lift,Quaternion.Euler(0,ArenaView.Yaw+yaw,0)*Quaternion.Euler(0,0,death*82f-facing.x*pace*2.2f));
        float flight=Mathf.Clamp01(feet.y-floor);
        shadow.SetPositionAndRotation(new Vector3(feet.x,floor+.014f,feet.z),Quaternion.Euler(90,ArenaView.Yaw,0));
        shadow.localScale=new Vector3(1.1f,.68f,1f)*(1-.25f*flight);
    }
}
