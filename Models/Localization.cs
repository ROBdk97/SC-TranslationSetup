using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace SC_TranslationSetup.Models
{

    internal class Localization
    {
        /// <summary>
        /// Get the language file from the assembly resources
        /// </summary>
        internal static Lang GetLang()
        {
            RootObject? root = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using Stream? stream = assembly.GetManifestResourceStream("SC_TranslationSetup.Localisation.json") ?? throw new InvalidOperationException("Missing localization resource.");
                using StreamReader reader = new(stream);
                string result = reader.ReadToEnd();
                root = JsonSerializer.Deserialize(result, SourceGenerationContext.Default.RootObject);
                string languageCode = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
                if (root?.languages is not null && root.languages.TryGetValue(languageCode, out var language))
                    return language;

                if (root?.languages is not null && root.languages.TryGetValue("en", out var fallback))
                    return fallback;

                return new Lang();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting language file: {ex.Message}");
                Console.ReadKey();
                return root?.languages?.GetValueOrDefault("en") ?? new Lang();
            }
        }
    }
}
