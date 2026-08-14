using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GenesisAR.Ecosystem;

namespace GenesisAR.UI
{
    public enum LogCategory
    {
        Animals,
        Plants,
        Bugs,
        Weather,
        Water
    }

    [Serializable]
    public class JournalEntry
    {
        public string entryID;
        public string title;
        public LogCategory category;
        public string timestamp;
        [TextArea(3, 5)] public string description;
        public Sprite thumbnailIcon;
        public string geneticSignature; // Associated DNA markers or metrics
    }

    /// <summary>
    /// Powers the interactive holographic AR Field Journal tablet.
    /// Allows players to log observations, scan new species, and view
    /// categorized telemetry logs for Animals, Plants, Bugs, Weather, and Water.
    ///
    /// Attach this script to your World-Space AR Journal Canvas or holographic tablet prefab.
    /// </summary>
    public class DataCollectionJournalUI : MonoBehaviour
    {
        [Header("UI Category Tabs")]
        public Button animalsTabButton;
        public Button plantsTabButton;
        public Button bugsTabButton;
        public Button weatherTabButton;
        public Button waterTabButton;

        [Header("UI Display Components")]
        public TextMeshProUGUI activeTabTitleText;
        public Transform entryListContainer;
        public GameObject entryCardPrefab;

        [Header("Detail Inspection View")]
        public TextMeshProUGUI detailTitleText;
        public TextMeshProUGUI detailDescriptionText;
        public TextMeshProUGUI detailGeneticsText;
        public Image detailPreviewImage;

        [Header("Live Planet Telemetry Reference")]
        public PlanetEcosystemManager ecosystemManager;

        // In-memory database of discovered field logs
        private List<JournalEntry> discoveredLogs = new List<JournalEntry>();
        private LogCategory currentActiveCategory = LogCategory.Animals;

