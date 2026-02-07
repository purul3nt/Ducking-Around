using UnityEngine;
using TMPro;

namespace DuckingAround
{
    /// <summary>
    /// Spawns floating damage numbers in the world when ducks (or others) take damage.
    /// Creates world-space Canvas + TMP text at runtime; no prefab required.
    /// Optional: add to scene and assign a parent transform to keep hierarchy tidy.
    /// </summary>
    public class FloatingDamageNumbersManager : MonoBehaviour
    {
        public static FloatingDamageNumbersManager Instance { get; private set; }

        [Header("Appearance")]
        [Tooltip("Colour of the damage number text.")]
        public Color damageNumberColor = Color.white;
        [Tooltip("Height in world units above the hit point to spawn the number.")]
        public float spawnHeightOffset = 0.2f;
        [Tooltip("World scale of the damage number (small = 0.1–0.2).")]
        public float worldScale = 0.15f;
        [Tooltip("Font size for the damage text.")]
        public int fontSize = 48;

        [Header("Arc animation")]
        [Tooltip("Initial upward speed (world units per second) so the number rises quickly from the duck.")]
        public float riseSpeed = 1.5f;
        [Tooltip("Gravity (world units per second²) applied so the number falls in an arc after rising.")]
        public float arcGravity = 4f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Spawn a floating damage number at the given world position. It will disappear after 1 second.</summary>
        public void ShowDamage(int amount, Vector3 worldPosition)
        {
            Vector3 spawnPos = worldPosition + Vector3.up * spawnHeightOffset;

            var root = new GameObject("FloatingDamage");
            root.transform.position = spawnPos;
            root.transform.localScale = Vector3.one * worldScale;
            root.transform.SetParent(transform);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var cg = root.AddComponent<CanvasGroup>();

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 1f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, false);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = amount.ToString();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = damageNumberColor;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var fdn = root.AddComponent<FloatingDamageNumber>();
            fdn.Init(amount, cg, riseSpeed, arcGravity);
        }
    }
}
