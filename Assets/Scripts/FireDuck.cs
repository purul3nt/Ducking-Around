using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// A special duck that explodes on death, damaging nearby ducks.
    /// Inherits normal duck movement / behaviour.
    /// </summary>
    public class FireDuck : Duck
    {
        [Header("Explosion")]
        [Tooltip("Damage dealt to other ducks within the explosion range when this duck dies.")]
        public int explosionDamage = 1;

        [Tooltip("Explosion radius in world units on the water plane.")]
        public float explosionRadius = 1.5f;

        protected override void Die()
        {
            // Trigger explosion damage before running the normal death flow.
            Explode();
            base.Die();
        }

        void Explode()
        {
            if (GameManager.Instance == null) return;

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

