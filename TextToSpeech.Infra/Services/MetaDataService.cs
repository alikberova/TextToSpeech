using TextToSpeech.Core;
using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Infra.Services;

public sealed class MetaDataService : IMetaDataService
{
    public async Task<byte[]> AddMetaData(byte[] data, string ext, string title)
    {
        var tempPath = Path.Combine(Path.GetTempPath(),
            $"{Guid.NewGuid()}.{ext}");

        try
        {
            await File.WriteAllBytesAsync(tempPath, data);

            using var file = TagLib.File.Create(tempPath);

            file.Tag.Title = title;
            file.Tag.Comment = $"Website: {AppConstants.Domain}";
            file.Save();

            return await File.ReadAllBytesAsync(tempPath);
        }
		finally
		{
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
		}
    }
}