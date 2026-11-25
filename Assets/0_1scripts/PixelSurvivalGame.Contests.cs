using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class PixelSurvivalGame
{
    private float[] nerve, craft, botClock;
    private int[] botStep, cracks;
    private readonly int[] wounds = new int[2];
    private static readonly string[] ShapeNames = { "Estrella", "Círculo", "Triángulo", "Cuadrado" };
    private readonly int[] shapeBag = { 0, 1, 2, 3 };
    private int shapesLeft, cookieShape;
    private const int CookieSteps = 240; // Divisible by every polygon's side count and by the five baked sections.
    private const float CookieTolerance = 9f, CookieSeconds = 42f;
    private const float CrackGrowth = .36f, FinalCrackHold = .85f;
    private static readonly Rect CookieRect = new Rect(330, 144, 300, 300);
    private readonly Vector2[] cookiePath = new Vector2[CookieSteps + 1];
    private int cookiePlayer, cookieIndex, cookiePointerIndex;
    private Vector2 lastCookiePointer;
    private bool cookieTracing, cookieStarted, cookieSlipped;
    private float cookieTimeLeft;
    private readonly Vector2[][] cookieFractures = new Vector2[3][];
    private readonly float[] fractureStart = new float[3];
    private float cookieVisualTime;
    private readonly Texture2D[,] cookieFaces=new Texture2D[4,6];
    private readonly float[] pulse = new float[2], shield = new float[2], cooldown = new float[2];
    private readonly bool[] knownGlass = new bool[8];
    private bool[] nightHostile = new bool[0];
    private bool[] tugAllies = System.Array.Empty<bool>();
    private Vector2[] knockback;
    private int ballots, yes, no, ally;
    private bool share, doubleCross;
    private const float NightDuration = 22f;

    private void CreateRoster()
    {
        rivalBots = CampaignBots;
        botRunnerAlive = new bool[rivalBots];
        nerve = new float[rivalBots];
        craft = new float[rivalBots];
        for (int i = 0; i < rivalBots; i++)
        {
            botRunnerAlive[i] = true;
            nerve[i] = Random.value;
            craft[i] = Random.value;
        }
        ballots = yes = no = 0;
        ally = -1;
        share = doubleCross = false;
    }

    private void PrepareContest()
    {
        if ((Trial)trialIndex == Trial.Tug)
        {
            PrepareTugTiming();
            tugAllies = new bool[rivalBots];
            int allies = humanCount, opponents = 0;
            for (int i = 0; i < rivalBots; i++)
            {
                if (!botRunnerAlive[i]) continue;
                tugAllies[i] = allies < opponents;
                if (tugAllies[i]) allies++;
                else opponents++;
            }
        }
        if ((Trial)trialIndex == Trial.Cookie) PrepareCookie();
        botClock = new float[rivalBots];
        botStep = new int[rivalBots];
        knockback = new Vector2[rivalBots];
        nightHostile = new bool[rivalBots];
        cracks = new int[rivalBots + 2];
        System.Array.Clear(knownGlass, 0, knownGlass.Length);
        for (int p = 0; p < 2; p++)
        {
            wounds[p] = 0;
            cooldown[p] = shield[p] = pulse[p] = 0f;
            if ((Trial)trialIndex == Trial.Night) finalPosition[p] = new Vector2(-1.5f + 3f*p, -3f);
        }
        for (int i = 0; i < rivalBots; i++)
        {
            nightHostile[i] = i==ally && share ? doubleCross : nerve[i]>.58f;
            botClock[i] = Random.Range(.3f, 1.6f);
            if ((Trial)trialIndex == Trial.Night)
                botPosition[i] = new Vector2(-7f + (i % 8)*2f, -3.8f + (i / 8)*1.65f);
        }
    }

    private void ResolveRoster()
    {
        if ((Trial)trialIndex == Trial.Tug)
            for (int i = 0; i < rivalBots; i++)
                if (!tugAllies[i]) botRunnerAlive[i] = false;
        if ((Trial)trialIndex == Trial.Cookie)
        {
            // Allow the rest of the room its full turn even if the humans finished early.
            for (int i = 0; i < rivalBots; i++)
                while (botRunnerAlive[i] && botStep[i] < 5) CutBotCookie(i);
            for (int i = 0; i < rivalBots; i++)
                if (botRunnerAlive[i]) { ally = i; break; }
        }
    }

    private void DrawVote()
    {
        Fill(new Rect(120, 80, 720, 385), new Color(.04f,.06f,.10f,.95f));
        Label(new Rect(145, 100, 670, 60), screen == Screen.Exit ? "LA PUERTA ABIERTA" : "LA ASAMBLEA", titleStyle);
        if (screen == Screen.Exit)
        {
            Label(new Rect(175, 180, 610, 100), $"Seguir: {yes}   ·   Salir: {no}\nLos cerrojos ceden. Esta vez el grupo vuelve a casa.", bodyStyle);
            if (GUI.Button(new Rect(355,345,250,55),"VOLVER AL MENÚ",buttonStyle)) screen = Screen.Title;
            return;
        }
        Label(new Rect(170,168,620,95),"Guardia: «También cuentan los votos de quienes quieren marcharse».\nSe necesita mayoría para continuar. Un empate abre las puertas.",bodyStyle);
        if (ballots < humanCount)
        {
            Label(new Rect(180,275,600,40), $"Turno de J{ballots+1}   ·   {RemainingRivals()+humanCount} votantes",smallStyle);
            if (GUI.Button(new Rect(230,345,220,55),"SEGUIR",buttonStyle)) CastVote(true);
            else if (GUI.Button(new Rect(510,345,220,55),"SALIR",buttonStyle)) CastVote(false);
        }
        else
        {
            Label(new Rect(180,265,600,60), $"Seguir: {yes}   ·   Salir: {no}",bodyStyle);
            if (GUI.Button(new Rect(345,345,270,55),"ABRIR EL ACTA",buttonStyle))
                OpenVoteResult();
        }
    }

    private void OpenVoteResult()
    {
        if (yes > no) Advance();
        else screen = Screen.Exit;
    }

    private void CastVote(bool stay)
    {
        Chime(stay?1.3f:.9f);
        if (stay) yes++; else no++;
        if (++ballots != humanCount) return;
        for (int i = 0; i < rivalBots; i++)
        {
            if (!botRunnerAlive[i]) continue;
            if (nerve[i] + .28f*craft[i] - .09f*botStrikes[i] > .53f) yes++; else no++;
        }
    }

    private void DrawInterlude()
    {
        string line;
        switch ((Trial)trialIndex)
        {
            case Trial.Cookie: line = phaseTime < 3f ? "El recuento termina. Bajo cada tapa de metal espera una figura de azúcar." : "Nerea: «Empieza en la marca blanca. Arrastra el ratón por el surco hasta dar la vuelta. A la tercera grieta, la figura se rompe»."; break;
            case Trial.Carousel: line = phaseTime < 3f ? "Al otro lado del comedor se encienden las bombillas de un carrusel. Los guardias abren las puertas de colores." : "Nerea: «Mira cada puerta. Si marca dos de tres, solo cabe una persona más»."; break;
            case Trial.Night: line = phaseTime < 3f ? "Las bandejas vuelven vacías. Alguien ha escondido las raciones bajo una litera." : "Por los altavoces: «Esta noche los guardias harán el recuento desde fuera»."; break;
            case Trial.Tug: line = "Al amanecer, una cuerda reúne a los supervivientes. Esta vez solo cruzará el equipo que tire al mismo ritmo."; break;
            case Trial.Rope: line = "Las muñecas darán doce vueltas. Un mal salto te deja fuera; tu compañero puede seguir."; break;
            case Trial.Final: line = "Los VIP toman asiento. Salta hasta la salida dorada. Cada loseta cede poco después de que alguien la pise."; break;
            default: line = "Las puertas se abren sobre ocho pares de cristales. Desde la fila de atrás alguien susurra: «Recuerda dónde pisó el primero»."; break;
        }
        Fill(new Rect(40,365,880,140),new Color(.02f,.03f,.06f,.92f));
        Label(new Rect(70,374,820,85),line,bodyStyle);
        Label(new Rect(80,464,800,30),"ESPACIO para continuar",smallStyle);
    }

    private void CutBotCookie(int i)
    {
        if (Random.value < .54f + craft[i]*.43f) botStep[i]++;
        else if (++cracks[i+2] >= 3) botRunnerAlive[i] = false;
    }

    private void PrepareCookie()
    {
        if (shapesLeft == 0)
        {
            for (int i = shapeBag.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shapeBag[i], shapeBag[j]) = (shapeBag[j], shapeBag[i]);
            }
            shapesLeft = shapeBag.Length;
        }
        cookieShape = shapeBag[--shapesLeft];
        for (int i = 0; i <= CookieSteps; i++)
            cookiePath[i] = CookieRect.center + CookiePoint(i / (float)CookieSteps) * (CookieRect.width / 180f);
        BeginCookieTurn(0);
    }

    private void BeginCookieTurn(int player)
    {
        cookiePlayer = player;
        cookieIndex = 0;
        cookieTracing = cookieStarted = cookieSlipped = false;
        cookieTimeLeft = CookieSeconds;
        cookieVisualTime = 0f;
        System.Array.Clear(cookieFractures, 0, cookieFractures.Length);
    }

    private void UpdateCookie(float dt)
    {
        for (int i = 0; i < rivalBots; i++)
        {
            if (!botRunnerAlive[i] || botStep[i] >= 5 || (botClock[i] -= dt) > 0f) continue;
            CutBotCookie(i);
            botClock[i] = 1.8f - .6f*craft[i];
        }
        Mouse mouse = Mouse.current;
        Vector2 pointer = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        pointer.y = UnityEngine.Screen.height - pointer.y;
        Vector2 origin = UiOrigin(out float scale);
        StepCookieTurn((pointer - origin) / scale, mouse?.leftButton.wasPressedThisFrame == true,
            mouse?.leftButton.isPressed == true, dt);
    }

    private void StepCookieTurn(Vector2 pointer, bool pressed, bool held, float dt)
    {
        cookieVisualTime += dt;
        if (cracks[cookiePlayer] == cookieFractures.Length)
        {
            if (cookieVisualTime - fractureStart[2] >= FinalCrackHold)
                CompleteTrial(false, $"La tercera grieta rompió la galleta de J{cookiePlayer + 1}.");
            return;
        }
        if (cookieStarted)
        {
            cookieTimeLeft -= dt;
            if (cookieTimeLeft <= 0f)
            {
                CompleteTrial(false, $"Se acabó el tiempo de J{cookiePlayer + 1} antes de cerrar el contorno.");
                return;
            }
        }
        if (!held)
        {
            cookieTracing = false;
            return;
        }
        if (!cookieTracing)
        {
            if (!pressed || Vector2.Distance(pointer, cookiePath[cookieIndex]) > CookieTolerance) return;
            cookieTracing = cookieStarted = true;
            cookieSlipped = false;
            lastCookiePointer = pointer;
            cookiePointerIndex = cookieIndex;
            return;
        }

        // Validate the swept segment too: a fast drag across the centre cannot skip an edge.
        int samples = Mathf.CeilToInt(Vector2.Distance(lastCookiePointer, pointer) / 3f);
        for (int i = 1; i <= samples; i++)
        {
            Vector2 point = Vector2.Lerp(lastCookiePointer, pointer, i / (float)samples);
            int nearest = cookiePointerIndex;
            float distance = (point - cookiePath[nearest]).sqrMagnitude;
            int last = Mathf.Min(cookiePointerIndex + 6, CookieSteps);
            for (int j = Mathf.Max(0, cookiePointerIndex - 6); j <= last; j++)
            {
                float candidate = (point - cookiePath[j]).sqrMagnitude;
                if (candidate >= distance) continue;
                nearest = j;
                distance = candidate;
            }
            if (distance > CookieTolerance * CookieTolerance)
            {
                cookieTracing = false;
                cookieSlipped = true;
                AddCookieFracture(cookiePath[nearest], point);
                Chime(.58f);
                return;
            }
            cookiePointerIndex = nearest;
            cookieIndex = Mathf.Max(cookieIndex, nearest);
            if (cookieIndex == CookieSteps)
            {
                if (cookiePlayer + 1 == humanCount)
                    CompleteTrial(true, humanCount == 1 ? "La figura salió entera." : "Las dos figuras salieron enteras.");
                else
                {
                    Chime(1.45f);
                    BeginCookieTurn(cookiePlayer + 1);
                }
                return;
            }
        }
        lastCookiePointer = pointer;
    }

    private void AddCookieFracture(Vector2 origin, Vector2 slip)
    {
        int index = cracks[cookiePlayer]++;
        bool final = index == cookieFractures.Length - 1;
        Vector2 radial = (origin - CookieRect.center).normalized;
        float side = Vector2.Dot(slip-origin,Vector2.Perpendicular(radial)) >= 0f ? 1f : -1f;
        if (index == 1) side = -side;
        Vector2 direction = (final ? -radial : radial) + Vector2.Perpendicular(radial) * (.30f * side);
        Vector2 rim = CookieRect.center + direction.normalized * (CookieRect.width * .47f);
        Vector2 across = Vector2.Perpendicular((rim-origin).normalized) * side;
        int segments = final ? 9 : 5;
        var path = new Vector2[segments+1];
        path[0] = origin;
        for (int i=1; i<segments; i++)
        {
            float t = i/(float)segments;
            float kink = (i%2==0 ? -1f : 1f)*Mathf.Sin(t*Mathf.PI)*(final ? 9f : 4f);
            path[i] = Vector2.Lerp(origin,rim,t) + across*kink;
        }
        path[segments] = rim;
        cookieFractures[index] = path;
        fractureStart[index] = cookieVisualTime;
    }

    private void DrawCookieFractures()
    {
        if (Event.current.type != EventType.Repaint) return;
        for (int i=0; i<cracks[cookiePlayer]; i++)
        {
            var path = cookieFractures[i];
            float progress = Mathf.Clamp01((cookieVisualTime-fractureStart[i])/CrackGrowth);
            float reach = (path.Length-1)*Mathf.Sqrt(progress);
            for (int j=0; j<Mathf.CeilToInt(reach); j++)
            {
                Vector2 end = Vector2.Lerp(path[j],path[j+1],reach-j);
                DrawSugarSplit(path[j],end,i==2 ? 4f : 2.8f);
            }
            // A finer branch opens behind the growing tip.
            if (reach>2f)
            {
                Vector2 direction = path[2]-path[1];
                Vector2 fork = path[2]+direction*.55f+Vector2.Perpendicular(direction)*.66f;
                DrawSugarSplit(path[2],Vector2.Lerp(path[2],fork,reach-2f),1.7f);
            }
        }
    }

    private void DrawSugarSplit(Vector2 start, Vector2 end, float width)
    {
        Vector2 delta = end - start;
        Matrix4x4 saved = GUI.matrix;
        GUI.matrix = saved * Matrix4x4.TRS(start,Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one);
        Fill(new Rect(-.5f,-width*.5f,delta.magnitude+1f,width+1.2f),new Color(.99f,.75f,.38f));
        Fill(new Rect(-.5f,-width*.5f,delta.magnitude+1f,width),new Color(.23f,.105f,.055f));
        GUI.matrix = saved;
    }

    private void DrawCookie()
    {
        bool broken = cracks[cookiePlayer] == cookieFractures.Length;
        string hint = broken ? "La tercera grieta partió la figura."
            : cookieTracing ? "Sigue los puntos del borde. Puedes soltar el botón y retomar en la marca blanca."
            : cookieSlipped ? "¡Una grieta! Suelta el botón y vuelve a pulsar en la marca blanca."
            : cookieStarted ? "Para continuar, mantén el botón izquierdo sobre la marca blanca y sigue el borde."
            : cookiePlayer == 1 ? "¡J1 terminó! Pasa el ratón a J2. Mantén el clic en la marca blanca y sigue los puntos."
            : "Mantén el botón izquierdo en la marca blanca y arrastra por el borde hacia los puntos.";
        Fill(new Rect(48, 98, 864, 43), new Color(.035f, .055f, .085f, .96f));
        Label(new Rect(55, 99, 850, 40), hint, smallStyle);

        const int sectionSize = CookieSteps / 5;
        GUI.DrawTexture(CookieRect, CookieFace(cookieIndex / sectionSize));
        for (int i = cookieIndex / sectionSize * sectionSize; i < cookieIndex; i++)
            CookieMark(cookiePath[i], 4f, new Color(.37f, .95f, .59f));
        DrawCookieFractures();
        if (!broken)
        {
            for (int i = 4; i <= 12 && cookieIndex + i <= CookieSteps; i += 4)
                CookieMark(cookiePath[cookieIndex + i], 5f, new Color(.18f, .17f, .12f));
            CookieMark(cookiePath[cookieIndex], 13f, new Color(.16f, .10f, .06f));
            CookieMark(cookiePath[cookieIndex], 8f, Color.white);
        }

        string turn = humanCount == 2 ? $"Turno de J{cookiePlayer + 1} de 2   ·   " : "";
        Fill(new Rect(85, 450, 790, 42), new Color(.035f, .055f, .085f, .96f));
        Label(new Rect(90, 451, 780, 40),
            $"{turn}{ShapeNames[cookieShape]}   ·   {100 * cookieIndex / CookieSteps} %   ·   Grietas {cracks[cookiePlayer]}/3   ·   {Mathf.CeilToInt(cookieTimeLeft)} s", smallStyle);
    }

    private void CookieMark(Vector2 point, float size, Color color)
    {
        Fill(new Rect(point.x - size * .5f, point.y - size * .5f, size, size), color);
    }

    private Vector2 CookiePoint(float t)
    {
        if(cookieShape==1) return CookieVertex(t,66);
        int sides=cookieShape==0?10:cookieShape==2?3:4;
        float step=Mathf.Repeat(t,1)*sides;
        int a=Mathf.FloorToInt(step), b=(a+1)%sides;
        float shift=cookieShape==3?.125f:0f;
        Vector2 first=CookieVertex(a/(float)sides+shift,cookieShape==0 && a%2==1?32:72);
        Vector2 next=CookieVertex(b/(float)sides+shift,cookieShape==0 && b%2==1?32:72);
        return Vector2.Lerp(first,next,step-a);
    }

    private static Vector2 CookieVertex(float turn,float radius)
    {
        float angle=(turn-.25f)*Mathf.PI*2;
        return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
    }

    private Texture2D CookieFace(int completed)
    {
        if(cookieFaces[cookieShape,completed]!=null) return cookieFaces[cookieShape,completed];
        const int size=180;
        var pixels=new Color32[size*size];
        var middle=new Vector2(90,90);
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            Vector2 p=new Vector2(x+.5f,y+.5f);
            float radius=(p-new Vector2(90,90)).magnitude;
            if(radius>89.8f) continue;
            float grain=((x*31+y*17+x*y*3)%19)/160f;
            Color color=Color.Lerp(new Color(.98f,.71f,.31f),new Color(.72f,.38f,.12f),(.34f*x+.66f*y)/size+grain);
            if(radius>86) color*=.72f;
            else if(radius>83.8f) color=Color.Lerp(color,Color.white,.22f);
            color.a=Mathf.Clamp01(90-radius);
            pixels[(size-1-y)*size+x]=color;
        }
        // Draw the same contour the moving tip follows. Render the groove before its highlight.
        for(int pass=0;pass<2;pass++)
        for(int n=0;n<720;n++)
        {
            var point=middle+CookiePoint(n/720f);
            int cx=Mathf.RoundToInt(point.x),cy=Mathf.RoundToInt(point.y);
            int radius=pass==0?3:1;
            Color32 ink=pass==0?new Color(.47f,.24f,.08f):n<completed*144?new Color(.37f,.95f,.59f):new Color(1f,.81f,.42f);
            for(int dy=-radius;dy<=radius;dy++)
            for(int dx=-radius;dx<=radius;dx++)
                if(dx*dx+dy*dy<=radius*radius) pixels[(size-1-cy-dy)*size+cx+dx]=ink;
        }
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Dalgona "+completed,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        texture.SetPixels32(pixels);
        texture.Apply(false,true);
        cookieFaces[cookieShape,completed]=texture;
        return texture;
    }

    private void UpdateNight(Keyboard k,float dt)
    {
        for (int p = 0; p < humanCount; p++)
        {
            Vector2 d = p == 0 ? new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0))
                : new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
            playerMoving[p] = d.sqrMagnitude > 0f;
            Vector2 pos = finalPosition[p] + ArenaView.GroundInput(d)*dt*4.6f;
            finalPosition[p] = ArenaView.MoveBetweenBunks(finalPosition[p],pos);
            shield[p] -= dt; cooldown[p] -= dt;
            if (Pressed(k,p==0?Key.Space:Key.Enter) && cooldown[p]<=0f) { shield[p]=.85f; cooldown[p]=2.4f; }
        }
        for (int i=0;i<rivalBots;i++)
        {
            if (!botRunnerAlive[i]) continue;
            int p = humanCount==2 && (botPosition[i]-finalPosition[1]).sqrMagnitude < (botPosition[i]-finalPosition[0]).sqrMagnitude ? 1:0;
            bool hostile = nightHostile[i];
            Vector2 target = hostile ? finalPosition[p] : new Vector2(Mathf.Sin(i+trialTime*.35f)*6f,2f+Mathf.Cos(i*2+trialTime*.4f)*3f);
            if (i==ally && share && !doubleCross) target=finalPosition[p]+Vector2.right;
            Vector2 before=botPosition[i];
            botPosition[i] = ArenaView.MoveBetweenBunks(before,before+knockback[i]*dt);
            knockback[i] *= Mathf.Exp(-3.6f*dt);
            Vector2 waypoint=ArenaView.BunkWaypoint(botPosition[i],target);
            botPosition[i]=ArenaView.MoveBetweenBunks(botPosition[i],Vector2.MoveTowards(botPosition[i],waypoint,dt*(1.1f+nerve[i]*1.35f)));
            if (!hostile || Vector2.Distance(botPosition[i],target)>.85f) continue;
            bool defended = shield[p]>0 || (share && !doubleCross && ally>=0 && Vector2.Distance(botPosition[ally],target)<1.8f);
            Vector2 away = botPosition[i]-target;
            if (away.sqrMagnitude < .01f) away = Vector2.up;
            knockback[i] = away.normalized * (defended ? 14f : 7f);
            if (botClock[i] > trialTime) continue;
            botClock[i] = trialTime + 1.8f;
            if (defended) { if (++botStep[i]>=2) botRunnerAlive[i]=false; }
            else { wounds[p]++; shield[p]=1.4f; cueSpeaker.pitch=.9f; cueSpeaker.PlayOneShot(impactTone); }
            if (wounds[p]>=3) { CompleteTrial(false,"El equipo cayó antes del recuento."); return; }
        }
        if (trialTime>=NightDuration) CompleteTrial(true,"Se encendieron las luces. El equipo sigue en pie.");
    }

    private void DrawNight()
    {
        Fill(new Rect(80,98,800,88),new Color(.04f,.05f,.08f,.88f));
        Label(new Rect(90,98,780,45),"Esquiva a los rivales en naranja. Bloquea al contacto para empujarlos.",bodyStyle);
        Label(new Rect(90,144,780,38),"J1 WASD + Espacio   ·   J2 Flechas + Enter   ·   Resiste "+Mathf.CeilToInt(NightDuration-trialTime)+" s",smallStyle);
        if (share && trialTime < 6f) Label(new Rect(120,375,720,45),doubleCross ? "Tu aliado señala tu litera: «¡La ración está ahí!»" : "Tu aliado: «Quédate cerca. Yo cubro tu espalda».",bodyStyle);
        for(int p=0;p<humanCount;p++)
            Label(new Rect(90+p*400,435,390,48),$"J{p+1} · Golpes {wounds[p]}/3 · {(shield[p]>0?"Cubierto":cooldown[p]>0?"Recuperando":"Bloqueo listo")}",smallStyle);
    }
}
