namespace TextToSpeech.Core.Interfaces;

public interface IPathService
{
    string ResolveFilePathForStorage(Guid fileId, string fileExtension = "mp3");
    string GetFileStoragePath();
}