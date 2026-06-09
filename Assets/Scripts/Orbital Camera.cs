using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitalCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Follow")]
    [SerializeField] private float followLerpSpeed = 8f;

    [Header("Orbit")]
    [SerializeField] private float orbitSensitivity = 160f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private bool invertY = false;

    private float yaw;
    private float pitch;
    private float orbitDistance;
    private Vector3 smoothedPivot;
    private bool initialized;

    public void SetTarget(Transform newTarget)
    {
        if (target == newTarget)
        {
            return;
        }

        target = newTarget;

        if (target == null)
        {
            initialized = false;
            return;
        }

        if (!initialized)
        {
            InitializeOrbitState();
            return;
        }

        // Keep current yaw/pitch/distance so camera does not snap or zoom on target switch.
        Vector3 newPivot = target.position + targetOffset;
        smoothedPivot = Vector3.Lerp(smoothedPivot, newPivot, 0.15f);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!initialized)
        {
            InitializeOrbitState();
        }

        Mouse mouse = Mouse.current;
        bool isOrbiting = mouse != null && mouse.rightButton.isPressed;
        if (isOrbiting)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float mouseX = delta.x;
            float mouseY = delta.y;

            yaw += mouseX * orbitSensitivity * Time.deltaTime;
            float ySign = invertY ? 1f : -1f;
            pitch += mouseY * ySign * orbitSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Vector3 targetPivot = target.position + targetOffset;
        float followT = 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime);
        smoothedPivot = Vector3.Lerp(smoothedPivot, targetPivot, followT);

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = smoothedPivot + orbitRotation * (Vector3.back * orbitDistance);
        if (isOrbiting)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);
        }

        Vector3 lookDirection = smoothedPivot - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            if (isOrbiting)
            {
                transform.rotation = desiredRotation;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, followT);
            }
        }
    }

    private void InitializeOrbitState()
    {
        Vector3 pivot = target.position + targetOffset;
        smoothedPivot = pivot;

        Vector3 offset = transform.position - pivot;
        orbitDistance = Mathf.Max(0.1f, offset.magnitude);

        Vector3 dir = offset / orbitDistance;
        yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        initialized = true;
    }
}
