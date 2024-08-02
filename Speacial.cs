using SC_TranslationSetup;

public static class Speacial
{
    /// <summary>
    /// Handle officially not supported languages by replacing the language with the default one (english)
    /// </summary>
    /// <param name="selectedLanguage"></param>
    /// <returns></returns>
    public static string HandleNotSupported(string selectedLanguage)
    {
        return selectedLanguage switch
        {
            "lithuanian_(lithuania)" => "english",
            _ => selectedLanguage
        };
    }
}
