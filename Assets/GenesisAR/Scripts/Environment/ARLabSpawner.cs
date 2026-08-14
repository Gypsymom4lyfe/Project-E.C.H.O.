using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GenesisAR.Environment
{
    // ─────────────────────────────────────────────────────────────
    //  SpawnEntry – links a prefab to a species ID and spawn weight
    // ─────────────────────────────────────────────────────────────
    [System.Serializable]
    public class SpawnEntry
    {
        public string SpeciesID;
        public GameObject Prefab;
        [Range(0f, 1f)] public float Weight = 1f;
    }

    // ─────────────────────────────────────────────────────────────
    //  ARLabSpawner – raycasts against AR planes and places creatures
    // ─────────────────────────────────────────────────────────────
    [RequireComponent(typeof(ARRaycastManager))]
    public class ARLabSpawner : MonoBehaviour
    {
        [Header("Spawn Catalogue")]
        public List<SpawnEntry> SpawnCatalogue = new List<SpawnEntry>();

        [Header("Settings")]
        public int   MaxActiveCreatures  = 20;
        public float MinSpawnInterval    = 2f;
        public float SpawnRadiusFromHit  = 0.5f;
        public bool  RequireTap          = true; // tap-to-place vs auto-spawn

        private ARRaycastManager         _raycastManager;
        private ARPlaneManager           _planeManager;
        private readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();
        private readonly List<GameObject>   _spawned = new List<GameObject>();
        private float _lastSpawnTime;

        private void Awake()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
            _planeManager   = GetComponent<ARPlaneManager>();
        }

        private void Update()
        {
            if (RequireTap)
                HandleTapInput();
            else
                AutoSpawn();

            // Cull despawned references
            _spawned.RemoveAll(g => g == null);
        }

        // ── Tap-to-place ──────────────────────────────────────────
        private void HandleTapInput()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
                TrySpawnAtScreen(Input.mousePosition);
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                TrySpawnAtScreen(Input.GetTouch(0).position);
#endif
        }

        // ── Auto-spawn on detected planes ────────────────────────
        private void AutoSpawn()
        {
            if (Time.time - _lastSpawnTime < MinSpawnInterval) return;
            if (_spawned.Count >= MaxActiveCreatures) return;
            if (_planeManager == null) return;

            foreach (ARPlane plane in _planeManager.trackables)
            {
                Vector3 worldPos = plane.transform.position +
                                   new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                SpawnCreature(worldPos, plane.transform.rotation);
                _lastSpawnTime = Time.time;
                break;
            }
        }

        // ── Raycast and spawn ─────────────────────────────────────
        private void TrySpawnAtScreen(Vector2 screenPos)
        {
            if (_spawned.Count >= MaxActiveCreatures) return;

            if (_raycastManager.Raycast(screenPos, _hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = _hits[0].pose;
                SpawnCreature(hitPose.position, hitPose.rotation);
            }
        }

        private void SpawnCreature(Vector3 position, Quaternion rotation)
        {
            SpawnEntry entry = PickWeighted();
            if (entry?.Prefab == null) return;

            Vector3 offset = Random.insideUnitSphere * SpawnRadiusFromHit;
            offset.y = 0f;

            GameObject instance = Instantiate(entry.Prefab, position + offset, rotation);
            _spawned.Add(instance);

            AI.CreatureAIStateMachine ai = instance.GetComponent<AI.CreatureAIStateMachine>();
            if (ai != null) ai.SpeciesID = entry.SpeciesID;

            Debug.Log($"[ARLabSpawner] Spawned {entry.SpeciesID} at {position}");
        }

        // ── Weighted random selection ─────────────────────────────
        private SpawnEntry PickWeighted()
        {
            if (SpawnCatalogue.Count == 0) return null;
            float total = 0f;
            foreach (SpawnEntry e in SpawnCatalogue) total += e.Weight;
            float roll = Random.Range(0f, total);
            float acc  = 0f;
            foreach (SpawnEntry e in SpawnCatalogue)
            {
                acc += e.Weight;
                if (roll <= acc) return e;
            }
            return SpawnCatalogue[SpawnCatalogue.Count - 1];
        }

        // ── Public API ────────────────────────────────────────────
        public void DespawnAll()
        {
            foreach (GameObject g in _spawned)
                if (g != null) Destroy(g);
            _spawned.Clear();
        }

        public int ActiveCount => _spawned.Count;
    }
}
