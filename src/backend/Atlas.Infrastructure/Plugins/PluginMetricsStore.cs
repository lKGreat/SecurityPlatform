using System.Collections.Concurrent;

namespace Atlas.Infrastructure.Plugins;

/// <summary>
/// 插件运行时指标（内存计数器，进程生命周期内累积）
/// </summary>
public sealed class PluginMetricsStore
{
    private readonly ConcurrentDictionary<string, PluginMetricsEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public void RecordCall(string pluginCode, bool success, long elapsedMs)
    {
        var entry = _entries.GetOrAdd(pluginCode, _ => new PluginMetricsEntry(pluginCode));
        entry.RecordCall(success, elapsedMs);
    }

    public PluginMetricsSnapshot? GetSnapshot(string pluginCode)
    {
        if (!_entries.TryGetValue(pluginCode, out var entry)) return null;
        return entry.GetSnapshot();
    }

    public IReadOnlyList<PluginMetricsSnapshot> GetAllSnapshots()
    {
        return _entries.Values.Select(e => e.GetSnapshot()).ToList();
    }
}

internal sealed class PluginMetricsEntry
{
    private readonly string _code;
    private long _totalCalls;
    private long _errorCalls;
    private long _totalElapsedMs;
    private bool _circuitOpen;
    private DateTimeOffset _circuitOpenedAt;
    private const int CircuitBreakerThreshold = 5;
    private const int CircuitBreakerWindowSeconds = 60;
    private static readonly TimeSpan CircuitResetTimeout = TimeSpan.FromMinutes(2);

    public PluginMetricsEntry(string code) => _code = code;

    public void RecordCall(bool success, long elapsedMs)
    {
        Interlocked.Increment(ref _totalCalls);
        Interlocked.Add(ref _totalElapsedMs, elapsedMs);
        if (!success)
        {
            Interlocked.Increment(ref _errorCalls);
            CheckCircuitBreaker();
        }
        else if (_circuitOpen && DateTimeOffset.UtcNow - _circuitOpenedAt > CircuitResetTimeout)
        {
            _circuitOpen = false;
        }
    }

    private void CheckCircuitBreaker()
    {
        if (_circuitOpen) return;
        var errorRate = _totalCalls == 0 ? 0 : (double)_errorCalls / _totalCalls;
        if (_errorCalls >= CircuitBreakerThreshold && errorRate > 0.5)
        {
            _circuitOpen = true;
            _circuitOpenedAt = DateTimeOffset.UtcNow;
        }
    }

    public PluginMetricsSnapshot GetSnapshot()
    {
        var total = Interlocked.Read(ref _totalCalls);
        var errors = Interlocked.Read(ref _errorCalls);
        var elapsed = Interlocked.Read(ref _totalElapsedMs);
        return new PluginMetricsSnapshot(
            _code,
            (int)total,
            (int)errors,
            total == 0 ? 0 : (double)errors / total,
            total == 0 ? 0 : elapsed / total,
            _circuitOpen,
            _circuitOpen ? _circuitOpenedAt : null);
    }
}

public sealed record PluginMetricsSnapshot(
    string PluginCode,
    int TotalCalls,
    int ErrorCalls,
    double ErrorRate,
    long AvgElapsedMs,
    bool CircuitOpen,
    DateTimeOffset? CircuitOpenedAt);
