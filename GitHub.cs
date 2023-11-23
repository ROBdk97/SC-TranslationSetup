using System.Text.Json;

namespace SC_TranslationSetup
{
    internal static class GitHub
    {
        internal static async Task<string[]> GetRepoData()
        {
            string branch = "main";
            string path = "data/Localization";
            string repoOwner = "Dymerz";
            string repoName = "StarCitizen-Localization";

            return await ListRepositoryContents(repoOwner, repoName, branch, path);
        }

        static async Task<string[]> ListRepositoryContents(string owner, string repo, string branch, string path)
        {
            string url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}";
            List<string> languages = [];

            using (HttpClient client = new HttpClient())
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
                    {
                        foreach (JsonElement item in root.EnumerateArray())
                        {
                            string type = item.GetProperty("type").GetString();
                            string name = item.GetProperty("name").GetString();

                            if (type == "dir")
                            {
                                languages.Add(name);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                }
            }
            return [.. languages];
        }
    }
}
