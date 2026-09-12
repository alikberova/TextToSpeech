namespace TextToSpeech.SeleniumTests.Constants;

internal static class UiConstants
{
    internal static class DataTestId
    {
        public const string TtsForm = "ttsForm";
        public const string FormErrorBanner = "formErrorBanner";
        public const string FileInput = "fileInput";
        public const string FileDropzone = "fileDropzone";
        public const string SubmitBtn = "submitBtn";
        public const string DownloadBtn = "downloadBtn";
        public const string ClearBtn = "clearBtn";
        public const string SampleTextArea = "sampleTextArea";
        public const string SamplePlayButton = "samplePlayButton";
    }

    internal static class NameAttributes
    {
        public const string Provider = "provider";
        public const string Model = "model";
        public const string Language = "language";
        public const string Voice = "voice";
    }

    internal static class Selectors
    {
        public const string FileInfoName = ".file-info .name";
        public const string ProgressPanel = ".progress-panel";
        public const string ProgressCancelButton = ".progress-panel .cancel-btn";
        public const string StatusIcon = ".progress-panel .status-icon";
    }

    internal static class StatusIcons
    {
        public const string Canceled = "cancel";
    }

    internal static class Ids
    {
        public const string FileInput = "fileInputEl";
        public const string SpeedInput = "speedInputEl";
    }
}
