using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float fieldHalfWidth = 43.5f;
    [SerializeField] private float fieldHalfLength = 28.5f;

    private CharacterController controller;
    private void Awake()
    {
        controller=GetComponent<CharacterController>();
        if(!TryGetComponent<WorldCastActor>(out _)) gameObject.AddComponent<WorldCastActor>();
    }

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

    }
}
