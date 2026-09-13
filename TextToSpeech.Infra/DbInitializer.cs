using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Entities;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra;

public sealed class DbInitializer(AppDbContext dbContext) : IDbInitializer
{
    public async Task Initialize()
    {
        await dbContext.Database.MigrateAsync();

        await Seed();
    }

    private async Task Seed()
    {
        foreach (var keyValue in Shared.TtsApis)
        {
            if (!dbContext.TtsApis.Any(s => s.Id == keyValue.Value))
            {
                var service = new TtsApi()
                {
                    Name = keyValue.Key,
                    Id = keyValue.Value,
                };

                await dbContext.TtsApis.AddAsync(service);
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
