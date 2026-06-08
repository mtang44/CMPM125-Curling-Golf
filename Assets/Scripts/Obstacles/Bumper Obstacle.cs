using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BumperObstacle : MonoBehaviour
{
    [Header("Bumper Settings")]
    [SerializeField] private float bounceMultiplier = 1.2f;
    [SerializeField] private float minimumBounceSpeed = 4f;
    [SerializeField] private bool pushDirectlyAway = true;
    [SerializeField] private bool reflectOffSurface = true;

    [Header("Safety")]
    [SerializeField] private float repeatHitCooldown = 0.05f;

    [Header("Visual Pulse")]
    [SerializeField] private Transform bumperRingVisual;
    [SerializeField] private float ringExpandMultiplier = 1.35f;
    [SerializeField] private float ringExpandDuration = 0.06f;
    [SerializeField] private float ringRetractDuration = 0.12f;

    private Rigidbody lastHitBody;
    private float lastHitTime;
    private Vector3 ringBaseScale = Vector3.one;
    private Coroutine ringPulseRoutine;

    private void Awake()
    {
        if (bumperRingVisual != null)
        {
            ringBaseScale = bumperRingVisual.localScale;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0)
        {
            return;
        }

        Vector3 surfaceNormal = collision.GetContact(0).normal;
        TryBounce(collision.rigidbody, surfaceNormal);
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null)
        {
            return;
        }

        Vector3 fromBumperToStone = (body.worldCenterOfMass - transform.position).normalized;
        if (fromBumperToStone == Vector3.zero)
        {
            fromBumperToStone = transform.forward;
        }

        TryBounce(body, fromBumperToStone);
    }

    private void TryBounce(Rigidbody body, Vector3 surfaceNormal)
    {
        if (body == null)
        {
            return;
        }

        // Only affect curling stones.
        if (body.GetComponent<StoneController>() == null && body.GetComponentInParent<StoneController>() == null)
        {
            return;
        }

        if (body == lastHitBody && Time.time - lastHitTime < repeatHitCooldown)
        {
            return;
        }

        Vector3 incomingVelocity = body.linearVelocity;
        if (incomingVelocity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 incomingPlanar = new Vector3(incomingVelocity.x, 0f, incomingVelocity.z);

        Vector3 outgoingDirection;
        if (pushDirectlyAway)
        {
            outgoingDirection = body.worldCenterOfMass - transform.position;
            outgoingDirection.y = 0f;

            if (outgoingDirection.sqrMagnitude < 0.0001f)
            {
                outgoingDirection = -incomingPlanar;
            }
        }
        else if (reflectOffSurface)
        {
            Vector3 planarNormal = new Vector3(surfaceNormal.x, 0f, surfaceNormal.z).normalized;
            if (planarNormal.sqrMagnitude < 0.0001f)
            {
                planarNormal = transform.forward;
                planarNormal.y = 0f;
            }

            Vector3 reflected = Vector3.Reflect(incomingPlanar.normalized, planarNormal);
            outgoingDirection = reflected;
        }
        else
        {
            outgoingDirection = -incomingPlanar;
        }

        outgoingDirection = outgoingDirection.normalized;
        if (outgoingDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float outgoingSpeed = Mathf.Max(incomingVelocity.magnitude * bounceMultiplier, minimumBounceSpeed);
        body.linearVelocity = outgoingDirection * outgoingSpeed;

        TriggerRingPulse();

        lastHitBody = body;
        lastHitTime = Time.time;
    }

    private void TriggerRingPulse()
    {
        if (bumperRingVisual == null)
        {
            return;
        }

        if (ringPulseRoutine != null)
        {
            StopCoroutine(ringPulseRoutine);
            bumperRingVisual.localScale = ringBaseScale;
        }

        ringPulseRoutine = StartCoroutine(PulseRing());
    }

    private IEnumerator PulseRing()
    {
        Vector3 expandedScale = ringBaseScale * ringExpandMultiplier;

        float elapsed = 0f;
        while (elapsed < ringExpandDuration)
        {
            elapsed += Time.deltaTime;
            float t = ringExpandDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / ringExpandDuration);
            bumperRingVisual.localScale = Vector3.LerpUnclamped(ringBaseScale, expandedScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < ringRetractDuration)
        {
            elapsed += Time.deltaTime;
            float t = ringRetractDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / ringRetractDuration);
            bumperRingVisual.localScale = Vector3.LerpUnclamped(expandedScale, ringBaseScale, t);
            yield return null;
        }

        bumperRingVisual.localScale = ringBaseScale;
        ringPulseRoutine = null;
    }
}
