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
            cachedRenderer = GetComponentInChildren<Renderer>();
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
                // Swim forward in the direction the duck is facing (XZ plane),
                // plus gentle bobbing up and down on the water surface.
                Vector3 pos = transform.position;

                // Horizontal forward direction (ignore any tilt).
                Vector3 forwardXZ = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                pos += forwardXZ * swimSpeed * Time.deltaTime;

                // If we get too close to the tub edge, steer back toward the center.
                Vector3 center = new Vector3(0f, pos.y, 0f);
                Vector3 toCenter = center - pos;
                Vector3 toCenterXZ = new Vector3(toCenter.x, 0f, toCenter.z);
                float distFromCenter = toCenterXZ.magnitude;
                if (distFromCenter > swimRadius && toCenterXZ.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toCenterXZ.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }

                // Gentle float motion on water surface (vertical bobbing).
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

        void Die()
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

        void TriggerHitFlash()
        {
            if (cachedRenderer == null) return;

            hitFlashTimer = hitFlashDuration;
            cachedRenderer.material.color = Color.white;
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

