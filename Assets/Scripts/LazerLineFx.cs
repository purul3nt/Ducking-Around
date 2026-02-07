using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Animates a lazer LineRenderer: starts very thin, quickly grows to target width, then shrinks back to thin.
    /// Add to the lazer line prefab, or it will be added at runtime by LazerDuck.
    /// </summary>
    public class LazerLineFx : MonoBehaviour
    {
        const float MinWidth = 0.001f;

        public LineRenderer lineRenderer;
        public float duration = 0.3f;
        public float targetWidth = 0.5f;

        float elapsed;

        void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
        }

        void Update()
        {
            if (lineRenderer == null) return;

            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            // 0–20%: thin -> full, 20–80%: full, 80–100%: full -> thin
            float width;
            if (t < 0.2f)
                width = Mathf.Lerp(MinWidth, targetWidth, t / 0.2f);
            else if (t < 0.8f)
                width = targetWidth;
            else
                width = Mathf.Lerp(targetWidth, MinWidth, (t - 0.8f) / 0.2f);

            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }
}
