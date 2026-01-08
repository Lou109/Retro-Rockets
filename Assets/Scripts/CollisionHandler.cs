using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] float levelLoadDelay = 2f;
    [SerializeField]float crashTimeDelay = 3f;
    [SerializeField] AudioClip crashSound;
    [SerializeField] AudioClip successSound;
    [SerializeField] ParticleSystem successParticles;
    [SerializeField] ParticleSystem crashParticles;

    AudioSource audioSource;
   
    bool isControllable = true;
    bool isCollidable = true;

    const string TAG_FRIENDLY = "Friendly";
    const string TAG_FINISH = "Finish";
    const string TAG_OBSTACLE = "Obstacle";
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
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
        audioSource.Stop();
        audioSource.PlayOneShot(successSound);
        successParticles.Play();
        GetComponent<Movement>().enabled = false;
        Invoke("LoadNextLevel", levelLoadDelay);
    }

    void StartCrashSequence()
    {
        isControllable = false;
        audioSource.Stop();
        audioSource.PlayOneShot(crashSound);
        crashParticles.Play();
        GetComponent<Movement>().enabled = false;
        Invoke("ReloadLevel", crashTimeDelay);    
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