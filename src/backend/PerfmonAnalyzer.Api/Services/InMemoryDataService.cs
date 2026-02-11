using System.Collections.Concurrent;
using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// メモリ上でセッションデータを管理するサービス実装
/// </summary>
public class InMemoryDataService : IDataService
{
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();

    /// <inheritdoc/>
    public string CreateSession(List<CounterInfo> counters)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new SessionInfo
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Counters = counters,
        };

        _sessions[sessionId] = session;
        return sessionId;
    }

    /// <inheritdoc/>
    public List<CounterInfo> GetCounters(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new KeyNotFoundException($"Session {sessionId} not found");
        }

        session.LastAccessedAt = DateTime.UtcNow;
        return session.Counters;
    }

    /// <inheritdoc/>
    public List<CounterInfo> GetCounters(string sessionId, DateTime startTime, DateTime endTime)
    {
        var counters = GetCounters(sessionId);

        return counters.Select(counter => new CounterInfo
        {
            MachineName = counter.MachineName,
            Category = counter.Category,
            InstanceName = counter.InstanceName,
            CounterName = counter.CounterName,
            DisplayName = counter.DisplayName,
            DataPoints = counter.DataPoints
                .Where(dp => dp.Timestamp >= startTime && dp.Timestamp <= endTime)
                .ToArray(),
        }).ToList();
    }

    /// <inheritdoc/>
    public bool SessionExists(string sessionId)
    {
        return _sessions.ContainsKey(sessionId);
    }

    /// <inheritdoc/>
    public void CleanupExpiredSessions(TimeSpan expirationTime)
    {
        var now = DateTime.UtcNow;

        foreach (var kvp in _sessions)
        {
            if (now - kvp.Value.LastAccessedAt > expirationTime)
            {
                _sessions.TryRemove(kvp.Key, out _);
            }
        }
    }
}
