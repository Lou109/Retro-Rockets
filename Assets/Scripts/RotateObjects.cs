using UnityEngine;

public class RotateObjects : MonoBehaviour
{
    [SerializeField] float xSpeedRotate = 1f;
    [SerializeField] float ySpeedRotate = 1f;
    [SerializeField] float zSpeedRotate = 1f;
    void Update()
    {
        transform.Rotate(new Vector3(xSpeedRotate, ySpeedRotate, zSpeedRotate) * Time.deltaTime);
    }
}
