using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class StadiumIntroCamera : MonoBehaviour
{
    [SerializeField] private float introDuration = 6f;
    [SerializeField] private float followSmoothTime = .18f;
    private Transform player;
    private Camera view;
    private Vector3 focus, velocity;
    private float elapsed;
    private readonly RaycastHit[] hits=new RaycastHit[32];
    private readonly System.Collections.Generic.HashSet<Renderer> covers=new System.Collections.Generic.HashSet<Renderer>();
    private void Start()
    {
        view=GetComponent<Camera>();
        view.orthographic=true;
        view.clearFlags=CameraClearFlags.SolidColor;
        view.backgroundColor=new Color(.075f,.11f,.16f);
        var controller=FindAnyObjectByType<PlayerMovement>();
        if(controller!=null) player=controller.transform;
        // Cut the near walls and overhead slab, keeping their colliders and the rear elevation.
        foreach(string label in new[]{"Techo azul","Revestimiento azul interior","Viga perimetral superior","Muro oeste","Muro de entrada","Fondo de los cuartos"})
        {
            var part=GameObject.Find(label);
            if(part!=null && part.TryGetComponent<Renderer>(out var r)) r.enabled=false;
        }
        view.farClipPlane=400;
    }
    private void LateUpdate()
    {
        if(player==null) return;
        elapsed+=Time.deltaTime;
        float t=Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.1f,introDuration));
        focus=Vector3.SmoothDamp(focus,Vector3.Lerp(Vector3.zero,player.position,t),ref velocity,followSmoothTime);
        view.orthographicSize=Mathf.Lerp(Mathf.Max(55,90/view.aspect),Mathf.Max(12,17/view.aspect),t);
        transform.SetPositionAndRotation(focus-ArenaView.Rotation*Vector3.forward*Mathf.Lerp(170,38,t),ArenaView.Rotation);
        foreach(var cover in covers) if(cover!=null) cover.forceRenderingOff=false;
        covers.Clear();
        int count=Physics.RaycastNonAlloc(player.position+Vector3.up*.55f,-transform.forward,hits,view.farClipPlane);
        for(int i=0;i<count;i++)
        {
            if(hits[i].transform.IsChildOf(player)) continue;
            if(hits[i].collider.TryGetComponent<MeshRenderer>(out var cover) && cover.enabled)
            { cover.forceRenderingOff=true; covers.Add(cover); }
        }
    }
}
