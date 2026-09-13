namespace TextToSpeech.Infra.Services.FileProcessing;

public static class AudioFileService
{
    public static byte[] ConcatenateRawAudioChunks(ReadOnlyMemory<byte>[] audioFiles, string format)
    {
        if (format is not "mp3" && format is not "pcm")
        {
            throw new NotSupportedException($"Concatenation not supported for {format}");
        }

        using var memoryStream = new MemoryStream();

        foreach (var file in audioFiles)
        {
            // Convert ReadOnlyMemory<byte> to byte[] and write to the MemoryStream
            byte[] buffer = file.ToArray();
            memoryStream.Write(buffer, 0, buffer.Length);
        }

        return memoryStream.ToArray();
    }

    public static byte[] GenerateSilentMp3(int durationSeconds)
    {
        int sampleRate = 44100;
        int channels = 2;
        int bitrate = 128; // in kbps

        using MemoryStream memoryStream = new();
        // Write ID3 header (optional but good practice)
        WriteId3v1Tag(memoryStream);

        // Calculate frame count
        int frameSize = (144 * bitrate * 1000) / sampleRate;
        int frameCount = (durationSeconds * sampleRate) / 1152;

        // Write silent MP3 frames
        byte[] frameHeader = CreateMp3FrameHeader(bitrate, sampleRate, channels);

        for (int i = 0; i < frameCount; i++)
        {
            memoryStream.Write(frameHeader, 0, frameHeader.Length);
            byte[] silence = new byte[frameSize - frameHeader.Length];
            memoryStream.Write(silence, 0, silence.Length);
        }

        return memoryStream.ToArray();
    }

    private static void WriteId3v1Tag(MemoryStream stream)
    {
        byte[] id3v1Tag = new byte[128];
        id3v1Tag[0] = (byte)'T';
        id3v1Tag[1] = (byte)'A';
        id3v1Tag[2] = (byte)'G';
        stream.Write(id3v1Tag, 0, id3v1Tag.Length);
    }

    private static byte[] CreateMp3FrameHeader(int bitrate, int sampleRate, int channels)
    {
        int bitrateIndex = GetBitrateIndex(bitrate);
        int sampleRateIndex = GetSampleRateIndex(sampleRate);

        byte[] header = new byte[4];
        header[0] = 0xFF; // Sync byte 1
        header[1] = 0xFB; // Sync byte 2, MPEG1, Layer III
        header[2] = (byte)((bitrateIndex << 4) | (sampleRateIndex << 2) | (channels == 2 ? 0 : 1));
        header[3] = 0x00; // No padding, no private, stereo, no copyright, no original, no emphasis
        return header;
    }

    private static int GetBitrateIndex(int bitrate)
    {
        int[] bitrates = { 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 };
        for (int i = 0; i < bitrates.Length; i++)
        {
            if (bitrates[i] == bitrate)
                return i + 1;
        }
        throw new ArgumentOutOfRangeException(nameof(bitrate));
    }

    private static int GetSampleRateIndex(int sampleRate)
    {
        int[] sampleRates = { 44100, 48000, 32000 };
        for (int i = 0; i < sampleRates.Length; i++)
        {
            if (sampleRates[i] == sampleRate)
                return i;
        }
        throw new ArgumentOutOfRangeException(nameof(sampleRate));
    }
}
