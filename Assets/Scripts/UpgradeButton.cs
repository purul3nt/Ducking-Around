using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DuckingAround
{
    /// <summary>
    /// Controls a single upgrade slot in the UI.
    /// Shows cost, lock/purchased state, and enables/disables the button
    /// based on gold and dependency. Title/description/cost are only
    /// visible while the mouse is hovering the button.
    /// </summary>
    public class UpgradeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Config")]
        [Tooltip("Upgrade code as defined in GameManager (e.g. U1, U2...).")]
        public string upgradeCode;

        [Header("UI")]
        public TMP_Text titleText;
        public TMP_Text descriptionText;
        public TMP_Text costText;
        [Tooltip("Optional: Image to show the upgrade icon. Sprite is set from GameManager's upgrade icons list.")]
        public Image iconImage;
        public GameObject lockedOverlay;
        public GameObject purchasedOverlay;

        [Header("Hover")]
        [Tooltip("Optional container that holds only description and cost; shown on hover. Title is always visible.")]
        public GameObject hoverPanel;

        [Header("Highlight")]
        [Tooltip("Button image tint when purchasable (available + can afford).")]
        public Color purchasableHighlightColor = new Color(0.45f, 0.95f, 0.45f);
        [Tooltip("Button image tint when locked or unaffordable.")]
        public Color lockedTintColor = new Color(0.75f, 0.75f, 0.75f);

        Button button;

        void Awake()
        {
            button = GetComponentInChildren<Button>(true);
            // Title always visible; hide only cost and description until hover.
            SetHoverVisible(false);
        }

        /// <summary>
        /// Called from UIManager.UpdateHUD() to keep this slot in sync.
        /// </summary>
        public void Refresh()
        {
            if (GameManager.Instance == null || string.IsNullOrEmpty(upgradeCode)) return;

            var gm = GameManager.Instance;

            bool purchased = gm.IsUpgradePurchased(upgradeCode);
            int cost = gm.GetUpgradeCost(upgradeCode);
            bool available = gm.IsUpgradeAvailable(upgradeCode);
            bool canAfford = gm.gold >= cost;

            UpdateTexts(gm);

            if (costText != null)
            {
                costText.text = purchased ? "✓" : cost.ToString();
            }

            if (button != null)
            {
                button.interactable = available && canAfford;
            }

            if (lockedOverlay != null)
            {
                // Locked if dependency not met or already purchased.
                lockedOverlay.SetActive(!available && !purchased);
            }

            if (purchasedOverlay != null)
            {
                purchasedOverlay.SetActive(purchased);
            }

            // Highlight: purchasable (available + can afford) gets highlight color; otherwise locked tint
            var img = button != null ? button.targetGraphic as Image : null;
            if (img != null)
            {
                if (purchased)
                    img.color = Color.white;
                else if (available && canAfford)
                    img.color = purchasableHighlightColor;
                else
                    img.color = lockedTintColor;
            }

            // Icon: set from GameManager's per-upgrade icon list
            if (iconImage != null)
            {
                var icon = GameManager.Instance.GetUpgradeIcon(upgradeCode);
                iconImage.enabled = icon != null;
                if (icon != null)
                    iconImage.sprite = icon;
            }
        }

        void UpdateTexts(GameManager gm)
        {
            if (titleText == null && descriptionText == null) return;

            switch (upgradeCode)
            {
                case "U1":
                    if (titleText != null) titleText.text = "+Session Time";
                    if (descriptionText != null) descriptionText.text = "+2 seconds to session length.";
                    break;

                case "U2":
                    if (titleText != null) titleText.text = "+Breaker Radius I";
                    if (descriptionText != null)
                    {
                        float newRadius = gm.breakerRadius * 1.15f;
                        descriptionText.text = $"Increase the breaker radius to {newRadius:0.00}.";
                    }
                    break;

                case "U3":
                    if (titleText != null) titleText.text = "+Breaker Damage I";
                    if (descriptionText != null) descriptionText.text = "Increase breaker damage by 10%.";
                    break;

                case "U4":
                    if (titleText != null) titleText.text = "+Max Ducks I";
                    if (descriptionText != null) descriptionText.text = "Increase max ducks to 30.";
                    break;

                case "U5":
                    if (titleText != null) titleText.text = "+Breaker Radius II";
                    if (descriptionText != null)
                    {
                        float newRadius = gm.breakerRadius * 1.25f;
                        descriptionText.text = $"Increase the breaker radius to {newRadius:0.00}.";
                    }
                    break;

                case "U6":
                    if (titleText != null) titleText.text = "+Breaker Damage II";
                    if (descriptionText != null) descriptionText.text = "Add +1 flat breaker damage.";
                    break;

                case "U7":
                    if (titleText != null) titleText.text = "+Max Ducks II";
                    if (descriptionText != null) descriptionText.text = "Increase max ducks to 55.";
                    break;

                case "U8":
                    if (titleText != null) titleText.text = "+Breaker Speed I";
                    if (descriptionText != null) descriptionText.text = "Breaker ticks 25% faster.";
                    break;

                case "U9":
                    if (titleText != null) titleText.text = "+Crit Chance I";
                    if (descriptionText != null) descriptionText.text = "+10% chance for critical breaker hits.";
                    break;

                case "U10":
                    if (titleText != null) titleText.text = "+Duck Size I";
                    if (descriptionText != null) descriptionText.text = "Increase duck size by 10%.";
                    break;

                case "U11":
                    if (titleText != null) titleText.text = "+Crit Chance II";
                    if (descriptionText != null) descriptionText.text = "+5% chance for critical breaker hits.";
                    break;

                case "U12":
                    if (titleText != null) titleText.text = "+Breaker Speed II";
                    if (descriptionText != null) descriptionText.text = "Breaker ticks another 25% faster.";
                    break;

                case "U13":
                    if (titleText != null) titleText.text = "+Breaker Radius III";
                    if (descriptionText != null)
                    {
                        float newRadius = gm.breakerRadius * 1.25f;
                        descriptionText.text = $"Increase the breaker radius to {newRadius:0.00}.";
                    }
                    break;

                case "U14":
                    if (titleText != null) titleText.text = "+Breaker Damage III";
                    if (descriptionText != null) descriptionText.text = "Increase breaker damage by 10%.";
                    break;

                case "U15":
                    if (titleText != null) titleText.text = "+Duck Size II";
                    if (descriptionText != null) descriptionText.text = "Increase duck size by another 10%.";
                    break;

                case "U16":
                    if (titleText != null) titleText.text = "+Breaker Speed III";
                    if (descriptionText != null) descriptionText.text = "Breaker ticks 25% faster again.";
                    break;

                case "U17":
                    if (titleText != null) titleText.text = "+Duck Mass I";
                    if (descriptionText != null) descriptionText.text = "Ducks award 40% more gold.";
                    break;

                case "U18":
                    if (titleText != null) titleText.text = "+Breaker Damage IV";
                    if (descriptionText != null) descriptionText.text = "Add +2 flat breaker damage.";
                    break;

                case "U19":
                    if (titleText != null) titleText.text = "+Breaker Damage V";
                    if (descriptionText != null) descriptionText.text = "Add +1 flat breaker damage.";
                    break;

                case "U20":
                    if (titleText != null) titleText.text = "+Crit Chance III";
                    if (descriptionText != null) descriptionText.text = "+10% chance for critical breaker hits.";
                    break;

                case "U21":
                    if (titleText != null) titleText.text = "+Crit Damage";
                    if (descriptionText != null) descriptionText.text = "+25% extra damage on crits.";
                    break;

                case "U22":
                    if (titleText != null) titleText.text = "+Duck Mass II";
                    if (descriptionText != null) descriptionText.text = "Ducks award 50% more gold.";
                    break;
            }
        }

        void SetHoverVisible(bool visible)
        {
            // Title is always visible; only cost and description (or hover panel) show on hover.
            if (titleText != null)
                titleText.gameObject.SetActive(true);
            if (hoverPanel != null)
                hoverPanel.SetActive(visible);
            else
            {
                if (descriptionText != null) descriptionText.gameObject.SetActive(visible);
                if (costText != null) costText.gameObject.SetActive(visible);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHoverVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHoverVisible(false);
        }
    }
}

