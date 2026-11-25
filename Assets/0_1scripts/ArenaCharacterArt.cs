using UnityEngine;
using System.Collections.Generic;

internal enum CastSkin { Green_Unarmed, Green_Flashlight, Red_Unarmed, Red_Pistol, Red_Rifle, Black_Unarmed, Black_Knife }
internal enum CastMotion { Idle, Walk, Jump, Death, Pull }

internal sealed class ArenaCharacterArt
{
    private const int Width=40, Height=44;
    private static readonly int[] Lengths={5,8,6,7,6};
    private static readonly Color32[] HairColors = {
        new Color32(28,14,6,255), new Color32(119,71,43,255), new Color32(203,157,79,255),
        new Color32(164,74,47,255), new Color32(154,156,164,255), new Color32(83,55,75,255),
        new Color32(72,75,86,255)
    };
    private readonly Dictionary<(CastSkin,int),Sprite[][]> strips = new Dictionary<(CastSkin,int),Sprite[][]>();
    private readonly List<Texture2D> coloredAtlases = new List<Texture2D>();
    private Texture2D badgeAtlas;
    private readonly Dictionary<int,Sprite> badges = new Dictionary<int,Sprite>();
    private readonly Texture2D shadowTexture;
    public Sprite Shadow { get; }

    public ArenaCharacterArt()
    {
        shadowTexture=new Texture2D(22,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
        var pixels=new Color32[22*16];
        for(int y=0;y<16;y++)
            for(int x=0;x<22;x++)
                if(Mathf.Pow((x-10.5f)/10f,2)+Mathf.Pow((y-7.5f)/7f,2)<1)
                    pixels[y*22+x]=new Color32(14,19,29,95);
        shadowTexture.SetPixels32(pixels); shadowTexture.Apply(false,true);
        Shadow=Sprite.Create(shadowTexture,new Rect(0,0,22,16),new Vector2(.5f,.5f),22);
    }

    public Sprite Frame(CastSkin skin,CastMotion motion,int view,int frame,int participant=0)
    {
        int hair = participant > 0 && (skin == CastSkin.Green_Unarmed || skin == CastSkin.Green_Flashlight)
            ? (participant - 1) * 3 % HairColors.Length : 0;
        if(!strips.TryGetValue((skin,hair),out var rows))
        {
            var texture=Resources.Load<Texture2D>("Characters/Cast/"+skin);
            if(texture==null) throw new System.InvalidOperationException("Falta el atlas de "+skin);
            if (hair != 0) texture = RecolorHair(texture, skin, HairColors[hair]);
            rows=new Sprite[texture.height/Height][];
            strips.Add((skin,hair),rows);
            for(int r=0;r<rows.Length;r++)
            {
                rows[r]=new Sprite[Lengths[r/4]];
                for(int i=0;i<rows[r].Length;i++)
                {
                    var rect=new Rect(i*Width,texture.height-(r+1)*Height,Width,Height);
                    var sprite=Sprite.Create(texture,rect,new Vector2(.5f,9f/Height),12f,0,SpriteMeshType.FullRect);
                    sprite.name=$"{skin}_{(CastMotion)(r/4)}_{r%4}_{i}";
                    rows[r][i]=sprite;
                }
            }
        }
        var strip=rows[motion==CastMotion.Pull?16:(int)motion*4+view];
        return strip[Mathf.Clamp(frame,0,strip.Length-1)];
    }

    private Texture2D RecolorHair(Texture2D source, CastSkin skin, Color32 tone)
    {
        var pixels = source.GetPixels32();
        var dark = Color32.Lerp(tone, new Color32(32,22,23,255), .44f);
        var light = Color32.Lerp(tone, new Color32(255,225,184,255), .33f);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 p = pixels[i];
            if (p.a == 0) continue;
            // Exact paints from the two supplied sheets. Keep the black outlines and pupils.
            switch (p.r << 16 | p.g << 8 | p.b)
            {
                case 0x130801: case 0x1c0e06: pixels[i] = tone; break;
                case 0x180a01: if (skin == CastSkin.Green_Flashlight) pixels[i] = tone; break;
                case 0x0a0000: case 0x0c0100: case 0x0e0300: case 0x110600:
                case 0x150800: case 0x170901: pixels[i] = dark; break;
                case 0x2b190a: pixels[i] = Color32.Lerp(tone,light,.5f); break;
                case 0x3f2714: pixels[i] = light; break;
            }
        }
        var texture = new Texture2D(source.width,source.height,TextureFormat.RGBA32,false) {
            name = source.name + "_Hair_" + tone.r, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false,true);
        coloredAtlases.Add(texture);
        return texture;
    }

