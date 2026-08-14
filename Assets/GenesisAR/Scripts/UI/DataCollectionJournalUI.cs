using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisAR.UI
{
    // ─────────────────────────────────────────────────────────────
    //  JournalCategory – categories for field-journal entries
    // ─────────────────────────────────────────────────────────────
    public enum JournalCategory { Animal, Plant, Bug, Weather, Water }

    // ─────────────────────────────────────────────────────────────
    //  JournalEntry – one observation record
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class JournalEntry
    {
        public JournalCategory Category;
        public string          Subject;
        public string          Note;
        public string          Timestamp;
        public Texture2D       Photo;         // optional AR snapshot

        public override string ToString() =>
            $"[{Timestamp}] [{Category}] {Subject}: {Note}";
    }

    // ─────────────────────────────────────────────────────────────
    //  DataCollectionJournalUI – field journal panel where students
    //  log observations about animals, plants, bugs, weather & water
    // ─────────────────────────────────────────────────────────────
    public class DataCollectionJournalUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject JournalPanel;
        public GameObject EntryListPanel;
        public GameObject EntryDetailPanel;

        [Header("Input Fields")]
        public Dropdown   CategoryDropdown;
        public InputField SubjectField;
        public InputField NoteField;
        public Button     SubmitButton;
        public Button     CapturePhotoButton;

        [Header("Entry List")]
        public Transform     EntryListContent;   // Scroll View Content
        public GameObject    EntryRowPrefab;

        [Header("Detail View")]
        public Text    DetailCategoryText;
        public Text    DetailSubjectText;
        public Text    DetailNoteText;
        public Text    DetailTimestampText;
        public RawImage DetailPhoto;
        public Button  CloseDetailButton;

        [Header("Filter")]
        public Dropdown FilterDropdown;

        // All recorded entries
        private List<JournalEntry> _entries  = new List<JournalEntry>();
        private Texture2D          _pendingPhoto;
        private JournalEntry       _detailTarget;

        // Events
        public static event Action<JournalEntry> OnEntryAdded;

        private void Start()
        {
            PopulateCategoryDropdown();
            if (SubmitButton != null)      SubmitButton.onClick.AddListener(OnSubmit);
            if (CapturePhotoButton != null) CapturePhotoButton.onClick.AddListener(OnCapturePhoto);
            if (CloseDetailButton != null)  CloseDetailButton.onClick.AddListener(CloseDetail);
            if (FilterDropdown != null)     FilterDropdown.onValueChanged.AddListener(OnFilterChanged);

            if (EntryDetailPanel != null) EntryDetailPanel.SetActive(false);
        }

        // ── Category dropdown ─────────────────────────────────────
        private void PopulateCategoryDropdown()
        {
            if (CategoryDropdown == null) return;
            CategoryDropdown.ClearOptions();
            List<string> options = new List<string>();
            foreach (JournalCategory cat in Enum.GetValues(typeof(JournalCategory)))
                options.Add(cat.ToString());
            CategoryDropdown.AddOptions(options);

            // Mirror for filter dropdown
            if (FilterDropdown != null)
            {
                FilterDropdown.ClearOptions();
                List<string> filterOptions = new List<string> { "All" };
                filterOptions.AddRange(options);
                FilterDropdown.AddOptions(filterOptions);
            }
        }

        // ── Submit entry ──────────────────────────────────────────
        private void OnSubmit()
        {
            string subject = SubjectField?.text.Trim();
            string note    = NoteField?.text.Trim();

            if (string.IsNullOrEmpty(subject))
            {
                Debug.LogWarning("[Journal] Subject is required.");
                return;
            }

            JournalCategory category = (JournalCategory)(CategoryDropdown?.value ?? 0);

            JournalEntry entry = new JournalEntry
            {
                Category  = category,
                Subject   = subject,
                Note      = string.IsNullOrEmpty(note) ? "(no notes)" : note,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Photo     = _pendingPhoto
            };

            _entries.Add(entry);
            _pendingPhoto = null;

            Debug.Log($"[Journal] Entry added: {entry}");
            OnEntryAdded?.Invoke(entry);

            ClearInputFields();
            RefreshEntryList();
        }

        // ── Photo capture ─────────────────────────────────────────
        private void OnCapturePhoto()
        {
            StartCoroutine(CaptureScreenshot());
        }

        private System.Collections.IEnumerator CaptureScreenshot()
        {
            yield return new WaitForEndOfFrame();
            Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();
            _pendingPhoto = tex;
            Debug.Log("[Journal] Photo captured.");
        }

        // ── Entry list ────────────────────────────────────────────
        private void RefreshEntryList(JournalCategory? filter = null)
        {
            if (EntryListContent == null || EntryRowPrefab == null) return;

            foreach (Transform child in EntryListContent)
                Destroy(child.gameObject);

            foreach (JournalEntry entry in _entries)
            {
                if (filter.HasValue && entry.Category != filter.Value) continue;

                GameObject row = Instantiate(EntryRowPrefab, EntryListContent);

                // Expect the prefab to have a Text child for label and a Button for detail
                Text rowText = row.GetComponentInChildren<Text>();
                if (rowText != null)
                    rowText.text = $"[{entry.Category}] {entry.Subject}  {entry.Timestamp}";

                Button rowBtn = row.GetComponentInChildren<Button>();
                if (rowBtn != null)
                {
                    JournalEntry captured = entry;   // closure capture
                    rowBtn.onClick.AddListener(() => ShowDetail(captured));
                }
            }
        }

        // ── Detail view ───────────────────────────────────────────
        private void ShowDetail(JournalEntry entry)
        {
            _detailTarget = entry;
            if (EntryDetailPanel != null) EntryDetailPanel.SetActive(true);

            if (DetailCategoryText  != null) DetailCategoryText.text  = entry.Category.ToString();
            if (DetailSubjectText   != null) DetailSubjectText.text   = entry.Subject;
            if (DetailNoteText      != null) DetailNoteText.text      = entry.Note;
            if (DetailTimestampText != null) DetailTimestampText.text = entry.Timestamp;
            if (DetailPhoto         != null) DetailPhoto.texture      = entry.Photo;
        }

        private void CloseDetail()
        {
            if (EntryDetailPanel != null) EntryDetailPanel.SetActive(false);
            _detailTarget = null;
        }

        // ── Filter ────────────────────────────────────────────────
        private void OnFilterChanged(int index)
        {
            if (index == 0) { RefreshEntryList(); return; }
            JournalCategory cat = (JournalCategory)(index - 1);
            RefreshEntryList(cat);
        }

        // ── Helpers ───────────────────────────────────────────────
        private void ClearInputFields()
        {
            if (SubjectField != null) SubjectField.text = "";
            if (NoteField    != null) NoteField.text    = "";
        }

        // ── Public API ────────────────────────────────────────────
        public IReadOnlyList<JournalEntry> AllEntries => _entries.AsReadOnly();

        public List<JournalEntry> GetEntriesByCategory(JournalCategory category) =>
            _entries.FindAll(e => e.Category == category);

        public void TogglePanel()
        {
            if (JournalPanel != null)
                JournalPanel.SetActive(!JournalPanel.activeSelf);
        }

        public string ExportToJSON() => JsonUtility.ToJson(
            new JournalExport { entries = _entries }, prettyPrint: true);

        [Serializable]
        private class JournalExport { public List<JournalEntry> entries; }
    }
}
