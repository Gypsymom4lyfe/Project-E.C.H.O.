using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisAR.Genetics
{
    // ─────────────────────────────────────────────────────────────
    //  Gene – a single heritable unit with a name, value, and dominance
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class Gene
    {
        public string Name;
        [Range(0f, 1f)] public float Value;   // normalised allele expression
        public bool IsDominant;

        public Gene(string name, float value, bool isDominant = false)
        {
            Name = name;
            Value = value;
            IsDominant = isDominant;
        }

        public Gene Clone() => new Gene(Name, Value, IsDominant);
    }

    // ─────────────────────────────────────────────────────────────
    //  Genome – a complete set of genes organised into chromosomes
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class Genome
    {
        public string SpeciesID;
        public List<Gene> MaternalChromosome = new List<Gene>();
        public List<Gene> PaternalChromosome = new List<Gene>();

        public Genome(string speciesID)
        {
            SpeciesID = speciesID;
        }

        // Returns the expressed (phenotype) value of a gene after dominance rules
        public float GetExpressedValue(string geneName)
        {
            Gene maternal = MaternalChromosome.Find(g => g.Name == geneName);
            Gene paternal = PaternalChromosome.Find(g => g.Name == geneName);

            if (maternal == null && paternal == null) return 0f;
            if (maternal == null) return paternal.Value;
            if (paternal == null) return maternal.Value;

            // Dominant gene wins; if both dominant or both recessive, average
            if (maternal.IsDominant && !paternal.IsDominant) return maternal.Value;
            if (paternal.IsDominant && !maternal.IsDominant) return paternal.Value;
            return (maternal.Value + paternal.Value) * 0.5f;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Phenotype – the observable characteristics derived from a Genome
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class Phenotype
    {
        public float BodySize;
        public Color PrimaryColor;
        public float Camouflage;
        public float Aggression;
        public float Fertility;
        public float Longevity;
        public float MetabolicRate;
        public float SpeedMultiplier;

        public override string ToString() =>
            $"Size={BodySize:F2} Aggression={Aggression:F2} Fertility={Fertility:F2} " +
            $"Longevity={Longevity:F2} Speed={SpeedMultiplier:F2}";
    }

    // ─────────────────────────────────────────────────────────────
    //  GeneticsSystem – crossover, mutation, and phenotype expression
    // ─────────────────────────────────────────────────────────────
    public static class GeneticsSystem
    {
        public static float MutationRate = 0.02f;   // 2 % per gene per generation
        public static float MutationStrength = 0.1f; // max deviation per mutation

        // Mendelian single-point crossover
        public static Genome CrossOver(Genome parentA, Genome parentB)
        {
            if (parentA.SpeciesID != parentB.SpeciesID)
                throw new InvalidOperationException(
                    $"Cannot cross species '{parentA.SpeciesID}' with '{parentB.SpeciesID}'.");

            Genome offspring = new Genome(parentA.SpeciesID);
            int crossPoint = UnityEngine.Random.Range(0, parentA.MaternalChromosome.Count);

            for (int i = 0; i < parentA.MaternalChromosome.Count; i++)
            {
                // Offspring maternal = A's maternal up to crosspoint, then B's paternal
                Gene mat = i < crossPoint
                    ? parentA.MaternalChromosome[i].Clone()
                    : parentB.PaternalChromosome[i].Clone();

                // Offspring paternal = B's maternal up to crosspoint, then A's paternal
                Gene pat = i < crossPoint
                    ? parentB.MaternalChromosome[i].Clone()
                    : parentA.PaternalChromosome[i].Clone();

                Mutate(ref mat);
                Mutate(ref pat);

                offspring.MaternalChromosome.Add(mat);
                offspring.PaternalChromosome.Add(pat);
            }

            return offspring;
        }

        // Apply random point mutation to a gene
        private static void Mutate(ref Gene gene)
        {
            if (UnityEngine.Random.value < MutationRate)
            {
                float delta = UnityEngine.Random.Range(-MutationStrength, MutationStrength);
                gene.Value = Mathf.Clamp01(gene.Value + delta);
            }
        }

        // Express a Genome into a Phenotype
        public static Phenotype ExpressPhenotype(Genome genome)
        {
            return new Phenotype
            {
                BodySize        = genome.GetExpressedValue("BodySize"),
                Camouflage      = genome.GetExpressedValue("Camouflage"),
                Aggression      = genome.GetExpressedValue("Aggression"),
                Fertility       = genome.GetExpressedValue("Fertility"),
                Longevity       = genome.GetExpressedValue("Longevity"),
                MetabolicRate   = genome.GetExpressedValue("MetabolicRate"),
                SpeedMultiplier = genome.GetExpressedValue("SpeedMultiplier"),
                PrimaryColor    = new Color(
                    genome.GetExpressedValue("ColorR"),
                    genome.GetExpressedValue("ColorG"),
                    genome.GetExpressedValue("ColorB"))
            };
        }
    }
}
