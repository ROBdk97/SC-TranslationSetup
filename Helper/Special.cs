namespace SC_TranslationSetup.Helper
{
    public static class Special
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
                "lithuanian_(lithuania)" => "german_(germany)",
                "turkish_(turkey)" => "german_(germany)",
                _ => selectedLanguage
            };
        }
    }
}