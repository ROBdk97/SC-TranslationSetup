using System.Text.RegularExpressions;

namespace SC_TranslationSetup.Helper
{

    /// <summary>
    /// Provides helper methods for parsing and extracting information from RSI Launcher log files, including resolving
    /// installation paths and retrieving version data.
    /// </summary>
    /// <remarks>This class is intended for internal use to support operations related to Star Citizen launcher log
    /// analysis. All members are static and thread-safe. The class is not intended to be instantiated.</remarks>
    internal static partial class SCLaucher
    {
        private static readonly Regex DeltaUpdate = DeltaUpdateRegex();

        private static readonly Regex FilePaths = FilePathsRegex();
        private static readonly Regex Installer = InstallerRegex();
        private static readonly Regex Launcher = LauncherRegex();
        [GeneratedRegex("SC (LIVE|PTU|TECH-PREVIEW) ([^\\s\\)]+).*?\\bin\\s+(C:\\\\.+?)(?=\\s*\\\"|\\s*\\)|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex DeltaUpdateRegex();
        [GeneratedRegex("\\\"filePaths\\\":\\s*\\[\\s*\\\"([^\\\"]+)\\\"\\s*\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex FilePathsRegex();
        [GeneratedRegex("Star Citizen (LIVE|PTU|TECH-PREVIEW) ([^\\s]+).*? at (C:\\\\.+?)(?=\\s*\\(|\\s*\\\"|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex InstallerRegex();
        [GeneratedRegex("Launching Star Citizen (PTU|LIVE|TECH-PREVIEW) from \\(([^)]+)\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex LauncherRegex();

        /// <summary>
        /// Returns the specified path if it exists as a directory, or the directory containing the file if the path exists
        /// as a file.
        /// </summary>
        /// <remarks>If the specified path is a file and the file exists, the method returns the directory
        /// containing the file if that directory exists. If neither the path nor its containing directory exists, the
        /// method returns null.</remarks>
        /// <param name="path">The file or directory path to check. Can be either a file or directory path. Cannot be null or empty.</param>
        /// <returns>The existing directory path if found; otherwise, null.</returns>
        static string? GetExistingPath(string path)
        {
            if (Directory.Exists(path))
                return path;

            if (File.Exists(path))
            {
                var directoryPath = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
                    return directoryPath;
            }

            return null;
        }

        /// <summary>
        /// Resolves the full entry path by appending the specified channel name to the given path, unless the path already
        /// ends with the channel name.
        /// </summary>
        /// <remarks>If the file or directory name at the end of the path matches the channel name
        /// (case-insensitive), the original path is returned. Otherwise, the channel name is appended to the
        /// path.</remarks>
        /// <param name="path">The base file or directory path to resolve. Cannot be null or whitespace.</param>
        /// <param name="channel">The channel name to append to the path. If null or whitespace, the original path is returned.</param>
        /// <returns>A string representing the resolved entry path. Returns an empty string if the path is null or whitespace.</returns>
        private static string ResolveEntryPath(string path, string channel)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(channel))
                return path;

            string trimmedChannel = channel.Trim();
            string fileName = Path.GetFileName(path);
            if (string.Equals(fileName, trimmedChannel, StringComparison.OrdinalIgnoreCase))
                return path;

            return Path.Combine(path, trimmedChannel);
        }

        /// <summary>
        /// Attempts to parse a log line and extract its components into a LogEntry instance.
        /// </summary>
        /// <remarks>This method supports multiple log line formats and extracts relevant information based on the
        /// detected pattern. If the input does not match any known format, parsing fails and the out parameter is set to a
        /// default value.</remarks>
        /// <param name="line">The log line to parse. Cannot be null.</param>
        /// <param name="entry">When this method returns, contains the parsed LogEntry if parsing succeeds; otherwise, contains a default
        /// LogEntry with empty fields.</param>
        /// <returns>true if the log line was successfully parsed and a LogEntry was created; otherwise, false.</returns>
        private static bool TryParseLogLine(string line, out LogEntry entry)
        {
            entry = new LogEntry(string.Empty, string.Empty, string.Empty);

            var filePathsMatch = FilePaths.Match(line);
            if (filePathsMatch.Success)
            {
                entry = new LogEntry(NormalizePath(filePathsMatch.Groups[1].Value), string.Empty, string.Empty);
                return true;
            }

            var deltaUpdateMatch = DeltaUpdate.Match(line);
            if (deltaUpdateMatch.Success)
            {
                entry = new LogEntry(
                    NormalizePath(deltaUpdateMatch.Groups[3].Value),
                    deltaUpdateMatch.Groups[1].Value,
                    deltaUpdateMatch.Groups[2].Value);
                return true;
            }

            var launcherMatch = Launcher.Match(line);
            if (launcherMatch.Success)
            {
                entry = new LogEntry(NormalizePath(launcherMatch.Groups[2].Value), launcherMatch.Groups[1].Value, string.Empty);
                return true;
            }

            var installerMatch = Installer.Match(line);
            if (installerMatch.Success)
            {
                entry = new LogEntry(
                    NormalizePath(installerMatch.Groups[3].Value),
                    installerMatch.Groups[1].Value,
                    installerMatch.Groups[2].Value);
                return true;
            }

            return false;

            static string NormalizePath(string path) => path.Replace("\\\\", "\\");
        }

        /// <summary>
        /// Get the path to the RSI Launcher log file
        /// </summary>
        static string GetLogFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "rsilauncher", "logs", "log.log");
        }

        /// <summary>
        /// Retrieves the most recent valid log file path and a lookup of version information for up to three entries from
        /// the log file.
        /// </summary>
        /// <remarks>The method reads the log file in reverse order to prioritize the most recent entries. Only up
        /// to three version entries are included in the returned dictionary. If the log file does not exist or contains no
        /// valid entries, both return values are empty.</remarks>
        /// <returns>A tuple containing the latest valid log file path and a dictionary mapping entry names to their corresponding
        /// version strings. If no valid entries are found, the path is an empty string and the dictionary is empty.</returns>
        internal static (string LatestPath, Dictionary<string, string> VersionLookup) GetLogData()
        {
            var logFilePath = GetLogFilePath();
            string latestPath = string.Empty;
            var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(logFilePath))
                return (latestPath, versions);

            string[] lines = File.ReadAllLines(logFilePath);
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (!TryParseLogLine(lines[i], out var entry))
                    continue;

                string resolvedPath = ResolveEntryPath(entry.Path, entry.Channel);
                if (string.IsNullOrWhiteSpace(resolvedPath))
                    continue;

                var existingPath = GetExistingPath(resolvedPath);
                if (string.IsNullOrWhiteSpace(latestPath) && !string.IsNullOrWhiteSpace(existingPath))
                    latestPath = existingPath;

                string versionKey = Path.GetFileName(resolvedPath);
                if (!string.IsNullOrWhiteSpace(entry.Version) &&
                    !string.IsNullOrWhiteSpace(existingPath) &&
                    !versions.ContainsKey(versionKey))
                {
                    versions[versionKey] = entry.Version;
                }

                if (!string.IsNullOrWhiteSpace(latestPath) && versions.Count >= 3)
                    break;
            }

            return (latestPath, versions);
        }

        private readonly record struct LogEntry(string Path, string Channel, string Version);
    }
}