// TelemetryExporters.cs
// Genesis AR – Project E.C.H.O.
// Built-in ITelemetryExporter implementations for common sinks.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GenesisAR.Telemetry.Exporters
{
    /// <summary>
    /// Exports telemetry events as JSON lines to the debug output / console.
    /// Suitable for local development and unit tests.
    /// </summary>
    public sealed class ConsoleJsonTelemetryExporter : ITelemetryExporter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented       = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <inheritdoc/>
        public string Name => "ConsoleJson";

        /// <inheritdoc/>
        public async Task<ExportResult> ExportAsync(
            IReadOnlyList<TelemetryEvent> events,
            CancellationToken             cancellationToken = default)
        {
            int exported = 0;

            foreach (var evt in events)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var record = new
                {
                    eventId       = evt.EventId,
                    timestamp     = evt.Timestamp,
                    name          = evt.Name,
                    category      = evt.Category.ToString(),
                    severity      = evt.Severity.ToString(),
                    correlationId = evt.CorrelationId,
                    properties    = evt.Properties
                };

                var json = JsonSerializer.Serialize(record, JsonOptions);
                Console.WriteLine(json);
                Debug.WriteLine($"[TELEMETRY] {json}");
                exported++;
            }

            await Task.CompletedTask.ConfigureAwait(false);
            return ExportResult.Succeeded(exported);
        }

        /// <inheritdoc/>
        public Task FlushAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <inheritdoc/>
        public void Dispose() { /* stateless */ }
    }

    /// <summary>
    /// No-op exporter. Useful in tests or when telemetry must be silenced.
    /// </summary>
    public sealed class NullTelemetryExporter : ITelemetryExporter
    {
        /// <inheritdoc/>
        public string Name => "Null";

        /// <inheritdoc/>
        public Task<ExportResult> ExportAsync(
            IReadOnlyList<TelemetryEvent> events,
            CancellationToken             cancellationToken = default) =>
            Task.FromResult(ExportResult.Succeeded(events.Count));

        /// <inheritdoc/>
        public Task FlushAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <inheritdoc/>
        public void Dispose() { }
    }

    /// <summary>
    /// In-memory exporter that stores received events for later inspection.
    /// Intended for integration tests.
    /// </summary>
    public sealed class InMemoryTelemetryExporter : ITelemetryExporter
    {
        private readonly List<TelemetryEvent> _received = new();
        private readonly object               _lock     = new();

        /// <inheritdoc/>
        public string Name => "InMemory";

        /// <summary>All events received so far (thread-safe snapshot).</summary>
        public IReadOnlyList<TelemetryEvent> ReceivedEvents
        {
            get
            {
                lock (_lock) return _received.ToList();
            }
        }

        /// <inheritdoc/>
        public Task<ExportResult> ExportAsync(
            IReadOnlyList<TelemetryEvent> events,
            CancellationToken             cancellationToken = default)
        {
            lock (_lock) _received.AddRange(events);
            return Task.FromResult(ExportResult.Succeeded(events.Count));
        }

        /// <inheritdoc/>
        public Task FlushAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <summary>Clears all stored events.</summary>
        public void Clear()
        {
            lock (_lock) _received.Clear();
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
