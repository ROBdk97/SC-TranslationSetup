namespace SC_TranslationSetup.Models
{
    internal sealed record LanguageOption(
    string Id,
    string DisplayName,
    string TargetLanguage,
    string DownloadUrl,
    bool IsCleanup = false,
    DateTimeOffset? LastUpdated = null);
}
