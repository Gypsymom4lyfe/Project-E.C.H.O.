using System.Collections.Generic;
using System.Linq;

namespace GenesisAR.Genetics
{
    /// <summary>
    /// Container for the full ordered sequence of genes that make up a creature's genome.
    /// </summary>
    [System.Serializable]
    public class Genome
    {
        /// <summary>Ordered list of genes in this genome.</summary>
        public List<Gene> sequence = new List<Gene>();

        /// <summary>
        /// Returns all genes that belong to the given category,
        /// with dominant genes sorted first.
        /// </summary>
        public List<Gene> GetTraits(TraitCategory category)
        {
            return sequence
                .Where(g => g.category == category)
                .OrderByDescending(g => g.isDominant)
                .ToList();
        }

        /// <summary>
        /// Returns the single most-expressive (dominant-first, then highest value)
        /// gene for the given category, or null if none exist.
        /// </summary>
        public Gene GetDominantTrait(TraitCategory category)
        {
            return sequence
                .Where(g => g.category == category)
                .OrderByDescending(g => g.isDominant)
                .ThenByDescending(g => g.value)
                .FirstOrDefault();
        }
    }
}
