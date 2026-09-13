namespace TextToSpeech.Infra.Stubs;

internal static class Delay
{
    public static Task RandomShort(CancellationToken token) =>
        Task.Delay(Random.Shared.Next(50, 150), token);

    public static Task RandomMedium(CancellationToken token) =>
        Task.Delay(Random.Shared.Next(250, 500), token);
}
