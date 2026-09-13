namespace TextToSpeech.Core.Interfaces;

public interface IMetaDataService
{
    Task<byte[]> AddMetaData(byte[] data, string ext, string title);
}