using System;
using System.Collections.Generic;
using UnityEngine;
using GenesisAR.Genetics;

namespace GenesisAR.Environment
{
    public enum CosmicDustType
    {
        IonizedStardust,    // Boosts bioluminescence & radiation resistance
        NebularSilicate,     // High particle density, lowers atmospheric temp
        IrradiatedDebris,    // Increases mutation rates in new genetic material
        CrystallineAuroric   // High atmospheric energy, boosts plant photosynthesis
    }

    [Serializable]
    public class CelestialBody
    {
        public string bodyName;
        public Transform bodyTransform;
        public Light bodyLightSource;
        public float orbitSpeed = 1.0f;
        public Color atmosphericColor = Color.white;
    }

    [Serializable]
    public class XenoGeneticSample
    {
        public string sampleID;
        public string sampleName;
        public CosmicDustType originDustType;
        public float radiationStability; // 0.0 (Unstable) to 1.0 (Highly Viable)
        public Gene extractedXenoGene;
    }

    public class XenoPlanetSkyAndSpaceEngine : MonoBehaviour
    {
        [Header("Celestial Tracking (Sun, Moon, Sister Planets)")]
        public CelestialBody primarySun;
        public CelestialBody secondaryMoon;
        public List<CelestialBody> nearbyPlanets = new List<CelestialBody>();

        [Header("Space Sky & Constellations")]
        public ParticleSystem spaceDustParticleSystem;
        public ParticleSystem constellationStarCluster;
        public CosmicDustType activeDustType = CosmicDustType.IonizedStardust;

        [Header("Atmospheric Ambient Light")]
        public Light mainSkyLight;
        public Gradient skyColorOverDay;

        [Header("Xeno-Genetic Material Database")]
        public List<XenoGeneticSample> gatheredXenoSamples = new List<XenoGeneticSample>();

        private float timeOfDay = 0f; // 0.0 to 1.0 (Full Day Cycle)

        private void Update()
        {
            SimulateCelestialOrbits();
            UpdateSkyAndSpaceDust();
        }

        private void SimulateCelestialOrbits()
        {
            timeOfDay += Time.deltaTime * 0.01f;
            if (timeOfDay > 1.0f) timeOfDay = 0f;

            // 1. Orbit Primary Sun
            if (primarySun.bodyTransform != null)
            {
                primarySun.bodyTransform.RotateAround(Vector3.zero, Vector3.right, primarySun.orbitSpeed * Time.deltaTime);
                primarySun.bodyTransform.LookAt(Vector3.zero);
            }

            // 2. Orbit Secondary Moon
            if (secondaryMoon.bodyTransform != null)
            {
                secondaryMoon.bodyTransform.RotateAround(Vector3.zero, Vector3.up, secondaryMoon.orbitSpeed * Time.deltaTime);
                secondaryMoon.bodyTransform.LookAt(Vector3.zero);
            }

            // 3. Orbit Neighboring Planets in the Skybox
            foreach (var planet in nearbyPlanets)
            {
                if (planet.bodyTransform != null)
                {
                    planet.bodyTransform.RotateAround(Vector3.zero, Vector3.forward, planet.orbitSpeed * Time.deltaTime);
                }
            }

            // Update main sky light tint
            if (mainSkyLight != null)
            {
                mainSkyLight.color = skyColorOverDay.Evaluate(timeOfDay);
            }
        }

        public void UpdateSkyAndSpaceDust()
        {
            if (spaceDustParticleSystem == null) return;

            var mainModule = spaceDustParticleSystem.main;

            switch (activeDustType)
            {
                case CosmicDustType.IonizedStardust:
                    mainModule.startColor = new Color(0.2f, 0.8f, 1.0f, 0.6f); // Cyan Glow
                    break;

                case CosmicDustType.NebularSilicate:
                    mainModule.startColor = new Color(0.8f, 0.5f, 0.2f, 0.4f); // Amber Dust
                    break;

                case CosmicDustType.IrradiatedDebris:
                    mainModule.startColor = new Color(0.9f, 0.1f, 0.2f, 0.7f); // Crimson Plasma
                    break;

                case CosmicDustType.CrystallineAuroric:
                    mainModule.startColor = new Color(0.4f, 1.0f, 0.4f, 0.5f); // Aurora Green
                    break;
            }
        }

        public XenoGeneticSample HarvestXenoGeneticSample(string name, TraitCategory traitCategory, float baseValue)
        {
            // Radiation stability varies based on active cosmic dust type
            float stability = (activeDustType == CosmicDustType.IrradiatedDebris) ? 0.35f : 0.88f;

            XenoGeneticSample newSample = new XenoGeneticSample
            {
                sampleID = $"XENO-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                sampleName = name,
                originDustType = activeDustType,
                radiationStability = stability,
                extractedXenoGene = new Gene
                {
                    geneName = $"[Xeno] {name}",
                    category = traitCategory,
                    value = baseValue,
                    isDominant = true,
                    expressColor = (activeDustType == CosmicDustType.CrystallineAuroric) ? Color.green : Color.magenta
                }
            };

            gatheredXenoSamples.Add(newSample);

            // Integrate with master Earth/Xeno Genome Database if available
            if (GenesisAR.Databases.EarthGenomeDatabase.Instance != null)
            {
                GenesisAR.Databases.EarthGenomeDatabase.Instance.masterLibrary[0].extractedGenes.Add(newSample.extractedXenoGene);
            }

            Debug.Log($"[Genesis AR Space Engine] New Xeno-Genetic Sample Harvested: {newSample.sampleName} (Stability: {stability * 100}%)");
            return newSample;
        }
    }
}
