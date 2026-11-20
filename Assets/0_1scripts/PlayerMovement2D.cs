using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private float minX = -40f;
    [SerializeField] private float maxX = 40f;
    [SerializeField] private float minY = -25f;
    [SerializeField] private float maxY = 25f;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        Vector2 movement = new Vector2(x, y).normalized;
        Vector3 position = transform.position + (Vector3)(movement * speed * Time.deltaTime);
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        transform.position = position;
    }
}
