
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SC_TranslationSetup
{
    public class RootObject
    {
        public RootObject()
        {
        }

        public Dictionary<string, Lang> languages { get; set; }
    }

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

    internal class Localization
    {
        /// <summary>
        /// Get the language file from the assembly resources
        /// </summary>
        internal static Lang GetLang()
        {
            RootObject root = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("SC_TranslationSetup.Localisation.json"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    root = JsonSerializer.Deserialize(result, SourceGenerationContext.Default.RootObject);
                }
                string languageCode = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
                return root.languages[languageCode];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting language file: {ex.Message}");
                Console.ReadKey();
                return root.languages["en"];
            }
        }
    }
}
