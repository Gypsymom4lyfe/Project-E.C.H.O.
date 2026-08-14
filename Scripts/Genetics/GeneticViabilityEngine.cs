using System;
using System.Collections.Generic;
using UnityEngine;
using GenesisAR.Genetics;

namespace GenesisAR.Genetics
{
    public enum ViabilityStatus
    {
        Viable,               // Healthy, fully functional organism
        GeneticallyUnstable,  // Viable, but suffers reduced lifespan or frequent mutations
        LethalNonViable       // Cannot survive incubation (non-viable)
    }

    [Serializable]
    public class ViabilityDiagnosticReport
    {
        public ViabilityStatus status;
        public float viabilityScore; // 0.0 (Lethal) to 1.0 (Optimal)
        public List<string> fatalFlaws = new List<string>();
        public List<string> instabilityWarnings = new List<string>();
    }

    public class GeneticViabilityEngine : MonoBehaviour
    {
        public static GeneticViabilityEngine Instance { get; private set; }

        [Header("Viability Thresholds")]
        [Range(0f, 1f)] public float minViableScoreThreshold = 0.45f;
        public int maxConflictingGenes = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Evaluates whether a spliced genome represents Viable or Non-Viable Life
        /// using real biological rules: thermal conflict, square-cube law, and genetic degeneracy.
        /// </summary>
        public ViabilityDiagnosticReport EvaluateGenomeViability(Genome genome)
        {
            ViabilityDiagnosticReport report = new ViabilityDiagnosticReport
            {
                status = ViabilityStatus.Viable,
                viabilityScore = 1.0f
            };

            if (genome == null || genome.sequence.Count == 0)
            {
                report.status = ViabilityStatus.LethalNonViable;
                report.viabilityScore = 0f;
                report.fatalFlaws.Add("Empty Genome: No genetic material present.");
                return report;
            }

            // =========================================================================
            // RULE 1: THERMAL CONFLICT (e.g., Extreme Heat + Arctic Ice Genes)
            // =========================================================================
            bool hasHyperThermal = false;
            bool hasDeepArctic = false;

            foreach (Gene gene in genome.sequence)
            {
                if (gene.geneName.Contains("Hyper-Thermal") || gene.geneName.Contains("Vulcan"))
                    hasHyperThermal = true;
                if (gene.geneName.Contains("Subcutaneous Fat") || gene.geneName.Contains("Cryo"))
                    hasDeepArctic = true;
            }

            if (hasHyperThermal && hasDeepArctic)
            {
                report.viabilityScore -= 0.35f;
                report.fatalFlaws.Add("Lethal Thermal Conflict: Hyper-thermal enzyme synthesis incompatible with deep cryo-fat protein structures.");
            }

            // =========================================================================
            // RULE 2: SQUARE-CUBE LAW (Mass vs Locomotion Power)
            // =========================================================================
            float bodyScaleValue = 0f;
            float locomotionValue = 0f;

            foreach (Gene gene in genome.sequence)
            {
                if (gene.category == TraitCategory.BodyScale) bodyScaleValue += gene.value;
                if (gene.category == TraitCategory.Locomotion) locomotionValue += gene.value;
            }

            // Massive creatures with insufficient muscle/locomotion structure collapse under weight
            if (bodyScaleValue > 2.0f && locomotionValue < 0.2f)
            {
                report.viabilityScore -= 0.40f;
                report.fatalFlaws.Add("Biomechanical Structural Failure: Mass exceeds skeletal support capacity (Square-Cube Law violation).");
            }

            // =========================================================================
            // RULE 3: GENETIC DEGENERACY / OVERLOAD (Too many conflicting traits)
            // =========================================================================
            if (genome.sequence.Count > 12)
            {
                report.viabilityScore -= 0.20f;
                report.instabilityWarnings.Add("High Genetic Load: Splicing density exceeds chromosome stability limits.");
            }

            // =========================================================================
            // FINAL STATUS DETERMINATION
            // =========================================================================
            if (report.fatalFlaws.Count > 0 || report.viabilityScore < minViableScoreThreshold)
            {
                report.status = ViabilityStatus.LethalNonViable;
            }
            else if (report.instabilityWarnings.Count > 0 || report.viabilityScore < 0.75f)
            {
                report.status = ViabilityStatus.GeneticallyUnstable;
            }
            else
            {
                report.status = ViabilityStatus.Viable;
            }

            Debug.Log($"[Viability Engine] Genome Evaluated: Status={report.status}, Score={report.viabilityScore:F2}");
            return report;
        }
    }
}
