using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class StadiumIntroCamera : MonoBehaviour
{
    [Header("Entrada cinematográfica")]
    [SerializeField] private float introDuration = 6f;
    [SerializeField] private Vector3 overviewPosition = new Vector3(0f, 59f, -127f);
    [SerializeField] private Vector3 overviewLookAt = new Vector3(0f, 4f, 1f);
    [SerializeField] private float overviewFieldOfView = 54f;

    [Header("Cámara de juego")]
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 8f, -13f);
    [SerializeField] private Vector3 followLookOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float followFieldOfView = 48f;
    [SerializeField] private float followSmoothTime = 0.18f;
    [SerializeField] private float rotationSharpness = 5f;

    private Transform player;
    private Camera sceneCamera;
    private Vector3 followVelocity;
    private float elapsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachToMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = Object.FindFirstObjectByType<Camera>();
        if (mainCamera != null && !mainCamera.orthographic && mainCamera.GetComponent<StadiumIntroCamera>() == null)
            mainCamera.gameObject.AddComponent<StadiumIntroCamera>();
    }

    private void Start()
    {
        sceneCamera = GetComponent<Camera>();
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject == null)
        {
            Debug.LogError("StadiumIntroCamera: no se encontró el objeto Player.");
            enabled = false;
            return;
        }

        player = playerObject.transform;
        transform.position = overviewPosition;
        transform.LookAt(overviewLookAt);
        sceneCamera.fieldOfView = overviewFieldOfView;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, introDuration));
        float easedProgress = progress * progress * (3f - 2f * progress);
        Vector3 destination = player.position + followOffset;
        Vector3 lookTarget = player.position + followLookOffset;

        if (progress < 1f)
        {
            transform.position = Vector3.Lerp(overviewPosition, destination, easedProgress);
            Vector3 introLookTarget = Vector3.Lerp(overviewLookAt, lookTarget, easedProgress);
            transform.rotation = Quaternion.LookRotation(introLookTarget - transform.position, Vector3.up);
            sceneCamera.fieldOfView = Mathf.Lerp(overviewFieldOfView, followFieldOfView, easedProgress);
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref followVelocity, followSmoothTime);
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSharpness * Time.deltaTime);
            sceneCamera.fieldOfView = followFieldOfView;
        }
    }
}
