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
    [Tooltip("Within this distance (world units) from an enabled boundary face, thrust fades down to 0.")]
    [SerializeField] float boundarySoftZoneDistance = 4f;
    [Tooltip("If true, approaching the ceiling (max Y) reduces thrust/boost so you don't pin against the top boundary.")]
    [SerializeField] bool softenCeiling = true;
    [Tooltip("If true, approaching the side walls (min/max X/Z) reduces thrust. Leave false to avoid losing altitude when touching a wall.")]
    [SerializeField] bool softenSideWalls = false;
    [Tooltip("Extra drag added when near an enabled soft boundary to help the rocket come to a smooth stop.")]
    [SerializeField] float maxExtraDragNearBoundary = 2f;
     
    Rigidbody rb;
    AudioSource audioSource;
    float baseDrag;
    
    void Awake()
    {
        EnsureHardBounds();
    }

    void EnsureHardBounds()
    {
        if (movementBounds == null)
        {
            return;
        }

        var clamp = GetComponent<BoundaryClamp>();
        if (clamp == null)
        {
            clamp = gameObject.AddComponent<BoundaryClamp>();
        }

        clamp.SetBoundary(movementBounds);
        clamp.enabled = true;
    }

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

        // Important: don't kill all thrust when touching a side wall.
        // If side walls reduce thrust, the rocket can lose altitude and "slide down" into a crash.
        float scale = 1f;
        if (softenCeiling)
        {
            float distToMaxY = b.max.y - pos.y;
            scale = Mathf.Min(scale, Mathf.Clamp01(distToMaxY / soft));
        }
        if (softenSideWalls)
        {
            float distToMinX = pos.x - b.min.x;
            float distToMaxX = b.max.x - pos.x;
            float distToMinZ = pos.z - b.min.z;
            float distToMaxZ = b.max.z - pos.z;
            float nearestSide = Mathf.Min(distToMinX, distToMaxX, distToMinZ, distToMaxZ);
            scale = Mathf.Min(scale, Mathf.Clamp01(nearestSide / soft));
        }

        return scale;
    }
}