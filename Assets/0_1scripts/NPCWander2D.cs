using UnityEngine;

/// <summary>Small bounded idle movement for the players waiting in the dorm or field.</summary>
public sealed class NPCWander2D : MonoBehaviour
{
    [SerializeField] private Vector2 minBounds = new Vector2(-40f, -25f);
    [SerializeField] private Vector2 maxBounds = new Vector2(40f, 25f);
    [SerializeField] private float speed = 1.3f;
    [SerializeField] private float pauseMin = 1f;
    [SerializeField] private float pauseMax = 3f;
    private Vector2 destination;
    private float wait;

    private void Start() { PickDestination(); }

    private void Update()
    {
        if (wait > 0f) { wait -= Time.deltaTime; return; }
        Vector2 current = transform.position;
        Vector2 next = Vector2.MoveTowards(current, destination, speed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);
        if (Vector2.Distance(next, destination) < 0.08f) PickDestination();
    }

    private void PickDestination()
    {
        destination = new Vector2(Random.Range(minBounds.x, maxBounds.x), Random.Range(minBounds.y, maxBounds.y));
        wait = Random.Range(pauseMin, pauseMax);
    }
}
