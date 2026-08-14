using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using GenesisAR.Ecosystem;

namespace GenesisAR.Telemetry
{
    // ------------------------------------------------------------------
    // TELEMETRY DATA CONTRACTS (JSON Serializable)
    // ------------------------------------------------------------------
    [Serializable]
    public class SpeciesTelemetrySnapshot
    {
        public string speciesName;
        public bool isPredator;
        public int currentPopulation;
        public float optimalTemp;
    }

    [Serializable]
    public class BiomeTelemetrySnapshot
    {
        public string biomeName;
        public string biomeType;
        public float temperature;
        public float resourceCarryingCapacity;
        public float currentResources;
        public float biomeHealthIndex; // Calculated 0.0 - 1.0 (Resource ratio & balance)
        public List<SpeciesTelemetrySnapshot> species = new List<SpeciesTelemetrySnapshot>();
    }

    [Serializable]
    public class EcosystemTelemetryPayload
    {
        public string sessionID;
        public string timestamp;
        public long epochTimestamp;
        public int totalActiveBiomes;
        public int totalGlobalPopulation;
        public List<BiomeTelemetrySnapshot> biomes = new List<BiomeTelemetrySnapshot>();
    }

    // ------------------------------------------------------------------
    // TELEMETRY SERVICE PIPELINE
    // ------------------------------------------------------------------
    public class EcosystemTelemetryService : MonoBehaviour
    {
        [Header("Telemetry Configuration")]
        [Tooltip("Target backend ingestion endpoint URL")]
        public string telemetryEndpointURL = "https://api.genesis-ar.io/v1/telemetry/ecosystem";

        [Tooltip("Frequency in seconds between telemetry transmissions")]
        public float uploadIntervalSeconds = 10.0f;

        public bool enableConsoleLogging = true;
        public bool enableCloudStreaming = false; // Set true when live backend is connected

        [Header("Ecosystem Reference")]
        public PlanetEcosystemManager ecosystemManager;

        private string currentSessionID;
        private float telemetryTimer = 0f;

        private void Awake()
        {
            // Generate a unique session GUID for persistent session tracking
            currentSessionID = Guid.NewGuid().ToString();
        }

        private void Start()
        {
            if (ecosystemManager == null)
            {
                ecosystemManager = GetComponent<PlanetEcosystemManager>();
            }
        }

        private void Update()
        {
            telemetryTimer += Time.deltaTime;
            if (telemetryTimer >= uploadIntervalSeconds)
            {
                telemetryTimer = 0f;
                CaptureAndEmitTelemetry();
            }
        }

        /// <summary>
        /// Captures the current ecosystem state and emits it as a telemetry payload.
        /// </summary>
        public void CaptureAndEmitTelemetry()
        {
            if (ecosystemManager == null || ecosystemManager.biomes == null) return;

            EcosystemTelemetryPayload payload = BuildTelemetryPayload();

            // Convert data model to JSON
            string jsonPayload = JsonUtility.ToJson(payload, true);

            if (enableConsoleLogging)
            {
                Debug.Log($"[Genesis AR Telemetry] Captured Snapshot (Session: {currentSessionID}):\n{jsonPayload}");
            }

            if (enableCloudStreaming && !string.IsNullOrEmpty(telemetryEndpointURL))
            {
                StartCoroutine(PostTelemetryCoroutine(jsonPayload));
            }
        }

        private EcosystemTelemetryPayload BuildTelemetryPayload()
        {
            EcosystemTelemetryPayload payload = new EcosystemTelemetryPayload
            {
                sessionID = currentSessionID,
                timestamp = DateTime.UtcNow.ToString("o"),
                epochTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                totalActiveBiomes = ecosystemManager.biomes.Count,
                totalGlobalPopulation = 0
            };

            foreach (var biome in ecosystemManager.biomes)
            {
                BiomeTelemetrySnapshot biomeSnapshot = new BiomeTelemetrySnapshot
                {
                    biomeName = biome.biomeName,
                    biomeType = biome.type.ToString(),
                    temperature = biome.temperature,
                    resourceCarryingCapacity = biome.resourceCarryingCapacity,
                    currentResources = biome.currentResources,
                    // Health Index = Resource Ratio (0.0 to 1.0)
                    biomeHealthIndex = Mathf.Clamp01(biome.currentResources / Mathf.Max(1f, biome.resourceCarryingCapacity))
                };

                foreach (var species in biome.speciesInBiome)
                {
                    int roundedPop = Mathf.RoundToInt(species.populationCount);
                    payload.totalGlobalPopulation += roundedPop;

                    biomeSnapshot.species.Add(new SpeciesTelemetrySnapshot
                    {
                        speciesName = species.speciesName,
                        isPredator = species.isPredator,
                        currentPopulation = roundedPop,
                        optimalTemp = species.optimalTemperature
                    });
                }

                payload.biomes.Add(biomeSnapshot);
            }

            return payload;
        }

        /// <summary>
        /// Asynchronously posts the JSON telemetry payload to the configured cloud backend endpoint.
        /// </summary>
        private IEnumerator PostTelemetryCoroutine(string jsonBody)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(telemetryEndpointURL, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Genesis-Session-Id", currentSessionID);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (enableConsoleLogging)
                    {
                        Debug.Log("[Genesis AR Telemetry] Successfully posted telemetry to cloud backend.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Genesis AR Telemetry] Failed to post telemetry. Error: {request.error}");
                }
            }
        }
    }
}
