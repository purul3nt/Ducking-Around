using UnityEngine;
using TMPro;

namespace DuckingAround
{
    /// <summary>
    /// Single floating damage number: floats upward and fades out over lifetime, then destroys itself.
    /// Used by FloatingDamageNumbersManager; do not add manually unless you set up the required components.
    /// </summary>
    public class FloatingDamageNumber : MonoBehaviour
    {
        [Tooltip("World-space units per second the number moves upward.")]
        public float floatSpeed = 0.5f;

        CanvasGroup _canvasGroup;
        float _birthTime;
        float _lifetime = 1f;

        /// <summary>Call after the GameObject is set up (Canvas + TMP_Text + CanvasGroup). Sets text and starts timer.</summary>
        public void Init(int amount, CanvasGroup canvasGroup)
        {
            _canvasGroup = canvasGroup;
            _birthTime = Time.time;

            var tmp = GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = amount.ToString();
        }

        void Update()
        {
            float elapsed = Time.time - _birthTime;
            if (elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f - (elapsed / _lifetime);

            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}
