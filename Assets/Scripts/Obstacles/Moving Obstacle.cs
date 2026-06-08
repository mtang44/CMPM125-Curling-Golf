using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    private MovingObstaclePath path;
    private float phaseOffset01;

    public void Configure(MovingObstaclePath pathSource, float phaseOffset)
    {
        path = pathSource;
        phaseOffset01 = Mathf.Repeat(phaseOffset, 1f);

        if (path != null)
        {
            float progress = path.GetProgress(phaseOffset01);
            transform.position = path.GetPositionAtProgress(progress);
        }
    }

    private void Update()
    {
        if (path == null)
        {
            return;
        }

        float progress = path.GetProgress(phaseOffset01);
        transform.position = path.GetPositionAtProgress(progress);

        if (path.FaceDirection)
        {
            Vector3 tangent = path.GetTangentAtProgress(progress);
            tangent.y = 0f;

            if (tangent.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
            }
        }
    }
}
