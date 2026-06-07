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
                                string languagePath = $"{path}/{name}";
                                DateTimeOffset? lastUpdated = null;
                                if (name != "english")
                                    lastUpdated = await GetLastCommitDateAsync(owner, repo, branch, languagePath);
                                languages.Add(new LanguageOption(name, name, name, downloadUrl, name == "english", lastUpdated));
                            }
                        }
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteWarningLine($"Error occurred: {ex.Message}");
                    languages.Add(new LanguageOption("english", "english", "english", $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/data/Localization/english/global.ini", true, null));
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
                ConsoleHelper.WriteMutedLine($"{Program.l.fileDownloaded}{fileName}");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteWarningLine($"{Program.l.errorMessage}{ex.Message}");
            }
        }

        /// <summary>
        /// Get the last commit date for a specific path in a GitHub repository
        /// </summary>
        /// <param name="owner">Repository owner</param>
        /// <param name="repo">Repository name</param>
        /// <param name="branch">Branch name</param>
        /// <param name="path">Path to the file or directory</param>
        /// <returns>DateTimeOffset of the last commit, or null if not found</returns>
        private static async Task<DateTimeOffset?> GetLastCommitDateAsync(string owner, string repo, string branch, string path)
        {
            string url = $"https://api.github.com/repos/{owner}/{repo}/commits?path={path}&per_page=1&sha={branch}";

            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "request");

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();

                JsonDocument doc = JsonDocument.Parse(content);
                JsonElement root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    JsonElement firstCommit = root[0];
                    if (firstCommit.TryGetProperty("commit", out JsonElement commitElement))
                    {
                        if (commitElement.TryGetProperty("committer", out JsonElement committerElement))
                        {
                            if (committerElement.TryGetProperty("date", out JsonElement dateElement))
                            {
                                string? dateString = dateElement.GetString();
                                if (!string.IsNullOrWhiteSpace(dateString) && DateTimeOffset.TryParse(dateString, out DateTimeOffset commitDate))
                                {
                                    return commitDate;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            return null;
        }

        /// <summary>
        /// Helper method to create a custom LanguageOption with repository information
        /// </summary>
        private static async Task<LanguageOption> CreateCustomLanguageOption(
            string id,
            string displayName,
            string targetLanguage,
            string owner,
            string repo,
            string branch,
            string filePath,
            bool isCleanup = false)
        {
            string downloadUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{filePath}";
            DateTimeOffset? lastUpdated = await GetLastCommitDateAsync(owner, repo, branch, filePath);
            return new LanguageOption(id, displayName, targetLanguage, downloadUrl, isCleanup, lastUpdated);
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
                languages.Add(await CreateCustomLanguageOption(
                    "english_starstrings",
                    "english / StarStrings / MrKraken",
                    "english",
                    "MrKraken",
                    "StarStrings",
                    "master",
                    "Data/Localization/english/global.ini"));

                languages.Add(await CreateCustomLanguageOption(
                    "english_sccomplangpack",
                    "english / ScCompLangPack / ExoAE",
                    "english",
                    "ExoAE",
                    "ScCompLangPack",
                    "main",
                    "ScCompLangPack/data/Localization/english/global.ini"));

                languages.Add(await CreateCustomLanguageOption(
                    "german_scdeutsch",
                    "german / SC Deutsch / rjcncpt",
                    "german_(germany)",
                    "rjcncpt",
                    "StarCitizen-Deutsch-INI",
                    "main",
                    "live/global.ini"));

                languages.Add(await CreateCustomLanguageOption(
                    "swiss_german_scdeutsch",
                    "swiss german / SC Deutsch / rjcncpt",
                    "german_(germany)",
                    "rjcncpt",
                    "StarCitizen-Deutsch-INI",
                    "main",
                    "live-CH/global.ini"));
            }
            return [.. languages];
        }
    }
}
