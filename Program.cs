using SC_TranslationSetup;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

Lang l = GetLang();
Console.WriteLine($"{l.translationSetup}\n----------------------------------------\n");
await Setup();
Console.WriteLine($"{l.exitPrompt}");
Console.ReadKey();
Environment.Exit(0);

/// <summary>
/// Main setup method
/// </summary>
async Task Setup()
{
    string scPath = "C:\\Program Files\\Roberts Space Industries\\StarCitizen\\";
    try
    {
        if (!File.Exists(scPath))
        {
            // Find latest Star Citizen path from RSI Launcher log file
            Console.WriteLine($"{l.lookingForLogFile}\n");
            string logFilePath = GetLogFilePath();
            if (File.Exists(logFilePath))
            {
                scPath = FindLatestStarCitizenPath(logFilePath);
            }
            // cleanup scPath to remove additional //
            scPath = scPath.Replace("\\\\", "\\");
            scPath = scPath.Substring(0, scPath.IndexOf("StarCitizen") + 11);
        PATHNOTFOUND:
            if (!Directory.Exists(scPath) || (Directory.Exists(scPath) && !scPath.Contains("StarCitizen")))
            {
                // Ask for path
                Console.WriteLine($"\n{l.directoryNotFound}\n");
                Console.Write($"\n{l.enterPath}");
                scPath = Console.ReadLine();
                scPath = scPath.Substring(0, scPath.IndexOf("StarCitizen") + 11);
                goto PATHNOTFOUND;
            }
        }


        Console.WriteLine($"{l.starCitizenPath}{scPath}\n");

        // Get available versions
        string selectedVersion = SelectStarCitizenVersion(scPath);
        if (string.IsNullOrEmpty(selectedVersion))
        {
            return;
        }
        Console.WriteLine($"{l.selectedVersion}{selectedVersion}\n");

        // Get available languages
        Console.WriteLine($"{l.gettingLanguages}\n");
        var languages = await GitHub.GetRepoData();
        string selectedLanguage = SelectLanguage(languages);
        if (string.IsNullOrEmpty(selectedLanguage))
        {
            return;
        }
        Console.WriteLine($"{l.enterLanguage}{selectedLanguage}\n");

        // Check if language is english and if so, clean up
        if (selectedLanguage == "english")
        {
            CleanUp(selectedVersion);
            return;
        }

        // Download files and edit user.cfg
        Console.WriteLine($"{l.downloadingFiles}\n");
        await ProcessLanguageSelection(selectedVersion, selectedLanguage);

        Console.WriteLine($"{l.done}\n");
        Console.WriteLine($"{l.restartToApply}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{l.errorOccurred}{ex.Message}");
    }
}

/// <summary>
/// Download the global.ini file for the selected language and edit the user.cfg
async Task<Task> ProcessLanguageSelection(string selectedVersion, string selectedLanguage)
{
    string filePath = Path.Combine(selectedVersion, "data", "Localization", selectedLanguage, "global.ini");
    string branch = "main";
    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
    await DownloadFileAsync(branch, selectedLanguage, filePath);
    EditUserConfig(selectedVersion, selectedLanguage);
    return Task.CompletedTask;
}

