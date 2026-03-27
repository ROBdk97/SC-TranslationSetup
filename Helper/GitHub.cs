using SC_TranslationSetup.Models;
using System.Text;
using System.Text.Json;

namespace SC_TranslationSetup.Helper
{
    internal static class GitHub
    {

        /// <summary>
        /// Get the list of languages from a GitHub repository
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="repo"></param>
        /// <param name="branch"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static async Task<LanguageOption[]> ListRepositoryContents(string owner, string repo, string branch, string path)
        {
            string url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}";
            List<LanguageOption> languages = [];

            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "request"); // GitHub API requires a user-agent
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string content = await response.Content.ReadAsStringAsync();

                    JsonDocument doc = JsonDocument.Parse(content);
                    JsonElement root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                        foreach (JsonElement item in root.EnumerateArray())
                        {
                            string? type = item.GetProperty("type").GetString();
                            string? name = item.GetProperty("name").GetString();

                            if (type == "dir" && !string.IsNullOrWhiteSpace(name))
                            {
                                string downloadUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/data/Localization/{name}/global.ini";
                                languages.Add(new LanguageOption(name, name, name, downloadUrl, name == "english"));
                            }
                        }
                }
                catch (Exception ex)
                {
                    Helper.ConsoleHelper.WriteWarningLine($"Error occurred: {ex.Message}");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }

            return [.. languages];
        }

        /// <summary>
        /// Download the global.ini file from GitHub for the given language
        /// </summary>
        internal static async Task DownloadFileAsync(string downloadUrl, string fileName)
        {
            using HttpClient client = new();
            try
            {
                HttpResponseMessage response = await client.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                Encoding utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

                await File.WriteAllTextAsync(fileName, content, utf8WithBom);
                Helper.ConsoleHelper.WriteMutedLine($"{Program.l.fileDownloaded}{fileName}");
            }
            catch (Exception ex)
            {
                Helper.ConsoleHelper.WriteWarningLine($"{Program.l.errorMessage}{ex.Message}");
            }
        }

        /// <summary>
        /// Get the list of languages from the GitHub repository
        /// </summary>
        /// <param name="selectedVersion"></param>
        /// <returns></returns>
        internal static async Task<LanguageOption[]> GetRepoData(string selectedVersion)
        {
            string branch = "main";
            if (selectedVersion.Contains("PTU"))
                branch = "ptu";

            List<LanguageOption> languages = [.. await ListRepositoryContents("Dymerz", "StarCitizen-Localization", branch, "data/Localization")];
            if (branch == "main")
            {
                // Add some custom languages that are not in the repository
                languages.Add(new LanguageOption(
                "english_starstrings",
                "english / StarStrings / MrKraken",
                "english",
                "https://raw.githubusercontent.com/MrKraken/StarStrings/master/Data/Localization/english/global.ini"));
                languages.Add(new LanguageOption(
                    "english_sccomplangpack",
                    "english / ScCompLangPack / ExoAE",
                    "english",
                    "https://raw.githubusercontent.com/ExoAE/ScCompLangPack/main/ScCompLangPack/data/Localization/english/global.ini"));
                languages.Add(new LanguageOption(
                    "german_scdeutsch",
                    "german / SC Deutsch / rjcncpt",
                    "german_(germany)",
                    "https://raw.githubusercontent.com/rjcncpt/StarCitizen-Deutsch-INI/main/live/global.ini"));
                languages.Add(new LanguageOption(
                    "swiss_german_scdeutsch",
                    "swiss german / SC Deutsch / rjcncpt",
                    "german_(germany)",
                    "https://raw.githubusercontent.com/rjcncpt/StarCitizen-Deutsch-INI/main/live-CH/global.ini"));
            }
            return [.. languages];
        }
    }
}
