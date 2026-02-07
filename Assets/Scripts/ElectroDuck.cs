using System.Linq;
using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// A duck that, when killed, fires chain lightning to nearby ducks.
    /// </summary>
    public class ElectroDuck : Duck
    {
        [Header("Chain Lightning")]
        [Tooltip("How many other ducks the lightning can jump to.")]
        public int chainCount = 3;

        [Tooltip("Damage dealt to each duck hit by the lightning.")]
        public int chainDamage = 1;

        [Tooltip("Optional FX prefab to show a bolt to each chained duck. Add LightningBoltFx + LineRenderer for a visible bolt.")]
        public GameObject lightningBoltFxPrefab;

        bool hasChained;

        /// <summary>
        /// Electro Duck mesh is on a child; use that for hit flash.
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

        protected override void Die()
        {
            if (!hasChained)
            {
                hasChained = true;
                DoChainLightning();
            }
            base.Die();
        }

        void DoChainLightning()
        {
            if (GameManager.Instance == null || chainCount <= 0 || chainDamage <= 0) return;

            var ducksSnapshot = GameManager.Instance.Ducks.ToArray();

            Vector3 origin = transform.position;
            origin.y = 0f;

            // Collect valid targets (other ducks).
            var candidates = ducksSnapshot
                .Where(d => d != null && d != this)
                .Select(d => new
                {
                    duck = d,
                    distSqr = (new Vector3(d.transform.position.x, 0f, d.transform.position.z) - origin).sqrMagnitude
                })
                .OrderBy(x => x.distSqr)
                .ToList();

            int targets = Mathf.Min(chainCount, candidates.Count);
            Vector3 originPos = transform.position;

            for (int i = 0; i < targets; i++)
            {
                Duck target = candidates[i].duck;
                Vector3 targetPos = target.transform.position;

                if (lightningBoltFxPrefab != null)
                {
                    GameObject fx = Instantiate(lightningBoltFxPrefab, originPos, Quaternion.identity);
                    var bolt = fx.GetComponent<LightningBoltFx>();
                    if (bolt != null)
                        bolt.SetEndpoints(originPos, targetPos);
                    Destroy(fx, 0.5f);
                }

                target.TakeDamage(chainDamage);
            }
        }
    }
}

