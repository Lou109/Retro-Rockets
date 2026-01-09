using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class CollisionHandler : MonoBehaviour
{
    [SerializeField] float levelLoadDelay = 2f;
    [SerializeField]float crashTimeDelay = 3f;
    [SerializeField] AudioClip crashSound;
    [SerializeField] AudioClip successSound;
    [SerializeField] ParticleSystem successParticles;
    [SerializeField] ParticleSystem crashParticles;

    [Header("Crash VFX (optional prefab)")]
    [Tooltip("If set, this prefab will be spawned at the rocket position on crash (use this for complex VFX packs like DAVFX).")]
    [SerializeField] GameObject crashVfxPrefab;
    [Tooltip("If > 0, the spawned VFX prefab will be destroyed after this many seconds.")]
    [SerializeField] float crashVfxDestroyDelay = 5f;

    [Header("Crash VFX spawn")]
    [Tooltip("Optional: spawn transform used for the crash VFX (create an empty child at the rocket nose and assign it here).")]
    [SerializeField] Transform crashVfxSpawnPoint;

    AudioSource audioSource;
   
    bool isControllable = true;
    bool isCollidable = true;

    const string TAG_FRIENDLY = "Friendly";
    const string TAG_FINISH = "Finish";
    const string TAG_OBSTACLE = "Obstacle";
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.pitch = 1f;
    }

    void Update()
    {
        RespondToDebugKeys();
    }

     void RespondToDebugKeys()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            LoadNextLevel();
        }
        else if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            isCollidable = !isCollidable;     
        }
    }    

    private void OnCollisionEnter(Collision other)
    {
        HandleContact(other.gameObject, isTrigger: false);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleContact(other.gameObject, isTrigger: true);
    }

    void HandleContact(GameObject otherObject, bool isTrigger)
    {
        if (!isControllable || !isCollidable)
        {
            return;
        }

        // Prefer tags so level design stays simple.
        if (otherObject.CompareTag(TAG_FRIENDLY))
        {
            // Friendly objects: landing pads, boundaries, etc.
            return;
        }

        if (otherObject.CompareTag(TAG_FINISH))
        {
            StartSuccessSequence();
            return;
        }

        if (isTrigger)
        {
            // Triggers are opt-in for crashing (so you don't crash on random trigger volumes).
            if (otherObject.CompareTag(TAG_OBSTACLE))
            {
                StartCrashSequence();
            }

            return;
        }

        // Physical collisions with anything not Friendly/Finish count as crash.
        StartCrashSequence();
    }

    void StartSuccessSequence()
    {
        isControllable = false;

        var movement = GetComponent<Movement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        if (successSound == null)
        {
            Debug.LogWarning($"{nameof(CollisionHandler)}: successSound is not assigned.", this);
        }

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.PlayOneShot(successSound);
        successParticles.Play();
        Invoke("LoadNextLevel", levelLoadDelay);
    }

    void StartCrashSequence()
    {
        isControllable = false;

        var movement = GetComponent<Movement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        if (crashSound == null)
        {
            Debug.LogWarning($"{nameof(CollisionHandler)}: crashSound is not assigned (this is your death explosion SFX).", this);
        }

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.PlayOneShot(crashSound);
        PlayCrashVfx();
        Invoke("ReloadLevel", crashTimeDelay);    
    }

    void PlayCrashVfx()
    {
        if (crashVfxPrefab != null)
        {
            GetCrashVfxSpawnPose(out var spawnPos, out var spawnRot);
            var instance = Instantiate(crashVfxPrefab, spawnPos, spawnRot);

            // Force-play all child particle systems (handles complex prefabs and inactive children).
            var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                ps.gameObject.SetActive(true);
                ps.Play(true);
            }

            if (crashVfxDestroyDelay > 0f)
            {
                Destroy(instance, crashVfxDestroyDelay);
            }

            return;
        }

        if (crashParticles != null)
        {
            crashParticles.gameObject.SetActive(true);
            crashParticles.Play(true);
        }
        else
        {
            Debug.LogWarning($"{nameof(CollisionHandler)}: No crash VFX assigned (set crashVfxPrefab or crashParticles).", this);
        }
    }

    void GetCrashVfxSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        if (crashVfxSpawnPoint != null)
        {
            position = crashVfxSpawnPoint.position;
            rotation = crashVfxSpawnPoint.rotation;
            return;
        }

        position = transform.position;
        rotation = transform.rotation;
    }

    void LoadNextLevel()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        int nextScene = currentScene + 1;
        if (nextScene == SceneManager.sceneCountInBuildSettings)
        {
            nextScene = 0;
        }
        SceneManager.LoadScene(nextScene);
    }

    void ReloadLevel()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentScene);
    } 
}