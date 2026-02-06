using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Optional component for the Electro Duck lightning bolt FX prefab.
    /// After Instantiate, call SetEndpoints(from, to). The bolt will be drawn (or the
    /// object positioned) and this GameObject will destroy itself after duration.
    /// Add a LineRenderer to the same GameObject for a visible bolt; otherwise the
    /// prefab can be a particle system or other FX that plays at the start position.
    /// </summary>
    public class LightningBoltFx : MonoBehaviour
    {
        [Tooltip("How long the bolt stays visible before being destroyed.")]
        public float duration = 0.25f;

        [Header("Bolt shape")]
        [Tooltip("Number of segments along the bolt (more = bendier lightning).")]
        public int segments = 8;
        [Tooltip("How far each segment can bend sideways (world units).")]
        public float jagAmount = 0.15f;

        LineRenderer lineRenderer;
        float spawnTime;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            spawnTime = Time.time;
        }

        /// <summary>
        /// Call after instantiating the prefab to draw from one position to another.
        /// </summary>
        public void SetEndpoints(Vector3 from, Vector3 to)
        {
            if (lineRenderer != null)
            {
                Vector3 dir = to - from;
                float length = dir.magnitude;
                if (length < 0.001f)
                {
                    lineRenderer.useWorldSpace = true;
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, from);
                    lineRenderer.SetPosition(1, to);
                    return;
                }

                dir /= length;
                // Perpendicular vectors for jitter (avoid zero cross when dir is vertical).
                Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.01f)
                    right = Vector3.Cross(dir, Vector3.forward).normalized;
                Vector3 up = Vector3.Cross(right, dir).normalized;

                int pointCount = Mathf.Max(2, segments + 1);
                lineRenderer.useWorldSpace = true;
                lineRenderer.positionCount = pointCount;

                for (int i = 0; i < pointCount; i++)
                {
                    float t = i / (float)(pointCount - 1);
                    Vector3 basePos = from + dir * (length * t);

                    // Random bend perpendicular to the bolt direction.
                    if (i > 0 && i < pointCount - 1)
                    {
                        float j = jagAmount * (1f - Mathf.Abs(t - 0.5f) * 2f); // Slightly less jag near endpoints.
                        basePos += right * Random.Range(-j, j) + up * Random.Range(-j, j);
                    }

                    lineRenderer.SetPosition(i, basePos);
                }
            }
            else
            {
                transform.position = from;
                transform.LookAt(to);
            }
        }

        void Update()
        {
            if (Time.time - spawnTime >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
