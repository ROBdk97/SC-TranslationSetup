﻿
using System.Text.Json.Serialization;

namespace SC_TranslationSetup
{
    //[JsonSerializable(typeof(RootObject))]
    public class RootObject
    {
        public RootObject()
        {
        }

        public Dictionary<string, Lang> languages { get; set; }
    }

    //[JsonSerializable(typeof(Lang))]
    public class Lang
    {
        public Lang()
        {
        }

        public string? translationSetup { get; set; }
        public string? exitPrompt { get; set; }
        public string? lookingForLogFile { get; set; }
        public string? directoryNotFound { get; set; }
        public string? enterPath { get; set; }
        public string? starCitizenPath { get; set; }
        public string? selectedVersion { get; set; }
        public string? gettingLanguages { get; set; }
        public string? selectedLanguage { get; set; }
        public string? downloadingFiles { get; set; }
        public string? done { get; set; }
        public string? restartToApply { get; set; }
        public string? errorOccurred { get; set; }
        public string? cfgUpdated { get; set; }
        public string? selectVersion { get; set; }
        public string? enterVersion { get; set; }
        public string? invalidSelection { get; set; }
        public string? selectLanguage { get; set; }
        public string? enterLanguage { get; set; }
        public string? confirmEnglishTranslation { get; set; }
        public string? uninstall { get; set; }
        public string? cleanupDone { get; set; }
        public string? fileDownloaded { get; set; }
        public string? errorMessage { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RootObject))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
