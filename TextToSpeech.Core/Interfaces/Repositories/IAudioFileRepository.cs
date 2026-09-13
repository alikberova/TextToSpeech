using TextToSpeech.Core.Entities;

namespace TextToSpeech.Core.Interfaces.Repositories;

public interface IAudioFileRepository
{
    Task Add(AudioFile audioFile);
    Task Delete(Guid id);
    Task<List<AudioFile>> GetAll();
    Task<AudioFile?> GetById(Guid id);
    Task<AudioFile?> GetByIdAsNoTracking(Guid id);
    Task<AudioFile?> GetByHash(string hash);
    Task Update(AudioFile audioFile);
}