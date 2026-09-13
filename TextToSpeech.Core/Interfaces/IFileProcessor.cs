namespace TextToSpeech.Core.Interfaces;

public interface IFileProcessor
{
    bool CanProcess(string fileType);
    Task<string> ExtractTextAsync(byte[] fileBytes);
}