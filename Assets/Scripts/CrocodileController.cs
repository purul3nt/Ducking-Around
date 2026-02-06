using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Attach to the crocodile in the centre of the scene.
    /// Makes the crocodile rotate to face the breaker (mouse position) on the XZ plane.
    /// </summary>
    public class CrocodileController : MonoBehaviour
    {
        [Tooltip("Assign the breaker transform. If unset, finds BreakerController in scene.")]
        public Transform breaker;

        [Tooltip("How quickly the crocodile rotates to face the breaker.")]
        public float turnSpeed = 5f;

        Transform breakerTransform;

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
