using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DuckingAround
{
    /// <summary>
    /// Auto-generates a dependency graph UI from upgrade definitions: topologically sorted layers,
    /// node buttons from a prefab, and uGUI Image edges. Styles by UpgradeNodeState.
    /// </summary>
    public class UpgradeGraphView : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Prefab for each node (must have RectTransform; Button and optional TMP_Text for label).")]
        public GameObject nodePrefab;
        [Tooltip("Container where node instances are created. If null, uses this transform.")]
        public RectTransform nodeContainer;
        [Tooltip("Container for edge lines (created if null). Drawn behind nodes.")]
        public RectTransform edgeContainer;
        [Tooltip("Vertical spacing between dependency layers (top to bottom).")]
        public float layerSpacing = 64f;
        [Tooltip("Horizontal spacing between buttons in the same row (same layer).")]
        public float nodeSpacing = 52f;

        [Header("Node colors by state")]
        public Color unlockedColor = new Color(0.4f, 0.8f, 0.4f);
        public Color availableColor = new Color(0.9f, 0.9f, 0.5f);
        public Color lockedColor = new Color(0.5f, 0.5f, 0.5f);

        [Header("Edge")]
        [Tooltip("Thickness of the lines drawn between prerequisite and dependent nodes.")]
        public float edgeLineWidth = 5f;
        public Color edgeSatisfiedColor = new Color(0.5f, 0.9f, 0.5f, 0.8f);
        public Color edgeUnsatisfiedColor = new Color(0.6f, 0.5f, 0.5f, 0.6f);

        RectTransform _rect;
        Dictionary<string, NodeEntry> _nodes = new Dictionary<string, NodeEntry>();
        List<GameObject> _edgeLines = new List<GameObject>();
        List<UpgradeDef> _defs;
        IUpgradeStateProvider _stateProvider;
        Action<string> _onClick;

        class NodeEntry
        {
            public UpgradeDef def;
            public int layer;
            public RectTransform rect;
            public Button button;
            public Image image;
            public TMP_Text label;
        }

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (nodeContainer == null) nodeContainer = _rect;
            if (edgeContainer == null)
            {
                var go = new GameObject("EdgeContainer");
                go.transform.SetParent(nodeContainer, false);
                go.transform.SetAsFirstSibling();
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                edgeContainer = rt;
            }
        }

        /// <summary>
        /// Build the graph from definitions and state. Call when upgrade panel becomes visible.
        /// </summary>
        public void Build(List<UpgradeDef> defs, IUpgradeStateProvider stateProvider, Action<string> onClick)
        {
            _defs = defs;
            _stateProvider = stateProvider;
            _onClick = onClick;

            Clear();

            if (defs == null || defs.Count == 0 || nodePrefab == null)
                return;

            var idToDef = new Dictionary<string, UpgradeDef>();
            foreach (var d in defs)
            {
                if (string.IsNullOrEmpty(d.id)) continue;
                idToDef[d.id] = d;
            }

            // Validate: missing prerequisite ids
            foreach (var d in defs)
            {
                if (d.requiresIds == null) continue;
                foreach (var reqId in d.requiresIds)
                {
                    if (string.IsNullOrEmpty(reqId)) continue;
                    if (!idToDef.ContainsKey(reqId))
                        Debug.LogWarning($"[UpgradeGraphView] Upgrade '{d.id}' requires missing id '{reqId}'.");
                }
            }

            // Topological sort: in-degree = count of prereqs that exist
            var inDegree = new Dictionary<string, int>();
            var dependents = new Dictionary<string, List<string>>();
            foreach (var id in idToDef.Keys)
            {
                inDegree[id] = 0;
                dependents[id] = new List<string>();
            }
            foreach (var d in defs)
            {
                if (d.requiresIds == null) continue;
                foreach (var reqId in d.requiresIds)
                {
                    if (!idToDef.ContainsKey(reqId)) continue;
                    inDegree[d.id]++;
                    dependents[reqId].Add(d.id);
                }
            }

            var layerById = new Dictionary<string, int>();
            var queue = new Queue<string>(inDegree.Where(p => p.Value == 0).Select(p => p.Key));
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                var def = idToDef[id];
                int myLayer = 0;
                if (def.requiresIds != null)
                {
                    foreach (var reqId in def.requiresIds)
                    {
                        if (!idToDef.TryGetValue(reqId, out _)) continue;
                        int reqLayer = layerById.TryGetValue(reqId, out var rl) ? rl : 0;
                        myLayer = Mathf.Max(myLayer, reqLayer + 1);
                    }
                }
                layerById[id] = myLayer;

                foreach (var depId in dependents[id])
                {
                    inDegree[depId]--;
                    if (inDegree[depId] == 0)
                        queue.Enqueue(depId);
                }
            }

            // Cycle detection
            foreach (var kv in inDegree)
            {
                if (kv.Value > 0)
                    Debug.LogError($"[UpgradeGraphView] Cycle or missing prereq involving upgrade '{kv.Key}'.");
            }

            int maxLayer = 0;
            foreach (var id in idToDef.Keys)
            {
                if (!layerById.ContainsKey(id))
                    layerById[id] = 0;
                maxLayer = Mathf.Max(maxLayer, layerById[id]);
            }

            // Group by layer
            var byLayer = new Dictionary<int, List<string>>();
            for (int i = 0; i <= maxLayer; i++)
                byLayer[i] = new List<string>();
            foreach (var id in idToDef.Keys)
                byLayer[layerById[id]].Add(id);

            // Crossing minimization: order nodes within each layer so edges cross less (barycentric / median heuristic)
            const int crossingPasses = 4;
            for (int pass = 0; pass < crossingPasses; pass++)
            {
                // Forward: order each layer by median index of prerequisites in previous layer
                for (int layer = 1; layer <= maxLayer; layer++)
                {
                    var list = byLayer[layer];
                    var prevList = byLayer[layer - 1];
                    var indexInPrev = new Dictionary<string, int>();
                    for (int i = 0; i < prevList.Count; i++)
                        indexInPrev[prevList[i]] = i;
                    list.Sort((a, b) =>
                    {
                        float medA = MedianPrereqIndex(idToDef[a], indexInPrev, prevList.Count);
                        float medB = MedianPrereqIndex(idToDef[b], indexInPrev, prevList.Count);
                        int c = medA.CompareTo(medB);
                        return c != 0 ? c : string.CompareOrdinal(a, b);
                    });
                }
                // Backward: order each layer by median index of dependents in next layer
                for (int layer = maxLayer - 1; layer >= 0; layer--)
                {
                    var list = byLayer[layer];
                    var nextList = byLayer[layer + 1];
                    var indexInNext = new Dictionary<string, int>();
                    for (int i = 0; i < nextList.Count; i++)
                        indexInNext[nextList[i]] = i;
                    list.Sort((a, b) =>
                    {
                        float medA = MedianDependentIndex(dependents[a], indexInNext, nextList.Count);
                        float medB = MedianDependentIndex(dependents[b], indexInNext, nextList.Count);
                        int c = medA.CompareTo(medB);
                        return c != 0 ? c : string.CompareOrdinal(a, b);
                    });
                }
            }

            // Read Button size from the prefab asset so we use intended height (e.g. 120), not the clone's layout-affected value
            Vector2 buttonSizeFromPrefab = new Vector2(160f, 120f);
            if (nodePrefab != null)
            {
                var prefabBtn = nodePrefab.GetComponentInChildren<Button>(true);
                var prefabBtnRt = prefabBtn != null ? prefabBtn.GetComponent<RectTransform>() : null;
                if (prefabBtnRt != null)
                    buttonSizeFromPrefab = new Vector2(prefabBtnRt.sizeDelta.x, prefabBtnRt.sizeDelta.y);
            }

            // Create nodes (top-center anchor; layers go top to bottom; within layer, horizontal row centered)
            foreach (int layer in Enumerable.Range(0, maxLayer + 1))
            {
                var ids = byLayer[layer];
                float totalW = (ids.Count - 1) * nodeSpacing;
                float x = -totalW * 0.5f;
                foreach (var id in ids)
                {
                    var def = idToDef[id];
                    var go = Instantiate(nodePrefab, nodeContainer);
                    var rt = go.GetComponent<RectTransform>();
                    if (rt == null) rt = go.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(x, -layer * layerSpacing);
                    rt.sizeDelta = buttonSizeFromPrefab;
                    rt.localScale = Vector3.one;

                    var btn = go.GetComponentInChildren<Button>(true);
                    var buttonRt = btn != null ? btn.GetComponent<RectTransform>() : null;
                    if (buttonRt != null)
                    {
                        buttonRt.sizeDelta = buttonSizeFromPrefab;
                    }

                    // Only stretch a direct child that looks like a container (e.g. nested Canvas); leave Description/Cost/Button layout as in prefab
                    for (int i = 0; i < rt.childCount; i++)
                    {
                        var child = rt.GetChild(i);
                        var childRt = child.GetComponent<RectTransform>();
                        if (childRt == null) continue;
                        var name = child.name.ToLowerInvariant();
                        if (name.Contains("canvas") || name.Contains("container"))
                        {
                            childRt.anchorMin = Vector2.zero;
                            childRt.anchorMax = Vector2.one;
                            childRt.offsetMin = Vector2.zero;
                            childRt.offsetMax = Vector2.zero;
                            childRt.localScale = Vector3.one;
                        }
                        break;
                    }
                    var img = go.GetComponent<Image>();
                    if (img == null) img = go.GetComponentInChildren<Image>();
                    // Prefer the TMP_Text on the child named "Title" (e.g. Button/Title in the prefab)
                    TMP_Text label = null;
                    foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (string.Equals(t.gameObject.name, "Title", StringComparison.OrdinalIgnoreCase))
                        {
                            label = t;
                            break;
                        }
                    }
                    if (label == null)
                        label = go.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                        label.text = string.IsNullOrEmpty(def.name) ? def.id : def.name;

                    var tag = go.GetComponent<UpgradeGraphNodeTag>();
                    if (tag == null) tag = go.AddComponent<UpgradeGraphNodeTag>();
                    tag.upgradeId = id;

                    var upgradeButton = go.GetComponent<UpgradeButton>();
                    if (upgradeButton != null)
                    {
                        upgradeButton.upgradeCode = id;
                        upgradeButton.Refresh();
                    }

                    string captureId = id;
                    if (btn != null)
                        btn.onClick.AddListener(() => _onClick?.Invoke(captureId));

                    _nodes[id] = new NodeEntry
                    {
                        def = def,
                        layer = layer,
                        rect = rt,
                        button = btn,
                        image = img,
                        label = label
                    };
                    x += nodeSpacing;
                }
            }

            // Force layout so node rects have valid size before we draw edges
            Canvas.ForceUpdateCanvases();

            // Create edges (prereq -> dependent)
            foreach (var d in defs)
            {
                if (d.requiresIds == null) continue;
                foreach (var reqId in d.requiresIds)
                {
                    if (!_nodes.TryGetValue(reqId, out var from) || !_nodes.TryGetValue(d.id, out var to))
                        continue;
                    CreateEdge(from.rect, to.rect, reqId);
                }
            }

            // Draw edges on top of nodes so lines are visible
            if (edgeContainer != null)
                edgeContainer.SetAsLastSibling();

            Refresh();
        }

        void CreateEdge(RectTransform from, RectTransform to, string fromId)
        {
            var go = new GameObject("Edge");
            go.transform.SetParent(edgeContainer, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = new Color(edgeUnsatisfiedColor.r, edgeUnsatisfiedColor.g, edgeUnsatisfiedColor.b, 1f);
            img.raycastTarget = false;
            img.type = Image.Type.Simple;

            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(edgeLineWidth, edgeLineWidth);

            _edgeLines.Add(go);

            var line = go.AddComponent<GraphEdgeLine>();
            line.SetTransforms(from, to, rt, img, this, fromId);
        }

        static Sprite _whiteSprite;
        static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }

        /// <summary>
        /// Re-query state and update node/edge styling. Call when state changes (e.g. after purchase).
        /// </summary>
        public void Refresh()
        {
            if (_stateProvider == null) return;

            foreach (var kv in _nodes)
            {
                var entry = kv.Value;
                var ub = entry.rect != null ? entry.rect.GetComponent<UpgradeButton>() : null;
                if (ub != null)
                    ub.Refresh();
                else
                {
                    var state = _stateProvider.GetState(kv.Key);
                    if (entry.image != null)
                        entry.image.color = StateToColor(state);
                    if (entry.button != null)
                        entry.button.interactable = (state == UpgradeNodeState.Available);
                }
            }

            foreach (var go in _edgeLines)
            {
                var line = go.GetComponent<GraphEdgeLine>();
                if (line != null) line.Refresh();
            }
        }

        Color StateToColor(UpgradeNodeState s)
        {
            switch (s)
            {
                case UpgradeNodeState.Unlocked: return unlockedColor;
                case UpgradeNodeState.Available: return availableColor;
                default: return lockedColor;
            }
        }

        public bool IsUnlocked(string id)
        {
            return _stateProvider != null && _stateProvider.GetState(id) == UpgradeNodeState.Unlocked;
        }

        public Color EdgeSatisfiedColor => edgeSatisfiedColor;
        public Color EdgeUnsatisfiedColor => edgeUnsatisfiedColor;

        static float MedianPrereqIndex(UpgradeDef def, Dictionary<string, int> indexInPrev, int layerSize)
        {
            if (def.requiresIds == null || def.requiresIds.Count == 0)
                return layerSize * 0.5f;
            var indices = new List<int>();
            foreach (var reqId in def.requiresIds)
            {
                if (indexInPrev.TryGetValue(reqId, out int idx))
                    indices.Add(idx);
            }
            if (indices.Count == 0) return layerSize * 0.5f;
            indices.Sort();
            int m = indices.Count / 2;
            return indices.Count % 2 == 1 ? indices[m] : (indices[m - 1] + indices[m]) * 0.5f;
        }

        static float MedianDependentIndex(List<string> dependents, Dictionary<string, int> indexInNext, int layerSize)
        {
            if (dependents == null || dependents.Count == 0)
                return layerSize * 0.5f;
            var indices = new List<int>();
            foreach (var depId in dependents)
            {
                if (indexInNext.TryGetValue(depId, out int idx))
                    indices.Add(idx);
            }
            if (indices.Count == 0) return layerSize * 0.5f;
            indices.Sort();
            int m = indices.Count / 2;
            return indices.Count % 2 == 1 ? indices[m] : (indices[m - 1] + indices[m]) * 0.5f;
        }

        void Clear()
        {
            foreach (var n in _nodes.Values)
            {
                if (n.rect != null && n.rect.gameObject != null)
                    Destroy(n.rect.gameObject);
            }
            _nodes.Clear();
            foreach (var go in _edgeLines)
            {
                if (go != null) Destroy(go);
            }
            _edgeLines.Clear();
        }
    }

    /// <summary>
    /// Updates edge line position/rotation and color based on graph state.
    /// </summary>
    public class GraphEdgeLine : MonoBehaviour
    {
        RectTransform _from, _to, _rect;
        Image _image;
        UpgradeGraphView _graph;
        string _fromId;

        public void SetTransforms(RectTransform from, RectTransform to, RectTransform rect, Image image, UpgradeGraphView graph, string fromId)
        {
            _from = from;
            _to = to;
            _rect = rect;
            _image = image;
            _graph = graph;
            _fromId = fromId;
        }

        void LateUpdate()
        {
            UpdateGeometry();
        }

        public void Refresh()
        {
            UpdateGeometry();
            if (_image != null && _graph != null && !string.IsNullOrEmpty(_fromId))
                _image.color = _graph.IsUnlocked(_fromId) ? _graph.EdgeSatisfiedColor : _graph.EdgeUnsatisfiedColor;
        }

        void UpdateGeometry()
        {
            if (_from == null || _to == null || _rect == null || _image == null) return;

            RectTransform edgeParent = _rect.parent as RectTransform;
            if (edgeParent == null) return;

            // Get connection points: use the Button child's rect if present (actual button), else node root; use center in world space
            RectTransform fromRect = _from;
            RectTransform toRect = _to;
            var fromBtn = _from.GetComponentInChildren<Button>(true);
            var toBtn = _to.GetComponentInChildren<Button>(true);
            if (fromBtn != null) fromRect = fromBtn.GetComponent<RectTransform>();
            if (toBtn != null) toRect = toBtn.GetComponent<RectTransform>();
            if (fromRect == null) fromRect = _from;
            if (toRect == null) toRect = _to;
            Vector2 fromCenter = fromRect.rect.center;
            Vector2 toCenter = toRect.rect.center;
            Vector3 worldA = fromRect.TransformPoint(new Vector3(fromCenter.x, fromCenter.y, 0f));
            Vector3 worldB = toRect.TransformPoint(new Vector3(toCenter.x, toCenter.y, 0f));

            // Convert to edge container's local space (origin at container pivot/center)
            Vector2 localA = edgeParent.InverseTransformPoint(worldA);
            Vector2 localB = edgeParent.InverseTransformPoint(worldB);

            Vector2 dir = localB - localA;
            float len = dir.magnitude;
            if (len < 1f) len = 1f;
            float lineW = _graph != null ? _graph.edgeLineWidth : 5f;
            _rect.sizeDelta = new Vector2(len, lineW);
            _rect.pivot = new Vector2(0f, 0.5f);
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = localA;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>
    /// Optional: add to node prefab and set upgradeId so edge satisfaction uses it.
    /// </summary>
    public class UpgradeGraphNodeTag : MonoBehaviour
    {
        public string upgradeId;
    }
}
