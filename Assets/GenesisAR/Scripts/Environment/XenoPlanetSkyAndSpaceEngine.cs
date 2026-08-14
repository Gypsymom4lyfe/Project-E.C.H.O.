using System.Collections.Generic;
using UnityEngine;

namespace GenesisAR.Environment
{
    // ─────────────────────────────────────────────────────────────
    //  CelestialBody – orbiting moon, sun, or asteroid
    // ─────────────────────────────────────────────────────────────
    [System.Serializable]
    public class CelestialBody
    {
        public string Name;
        public Transform Body;
        public Transform OrbitPivot;      // empty GameObject at orbit centre
        public float     OrbitSpeed = 10f; // degrees per second
        public float     OrbitTilt  = 0f;  // inclination in degrees
        public Vector3   AxialTilt  = Vector3.zero;
    }

    // ─────────────────────────────────────────────────────────────
    //  XenoBiome – xenoplanetary biome with alien palette
    // ─────────────────────────────────────────────────────────────
    [System.Serializable]
    public class XenoBiome
    {
        public string Name;
        public Color  SkyColorDay;
        public Color  SkyColorNight;
        public Color  FogColor;
        public float  FogDensity   = 0.02f;
        public float  GravityScale = 1.0f;   // relative to Earth
        public float  AtmosphericPressure = 1.0f;
    }

    // ─────────────────────────────────────────────────────────────
    //  XenoPlanetSkyAndSpaceEngine – renders alien skies, orbiting
    //  bodies, space dust and hooks into Xeno-Genetics modifiers
    // ─────────────────────────────────────────────────────────────
    public class XenoPlanetSkyAndSpaceEngine : MonoBehaviour
    {
        [Header("Sky & Atmosphere")]
        public Light   SunLight;
        public Material SkyboxMaterial;         // expects _SkyColorTop, _SkyColorHorizon
        public float   DayNightCycleDuration = 300f; // seconds per full cycle

        [Header("Celestial Bodies")]
        public List<CelestialBody> CelestialBodies = new List<CelestialBody>();

        [Header("Space Dust")]
        public ParticleSystem SpaceDustParticles;
        public float          SpaceDustDensityMultiplier = 1f;

        [Header("Xeno Biomes")]
        public List<XenoBiome> AvailableBiomes = new List<XenoBiome>();
        [SerializeField] private int _activeBiomeIndex = 0;

        [Header("Xeno-Genetics Modifiers")]
        [Tooltip("Mutation rate bonus applied to organisms on this planet")]
        [Range(0f, 2f)] public float XenoMutationMultiplier = 1.0f;
        [Tooltip("Radiation index (0=safe, 1=lethal). Affects creature Longevity gene.")]
        [Range(0f, 1f)] public float RadiationIndex = 0f;

        private float _timeOfDay; // 0-1 (0=dawn, 0.5=dusk, 1=midnight)

        public XenoBiome ActiveBiome =>
            AvailableBiomes.Count > 0
                ? AvailableBiomes[Mathf.Clamp(_activeBiomeIndex, 0, AvailableBiomes.Count - 1)]
                : null;

        // Events
        public static System.Action<XenoBiome> OnBiomeChanged;
        public static System.Action<float>     OnDayNightTick;

        private void Start()
        {
            if (AvailableBiomes.Count == 0)
                AvailableBiomes.Add(CreateDefaultAlienBiome());

            ApplyBiome(ActiveBiome);
            ApplyXenoGeneticsModifiers();
        }

        private void Update()
        {
            AdvanceDayNightCycle();
            OrbitCelestialBodies();
            UpdateSpaceDust();
        }

        // ── Day / Night Cycle ─────────────────────────────────────
        private void AdvanceDayNightCycle()
        {
            _timeOfDay += Time.deltaTime / DayNightCycleDuration;
            if (_timeOfDay > 1f) _timeOfDay -= 1f;

            float sunAngle = _timeOfDay * 360f - 90f;
            if (SunLight != null)
            {
                SunLight.transform.localRotation = Quaternion.Euler(sunAngle, 0f, 0f);
                SunLight.intensity = Mathf.Clamp01(Mathf.Sin(_timeOfDay * Mathf.PI));
            }

            if (SkyboxMaterial != null && ActiveBiome != null)
            {
                Color sky = Color.Lerp(ActiveBiome.SkyColorNight, ActiveBiome.SkyColorDay,
                                       Mathf.Clamp01(Mathf.Sin(_timeOfDay * Mathf.PI)));
                SkyboxMaterial.SetColor("_SkyColorTop", sky);
                SkyboxMaterial.SetColor("_SkyColorHorizon", sky * 0.7f);
            }

            OnDayNightTick?.Invoke(_timeOfDay);
        }

        // ── Celestial Orbits ──────────────────────────────────────
        private void OrbitCelestialBodies()
        {
            foreach (CelestialBody body in CelestialBodies)
            {
                if (body.OrbitPivot == null || body.Body == null) continue;
                body.OrbitPivot.Rotate(
                    Quaternion.Euler(body.OrbitTilt, 0f, 0f) * Vector3.up,
                    body.OrbitSpeed * Time.deltaTime,
                    Space.World);
                body.Body.Rotate(body.AxialTilt, body.OrbitSpeed * 0.5f * Time.deltaTime);
            }
        }

        // ── Space Dust ────────────────────────────────────────────
        private void UpdateSpaceDust()
        {
            if (SpaceDustParticles == null) return;
            ParticleSystem.EmissionModule em = SpaceDustParticles.emission;
            em.rateOverTime = 20f * SpaceDustDensityMultiplier;
        }

        // ── Biome management ──────────────────────────────────────
        public void SetBiome(int index)
        {
            if (index < 0 || index >= AvailableBiomes.Count) return;
            _activeBiomeIndex = index;
            ApplyBiome(ActiveBiome);
            OnBiomeChanged?.Invoke(ActiveBiome);
        }

        private void ApplyBiome(XenoBiome biome)
        {
            if (biome == null) return;
            RenderSettings.fogColor   = biome.FogColor;
            RenderSettings.fogDensity = biome.FogDensity;
            Physics.gravity           = new Vector3(0f, -9.81f * biome.GravityScale, 0f);
            Debug.Log($"[XenoPlanetSky] Biome applied: {biome.Name}  Gravity={biome.GravityScale}g");
        }

        // ── Xeno-Genetics ─────────────────────────────────────────
        private void ApplyXenoGeneticsModifiers()
        {
            Genetics.GeneticsSystem.MutationRate *= XenoMutationMultiplier;
            Debug.Log($"[XenoPlanetSky] Xeno mutation multiplier: ×{XenoMutationMultiplier}  " +
                      $"Radiation: {RadiationIndex:P0}");
        }

        // ── Defaults ──────────────────────────────────────────────
        private static XenoBiome CreateDefaultAlienBiome() => new XenoBiome
        {
            Name               = "Crimson Wastes",
            SkyColorDay        = new Color(0.7f, 0.2f, 0.1f),
            SkyColorNight      = new Color(0.05f, 0.0f, 0.15f),
            FogColor           = new Color(0.5f, 0.15f, 0.05f),
            FogDensity         = 0.04f,
            GravityScale       = 0.38f,
            AtmosphericPressure = 0.006f
        };
    }
}
