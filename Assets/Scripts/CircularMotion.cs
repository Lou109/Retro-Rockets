using UnityEngine;

public class CircularMotion : MonoBehaviour
{
    [Header("Circular Motion Settings")]
    [Tooltip("Speed of rotation in degrees per second")]
    public float speed = 30f;
    
    [Tooltip("Radius of the circular path")]
    public float radius = 5f;
    
    private Vector3 centerPoint;
    private float currentAngle = 0f;

    void Start()
    {
        // Store the center point as the object's starting position
        centerPoint = transform.position;
    }

    void Update()
    {
        // Increment the angle based on speed and deltaTime
        currentAngle += speed * Time.deltaTime;
        
        // Calculate the X and Y positions using cosine and sine
        // Object moves in a circle in the X-Y plane while staying at Z=0
        float xPos = centerPoint.x + Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius;
        float yPos = centerPoint.y + Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
        
        // Set the new position with Z always at 0
        transform.position = new Vector3(xPos, yPos, 0f);
    }
}
