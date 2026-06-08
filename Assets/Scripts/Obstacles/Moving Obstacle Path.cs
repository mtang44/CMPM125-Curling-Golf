using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class MovingObstaclePath : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private bool loopPath = true;
    [SerializeField] private int distanceSamples = 200;

    [Header("Motion")]
    [SerializeField] private float cycleDuration = 6f;
    [SerializeField] private bool faceDirection = true;

    [Header("Spawn")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private int obstacleCount = 4;
    [SerializeField] private bool spawnOnStart = true;

    private readonly List<MovingObstacle> spawnedObstacles = new List<MovingObstacle>();
    private readonly List<float> distanceTable = new List<float>();
    private readonly List<float> tTable = new List<float>();
    private float totalPathLength;

    public bool FaceDirection => faceDirection;

    private void Awake()
    {
        RebuildPathCache();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnObstacles();
        }
    }

    private void OnValidate()
    {
        obstacleCount = Mathf.Max(1, obstacleCount);
        cycleDuration = Mathf.Max(0.01f, cycleDuration);
        distanceSamples = Mathf.Max(8, distanceSamples);
    }

    [ContextMenu("Spawn Obstacles")]
    public void SpawnObstacles()
    {
        RebuildPathCache();

        if (obstaclePrefab == null || !HasValidPath())
        {
            return;
        }

        ClearSpawnedObstacles();

        for (int i = 0; i < obstacleCount; i++)
        {
            float phaseOffset = (float)i / obstacleCount;
            float progress = GetProgress(phaseOffset);
            Vector3 spawnPosition = GetPositionAtProgress(progress);

            GameObject instance = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, transform);
            MovingObstacle mover = instance.GetComponent<MovingObstacle>();
            if (mover == null)
            {
                mover = instance.AddComponent<MovingObstacle>();
            }

            mover.Configure(this, phaseOffset);
            spawnedObstacles.Add(mover);
        }
    }

    [ContextMenu("Clear Spawned Obstacles")]
    public void ClearSpawnedObstacles()
    {
        for (int i = spawnedObstacles.Count - 1; i >= 0; i--)
        {
            if (spawnedObstacles[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(spawnedObstacles[i].gameObject);
                }
                else
                {
                    DestroyImmediate(spawnedObstacles[i].gameObject);
                }
            }
        }

        spawnedObstacles.Clear();
    }

    public float GetProgress(float phaseOffset01)
    {
        float normalizedTime = Time.time / cycleDuration + phaseOffset01;
        return loopPath ? Mathf.Repeat(normalizedTime, 1f) : Mathf.PingPong(normalizedTime, 1f);
    }

    public Vector3 GetPositionAtProgress(float progress01)
    {
        if (!HasValidPath())
        {
            return transform.position;
        }

        float clampedProgress = Mathf.Clamp01(progress01);
        if (loopPath)
        {
            clampedProgress = Mathf.Repeat(progress01, 1f);
        }

        float targetDistance = clampedProgress * totalPathLength;
        float splineT = GetTAtDistance(targetDistance);
        return EvaluatePosition(splineT);
    }

    public Vector3 GetTangentAtProgress(float progress01)
    {
        if (!HasValidPath())
        {
            return Vector3.forward;
        }

        float clampedProgress = Mathf.Clamp01(progress01);
        if (loopPath)
        {
            clampedProgress = Mathf.Repeat(progress01, 1f);
        }

        float targetDistance = clampedProgress * totalPathLength;
        float splineT = GetTAtDistance(targetDistance);
        return EvaluateTangent(splineT);
    }

    private void RebuildPathCache()
    {
        distanceTable.Clear();
        tTable.Clear();
        totalPathLength = 0f;

        if (!HasValidPath())
        {
            return;
        }

        Vector3 previous = EvaluatePosition(0f);
        distanceTable.Add(0f);
        tTable.Add(0f);

        for (int i = 1; i <= distanceSamples; i++)
        {
            float t = (float)i / distanceSamples;
            Vector3 current = EvaluatePosition(t);
            totalPathLength += Vector3.Distance(previous, current);
            previous = current;

            distanceTable.Add(totalPathLength);
            tTable.Add(t);
        }

        if (totalPathLength <= 0.0001f)
        {
            totalPathLength = 0.0001f;
        }
    }

    private float GetTAtDistance(float targetDistance)
    {
        if (distanceTable.Count < 2)
        {
            return 0f;
        }

        float clampedDistance = Mathf.Clamp(targetDistance, 0f, totalPathLength);

        for (int i = 1; i < distanceTable.Count; i++)
        {
            float prevDistance = distanceTable[i - 1];
            float nextDistance = distanceTable[i];
            if (clampedDistance <= nextDistance)
            {
                float segmentLength = nextDistance - prevDistance;
                float segmentT = segmentLength <= 0.0001f ? 0f : (clampedDistance - prevDistance) / segmentLength;
                return Mathf.Lerp(tTable[i - 1], tTable[i], segmentT);
            }
        }

        return 1f;
    }

    private bool HasValidPath()
    {
        return splineContainer != null && splineContainer.Spline != null;
    }

    private Vector3 EvaluatePosition(float t)
    {
        Vector3 local = (Vector3)SplineUtility.EvaluatePosition(splineContainer.Spline, Mathf.Clamp01(t));
        return splineContainer.transform.TransformPoint(local);
    }

    private Vector3 EvaluateTangent(float t)
    {
        Vector3 localTangent = (Vector3)SplineUtility.EvaluateTangent(splineContainer.Spline, Mathf.Clamp01(t));
        return splineContainer.transform.TransformDirection(localTangent);
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasValidPath())
        {
            return;
        }

        Gizmos.color = Color.cyan;
        const int drawSegments = 50;
        Vector3 previous = EvaluatePosition(0f);

        for (int i = 1; i <= drawSegments; i++)
        {
            float t = (float)i / drawSegments;
            Vector3 current = EvaluatePosition(t);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }

        if (loopPath)
        {
            Gizmos.DrawLine(EvaluatePosition(1f), EvaluatePosition(0f));
        }
    }
}
