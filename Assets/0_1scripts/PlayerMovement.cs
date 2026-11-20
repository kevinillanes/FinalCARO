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
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                         - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float vertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

        Vector3 movement = new Vector3(horizontal, 0f, vertical);
        if (movement.sqrMagnitude > 1f) movement.Normalize();
        controller.Move(movement * speed * Time.deltaTime);

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, -fieldHalfWidth, fieldHalfWidth);
        position.z = Mathf.Clamp(position.z, -fieldHalfLength, fieldHalfLength);
        transform.position = position;
    }
}
