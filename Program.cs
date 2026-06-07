using SC_TranslationSetup.Helper;
using SC_TranslationSetup.Models;

internal partial class Program
{
    internal static Lang l = Localization.GetLang();

    private static async Task Main()
    {
        Console.WriteLine($"{l.translationSetup}\n----------------------------------------");
        await Setup();
        Console.WriteLine($"{l.exitPrompt}");
        Console.ReadKey();
        Environment.Exit(0);
    }

    /// <summary>
    /// Main setup method
    /// </summary>
    static async Task Setup()
    {
        string scPath = "C:\\Program Files\\Roberts Space Industries\\StarCitizen";
        try
        {
            var (LatestPath, VersionLookup) = await SCLauncher.GetLogDataAsync();
            Dictionary<string, string> versionLookup = VersionLookup;
            if (!Directory.Exists(scPath))
            {
                // Find latest Star Citizen path from RSI Launcher log file
                ConsoleHelper.WriteMutedLine($"{l.lookingForLogFile}");
                if (!string.IsNullOrWhiteSpace(LatestPath))
                    scPath = LatestPath;
                if (!string.IsNullOrWhiteSpace(scPath))
                {
                    // cleanup scPath to remove additional //
                    scPath = scPath.Replace("\\\\", "\\");
                    scPath = ExtractStarCitizenPath(scPath);

                }

                // check if path exists and if not, ask for path
            PATHNOTFOUND:
                if (!Directory.Exists(scPath) || Directory.Exists(scPath) && !scPath.Contains("StarCitizen"))
                {
                    // Ask for path
                    ConsoleHelper.WriteWarningLine($"\n{l.directoryNotFound}\n");
                    Console.Write($"\n{l.enterPath}");
                    scPath = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(scPath))
                        goto PATHNOTFOUND;
                    if (scPath.Contains("StarCitizen"))
                        scPath = scPath[..(scPath.IndexOf("StarCitizen") + 11)];
                    goto PATHNOTFOUND;
                }
            }

            ConsoleHelper.WriteMutedLine($"{l.starCitizenPath}{scPath}");

            // Get available versions
            string selectedVersion = SelectStarCitizenVersion(scPath, versionLookup);
            if (string.IsNullOrEmpty(selectedVersion))
                return;
            // Get available languages
            ConsoleHelper.WriteMutedLine($"{l.gettingLanguages}");
            var languages = await GitHub.GetRepoData(selectedVersion);
            LanguageOption? selectedLanguage = SelectLanguage(languages);
            if (selectedLanguage is null)
                return;

            // Check if language is english and if so, clean up
            if (selectedLanguage.IsCleanup)
            {
                CleanUp(selectedVersion);
                return;
            }
            // Download files and edit user.cfg
            ConsoleHelper.WriteMutedLine($"{l.downloadingFiles}");
            await ProcessLanguageSelection(selectedVersion, selectedLanguage);
            ConsoleHelper.WriteSuccessLine($"\n{l.done}");
            ConsoleHelper.WriteSuccessLine($"{l.restartToApply}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{l.errorOccurred}{ex.Message}");
        }
    }

    /// <summary>
    /// Download the global.ini file for the selected language and edit the user.cfg
    /// </summary>
    static async Task<Task> ProcessLanguageSelection(string selectedVersion, LanguageOption selectedLanguage)
    {
        string fileLang = Special.HandleNotSupported(selectedLanguage.TargetLanguage);
        string filePath = Path.Combine(selectedVersion, "data", "Localization", fileLang, "global.ini");

        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);
        await GitHub.DownloadFileAsync(selectedLanguage.DownloadUrl, filePath);
        EditUserConfig(selectedVersion, fileLang);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Edit the user.cfg file to set the selected language
    /// </summary>
    static void EditUserConfig(string selectedVersion, string selectedLanguage)
    {
        List<string> userCfgContent = [];
        // Read user.cfg
        string filePath = Path.Combine(selectedVersion, "user.cfg");
        if (File.Exists(filePath))
        {
            userCfgContent = [.. File.ReadAllLines(filePath)];
        }
        // Remove lines and check if settings exist
        userCfgContent.RemoveAll(
            line =>
            {
                if (line.Contains("g_language"))
                    return true; // Remove the line
                if (line.Contains("g_languageAudio"))
                    return true; // Remove the line
                return false; // Keep the line
            });
        // Add missing settings
        userCfgContent.Add($"g_language = {selectedLanguage}");
        userCfgContent.Add("g_languageAudio = english");

        // Write back to file
        File.WriteAllLines(filePath, userCfgContent);
        ConsoleHelper.WriteMutedLine($"{l.cfgUpdated}");
    }

    /// <summary>
    /// Select the Star Citizen version to use
    /// </summary>
    static string SelectStarCitizenVersion(string latestPath, Dictionary<string, string> versionLookup)
    {
        string[] subfolders = [.. Directory.GetDirectories(latestPath).Where(folder => File.Exists(Path.Combine(folder, "Data.p4k")))];
        var displayNames = subfolders
            .Select(folder =>
            {
                string versionName = Path.GetFileName(folder);
                if (versionLookup.TryGetValue(versionName, out var gameVersion) && !string.IsNullOrWhiteSpace(gameVersion))
                    return $"{versionName} ({gameVersion})";
                return versionName;
            })
            .ToArray();

        int selectedIndex = ConsoleHelper.SelectFromList(l.selectVersion, displayNames, l.selectedVersion);
        if (selectedIndex < 0)
            Environment.Exit(0);

        return subfolders[selectedIndex];
    }

    /// <summary>
    /// Select the language to use
    /// </summary>
    static LanguageOption? SelectLanguage(LanguageOption[] languages)
    {
        var displayNames = languages
            .Select(language => language.IsCleanup ? $"{language.DisplayName} / {l.uninstall}" : language.DisplayName)
            .ToArray();

        int selectedIndex = ConsoleHelper.SelectFromList(l.selectLanguage, displayNames, l.enterLanguage);
        if (selectedIndex < 0)
            Environment.Exit(0);

        return languages[selectedIndex];
    }

    /// <summary>
    /// Clean up the the localization folder and user.cfg
    /// </summary>
    static void CleanUp(string scPath)
    {
        // ask if user wants to delete the english files
        string[] options = ["Yes", "No"];
        int selectedIndex = ConsoleHelper.SelectFromList($"{l.confirmEnglishTranslation}", options);
        if (selectedIndex != 0)
            return;

        // delete all files in data/Localization
        string fileName = Path.Combine(scPath, "data", "Localization");
        if (!Directory.Exists(fileName))
            Directory.CreateDirectory(fileName);

        string[] subfolders = Directory.GetDirectories(fileName);
        foreach (var subfolder in subfolders)
            Directory.Delete(subfolder, true);

        string userCfgPath = Path.Combine(scPath, "user.cfg");
        var userCfgContent = File.ReadAllLines(userCfgPath).ToList();
        // remove user.cfg entry g_language and g_languageAudio
        userCfgContent.RemoveAll(
            line =>
            {
                if (line.Contains("g_language"))
                {
                    return true; // Remove the line
                }
                if (line.Contains("g_languageAudio"))
                {
                    return true; // Remove the line
                }
                return false; // Keep the line
            });
        File.WriteAllLines(userCfgPath, userCfgContent);
        ConsoleHelper.WriteSuccessLine($"{l.cleanupDone}");
        ConsoleHelper.WriteSuccessLine($"{l.restartToApply}");
    }

    /// <summary>
    /// Extract the Star Citizen path from the given path by looking for the "StarCitizen" 
    /// folder and verifying it contains a Data.p4k file in one of its subdirectories
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static string ExtractStarCitizenPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var parts = path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        var current = Path.IsPathRooted(path) ? Path.GetPathRoot(path)! : "";

        foreach (var part in parts)
        {
            current = string.IsNullOrEmpty(current) ? part : Path.Combine(current, part);

            if (part == "StarCitizen" &&
                Directory.Exists(current) &&
                Directory.EnumerateDirectories(current).Any(d => File.Exists(Path.Combine(d, "Data.p4k"))))
                return current;
        }

        return path;
    }
}