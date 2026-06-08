using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;
using System.Collections.Generic;
using System.Collections;

public class ThrowerObstacle : MonoBehaviour
{
    private enum EnemyState
    {
        Patrol,
        Chase,
        Carrying,
        Throwing,
        Cooldown
    }

    [Header("Patrol")]
    [SerializeField] private SplineContainer patrolSpline;
    [SerializeField] private bool loopPatrol = true;
    [SerializeField] private float patrolCycleDuration = 8f;
    [SerializeField] private int splineDistanceSamples = 120;

    [Header("Detection")]
    [SerializeField] private ThrowerSightTrigger sightTrigger;
    [SerializeField] private float chaseAcquireRange = 10f;

    [Header("Movement")]
    [SerializeField] private bool useNavMesh = true;
    [SerializeField] private float patrolMoveSpeed = 4f;
    [SerializeField] private float chaseMoveSpeed = 7f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float navRepathInterval = 0.1f;
    [SerializeField] private float maxDistanceFromPath = 8f;

    [Header("Grab And Throw")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float grabDistance = 1.4f;
    [SerializeField] private float carryDuration = 1.2f;
    [SerializeField] private float throwWindupDuration = 0.35f;
    [SerializeField] private float throwSpeed = 18f;
    [SerializeField] private float throwUpwardBoost = 2f;
    [SerializeField] private Transform throwDirectionReference;
    [SerializeField] private float postThrowCooldown = 1.2f;

    private EnemyState state = EnemyState.Patrol;
    private StoneController chaseTarget;
    private StoneController carriedStoneController;
    private Rigidbody carriedStoneBody;
    private Transform carriedOriginalParent;
    private float carryTimer;
    private float cooldownTimer;
    private Coroutine throwRoutine;
    private NavMeshAgent navAgent;
    private float nextRepathTime;
    private readonly List<StoneController> sensedTargets = new List<StoneController>();

    private readonly List<float> distanceTable = new List<float>();
    private readonly List<float> tTable = new List<float>();
    private float patrolLength;

    private void Awake()
    {
        patrolCycleDuration = Mathf.Max(0.01f, patrolCycleDuration);
        splineDistanceSamples = Mathf.Max(8, splineDistanceSamples);
        chaseAcquireRange = Mathf.Max(0.1f, chaseAcquireRange);
        maxDistanceFromPath = Mathf.Max(0.1f, maxDistanceFromPath);
        navRepathInterval = Mathf.Max(0.02f, navRepathInterval);
        throwWindupDuration = Mathf.Max(0f, throwWindupDuration);
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.updateRotation = false;
        }

        if (sightTrigger != null)
        {
            sightTrigger.Initialize(this);
        }

        BuildPatrolCache();
    }

    private void Update()
    {
        switch (state)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                TryAcquireTarget();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;

            case EnemyState.Carrying:
                UpdateCarrying();
                break;

            case EnemyState.Throwing:
                break;

            case EnemyState.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    private void OnValidate()
    {
        patrolCycleDuration = Mathf.Max(0.01f, patrolCycleDuration);
        splineDistanceSamples = Mathf.Max(8, splineDistanceSamples);
        grabDistance = Mathf.Max(0.1f, grabDistance);
        carryDuration = Mathf.Max(0.1f, carryDuration);
        throwWindupDuration = Mathf.Max(0f, throwWindupDuration);
        throwSpeed = Mathf.Max(0.1f, throwSpeed);
        chaseAcquireRange = Mathf.Max(0.1f, chaseAcquireRange);
        patrolMoveSpeed = Mathf.Max(0.1f, patrolMoveSpeed);
        chaseMoveSpeed = Mathf.Max(0.1f, chaseMoveSpeed);
        turnSpeed = Mathf.Max(0.1f, turnSpeed);
        postThrowCooldown = Mathf.Max(0f, postThrowCooldown);
        navRepathInterval = Mathf.Max(0.02f, navRepathInterval);
        maxDistanceFromPath = Mathf.Max(0.1f, maxDistanceFromPath);

        BuildPatrolCache();
    }

    private void UpdatePatrol()
    {
        if (!HasValidPatrolSpline())
        {
            return;
        }

        float normalizedTime = Time.time / patrolCycleDuration;
        float progress = loopPatrol ? Mathf.Repeat(normalizedTime, 1f) : Mathf.PingPong(normalizedTime, 1f);
        Vector3 targetPos = GetPatrolPositionAtProgress(progress);
        MoveEnemyTowards(targetPos, patrolMoveSpeed);

        Vector3 tangent = GetPatrolTangentAtProgress(progress);
        RotateTowards(tangent);
    }

    private void TryAcquireTarget()
    {
        StoneController bestVisible = null;
        float bestDistanceSq = float.MaxValue;

        for (int i = sensedTargets.Count - 1; i >= 0; i--)
        {
            StoneController candidate = sensedTargets[i];
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                sensedTargets.RemoveAt(i);
                continue;
            }

            Rigidbody body = candidate.GetComponent<Rigidbody>();
            if (body == null)
            {
                continue;
            }

            float distanceSq = (body.worldCenterOfMass - transform.position).sqrMagnitude;
            if (distanceSq > chaseAcquireRange * chaseAcquireRange)
            {
                continue;
            }

            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestVisible = candidate;
            }
        }

