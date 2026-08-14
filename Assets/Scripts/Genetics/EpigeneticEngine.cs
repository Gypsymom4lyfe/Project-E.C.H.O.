using System;
using UnityEngine;

namespace GenesisAR.Genetics
{
    [Serializable]
    public struct EnvironmentState
    {
        public float TemperatureCelsius; // e.g., -10 to 50
        public float ToxicityLevel;      // 0.0 (Clean) to 1.0 (Severe)
        public float RadiationLevel;     // 0.0 to 1.0
        public float ThermalPlumeProximity; // Distance to Project E.C.H.O. thermal vent
    }

    [RequireComponent(typeof(GeneticsSystem))]
    public class EpigeneticEngine : MonoBehaviour
    {
        private GeneticsSystem _geneticsSystem;
        private Renderer _creatureRenderer;
        private Material _creatureMaterial;

        [Header("Current Environment")]
        [SerializeField] private EnvironmentState _currentEnvironment;

        [Header("Active Epigenetic Modifiers")]
        [Range(0f, 2f)] public float ThermalToleranceMultiplier = 1.0f;
        [Range(0f, 2f)] public float MetabolismSpeedMultiplier = 1.0f;
        public bool HeatShockProteinExpressed { get; private set; }

        private void Awake()
        {
            _geneticsSystem = GetComponent<GeneticsSystem>();
            _creatureRenderer = GetComponentInChildren<Renderer>();
            if (_creatureRenderer != null)
            {
                _creatureMaterial = _creatureRenderer.material;
            }
        }

        private void OnDestroy()
        {
            if (_creatureMaterial != null)
            {
                Destroy(_creatureMaterial);
                _creatureMaterial = null;
            }
        }

        public void UpdateEnvironmentConditions(EnvironmentState newState)
        {
            _currentEnvironment = newState;
            EvaluateEpigeneticExpression();
        }

        private void EvaluateEpigeneticExpression()
        {
            // 1. Extreme Heat / Thermal Plume Proximity -> Trigger Heat Shock Response
            if (_currentEnvironment.TemperatureCelsius > 35.0f || _currentEnvironment.ThermalPlumeProximity < 3.0f)
            {
                HeatShockProteinExpressed = true;
                ThermalToleranceMultiplier = 1.5f;
                MetabolismSpeedMultiplier = 0.8f; // Slow down metabolism to conserve energy

                ApplyVisualPhenotypeShift(Color.red, "Heat Adaptation Active");
            }
            // 2. Cold Stress -> Suppress Expression
            else if (_currentEnvironment.TemperatureCelsius < 5.0f)
            {
                HeatShockProteinExpressed = false;
                ThermalToleranceMultiplier = 0.7f;
                MetabolismSpeedMultiplier = 0.5f; // Semi-dormancy / hibernation state

                ApplyVisualPhenotypeShift(Color.cyan, "Cold Dormancy Active");
            }
            // 3. Optimal Baseline
            else
            {
                HeatShockProteinExpressed = false;
                ThermalToleranceMultiplier = 1.0f;
                MetabolismSpeedMultiplier = 1.0f;

                ApplyVisualPhenotypeShift(Color.white, "Baseline Expression");
            }
        }

        private void ApplyVisualPhenotypeShift(Color stressColor, string logMessage)
        {
            if (_creatureMaterial != null && _creatureMaterial.HasProperty("_EmissionColor"))
            {
                // Smoothly shift emission color to reflect epigenetic stress state in AR
                _creatureMaterial.SetColor("_EmissionColor", stressColor * 0.3f);
            }

            Debug.Log($"[Epigenetics] {gameObject.name}: {logMessage} | Metabolism: {MetabolismSpeedMultiplier}x");
        }
    }
}
