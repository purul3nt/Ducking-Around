using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// A duck that, when killed, fires a lazer across the full screen and damages any duck hit by it.
    /// Same hierarchy as Fire Duck: mesh/renderer on a child.
    /// </summary>
    public class LazerDuck : Duck
    {
        [Header("Lazer")]
        [Tooltip("Width of the lazer in world units (ducks within this distance of the line are hit).")]
        public float lazerWidth = 0.5f;

        [Tooltip("Damage dealt to each duck hit by the lazer.")]
        public int lazerDamage = 1;

        [Tooltip("Optional: GameObject with a LineRenderer used to draw the lazer. Will be instantiated, configured, and destroyed after a short time.")]
        public GameObject lazerLinePrefab;

        [Tooltip("How long the lazer line stays visible.")]
        public float lazerDuration = 0.3f;

        bool hasFiredLazer;

        protected override Renderer GetFlashRenderer()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var r = transform.GetChild(i).GetComponentInChildren<Renderer>();
                if (r != null)
                    return r;
            }
            return null;
        }

        protected override Color GetHitFlashColor()
        {
            return Color.red;
        }

        protected override void Die()
        {
            if (!hasFiredLazer)
            {
                hasFiredLazer = true;
                FireLazer();
            }
            base.Die();
        }

        void FireLazer()
        {
            if (GameManager.Instance == null || lazerDamage <= 0) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // Line passes through the lazer duck's position at a random angle, long enough to cover the screen.
            Vector3 origin = transform.position;
            float y = origin.y;
            var plane = new Plane(Vector3.up, new Vector3(0, y, 0));

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            // Half-length: ensure line spans the visible view at this height (distance to farthest viewport corner).
            float halfLength = 50f;
            Vector3[] corners = new Vector3[4];
            corners[0] = ViewportToWorldOnPlane(cam, plane, 0f, 0f);
            corners[1] = ViewportToWorldOnPlane(cam, plane, 1f, 0f);
            corners[2] = ViewportToWorldOnPlane(cam, plane, 0f, 1f);
            corners[3] = ViewportToWorldOnPlane(cam, plane, 1f, 1f);
            for (int i = 0; i < 4; i++)
            {
                float d = Vector3.Distance(origin, corners[i]);
                if (d > halfLength) halfLength = d;
            }

            Vector3 start = origin - direction * halfLength;
            Vector3 end = origin + direction * halfLength;
            float halfWidth = lazerWidth * 0.5f;

            // Damage any duck whose position is within lazerWidth/2 of the line segment.
            var ducksSnapshot = GameManager.Instance.Ducks.ToArray();
            foreach (var duck in ducksSnapshot)
            {
                if (duck == null || duck == this) continue;
                float dist = PointToSegmentDistance(duck.transform.position, start, end);
                if (dist <= halfWidth)
                    duck.TakeDamage(lazerDamage);
            }

            // Visual: LineRenderer from start to end.
            if (lazerLinePrefab != null)
            {
                GameObject go = Instantiate(lazerLinePrefab, start, Quaternion.identity);
                LineRenderer lr = go.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.useWorldSpace = true;
                    lr.positionCount = 2;
                    lr.SetPosition(0, start);
                    lr.SetPosition(1, end);
                    lr.startWidth = lazerWidth;
                    lr.endWidth = lazerWidth;
                }
                Destroy(go, lazerDuration);
            }
        }

        static Vector3 ViewportToWorldOnPlane(Camera cam, Plane plane, float vx, float vy)
        {
            Ray r = cam.ViewportPointToRay(new Vector3(vx, vy, 0f));
            plane.Raycast(r, out float t);
            return r.GetPoint(t);
        }

        static float PointToSegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f)
                return Vector3.Distance(p, a);
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / lenSq);
            Vector3 closest = a + t * ab;
            return Vector3.Distance(p, closest);
        }
    }
}
