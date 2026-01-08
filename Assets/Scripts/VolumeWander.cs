using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Makes an object "float" around inside a BoxCollider volume by picking random target points
/// within the volume and smoothly drifting toward them.
///
/// Setup:
/// 1) Create an empty GameObject called "WanderVolume".
/// 2) Add a BoxCollider and size it to the space you want.
/// 3) Add this component to your debris/asteroid.
/// 4) Assign the BoxCollider in the inspector.
///
/// Tip: For the nicest look, put your mesh under a child and add PerlinFloat to the child.
/// Parent: VolumeWander (path) → Child: PerlinFloat (organic jitter).
/// </summary>
public class VolumeWander : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] BoxCollider volume;

    [Header("Movement")]
    [FormerlySerializedAs("maxSpeed")]
    [SerializeField] float speed = 2f;

    [Tooltip("How quickly it changes direction. Smaller = floatier, larger = snappier.")]
    [SerializeField] float smoothTime = 0.6f;

    [Tooltip("When we get within this distance of the target, pick a new target.")]
    [SerializeField] float arriveDistance = 0.5f;

    [Tooltip("Optional padding so objects don't touch the volume edges.")]
    [SerializeField] Vector3 padding = Vector3.zero;

    Vector3 target;
    Vector3 velocity;

    void Start()
    {
        if (volume == null)
        {
            Debug.LogError($"{nameof(VolumeWander)} needs a {nameof(BoxCollider)} volume assigned.", this);
            enabled = false;
            return;
        }

        PickNewTarget();
    }

    void Update()
    {
        if (speed <= 0f)
        {
            return;
        }

        float sqrDist = (transform.position - target).sqrMagnitude;
        if (sqrDist <= arriveDistance * arriveDistance)
        {
            PickNewTarget();
        }

        // SmoothDamp gives that "floating" easing. maxSpeed caps how fast we can drift.
        float st = Mathf.Max(0.0001f, smoothTime);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, st, speed);
    }

    void PickNewTarget()
    {
        var b = volume.bounds;

        // Apply optional padding.
        var min = b.min + padding;
        var max = b.max - padding;

        target = new Vector3(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y),
            Random.Range(min.z, max.z)
        );
    }

    void OnDrawGizmosSelected()
    {
        if (volume == null)
        {
            return;
        }

        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.6f);
        var b = volume.bounds;
        Gizmos.DrawWireCube(b.center, b.size);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target, 0.2f);
    }
}
