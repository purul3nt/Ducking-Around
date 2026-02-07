using System.Collections;
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

        [Header("Gold pulse")]
        [Tooltip("Scale the gold text reaches at peak of pulse when gold is collected.")]
        public float goldPulseScale = 1.35f;
        [Tooltip("Duration of the gold pulse animation in seconds.")]
        public float goldPulseDuration = 0.25f;

        int _lastGold = -1;
        Coroutine _goldPulseRoutine;

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

            if (goldText != null)
            {
                int currentGold = gm.gold;
                goldText.text = currentGold.ToString();
                if (_lastGold >= 0 && currentGold > _lastGold)
                    TriggerGoldPulse();
                _lastGold = currentGold;
            }
            else
            {
                _lastGold = gm.gold;
            }
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

        void TriggerGoldPulse()
        {
            if (goldText == null) return;
            if (_goldPulseRoutine != null)
                StopCoroutine(_goldPulseRoutine);
            _goldPulseRoutine = StartCoroutine(GoldPulseRoutine());
        }

        IEnumerator GoldPulseRoutine()
        {
            Transform t = goldText.transform;
            Vector3 baseScale = t.localScale;
            float half = goldPulseDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float tNorm = elapsed / half;
                float s = Mathf.Lerp(1f, goldPulseScale, tNorm);
                t.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
                yield return null;
            }
            elapsed = half;
            while (elapsed < goldPulseDuration)
            {
                elapsed += Time.deltaTime;
                float tNorm = (elapsed - half) / half;
                float s = Mathf.Lerp(goldPulseScale, 1f, tNorm);
                t.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
                yield return null;
            }

            t.localScale = baseScale;
            _goldPulseRoutine = null;
        }
    }
}

