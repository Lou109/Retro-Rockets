using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] InputAction thrust;
    [SerializeField] InputAction rotation;
    [SerializeField] float thrustStrength = 100f;
    [SerializeField] float rotationStrength = 100f;
    [SerializeField] AudioClip mainEngine;
    [SerializeField] ParticleSystem mainBooster;
    [SerializeField] ParticleSystem leftBooster;
    [SerializeField] ParticleSystem rightBooster;
     
    Rigidbody rb;
    AudioSource audioSource;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
    }
    private void OnEnable()
    {
        thrust.Enable();
        rotation.Enable();
    }

    private void FixedUpdate()
    {
        ProcessThrust();
        ProcessRotation();
    }

    private void ProcessThrust()
    {
        if (thrust.IsPressed())
        {
            rb.AddRelativeForce(thrustStrength * Time.fixedDeltaTime * Vector3.up);
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(mainEngine);
            }
            if (!mainBooster.isPlaying)
            {
                 mainBooster.Play();
            }
        }     
        else
        {
            audioSource.Stop();
            mainBooster.Stop();
        }
    }

    private void ProcessRotation()
    {
        float rotationInput = rotation.ReadValue<float>();
        if(rotationInput < 0)
        {
            ApplyRotation(rotationStrength); 
              
            if (!leftBooster.isPlaying)
            {
                 rightBooster.Stop();
                 leftBooster.Play();
            }  
        }
        
        else if(rotationInput > 0)
        {
            ApplyRotation(-rotationStrength);
             if (!rightBooster.isPlaying)
            {
                 leftBooster.Stop();
                 rightBooster.Play();
            }  
        }
        else
        {
            rightBooster.Stop();
            leftBooster.Stop();
        }
    }

    private void ApplyRotation(float rotationThisFrame)
    {
        rb.freezeRotation = true;
        transform.Rotate(Vector3.forward * rotationThisFrame * Time.fixedDeltaTime);
        rb.freezeRotation = false;
    }   
}