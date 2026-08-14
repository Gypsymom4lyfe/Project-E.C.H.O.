using System.Collections.Generic;
using UnityEngine;

namespace GenesisAR.Ecosystem
{
    /// <summary>
    /// Manages all biome regions on the holographic planet.
    /// Acts as the single source of truth for biome state; other systems read from this component.
    /// Attach to the Holographic Planet parent GameObject.
    /// </summary>
    public class PlanetEcosystemManager : MonoBehaviour
    {
        [Header("Biome Configuration")]
        [Tooltip("All biomes tracked on this planet. Configure via Inspector or populate at runtime.")]
        [SerializeField] private List<BiomeData> biomes = new List<BiomeData>();

        [Header("Simulation Settings")]
        [Tooltip("Automatically simulate population/health drift for demo purposes.")]
        [SerializeField] private bool simulateDrift = true;

        [Tooltip("How often (seconds) drift values are recalculated.")]
        [SerializeField] private float driftInterval = 5f;

        private float _driftTimer;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns a read-only view of all registered biomes.</summary>
        public IReadOnlyList<BiomeData> Biomes => biomes;

        /// <summary>Returns the biome at <paramref name="index"/>, or null if out of range.</summary>
        public BiomeData GetBiome(int index) =>
            (index >= 0 && index < biomes.Count) ? biomes[index] : null;

        /// <summary>
        /// Updates a biome's live metrics. Call this from telemetry services or game logic.
        /// </summary>
        public void UpdateBiomeMetrics(int index, int population, float healthScore,
                                       float temperatureCelsius, float humidity, int speciesCount)
        {
            if (index < 0 || index >= biomes.Count)
            {
                Debug.LogWarning($"[PlanetEcosystemManager] Biome index {index} is out of range.");
                return;
            }

            BiomeData b = biomes[index];
            b.population = Mathf.Max(0, population);
            b.healthScore = Mathf.Clamp01(healthScore);
            b.temperatureCelsius = temperatureCelsius;
            b.humidity = Mathf.Clamp(humidity, 0f, 100f);
            b.speciesCount = Mathf.Max(0, speciesCount);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            if (!simulateDrift) return;

            _driftTimer += Time.deltaTime;
            if (_driftTimer >= driftInterval)
            {
                _driftTimer = 0f;
                ApplySimulatedDrift();
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplySimulatedDrift()
        {
            foreach (BiomeData b in biomes)
            {
                // Small random drift so HUD labels visibly update during demos.
                b.healthScore = Mathf.Clamp01(b.healthScore + Random.Range(-0.05f, 0.05f));
                b.population = Mathf.Max(0, b.population + Random.Range(-10, 11));
                b.temperatureCelsius += Random.Range(-0.5f, 0.5f);
                b.humidity = Mathf.Clamp(b.humidity + Random.Range(-2f, 2f), 0f, 100f);
            }
        }
    }
}
