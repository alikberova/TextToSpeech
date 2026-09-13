using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Entities;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Repositories;

public sealed class AudioFileRepository(AppDbContext context, IArtifactStorage storage) : IAudioFileRepository
{
    private readonly AppDbContext _context = context;

    public async Task Add(AudioFile audioFile, byte[] content)
    {
        if (string.IsNullOrWhiteSpace(audioFile.Hash))
        {
            throw new Exception("Unable to save audio file without hash");
        }

        audioFile.ContentId = await storage.WriteAsync(content, CancellationToken.None);

        _context.AudioFiles.Add(audioFile);
        await _context.SaveChangesAsync();
    }

    public async Task<AudioFile?> GetById(Guid id)
    {
        return await _context.AudioFiles.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<AudioFile?> GetByIdAsNoTracking(Guid id)
    {
        return await _context.AudioFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<AudioFile?> GetByHash(string hash)
    {
        return await _context.AudioFiles.FirstOrDefaultAsync(f => f.Hash == hash);
    }

    public async Task<List<AudioFile>> GetAll()
    {
        return await _context.AudioFiles.ToListAsync();
    }

    public async Task<Guid?> GetCompletedByHash(string hash, string ownerId, Guid ttsApiId)
    {
        return await _context.AudioFiles
            .Where(f => f.Hash == hash && f.OwnerId == ownerId &&
                f.TtsApiId == ttsApiId && f.Status == Status.Completed)
            .Select(f => (Guid?)f.Id)
            .FirstOrDefaultAsync();
    }

    public async Task Update(AudioFile audioFile, byte[] content)
    {
        audioFile.ContentId = await storage.WriteAsync(content, CancellationToken.None);

        _context.AudioFiles.Update(audioFile);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var audioFile = await _context.AudioFiles.FindAsync(id);
        if (audioFile is not null)
        {
            _context.AudioFiles.Remove(audioFile);
            await _context.SaveChangesAsync();
        }
    }
}
