using UnityEngine;

/// <summary>
/// Simple "bob up and down" motion using a sine wave.
///
/// Good for early levels and for learning:
/// - amplitude = how far it moves
/// - cyclesPerSecond = how fast it bobs
///
/// Tip: For more complex paths, use WaypointMover.
/// </summary>
public class BobbingMotion : MonoBehaviour
{
    [Header("Space")]
    [SerializeField] bool useLocalSpace = true;

    [Header("Bobbing")]
    [SerializeField] Vector3 direction = Vector3.up;
    [SerializeField] float amplitude = 1f;
    [Tooltip("How many full up+down cycles per second.")]
    [SerializeField] float cyclesPerSecond = 0.5f;
    [Tooltip("Randomizes the starting phase so multiple objects don't move in sync.")]
    [SerializeField] bool randomizeStartPhase = true;

    Vector3 startPos;
    float phase;

    void Start()
    {
        startPos = useLocalSpace ? transform.localPosition : transform.position;
        phase = randomizeStartPhase ? Random.value * Mathf.PI * 2f : 0f;
    }

    void Update()
    {
        if (amplitude <= 0f || cyclesPerSecond <= 0f)
        {
            return;
        }

        Vector3 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
        float t = (Time.time * cyclesPerSecond * Mathf.PI * 2f) + phase;

        // -1..1
        float sine = Mathf.Sin(t);
        Vector3 offset = dir * (sine * amplitude);

        if (useLocalSpace)
        {
            transform.localPosition = startPos + offset;
        }
        else
        {
            transform.position = startPos + offset;
        }
    }
}
