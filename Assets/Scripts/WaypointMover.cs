using UnityEngine;

/// <summary>
/// Move a GameObject between a list of waypoint points (Transforms).
///
/// Typical setup:
/// 1) Create an empty GameObject called "Waypoints".
/// 2) Create children under it: P0, P1, P2... and position them.
/// 3) Put this component on the obstacle.
/// 4) Drag the waypoint children into the Points list in order.
///
/// Notes:
/// - This moves using transform.position (simple + reusable).
/// - If your obstacle has a Rigidbody, consider moving it with Rigidbody.MovePosition in FixedUpdate instead.
/// </summary>
public class WaypointMover : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] Transform[] points;

    [Header("Movement")]
    [SerializeField] float speed = 2f;

    [Header("Behaviour")]
    [Tooltip("If true: after reaching a point, choose a random next point (ignores loop/ping-pong ordering).")]
    [SerializeField] bool chooseRandomNextPoint;

    [Tooltip("Optional pause after reaching each point (seconds).")]
    [SerializeField] float waitTimeAtPoint;

    [Tooltip("If true: last point returns to first point.")]
    [SerializeField] bool loop = true;

    [Tooltip("If true: go forward then backward (ignores 'loop').")]
    [SerializeField] bool pingPong;

    [Tooltip("How close we must get to a point before switching to the next.")]
    [SerializeField] float arriveDistance = 0.05f;

    int index;
    int direction = 1;
    float waitTimer;

    void Start()
    {
        if (points == null || points.Length < 2)
        {
            Debug.LogError($"{nameof(WaypointMover)} needs at least 2 points.", this);
            enabled = false;
            return;
        }

        index = 0;
        direction = 1;
    }

    void Update()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        if (speed <= 0f)
        {
            return;
        }

        var target = points[index].position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        float sqrDist = (transform.position - target).sqrMagnitude;
        if (sqrDist <= arriveDistance * arriveDistance)
        {
            AdvanceIndex();
        }
    }

    void AdvanceIndex()
    {
        waitTimer = Mathf.Max(0f, waitTimeAtPoint);

        if (chooseRandomNextPoint)
        {
            if (points.Length <= 1)
            {
                return;
            }

            int next;
            do
            {
                next = Random.Range(0, points.Length);
            } while (next == index);

            index = next;
            return;
        }

        if (pingPong)
        {
            // Flip direction at the ends.
            if (index == points.Length - 1)
            {
                direction = -1;
            }
            else if (index == 0)
            {
                direction = 1;
            }

            index += direction;
            index = Mathf.Clamp(index, 0, points.Length - 1);
            return;
        }

        index++;

        if (index < points.Length)
        {
            return;
        }

        if (loop)
        {
            index = 0;
        }
        else
        {
            // Stop on the last point.
            index = points.Length - 1;
            enabled = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (points == null || points.Length < 2)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(points[i].position, 0.15f);

            int next = i + 1;
            if (next < points.Length && points[next] != null)
            {
                Gizmos.DrawLine(points[i].position, points[next].position);
            }
        }

        // Optionally draw loop-back line.
        if (loop && !pingPong && points[0] != null && points[points.Length - 1] != null)
        {
            Gizmos.DrawLine(points[points.Length - 1].position, points[0].position);
        }
    }
}
