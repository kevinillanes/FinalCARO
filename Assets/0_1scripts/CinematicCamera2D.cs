using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CinematicCamera2D : MonoBehaviour
{
    [SerializeField] private float introDuration = 6f;
    [SerializeField] private Vector2 overviewCenter;
    [SerializeField] private float overviewSize = 50f;
    [SerializeField] private Vector2 followOffset = new Vector2(0f, 3f);
    [SerializeField] private float followSize = 14f;
    [SerializeField] private float followSmoothTime = 0.2f;

    private Transform player;
    private Camera sceneCamera;
    private Vector2 followVelocity;
    private float sizeVelocity;
    private float elapsed;
    private float cameraDepth;

    private void Start()
    {
        sceneCamera = GetComponent<Camera>();
        if (!sceneCamera.orthographic)
        {
            Debug.LogError("CinematicCamera2D requiere una cámara ortográfica.");
            enabled = false;
            return;
        }

        GameObject playerObject = GameObject.Find("Player");
        if (playerObject == null)
        {
            Debug.LogError("CinematicCamera2D: no se encontró el objeto Player.");
            enabled = false;
            return;
        }

        player = playerObject.transform;
        cameraDepth = transform.position.z;
        transform.position = new Vector3(overviewCenter.x, overviewCenter.y, cameraDepth);
        sceneCamera.orthographicSize = overviewSize;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, introDuration));
        float eased = progress * progress * (3f - 2f * progress);
        Vector2 destination = (Vector2)player.position + followOffset;

        if (progress < 1f)
        {
            Vector2 center = Vector2.Lerp(overviewCenter, destination, eased);
            transform.position = new Vector3(center.x, center.y, cameraDepth);
            sceneCamera.orthographicSize = Mathf.Lerp(overviewSize, followSize, eased);
        }
        else
        {
            Vector2 current = new Vector2(transform.position.x, transform.position.y);
            Vector2 center = Vector2.SmoothDamp(current, destination, ref followVelocity, followSmoothTime);
            transform.position = new Vector3(center.x, center.y, cameraDepth);
            sceneCamera.orthographicSize = Mathf.SmoothDamp(sceneCamera.orthographicSize, followSize, ref sizeVelocity, followSmoothTime);
        }
    }
}