        if (bestVisible != null)
        {
            chaseTarget = bestVisible;
            state = EnemyState.Chase;
        }
    }

    private void UpdateChase()
    {
        if (chaseTarget == null)
        {
            state = EnemyState.Patrol;
            return;
        }

        Rigidbody targetBody = chaseTarget.GetComponent<Rigidbody>();
        if (targetBody == null)
        {
            state = EnemyState.Patrol;
            chaseTarget = null;
            return;
        }

        Vector3 targetPos = targetBody.worldCenterOfMass;

        if (GetDistanceFromPatrolPathSqr(transform.position) > maxDistanceFromPath * maxDistanceFromPath)
        {
            chaseTarget = null;
            state = EnemyState.Patrol;
            StopMovement();
            return;
        }

        MoveEnemyTowards(targetPos, chaseMoveSpeed);
        RotateTowards(targetPos - transform.position);

        float distSq = (targetPos - transform.position).sqrMagnitude;
        if (distSq <= grabDistance * grabDistance)
        {
            GrabTarget(chaseTarget, targetBody);
        }
    }

    private void GrabTarget(StoneController target, Rigidbody body)
    {
        if (target == null || body == null)
        {
            state = EnemyState.Patrol;
            chaseTarget = null;
            return;
        }

        if (holdPoint == null)
        {
            holdPoint = transform;
        }

        carriedStoneController = target;
        carriedStoneBody = body;
        carriedOriginalParent = target.transform.parent;

        carriedStoneController.SetBeingThrownByEnemy(true);
        carriedStoneController.enabled = false;
        carriedStoneBody.linearVelocity = Vector3.zero;
        carriedStoneBody.angularVelocity = Vector3.zero;
        carriedStoneBody.isKinematic = true;

        target.transform.SetParent(holdPoint, true);
        target.transform.position = holdPoint.position;
        target.transform.rotation = holdPoint.rotation;

        StopMovement();

        carryTimer = carryDuration;
        state = EnemyState.Carrying;
        sensedTargets.Remove(target);
        chaseTarget = null;
    }

    private void UpdateCarrying()
    {
        if (carriedStoneController == null || carriedStoneBody == null)
        {
            state = EnemyState.Patrol;
            return;
        }

        if (holdPoint != null)
        {
            carriedStoneController.transform.position = holdPoint.position;
            carriedStoneController.transform.rotation = holdPoint.rotation;
        }

        carryTimer -= Time.deltaTime;
        if (carryTimer <= 0f)
        {
            if (throwRoutine == null)
            {
                throwRoutine = StartCoroutine(ThrowCarriedStoneRoutine());
            }
        }
    }

    private IEnumerator ThrowCarriedStoneRoutine()
    {
        if (carriedStoneController == null || carriedStoneBody == null)
        {
            state = EnemyState.Patrol;
            throwRoutine = null;
            yield break;
        }

        Vector3 throwTargetPoint = throwDirectionReference != null ? throwDirectionReference.position : transform.position + transform.forward;
        Vector3 throwDirection = throwTargetPoint - transform.position;
        throwDirection.y = 0f;
        if (throwDirection.sqrMagnitude < 0.0001f)
        {
            throwDirection = transform.forward;
            throwDirection.y = 0f;
        }

        throwDirection = throwDirection.normalized;
        state = EnemyState.Throwing;

        Quaternion targetRotation = Quaternion.LookRotation(throwDirection, Vector3.up);
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, throwWindupDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            yield return null;
        }

        Vector3 launchVelocity = throwDirection * throwSpeed + Vector3.up * throwUpwardBoost;

        carriedStoneController.transform.SetParent(carriedOriginalParent, true);
        carriedStoneBody.isKinematic = false;
        carriedStoneBody.linearVelocity = launchVelocity;
        carriedStoneController.SetBeingThrownByEnemy(false);
        carriedStoneController.enabled = true;

        carriedStoneController = null;
        carriedStoneBody = null;
        carriedOriginalParent = null;

        cooldownTimer = postThrowCooldown;
        state = EnemyState.Cooldown;
        throwRoutine = null;
    }

    private void UpdateCooldown()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            state = EnemyState.Patrol;
        }
    }

    private void MoveEnemyTowards(Vector3 destination, float speed)
    {
        if (CanUseNavMesh())
        {
            navAgent.speed = speed;

            if (Time.time >= nextRepathTime)
            {
                navAgent.SetDestination(destination);
                nextRepathTime = Time.time + navRepathInterval;
            }

            return;
        }

        Vector3 from = transform.position;
        destination.y = from.y;
        transform.position = Vector3.MoveTowards(from, destination, speed * Time.deltaTime);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (CanUseNavMesh())
        {
            Vector3 desired = navAgent.desiredVelocity;
            desired.y = 0f;
            if (desired.sqrMagnitude > 0.0001f)
            {
                direction = desired;
            }
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    private bool CanUseNavMesh()
    {
        return useNavMesh && navAgent != null && navAgent.isOnNavMesh;
    }

    private void StopMovement()
    {
        if (CanUseNavMesh())
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }
    }

    private bool HasValidPatrolSpline()
    {
        return patrolSpline != null && patrolSpline.Spline != null;
    }

    private Vector3 GetPatrolPositionAtProgress(float progress01)
    {
        float distance = Mathf.Clamp01(progress01) * patrolLength;
        float t = GetTAtDistance(distance);
        return EvaluateSplinePosition(t);
    }

    private Vector3 GetPatrolTangentAtProgress(float progress01)
    {
        float distance = Mathf.Clamp01(progress01) * patrolLength;
        float t = GetTAtDistance(distance);
        return EvaluateSplineTangent(t);
    }

    private void BuildPatrolCache()
    {
        distanceTable.Clear();
        tTable.Clear();
        patrolLength = 0f;

        if (!HasValidPatrolSpline())
        {
            return;
        }

        Vector3 previous = EvaluateSplinePosition(0f);
        distanceTable.Add(0f);
        tTable.Add(0f);

        for (int i = 1; i <= splineDistanceSamples; i++)
        {
            float t = (float)i / splineDistanceSamples;
            Vector3 current = EvaluateSplinePosition(t);
            patrolLength += Vector3.Distance(previous, current);
            previous = current;

            distanceTable.Add(patrolLength);
            tTable.Add(t);
        }

        if (patrolLength <= 0.0001f)
        {
            patrolLength = 0.0001f;
        }
    }

    private float GetTAtDistance(float distance)
    {
        if (distanceTable.Count < 2)
        {
            return 0f;
        }

        float clampedDistance = Mathf.Clamp(distance, 0f, patrolLength);
        for (int i = 1; i < distanceTable.Count; i++)
        {
            float previousDistance = distanceTable[i - 1];
            float nextDistance = distanceTable[i];
            if (clampedDistance <= nextDistance)
            {
                float segmentLength = nextDistance - previousDistance;
                float segmentT = segmentLength <= 0.0001f ? 0f : (clampedDistance - previousDistance) / segmentLength;
                return Mathf.Lerp(tTable[i - 1], tTable[i], segmentT);
            }
        }

        return 1f;
    }

    private float GetDistanceFromPatrolPathSqr(Vector3 worldPosition)
    {
        if (!HasValidPatrolSpline())
        {
            return float.MaxValue;
        }

        float bestDistanceSq = float.MaxValue;
        const int samples = 60;

        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 splinePoint = EvaluateSplinePosition(t);
            float distanceSq = (worldPosition - splinePoint).sqrMagnitude;
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
            }
        }

        return bestDistanceSq;
    }

    private Vector3 EvaluateSplinePosition(float t)
    {
        Vector3 local = (Vector3)SplineUtility.EvaluatePosition(patrolSpline.Spline, Mathf.Clamp01(t));
        return patrolSpline.transform.TransformPoint(local);
    }

    private Vector3 EvaluateSplineTangent(float t)
    {
        Vector3 localTangent = (Vector3)SplineUtility.EvaluateTangent(patrolSpline.Spline, Mathf.Clamp01(t));
        return patrolSpline.transform.TransformDirection(localTangent);
    }

    private void OnDrawGizmosSelected()
    {
        if (HasValidPatrolSpline())
        {
            Gizmos.color = Color.red;
            const int drawSegments = 50;
            Vector3 previous = EvaluateSplinePosition(0f);
            for (int i = 1; i <= drawSegments; i++)
            {
                float t = (float)i / drawSegments;
                Vector3 current = EvaluateSplinePosition(t);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }

            if (loopPatrol)
            {
                Gizmos.DrawLine(EvaluateSplinePosition(1f), EvaluateSplinePosition(0f));
            }
        }
    }

    public void RegisterSightTarget(StoneController target)
    {
        if (target == null)
        {
            return;
        }

        if (!sensedTargets.Contains(target))
        {
            sensedTargets.Add(target);
        }
    }

    public void UnregisterSightTarget(StoneController target)
    {
        if (target == null)
        {
            return;
        }

        if (chaseTarget == target)
        {
            chaseTarget = null;
        }

        sensedTargets.Remove(target);
    }
}
