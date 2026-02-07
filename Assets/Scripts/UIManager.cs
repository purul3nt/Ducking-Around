using UnityEngine;
using TMPro;

namespace DuckingAround
{
    /// <summary>
    /// Minimal UI controller for showing gold, time, and upgrades.
    /// Hook this up to a Canvas with TMP_Text labels and buttons.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Labels")]
        public TMP_Text goldText;
        public TMP_Text timeText;
        public TMP_Text breakerRadiusText;
        public TMP_Text ducksPerDeathText;

        [Header("Panels")]
        public GameObject upgradesPanel;

        [Header("Upgrade Grid")]
        [Tooltip("All upgrade slots in the grid, each configured with its upgrade code.")]
        public UpgradeButton[] upgradeButtons;

        [Header("Upgrade Graph")]
        [Tooltip("Optional dependency graph view; Refresh() is called when HUD updates (e.g. after purchase).")]
        public UpgradeGraphView upgradeGraphView;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void UpdateHUD()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;

            if (goldText != null) goldText.text = gm.gold.ToString();
            if (timeText != null) timeText.text = gm.timeLeft.ToString("0.0") + "s";
            if (breakerRadiusText != null) breakerRadiusText.text = gm.breakerRadius.ToString("0.00");
            if (ducksPerDeathText != null) ducksPerDeathText.text = gm.ducksPerDeath.ToString();

            // Refresh upgrade grid state (costs, lock, purchased, interactable).
            if (upgradeButtons != null)
            {
                foreach (var ub in upgradeButtons)
                {
                    if (ub != null)
                    {
                        ub.Refresh();
                    }
                }
            }

            if (upgradeGraphView != null)
                upgradeGraphView.Refresh();
        }

        public void ShowUpgrades(bool show)
        {
            if (upgradesPanel != null)
            {
                upgradesPanel.SetActive(show);
                if (show && upgradeGraphView != null)
                {
                    var binder = upgradeGraphView.GetComponent<UpgradeGraphBinder>();
                    if (binder != null)
                        binder.BuildFromGameManager();
                }
            }
        }

        // --- Button hooks ---------------------------------------------------
        // Generic upgrade button that takes an upgrade code (e.g. \"U1\", \"U2\"...).
        public void OnUpgradeClicked(string upgradeCode)
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.PurchaseUpgrade(upgradeCode);
        }

        public void OnNextSessionClicked()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.StartNewSession();
        }

        // Optional: separate restart button alias for clarity in the editor.
        public void OnRestartSessionClicked()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.StartNewSession();
        }
    }
}

