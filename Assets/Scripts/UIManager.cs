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

        [Header("Upgrade Cost Labels")]
        public TMP_Text breakerCostText;
        public TMP_Text ducksCostText;

        [Header("Panels")]
        public GameObject upgradesPanel;

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

            if (breakerCostText != null) breakerCostText.text = gm.GetBreakerUpgradeCost().ToString();
            if (ducksCostText != null) ducksCostText.text = gm.GetDucksUpgradeCost().ToString();
        }

        public void ShowUpgrades(bool show)
        {
            if (upgradesPanel != null)
            {
                upgradesPanel.SetActive(show);
            }
        }

        // --- Button hooks ---------------------------------------------------

        public void OnUpgradeBreakerClicked()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.UpgradeBreaker();
        }

        public void OnUpgradeDucksClicked()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.UpgradeDucks();
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

