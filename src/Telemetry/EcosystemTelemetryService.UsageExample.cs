// EcosystemTelemetryService.UsageExample.cs
// Genesis AR – Project E.C.H.O.
// Annotated usage example — NOT production code.
// Demonstrates wiring and calling the telemetry pipeline from a game session.

#if ECHO_EXAMPLES

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenesisAR.Telemetry;
using GenesisAR.Telemetry.Exporters;

namespace GenesisAR.Examples
{
    internal static class TelemetryUsageExample
    {
        internal static async Task RunAsync()
        {
            // ── 1. Configure and create the service ──────────────────────────

            var options = new TelemetryServiceOptions
            {
                AppVersion                = "1.0.0",
                Environment               = "production",
                EnablePeriodicMetrics     = true,
                MetricsInterval           = TimeSpan.FromSeconds(30),
                PerformanceAlertThresholdMs = 250,
                BatchSize                 = 100,
                FlushInterval             = TimeSpan.FromSeconds(5),

                // Drop 90 % of high-volume Verbose/Performance events in production.
                SamplingRates = new()
                {
                    [TelemetryCategory.Performance] = new()
                    {
                        [TelemetrySeverity.Verbose] = 0.10
                    },
                    [TelemetryCategory.Ecosystem] = new()
                    {
                        [TelemetrySeverity.Verbose] = 0.50
                    }
                }
            };

            // Register exporters — production would include an Azure / OTel exporter here.
            var consoleExporter  = new ConsoleJsonTelemetryExporter();
            var inMemoryExporter = new InMemoryTelemetryExporter(); // useful for integration tests

            await using var telemetry = new EcosystemTelemetryService(
                new[] { (ITelemetryExporter)consoleExporter, inMemoryExporter },
                options);

            // ── 2. Register ecosystem metrics provider ────────────────────────

            // In a real game, this delegate would read from the live ecosystem sim.
            telemetry.RegisterMetricsProvider(() => new EcosystemMetricsSnapshot
            {
                CapturedAt            = DateTimeOffset.UtcNow,
                TotalCreatureCount    = 1_200,
                ActiveSpeciesCount    = 47,
                BiodiversityIndex     = 0.83,
                AverageGeneticFitness = 0.71,
                ResourceAvailability  = 0.62,
                ExtinctionEvents      = 3,
                FramesPerSecond       = 60.0f,
                MemoryUsageMb         = 512
            });

            // ── 3. Track events throughout the session ────────────────────────

            // Session start
            telemetry.TrackEvent(
                "session.started",
                TelemetryCategory.Session,
                TelemetrySeverity.Information,
                new() { ["device"] = "HoloLens 2", ["region"] = "us-west" });

            // Player action
            telemetry.TrackPlayerAction(
                action:    "gene_splice",
                playerId:  "player-abc-123",
                properties: new()
                {
                    ["creature_id"] = "creature-xyz-789",
                    ["gene_used"]   = "photosynthesis",
                    ["slot"]        = 2
                },
                correlationId: "round-42");

            // Performance measurement
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // ... AR scene load ...
            watch.Stop();
            telemetry.TrackPerformance("ar_scene_load", watch.Elapsed);

            // Exception tracking
            try
            {
                throw new InvalidOperationException("Simulated gene conflict.");
            }
            catch (Exception ex)
            {
                telemetry.TrackException(
                    ex,
                    TelemetrySeverity.Warning,
                    new() { ["creature_id"] = "creature-xyz-789" },
                    correlationId: "round-42");
            }

            // Manual metrics snapshot
            telemetry.TrackMetrics(new EcosystemMetricsSnapshot
            {
                CapturedAt         = DateTimeOffset.UtcNow,
                TotalCreatureCount = 850,
                ActiveSpeciesCount = 32,
                BiodiversityIndex  = 0.55,
                FramesPerSecond    = 58.4f,
                MemoryUsageMb      = 490
            });

            // ── 4. Check pipeline health ──────────────────────────────────────

            var health = telemetry.GetHealth();
            Console.WriteLine(
                $"[Health] buffered={health.BufferedEvents} " +
                $"exported={health.ExportedEvents} " +
                $"dropped={health.DroppedEvents}");

            // ── 5. Flush before session end ───────────────────────────────────

            await telemetry.FlushAsync();

            Console.WriteLine(
                $"[InMemory] {inMemoryExporter.ReceivedEvents.Count} events captured.");

            // DisposeAsync is called automatically by `await using`.
        }
    }
}

#endif
