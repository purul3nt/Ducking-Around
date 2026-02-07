using UnityEngine;
using TMPro;

namespace DuckingAround
{
    /// <summary>
    /// Single floating damage number: rises quickly then falls in an arc, fades out over lifetime, then destroys itself.
    /// Used by FloatingDamageNumbersManager; do not add manually unless you set up the required components.
    /// </summary>
    public class FloatingDamageNumber : MonoBehaviour
    {
        CanvasGroup _canvasGroup;
        float _birthTime;
        float _lifetime = 1f;
        float _velocityY;
        float _gravity;

        /// <summary>Call after the GameObject is set up (Canvas + TMP_Text + CanvasGroup). Sets text and arc parameters.</summary>
        public void Init(int amount, CanvasGroup canvasGroup, float initialVelocityY, float gravity)
        {
            _canvasGroup = canvasGroup;
            _birthTime = Time.time;
            _velocityY = initialVelocityY;
            _gravity = gravity;

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

            _velocityY -= _gravity * Time.deltaTime;
            transform.position += Vector3.up * (_velocityY * Time.deltaTime);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f - (elapsed / _lifetime);

            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}
