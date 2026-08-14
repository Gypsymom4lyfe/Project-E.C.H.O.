// EcosystemTelemetryService.cs
// Genesis AR – Project E.C.H.O.
// Production-ready telemetry pipeline for the Genesis AR ecosystem.
//
// Architecture overview
// ─────────────────────
//  • Events are captured via TrackEvent() / TrackMetrics() on any thread.
//  • A thread-safe in-memory Channel buffers events so callers never block.
//  • A background Task drains the channel and flushes batches to all
//    registered ITelemetryExporter instances.
//  • A periodic timer snapshots EcosystemMetricsSnapshot objects and
//    converts them to TelemetryEvents automatically.
//  • Adaptive sampling is applied per-category so high-volume Verbose
//    events do not overwhelm exporters in production.
//  • The service implements IAsyncDisposable for clean shutdown with flush.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GenesisAR.Telemetry
{
    /// <summary>
    /// Central telemetry pipeline for Genesis AR.
    /// Collects, samples, batches and exports ecosystem telemetry events.
    /// </summary>
    public sealed class EcosystemTelemetryService : IAsyncDisposable
    {
        // ─── Constants ────────────────────────────────────────────────────────

        private const int DefaultBatchSize         = 200;
        private const int DefaultChannelCapacity   = 8_000;
        private static readonly TimeSpan DefaultFlushInterval   = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DefaultMetricsInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ShutdownTimeout        = TimeSpan.FromSeconds(10);

        // ─── State ────────────────────────────────────────────────────────────

        private readonly Channel<TelemetryEvent>         _channel;
        private readonly List<ITelemetryExporter>        _exporters;
        private readonly SamplingConfig                  _sampling;
        private readonly TelemetryServiceOptions         _options;
        private readonly string                          _sessionId;
        private readonly Dictionary<string, string>      _globalProperties;

        private readonly CancellationTokenSource         _shutdownCts = new();
        private readonly Task                            _drainTask;
        private readonly Timer?                          _metricsTimer;
        private          Func<EcosystemMetricsSnapshot>? _metricsProvider;

        // Per-category event counters for sampling decisions.
        private readonly long[]  _eventCounters = new long[(int)TelemetryCategory.Error + 2];
        private          long    _droppedEvents;
        private          long    _exportedEvents;

        // ─── Construction ─────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new <see cref="EcosystemTelemetryService"/> and starts the
        /// background drain loop.
        /// </summary>
        /// <param name="exporters">
        ///   One or more export sinks. At least one must be provided.
        /// </param>
        /// <param name="options">Optional pipeline configuration overrides.</param>
        public EcosystemTelemetryService(
            IEnumerable<ITelemetryExporter> exporters,
            TelemetryServiceOptions?        options = null)
        {
            if (exporters is null)
                throw new ArgumentNullException(nameof(exporters));

            _exporters = exporters.ToList();
            if (_exporters.Count == 0)
                throw new ArgumentException(
                    "At least one ITelemetryExporter must be registered.", nameof(exporters));

            _options  = options ?? new TelemetryServiceOptions();
            _sampling = new SamplingConfig(_options.SamplingRates);
            _sessionId = Guid.NewGuid().ToString("N");

            _globalProperties = new Dictionary<string, string>
            {
                ["app.name"]    = "GenesisAR",
                ["app.version"] = _options.AppVersion ?? "unknown",
                ["session.id"]  = _sessionId,
                ["env"]         = _options.Environment ?? "production"
            };

            var channelOptions = new BoundedChannelOptions(
                _options.ChannelCapacity ?? DefaultChannelCapacity)
            {
                FullMode     = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };

            _channel   = Channel.CreateBounded<TelemetryEvent>(channelOptions);
            _drainTask = DrainAsync(_shutdownCts.Token);

            if (_options.EnablePeriodicMetrics)
            {
                _metricsTimer = new Timer(
                    OnMetricsTick,
                    state:     null,
                    dueTime:   _options.MetricsInterval ?? DefaultMetricsInterval,
                    period:    _options.MetricsInterval ?? DefaultMetricsInterval);
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Unique identifier for the current AR session.</summary>
        public string SessionId => _sessionId;

        /// <summary>Total events exported since service start.</summary>
        public long ExportedEventCount => Volatile.Read(ref _exportedEvents);

        /// <summary>Total events dropped due to back-pressure since service start.</summary>
        public long DroppedEventCount => Volatile.Read(ref _droppedEvents);

        /// <summary>
        /// Registers a delegate that the service will call on every metrics tick
        /// to capture a fresh <see cref="EcosystemMetricsSnapshot"/>.
        /// Only effective when <see cref="TelemetryServiceOptions.EnablePeriodicMetrics"/>
        /// is <c>true</c>.
        /// </summary>
        public void RegisterMetricsProvider(Func<EcosystemMetricsSnapshot> provider) =>
            _metricsProvider = provider ?? throw new ArgumentNullException(nameof(provider));

        /// <summary>
        /// Tracks a single telemetry event. Thread-safe; non-blocking.
        /// </summary>
        /// <param name="eventName">Schema name for the event.</param>
        /// <param name="category">High-level category used for routing and sampling.</param>
        /// <param name="severity">Importance level.</param>
        /// <param name="properties">Additional key-value payload.</param>
        /// <param name="correlationId">Optional correlation token.</param>
        public void TrackEvent(
            string                     eventName,
            TelemetryCategory          category        = TelemetryCategory.Ecosystem,
            TelemetrySeverity          severity        = TelemetrySeverity.Information,
            Dictionary<string, object?>? properties    = null,
            string?                    correlationId   = null)
        {
            if (!ShouldSample(category, severity))
            {
                Interlocked.Increment(ref _droppedEvents);
                return;
            }

            var enriched = Enrich(properties);
            var evt = new TelemetryEvent(eventName, category, severity, enriched, correlationId);

            if (!_channel.Writer.TryWrite(evt))
            {
                // Channel full – BoundedChannelFullMode.DropOldest handles eviction,
                // but count the drop for observability.
                Interlocked.Increment(ref _droppedEvents);
            }

            Interlocked.Increment(ref _eventCounters[(int)category]);
        }

        /// <summary>
        /// Tracks an ecosystem metrics snapshot as a structured telemetry event.
        /// </summary>
        public void TrackMetrics(EcosystemMetricsSnapshot snapshot)
        {
            if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

            var props = new Dictionary<string, object?>
            {
                ["metrics.creature_count"]       = snapshot.TotalCreatureCount,
                ["metrics.species_count"]         = snapshot.ActiveSpeciesCount,
                ["metrics.biodiversity"]          = snapshot.BiodiversityIndex,
                ["metrics.genetic_fitness"]       = snapshot.AverageGeneticFitness,
                ["metrics.resource_availability"] = snapshot.ResourceAvailability,
                ["metrics.extinctions"]           = snapshot.ExtinctionEvents,
                ["metrics.fps"]                   = snapshot.FramesPerSecond,
                ["metrics.memory_mb"]             = snapshot.MemoryUsageMb,
                ["metrics.captured_at"]           = snapshot.CapturedAt.ToString("O")
            };

            TrackEvent(
                "ecosystem.metrics_snapshot",
                TelemetryCategory.Performance,
                TelemetrySeverity.Verbose,
                props);
        }

        /// <summary>
        /// Tracks an exception as a Critical or Error telemetry event.
        /// </summary>
        public void TrackException(
            Exception         exception,
            TelemetrySeverity severity       = TelemetrySeverity.Error,
            Dictionary<string, object?>? properties = null,
            string?           correlationId  = null,
            [CallerMemberName] string? caller = null)
        {
            if (exception is null) throw new ArgumentNullException(nameof(exception));

            var props = properties != null
                ? new Dictionary<string, object?>(properties)
                : new Dictionary<string, object?>();

            props["exception.type"]       = exception.GetType().FullName;
            props["exception.message"]    = exception.Message;
            props["exception.stacktrace"] = exception.StackTrace;
            props["exception.caller"]     = caller;

            if (exception.InnerException is { } inner)
                props["exception.inner"] = inner.Message;

            TrackEvent("exception.captured", TelemetryCategory.Error, severity, props, correlationId);
        }

        /// <summary>
        /// Tracks a structured player action event (e.g. gene editing, creature release).
        /// </summary>
        public void TrackPlayerAction(
            string  action,
            string  playerId,
            Dictionary<string, object?>? properties = null,
            string? correlationId = null)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action must not be empty.", nameof(action));
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("PlayerId must not be empty.", nameof(playerId));

            var props = properties != null
                ? new Dictionary<string, object?>(properties)
                : new Dictionary<string, object?>();

            props["player.id"]     = playerId;
            props["player.action"] = action;

            TrackEvent(
                $"player.{action.ToLowerInvariant()}",
                TelemetryCategory.Player,
                TelemetrySeverity.Information,
                props,
                correlationId);
        }

        /// <summary>
        /// Records a timed performance measurement (e.g. AR scene load time).
        /// </summary>
        public void TrackPerformance(
            string   metricName,
            TimeSpan duration,
            Dictionary<string, object?>? properties = null)
        {
            var props = properties != null
                ? new Dictionary<string, object?>(properties)
                : new Dictionary<string, object?>();

            props["perf.metric"]      = metricName;
            props["perf.duration_ms"] = duration.TotalMilliseconds;

            var severity = duration.TotalMilliseconds > (_options.PerformanceAlertThresholdMs ?? 500)
                ? TelemetrySeverity.Warning
                : TelemetrySeverity.Verbose;

            TrackEvent(
                $"perf.{metricName.ToLowerInvariant()}",
                TelemetryCategory.Performance,
                severity,
                props);
        }

        /// <summary>
        /// Flushes all buffered events to exporters and waits for completion.
        /// </summary>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // Signal the channel that no more items are being written for the
            // flush duration by completing a temporary drain; we do a simpler
            // approach: wait until the channel is drained or timeout elapses.
            var deadline = DateTimeOffset.UtcNow.Add(_options.FlushTimeout ?? DefaultFlushInterval);

            while (_channel.Reader.Count > 0 && DateTimeOffset.UtcNow < deadline)
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);

            var flushTasks = _exporters.Select(e => e.FlushAsync(cancellationToken));
            await Task.WhenAll(flushTasks).ConfigureAwait(false);
        }

        // ─── IAsyncDisposable ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _metricsTimer?.Dispose();
            _channel.Writer.TryComplete();

            _shutdownCts.CancelAfter(ShutdownTimeout);

            try
            {
                await _drainTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* expected during shutdown */ }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EcosystemTelemetryService] Error during drain: {ex.Message}");
            }
            finally
            {
                _shutdownCts.Dispose();

                foreach (var exporter in _exporters)
                    exporter.Dispose();
            }
        }

        // ─── Internal pipeline ────────────────────────────────────────────────

        private async Task DrainAsync(CancellationToken ct)
        {
            var batch = new List<TelemetryEvent>(_options.BatchSize ?? DefaultBatchSize);
            var timer = Stopwatch.StartNew();
            var flushInterval = _options.FlushInterval ?? DefaultFlushInterval;

            await foreach (var evt in _channel.Reader.ReadAllAsync(ct)
                                                     .ConfigureAwait(false))
            {
                batch.Add(evt);

                bool batchFull    = batch.Count >= (_options.BatchSize ?? DefaultBatchSize);
                bool timerExpired = timer.Elapsed >= flushInterval;

                if (!batchFull && !timerExpired)
                    continue;

                await FlushBatchAsync(batch, ct).ConfigureAwait(false);
                batch.Clear();
                timer.Restart();
            }

            // Drain any remaining events on shutdown.
            if (batch.Count > 0)
            {
                using var flushCts = new CancellationTokenSource(ShutdownTimeout);
                await FlushBatchAsync(batch, flushCts.Token).ConfigureAwait(false);
            }
        }

        private async Task FlushBatchAsync(
            List<TelemetryEvent> batch,
            CancellationToken    ct)
        {
            if (batch.Count == 0) return;

            var exportTasks = _exporters
                .Select(exporter => ExportSafeAsync(exporter, batch, ct))
                .ToList();

            var results = await Task.WhenAll(exportTasks).ConfigureAwait(false);

            long exported = results.Sum(r => r.ExportedCount);
            Interlocked.Add(ref _exportedEvents, exported);
        }

        private static async Task<ExportResult> ExportSafeAsync(
            ITelemetryExporter         exporter,
            IReadOnlyList<TelemetryEvent> batch,
            CancellationToken          ct)
        {
            try
            {
                return await exporter.ExportAsync(batch, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return ExportResult.Failed(batch.Count, "Export cancelled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[EcosystemTelemetryService] Exporter '{exporter.Name}' threw: {ex.Message}");
                return ExportResult.Failed(batch.Count, ex.Message, ex);
            }
        }

        private void OnMetricsTick(object? _)
        {
            try
            {
                var provider = _metricsProvider;
                if (provider is null) return;

                var snapshot = provider();
                TrackMetrics(snapshot);
            }
            catch (Exception ex)
            {
                TrackException(ex, TelemetrySeverity.Warning);
            }
        }

        // ─── Sampling ─────────────────────────────────────────────────────────

        private bool ShouldSample(TelemetryCategory category, TelemetrySeverity severity)
        {
            // Critical and Error events are never dropped.
            if (severity >= TelemetrySeverity.Error) return true;

            var rate = _sampling.GetRate(category, severity);
            if (rate >= 1.0) return true;
            if (rate <= 0.0) return false;

            return (Interlocked.Read(ref _eventCounters[(int)category]) % 100) <
                   (long)(rate * 100);
        }

        // ─── Enrichment ───────────────────────────────────────────────────────

        private Dictionary<string, object?> Enrich(Dictionary<string, object?>? source)
        {
            var result = source != null
                ? new Dictionary<string, object?>(source)
                : new Dictionary<string, object?>();

            foreach (var kv in _globalProperties)
                result.TryAdd(kv.Key, kv.Value);

            return result;
        }

        // ─── Diagnostics ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns a snapshot of pipeline health metrics for operational dashboards.
        /// </summary>
        public TelemetryPipelineHealth GetHealth() => new()
        {
            SessionId        = _sessionId,
            BufferedEvents   = _channel.Reader.Count,
            ExportedEvents   = ExportedEventCount,
            DroppedEvents    = DroppedEventCount,
            RegisteredExporters = _exporters.Select(e => e.Name).ToArray(),
            CapturedAt       = DateTimeOffset.UtcNow
        };
    }

    // ─── Supporting types ─────────────────────────────────────────────────────

    /// <summary>
    /// Configuration options for <see cref="EcosystemTelemetryService"/>.
    /// All properties are optional; sensible defaults are applied.
    /// </summary>
    public sealed class TelemetryServiceOptions
    {
        /// <summary>Application version string embedded in every event.</summary>
        public string? AppVersion { get; init; }

        /// <summary>Deployment environment tag (e.g. "production", "staging").</summary>
        public string? Environment { get; init; }

        /// <summary>Maximum events held in the in-memory channel before back-pressure kicks in.</summary>
        public int? ChannelCapacity { get; init; }

        /// <summary>Maximum number of events per export batch.</summary>
        public int? BatchSize { get; init; }

        /// <summary>How often the drain loop flushes a partial batch.</summary>
        public TimeSpan? FlushInterval { get; init; }

        /// <summary>Maximum time allowed for a flush operation on disposal.</summary>
        public TimeSpan? FlushTimeout { get; init; }

        /// <summary>Whether to enable the periodic ecosystem metrics timer.</summary>
        public bool EnablePeriodicMetrics { get; init; } = true;

        /// <summary>Interval at which ecosystem metrics snapshots are captured.</summary>
        public TimeSpan? MetricsInterval { get; init; }

        /// <summary>Performance measurements above this threshold (ms) are emitted as Warnings.</summary>
        public double? PerformanceAlertThresholdMs { get; init; }

        /// <summary>
        /// Per-category sampling rates (0.0 – 1.0).
        /// Defaults to 1.0 (100 %) for all categories.
        /// </summary>
        public Dictionary<TelemetryCategory, Dictionary<TelemetrySeverity, double>>? SamplingRates { get; init; }
    }

    /// <summary>
    /// Resolves effective sampling rates for a given category + severity pair.
    /// </summary>
    internal sealed class SamplingConfig
    {
        private readonly Dictionary<TelemetryCategory, Dictionary<TelemetrySeverity, double>> _rates;

        internal SamplingConfig(
            Dictionary<TelemetryCategory, Dictionary<TelemetrySeverity, double>>? overrides)
        {
            _rates = overrides ?? new Dictionary<TelemetryCategory, Dictionary<TelemetrySeverity, double>>();
        }

        internal double GetRate(TelemetryCategory category, TelemetrySeverity severity)
        {
            if (_rates.TryGetValue(category, out var bySeverity) &&
                bySeverity.TryGetValue(severity, out var rate))
            {
                return Math.Clamp(rate, 0.0, 1.0);
            }

            return 1.0; // default: keep 100 %
        }
    }

    /// <summary>
    /// Operational health snapshot returned by
    /// <see cref="EcosystemTelemetryService.GetHealth"/>.
    /// </summary>
    public sealed class TelemetryPipelineHealth
    {
        public string   SessionId            { get; init; } = string.Empty;
        public int      BufferedEvents       { get; init; }
        public long     ExportedEvents       { get; init; }
        public long     DroppedEvents        { get; init; }
        public string[] RegisteredExporters  { get; init; } = Array.Empty<string>();
        public DateTimeOffset CapturedAt     { get; init; }
    }
}
