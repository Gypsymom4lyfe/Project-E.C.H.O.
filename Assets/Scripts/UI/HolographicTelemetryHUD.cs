// HolographicTelemetryHUD.cs
// Attach to the Holographic Planet parent GameObject.
// Requires:
//   - PlanetEcosystemManager on the same GameObject (or assigned via Inspector)
//   - EcosystemTelemetryService on the same GameObject (or assigned via Inspector)
//   - A World Space UI prefab (biomeHudLabelPrefab) containing:
//       * TextMeshProUGUI named "BiomeTitle"
//       * TextMeshProUGUI named "MetricsText"
//       * UnityEngine.UI.Image named "HealthBar" (fill-type)
//   - A hudContainer Transform to parent instantiated labels

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GenesisAR.Ecosystem;
using GenesisAR.Telemetry;

namespace GenesisAR.UI
{
    /// <summary>
    /// Projects dynamic holographic HUD labels above each biome on the AR planet.
    /// Labels display live population counts, health bars, temperature and humidity,
    /// and billboard toward the player's AR camera every frame.
    /// </summary>
    public class HolographicTelemetryHUD : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("Source of biome data. Auto-discovered on the same GameObject if not assigned.")]
        public PlanetEcosystemManager ecosystemManager;

        [Tooltip("Provides polled telemetry snapshots. Auto-discovered on the same GameObject if not assigned.")]
        public EcosystemTelemetryService telemetryService;

        [Tooltip("AR/XR camera used for billboard facing and visibility culling.")]
        public Camera arCamera;

        [Header("UI Prefabs & Anchors")]
        [Tooltip("World Space UI Panel prefab containing BiomeTitle, MetricsText and HealthBar child objects.")]
        public GameObject biomeHudLabelPrefab;

        [Tooltip("Parent transform that holds all instantiated HUD labels in AR space.")]
        public Transform hudContainer;

        [Header("Display Settings")]
        [Tooltip("Vertical offset (metres) above each biome anchor point.")]
        public float labelVerticalOffset = 0.3f;

        [Tooltip("When true, each label rotates each frame to face the AR camera.")]
        public bool billboardToCamera = true;

        [Tooltip("Label colour when health score is >= healthyThreshold.")]
        public Color healthyColor = Color.cyan;

        [Tooltip("Label colour when health score is <= criticalThreshold.")]
        public Color criticalColor = Color.red;

        [Tooltip("Health score at or above which a biome is considered healthy (0–1).")]
        [Range(0f, 1f)]
        public float healthyThreshold = 0.65f;

        [Tooltip("Health score at or below which a biome is considered critical (0–1).")]
        [Range(0f, 1f)]
        public float criticalThreshold = 0.25f;

        [Tooltip("Only show labels for biomes within this world-space distance of the camera.")]
        public float visibilityRadius = 10f;

        [Tooltip("How quickly label colours lerp to new values (higher = faster).")]
        public float colorLerpSpeed = 3f;

        // ── Private state ─────────────────────────────────────────────────────────

        // Per-label runtime data.
        private class LabelEntry
        {
            public GameObject Root;
            public TextMeshProUGUI TitleText;
            public TextMeshProUGUI MetricsText;
            public Image HealthBar;
            public Color CurrentColor;
        }

        private readonly List<LabelEntry> _labels = new List<LabelEntry>();
        private bool _telemetryDirty;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Auto-discover components if not assigned.
            if (ecosystemManager == null)
                ecosystemManager = GetComponentInParent<PlanetEcosystemManager>();

            if (telemetryService == null)
                telemetryService = GetComponentInParent<EcosystemTelemetryService>();

            if (arCamera == null)
                arCamera = Camera.main;

            ValidateRequiredReferences();
        }

        private void OnEnable()
        {
            if (telemetryService != null)
                telemetryService.OnTelemetryUpdated += HandleTelemetryUpdated;
        }

        private void OnDisable()
        {
            if (telemetryService != null)
                telemetryService.OnTelemetryUpdated -= HandleTelemetryUpdated;
        }

        private void Start()
        {
            if (ecosystemManager == null || biomeHudLabelPrefab == null) return;
            BuildLabels();
        }

        private void Update()
        {
            if (_labels.Count == 0) return;

            UpdateLabelPositions();

            if (billboardToCamera)
                BillboardLabels();

            CullByDistance();

            if (_telemetryDirty)
            {
                RefreshLabelContent();
                _telemetryDirty = false;
            }
        }

        // ── Label construction ────────────────────────────────────────────────────

        /// <summary>Instantiates one HUD label per biome in the ecosystem manager.</summary>
        private void BuildLabels()
        {
            DestroyAllLabels();

            Transform container = hudContainer != null ? hudContainer : transform;

            foreach (BiomeData biome in ecosystemManager.Biomes)
            {
                GameObject labelRoot = Instantiate(biomeHudLabelPrefab, container);
                labelRoot.name = $"HUD_{biome.biomeName}";

                LabelEntry entry = new LabelEntry
                {
                    Root        = labelRoot,
                    TitleText   = labelRoot.GetComponentInChildren<TextMeshProUGUI>(),
                    MetricsText = FindTextComponent(labelRoot, "MetricsText"),
                    HealthBar   = FindImageComponent(labelRoot, "HealthBar"),
                    CurrentColor = healthyColor,
                };

                // Separate title text if a child named "BiomeTitle" exists.
                TextMeshProUGUI titleComp = FindTextComponent(labelRoot, "BiomeTitle");
                if (titleComp != null)
                    entry.TitleText = titleComp;

                if (entry.TitleText != null)
                    entry.TitleText.text = biome.biomeName;

                _labels.Add(entry);
            }

            // Populate content immediately using ecosystem manager's current state.
            RefreshLabelContent();
        }

        private void DestroyAllLabels()
        {
            foreach (LabelEntry entry in _labels)
            {
                if (entry.Root != null)
                    Destroy(entry.Root);
            }
            _labels.Clear();
        }

        // ── Per-frame updates ─────────────────────────────────────────────────────

        private void UpdateLabelPositions()
        {
            IReadOnlyList<BiomeData> biomes = ecosystemManager.Biomes;

            for (int i = 0; i < _labels.Count && i < biomes.Count; i++)
            {
                if (_labels[i].Root == null) continue;

                Vector3 target = transform.TransformPoint(biomes[i].worldPosition)
                                 + Vector3.up * labelVerticalOffset;
                _labels[i].Root.transform.position = target;
            }
        }

        private void BillboardLabels()
        {
            if (arCamera == null) return;

            foreach (LabelEntry entry in _labels)
            {
                if (entry.Root == null) continue;

                Vector3 dirToCamera = entry.Root.transform.position - arCamera.transform.position;
                if (dirToCamera.sqrMagnitude > 0.0001f)
                    entry.Root.transform.rotation = Quaternion.LookRotation(dirToCamera);
            }
        }

        private void CullByDistance()
        {
            if (arCamera == null) return;

            Vector3 camPos = arCamera.transform.position;
            float radiusSq = visibilityRadius * visibilityRadius;

            foreach (LabelEntry entry in _labels)
            {
                if (entry.Root == null) continue;

                bool inRange = (entry.Root.transform.position - camPos).sqrMagnitude <= radiusSq;
                if (entry.Root.activeSelf != inRange)
                    entry.Root.SetActive(inRange);
            }
        }

        // ── Content refresh ───────────────────────────────────────────────────────

        private void RefreshLabelContent()
        {
            IReadOnlyList<BiomeData> biomes = ecosystemManager.Biomes;

            for (int i = 0; i < _labels.Count && i < biomes.Count; i++)
            {
                LabelEntry entry  = _labels[i];
                BiomeData  biome  = biomes[i];

                if (entry.Root == null) continue;

                // Health bar fill.
                if (entry.HealthBar != null)
                    entry.HealthBar.fillAmount = biome.healthScore;

                // Metrics text block.
                if (entry.MetricsText != null)
                {
                    entry.MetricsText.text =
                        $"Population : {biome.population:N0}\n" +
                        $"Health     : {biome.healthScore * 100f:F1}%\n" +
                        $"Temp       : {biome.temperatureCelsius:F1}°C\n" +
                        $"Humidity   : {biome.humidity:F1}%\n" +
                        $"Species    : {biome.speciesCount}";
                }

                // Smoothly lerp label colour based on health score.
                Color targetColor = HealthColor(biome.healthScore);
                entry.CurrentColor = Color.Lerp(entry.CurrentColor, targetColor,
                                                colorLerpSpeed * Time.deltaTime);

                if (entry.TitleText  != null) entry.TitleText.color  = entry.CurrentColor;
                if (entry.MetricsText != null) entry.MetricsText.color = entry.CurrentColor;
                if (entry.HealthBar   != null) entry.HealthBar.color   = entry.CurrentColor;
            }
        }

        // ── Telemetry event handler ───────────────────────────────────────────────

        private void HandleTelemetryUpdated(object sender, TelemetryUpdatedEventArgs e)
        {
            // Flag dirty; the actual refresh happens in Update() on the main thread
            // so we never touch Unity objects from a potential background thread.
            _telemetryDirty = true;
        }

        // ── Utility ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a colour interpolated between <see cref="criticalColor"/> and
        /// <see cref="healthyColor"/> according to the supplied health score.
        /// </summary>
        private Color HealthColor(float healthScore)
        {
            if (healthScore >= healthyThreshold)  return healthyColor;
            if (healthScore <= criticalThreshold) return criticalColor;

            // Normalise to [0, 1] between the two thresholds.
            float t = Mathf.InverseLerp(criticalThreshold, healthyThreshold, healthScore);
            return Color.Lerp(criticalColor, healthyColor, t);
        }

        private static TextMeshProUGUI FindTextComponent(GameObject root, string childName)
        {
            Transform child = root.transform.Find(childName);
            if (child != null) return child.GetComponent<TextMeshProUGUI>();

            // Fall back to a deep search.
            foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.gameObject.name == childName) return tmp;
            }
            return null;
        }

        private static Image FindImageComponent(GameObject root, string childName)
        {
            Transform child = root.transform.Find(childName);
            if (child != null) return child.GetComponent<Image>();

            foreach (Image img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name == childName) return img;
            }
            return null;
        }

        private void ValidateRequiredReferences()
        {
            if (ecosystemManager == null)
                Debug.LogError("[HolographicTelemetryHUD] PlanetEcosystemManager is not assigned and could not be found.", this);

            if (telemetryService == null)
                Debug.LogWarning("[HolographicTelemetryHUD] EcosystemTelemetryService is not assigned. Content will refresh only when the planet's biome state changes.", this);

            if (biomeHudLabelPrefab == null)
                Debug.LogError("[HolographicTelemetryHUD] biomeHudLabelPrefab is not assigned. No HUD labels will be created.", this);

            if (arCamera == null)
                Debug.LogWarning("[HolographicTelemetryHUD] AR camera not found. Billboard and culling features are disabled.", this);
        }
    }
}
