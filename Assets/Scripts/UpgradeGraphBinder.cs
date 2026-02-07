using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// When the upgrade panel is visible, builds the UpgradeGraphView from GameManager defs and state.
    /// Assign this and the graph view to the upgrades panel (or a child). Wire UIManager.upgradeGraphView
    /// so Refresh() is called when state changes (e.g. after purchase).
    /// </summary>
    [RequireComponent(typeof(UpgradeGraphView))]
    public class UpgradeGraphBinder : MonoBehaviour
    {
        UpgradeGraphView _graph;

        void Awake()
        {
            _graph = GetComponent<UpgradeGraphView>();
        }

        void OnEnable()
        {
            BuildFromGameManager();
        }

        /// <summary>
        /// Build the graph from current GameManager definitions and state. Call when panel becomes visible.
        /// </summary>
        public void BuildFromGameManager()
        {
            if (_graph == null) _graph = GetComponent<UpgradeGraphView>();
            if (_graph == null || GameManager.Instance == null) return;

            var defs = GameManager.Instance.GetUpgradeDefsForGraph();
            _graph.Build(defs, GameManager.Instance, OnUpgradeClick);
        }

        void OnUpgradeClick(string upgradeCode)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.PurchaseUpgrade(upgradeCode);
        }
    }
}
