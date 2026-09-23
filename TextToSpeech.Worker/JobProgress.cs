namespace TextToSpeech.Worker;

internal sealed class JobProgress : IProgress<int>
{
    private int _value;

    public int Value => Volatile.Read(ref _value);

    public void Report(int value)
    {
        var progress = Math.Clamp(value, 0, 100);
        int current;

        do
        {
            current = Value;

            if (progress <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref _value, progress, current) != current);
    }
}
