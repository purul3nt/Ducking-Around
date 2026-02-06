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

        [Header("Session")]
        [Tooltip("Length of a single session in seconds.")]
        public float sessionDuration = 10f;

        [HideInInspector] public float timeLeft;
        [HideInInspector] public bool sessionActive = true;

        [Header("Economy")]
        [HideInInspector] public int gold = 0;

        [Header("Duck settings")]
        public Duck duckPrefab;
        [Tooltip("Initial number of ducks to spawn at the start of a session.")]
        public int initialDuckCount = 4;
        [Tooltip("Radius of the hot tub in world units, used for random spawn positions.")]
        public float tubRadius = 5f;

        [Header("Upgrades (runtime values)")]
        [Tooltip("Current breaker radius in world units on the water plane.")]
        public float breakerRadius = 1.4f;
        [Tooltip("Increase to breaker radius per upgrade level.")]
        public float breakerRadiusIncrement = 0.4f;
        [Tooltip("Number of ducks to spawn when one dies.")]
        public int ducksPerDeath = 1;

        [HideInInspector] public int breakerLevel = 0;
        [HideInInspector] public int ducksUpgradeLevel = 0;

        [Header("Upgrade costs")]
        public int breakerBaseCost = 5;
        public float breakerCostMultiplier = 1.7f;

        public int ducksBaseCost = 10;
        public float ducksCostMultiplier = 1.8f;

        /// <summary>
        /// All currently alive ducks in the scene.
        /// </summary>
        public List<Duck> Ducks { get; private set; } = new List<Duck>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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

            // Random point inside a circle in XZ plane.
            float r = Mathf.Sqrt(Random.value) * (tubRadius * 0.8f);
            float angle = Random.value * Mathf.PI * 2f;

            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * r,
                0.35f,
                Mathf.Sin(angle) * r
            );

            // Spawn facing a random horizontal direction so swimming follows rotation.
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Duck d = Instantiate(duckPrefab, pos, rot);
            Ducks.Add(d);
        }

        public void OnDuckKilled(Duck duck)
        {
            gold += 1;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHUD();
            }

            for (int i = 0; i < ducksPerDeath; i++)
            {
                SpawnDuck();
            }
        }

        public int GetBreakerUpgradeCost()
        {
            return Mathf.RoundToInt(
                breakerBaseCost * Mathf.Pow(breakerCostMultiplier, breakerLevel)
            );
        }

        public int GetDucksUpgradeCost()
        {
            return Mathf.RoundToInt(
                ducksBaseCost * Mathf.Pow(ducksCostMultiplier, ducksUpgradeLevel)
            );
        }

        public void UpgradeBreaker()
        {
            int cost = GetBreakerUpgradeCost();
            if (gold < cost) return;

            gold -= cost;
            breakerLevel += 1;
            breakerRadius += breakerRadiusIncrement;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHUD();
            }
        }

        public void UpgradeDucks()
        {
            int cost = GetDucksUpgradeCost();
            if (gold < cost) return;

            gold -= cost;
            ducksUpgradeLevel += 1;
            ducksPerDeath += 1;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHUD();
            }
        }
    }
}

