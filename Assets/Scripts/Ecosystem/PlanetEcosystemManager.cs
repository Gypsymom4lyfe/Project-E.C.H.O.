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
        /// Returns a human-readable summary of current atmospheric/weather conditions
        /// across all biomes (average temperature and humidity).
        /// Returns null if no biomes are registered.
        /// </summary>
        public string GetCurrentWeatherSummary()
        {
            if (biomes == null || biomes.Count == 0) return null;

            float totalTemp = 0f;
            float totalHumidity = 0f;
            int count = 0;

            foreach (BiomeData b in biomes)
            {
                if (b == null) continue;
                totalTemp += b.temperatureCelsius;
                totalHumidity += b.humidity;
                count++;
            }

            if (count == 0) return null;

            float avgTemp = totalTemp / count;
            float avgHumidity = totalHumidity / count;
            string condition = avgTemp > 30f ? "Hot" : avgTemp < 0f ? "Freezing" : "Temperate";

            return $"Conditions: {condition} | Avg Temp: {avgTemp:F1}°C | Avg Humidity: {avgHumidity:F1}% ({count} biomes sampled)";
        }

        /// <summary>
        /// Returns a human-readable summary of current water/humidity conditions
        /// across all biomes, highlighting the wettest and driest regions.
        /// Returns null if no biomes are registered.
        /// </summary>
        public string GetCurrentWaterSummary()
        {
            if (biomes == null || biomes.Count == 0) return null;

            BiomeData wettest = null;
            BiomeData driest = null;
            float totalHumidity = 0f;
            int count = 0;

            foreach (BiomeData b in biomes)
            {
                if (b == null) continue;
                totalHumidity += b.humidity;
                count++;

                if (wettest == null || b.humidity > wettest.humidity) wettest = b;
                if (driest == null || b.humidity < driest.humidity) driest = b;
            }

            if (count == 0) return null;

            float avgHumidity = totalHumidity / count;

            return $"Avg Humidity: {avgHumidity:F1}% | Wettest: {wettest.biomeName} ({wettest.humidity:F1}%) | Driest: {driest.biomeName} ({driest.humidity:F1}%)";
        }

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
