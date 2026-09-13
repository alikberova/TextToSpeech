namespace TextToSpeech.Core.Interfaces.Ai;

public interface ITtsServiceFactory
{
    ITtsService Get(string key);
}