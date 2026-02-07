using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Add to the main camera to enable screen shake. Call Shake() when you want a small camera shake.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Default (when triggered from code without params)")]
        [Tooltip("Position offset magnitude in world units.")]
        public float defaultIntensity = 0.08f;
        [Tooltip("Duration in seconds.")]
        public float defaultDuration = 0.2f;

        Vector3 _baseLocalPosition;
        float _shakeEndTime;
        float _shakeIntensity;
        float _shakeDuration;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _baseLocalPosition = transform.localPosition;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void LateUpdate()
        {
            if (Time.time < _shakeEndTime)
            {
                float remaining = (_shakeEndTime - Time.time) / _shakeDuration;
                float currentIntensity = _shakeIntensity * remaining;
                Vector3 offset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * currentIntensity;
                transform.localPosition = _baseLocalPosition + offset;
            }
            else
            {
                transform.localPosition = _baseLocalPosition;
            }
        }

        /// <summary>Trigger a camera shake. Uses default intensity and duration if not set on this component.</summary>
        public void Shake()
        {
            Shake(defaultIntensity, defaultDuration);
        }

        /// <summary>Trigger a camera shake with given intensity and duration.</summary>
        public void Shake(float intensity, float duration)
        {
            _baseLocalPosition = transform.localPosition;
            _shakeEndTime = Time.time + duration;
            _shakeIntensity = intensity;
            _shakeDuration = duration;
        }
    }
}
