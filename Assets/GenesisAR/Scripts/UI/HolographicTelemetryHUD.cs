using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GenesisAR.Ecosystem;

namespace GenesisAR.UI
{
    // ─────────────────────────────────────────────────────────────
    //  GraphBar – one species column in the 3D bar-chart HUD
    // ─────────────────────────────────────────────────────────────
    [System.Serializable]
    public class GraphBar
    {
        public string SpeciesID;
        public Transform BarTransform;   // scaled on Y axis
        public Text      LabelText;
        public Renderer  BarRenderer;
        public Color     PreyColor     = new Color(0.2f, 0.7f, 1.0f);
        public Color     PredatorColor = new Color(1.0f, 0.3f, 0.2f);
    }

    // ─────────────────────────────────────────────────────────────
    //  HolographicTelemetryHUD – 3D world-space billboard with
    //  animated bar-chart graphs and floating data labels
    // ─────────────────────────────────────────────────────────────
    public class HolographicTelemetryHUD : MonoBehaviour
    {
        [Header("World-Space Canvas")]
        public Canvas WorldCanvas;              // Render Mode: World Space
        public bool   AlwaysFaceCamera = true;

        [Header("Graph Bars")]
        public List<GraphBar> GraphBars = new List<GraphBar>();
        public float MaxPopulationDisplay = 500f;
        public float MaxBarHeight         = 0.3f; // metres in world space
        public float AnimationSpeed       = 3f;

        [Header("Labels")]
        public Text BiomeLabelText;
        public Text ElapsedTimeText;
        public Text ActiveSpeciesText;

        [Header("Alert")]
        public GameObject ExtinctionAlert;
        public Text       ExtinctionAlertText;

        private Camera _cam;
        private float  _elapsed;
        private readonly Dictionary<string, float> _targetHeights = new Dictionary<string, float>();

        private void Start()
        {
            _cam = Camera.main;

            PlanetEcosystemManager.OnTickComplete  += OnEcosystemTick;
            PlanetEcosystemManager.OnSpeciesExtinct += OnSpeciesExtinct;

            if (ExtinctionAlert != null) ExtinctionAlert.SetActive(false);
        }

        private void OnDestroy()
        {
            PlanetEcosystemManager.OnTickComplete  -= OnEcosystemTick;
            PlanetEcosystemManager.OnSpeciesExtinct -= OnSpeciesExtinct;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            BillboardToCamera();
            AnimateBars();
            UpdateTimeLabel();
        }

        // ── Billboard ─────────────────────────────────────────────
        private void BillboardToCamera()
        {
            if (!AlwaysFaceCamera || _cam == null) return;
            transform.LookAt(transform.position + _cam.transform.rotation * Vector3.forward,
                             _cam.transform.rotation * Vector3.up);
        }

        // ── Ecosystem event handlers ───────────────────────────────
        private void OnEcosystemTick(List<PopulationRecord> populations)
        {
            // Update target heights for animation
            foreach (PopulationRecord rec in populations)
            {
                float normalised = Mathf.Clamp01(rec.Population / MaxPopulationDisplay);
                _targetHeights[rec.SpeciesID] = normalised * MaxBarHeight;
            }

            // Update text labels
            if (ActiveSpeciesText != null)
                ActiveSpeciesText.text = $"Species: {populations.Count}";

            RefreshBiomeLabel();
        }

        private void OnSpeciesExtinct(string speciesID)
        {
            _targetHeights[speciesID] = 0f;

            if (ExtinctionAlert != null)
            {
                ExtinctionAlert.SetActive(true);
                if (ExtinctionAlertText != null)
                    ExtinctionAlertText.text = $"⚠ EXTINCT: {speciesID.ToUpper()}";

                CancelInvoke(nameof(HideExtinctionAlert));
                Invoke(nameof(HideExtinctionAlert), 5f);
            }
        }

        private void HideExtinctionAlert()
        {
            if (ExtinctionAlert != null) ExtinctionAlert.SetActive(false);
        }

        // ── Bar animation ─────────────────────────────────────────
        private void AnimateBars()
        {
            foreach (GraphBar bar in GraphBars)
            {
                if (bar.BarTransform == null) continue;

                if (!_targetHeights.TryGetValue(bar.SpeciesID, out float target))
                    target = 0f;

                Vector3 scale = bar.BarTransform.localScale;
                scale.y = Mathf.Lerp(scale.y, target, Time.deltaTime * AnimationSpeed);
                bar.BarTransform.localScale = scale;

                // Colour + label
                if (bar.BarRenderer != null)
                    bar.BarRenderer.material.color = bar.PreyColor;

                if (bar.LabelText != null)
                {
                    float pop = (scale.y / Mathf.Max(MaxBarHeight, 0.001f)) * MaxPopulationDisplay;
                    bar.LabelText.text = $"{bar.SpeciesID}\n{pop:F0}";
                }
            }
        }

        // ── Labels ────────────────────────────────────────────────
        private void UpdateTimeLabel()
        {
            if (ElapsedTimeText != null)
                ElapsedTimeText.text = $"T+{FormatTime(_elapsed)}";
        }

        private void RefreshBiomeLabel()
        {
            PlanetEcosystemManager eco = FindObjectOfType<PlanetEcosystemManager>();
            if (BiomeLabelText != null && eco != null)
                BiomeLabelText.text = $"Biome: {eco.CurrentBiome.Name}";
        }

        // ── Public API ────────────────────────────────────────────
        public void AddBar(string speciesID, Transform barTransform, bool isPredator,
                           Text label = null, Renderer renderer = null)
        {
            GraphBars.Add(new GraphBar
            {
                SpeciesID    = speciesID,
                BarTransform = barTransform,
                LabelText    = label,
                BarRenderer  = renderer,
            });
        }

        // ── Utility ───────────────────────────────────────────────
        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return $"{m:D2}:{s:D2}";
        }
    }
}
