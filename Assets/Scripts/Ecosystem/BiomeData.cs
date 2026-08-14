using UnityEngine;

namespace GenesisAR.Ecosystem
{
    /// <summary>
    /// Represents a single biome region on the holographic planet.
    /// </summary>
    [System.Serializable]
    public class BiomeData
    {
        /// <summary>Human-readable biome name (e.g. "Arctic Tundra").</summary>
        public string biomeName;

        /// <summary>World-space anchor position on the planet surface for this biome.</summary>
        public Vector3 worldPosition;

        /// <summary>Current population count of creatures in this biome.</summary>
        public int population;

        /// <summary>
        /// Overall health score in [0, 1].
        /// 0 = critically endangered, 1 = thriving.
        /// </summary>
        [Range(0f, 1f)]
        public float healthScore;

        /// <summary>Average temperature in degrees Celsius.</summary>
        public float temperatureCelsius;

        /// <summary>Humidity percentage in [0, 100].</summary>
        [Range(0f, 100f)]
        public float humidity;

        /// <summary>Biodiversity index: number of distinct species present.</summary>
        public int speciesCount;
    }
}
