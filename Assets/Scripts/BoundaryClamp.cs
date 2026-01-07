using UnityEngine;

/// <summary>
/// Clamps a Rigidbody's position to the world-space bounds of a BoxCollider.
///
/// Use this to create “soft” level boundaries without physical wall meshes.
/// Put an (optionally Trigger) BoxCollider in the scene to define the playable volume,
/// then assign it in the inspector.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoundaryClamp : MonoBehaviour
{
    [Header("Boundary")]
    [SerializeField] BoxCollider boundary;

    [Tooltip("Shrinks the usable volume by this amount on each axis (world units).")]
    [SerializeField] Vector3 padding = Vector3.zero;

    [Header("Soft boundary")]
    [Tooltip("Distance from each boundary face where we start slowing/pushing the rocket back.")]
    [SerializeField] float softZoneDistance = 1.5f;

    [Tooltip("Optional: how strongly we push the rocket back toward the inside when it enters the soft zone (m/s²). Set to 0 for no rebound.")]
    [SerializeField] float inwardPushAcceleration = 0f;

    [Tooltip("How quickly we damp velocity that is trying to leave the box when in the soft zone (m/s²).")]
    [SerializeField] float outwardVelocityDamping = 10f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"{nameof(BoundaryClamp)} requires a {nameof(Rigidbody)} on the same GameObject.", this);
            enabled = false;
        }
    }

    void Start()
    {
        if (boundary == null)
        {
            Debug.LogError($"{nameof(BoundaryClamp)} needs a boundary {nameof(BoxCollider)} assigned.", this);
            enabled = false;
        }
    }

    void FixedUpdate()
    {
        var bounds = boundary.bounds;
        var pos = rb.position;

        var min = bounds.min + padding;
        var max = bounds.max - padding;

        var v = rb.linearVelocity;
        var soft = Mathf.Max(0f, softZoneDistance);
        if (soft > 0f)
        {
            var accel = Vector3.zero;

            var tMinX = Mathf.Clamp01(Mathf.InverseLerp(min.x + soft, min.x, pos.x));
            var tMaxX = Mathf.Clamp01(Mathf.InverseLerp(max.x - soft, max.x, pos.x));
            if (tMinX > 0f && v.x < 0f)
            {
                var distToMin = Mathf.Max(0f, pos.x - min.x);
                var maxOutwardSpeed = distToMin / Time.fixedDeltaTime;
                v.x = Mathf.Max(v.x, -maxOutwardSpeed);
                v.x = Mathf.MoveTowards(v.x, 0f, outwardVelocityDamping * tMinX * Time.fixedDeltaTime);
            }
            if (tMaxX > 0f && v.x > 0f)
            {
                var distToMax = Mathf.Max(0f, max.x - pos.x);
                var maxOutwardSpeed = distToMax / Time.fixedDeltaTime;
                v.x = Mathf.Min(v.x, maxOutwardSpeed);
                v.x = Mathf.MoveTowards(v.x, 0f, outwardVelocityDamping * tMaxX * Time.fixedDeltaTime);
            }
            accel.x = (tMinX - tMaxX) * inwardPushAcceleration;

            var tMinY = Mathf.Clamp01(Mathf.InverseLerp(min.y + soft, min.y, pos.y));
            var tMaxY = Mathf.Clamp01(Mathf.InverseLerp(max.y - soft, max.y, pos.y));
            if (tMinY > 0f && v.y < 0f)
            {
                var distToMin = Mathf.Max(0f, pos.y - min.y);
                var maxOutwardSpeed = distToMin / Time.fixedDeltaTime;
                v.y = Mathf.Max(v.y, -maxOutwardSpeed);
                v.y = Mathf.MoveTowards(v.y, 0f, outwardVelocityDamping * tMinY * Time.fixedDeltaTime);
            }
            if (tMaxY > 0f && v.y > 0f)
            {
                var distToMax = Mathf.Max(0f, max.y - pos.y);
                var maxOutwardSpeed = distToMax / Time.fixedDeltaTime;
                v.y = Mathf.Min(v.y, maxOutwardSpeed);
                v.y = Mathf.MoveTowards(v.y, 0f, outwardVelocityDamping * tMaxY * Time.fixedDeltaTime);
            }
            accel.y = (tMinY - tMaxY) * inwardPushAcceleration;

            var tMinZ = Mathf.Clamp01(Mathf.InverseLerp(min.z + soft, min.z, pos.z));
            var tMaxZ = Mathf.Clamp01(Mathf.InverseLerp(max.z - soft, max.z, pos.z));
            if (tMinZ > 0f && v.z < 0f)
            {
                var distToMin = Mathf.Max(0f, pos.z - min.z);
                var maxOutwardSpeed = distToMin / Time.fixedDeltaTime;
                v.z = Mathf.Max(v.z, -maxOutwardSpeed);
                v.z = Mathf.MoveTowards(v.z, 0f, outwardVelocityDamping * tMinZ * Time.fixedDeltaTime);
            }
            if (tMaxZ > 0f && v.z > 0f)
            {
                var distToMax = Mathf.Max(0f, max.z - pos.z);
                var maxOutwardSpeed = distToMax / Time.fixedDeltaTime;
                v.z = Mathf.Min(v.z, maxOutwardSpeed);
                v.z = Mathf.MoveTowards(v.z, 0f, outwardVelocityDamping * tMaxZ * Time.fixedDeltaTime);
            }
            accel.z = (tMinZ - tMaxZ) * inwardPushAcceleration;

            rb.linearVelocity = v;
            if (inwardPushAcceleration > 0f)
            {
                rb.AddForce(accel, ForceMode.Acceleration);
            }
        }

        var clamped = new Vector3(
            Mathf.Clamp(pos.x, min.x, max.x),
            Mathf.Clamp(pos.y, min.y, max.y),
            Mathf.Clamp(pos.z, min.z, max.z)
        );

        if (clamped == pos)
        {
            return;
        }

        // If we hit a boundary, cancel velocity in the blocked axes to reduce jitter.
        v = rb.linearVelocity;
        if (!Mathf.Approximately(pos.x, clamped.x)) v.x = 0f;
        if (!Mathf.Approximately(pos.y, clamped.y)) v.y = 0f;
        if (!Mathf.Approximately(pos.z, clamped.z)) v.z = 0f;
        rb.linearVelocity = v;

        // Keep it simple and deterministic.
        rb.position = clamped;
    }

    void OnDrawGizmosSelected()
    {
        if (boundary == null)
        {
            return;
        }

        var b = boundary.bounds;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