        private void Start()
        {
            // Bind navigation tab buttons
            if (animalsTabButton != null) animalsTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Animals));
            if (plantsTabButton != null)  plantsTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Plants));
            if (bugsTabButton != null)    bugsTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Bugs));
            if (weatherTabButton != null) weatherTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Weather));
            if (waterTabButton != null)   waterTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Water));

            // Seed initial field log samples
            SeedInitialFieldLogs();

            // Render initial tab view
            SwitchCategory(LogCategory.Animals);
        }

        /// <summary>
        /// Switches the active journal category tab and refreshes the entry list.
        /// </summary>
        public void SwitchCategory(LogCategory newCategory)
        {
            currentActiveCategory = newCategory;

            if (activeTabTitleText != null)
            {
                activeTabTitleText.text = $"<b>{newCategory.ToString().ToUpper()} LOGS</b>";
            }

            PopulateCategoryView();
        }

        /// <summary>
        /// Adds a new scanned observation to the journal database.
        /// </summary>
        /// <param name="title">Display name for the observation.</param>
        /// <param name="category">Category the log belongs to.</param>
        /// <param name="description">Detailed field notes.</param>
        /// <param name="dnaSig">Optional genetic/DNA signature string.</param>
        public void AddScanLog(string title, LogCategory category, string description, string dnaSig = "N/A")
        {
            JournalEntry newEntry = new JournalEntry
            {
                entryID          = Guid.NewGuid().ToString().Substring(0, 8),
                title            = title,
                category         = category,
                timestamp        = DateTime.Now.ToString("HH:mm:ss"),
                description      = description,
                geneticSignature = dnaSig
            };

            discoveredLogs.Add(newEntry);
            Debug.Log($"[Genesis AR Journal] New {category} Log Created: {title}");

            // Refresh UI if logging into the currently open category
            if (category == currentActiveCategory)
            {
                PopulateCategoryView();
            }
        }

        /// <summary>
        /// Clears and rebuilds the card list for the current active category.
        /// </summary>
        private void PopulateCategoryView()
        {
            if (entryListContainer == null || entryCardPrefab == null)
            {
                Debug.LogWarning("[Genesis AR Journal] entryListContainer or entryCardPrefab is not assigned.");
                return;
            }

            // Clear existing UI cards in container
            foreach (Transform child in entryListContainer)
            {
                Destroy(child.gameObject);
            }

            List<JournalEntry> filteredLogs = discoveredLogs.FindAll(e => e.category == currentActiveCategory);

            if (filteredLogs.Count == 0)
            {
                SpawnEmptyStateCard();
                return;
            }

            foreach (var log in filteredLogs)
            {
                GameObject cardObj = Instantiate(entryCardPrefab, entryListContainer);

                // Populate summary card text
                TextMeshProUGUI cardText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
                if (cardText != null)
                {
                    cardText.text = $"<b>{log.title}</b>\n<size=70%>{log.timestamp}</size>";
                }

                // Populate thumbnail if available
                Image cardImage = cardObj.GetComponentInChildren<Image>();
                if (cardImage != null && log.thumbnailIcon != null)
                {
                    cardImage.sprite = log.thumbnailIcon;
                }

                // Wire the card button to open detail view for this entry
                Button cardButton = cardObj.GetComponent<Button>();
                if (cardButton != null)
                {
                    JournalEntry capturedLog = log; // closure capture
                    cardButton.onClick.AddListener(() => OpenDetailView(capturedLog));
                }
            }
        }

        /// <summary>
        /// Displays full details for a selected journal entry.
        /// </summary>
        /// <param name="entry">The entry to inspect.</param>
        public void OpenDetailView(JournalEntry entry)
        {
            if (entry == null) return;

            if (detailTitleText != null)
                detailTitleText.text = $"<b>{entry.title}</b>  <size=70%>[{entry.timestamp}]</size>";

            if (detailDescriptionText != null)
                detailDescriptionText.text = entry.description;

            if (detailGeneticsText != null)
                detailGeneticsText.text = $"Genetic Signature: {entry.geneticSignature}";

            if (detailPreviewImage != null && entry.thumbnailIcon != null)
                detailPreviewImage.sprite = entry.thumbnailIcon;

            Debug.Log($"[Genesis AR Journal] Viewing detail for: {entry.title} (ID: {entry.entryID})");
        }

        /// <summary>
        /// Pulls the latest telemetry snapshot from the PlanetEcosystemManager and
        /// creates journal entries for any new readings.
        /// </summary>
        public void SyncEcosystemTelemetry()
        {
            if (ecosystemManager == null)
            {
                Debug.LogWarning("[Genesis AR Journal] No PlanetEcosystemManager assigned.");
                return;
            }

            // Example: pull weather telemetry
            string weatherSummary = ecosystemManager.GetCurrentWeatherSummary();
            if (!string.IsNullOrEmpty(weatherSummary))
            {
                AddScanLog(
                    title:       "Live Weather Reading",
                    category:    LogCategory.Weather,
                    description: weatherSummary,
                    dnaSig:      "N/A"
                );
            }

            // Example: pull water telemetry
            string waterSummary = ecosystemManager.GetCurrentWaterSummary();
            if (!string.IsNullOrEmpty(waterSummary))
            {
                AddScanLog(
                    title:       "Live Water Reading",
                    category:    LogCategory.Water,
                    description: waterSummary,
                    dnaSig:      "N/A"
                );
            }

            Debug.Log("[Genesis AR Journal] Ecosystem telemetry synced.");
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Spawns a placeholder card when no logs exist for the active category.
        /// </summary>
        private void SpawnEmptyStateCard()
        {
            GameObject cardObj = Instantiate(entryCardPrefab, entryListContainer);
            TextMeshProUGUI cardText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (cardText != null)
            {
                cardText.text = "<i>No observations logged yet.\nUse your scanner to discover life!</i>";
            }

            // Disable button interaction on the empty-state card
            Button cardButton = cardObj.GetComponent<Button>();
            if (cardButton != null)
            {
                cardButton.interactable = false;
            }
        }

        /// <summary>
        /// Seeds sample field log entries for demonstration and testing purposes.
        /// </summary>
        private void SeedInitialFieldLogs()
        {
            AddScanLog(
                title:       "Luminara Deer",
                category:    LogCategory.Animals,
                description: "A graceful quadruped with bioluminescent antlers. Active at dusk near crystalline water bodies. " +
                             "Feeds primarily on glow-moss and starberries.",
                dnaSig:      "GNS-ANIM-0042"
            );

            AddScanLog(
                title:       "Radiant Ferncap",
                category:    LogCategory.Plants,
                description: "A broad-leafed plant found in high-humidity zones. Emits a faint amber glow during photosynthesis. " +
                             "Provides shade for small insects and ground-dwelling creatures.",
                dnaSig:      "GNS-PLNT-0018"
            );

            AddScanLog(
                title:       "Crystal Moth",
                category:    LogCategory.Bugs,
                description: "Translucent-winged moth with fractal wing patterns. Drawn to mineral deposits and glowing flora. " +
                             "Wingspan approximately 12 cm.",
                dnaSig:      "GNS-BUGS-0007"
            );

            AddScanLog(
                title:       "Ionic Storm Event",
                category:    LogCategory.Weather,
                description: "Rapid discharge of atmospheric ions across the upper cloud layer. " +
                             "Correlated with increased magnetic field activity from the planet's twin moons.",
                dnaSig:      "N/A"
            );

            AddScanLog(
                title:       "Tidal Spring Survey",
                category:    LogCategory.Water,
                description: "Mineral-rich thermal spring located at grid sector 7-C. " +
                             "Water temperature: 38°C. pH: 7.2. Bioluminescent microbe population detected.",
                dnaSig:      "GNS-H2O-0003"
            );
        }
    }
}
