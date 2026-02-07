using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Controls the breaker area: projects the mouse onto the water plane and
    /// periodically damages any ducks within a given radius.
    /// Attach this to an empty GameObject (optionally with a visible ring mesh).
    /// </summary>
    public class BreakerController : MonoBehaviour
    {
        [Tooltip("Seconds between damage ticks while the breaker overlaps ducks.")]
        public float damageTickInterval = 0.3f;

        [Header("Position")]
        [Tooltip("Y height of the water surface used for mouse projection.")]
        public float waterHeight = 0.35f;
        [Tooltip("Fixed Y height for the breaker so it always hovers above ducks.")]
        public float breakerHeight = 0.7f;

        [Header("Bounce Animation")]
        [Tooltip("How much to scale the breaker on each damage tick. 1.1 = +10%.")]
        public float bounceScaleMultiplier = 1.1f;
        [Tooltip("How long the bounce animation lasts, in seconds.")]
        public float bounceDuration = 0.1f;

        [Header("Visual")]
        [Tooltip("Child transform used to visually represent the breaker radius (e.g. a sphere).")]
        public Transform visual;

        float damageTimer;
        Camera mainCamera;
        float bounceTimer;
        float bounceScaleFactor = 1f;
        float initialRadius = 1f;
        float initialVisualScale = 1f;

        void Start()
        {
            mainCamera = Camera.main;
            damageTimer = damageTickInterval;

            if (GameManager.Instance != null)
            {
                initialRadius = GameManager.Instance.breakerRadius;
            }

            // Auto-assign the first child as visual if not set explicitly.
            if (visual == null && transform.childCount > 0)
            {
                visual = transform.GetChild(0);
            }

            if (visual != null)
            {
                initialVisualScale = visual.localScale.x;
            }
        }

        void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.sessionActive) return;

            UpdatePositionFromMouse();

            // Faster breaker speeds multiply how quickly the timer counts down.
            float speed = GameManager.Instance != null ? GameManager.Instance.breakerSpeedMultiplier : 1f;
            damageTimer -= Time.deltaTime * speed;
            if (damageTimer <= 0f)
            {
                damageTimer = damageTickInterval;
                if (ApplyDamage())
                {
                    if (MusicManager.Instance != null)
                        MusicManager.Instance.PlayBreakerHitSfx();
                }
                TriggerBounce();
            }

            UpdateBounce();
            UpdateVisualSize();
        }

        void UpdatePositionFromMouse()
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            // Project the mouse onto the water surface plane at waterHeight so
            // the visual cursor alignment with ducks matches better for a
            // perspective top-down camera.
            Plane plane = new Plane(Vector3.up, new Vector3(0f, waterHeight, 0f));

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                // Keep X/Z from the water-plane hit, but force Y to a fixed
                // height so the breaker always renders above ducks.
                transform.position = new Vector3(hit.x, breakerHeight, hit.z);
            }
        }

        /// <returns>True if at least one duck was hit this pulse.</returns>
        bool ApplyDamage()
        {
            if (GameManager.Instance == null) return false;

            // Slightly pad the logical radius so the visual breaker ring feels
            // generous when overlapping ducks.
            float radius = GameManager.Instance.breakerRadius * 1.1f;

            // Copy the list so it is safe if ducks are added/removed
            // (e.g. via OnDuckKilled/SpawnDuck) while we are iterating.
            var ducksSnapshot = GameManager.Instance.Ducks.ToArray();
            bool hitAny = false;

            foreach (var duck in ducksSnapshot)
            {
                if (duck == null) continue;

                Vector3 breakerPos = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 duckPos = new Vector3(duck.transform.position.x, 0f, duck.transform.position.z);

                float dist = Vector3.Distance(breakerPos, duckPos);
                if (dist <= radius)
                {
                    hitAny = true;
                    int damage = GameManager.Instance.GetBreakerDamage();
                    duck.TakeDamage(damage);
                }
            }

            return hitAny;
        }

        void TriggerBounce()
        {
            bounceTimer = bounceDuration;
        }

        void UpdateBounce()
        {
            if (bounceDuration <= 0f)
            {
                bounceScaleFactor = 1f;
                return;
            }

            if (bounceTimer > 0f)
            {
                bounceTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(bounceTimer / bounceDuration);
                bounceScaleFactor = Mathf.Lerp(bounceScaleMultiplier, 1f, t);
            }
            else
            {
                bounceScaleFactor = 1f;
            }
        }

        void UpdateVisualSize()
        {
            if (visual == null || GameManager.Instance == null) return;

            float radius = GameManager.Instance.breakerRadius;
            if (initialRadius <= 0f) initialRadius = radius;

            // Scale the visual so its radius matches the logical breaker radius,
            // then apply the bounce scale on top.
            float radiusScale = radius / initialRadius;
            float finalScale = initialVisualScale * radiusScale * bounceScaleFactor;
            visual.localScale = new Vector3(finalScale, finalScale, finalScale);
        }
    }
}

