using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GenesisAR.Avatar
{
    // ─────────────────────────────────────────────────────────────
    //  BodyMeasurements – morphometric data from the volumetric scan
    // ─────────────────────────────────────────────────────────────
    [System.Serializable]
    public class BodyMeasurements
    {
        public float HeightMetres;
        public float ShoulderWidthMetres;
        public float ChestCircumferenceMetres;
        public float WaistCircumferenceMetres;
        public float ArmLengthMetres;
        public float LegLengthMetres;

        public override string ToString() =>
            $"H={HeightMetres:F2}m  Shoulder={ShoulderWidthMetres:F2}m  " +
            $"Chest={ChestCircumferenceMetres:F2}m  Waist={WaistCircumferenceMetres:F2}m";
    }

    // ─────────────────────────────────────────────────────────────
    //  StudentAvatarScanner – volumetric body scan via AR depth and
    //  morphs a rigged digital-twin mesh to match the student's body
    // ─────────────────────────────────────────────────────────────
    public class StudentAvatarScanner : MonoBehaviour
    {
        [Header("AR Components")]
        public AROcclusionManager OcclusionManager;
        public ARCameraManager    CameraManager;

        [Header("Digital Twin")]
        [Tooltip("The rigged humanoid mesh that will be morphed")]
        public SkinnedMeshRenderer AvatarMesh;

        [Tooltip("Blend shape indices matching measurement axes (Height, Shoulder, Chest, Waist)")]
        public int[] MorphBlendShapeIndices = new int[] { 0, 1, 2, 3 };

        [Header("Scan Settings")]
        public float ScanDurationSeconds = 3f;
        public bool  AutoScanOnStart     = false;

        [Header("Current Data")]
        [SerializeField] private BodyMeasurements _measurements = new BodyMeasurements();
        [SerializeField] private bool _scanComplete = false;

        public bool         ScanComplete   => _scanComplete;
        public BodyMeasurements Measurements => _measurements;

        // Events
        public System.Action<BodyMeasurements> OnScanComplete;
        public System.Action<float>            OnScanProgress; // 0-1

        private void Start()
        {
            if (AutoScanOnStart) StartScan();
        }

        // ── Public entry points ───────────────────────────────────
        public void StartScan()
        {
            if (_scanComplete)
            {
                Debug.LogWarning("[AvatarScanner] Scan already complete. Call Reset() first.");
                return;
            }
            StartCoroutine(PerformScan());
        }

        public void Reset()
        {
            StopAllCoroutines();
            _scanComplete = false;
            _measurements = new BodyMeasurements();
            ClearMorphTargets();
            Debug.Log("[AvatarScanner] Scan reset.");
        }

        // ── Scan coroutine ────────────────────────────────────────
        private IEnumerator PerformScan()
        {
            Debug.Log("[AvatarScanner] Volumetric scan starting…");
            float elapsed = 0f;

            while (elapsed < ScanDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / ScanDurationSeconds;
                OnScanProgress?.Invoke(progress);

                // Accumulate depth-based estimates each frame
                AccumulateDepthSample(progress);

                yield return null;
            }

            FinaliseeMeasurements();
            ApplyMorphTargets();
            _scanComplete = true;

            Debug.Log($"[AvatarScanner] Scan complete: {_measurements}");
            OnScanComplete?.Invoke(_measurements);
        }

        // ── Depth sampling (simplified – real impl uses AR depth texture) ──
        private float _heightAcc, _shoulderAcc, _chestAcc, _waistAcc;
        private int   _sampleCount;

        private void AccumulateDepthSample(float progress)
        {
            // Simulated depth read – in production this reads
            // OcclusionManager.humanDepthTexture / environmentDepthTexture
            _heightAcc   += Random.Range(1.55f, 1.95f);
            _shoulderAcc += Random.Range(0.38f, 0.55f);
            _chestAcc    += Random.Range(0.85f, 1.10f);
            _waistAcc    += Random.Range(0.70f, 1.00f);
            _sampleCount++;
        }

        private void FinaliseeMeasurements()
        {
            if (_sampleCount == 0) return;
            _measurements.HeightMetres              = _heightAcc   / _sampleCount;
            _measurements.ShoulderWidthMetres       = _shoulderAcc / _sampleCount;
            _measurements.ChestCircumferenceMetres  = _chestAcc    / _sampleCount;
            _measurements.WaistCircumferenceMetres  = _waistAcc    / _sampleCount;
            _measurements.ArmLengthMetres           = _measurements.HeightMetres * 0.33f;
            _measurements.LegLengthMetres           = _measurements.HeightMetres * 0.48f;

            // Reset accumulators
            _heightAcc = _shoulderAcc = _chestAcc = _waistAcc = 0f;
            _sampleCount = 0;
        }

        // ── Morph target application ──────────────────────────────
        private void ApplyMorphTargets()
        {
            if (AvatarMesh == null) return;

            // Map measurements to 0-100 blend-shape weight ranges
            float[] weights = new float[]
            {
                Mathf.InverseLerp(1.40f, 2.10f, _measurements.HeightMetres)              * 100f,
                Mathf.InverseLerp(0.30f, 0.65f, _measurements.ShoulderWidthMetres)       * 100f,
                Mathf.InverseLerp(0.75f, 1.30f, _measurements.ChestCircumferenceMetres)  * 100f,
                Mathf.InverseLerp(0.60f, 1.20f, _measurements.WaistCircumferenceMetres)  * 100f,
            };

            for (int i = 0; i < MorphBlendShapeIndices.Length && i < weights.Length; i++)
                AvatarMesh.SetBlendShapeWeight(MorphBlendShapeIndices[i], weights[i]);

            Debug.Log("[AvatarScanner] Morph targets applied.");
        }

        private void ClearMorphTargets()
        {
            if (AvatarMesh == null) return;
            for (int i = 0; i < MorphBlendShapeIndices.Length; i++)
                AvatarMesh.SetBlendShapeWeight(MorphBlendShapeIndices[i], 0f);
        }
    }
}
