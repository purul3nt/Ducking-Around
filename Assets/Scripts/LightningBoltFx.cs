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
                // Slight jag for a lightning look.
                Vector3 mid = (from + to) * 0.5f;
                mid += new Vector3(
                    Random.Range(-0.1f, 0.1f),
                    Random.Range(-0.05f, 0.05f),
                    Random.Range(-0.1f, 0.1f)
                );

                lineRenderer.useWorldSpace = true;
                lineRenderer.positionCount = 3;
                lineRenderer.SetPosition(0, from);
                lineRenderer.SetPosition(1, mid);
                lineRenderer.SetPosition(2, to);
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
