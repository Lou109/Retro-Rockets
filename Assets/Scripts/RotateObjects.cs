using UnityEngine;

public class RotateObjects : MonoBehaviour
{
    [SerializeField] float xSpeedRotate = 1f;
    [SerializeField] float ySpeedRotate = 1f;
    [SerializeField] float zSpeedRotate = 1f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        var eulerDelta = new Vector3(xSpeedRotate, ySpeedRotate, zSpeedRotate) * Time.fixedDeltaTime;

        // If this object is physics-driven, rotate in a physics-friendly way.
        if (rb != null && !rb.isKinematic)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(eulerDelta));
            return;
        }

        // Otherwise keep the simple transform rotation.
        transform.Rotate(eulerDelta);
    }
}
