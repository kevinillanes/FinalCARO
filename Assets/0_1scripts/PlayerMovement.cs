using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float fieldHalfWidth = 43.5f;
    [SerializeField] private float fieldHalfLength = 28.5f;

    private CharacterController controller;
    private ArenaCharacterArt art;
    private ArenaCharacterPose pose;
    private Transform portrait;
    private Sprite idle,walkA,walkB;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        idle=LoadPose("Player_Idle_A");walkA=LoadPose("Player_Walk_A");walkB=LoadPose("Player_Walk_B");
        if(idle!=null && walkA!=null && walkB!=null)
        {
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>()) renderer.enabled=false;
            art=new ArenaCharacterArt();pose=new ArenaCharacterPose();
            portrait=new GameObject("Participante").transform;portrait.SetParent(transform,false);
            portrait.localScale=Vector3.one*1.15f;
            portrait.gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private static Sprite LoadPose(string label)
    {
        var tex=Resources.Load<Texture2D>("Characters/"+label);
        return tex==null?null:Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.05f),100);
    }
    private void OnDestroy(){art?.Dispose(); if(idle!=null)Destroy(idle);if(walkA!=null)Destroy(walkA);if(walkB!=null)Destroy(walkB);}

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        float horizontal = keyboard==null?0:(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                         - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float vertical = keyboard==null?0:(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

        Vector2 ground=ArenaView.GroundInput(new Vector2(horizontal,vertical));
        Vector3 movement=new Vector3(ground.x,0f,ground.y);
        if (movement.sqrMagnitude > 1f) movement.Normalize();
        controller.Move(movement * speed * Time.deltaTime);

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, -fieldHalfWidth, fieldHalfWidth);
        position.z = Mathf.Clamp(position.z, -fieldHalfLength, fieldHalfLength);
        transform.position = position;
        if(portrait!=null)
        {
            Vector3 feet=new Vector3(position.x,controller.bounds.min.y+.02f,position.z);
            pose.Draw(portrait,feet,feet.y,true,movement.sqrMagnitude>.01f,false,0,Time.deltaTime,art,idle,walkA,walkB,null);
        }
    }
}
