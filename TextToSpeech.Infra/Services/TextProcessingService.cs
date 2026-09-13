using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Infra.Services;

public sealed class TextProcessingService : ITextProcessingService
{
    public List<string> SplitTextIfGreaterThan(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (maxLength < 1)
        {
            throw new ArgumentException($"{nameof(maxLength)} should not be negative or zero");
        }

        var chunks = new List<string>();

        if (text.Length <= maxLength)
        {
            chunks.Add(text);
            return chunks;
        }

        int start = 0;

        while (start < text.Length)
        {
            int end = start + maxLength > text.Length ?
                text.Length :
                start + maxLength;

            int lastSentenceEnd = FindLastSentenceEnd(text, start, end);

            if (lastSentenceEnd == -1)
            {
                // If no sentence end found within the limit, take the maximum length available.
                lastSentenceEnd = end;
            }

            var chunk = text.Substring(start, lastSentenceEnd - start).Trim();

            if (chunk != string.Empty)
            {
                chunks.Add(chunk);
            }

            start = lastSentenceEnd;
        }

        return chunks;
    }

    private static int FindLastSentenceEnd(string text, int start, int end)
    {
        // Search for sentence-ending punctuation within the specified range
        for (int i = end - 1; i >= start; i--)
        {
            if (IsSentenceEnd(text, i))
            {
                return i + 1;
            }
        }

        return -1; // No sentence end found in the range
    }

    private static bool IsSentenceEnd(string text, int i)
    {
        if (text[i] is '.' or '!')
        {
            return i + 1 == text.Length || char.IsWhiteSpace(text[i + 1]);
        }

        if (text[i] is '?')
        {
            // Check if this is a special case where the question mark is not the end of a sentence
            if (i + 3 < text.Length && text.Substring(i, 4) == "? - " && char.IsLower(text[i + 4]))
            {
                // Found a special case, this is not a sentence end
                return false;
            }

            return i + 1 == text.Length || char.IsWhiteSpace(text[i + 1]);
        }

        return false;
    }
}
