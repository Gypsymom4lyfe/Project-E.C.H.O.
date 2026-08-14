using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        [TextArea(3, 5)]
        public string description;

        public Sprite thumbnailIcon;
        public string geneticSignature;
    }

    public class JournalEntryCardUI : MonoBehaviour
    {
        [Header("Entry Card UI")]
        public Button selectButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI timestampText;
        public Image thumbnailImage;

        public void Bind(JournalEntry entry, Action<JournalEntry> onSelected)
        {
            if (entry == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = entry.title;
            }

            if (timestampText != null)
            {
                timestampText.text = entry.timestamp;
            }

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = entry.thumbnailIcon;
                thumbnailImage.enabled = entry.thumbnailIcon != null;
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                if (onSelected != null)
                {
                    selectButton.onClick.AddListener(() => onSelected(entry));
                }
            }
        }
    }

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

        [Header("Data")]
        [SerializeField]
        private List<JournalEntry> discoveredLogs = new List<JournalEntry>();

        private LogCategory currentActiveCategory = LogCategory.Animals;

        private void Start()
        {
            BindTabButtons();
            SeedInitialFieldLogs();
            SwitchCategory(LogCategory.Animals);
        }

        private void OnDestroy()
        {
            UnbindTabButtons();
        }

        public void SwitchCategory(LogCategory newCategory)
        {
            currentActiveCategory = newCategory;

            if (activeTabTitleText != null)
            {
                activeTabTitleText.text = $"<b>{newCategory.ToString().ToUpperInvariant()} LOGS</b>";
            }

            PopulateCategoryView();
        }

        public void AddScanLog(string title, LogCategory category, string description, string dnaSig = "N/A", Sprite thumbnail = null)
        {
            JournalEntry newEntry = new JournalEntry
            {
                entryID = Guid.NewGuid().ToString("N").Substring(0, 8),
                title = string.IsNullOrWhiteSpace(title) ? "Untitled Log" : title.Trim(),
                category = category,
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                description = string.IsNullOrWhiteSpace(description) ? "No description provided." : description.Trim(),
                geneticSignature = string.IsNullOrWhiteSpace(dnaSig) ? "N/A" : dnaSig.Trim(),
                thumbnailIcon = thumbnail
            };

            discoveredLogs.Add(newEntry);
            Debug.Log($"[Genesis AR Journal] New {category} Log Created: {newEntry.title}");

            if (category == currentActiveCategory)
            {
                PopulateCategoryView();
                ShowEntryDetails(newEntry);
            }
        }

        private void PopulateCategoryView()
        {
            if (entryListContainer == null || entryCardPrefab == null)
            {
                Debug.LogWarning("[Genesis AR Journal] Entry list container or card prefab is not assigned.");
                return;
            }

            foreach (Transform child in entryListContainer)
            {
                Destroy(child.gameObject);
            }

            List<JournalEntry> filteredLogs = discoveredLogs.FindAll(log => log.category == currentActiveCategory);

            foreach (JournalEntry log in filteredLogs)
            {
                GameObject cardObj = Instantiate(entryCardPrefab, entryListContainer);

                JournalEntryCardUI cardUI = cardObj.GetComponent<JournalEntryCardUI>();
                if (cardUI != null)
                {
                    cardUI.Bind(log, ShowEntryDetails);
                    continue;
                }

                TextMeshProUGUI fallbackText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
                if (fallbackText != null)
                {
                    fallbackText.text = $"<b>{log.title}</b>\n{log.timestamp}";
                }

                Button fallbackButton = cardObj.GetComponent<Button>();
                if (fallbackButton != null)
                {
                    fallbackButton.onClick.RemoveAllListeners();
                    fallbackButton.onClick.AddListener(() => ShowEntryDetails(log));
                }
            }

            if (filteredLogs.Count > 0)
            {
                ShowEntryDetails(filteredLogs[0]);
            }
            else
            {
                ClearDetailView();
            }
        }

        private void ShowEntryDetails(JournalEntry entry)
        {
            if (entry == null)
            {
                ClearDetailView();
                return;
            }

            if (detailTitleText != null)
            {
                detailTitleText.text = entry.title;
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = entry.description;
            }

            if (detailGeneticsText != null)
            {
                detailGeneticsText.text = $"Genetic Signature: {entry.geneticSignature}";
            }

            if (detailPreviewImage != null)
            {
                detailPreviewImage.sprite = entry.thumbnailIcon;
                detailPreviewImage.enabled = entry.thumbnailIcon != null;
            }
        }

        private void ClearDetailView()
        {
            if (detailTitleText != null)
            {
                detailTitleText.text = "No Logs in Category";
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = "Scan organisms or telemetry to begin collecting journal entries.";
            }

            if (detailGeneticsText != null)
            {
                detailGeneticsText.text = "Genetic Signature: N/A";
            }

            if (detailPreviewImage != null)
            {
                detailPreviewImage.sprite = null;
                detailPreviewImage.enabled = false;
            }
        }

        private void SeedInitialFieldLogs()
        {
            if (discoveredLogs.Count > 0)
            {
                return;
            }

            AddSeedLog("Aurora Fox", LogCategory.Animals, "Observed grazing near the basalt ridge.", "AFOX-77-LUMEN");
            AddSeedLog("Sky Fern", LogCategory.Plants, "Bioluminescent leaves pulse every 8 seconds.", "SFERN-21-VERD");
            AddSeedLog("Glasswing Beetle", LogCategory.Bugs, "Transparent wings refract ultraviolet light.", "GBUG-04-PRSM");
            AddSeedLog("Upper Atmosphere", LogCategory.Weather, "Ionized cloud layer rising rapidly.", "WX-ION-003");
            AddSeedLog("River Delta", LogCategory.Water, "pH stable with elevated mineral content.", "H2O-DELTA-12");
        }

        private void AddSeedLog(string title, LogCategory category, string description, string dnaSig)
        {
            discoveredLogs.Add(new JournalEntry
            {
                entryID = Guid.NewGuid().ToString("N").Substring(0, 8),
                title = title,
                category = category,
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                description = description,
                geneticSignature = dnaSig,
                thumbnailIcon = null
            });
        }

        private void BindTabButtons()
        {
            if (animalsTabButton != null) animalsTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Animals));
            if (plantsTabButton != null) plantsTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Plants));
            if (bugsTabButton != null) bugsTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Bugs));
            if (weatherTabButton != null) weatherTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Weather));
            if (waterTabButton != null) waterTabButton.onClick.AddListener(() => SwitchCategory(LogCategory.Water));
        }

        private void UnbindTabButtons()
        {
            if (animalsTabButton != null) animalsTabButton.onClick.RemoveAllListeners();
            if (plantsTabButton != null) plantsTabButton.onClick.RemoveAllListeners();
            if (bugsTabButton != null) bugsTabButton.onClick.RemoveAllListeners();
            if (weatherTabButton != null) weatherTabButton.onClick.RemoveAllListeners();
            if (waterTabButton != null) waterTabButton.onClick.RemoveAllListeners();
        }
    }
}
