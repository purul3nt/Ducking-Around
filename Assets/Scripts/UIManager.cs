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
        Coroutine _timerPulseRoutine;

        [Header("Panels")]
        public GameObject upgradesPanel;
        [Tooltip("Optional background image/object to show while the upgrade panel is active.")]
        public GameObject upgradePanelBackground;
        [Tooltip("Session end summary: ducks killed, gold gained, Restart and Upgrades buttons.")]
        public GameObject summaryPanel;
        [Tooltip("Text showing ducks killed this session (e.g. 'Ducks eliminated: 12').")]
        public TMP_Text summaryDucksKilledText;
        [Tooltip("Text showing gold gained this session (e.g. 'Gold earned: 45').")]
        public TMP_Text summaryGoldGainedText;

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
                goldText.text = "GOLD: " + currentGold.ToString();
                if (_lastGold >= 0 && currentGold > _lastGold)
                    TriggerGoldPulse();
                _lastGold = currentGold;
            }
            else
            {
                _lastGold = gm.gold;
            }
            if (timeText != null)
            {
                timeText.text = gm.timeLeft.ToString("0.0") + "s";
                if (gm.sessionActive && gm.timeLeft <= 3f && gm.timeLeft > 0f)
                {
                    if (_timerPulseRoutine == null)
                        _timerPulseRoutine = StartCoroutine(TimerPulseRoutine());
                }
                else
                {
                    if (_timerPulseRoutine != null)
                    {
                        StopCoroutine(_timerPulseRoutine);
                        _timerPulseRoutine = null;
                        timeText.transform.localScale = Vector3.one;
                    }
                }
            }
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
            if (upgradePanelBackground != null)
                upgradePanelBackground.SetActive(show);
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

        public void ShowSummary(bool show)
        {
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(show);
                if (show && GameManager.Instance != null)
                {
                    var gm = GameManager.Instance;
                    if (summaryDucksKilledText != null)
                        summaryDucksKilledText.text = "Ducks eliminated: " + gm.sessionDucksKilled;
                    if (summaryGoldGainedText != null)
                        summaryGoldGainedText.text = "Gold earned: " + gm.sessionGoldGained;
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

        /// <summary>Summary panel: restart session and hide summary.</summary>
        public void OnSummaryRestartClicked()
        {
            ShowSummary(false);
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewSession();
        }

        /// <summary>Summary panel: go to upgrades (hide summary, show upgrades panel).</summary>
        public void OnSummaryUpgradesClicked()
        {
            ShowSummary(false);
            ShowUpgrades(true);
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

        IEnumerator TimerPulseRoutine()
        {
            if (timeText == null) yield break;
            Transform t = timeText.transform;
            Vector3 baseScale = t.localScale;
            float half = goldPulseDuration * 0.5f;

            while (GameManager.Instance != null && GameManager.Instance.sessionActive)
            {
                float timeLeft = GameManager.Instance.timeLeft;
                if (timeLeft > 3f || timeLeft <= 0f) break;

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
            }

            if (timeText != null)
                timeText.transform.localScale = Vector3.one;
            _timerPulseRoutine = null;
        }
    }
}

