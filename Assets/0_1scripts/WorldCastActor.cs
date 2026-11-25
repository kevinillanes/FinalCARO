using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldCastActor : MonoBehaviour
{
    private ArenaCharacterArt art;
    private ArenaCharacterPose pose;
    private Transform portrait;
    private Vector3 localFeet;

    private void Awake()
    {
        var meshes=GetComponentsInChildren<MeshRenderer>();
        if(meshes.Length==0) return;
        Bounds bounds=meshes[0].bounds;
        foreach(var mesh in meshes) { bounds.Encapsulate(mesh.bounds); mesh.enabled=false; }
        localFeet=transform.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
        portrait=new GameObject("Participante animado",typeof(SpriteRenderer)).transform;
        portrait.SetParent(transform,false);
        portrait.localScale=Vector3.one*(bounds.size.y/(1.92f*Mathf.Max(.001f,transform.lossyScale.y)));
        art=new ArenaCharacterArt();
        pose=new ArenaCharacterPose();
    }

    private void LateUpdate()
    {
        if(portrait==null) return;
        Vector3 feet=transform.TransformPoint(localFeet);
        pose.Draw(portrait,feet,feet.y,true,true,0,art);
    }

    private void OnDestroy() { art?.Dispose(); }
}
