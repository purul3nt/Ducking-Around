using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Simple duck behaviour: HP, bobbing motion, and death animation toward the crocodile.
    /// </summary>
    public class Duck : MonoBehaviour
    {
        [Header("Stats")]
        public int maxHp = 3;
        public int hp;

        [Header("Bobbing")]
        [Tooltip("Vertical bobbing amplitude on the water surface.")]
        public float bobAmplitude = 0.04f;
        public float bobSpeed = 2f;

        [Header("Swimming")]
        [Tooltip("How fast the duck swims around the tub.")]
        public float swimSpeed = 0.4f;
        [Tooltip("Multiplier for tub radius to keep ducks away from the very edge.")]
        public float swimRadiusMultiplier = 0.8f;
        [Tooltip("How quickly the duck turns when steering back toward the center.")]
        public float turnSpeed = 2f;
        [Header("Crocodile avoidance")]
        [Tooltip("Center of the crocodile on the XZ plane. If GameManager exists, this is overridden at runtime.")]
        public Vector2 crocodileCenterXZ = new Vector2(0.2f, 0f);
        [Tooltip("Ducks steer away when within this distance. Kept outside this radius; use GameManager value if set.")]
        public float crocodileAvoidRadius = 1.5f;

        [Header("Death animation")]
        [Tooltip("Seconds for the duck to slide into the crocodile after death.")]
        public float deathDuration = 0.7f;

        [Header("Hit effect")]
        [Tooltip("How long the duck flashes when hit.")]
        public float hitFlashDuration = 0.1f;

        bool dying = false;
        Vector3 startPos;
        float deathStartTime;
        float swimRadius;
        Renderer cachedRenderer;
        Color baseColor;
        float hitFlashTimer;

        void Start()
        {
            hp = maxHp;
            startPos = transform.position;
            cachedRenderer = GetFlashRenderer();
            if (cachedRenderer != null)
            {
                baseColor = cachedRenderer.material.color;
            }

            // Determine swim radius from the GameManager tub radius if available.
            if (GameManager.Instance != null)
            {
                swimRadius = GameManager.Instance.tubRadius * swimRadiusMultiplier;
            }
            else
            {
                swimRadius = 4f;
            }

            //PickNewSwimTarget();
        }

        void Update()
        {
            if (dying)
            {
                float t = (Time.time - deathStartTime) / deathDuration;
                if (t >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }

                // Target: approximate crocodile position (tweak to match your scene).
                Vector3 target = new Vector3(0.2f, 0.4f, 0f);
                transform.position = Vector3.Lerp(startPos, target, t);

                float scale = 1f - 0.7f * t;
                transform.localScale = Vector3.one * scale;
                transform.Rotate(0f, 200f * Time.deltaTime, 0f);
            }
            else
            {
                Vector3 pos = transform.position;
                Vector2 crocCenter = crocodileCenterXZ;
                float avoidRadius = crocodileAvoidRadius;
                if (GameManager.Instance != null)
                {
                    crocCenter = GameManager.Instance.crocodileCenterXZ;
                    avoidRadius = Mathf.Max(avoidRadius, GameManager.Instance.crocodileSpawnAvoidRadius);
                }

                Vector3 crocPosXZ = new Vector3(crocCenter.x, 0f, crocCenter.y);
                Vector3 posXZ = new Vector3(pos.x, 0f, pos.z);
                Vector3 toCroc = crocPosXZ - posXZ;
                float distToCroc = toCroc.magnitude;
                Vector3 awayFromCroc = distToCroc > 0.001f ? -toCroc.normalized : new Vector3(1f, 0f, 0f);

                // Horizontal forward direction (XZ only).
                Vector3 forwardXZ = new Vector3(transform.forward.x, 0f, transform.forward.z);
                if (forwardXZ.sqrMagnitude < 0.0001f) forwardXZ = Vector3.forward;
                forwardXZ.Normalize();

                // 1) Crocodile avoidance: steer away when near or heading toward the croc (so we never swim into it).
                if (distToCroc < avoidRadius * 1.2f)
                {
                    float dot = Vector3.Dot(forwardXZ, awayFromCroc);
                    if (dot < 0.7f || distToCroc < avoidRadius)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(awayFromCroc, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * 1.5f * Time.deltaTime);
                        forwardXZ = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                    }
                }
                else if (distToCroc < avoidRadius * 1.8f)
                {
                    float dot = Vector3.Dot(forwardXZ, -awayFromCroc);
                    if (dot > 0.3f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(awayFromCroc, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                        forwardXZ = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                    }
                }

                // 2) Move forward (but not into the crocodile zone).
                Vector3 move = forwardXZ * (swimSpeed * Time.deltaTime);
                Vector3 newPosXZ = posXZ + move;
                float newDistToCroc = (crocPosXZ - newPosXZ).magnitude;
                if (newDistToCroc < avoidRadius && distToCroc > 0.001f)
                {
                    newPosXZ = crocPosXZ + awayFromCroc * avoidRadius;
                    move = newPosXZ - posXZ;
                }
                pos.x = newPosXZ.x;
                pos.z = newPosXZ.z;

                // 3) Tub edge: steer back toward center if we've left the swim radius.
                Vector3 center = new Vector3(0f, pos.y, 0f);
                Vector3 toCenterXZ = new Vector3(-pos.x, 0f, -pos.z);
                float distFromCenter = toCenterXZ.magnitude;
                if (distFromCenter > swimRadius && toCenterXZ.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toCenterXZ.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }

                // 4) Safety: never allow position inside crocodile radius (push out).
                posXZ = new Vector3(pos.x, 0f, pos.z);
                distToCroc = (crocPosXZ - posXZ).magnitude;
                if (distToCroc < avoidRadius && distToCroc > 0.001f)
                {
                    pos.x = crocPosXZ.x + awayFromCroc.x * avoidRadius;
                    pos.z = crocPosXZ.z + awayFromCroc.z * avoidRadius;
                }

                // Vertical bobbing.
                float y = startPos.y + Mathf.Sin(Time.time * bobSpeed + startPos.x) * bobAmplitude;
                pos.y = y;

                transform.position = pos;
                UpdateHitFlash();
            }
        }

        public void TakeDamage(int amount)
        {
            if (dying) return;

            hp -= amount;
            TriggerHitFlash();

            if (hp <= 0)
            {
                hp = 0;
                Die();
            }
        }

        protected virtual void Die()
        {
            if (dying) return;
            dying = true;
            deathStartTime = Time.time;
            startPos = transform.position;
            // Ensure we restore color when dying so it doesn't get stuck white.
            if (cachedRenderer != null)
            {
                cachedRenderer.material.color = baseColor;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDuckKilled(this);
            }
        }

        /// <summary>
        /// Override in subclasses (e.g. FireDuck) if the renderer is on a child instead of this object.
        /// </summary>
        protected virtual Renderer GetFlashRenderer()
        {
            return GetComponentInChildren<Renderer>();
        }

        void TriggerHitFlash()
        {
            if (cachedRenderer == null) return;

            hitFlashTimer = hitFlashDuration;
            Color flashColor = GetHitFlashColor();
            cachedRenderer.material.color = flashColor;
        }

        /// <summary>
        /// Override to customize hit flash color per duck type.
        /// </summary>
        protected virtual Color GetHitFlashColor()
        {
            return Color.white;
        }

        void UpdateHitFlash()
        {
            if (cachedRenderer == null || hitFlashDuration <= 0f) return;

            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                if (hitFlashTimer <= 0f)
                {
                    cachedRenderer.material.color = baseColor;
                }
            }
        }

    }
}

