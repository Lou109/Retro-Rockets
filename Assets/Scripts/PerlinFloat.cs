using UnityEngine;

/// <summary>
/// Adds organic "floating" motion using Perlin noise.
///
/// Recommended use for best results:
/// - Put WaypointMover on a PARENT GameObject (controls the path).
/// - Put PerlinFloat on the CHILD mesh/visual (adds natural drift/rotation).
///
/// This keeps the path deterministic while the visuals look alive.
/// </summary>
public class PerlinFloat : MonoBehaviour
{
    [Header("Space")]
    [SerializeField] bool useLocalSpace = true;

    [Header("Position")]
    [SerializeField] Vector3 positionAmplitude = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] float positionFrequency = 0.35f;

    [Header("Rotation")]
    [SerializeField] Vector3 rotationAmplitudeEuler = new Vector3(5f, 15f, 5f);
    [SerializeField] float rotationFrequency = 0.25f;

    Vector3 startPos;
    Quaternion startRot;

    Vector3 posSeed;
    Vector3 rotSeed;

    void Awake()
    {
        startPos = useLocalSpace ? transform.localPosition : transform.position;
        startRot = useLocalSpace ? transform.localRotation : transform.rotation;

        // Randomize each instance so they don't all move identically.
        posSeed = new Vector3(Random.value * 1000f, Random.value * 1000f, Random.value * 1000f);
        rotSeed = new Vector3(Random.value * 1000f, Random.value * 1000f, Random.value * 1000f);
    }

    void Update()
    {
        float tPos = Time.time * Mathf.Max(0f, positionFrequency);
        float tRot = Time.time * Mathf.Max(0f, rotationFrequency);

        Vector3 posOffset = new Vector3(
            NoiseSigned(posSeed.x, tPos),
            NoiseSigned(posSeed.y, tPos + 10f),
            NoiseSigned(posSeed.z, tPos + 20f)
        );
        posOffset = Vector3.Scale(posOffset, positionAmplitude);

        Vector3 rotOffsetEuler = new Vector3(
            NoiseSigned(rotSeed.x, tRot),
            NoiseSigned(rotSeed.y, tRot + 10f),
            NoiseSigned(rotSeed.z, tRot + 20f)
        );
        rotOffsetEuler = Vector3.Scale(rotOffsetEuler, rotationAmplitudeEuler);

        if (useLocalSpace)
        {
            transform.localPosition = startPos + posOffset;
            transform.localRotation = startRot * Quaternion.Euler(rotOffsetEuler);
        }
        else
        {
            transform.position = startPos + posOffset;
            transform.rotation = startRot * Quaternion.Euler(rotOffsetEuler);
        }
    }

    static float NoiseSigned(float seed, float t)
    {
        // PerlinNoise is 0..1, remap to -1..1
        return Mathf.PerlinNoise(seed, t) * 2f - 1f;
    }
}
