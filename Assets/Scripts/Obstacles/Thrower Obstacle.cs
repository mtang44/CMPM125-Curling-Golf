using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

public class ThrowerObstacle : MonoBehaviour
{
    private enum EnemyState
    {
        Patrol,
        Chase,
        Carrying,
        Cooldown
    }

    [Header("Patrol")]
    [SerializeField] private SplineContainer patrolSpline;
    [SerializeField] private bool loopPatrol = true;
    [SerializeField] private float patrolCycleDuration = 8f;
    [SerializeField] private int splineDistanceSamples = 120;

    [Header("Detection")]
    [SerializeField] private Transform eyePoint;
    [SerializeField] private float sightDistance = 14f;
    [SerializeField] private float fieldOfView = 100f;
    [SerializeField] private LayerMask lineOfSightBlockers = ~0;

    [Header("Movement")]
    [SerializeField] private bool useNavMesh = true;
    [SerializeField] private float patrolMoveSpeed = 4f;
    [SerializeField] private float chaseMoveSpeed = 7f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float navRepathInterval = 0.1f;

    [Header("Grab And Throw")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float grabDistance = 1.4f;
    [SerializeField] private float carryDuration = 1.2f;
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
    private NavMeshAgent navAgent;
    private float nextRepathTime;

    private readonly System.Collections.Generic.List<float> distanceTable = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<float> tTable = new System.Collections.Generic.List<float>();
    private float patrolLength;

    private void Awake()
    {
        patrolCycleDuration = Mathf.Max(0.01f, patrolCycleDuration);
        splineDistanceSamples = Mathf.Max(8, splineDistanceSamples);
        navRepathInterval = Mathf.Max(0.02f, navRepathInterval);
        if (eyePoint == null)
        {
            eyePoint = transform;
        }

        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.updateRotation = false;
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

            case EnemyState.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    private void OnValidate()
    {
        patrolCycleDuration = Mathf.Max(0.01f, patrolCycleDuration);
        splineDistanceSamples = Mathf.Max(8, splineDistanceSamples);
        sightDistance = Mathf.Max(0.1f, sightDistance);
        fieldOfView = Mathf.Clamp(fieldOfView, 1f, 360f);
        grabDistance = Mathf.Max(0.1f, grabDistance);
        carryDuration = Mathf.Max(0.1f, carryDuration);
        throwSpeed = Mathf.Max(0.1f, throwSpeed);
        patrolMoveSpeed = Mathf.Max(0.1f, patrolMoveSpeed);
        chaseMoveSpeed = Mathf.Max(0.1f, chaseMoveSpeed);
        turnSpeed = Mathf.Max(0.1f, turnSpeed);
        postThrowCooldown = Mathf.Max(0f, postThrowCooldown);
        navRepathInterval = Mathf.Max(0.02f, navRepathInterval);

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
        StoneController[] stones = FindObjectsByType<StoneController>(FindObjectsSortMode.None);
        StoneController bestVisible = null;
        float bestDistanceSq = float.MaxValue;

        for (int i = 0; i < stones.Length; i++)
        {
            StoneController candidate = stones[i];
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            Rigidbody body = candidate.GetComponent<Rigidbody>();
            if (body == null)
            {
                continue;
            }

            Vector3 toTarget = body.worldCenterOfMass - eyePoint.position;
            float distanceSq = toTarget.sqrMagnitude;
            if (distanceSq > sightDistance * sightDistance)
            {
                continue;
            }

            float angle = Vector3.Angle(transform.forward, toTarget);
            if (angle > fieldOfView * 0.5f)
            {
                continue;
            }

            if (!HasLineOfSight(candidate, body.worldCenterOfMass))
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
            ThrowCarriedStone();
        }
    }

    private void ThrowCarriedStone()
    {
        if (carriedStoneController == null || carriedStoneBody == null)
        {
            state = EnemyState.Patrol;
            return;
        }

        Transform directionSource = throwDirectionReference != null ? throwDirectionReference : transform;
        Vector3 throwDirection = directionSource.forward;
        throwDirection.y = 0f;
        if (throwDirection.sqrMagnitude < 0.0001f)
        {
            throwDirection = transform.forward;
            throwDirection.y = 0f;
        }

        throwDirection = throwDirection.normalized;
        Vector3 launchVelocity = throwDirection * throwSpeed + Vector3.up * throwUpwardBoost;

        carriedStoneController.transform.SetParent(carriedOriginalParent, true);
        carriedStoneBody.isKinematic = false;
        carriedStoneBody.linearVelocity = launchVelocity;
        carriedStoneController.enabled = true;

        carriedStoneController = null;
        carriedStoneBody = null;
        carriedOriginalParent = null;

        cooldownTimer = postThrowCooldown;
        state = EnemyState.Cooldown;
    }

    private void UpdateCooldown()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            state = EnemyState.Patrol;
        }
    }

    private bool HasLineOfSight(StoneController candidate, Vector3 targetPoint)
    {
        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position;
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.0001f)
        {
            return true;
        }

        Vector3 direction = toTarget / distance;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, lineOfSightBlockers, QueryTriggerInteraction.Ignore))
        {
            StoneController hitStone = hit.collider.GetComponentInParent<StoneController>();
            return hitStone == candidate;
        }

        return true;
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
        Transform origin = eyePoint != null ? eyePoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin.position, sightDistance);

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
}
