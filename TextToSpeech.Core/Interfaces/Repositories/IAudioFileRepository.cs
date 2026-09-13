using TextToSpeech.Core.Entities;

namespace TextToSpeech.Core.Interfaces.Repositories;

public interface IAudioFileRepository
{
    Task Add(AudioFile audioFile, byte[] content);
    Task Delete(Guid id);
    Task<List<AudioFile>> GetAll();
    Task<AudioFile?> GetById(Guid id);
    Task<AudioFile?> GetByIdAsNoTracking(Guid id);
    Task<AudioFile?> GetByHash(string hash);
    Task<Guid?> GetCompletedByHash(string hash, string ownerId, Guid ttsApiId);
    Task Update(AudioFile audioFile, byte[] content);
}
