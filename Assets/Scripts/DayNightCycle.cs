using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    public float dayLength = 240f;

    private float rotationSpeed;

    void Start()
    {
        rotationSpeed = 360f / dayLength;
    }

    void Update()
    {
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
    }
}