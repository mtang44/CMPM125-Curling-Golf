using UnityEngine;

public class RotatingObstacle : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float degreesPerSecond = 90f;
    [SerializeField] private Space rotationSpace = Space.Self;

    private void OnValidate()
    {
        if (rotationAxis == Vector3.zero)
        {
            rotationAxis = Vector3.up;
        }
    }

    private void Update()
    {
        Vector3 axis = rotationAxis.normalized;
        if (axis.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float frameRotation = degreesPerSecond * Time.deltaTime;
        transform.Rotate(axis, frameRotation, rotationSpace);
    }
}
