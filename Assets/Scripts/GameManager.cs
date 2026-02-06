using System.Collections.Generic;
using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Core game loop: session timer, gold, duck spawning, and upgrades.
    /// This is a minimal, scene-agnostic manager intended to sit in a Unity scene
    /// alongside a BreakerController, UIManager, and a simple hot-tub environment.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // --- Upgrade data ---------------------------------------------------

        class UpgradeNode
        {
            public string code;
            public int cost;
            public string dependencyCode;
            public bool purchased;
            public System.Action applyEffect;
        }

        [Header("Session")]
        [Tooltip("Length of a single session in seconds.")]
        public float sessionDuration = 10f;

        [HideInInspector] public float timeLeft;
        [HideInInspector] public bool sessionActive = true;

        [Header("Economy")]
        [HideInInspector] public int gold = 0;

        [Header("Duck settings")]
        public Duck duckPrefab;
        [Tooltip("Optional prefab for Fire Duck (explodes on death).")]
        public Duck fireDuckPrefab;
        [Tooltip("Chance (0–1) that a spawned duck will be a Fire Duck instead of a normal duck.")]
        public float fireDuckChance = 0.15f;
        [Tooltip("Initial number of ducks to spawn at the start of a session.")]
        public int initialDuckCount = 4;
        [Tooltip("Radius of the hot tub in world units, used for random spawn positions.")]
        public float tubRadius = 5f;
        [Tooltip("Center of the crocodile on the XZ plane; ducks will not spawn inside its radius.")]
        public Vector2 crocodileCenterXZ = new Vector2(0.2f, 0f);
        [Tooltip("Radius around the crocodile where ducks are not allowed to spawn.")]
        public float crocodileSpawnAvoidRadius = 1.2f;

        [Header("Runtime stats (affected by upgrades)")]
        [Tooltip("Current breaker radius in world units on the water plane.")]
        public float breakerRadius = 1.4f;
        [Tooltip("Base damage dealt by the breaker before crits.")]
        public float breakerDamage = 1f;
        [Tooltip("Multiplier affecting how quickly the breaker ticks (1 = default speed).")]
        public float breakerSpeedMultiplier = 1f;
        [Tooltip("Chance (0–1) that a breaker hit will critically strike.")]
        public float breakerCritChance = 0f;
        [Tooltip("Extra damage on a critical strike (0.25 = +25% damage).")]
        public float breakerCritBonus = 0f;

        [Tooltip("Number of ducks to spawn when one dies.")]
        public int ducksPerDeath = 1;
        [Tooltip("Maximum number of ducks allowed in the scene at once.")]
        public int maxDucks = 20;
        [Tooltip("Global size multiplier applied to all ducks.")]
        public float duckSizeMultiplier = 1f;
        [Tooltip("Gold multiplier applied per duck killed (1 = normal).")]
        public float duckGoldMultiplier = 1f;

        /// <summary>
        /// All currently alive ducks in the scene.
        /// </summary>
        public List<Duck> Ducks { get; private set; } = new List<Duck>();

        Dictionary<string, UpgradeNode> upgrades;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeUpgrades();
        }

        private void Start()
        {
            StartNewSession();
        }

        private void Update()
        {
            if (!sessionActive) return;

            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                EndSession();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHUD();
            }
        }

        public void StartNewSession()
        {
            // Clear any existing ducks.
            foreach (var d in Ducks)
            {
                if (d != null)
                {
                    Destroy(d.gameObject);
                }
            }

            Ducks.Clear();

            timeLeft = sessionDuration;
            sessionActive = true;

            for (int i = 0; i < initialDuckCount; i++)
            {
                SpawnDuck();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowUpgrades(false);
                UIManager.Instance.UpdateHUD();
            }
        }

        public void EndSession()
        {
            sessionActive = false;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowUpgrades(true);
            }
        }

        public void SpawnDuck()
        {
            if (duckPrefab == null) return;

            // Respect maximum duck count.
            if (Ducks.Count >= maxDucks) return;

            // Random point inside a circle in XZ plane, avoiding the crocodile area.
            Vector3 pos = Vector3.zero;
            const int maxTries = 16;
            int tries = 0;
            Vector2 crocCenter = crocodileCenterXZ;
            float avoidRadiusSqr = crocodileSpawnAvoidRadius * crocodileSpawnAvoidRadius;

            do
            {
                float r = Mathf.Sqrt(Random.value) * (tubRadius * 0.8f);
                float angle = Random.value * Mathf.PI * 2f;

                float x = Mathf.Cos(angle) * r;
                float z = Mathf.Sin(angle) * r;

                Vector2 pXZ = new Vector2(x, z);
                if ((pXZ - crocCenter).sqrMagnitude >= avoidRadiusSqr)
                {
                    pos = new Vector3(x, 0.35f, z);
                    break;
                }

                tries++;
            } while (tries < maxTries);

            // If we somehow failed to find a spot after several tries, just use the last position.
            if (pos == Vector3.zero)
            {
                float r = Mathf.Sqrt(Random.value) * (tubRadius * 0.8f);
                float angle = Random.value * Mathf.PI * 2f;
                pos = new Vector3(
                    Mathf.Cos(angle) * r,
                    0.35f,
                    Mathf.Sin(angle) * r
                );
            }

            // Spawn facing a random horizontal direction so swimming follows rotation.
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Decide which duck type to spawn (normal vs Fire Duck).
            Duck prefabToUse = duckPrefab;
            if (fireDuckPrefab != null && Random.value < fireDuckChance)
            {
                prefabToUse = fireDuckPrefab;
            }

            Duck d = Instantiate(prefabToUse, pos, rot);

            // Apply global duck size multiplier.
            d.transform.localScale *= duckSizeMultiplier;
            Ducks.Add(d);
        }

        public void OnDuckKilled(Duck duck)
        {
            int reward = Mathf.Max(1, Mathf.RoundToInt(1f * duckGoldMultiplier));
            gold += reward;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHUD();
            }

            for (int i = 0; i < ducksPerDeath; i++)
            {
                SpawnDuck();
            }
        }

        // --- Breaker damage / crits ----------------------------------------

        public int GetBreakerDamage()
        {
            float dmg = breakerDamage;

            if (breakerCritChance > 0f && Random.value < breakerCritChance)
            {
                dmg *= (1f + breakerCritBonus);
            }

            return Mathf.Max(1, Mathf.RoundToInt(dmg));
        }

        // --- Upgrade system -------------------------------------------------

        void InitializeUpgrades()
        {
            upgrades = new Dictionary<string, UpgradeNode>();

            AddUpgrade("U1", 2, null, () =>
            {
                // Session Time +2 seconds
                sessionDuration += 2f;
            });

            AddUpgrade("U2", 2, null, () =>
            {
                // Breaker Radius +15%
                breakerRadius *= 1.15f;
            });

            AddUpgrade("U3", 6, "U1", () =>
            {
                // Breaker Damage +10%
                breakerDamage *= 1.10f;
            });

            AddUpgrade("U4", 7, null, () =>
            {
                // Max Number of ducks = 30
                maxDucks = 30;
            });

            AddUpgrade("U5", 7, "U2", () =>
            {
                // Breaker Radius +25%
                breakerRadius *= 1.25f;
            });

            AddUpgrade("U6", 18, "U3", () =>
            {
                // Breaker Damage +1
                breakerDamage += 1f;
            });

            AddUpgrade("U7", 25, "U4", () =>
            {
                // Max Number of ducks = 55
                maxDucks = 55;
            });

            AddUpgrade("U8", 30, "U7", () =>
            {
                // Breaker Speed +25%
                breakerSpeedMultiplier *= 1.25f;
            });

            AddUpgrade("U9", 20, null, () =>
            {
                // Breaker Critical Damage Chance +10%
                breakerCritChance += 0.10f;
            });

            AddUpgrade("U10", 20, "U8", () =>
            {
                // Duck Size +10%
                duckSizeMultiplier += 0.10f;
            });

            AddUpgrade("U11", 15, "U9", () =>
            {
                // Breaker Critical Damage Chance +5%
                breakerCritChance += 0.05f;
            });

            AddUpgrade("U12", 40, "U8", () =>
            {
                // Breaker Speed +25%
                breakerSpeedMultiplier *= 1.25f;
            });

            AddUpgrade("U13", 40, "U5", () =>
            {
                // Breaker Radius +25%
                breakerRadius *= 1.25f;
            });

            AddUpgrade("U14", 50, "U11", () =>
            {
                // Breaker Damage +10%
                breakerDamage *= 1.10f;
            });

            AddUpgrade("U15", 60, "U10", () =>
            {
                // Duck Size +10%
                duckSizeMultiplier += 0.10f;
            });

            AddUpgrade("U16", 60, "U15", () =>
            {
                // Breaker Speed +25%
                breakerSpeedMultiplier *= 1.25f;
            });

            AddUpgrade("U17", 60, "U16", () =>
            {
                // Duck Mass: 40% more gold awarded
                duckGoldMultiplier *= 1.4f;
            });

            AddUpgrade("U18", 80, "U17", () =>
            {
                // Breaker Damage +2
                breakerDamage += 2f;
            });

            AddUpgrade("U19", 60, "U18", () =>
            {
                // Breaker Damage +1
                breakerDamage += 1f;
            });

            AddUpgrade("U20", 60, "U16", () =>
            {
                // Breaker Critical Damage Chance +10%
                breakerCritChance += 0.10f;
            });

            AddUpgrade("U21", 65, "U16", () =>
            {
                // Breaker Critical Damage Bonus +25%
                breakerCritBonus += 0.25f;
            });

            AddUpgrade("U22", 70, "U20", () =>
            {
                // Duck Mass: 50% more gold awarded
                duckGoldMultiplier *= 1.5f;
            });
        }

        void AddUpgrade(string code, int cost, string dependencyCode, System.Action applyEffect)
        {
            upgrades[code] = new UpgradeNode
            {
                code = code,
                cost = cost,
                dependencyCode = dependencyCode,
                purchased = false,
                applyEffect = applyEffect
            };
        }

        public bool IsUpgradePurchased(string code)
        {
            return upgrades != null &&
                   upgrades.TryGetValue(code, out var u) &&
                   u.purchased;
        }

        public bool IsUpgradeAvailable(string code)
        {
            if (upgrades == null || !upgrades.TryGetValue(code, out var u)) return false;
            if (u.purchased) return false;

            if (!string.IsNullOrEmpty(u.dependencyCode))
            {
                if (!upgrades.TryGetValue(u.dependencyCode, out var dep) || !dep.purchased)
                    return false;
            }

            return true;
        }

        public int GetUpgradeCost(string code)
        {
            if (upgrades != null && upgrades.TryGetValue(code, out var u))
            {
                return u.cost;
            }

            return int.MaxValue;
        }

        public bool PurchaseUpgrade(string code)
        {
            if (upgrades == null || !upgrades.TryGetValue(code, out var u)) return false;
            if (u.purchased) return false;

            if (!string.IsNullOrEmpty(u.dependencyCode))
            {
                if (!upgrades.TryGetValue(u.dependencyCode, out var dep) || !dep.purchased)
                    return false;
            }

            if (gold < u.cost) return false;

            gold -= u.cost;
            u.applyEffect?.Invoke();
            u.purchased = true;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHUD();
            }

            return true;
        }
    }
}

