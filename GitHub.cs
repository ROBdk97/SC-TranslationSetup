using System.Text.Json;

namespace SC_TranslationSetup
{
    internal static class GitHub
    {
        /// <summary>
        /// Download the global.ini file from GitHub for the given language
        /// </summary>
        internal static async Task DownloadFileAsync(string branch, string language, string fileName)
        {
            string url = $"https://raw.githubusercontent.com/Dymerz/StarCitizen-Localization/{branch}/data/Localization/{language}/global.ini";

            using HttpClient client = new();
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();

                await File.WriteAllTextAsync(fileName, content);
                Console.WriteLine($"{Program.l.fileDownloaded}{fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Program.l.errorMessage}{ex.Message}");
            }
        }

        /// <summary>
        /// Get the list of languages from the GitHub repository
        /// </summary>
        /// <param name="selectedVersion"></param>
        /// <returns></returns>
        internal static async Task<string[]> GetRepoData(string selectedVersion)
        {
            string branch = "main";
            if (selectedVersion.Contains("PTU"))
                branch = "ptu";
            string path = "data/Localization";
            string repoOwner = "Dymerz";
            string repoName = "StarCitizen-Localization";

            return await ListRepositoryContents(repoOwner, repoName, branch, path);
        }

        /// <summary>
        /// Get the list of languages from a GitHub repository
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="repo"></param>
        /// <param name="branch"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static async Task<string[]> ListRepositoryContents(string owner, string repo, string branch, string path)
        {
            string url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}";
            List<string> languages = [];

            using(HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "request"); // GitHub API requires a user-agent
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string content = await response.Content.ReadAsStringAsync();

                    JsonDocument doc = JsonDocument.Parse(content);
                    JsonElement root = doc.RootElement;

                    if(root.ValueKind == JsonValueKind.Array)
                    {
                        foreach(JsonElement item in root.EnumerateArray())
                        {
                            string type = item.GetProperty("type").GetString();
                            string name = item.GetProperty("name").GetString();

                            if (type == "dir")
                                languages.Add(name);
                        }
                    }
                } catch(Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }
            return [.. languages];
        }
    }
}
