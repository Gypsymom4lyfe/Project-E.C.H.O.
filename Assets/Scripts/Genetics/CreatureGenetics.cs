using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisAR.Genetics
{
    public enum TraitCategory
    {
        Locomotion,
        ThermalReg,
        BodyScale,
        Bioluminescence,
        Aggression
    }

    [Serializable]
    public class Gene
    {
        public string geneName = string.Empty;
        public TraitCategory category;
        public float value;
        public bool isDominant;
        public Color expressColor;
    }

    [Serializable]
    public class Genome
    {
        public List<Gene> sequence = new List<Gene>();

        public static Genome CrossOver(Genome parentA, Genome parentB, float mutationRate = 0.05f)
        {
            if (parentA == null)
            {
                throw new ArgumentNullException(nameof(parentA));
            }

            if (parentB == null)
            {
                throw new ArgumentNullException(nameof(parentB));
            }

            Genome childGenome = new Genome();
            int minLength = Math.Min(parentA.sequence.Count, parentB.sequence.Count);

            for (int i = 0; i < minLength; i++)
            {
                Gene geneA = parentA.sequence[i];
                Gene geneB = parentB.sequence[i];
                Gene chosenGene;

                if (geneA == null && geneB == null)
                {
                    continue;
                }

                if (geneA == null)
                {
                    chosenGene = geneB;
                }
                else if (geneB == null)
                {
                    chosenGene = geneA;
                }
                else if (geneA.isDominant != geneB.isDominant)
                {
                    chosenGene = geneA.isDominant ? geneA : geneB;
                }
                else
                {
                    chosenGene = UnityEngine.Random.value > 0.5f ? geneA : geneB;
                }

                Gene newGene = new Gene
                {
                    geneName = chosenGene.geneName,
                    category = chosenGene.category,
                    value = chosenGene.value,
                    isDominant = chosenGene.isDominant,
                    expressColor = chosenGene.expressColor
                };

                if (UnityEngine.Random.value < mutationRate)
                {
                    newGene.value = Mathf.Clamp01(newGene.value + UnityEngine.Random.Range(-0.15f, 0.15f));
                    Debug.Log($"[Genesis AR] Mutation occurred in gene: {newGene.geneName}");
                }

                childGenome.sequence.Add(newGene);
            }

            return childGenome;
        }
    }

    public class CreaturePhenotype : MonoBehaviour
    {
        [Header("Genetic Profile")]
        public Genome activeGenome = new Genome();

        [Header("Expressed Physical Characteristics")]
        public float calculatedSize = 1.0f;
        public float moveSpeed = 2.0f;
        public Color primarySkinColor = Color.white;

        public void ExpressGenome()
        {
            if (activeGenome == null || activeGenome.sequence.Count == 0)
            {
                return;
            }

            float scaleModifier = 1.0f;
            float speedModifier = 1.0f;
            Color combinedColor = Color.black;
            int colorGenesCount = 0;

            foreach (Gene gene in activeGenome.sequence)
            {
                if (gene == null)
                {
                    continue;
                }

                switch (gene.category)
                {
                    case TraitCategory.BodyScale:
                        scaleModifier += gene.isDominant ? gene.value * 1.5f : gene.value * 0.75f;
                        break;
                    case TraitCategory.Locomotion:
                        speedModifier += gene.value * 3.0f;
                        break;
                    case TraitCategory.Bioluminescence:
                        combinedColor += gene.expressColor * gene.value;
                        colorGenesCount++;
                        break;
                }
            }

            calculatedSize = Mathf.Clamp(scaleModifier, 0.2f, 5.0f);
            transform.localScale = Vector3.one * calculatedSize;

            moveSpeed = Mathf.Clamp(speedModifier, 0.5f, 10.0f);

            Renderer creatureRenderer = GetComponentInChildren<Renderer>();
            if (creatureRenderer != null && colorGenesCount > 0)
            {
                primarySkinColor = combinedColor / colorGenesCount;
                creatureRenderer.material.color = primarySkinColor;
            }

            Debug.Log($"[Genesis AR] Phenotype Expressed: Size={calculatedSize}, Speed={moveSpeed}");
        }
    }
}
