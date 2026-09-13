using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Infra.Services.FileProcessing;

public sealed class FileProcessorFactory(IEnumerable<IFileProcessor> _processors) : IFileProcessorFactory
{
    public IFileProcessor? GetProcessor(string fileType)
    {
        return _processors.FirstOrDefault(p => p.CanProcess(fileType));
    }
}

