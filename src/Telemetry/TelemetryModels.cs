// TelemetryModels.cs
// Genesis AR – Project E.C.H.O.
// Shared telemetry value types and enumerations.

using System;
using System.Collections.Generic;

namespace GenesisAR.Telemetry
{
    /// <summary>
    /// Categories of telemetry events produced by the Genesis AR ecosystem.
    /// </summary>
    public enum TelemetryCategory
    {
        Ecosystem,
        Creature,
        Player,
        Session,
        Performance,
        Error
    }

    /// <summary>
    /// Severity level attached to a telemetry event.
    /// </summary>
    public enum TelemetrySeverity
    {
        Verbose,
        Information,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Immutable record representing a single telemetry event captured
    /// anywhere in the Genesis AR pipeline.
    /// </summary>
    public sealed class TelemetryEvent
    {
        /// <summary>Globally unique identifier for this event.</summary>
        public Guid EventId { get; }

        /// <summary>UTC timestamp at which the event was recorded.</summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>Logical name / schema for the event (e.g. "creature.spawn").</summary>
        public string Name { get; }

        /// <summary>High-level grouping for routing and filtering.</summary>
        public TelemetryCategory Category { get; }

        /// <summary>Importance level for alerting and sampling decisions.</summary>
        public TelemetrySeverity Severity { get; }

        /// <summary>Arbitrary key-value payload associated with the event.</summary>
        public IReadOnlyDictionary<string, object?> Properties { get; }

        /// <summary>Optional correlation token that ties related events together.</summary>
        public string? CorrelationId { get; }

        /// <summary>
        /// Constructs a new <see cref="TelemetryEvent"/>.
        /// </summary>
        public TelemetryEvent(
            string name,
            TelemetryCategory category,
            TelemetrySeverity severity = TelemetrySeverity.Information,
            Dictionary<string, object?>? properties = null,
            string? correlationId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Event name must not be empty.", nameof(name));

            EventId       = Guid.NewGuid();
            Timestamp     = DateTimeOffset.UtcNow;
            Name          = name;
            Category      = category;
            Severity      = severity;
            Properties    = properties != null
                            ? new Dictionary<string, object?>(properties)
                            : new Dictionary<string, object?>();
            CorrelationId = correlationId;
        }
    }

    /// <summary>
    /// Snapshot of real-time ecosystem metrics pushed to the telemetry pipeline
    /// on a scheduled interval.
    /// </summary>
    public sealed class EcosystemMetricsSnapshot
    {
        /// <summary>UTC time this snapshot was captured.</summary>
        public DateTimeOffset CapturedAt { get; init; }

        /// <summary>Total number of living creatures across all species.</summary>
        public int TotalCreatureCount { get; init; }

        /// <summary>Number of distinct species currently active.</summary>
        public int ActiveSpeciesCount { get; init; }

        /// <summary>Current biodiversity index (0.0 – 1.0).</summary>
        public double BiodiversityIndex { get; init; }

        /// <summary>Average genetic fitness score across all creatures.</summary>
        public double AverageGeneticFitness { get; init; }

        /// <summary>Current global resource availability level (0.0 – 1.0).</summary>
        public double ResourceAvailability { get; init; }

        /// <summary>Number of extinction events in the current session.</summary>
        public int ExtinctionEvents { get; init; }

        /// <summary>Current AR rendering frame rate (FPS).</summary>
        public float FramesPerSecond { get; init; }

        /// <summary>Memory used by the AR process in megabytes.</summary>
        public long MemoryUsageMb { get; init; }
    }
}
