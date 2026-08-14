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

            // Choose a random cut point within the shared region.
            // When minLength == 0 the cut is 0, so we fall straight through to the tail.
            int cutPoint = minLength > 1 ? UnityEngine.Random.Range(1, minLength) : 0;

            // Front segment [0, cutPoint) from parentA; back segment [cutPoint, minLength) from parentB.
            for (int i = 0; i < minLength; i++)
            {
                Gene sourceGene = i < cutPoint ? parentA.sequence[i] : parentB.sequence[i];
                if (sourceGene == null) continue;

                Gene newGene = new Gene
                {
                    geneName     = sourceGene.geneName,
                    category     = sourceGene.category,
                    value        = sourceGene.value,
                    isDominant   = sourceGene.isDominant,
                    expressColor = sourceGene.expressColor
                };

                if (UnityEngine.Random.value < mutationRate)
                {
                    newGene.value = Mathf.Clamp01(newGene.value + UnityEngine.Random.Range(-0.15f, 0.15f));
                    Debug.Log($"[Genesis AR] Mutation occurred in gene: {newGene.geneName}");
                }

                childGenome.sequence.Add(newGene);
            }

            // Always carry over tail genes from the longer parent.
            Genome longerParent = parentA.sequence.Count > parentB.sequence.Count ? parentA : parentB;
            if (longerParent.sequence.Count > minLength)
            {
                for (int i = minLength; i < longerParent.sequence.Count; i++)
                {
                    Gene tailGene = longerParent.sequence[i];
                    if (tailGene == null) continue;

                    Gene newTailGene = new Gene
                    {
                        geneName     = tailGene.geneName,
                        category     = tailGene.category,
                        value        = tailGene.value,
                        isDominant   = tailGene.isDominant,
                        expressColor = tailGene.expressColor
                    };

                    if (UnityEngine.Random.value < mutationRate)
                    {
                        newTailGene.value = Mathf.Clamp01(newTailGene.value + UnityEngine.Random.Range(-0.15f, 0.15f));
                        Debug.Log($"[Genesis AR] Mutation occurred in tail gene: {newTailGene.geneName}");
                    }

                    childGenome.sequence.Add(newTailGene);
                }
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
