using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisAR.Ecosystem
{
    // ─────────────────────────────────────────────────────────────
    //  Biome – defines carrying capacities and environmental factors
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class Biome
    {
        public string Name;
        public float PreyCarryingCapacity   = 200f;
        public float PredatorCarryingCapacity = 50f;
        public float PlantBiomass           = 1000f;  // available food for prey
        public float Temperature            = 20f;    // celsius
        public float Humidity               = 0.5f;   // 0-1
    }

    // ─────────────────────────────────────────────────────────────
    //  PopulationRecord – current state of a species population
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class PopulationRecord
    {
        public string SpeciesID;
        public bool IsPredator;
        public float Population;
        public float BirthRate;
        public float DeathRate;
    }

    // ─────────────────────────────────────────────────────────────
    //  PlanetEcosystemManager – Lotka-Volterra predator/prey dynamics
    //  dPrey/dt   = α·Prey  – β·Prey·Predator
    //  dPred/dt   = δ·Prey·Predator – γ·Predator
    // ─────────────────────────────────────────────────────────────
    public class PlanetEcosystemManager : MonoBehaviour
    {
        [Header("Simulation Settings")]
        [Tooltip("Simulation time-step in real seconds")]
        public float TickInterval = 1.0f;

        [Header("Lotka-Volterra Coefficients")]
        [Tooltip("α – prey natural growth rate")]
        public float PreyGrowthRate    = 0.10f;
        [Tooltip("β – predation rate")]
        public float PredationRate     = 0.005f;
        [Tooltip("δ – predator efficiency")]
        public float PredatorEfficiency = 0.002f;
        [Tooltip("γ – predator natural death rate")]
        public float PredatorDeathRate = 0.08f;

        [Header("Active Biome")]
        public Biome CurrentBiome = new Biome { Name = "Temperate Forest" };

        [Header("Populations")]
        public List<PopulationRecord> Populations = new List<PopulationRecord>();

        // Events so UI/telemetry can subscribe
        public static event Action<List<PopulationRecord>> OnTickComplete;
        public static event Action<string> OnSpeciesExtinct;

        private Coroutine _simCoroutine;

        private void Start()
        {
            SeedDefaultPopulations();
            _simCoroutine = StartCoroutine(SimulationLoop());
        }

        private void OnDestroy()
        {
            if (_simCoroutine != null) StopCoroutine(_simCoroutine);
        }

        // ── Default seed populations ──────────────────────────────
        private void SeedDefaultPopulations()
        {
            if (Populations.Count > 0) return;
            Populations.Add(new PopulationRecord { SpeciesID = "deer_whitetail", IsPredator = false, Population = 100f });
            Populations.Add(new PopulationRecord { SpeciesID = "wolf_grey",      IsPredator = true,  Population = 20f  });
            Populations.Add(new PopulationRecord { SpeciesID = "rabbit_cottontail", IsPredator = false, Population = 300f });
        }

        // ── Main simulation coroutine ─────────────────────────────
        private IEnumerator SimulationLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(TickInterval);
                StepSimulation();
            }
        }

        private void StepSimulation()
        {
            // Sum prey and predator totals for cross-species interaction
            float totalPrey     = 0f;
            float totalPredator = 0f;

            foreach (PopulationRecord rec in Populations)
            {
                if (rec.IsPredator) totalPredator += rec.Population;
                else                totalPrey     += rec.Population;
            }

            List<PopulationRecord> extinct = new List<PopulationRecord>();

            foreach (PopulationRecord rec in Populations)
            {
                float pop = rec.Population;
                float delta;

                if (!rec.IsPredator)
                {
                    // Lotka-Volterra prey equation with carrying-capacity cap
                    float capacityFactor = 1f - (totalPrey / CurrentBiome.PreyCarryingCapacity);
                    delta = PreyGrowthRate * pop * capacityFactor
                          - PredationRate  * pop * totalPredator;
                }
                else
                {
                    // Lotka-Volterra predator equation with carrying-capacity cap
                    float capacityFactor = 1f - (totalPredator / CurrentBiome.PredatorCarryingCapacity);
                    delta = PredatorEfficiency * totalPrey * pop * capacityFactor
                          - PredatorDeathRate  * pop;
                }

                rec.Population = Mathf.Max(0f, pop + delta);
                rec.BirthRate  = Mathf.Max(0f,  delta);
                rec.DeathRate  = Mathf.Max(0f, -delta);

                if (rec.Population < 1f)
                    extinct.Add(rec);
            }

            foreach (PopulationRecord e in extinct)
            {
                Debug.LogWarning($"[EcosystemManager] Species EXTINCT: {e.SpeciesID}");
                OnSpeciesExtinct?.Invoke(e.SpeciesID);
                Populations.Remove(e);
            }

            OnTickComplete?.Invoke(Populations);
        }

        // ── Public API ────────────────────────────────────────────
        public void AddSpecies(string speciesID, bool isPredator, float initialPop)
        {
            Populations.Add(new PopulationRecord
            {
                SpeciesID  = speciesID,
                IsPredator = isPredator,
                Population = initialPop
            });
        }

        public void RemoveSpecies(string speciesID) =>
            Populations.RemoveAll(r => r.SpeciesID == speciesID);

        public PopulationRecord GetRecord(string speciesID) =>
            Populations.Find(r => r.SpeciesID == speciesID);

        public void ChangeBiome(Biome biome)
        {
            CurrentBiome = biome;
            Debug.Log($"[EcosystemManager] Biome changed to: {biome.Name}");
        }
    }
}
