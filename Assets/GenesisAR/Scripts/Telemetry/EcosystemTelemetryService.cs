using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GenesisAR.Ecosystem;

namespace GenesisAR.Telemetry
{
    // ─────────────────────────────────────────────────────────────
    //  TelemetryPayload – JSON-serialisable snapshot
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class PopulationSnapshot
    {
        public string speciesID;
        public float  population;
        public float  birthRate;
        public float  deathRate;
    }

    [Serializable]
    public class TelemetryPayload
    {
        public string sessionID;
        public string timestamp;
        public string biome;
        public float  simElapsedSeconds;
        public List<PopulationSnapshot> populations = new List<PopulationSnapshot>();
        public Dictionary<string, string> customFields = new Dictionary<string, string>();
    }

    // ─────────────────────────────────────────────────────────────
    //  EcosystemTelemetryService – builds JSON payloads and streams
    //  them to an edge endpoint (configurable URL) via UnityWebRequest
    // ─────────────────────────────────────────────────────────────
    public class EcosystemTelemetryService : MonoBehaviour
    {
        [Header("Session")]
        public string SessionID = System.Guid.NewGuid().ToString();

        [Header("Edge Endpoint")]
        [Tooltip("REST endpoint that accepts POST with application/json body")]
        public string EndpointURL = "https://echo-telemetry.example.com/api/v1/ecosystem";
        public float  StreamIntervalSeconds = 5f;
        public bool   StreamEnabled = true;

        [Header("Diagnostics")]
        public bool LogPayloadToConsole = true;

        private float        _elapsed;
        private PlanetEcosystemManager _ecosystemManager;
        private Coroutine    _streamCoroutine;

        private void Start()
        {
            _ecosystemManager = FindObjectOfType<PlanetEcosystemManager>();
            PlanetEcosystemManager.OnTickComplete += CacheLatestPopulations;

            if (StreamEnabled)
                _streamCoroutine = StartCoroutine(StreamLoop());
        }

        private void OnDestroy()
        {
            PlanetEcosystemManager.OnTickComplete -= CacheLatestPopulations;
            if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
        }

        // ── Cache latest tick data ────────────────────────────────
        private List<PopulationRecord> _latestPopulations = new List<PopulationRecord>();

        private void CacheLatestPopulations(List<PopulationRecord> populations)
        {
            _latestPopulations = new List<PopulationRecord>(populations);
        }

        private void Update() => _elapsed += Time.deltaTime;

        // ── Payload builder ───────────────────────────────────────
        public TelemetryPayload BuildPayload(Dictionary<string, string> extra = null)
        {
            TelemetryPayload payload = new TelemetryPayload
            {
                sessionID          = SessionID,
                timestamp          = DateTime.UtcNow.ToString("o"),
                biome              = _ecosystemManager != null
                                       ? _ecosystemManager.CurrentBiome.Name
                                       : "Unknown",
                simElapsedSeconds  = _elapsed,
            };

            foreach (PopulationRecord rec in _latestPopulations)
            {
                payload.populations.Add(new PopulationSnapshot
                {
                    speciesID  = rec.SpeciesID,
                    population = rec.Population,
                    birthRate  = rec.BirthRate,
                    deathRate  = rec.DeathRate
                });
            }

            if (extra != null)
                foreach (var kv in extra) payload.customFields[kv.Key] = kv.Value;

            return payload;
        }

        public string SerialisePayload(TelemetryPayload payload) =>
            JsonUtility.ToJson(payload, prettyPrint: true);

        // ── Streaming loop ────────────────────────────────────────
        private IEnumerator StreamLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(StreamIntervalSeconds);
                TelemetryPayload payload = BuildPayload();
                string json = SerialisePayload(payload);

                if (LogPayloadToConsole)
                    Debug.Log($"[Telemetry] Payload:\n{json}");

                yield return StartCoroutine(PostPayload(json));
            }
        }

        private IEnumerator PostPayload(string json)
        {
            if (string.IsNullOrEmpty(EndpointURL) ||
                EndpointURL.StartsWith("https://echo-telemetry.example.com"))
            {
                // Simulated endpoint – skip real network call
                yield break;
            }

            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityEngine.Networking.UnityWebRequest req =
                   new UnityEngine.Networking.UnityWebRequest(EndpointURL, "POST"))
            {
                req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    Debug.LogError($"[Telemetry] POST failed: {req.error}");
                else
                    Debug.Log($"[Telemetry] POST {req.responseCode} OK");
            }
        }

        // ── Public API ────────────────────────────────────────────
        public void SendImmediate(Dictionary<string, string> extra = null) =>
            StartCoroutine(PostPayload(SerialisePayload(BuildPayload(extra))));
    }
}