/// <summary>
/// Edit the user.cfg file to set the selected language
/// </summary>
void EditUserConfig(string selectedVersion, string selectedLanguage)
{
    // Read user.cfg
    string filePath = Path.Combine(selectedVersion, "user.cfg");
    var userCfgContent = File.ReadAllLines(filePath).ToList();

    // Remove lines and check if settings exist
    userCfgContent.RemoveAll(line =>
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

    // Add missing settings
    userCfgContent.Add($"g_language = {selectedLanguage}");
    userCfgContent.Add("g_languageAudio = english");

    // Write back to file
    File.WriteAllLines(filePath, userCfgContent);
    Console.WriteLine($"{l.cfgUpdated}\n");
}

/// <summary>
/// Get the path to the RSI Launcher log file
/// </summary>
string GetLogFilePath()
{
    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    return Path.Combine(appDataPath, "rsilauncher", "logs", "log.log");
}

/// <summary>
/// Select the Star Citizen version to use
/// </summary>
string SelectStarCitizenVersion(string latestPath)
{
    string[] subfolders = Directory.GetDirectories(latestPath);

    Console.WriteLine($"{l.selectVersion}");
    for (int i = 0; i < subfolders.Length; i++)
    {
        Console.WriteLine($" {i,00} - {Path.GetFileName(subfolders[i])}");
    }
    Console.Write($"\n{l.enterVersion}");

    if (int.TryParse(Console.ReadLine(), out int selectedVersion) && selectedVersion >= 0 && selectedVersion < subfolders.Length)
    {
        return subfolders[selectedVersion];
    }

    Console.WriteLine($"{l.invalidSelection}\n");
    return "";
}

/// <summary>
/// Select the language to use
/// </summary>
string SelectLanguage(string[] languages)
{
    Console.WriteLine($"{l.selectLanguage}");
    for (int i = 0; i < languages.Length; i++)
    {
        Console.WriteLine($"{i,00} - {languages[i]}");
    }

    Console.Write($"\n{l.enterLanguage}");

    if (int.TryParse(Console.ReadLine(), out int selectedLanguage) && selectedLanguage >= 0 && selectedLanguage < languages.Length)
    {
        return languages[selectedLanguage];
    }

    Console.WriteLine($"{l.invalidSelection}\n");
    return null;
}

/// <summary>
/// Clean up the the localization folder and user.cfg
/// </summary>
void CleanUp(string scPath)
{
    // ask if user wants to delete the english files
    Console.Write($"\n{l.confirmEnglishTranslation}: ");
    string response = Console.ReadLine();
    if (!response.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }
    // delete all files in data/Localization
    string fileName = Path.Combine(scPath, "data", "Localization");
    if(!Directory.Exists(fileName))
    {
        Directory.CreateDirectory(fileName);
    }
    string[] subfolders = Directory.GetDirectories(fileName);
    foreach (var subfolder in subfolders)
    {
        Directory.Delete(subfolder, true);
    }
    string userCfgPath = Path.Combine(scPath, "user.cfg");
    var userCfgContent = File.ReadAllLines(userCfgPath).ToList();
    // remove user.cfg entry g_language and g_languageAudio
    userCfgContent.RemoveAll(line =>
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
    Console.WriteLine($"\n{l.cleanupDone}\n");
    Console.WriteLine($"{l.restartToApply}\n");
}

/// <summary>
/// Find the latest Star Citizen path from the RSI Launcher log file
/// </summary>
string FindLatestStarCitizenPath(string logFilePath)
{
    string[] lines = File.ReadAllLines(logFilePath);
    DateTime latestTimestamp = DateTime.MinValue;
    string latestPath = null;

    foreach (var line in lines)
    {
        var timestampMatch = Regex.Match(line, "{ \"t\":\"([^\"]+)\"");
        var pathMatch = Regex.Match(line, "\"filePaths\":\\s*\\[\\s*\"([^\"]+)\"\\s*\\]|Launching Star Citizen LIVE from \\(([^)]+)\\)|Deleting (C:\\\\[^ ]+)");

        if (timestampMatch.Success && pathMatch.Success)
        {
            DateTime timestamp = DateTime.Parse(timestampMatch.Groups[1].Value);
            if (timestamp > latestTimestamp)
            {
                latestTimestamp = timestamp;
                latestPath = pathMatch.Groups[1].Success ? pathMatch.Groups[1].Value : pathMatch.Groups[2].Success ? pathMatch.Groups[2].Value : pathMatch.Groups[3].Value;
            }
        }
    }
    return latestPath;
}

/// <summary>
/// Download the global.ini file from GitHub for the given language
/// </summary>
async Task DownloadFileAsync(string branch, string language, string fileName)
{
    string url = $"https://raw.githubusercontent.com/Dymerz/StarCitizen-Localization/{branch}/data/Localization/{language}/global.ini";

    using (HttpClient client = new HttpClient())
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();

            await File.WriteAllTextAsync(fileName, content);
            Console.WriteLine($"{l.fileDownloaded}{fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{l.errorMessage}{ex.Message}");
        }
    }
}

/// <summary>
/// Get the language file from the assembly resources
/// </summary>
Lang GetLang()
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