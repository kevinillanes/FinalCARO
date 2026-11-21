using UnityEngine;

internal static class ArenaView
{
    public const float Yaw = 45f;
    public static readonly Quaternion Rotation = Quaternion.Euler(35.26439f,Yaw,0);
    public static readonly Vector2 Right = new Vector2(.70710678f,-.70710678f);
    public static readonly Vector2 Forward = new Vector2(.70710678f,.70710678f);

    public static Vector2 GroundInput(Vector2 input)
    {
        input = Vector2.ClampMagnitude(input,1);
        return Right*input.x + Forward*input.y;
    }

    public static void KeyLight(Transform parent)
    {
        var go = new GameObject("Luz de pabellón");
        go.transform.SetParent(parent,false);
        go.transform.localRotation = Quaternion.Euler(47f,-32f,0);
        var light = go.AddComponent<Light>();
        light.type=LightType.Directional;
        light.color=new Color(1f,.92f,.8f);
        light.intensity=1.35f;
        light.shadows=LightShadows.Soft;
        light.shadowStrength=.65f;
        light.shadowNormalBias=.12f;
    }

    // A swept circle against the carousel column. The tangential component lets
    // a contestant slide past the post even on a long frame.
    public static Vector2 SlidePastColumn(Vector2 start,Vector2 end,bool steer)
    {
        const float radius=.89f;
        Vector2 motion=end-start;
        float length=motion.magnitude;
        if(length<.00001f) return start;
        Vector2 direction=motion/length;
        float closest=Mathf.Clamp(-Vector2.Dot(start,direction),0,length);
        Vector2 near=start+direction*closest;
        if(near.sqrMagnitude>=radius*radius) return end;
        float entry=Mathf.Max(0,closest-Mathf.Sqrt(radius*radius-near.sqrMagnitude));
        Vector2 normal=(start+direction*entry).normalized;
        if(normal==Vector2.zero) normal=Vector2.up;
        Vector2 slide=direction-normal*Mathf.Min(0,Vector2.Dot(direction,normal));
        if(steer && slide.sqrMagnitude<.2f) slide=new Vector2(-normal.y,normal.x);
        return normal*(radius+.002f)+slide*(length-entry);
    }

    public static Vector2 MoveBetweenBunks(Vector2 from,Vector2 to)
    {
        // Same bed centres and dimensions as the scenery, expanded by a foot radius.
        to.x=Mathf.Clamp(to.x,-10.1f,10.1f);
        to.y=Mathf.Clamp(to.y,-8.6f,10.45f);
        for(int side=-1;side<=1;side+=2)
        for(int row=0;row<4;row++)
        {
            float x=side*8.5f,z=-5f+row*4;
            const float width=1.15f,depth=1.99f;
            if(Mathf.Abs(from.y-z)<depth)
            {
                if(from.x<=x-width && to.x>x-width)to.x=x-width;
                if(from.x>=x+width && to.x<x+width)to.x=x+width;
            }
            if(Mathf.Abs(to.x-x)<width)
            {
                if(from.y<=z-depth && to.y>z-depth)to.y=z-depth;
                if(from.y>=z+depth && to.y<z+depth)to.y=z+depth;
            }
        }
        return to;
    }

    public static Vector2 BunkWaypoint(Vector2 from,Vector2 goal)
    {
        for(int side=-1;side<=1;side+=2)
        {
            float a=from.x*side,b=goal.x*side;
            if(Mathf.Min(a,b)>9.65f || Mathf.Max(a,b)<7.35f) continue;
            if(Mathf.Min(from.y,goal.y)>9.04f || Mathf.Max(from.y,goal.y)<-7.04f) continue;
            float detour=Mathf.Abs(from.y+7.3f)+Mathf.Abs(goal.y+7.3f) <
                Mathf.Abs(from.y-9.3f)+Mathf.Abs(goal.y-9.3f)?-7.3f:9.3f;
            if(Mathf.Abs(from.y-detour)>.10f) return new Vector2(from.x,detour);
            return new Vector2(side*(b>=8.5f?9.85f:7.15f),detour);
        }
        return goal;
    }
}
