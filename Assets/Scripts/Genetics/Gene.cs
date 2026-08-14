using UnityEngine;

namespace GenesisAR.Genetics
{
    /// <summary>
    /// A single heritable unit of information that influences a creature's phenotype.
    /// </summary>
    [System.Serializable]
    public class Gene
    {
        /// <summary>Human-readable name of this gene (e.g. "Giant Scale").</summary>
        public string geneName;

        /// <summary>Trait domain this gene belongs to.</summary>
        public TraitCategory category;

        /// <summary>
        /// Normalized expression strength [0, 1].
        /// For scale traits this multiplies the base size;
        /// for color/bioluminescence traits this controls intensity.
        /// </summary>
        [Range(0f, 1f)]
        public float value;

        /// <summary>
        /// Whether this allele is dominant.  A dominant gene overrides a
        /// recessive counterpart when both are present in the genome.
        /// </summary>
        public bool isDominant;

        /// <summary>
        /// Optional color payload used by BodyColor and Bioluminescence genes.
        /// </summary>
        public Color expressColor = Color.white;
    }
}
