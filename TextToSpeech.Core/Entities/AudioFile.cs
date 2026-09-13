using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Core.Entities;

public sealed class AudioFile
{
    private byte[] _data = [];

    public Guid Id { get; init; }
    /// <summary>
    /// ClaimTypes.NameIdentifier
    /// </summary>
    public string OwnerId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public byte[] Data
    {
        get => _data;
        private set => _data = value;
    }
    public DateTime CreatedAt { get; init; }
    public string Description { get; init; } = string.Empty;
    public Status Status { get; set; }
    public string Hash { get; init; } = string.Empty;
    /// <summary>
    /// Provider Voice Id
    /// </summary>
    public string Voice { get; init; } = string.Empty;
    public string? LanguageCode { get; init; }
    public double Speed { get; init; }
    public AudioType Type { get; init; }

    public Guid? TtsApiId { get; init; }
    public TtsApi? TtsApi { get; init; }

    /// <summary>
    /// Sets audio bytes exactly once. Subsequent calls throw.
    /// Bytes must be fully finalized (metadata applied) before calling.
    /// </summary>
    public void SetDataOnce(byte[] bytes)
    {
        if (_data.Length != 0)
        {
            throw new InvalidOperationException("Audio data is already set.");
        }

        _data = bytes;
    }

    public override bool Equals(object? obj)
    {
        return obj is AudioFile other
            && Hash == other.Hash
            && Data.SequenceEqual(other.Data);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Hash);
        hashCode.AddBytes(Data);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(AudioFile left, AudioFile right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(AudioFile left, AudioFile right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Id}: {Description}";
    }
}
