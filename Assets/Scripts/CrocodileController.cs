using System.Collections;
using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Attach to the crocodile in the centre of the scene.
    /// Makes the crocodile rotate to face the breaker (mouse position) on the XZ plane.
    /// Pulses slightly each time a duck is sucked in.
    /// </summary>
    public class CrocodileController : MonoBehaviour
    {
        [Tooltip("Assign the breaker transform. If unset, finds BreakerController in scene.")]
        public Transform breaker;

        [Tooltip("How quickly the crocodile rotates to face the breaker.")]
        public float turnSpeed = 5f;

        [Header("Suck-in pulse")]
        [Tooltip("Scale at peak of the pulse when a duck is sucked in.")]
        public float pulseScale = 1.06f;
        [Tooltip("Duration of the suck-in pulse in seconds.")]
        public float pulseDuration = 0.2f;

        Transform breakerTransform;
        Coroutine _pulseRoutine;

        void OnEnable()
        {
            GameManager.DuckSuckedIn += OnDuckSuckedIn;
        }

        void OnDisable()
        {
            GameManager.DuckSuckedIn -= OnDuckSuckedIn;
        }

        void Start()
        {
            if (breaker != null)
                breakerTransform = breaker;
            else
            {
                var bc = FindObjectOfType<BreakerController>();
                if (bc != null)
                    breakerTransform = bc.transform;
            }
        }

        void OnDuckSuckedIn()
        {
            if (_pulseRoutine != null)
                StopCoroutine(_pulseRoutine);
            _pulseRoutine = StartCoroutine(PulseRoutine());
        }

        IEnumerator PulseRoutine()
        {
            Vector3 baseScale = transform.localScale;
            float half = pulseDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float tNorm = elapsed / half;
                float s = Mathf.Lerp(1f, pulseScale, tNorm);
                transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z * s);
                yield return null;
            }
            elapsed = half;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float tNorm = (elapsed - half) / half;
                float s = Mathf.Lerp(pulseScale, 1f, tNorm);
                transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z * s);
                yield return null;
            }

            transform.localScale = baseScale;
            _pulseRoutine = null;
        }

        void Update()
        {
            if (breakerTransform == null) return;
            if (GameManager.Instance != null && !GameManager.Instance.sessionActive) return;

            Vector3 crocPos = transform.position;
            Vector3 breakerPos = breakerTransform.position;

            Vector3 lookDir = breakerPos - crocPos;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude < 0.0001f) return;

            lookDir.Normalize();
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }
}
