using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Entities;
using TextToSpeech.Core.Interfaces.Repositories;

namespace TextToSpeech.Infra.Repositories;

public sealed class AudioFileRepository(AppDbContext context) : IAudioFileRepository
{
    private readonly AppDbContext _context = context;

    public async Task Add(AudioFile audioFile)
    {
        if (string.IsNullOrWhiteSpace(audioFile.Hash))
        {
            throw new Exception("Unable to save audio file without hash");
        }

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

    public async Task Update(AudioFile audioFile)
    {
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
