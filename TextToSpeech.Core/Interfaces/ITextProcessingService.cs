namespace TextToSpeech.Core.Interfaces;

public interface ITextProcessingService
{
    List<string> SplitTextIfGreaterThan(string text, int maxLength);
}