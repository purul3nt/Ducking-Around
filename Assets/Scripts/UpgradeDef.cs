using System.Collections.Generic;

namespace DuckingAround
{
    /// <summary>
    /// Definition of an upgrade for dependency graph and UI.
    /// </summary>
    [System.Serializable]
    public class UpgradeDef
    {
        public string id;
        public string name;
        public List<string> requiresIds;

        public UpgradeDef(string id, string name, List<string> requiresIds = null)
        {
            this.id = id;
            this.name = name;
            this.requiresIds = requiresIds ?? new List<string>();
        }
    }

    /// <summary>
    /// Display state of a node in the upgrade graph.
    /// </summary>
    public enum UpgradeNodeState
    {
        Locked,   // Prereqs not met
        Available, // Can purchase
        Unlocked  // Already purchased
    }

    /// <summary>
    /// Provides current state per upgrade id (e.g. from GameManager).
    /// </summary>
    public interface IUpgradeStateProvider
    {
        UpgradeNodeState GetState(string upgradeId);
    }
}
