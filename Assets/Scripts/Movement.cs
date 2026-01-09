using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
public class Movement : MonoBehaviour
{
    [SerializeField] InputAction thrust;
    [SerializeField] InputAction rotation;
    [SerializeField] float thrustStrength = 100f;
    [SerializeField] float rotationStrength = 100f;
    [SerializeField] AudioClip mainEngine;
    [SerializeField] ParticleSystem mainEngineParticle;

    [Header("Optional: soft boundary")]
    [SerializeField] BoxCollider movementBounds;
    [Tooltip("Within this distance (world units) from a boundary face, thrust fades down to 0.")]
    [SerializeField] float boundarySoftZoneDistance = 4f;
    [Tooltip("Extra drag added near the boundary to help the rocket come to a smooth stop.")]
    [SerializeField] float maxExtraDragNearBoundary = 2f;
     
    Rigidbody rb;
    AudioSource audioSource;
    float baseDrag;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.pitch = 1f;
        baseDrag = rb.linearDamping;
        
    }
    private void OnEnable()
    {
        thrust.Enable();
        rotation.Enable();
    }

    private void FixedUpdate()
    {
        ApplySoftBoundaryDrag();
        ProcessThrust();
        ProcessRotation();
    }

    private void ApplySoftBoundaryDrag()
    {
        if (movementBounds == null)
        {
            rb.linearDamping = baseDrag;
            return;
        }

        float thrustScale = GetBoundaryThrustScale01();
        float extra = (1f - thrustScale) * Mathf.Max(0f, maxExtraDragNearBoundary);
        rb.linearDamping = baseDrag + extra;
    }

    private void ProcessThrust()
    {
        if (thrust.IsPressed())
        {
            StartThrusting();
        }
        else
        {
            StopThrusting();
        }
    }

    private void StartThrusting()
    {
        float thrustScale = GetBoundaryThrustScale01();
        rb.AddRelativeForce(thrustStrength * thrustScale * Time.fixedDeltaTime * Vector3.up);
        if (!audioSource.isPlaying)
        {
            audioSource.clip = mainEngine;
            audioSource.loop = true;
            audioSource.Play();
        }
        if (!mainEngineParticle.isPlaying)
        {
            mainEngineParticle.Play();
        }
    }

    private void StopThrusting()
    {
        audioSource.Stop();
        mainEngineParticle.Stop();
    }

    private void ProcessRotation()
    {
        float rotationInput = rotation.ReadValue<float>();
        if(rotationInput < 0)
        {
           
            RotateRight();
        }
        else if(rotationInput > 0)
        {
            RotateLeft();
        }
        else
        {
            StopRotating();
        }
    }

    private void RotateRight()
    {
        ApplyRotation(rotationStrength);
    }

     private void RotateLeft()
    {
        ApplyRotation(-rotationStrength);
    }

    private void StopRotating()
    {
    }

    private void ApplyRotation(float rotationThisFrame)
    {
        rb.freezeRotation = true;
        transform.Rotate(Vector3.forward * rotationThisFrame * Time.fixedDeltaTime);
        rb.freezeRotation = false;
    }   

    /// <summary>
    /// Returns 0..1 thrust scale based on proximity to the edges of movementBounds.
    /// This is intentionally "soft" (no position clamping), so it feels like slowing down, not hitting a wall.
    /// </summary>
    private float GetBoundaryThrustScale01()
    {
        if (movementBounds == null)
        {
            return 1f;
        }

        float soft = Mathf.Max(0.01f, boundarySoftZoneDistance);
        var b = movementBounds.bounds;
        var pos = rb.position;

        // For Rocket Boost, we typically care about horizontal area (X/Z),
        // plus optionally a "ceiling" (max Y) to avoid hard bounces at the top.
        float distToMinX = pos.x - b.min.x;
        float distToMaxX = b.max.x - pos.x;
        float distToMinZ = pos.z - b.min.z;
        float distToMaxZ = b.max.z - pos.z;

        float distToMaxY = b.max.y - pos.y;

        float nearest = Mathf.Min(distToMinX, distToMaxX, distToMinZ, distToMaxZ, distToMaxY);
        return Mathf.Clamp01(nearest / soft);
    }
}