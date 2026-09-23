using System.Collections.Concurrent;

namespace TextToSpeech.Infra.SignalR;

public sealed class SpeechJobConnections
{
    private readonly ConcurrentDictionary<string, string> _owners = new();
    private readonly ConcurrentDictionary<string, byte> _refresh = new();

    public void Add(string connectionId, string ownerId) => _owners[connectionId] = ownerId;

    public void Remove(string connectionId)
    {
        _owners.TryRemove(connectionId, out _);
        _refresh.TryRemove(connectionId, out _);
    }

    internal void Refresh(string ownerId)
    {
        foreach (var connection in _owners.Where(x => x.Value == ownerId))
        {
            _refresh[connection.Key] = 0;
        }
    }

    internal bool TakeRefresh(string connectionId) => _refresh.TryRemove(connectionId, out _);

    internal KeyValuePair<string, string>[] Snapshot() => [.. _owners];
}
