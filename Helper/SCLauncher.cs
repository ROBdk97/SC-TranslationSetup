using System.Text.RegularExpressions;

namespace SC_TranslationSetup.Helper
{
    /// <summary>
    /// Provides helper methods for parsing and extracting information from RSI Launcher log files,
    /// including resolving Star Citizen installation paths and retrieving version data.
    /// </summary>
    /// <remarks>
    /// All members are static and thread-safe. This class is not intended to be instantiated.
    /// Reads all rotated RSI Launcher log files in reverse to prioritise the most recent entries.
    /// Falls back to well-known default installation paths when no log data is available.
    /// </remarks>
    internal static partial class SCLauncher
    {
        private const int MaxVersionEntries = 4;

        /// <summary>
        /// Sub-paths appended to each drive root during fallback discovery.
        /// </summary>
        private static readonly string[] DefaultInstallSubPaths =
        [
            Path.Combine("Roberts Space Industries", "StarCitizen"),
            "StarCitizen",
            Path.Combine("Games", "StarCitizen"),
            "SC"
        ];

        // "filePaths": ["C:\..."]  — JSON array entry in launcher config lines
        [GeneratedRegex(@"""filePaths"":\s*\[\s*""([^""]+)""\s*\]", RegexOptions.IgnoreCase)]
        private static partial Regex FilePathsRegex();

        // SC LIVE 3.24.1 ... \bin\  C:\...
        [GeneratedRegex("""SC (LIVE|PTU|TECH-PREVIEW|EPTU) ([^\s)]+).*?\\bin\\s+(C:\\.+?)(?=\s*"|\s*\)|$)""", RegexOptions.IgnoreCase)]
        private static partial Regex DeltaUpdateRegex();

        // Launching Star Citizen LIVE from (C:\...)
        [GeneratedRegex("""Launching Star Citizen (PTU|LIVE|TECH-PREVIEW|EPTU) from \(([^)]+)\)""", RegexOptions.IgnoreCase)]
        private static partial Regex LauncherRegex();

        // Star Citizen LIVE 3.24.1 ... at C:\...
        [GeneratedRegex("""Star Citizen (LIVE|PTU|TECH-PREVIEW|EPTU) ([^\s]+).*? at (C:\\.+?)(?=\s*\(|\s*"|$)""", RegexOptions.IgnoreCase)]
        private static partial Regex InstallerRegex();

        /// <summary>
        /// Returns the specified path if it exists as a directory, or the directory containing
        /// the file if the path points to an existing file. Returns <see langword="null"/> otherwise.
        /// </summary>
        private static string? GetExistingPath(string path)
        {
            if (Directory.Exists(path))
                return path;

            if (File.Exists(path))
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                    return dir;
            }

            return null;
        }

