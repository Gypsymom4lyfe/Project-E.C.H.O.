// ITelemetryExporter.cs
// Genesis AR – Project E.C.H.O.
// Abstraction for pluggable telemetry export destinations.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GenesisAR.Telemetry
{
    /// <summary>
    /// Implemented by any class that can receive and forward a batch of
    /// <see cref="TelemetryEvent"/> objects to an external sink
    /// (e.g. Azure Monitor, OpenTelemetry collector, local file, etc.).
    /// </summary>
    public interface ITelemetryExporter : IDisposable
    {
        /// <summary>
        /// Human-readable name used in diagnostics and logging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Exports a batch of events to the underlying destination.
        /// Implementations must be idempotent-safe and must not throw
        /// for individual event failures; they should instead surface
        /// failures via the returned <see cref="ExportResult"/>.
        /// </summary>
        /// <param name="events">Events to export (never null or empty).</param>
        /// <param name="cancellationToken">Propagates cancellation.</param>
        /// <returns>Result describing success or partial failure.</returns>
        Task<ExportResult> ExportAsync(
            IReadOnlyList<TelemetryEvent> events,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes any internal buffers and waits for in-flight exports to complete.
        /// Called during graceful shutdown.
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Describes the outcome of a single export attempt.
    /// </summary>
    public sealed class ExportResult
    {
        /// <summary>Whether all events were exported successfully.</summary>
        public bool Success { get; init; }

        /// <summary>Number of events that were successfully exported.</summary>
        public int ExportedCount { get; init; }

        /// <summary>Number of events that failed to export.</summary>
        public int FailedCount { get; init; }

        /// <summary>Optional diagnostic message when <see cref="Success"/> is false.</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>Optional inner exception captured during export.</summary>
        public Exception? Exception { get; init; }

        /// <summary>Returns a successful result for the given count.</summary>
        public static ExportResult Succeeded(int count) =>
            new() { Success = true, ExportedCount = count };

        /// <summary>Returns a failed result with the given error details.</summary>
        public static ExportResult Failed(int failed, string message, Exception? ex = null) =>
            new() { Success = false, FailedCount = failed, ErrorMessage = message, Exception = ex };
    }
}
