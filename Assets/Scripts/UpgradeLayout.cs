using System.Collections.Generic;
using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Lays out upgrade buttons in two columns and positions their hover text
    /// panels to the right of each button.
    ///
    /// Column 1: upgrades U1–U11 at ~30% screen width.
    /// Column 2: upgrades U12+ at ~60% screen width.
    /// Vertical spacing: 8px between buttons.
    /// Hover text (title/description/cost) appears to the right of the button
    /// with a 4px gap, still activated on hover via UpgradeButton.
    /// </summary>
    [ExecuteAlways]
    public class UpgradeLayout : MonoBehaviour
    {
        [Tooltip("If empty, all UpgradeButton children will be used.")]
        public UpgradeButton[] buttons;

        [Tooltip("Vertical spacing in pixels between buttons.")]
        public float verticalSpacing = 8f;

        [Tooltip("Horizontal gap in pixels between button and its hover panel/text.")]
        public float hoverGap = 4f;

        [Header("Columns")]
        [Tooltip("Horizontal position of column 1 as a fraction of panel width (0 = left, 1 = right).")]
        public float column1X = 0.05f;

        [Tooltip("Horizontal position of column 2 as a fraction of panel width (0 = left, 1 = right).")]
        public float column2X = 0.50f;

        RectTransform rectTransform;

        void OnEnable()
        {
            rectTransform = GetComponent<RectTransform>();
            LayoutButtons();
        }

        void OnValidate()
        {
            // Keep layout roughly correct while tweaking in editor.
            rectTransform = GetComponent<RectTransform>();
            LayoutButtons();
        }

        void LayoutButtons()
        {
            if (rectTransform == null) return;

            UpgradeButton[] allButtons = buttons;
            if (allButtons == null || allButtons.Length == 0)
            {
                allButtons = GetComponentsInChildren<UpgradeButton>(true);
            }

            if (allButtons == null || allButtons.Length == 0) return;

            // Separate into two columns based on upgrade code number.
            var col1 = new List<UpgradeButton>();
            var col2 = new List<UpgradeButton>();

            foreach (var ub in allButtons)
            {
                if (ub == null || string.IsNullOrEmpty(ub.upgradeCode)) continue;

                if (TryParseUpgradeIndex(ub.upgradeCode, out int index))
                {
                    if (index >= 1 && index <= 11)
                        col1.Add(ub);
                    else
                        col2.Add(ub);
                }
                else
                {
                    // If code is not in U<number> format, put it in first column by default.
                    col1.Add(ub);
                }
            }

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            // Positions are relative to the panel's center (pivot assumed to be 0.5,0.5).
            float xCol1 = (column1X - 0.1f) * width;
            float xCol2 = (column2X - 0.5f) * width;

            LayoutColumn(col1, xCol1, height);
            LayoutColumn(col2, xCol2, height);
        }

        void LayoutColumn(List<UpgradeButton> column, float xCenter, float panelHeight)
        {
            if (column.Count == 0) return;

            // Sort column by upgrade index so they stack in order.
            column.Sort((a, b) =>
            {
                TryParseUpgradeIndex(a.upgradeCode, out int ia);
                TryParseUpgradeIndex(b.upgradeCode, out int ib);
                return ia.CompareTo(ib);
            });

            // Use the first button to estimate height.
            RectTransform firstRt = column[0].GetComponent<RectTransform>();
            float buttonHeight = firstRt != null ? firstRt.rect.height : 40f;

            // Start a bit below the top of the panel.
            float totalHeight = column.Count * buttonHeight + (column.Count - 1) * verticalSpacing;
            float startY = totalHeight * 0.5f - buttonHeight * 0.5f;

            for (int i = 0; i < column.Count; i++)
            {
                var ub = column[i];
                if (ub == null) continue;

                RectTransform rt = ub.GetComponent<RectTransform>();
                if (rt == null) continue;

                float y = startY - i * (buttonHeight + verticalSpacing);

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(xCenter, y);

                PositionHoverPanel(ub, rt);
            }
        }

        void PositionHoverPanel(UpgradeButton ub, RectTransform buttonRt)
        {
            float buttonWidth = buttonRt.rect.width;

            // If a hover panel is provided, position that panel to the right.
            if (ub.hoverPanel != null)
            {
                RectTransform hoverRt = ub.hoverPanel.GetComponent<RectTransform>();
                if (hoverRt == null) return;

                hoverRt.pivot = new Vector2(0f, 0.5f); // left-center

                // Case 1: hover panel is a child of the button (common setup).
                if (hoverRt.parent == buttonRt)
                {
                    hoverRt.anchorMin = new Vector2(0.5f, 0.5f);
                    hoverRt.anchorMax = new Vector2(0.5f, 0.5f);

                    float localX = buttonWidth * 0.5f + hoverGap;
                    hoverRt.anchoredPosition = new Vector2(localX, 0f);
                }
                else
                {
                    // Case 2: hover panel shares the same parent as the button (sibling).
                    hoverRt.anchorMin = new Vector2(0.5f, 0.5f);
                    hoverRt.anchorMax = new Vector2(0.5f, 0.5f);

                    float x = buttonRt.anchoredPosition.x + buttonWidth * 0.5f + hoverGap;
                    float y = buttonRt.anchoredPosition.y;
                    hoverRt.anchoredPosition = new Vector2(x, y);
                }
            }
            else
            {
                // No hover panel: position the individual label texts (title, description, cost)
                // to the right of the button, next to each other, with hoverGap spacing.
                RectTransform[] labelRts =
                {
                    ub.titleText != null ? ub.titleText.rectTransform : null,
                    ub.descriptionText != null ? ub.descriptionText.rectTransform : null,
                    ub.costText != null ? ub.costText.rectTransform : null
                };

                float currentLocalX = buttonWidth * 0.5f + hoverGap;

                for (int i = 0; i < labelRts.Length; i++)
                {
                    var rt = labelRts[i];
                    if (rt == null) continue;

                    // Assume labels are children of the button.
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f); // left-center

                    rt.anchoredPosition = new Vector2(currentLocalX, 0f);

                    float labelWidth = rt.rect.width;
                    currentLocalX += labelWidth + hoverGap;
                }
            }
        }

        bool TryParseUpgradeIndex(string code, out int index)
        {
            index = 0;
            if (string.IsNullOrEmpty(code)) return false;
            if (code[0] != 'U') return false;

            string num = code.Substring(1);
            return int.TryParse(num, out index);
        }
    }
}

