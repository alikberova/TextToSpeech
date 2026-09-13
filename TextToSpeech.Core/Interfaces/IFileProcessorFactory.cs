namespace TextToSpeech.Core.Interfaces;

public interface IFileProcessorFactory
{
    IFileProcessor? GetProcessor(string fileType);
}