using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GenesisAR.Genetics;

namespace GenesisAR.UI
{
    // ─────────────────────────────────────────────────────────────
    //  GeneSlot – a draggable holographic gene chip in the splicer UI
    // ─────────────────────────────────────────────────────────────
    public class GeneSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Gene GeneData;
        public Text LabelText;
        public Image BackgroundImage;

        [Header("Colours")]
        public Color DominantColor  = new Color(1f, 0.8f, 0.1f);
        public Color RecessiveColor = new Color(0.4f, 0.7f, 1.0f);

        private Transform _originalParent;
        private Vector3   _originalPosition;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Populate(Gene gene)
        {
            GeneData = gene;
            if (LabelText      != null) LabelText.text       = $"{gene.Name}\n{gene.Value:F2}";
            if (BackgroundImage != null) BackgroundImage.color = gene.IsDominant ? DominantColor : RecessiveColor;
        }

        // ── Drag handlers ─────────────────────────────────────────
        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent   = transform.parent;
            _originalPosition = transform.localPosition;
            transform.SetParent(transform.root);           // float above canvas
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;

            // If not dropped on a valid target, return home
            if (transform.parent == transform.root)
            {
                transform.SetParent(_originalParent);
                transform.localPosition = _originalPosition;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  SpliceDropZone – accepts dropped GeneSlots and records splice
    // ─────────────────────────────────────────────────────────────
    public class SpliceDropZone : MonoBehaviour, IDropHandler
    {
        public bool  IsMaternalSide = true;
        public ARGeneSplicerUI ParentUI;

        public void OnDrop(PointerEventData eventData)
        {
            GeneSlot slot = eventData.pointerDrag?.GetComponent<GeneSlot>();
            if (slot == null) return;

            slot.transform.SetParent(transform);
            slot.transform.localPosition = Vector3.zero;
            ParentUI?.RegisterSplicedGene(slot.GeneData, IsMaternalSide);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ARGeneSplicerUI – holographic drag-and-drop DNA splicing panel
    // ─────────────────────────────────────────────────────────────
    public class ARGeneSplicerUI : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject GeneSlotPrefab;

        [Header("Layout")]
        public Transform GeneLibraryPanel;   // source slots
        public Transform MaternalDropZone;
        public Transform PaternalDropZone;
        public Transform ResultPanel;

        [Header("Species")]
        public string TargetSpeciesID = "wolf_grey";

        [Header("Result")]
        public Text ResultLabel;
        public Text ViabilityLabel;
        public Button SpliceButton;

        private List<Gene> _maternalSpliced = new List<Gene>();
        private List<Gene> _paternalSpliced  = new List<Gene>();

        private void Start()
        {
            PopulateLibrary();
            if (SpliceButton != null) SpliceButton.onClick.AddListener(OnSplicePressed);
        }

        // ── Populate library from Earth Genome Database ───────────
        private void PopulateLibrary()
        {
            if (!EarthGenomeDatabase.TryGet(TargetSpeciesID, out GenomeTemplate template))
            {
                Debug.LogWarning($"[GeneSplicerUI] Species '{TargetSpeciesID}' not in database.");
                return;
            }

            foreach (Transform child in GeneLibraryPanel)
                Destroy(child.gameObject);

            foreach (Gene gene in template.BaseGenes)
            {
                GameObject obj  = Instantiate(GeneSlotPrefab, GeneLibraryPanel);
                GeneSlot   slot = obj.GetComponent<GeneSlot>();
                slot?.Populate(gene);
            }
        }

        // ── Called by drop zones ──────────────────────────────────
        public void RegisterSplicedGene(Gene gene, bool isMatermal)
        {
            if (isMatermal) _maternalSpliced.Add(gene);
            else            _paternalSpliced.Add(gene);

            Debug.Log($"[GeneSplicerUI] Gene spliced – {(isMatermal ? "Maternal" : "Paternal")}: {gene.Name}={gene.Value:F2}");
        }

        // ── Splice button ─────────────────────────────────────────
        private void OnSplicePressed()
        {
            if (_maternalSpliced.Count == 0 || _paternalSpliced.Count == 0)
            {
                if (ResultLabel != null) ResultLabel.text = "Drag at least one gene to each strand first.";
                return;
            }

            // Build a custom genome from spliced genes
            Genome genome = new Genome(TargetSpeciesID);
            genome.MaternalChromosome.AddRange(_maternalSpliced);
            genome.PaternalChromosome.AddRange(_paternalSpliced);

            Phenotype phenotype = GeneticsSystem.ExpressPhenotype(genome);
            GeneticViabilityEngine.ViabilityResult viability =
                GeneticViabilityEngine.GeneticViabilityEngine.Assess(genome);

            if (ResultLabel   != null) ResultLabel.text   = phenotype.ToString();
            if (ViabilityLabel != null)
            {
                ViabilityLabel.text  = viability.Status.ToString();
                ViabilityLabel.color = viability.Status == GeneticViabilityEngine.ViabilityStatus.Viable
                    ? Color.green
                    : viability.Status == GeneticViabilityEngine.ViabilityStatus.Marginal
                        ? Color.yellow
                        : Color.red;
            }

            Debug.Log($"[GeneSplicerUI] Splice result:\n{viability.DiagnosticReport()}");
        }

        // ── Public ────────────────────────────────────────────────
        public void ClearSplice()
        {
            _maternalSpliced.Clear();
            _paternalSpliced.Clear();
            foreach (Transform child in MaternalDropZone) Destroy(child.gameObject);
            foreach (Transform child in PaternalDropZone)  Destroy(child.gameObject);
            if (ResultLabel    != null) ResultLabel.text    = "";
            if (ViabilityLabel != null) ViabilityLabel.text = "";
        }

        public void SetSpecies(string speciesID)
        {
            TargetSpeciesID = speciesID;
            ClearSplice();
            PopulateLibrary();
        }
    }
}
