using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces;

public interface IVoiceService
{
    Task<List<Voice>?> GetVoices(string provider);
}