    // One shared pixel-art atlas keeps the three-digit badges crisp, including when an actor turns.
    public Sprite NumberBadge(int number)
    {
        const int w = 15, h = 9, columns = 10;
        if (badges.TryGetValue(number,out var badge)) return badge;
        if (badgeAtlas == null)
        {
            int[] digits = { 0b111101101101111,0b010110010010111,0b111001111100111,0b111001111001111,
                0b101101111001001,0b111100111001111,0b111100111101111,0b111001010010010,0b111101111101111,0b111101111001111 };
            badgeAtlas = new Texture2D(w*columns,h*10,TextureFormat.RGBA32,false) {
                name="Dorsales", filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp
            };
            var ink = new Color32[badgeAtlas.width*badgeAtlas.height];
            for (int n=0; n<100; n++)
            for (int y=0; y<h; y++)
            for (int x=0; x<w; x++)
            {
                if ((x==0 || x==w-1) && (y==0 || y==h-1)) continue;
                Color32 color = new Color32(19,43,43,248);
                int col=x-2, row=y-2;
                if (col>=0 && col<11 && col%4<3 && row>=0 && row<5)
                {
                    int digit = col<3 ? 0 : col<7 ? n/10 : n%10;
                    if ((digits[digit] & (1 << (14-row*3-col%4))) != 0) color = new Color32(255,247,219,255);
                }
                ink[(n/columns*h+h-1-y)*badgeAtlas.width+n%columns*w+x]=color;
            }
            badgeAtlas.SetPixels32(ink);
            badgeAtlas.Apply(false,true);
        }
        badge = Sprite.Create(badgeAtlas,new Rect(number%columns*w,number/columns*h,w,h),new Vector2(.5f,.5f),15f,0,SpriteMeshType.FullRect);
        badge.name = number.ToString("000");
        badges.Add(number,badge);
        return badge;
    }

    public void Dispose()
    {
        foreach(var rows in strips.Values)
            foreach(var strip in rows)
                foreach(var sprite in strip) Object.Destroy(sprite);
        foreach (var texture in coloredAtlases) Object.Destroy(texture);
        foreach (var badge in badges.Values) Object.Destroy(badge);
        if (badgeAtlas != null) Object.Destroy(badgeAtlas);
        Object.Destroy(Shadow); Object.Destroy(shadowTexture);
    }
}

internal sealed class ArenaCharacterPose
{
    private SpriteRenderer image;
    private Transform shadow;
    private Vector3 previous;
    private Vector2 facing=new Vector2(.7f,-.7f);
    private float stride, idleOffset, deathStarted=float.NegativeInfinity;
    public readonly CastSkin Skin;
    public readonly int Participant;
    private SpriteRenderer badge;
    public ArenaCharacterPose(CastSkin skin=CastSkin.Green_Unarmed,int participant=0){Skin=skin; Participant=participant;}

    public void Draw(Transform actor,Vector3 feet,float floor,bool alive,bool moving,float yaw,
        ArenaCharacterArt art,float jump=-1)
    {
        if(image==null)
        {
            image=actor.GetComponent<SpriteRenderer>();
            previous=feet;
            idleOffset=Mathf.Repeat(feet.x*1.3f+feet.z*2.7f,5f);
            shadow=new GameObject("Sombra de contacto").transform;
            shadow.SetParent(actor,false);
            shadow.gameObject.AddComponent<SpriteRenderer>().sprite=art.Shadow;
            if (Participant > 0)
            {
                badge = new GameObject("Dorsal").AddComponent<SpriteRenderer>();
                badge.transform.SetParent(actor,false);
                badge.transform.localPosition = new Vector3(0,2.20f,0);
                badge.transform.localRotation = Quaternion.Euler(ArenaView.Rotation.eulerAngles.x,0,0);
                badge.sprite = art.NumberBadge(Participant);
            }
        }
        Vector2 delta=new Vector2(feet.x-previous.x,feet.z-previous.z);
        previous=feet;
        float distance=delta.magnitude;
        bool stepping=alive && moving && distance>.0001f && distance<2.5f;
        if(stepping)
        {
            Vector2 travel=delta/distance;
            facing=new Vector2(Vector2.Dot(travel,ArenaView.Right),Vector2.Dot(travel,ArenaView.Forward));
            stride+=distance*4.5f;
        }
        int view=facing.y>.70f?3:facing.y>.12f?2:Mathf.Abs(facing.x)>.45f?1:0;
        var motion=CastMotion.Idle;
        int frame=Mathf.FloorToInt(Time.time*6+idleOffset)%5;
        if(!alive)
        {
            if(float.IsNegativeInfinity(deathStarted)) deathStarted=Time.time;
            motion=CastMotion.Death; frame=Mathf.FloorToInt((Time.time-deathStarted)*11);
        }
        else
        {
            deathStarted=float.NegativeInfinity;
            if(jump>=0){motion=CastMotion.Jump;frame=Mathf.FloorToInt(jump*6);}
            else if(stepping){motion=CastMotion.Walk;frame=Mathf.FloorToInt(stride)%8;}
        }
        image.sprite=art.Frame(Skin,motion,view,frame,Participant);
        if (badge != null) badge.enabled = alive;
        image.flipX=facing.x<-.08f;
        image.spriteSortPoint=SpriteSortPoint.Pivot;
        image.sortingOrder=0;
        image.color=Color.white;
        actor.SetPositionAndRotation(feet,Quaternion.Euler(0,ArenaView.Yaw+yaw,0));
        float flight=Mathf.Clamp01(feet.y-floor);
        shadow.SetPositionAndRotation(new Vector3(feet.x,floor+.014f,feet.z),Quaternion.Euler(90,ArenaView.Yaw,0));
        shadow.localScale=new Vector3(1.1f,.68f,1f)*(1-.25f*flight);
    }
}
