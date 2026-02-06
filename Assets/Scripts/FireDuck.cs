using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// A special duck that explodes on death, damaging nearby ducks.
    /// Inherits normal duck movement / behaviour, including hit flash effects.
    /// </summary>
    public class FireDuck : Duck
    {
        [Header("Explosion")]
        [Tooltip("Damage dealt to other ducks within the explosion range when this duck dies.")]
        public int explosionDamage = 1;

        [Tooltip("Explosion radius in world units on the water plane.")]
        public float explosionRadius = 1.5f;

        [Tooltip("Optional particle system prefab to play when this duck explodes.")]
        public ParticleSystem explosionFxPrefab;

        /// <summary>
        /// FireDuck's mesh/renderer is on a child; use that for hit flash and death color restore.
        /// </summary>
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

        /// <summary>
        /// Use a bright pink hit flash color for FireDuck.
        /// </summary>
        protected override Color GetHitFlashColor()
        {
            // Bright pink (RGB 1, 0, 1).
            return Color.magenta;
        }

        protected override void Die()
        {
            // Trigger explosion damage before running the normal death flow.
            Explode();
            base.Die();
        }

        void Explode()
        {
            if (GameManager.Instance == null) return;

            // Visual effect.
            if (explosionFxPrefab != null)
            {
                // Scale FX so its radius roughly matches the explosionRadius.
                float scale = explosionRadius;
                var fx = Instantiate(explosionFxPrefab, transform.position, Quaternion.identity);
                fx.transform.localScale = new Vector3(scale, scale, scale);
                Destroy(fx.gameObject, 1f);
            }

            var ducksSnapshot = GameManager.Instance.Ducks.ToArray();

            Vector3 center = transform.position;
            center.y = 0f;

            foreach (var duck in ducksSnapshot)
            {
                if (duck == null || duck == this) continue;

                Vector3 duckPos = duck.transform.position;
                duckPos.y = 0f;

                float dist = Vector3.Distance(center, duckPos);
                if (dist <= explosionRadius)
                {
                    duck.TakeDamage(explosionDamage);
                }
            }
        }
    }
}

