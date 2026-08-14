using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GenesisAR.Genetics
{
    // ─────────────────────────────────────────────────────────────
    //  ViabilityResult – outcome of a viability assessment
    // ─────────────────────────────────────────────────────────────
    public enum ViabilityStatus { Viable, Marginal, NonViable }

    [System.Serializable]
    public class ViabilityResult
    {
        public ViabilityStatus Status;
        public float Score;             // 0-1 composite score
        public List<string> Warnings = new List<string>();
        public List<string> Errors   = new List<string>();

        public string DiagnosticReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== Viability Report ===");
            sb.AppendLine($"Status : {Status}");
            sb.AppendLine($"Score  : {Score:P0}");

            if (Errors.Count > 0)
            {
                sb.AppendLine("-- ERRORS --");
                foreach (string e in Errors) sb.AppendLine($"  ✗ {e}");
            }
            if (Warnings.Count > 0)
            {
                sb.AppendLine("-- WARNINGS --");
                foreach (string w in Warnings) sb.AppendLine($"  ⚠ {w}");
            }
            return sb.ToString();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  GeneticViabilityEngine – applies biological plausibility rules
    // ─────────────────────────────────────────────────────────────
    public static class GeneticViabilityEngine
    {
        // Thresholds
        private const float MinFertility    = 0.05f;
        private const float MinMetabolicRate = 0.05f;
        private const float MaxAggressionForSmall = 0.30f; // small creatures can't be hyper-aggressive
        private const float SmallBodyThreshold    = 0.10f;
        private const float LethalMetabolicRate   = 0.98f;
        private const float LowLongevityThreshold = 0.08f;
        private const float HighSizeHighSpeed     = 0.80f; // very large things can't be very fast

        public static ViabilityResult Assess(Genome genome)
        {
            Phenotype p = GeneticsSystem.ExpressPhenotype(genome);
            ViabilityResult result = new ViabilityResult();
            float score = 1.0f;

            // ── Hard failure rules (Non-Viable) ──────────────────
            if (p.Fertility < MinFertility)
            {
                result.Errors.Add($"Fertility {p.Fertility:F2} is below minimum {MinFertility:F2} – species cannot reproduce.");
                score -= 0.40f;
            }

            if (p.MetabolicRate > LethalMetabolicRate)
            {
                result.Errors.Add($"Metabolic rate {p.MetabolicRate:F2} is lethally high – organism burns out instantly.");
                score -= 0.40f;
            }

            if (p.Longevity < LowLongevityThreshold)
            {
                result.Errors.Add($"Longevity {p.Longevity:F2} too low – lifespan insufficient for reproduction.");
                score -= 0.30f;
            }

            // ── Warning rules (Marginal) ──────────────────────────
            if (p.BodySize < SmallBodyThreshold && p.Aggression > MaxAggressionForSmall)
            {
                result.Warnings.Add($"High aggression ({p.Aggression:F2}) in a very small body ({p.BodySize:F2}) is energetically unsustainable.");
                score -= 0.10f;
            }

            if (p.BodySize > HighSizeHighSpeed && p.SpeedMultiplier > HighSizeHighSpeed)
            {
                result.Warnings.Add($"Extremely large ({p.BodySize:F2}) and extremely fast ({p.SpeedMultiplier:F2}) simultaneously is physically implausible.");
                score -= 0.15f;
            }

            if (p.MetabolicRate < MinMetabolicRate)
            {
                result.Warnings.Add($"Metabolic rate {p.MetabolicRate:F2} is critically low – organism may enter permanent stasis.");
                score -= 0.10f;
            }

            if (p.Camouflage > 0.90f && p.PrimaryColor.r + p.PrimaryColor.g + p.PrimaryColor.b > 2.5f)
            {
                result.Warnings.Add("High camouflage gene conflicts with near-white pigmentation.");
                score -= 0.05f;
            }

            // ── Determine final status ────────────────────────────
            result.Score = Mathf.Clamp01(score);

            if (result.Errors.Count > 0 || result.Score < 0.35f)
                result.Status = ViabilityStatus.NonViable;
            else if (result.Warnings.Count > 0 || result.Score < 0.70f)
                result.Status = ViabilityStatus.Marginal;
            else
                result.Status = ViabilityStatus.Viable;

            return result;
        }

        // Batch-assess a list of genomes and log results
        public static List<ViabilityResult> BatchAssess(IEnumerable<Genome> genomes)
        {
            List<ViabilityResult> results = new List<ViabilityResult>();
            foreach (Genome g in genomes)
            {
                ViabilityResult r = Assess(g);
                results.Add(r);
                if (r.Status != ViabilityStatus.Viable)
                    Debug.LogWarning($"[ViabilityEngine] {g.SpeciesID}: {r.DiagnosticReport()}");
            }
            return results;
        }
    }
}