        /// <summary>
        /// Resolves the full entry path by appending the channel name unless the path already ends with it.
        /// </summary>
        private static string ResolveEntryPath(string path, string channel)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(channel))
                return path;

            string trimmedChannel = channel.Trim();

            // Trim trailing separators so GetFileName always returns the last folder name
            string fileName = Path.GetFileName(
                path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (string.Equals(fileName, trimmedChannel, StringComparison.OrdinalIgnoreCase))
                return path;

            return Path.Combine(path, trimmedChannel);
        }

        /// <summary>
        /// Attempts to parse a log line into a <see cref="LogEntry"/>.
        /// Patterns are evaluated in order of reliability:
        /// filePaths → delta update → launcher launch → installer.
        /// </summary>
        private static bool TryParseLogLine(string line, out LogEntry entry)
        {
            entry = default;

            var filePathsMatch = FilePathsRegex().Match(line);
            if (filePathsMatch.Success)
            {
                entry = new LogEntry(NormalizePath(filePathsMatch.Groups[1].Value), string.Empty, string.Empty);
                return true;
            }

            var deltaMatch = DeltaUpdateRegex().Match(line);
            if (deltaMatch.Success)
            {
                entry = new LogEntry(
                    NormalizePath(deltaMatch.Groups[3].Value),
                    deltaMatch.Groups[1].Value,
                    deltaMatch.Groups[2].Value);
                return true;
            }

            var launcherMatch = LauncherRegex().Match(line);
            if (launcherMatch.Success)
            {
                entry = new LogEntry(
                    NormalizePath(launcherMatch.Groups[2].Value),
                    launcherMatch.Groups[1].Value,
                    string.Empty);
                return true;
            }

            var installerMatch = InstallerRegex().Match(line);
            if (installerMatch.Success)
            {
                entry = new LogEntry(
                    NormalizePath(installerMatch.Groups[3].Value),
                    installerMatch.Groups[1].Value,
                    installerMatch.Groups[2].Value);
                return true;
            }

            return false;

            static string NormalizePath(string path) =>
                path.Trim().Replace(@"\\", @"\");
        }

        /// <summary>
        /// Returns all RSI Launcher log files (e.g. log.log, log.1.log) ordered newest first.
        /// </summary>
        private static IEnumerable<string> GetAllLogFilePaths()
        {
            string logsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "rsilauncher", "logs");

            if (!Directory.Exists(logsDir))
                yield break;

            // Filter to "log*.log" to avoid picking up unrelated files in the same folder
            foreach (string file in Directory.GetFiles(logsDir, "log*.log")
                         .OrderByDescending(File.GetLastWriteTime))
            {
                yield return file;
            }
        }

        /// <summary>
        /// Returns common default Star Citizen installation paths to use as a last-resort fallback
        /// when RSI Launcher logs contain no usable installation data.
        /// </summary>
        private static IEnumerable<string> GetDefaultInstallationPaths()
        {
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Roberts Space Industries", "StarCitizen");

            foreach (DriveInfo drive in DriveInfo.GetDrives()
                         .Where(d => d is { DriveType: DriveType.Fixed, IsReady: true }))
            {
                foreach (string sub in DefaultInstallSubPaths)
                    yield return Path.Combine(drive.Name, sub);
            }
        }

        /// <summary>
        /// Scans a set of log lines in reverse and merges results into
        /// <paramref name="latestPath"/> and <paramref name="versions"/>.
        /// Stops early once a path and <see cref="MaxVersionEntries"/> versions are found.
        /// </summary>
        private static void ProcessLogLines(
            string[] lines,
            ref string latestPath,
            Dictionary<string, string> versions)
        {
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (!TryParseLogLine(lines[i], out LogEntry entry))
                    continue;

                string resolvedPath = ResolveEntryPath(entry.Path, entry.Channel);
                if (string.IsNullOrWhiteSpace(resolvedPath))
                    continue;

                string? existingPath = GetExistingPath(resolvedPath);

                if (string.IsNullOrWhiteSpace(latestPath) && !string.IsNullOrWhiteSpace(existingPath))
                    latestPath = existingPath;

                string versionKey = Path.GetFileName(
                    resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                if (!string.IsNullOrWhiteSpace(entry.Version) &&
                    !string.IsNullOrWhiteSpace(existingPath) &&
                    !versions.ContainsKey(versionKey))
                {
                    versions[versionKey] = entry.Version;
                }

                if (!string.IsNullOrWhiteSpace(latestPath) && versions.Count >= MaxVersionEntries)
                    break;
            }
        }

        /// <summary>
        /// Asynchronously retrieves the most recent valid Star Citizen installation path and a
        /// version lookup (up to <see cref="MaxVersionEntries"/> entries) by reading RSI Launcher
        /// log files in reverse order. Falls back to well-known default paths when no log data
        /// can be found.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>
        /// A tuple containing the latest existing installation directory and a case-insensitive
        /// dictionary mapping channel folder names to their version strings.
        /// Both values are empty if no valid data could be located.
        /// </returns>
        internal static async Task<(string LatestPath, Dictionary<string, string> VersionLookup)> GetLogDataAsync(
            CancellationToken cancellationToken = default)
        {
            string latestPath = string.Empty;
            var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string logFilePath in GetAllLogFilePaths())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string[] lines = await File.ReadAllLinesAsync(logFilePath, cancellationToken)
                        .ConfigureAwait(false);

                    ProcessLogLines(lines, ref latestPath, versions);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Log file locked or inaccessible — try the next one
                    continue;
                }

                // Early exit: no need to scan older log files once satisfied
                if (!string.IsNullOrWhiteSpace(latestPath) && versions.Count >= MaxVersionEntries)
                    break;
            }

            // Fallback: RSI logs missing or empty — check well-known installation locations
            if (string.IsNullOrWhiteSpace(latestPath))
            {
                foreach (string defaultPath in GetDefaultInstallationPaths())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (Directory.Exists(defaultPath))
                    {
                        latestPath = defaultPath;
                        break;
                    }
                }
            }

            return (latestPath, versions);
        }

        private readonly record struct LogEntry(string Path, string Channel, string Version);
    }
}