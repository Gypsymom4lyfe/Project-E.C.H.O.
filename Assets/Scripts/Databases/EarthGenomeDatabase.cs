using System;
using System.Collections.Generic;
using UnityEngine;
using GenesisAR.Genetics;

namespace GenesisAR.Databases
{
    public enum EarthOrganismKingdom
    {
        Animal,
        Plant,
        InsectBug,
        Microorganism
    }

    [Serializable]
    public class EarthOrganismSpecimen
    {
        public string commonName;
        public string scientificName;
        public EarthOrganismKingdom kingdom;
        [TextArea(2, 4)] public string biologicalOverview;

        [Header("Environmental Tolerances")]
        public float optimalTemperatureC = 20f;
        public float humidityTolerance = 0.5f; // 0.0 (Arid) to 1.0 (Aquatic)

        [Header("Genetic Blueprint")]
        public List<Gene> extractedGenes = new List<Gene>();
    }

    public class EarthGenomeDatabase : MonoBehaviour
    {
        public static EarthGenomeDatabase Instance { get; private set; }

        [Header("Master Earth Organism Library")]
        public List<EarthOrganismSpecimen> masterLibrary = new List<EarthOrganismSpecimen>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeEarthDatabase();
        }

        /// <summary>
        /// Populates the master library with pre-indexed Earth organism genetic profiles.
        /// </summary>
        private void InitializeEarthDatabase()
        {
            masterLibrary.Clear();

            // =========================================================================
            // 1. KINGDOM: ANIMALS
            // =========================================================================
            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Cheetah",
                scientificName = "Acinonyx jubatus",
                kingdom = EarthOrganismKingdom.Animal,
                biologicalOverview = "Fastest land mammal. Specialized non-retractable claws and lightweight skeletal structure.",
                optimalTemperatureC = 30f,
                humidityTolerance = 0.3f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Fast-Twitch Muscle Fibers",   category = TraitCategory.Locomotion,  value = 0.95f, isDominant = true },
                    new Gene { geneName = "Flexible Spine Scale",         category = TraitCategory.BodyScale,   value = 0.6f,  isDominant = false },
                    new Gene { geneName = "Predator Aggression Drive",    category = TraitCategory.Aggression,  value = 0.8f,  isDominant = true }
                }
            });

            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Emperor Penguin",
                scientificName = "Aptenodytes forsteri",
                kingdom = EarthOrganismKingdom.Animal,
                biologicalOverview = "Extreme cold adaptation, high-density blubber insulation, and hydrodynamic swimming body.",
                optimalTemperatureC = -15f,
                humidityTolerance = 0.9f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Dense Subcutaneous Fat",        category = TraitCategory.ThermalReg,  value = 0.98f, isDominant = true },
                    new Gene { geneName = "Hydrodynamic Body Scaling",     category = TraitCategory.BodyScale,   value = 0.7f,  isDominant = true },
                    new Gene { geneName = "Deep Dive Bradycardia",         category = TraitCategory.Locomotion,  value = 0.85f, isDominant = false }
                }
            });

            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Chameleon",
                scientificName = "Chamaeleonidae",
                kingdom = EarthOrganismKingdom.Animal,
                biologicalOverview = "Chromatophore-based adaptive camouflage and independent stereoscopic vision.",
                optimalTemperatureC = 26f,
                humidityTolerance = 0.6f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Chromatophore Pigment Shift", category = TraitCategory.Bioluminescence, value = 0.9f, isDominant = true, expressColor = Color.green },
                    new Gene { geneName = "Low-Energy Metabolism",        category = TraitCategory.Aggression,      value = 0.2f, isDominant = false }
                }
            });

            // =========================================================================
            // 2. KINGDOM: PLANTS
            // =========================================================================
            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Giant Sequoia",
                scientificName = "Sequoiadendron giganteum",
                kingdom = EarthOrganismKingdom.Plant,
                biologicalOverview = "Massive structural trunk scaling, thick fire-resistant bark, and long-term carbon storage.",
                optimalTemperatureC = 18f,
                humidityTolerance = 0.5f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Mega-Cellular Trunk Scale", category = TraitCategory.BodyScale,  value = 1.0f,  isDominant = true },
                    new Gene { geneName = "Thick Bark Insulation",      category = TraitCategory.ThermalReg, value = 0.75f, isDominant = true }
                }
            });

            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Venus Flytrap",
                scientificName = "Dionaea muscipula",
                kingdom = EarthOrganismKingdom.Plant,
                biologicalOverview = "Carnivorous plant equipped with rapid thigmonastic snap-traps for nutrient extraction in nitrogen-poor soil.",
                optimalTemperatureC = 24f,
                humidityTolerance = 0.7f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Rapid Thigmonastic Reflex",   category = TraitCategory.Locomotion, value = 0.88f, isDominant = true },
                    new Gene { geneName = "Digestive Enzyme Secretion",  category = TraitCategory.Aggression, value = 0.7f,  isDominant = true }
                }
            });

            // =========================================================================
            // 3. KINGDOM: BUGS / INSECTS / ARACHNIDS
            // =========================================================================
            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Firefly",
                scientificName = "Lampyridae",
                kingdom = EarthOrganismKingdom.InsectBug,
                biologicalOverview = "Cold-light bioluminescence produced via luciferin-luciferase chemical reaction.",
                optimalTemperatureC = 22f,
                humidityTolerance = 0.6f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Luciferin Bioluminescence", category = TraitCategory.Bioluminescence, value = 1.0f, isDominant = true, expressColor = Color.yellow },
                    new Gene { geneName = "Chitinous Wing Scale",       category = TraitCategory.Locomotion,      value = 0.5f, isDominant = false }
                }
            });

            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Rhinoceros Beetle",
                scientificName = "Dynastinae",
                kingdom = EarthOrganismKingdom.InsectBug,
                biologicalOverview = "Proportional strength capable of lifting up to 850 times its own body weight.",
                optimalTemperatureC = 27f,
                humidityTolerance = 0.5f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Exoskeleton Density Matrix",        category = TraitCategory.BodyScale,  value = 0.85f, isDominant = true },
                    new Gene { geneName = "Extreme Torque Muscle Attachment",  category = TraitCategory.Locomotion, value = 0.95f, isDominant = true }
                }
            });

            // =========================================================================
            // 4. KINGDOM: MICROORGANISMS / EXTREMOPHILES
            // =========================================================================
            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Tardigrade (Water Bear)",
                scientificName = "Ramazzottius varieornatus",
                kingdom = EarthOrganismKingdom.Microorganism,
                biologicalOverview = "Near-indestructible extremophile capable of cryptobiosis under extreme radiation, vacuum, and temperature spikes.",
                optimalTemperatureC = 15f,
                humidityTolerance = 0.1f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Dsup (Damage Suppressor Protein)", category = TraitCategory.ThermalReg, value = 1.0f,  isDominant = true },
                    new Gene { geneName = "Anhydrobiosis Stasis Response",    category = TraitCategory.ThermalReg, value = 0.95f, isDominant = true }
                }
            });

            masterLibrary.Add(new EarthOrganismSpecimen
            {
                commonName = "Pyrococcus furiosus",
                scientificName = "Pyrococcus furiosus",
                kingdom = EarthOrganismKingdom.Microorganism,
                biologicalOverview = "Hyperthermophilic archaea living in deep ocean hydrothermal vents at temperatures above 100°C.",
                optimalTemperatureC = 100f,
                humidityTolerance = 1.0f,
                extractedGenes = new List<Gene>
                {
                    new Gene { geneName = "Hyper-Thermal Polymerase",      category = TraitCategory.ThermalReg, value = 1.0f, isDominant = true },
                    new Gene { geneName = "Sulfur Metabolism Conversion",   category = TraitCategory.Aggression, value = 0.1f, isDominant = false }
                }
            });

            Debug.Log($"[Earth Genome Database] Initialized successfully with {masterLibrary.Count} core Earth specimens.");
        }

        // =========================================================================
        // QUERY ENGINE API
        // =========================================================================

        /// <summary>
        /// Returns all specimens belonging to the specified biological kingdom.
        /// </summary>
        public List<EarthOrganismSpecimen> GetSpecimensByKingdom(EarthOrganismKingdom kingdom)
        {
            return masterLibrary.FindAll(specimen => specimen.kingdom == kingdom);
        }

        /// <summary>
        /// Scans all specimens and returns every Gene that matches the requested trait category.
        /// </summary>
        public List<Gene> ExtractGenesByTrait(TraitCategory trait)
        {
            List<Gene> matchingGenes = new List<Gene>();
            foreach (var specimen in masterLibrary)
            {
                foreach (var gene in specimen.extractedGenes)
                {
                    if (gene.category == trait)
                    {
                        matchingGenes.Add(gene);
                    }
                }
            }
            return matchingGenes;
        }
    }
}
