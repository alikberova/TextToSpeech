using TextToSpeech.Core.Models;

namespace TextToSpeech.UnitTests;

internal static class TestData
{
    private const string NewLine = "\r\n";

    public const string Text1500chars =
        "- Oh, it won't be easy. In such weather, customers will have to be shooed away with a stick." + NewLine +
        "Pete and the brunette – Trish – get out, cross the alley and forty feet later are on Main Street. " +
        "The pharmacy is the second building on the left. The fog intensified and almost turned into real rain! " +
        "The woman puts a new scarf over her hair and looks at Pete, whose head is uncovered." + NewLine +
        "- You will get wet, - she says." + NewLine +
        "- I am from the north, - he answers. - And we guys are strong there." + NewLine +
        "- Do you think we will find them? - she asks." + NewLine +
        "He is already gently pushing her to the door, lightly touching her waist. He likes the smell of her " +
        "perfume and her hair even more. If it is so luxurious when it is raining outside, what is it like in " +
        "the sun?" + NewLine +
        "- My meeting..." + NewLine +
        "- You still have forty minutes to spare, - he interrupts. - It's not summer now, the tourists have left, " +
        "so you can easily get to Freiburg in twenty minutes. We'll spend ten minutes looking for the keys, and " +
        "if we don't find them, I'll drive you myself." + NewLine +
        "She looks at him doubtfully. And he glances past her into one of the neighboring offices and shouts:" +
        NewLine +
        "- Dick! Bye, Dickie M.!" + NewLine +
        "Dick MacDonald breaks away from stacks of bills." + NewLine +
        "- Tell this lady that when you have to take her to Freiburg, you can trust me." + NewLine +
        "\"Oh, you can trust him, ma'am,\" says Dick. - He is not a sex maniac or a fast driver. Will only try " +
        "to sell you a new car." + NewLine +
        "- I'm a tough nut, - she smiles slightly. - But, I think, I will go with you." + NewLine +
        "- Dick, watch my phone, okay? - asks Pete.";
    public const string CheckThatSentenceIsNotSplitByQuestionMark_Text1500chars = "? - she asks";

    public static TtsRequestOptions TtsRequestOptions =>
        new()
        {
            Voice = new Voice
            {
                Name = "Any",
                ProviderVoiceId = "any"
            },
            Speed = 1,
            ResponseFormat = SpeechResponseFormat.Mp3
        };
}
