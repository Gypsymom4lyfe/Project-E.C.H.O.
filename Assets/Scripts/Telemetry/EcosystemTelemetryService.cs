using System;
using System.Collections.Generic;
using UnityEngine;
using GenesisAR.Ecosystem;

namespace GenesisAR.Telemetry
{
    /// <summary>
    /// Snapshot of a single biome's telemetry at a point in time.
    /// </summary>
    public readonly struct BiomeTelemetrySnapshot
    {
        public readonly string BiomeName;
        public readonly int Population;
        public readonly float HealthScore;
        public readonly float TemperatureCelsius;
        public readonly float Humidity;
        public readonly int SpeciesCount;
        public readonly DateTime Timestamp;

        public BiomeTelemetrySnapshot(BiomeData source)
        {
            BiomeName         = source.biomeName;
            Population        = source.population;
            HealthScore       = source.healthScore;
            TemperatureCelsius = source.temperatureCelsius;
            Humidity          = source.humidity;
            SpeciesCount      = source.speciesCount;
            Timestamp         = DateTime.UtcNow;
        }
    }

    // ── Event args ────────────────────────────────────────────────────────────────

    public class TelemetryUpdatedEventArgs : EventArgs
    {
        public IReadOnlyList<BiomeTelemetrySnapshot> Snapshots { get; }

        public TelemetryUpdatedEventArgs(IReadOnlyList<BiomeTelemetrySnapshot> snapshots)
        {
            Snapshots = snapshots;
        }
    }

    // ── Service ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Polls <see cref="PlanetEcosystemManager"/> at a configurable interval and publishes
    /// telemetry snapshots for any system that needs to display or record biome metrics.
    /// </summary>
    public class EcosystemTelemetryService : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ecosystem manager whose biomes will be sampled.")]
        [SerializeField] private PlanetEcosystemManager ecosystemManager;

        [Header("Polling")]
        [Tooltip("How often (seconds) telemetry snapshots are taken.")]
        [SerializeField] private float pollInterval = 1f;

        /// <summary>Raised every time a new set of snapshots is captured.</summary>
        public event EventHandler<TelemetryUpdatedEventArgs> OnTelemetryUpdated;

        /// <summary>Most-recently captured snapshots (may be empty before first poll).</summary>
        public IReadOnlyList<BiomeTelemetrySnapshot> LatestSnapshots => _latestSnapshots;

        private readonly List<BiomeTelemetrySnapshot> _latestSnapshots = new List<BiomeTelemetrySnapshot>();
        private float _pollTimer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (ecosystemManager == null)
                ecosystemManager = GetComponentInParent<PlanetEcosystemManager>();

            if (ecosystemManager == null)
                Debug.LogError("[EcosystemTelemetryService] No PlanetEcosystemManager found. Assign it in the Inspector.");
        }

        private void Update()
        {
            if (ecosystemManager == null) return;

            _pollTimer += Time.deltaTime;
            if (_pollTimer >= pollInterval)
            {
                _pollTimer = 0f;
                PollAndPublish();
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void PollAndPublish()
        {
            _latestSnapshots.Clear();

            foreach (BiomeData biome in ecosystemManager.Biomes)
                _latestSnapshots.Add(new BiomeTelemetrySnapshot(biome));

            OnTelemetryUpdated?.Invoke(this, new TelemetryUpdatedEventArgs(_latestSnapshots));
        }
    }
}
