using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service for handling application updates via GitHub Releases
    /// </summary>
    public class UpdateService
    {
        private const string GitHubOwner = "ESTONlA";
        private const string GitHubRepo = "ModcreatorSchedule1";
        private const string GitHubApiBase = "https://api.github.com";
        private static readonly HttpClient HttpClient = new HttpClient();

        // Enable debug mode to see detailed logs and use test XML
        public static bool DebugMode { get; set; } = false;

        // Test XML URL - can point to a local file or test branch
        public static string? TestXmlUrl { get; set; }

        static UpdateService()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Schedule1ModdingTool-Updater");
        }

        /// <summary>
        /// Checks for updates based on the user's update channel preference
        /// </summary>
        public static async Task CheckForUpdatesAsync(bool silent = false)
        {
            var debugLog = new StringBuilder();

            try
            {
                var currentVersion = VersionInfo.Version;
                debugLog.AppendLine($"[UpdateService] Current version: {currentVersion}");

                var settings = ModSettings.Load();
                var isBetaChannel = settings.UpdateChannel == UpdateChannel.Beta;
                debugLog.AppendLine($"[UpdateService] Update channel: {(isBetaChannel ? "Beta" : "Stable")}");

                // Configure AutoUpdater.NET
                AutoUpdater.LetUserSelectRemindLater = true;
                AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
                AutoUpdater.RemindLaterAt = 1;
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.ShowRemindLaterButton = true;
                AutoUpdater.Mandatory = false;

                // CRITICAL: Set the installed version explicitly
                // AutoUpdater.NET defaults to Assembly.Version (1.0.0) if not set
                // Strip build metadata (everything after '+') since System.Version can't parse it
                var cleanVersion = VersionInfo.BaseVersion.Split('+')[0];
                AutoUpdater.InstalledVersion = new Version(cleanVersion);
                debugLog.AppendLine($"[UpdateService] Set InstalledVersion to: {AutoUpdater.InstalledVersion} (from {VersionInfo.Version})");

                // Check GitHub API first to filter by channel if needed
                GitHubReleaseInfo? latestRelease = null;
                if (!isBetaChannel)
                {
                    // Stable channel: get latest non-beta release
                    latestRelease = await GetLatestReleaseAsync(includeBeta: false);
                    debugLog.AppendLine($"[UpdateService] Latest stable release: {latestRelease?.Version ?? "none"}");
                }
                else
                {
                    // Beta channel: get latest release (beta or stable)
                    latestRelease = await GetLatestReleaseAsync(includeBeta: true);
                    debugLog.AppendLine($"[UpdateService] Latest beta release: {latestRelease?.Version ?? "none"}");
                }

                // If no suitable release found, skip update check
                if (latestRelease == null)
                {
                    debugLog.AppendLine("[UpdateService] No suitable release found, skipping update check");
                    if (DebugMode)
                    {
                        MessageBox.Show(debugLog.ToString(), "Update Check Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }

                // AutoUpdater.NET requires an XML file URL, not a GitHub releases page
                // The XML file is hosted in the repository and updated by the release workflow
                // Use raw.githubusercontent.com to access the XML file directly
                var xmlFileUrl = TestXmlUrl ?? $"https://raw.githubusercontent.com/{GitHubOwner}/{GitHubRepo}/beta/AutoUpdater.xml";
                debugLog.AppendLine($"[UpdateService] XML URL: {xmlFileUrl}");

                // Set up event handler for update check results
                AutoUpdater.CheckForUpdateEvent += (args) =>
                {
                    debugLog.AppendLine($"[UpdateService] CheckForUpdateEvent fired");

                    // Helper to dispatch UI operations to the UI thread
                    void DispatchToUIThread(Action action)
                    {
                        var app = Application.Current;
                        if (app == null)
                        {
                            debugLog.AppendLine("[UpdateService] Application.Current is null, cannot show UI");
                            return;
                        }

                        if (app.Dispatcher.CheckAccess())
                        {
                            // Already on UI thread
                            action();
                        }
                        else
                        {
                            // Dispatch to UI thread
                            app.Dispatcher.BeginInvoke(action);
                        }
                    }

                    if (args == null)
                    {
                        debugLog.AppendLine("[UpdateService] args is null");
                        if (DebugMode || !silent)
                        {
                            DispatchToUIThread(() =>
                            {
                                MessageBox.Show(debugLog.ToString() + "\n\nUpdate check returned null args.",
                                    "Update Check Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                        return;
                    }

                    if (args.Error != null)
                    {
                        debugLog.AppendLine($"[UpdateService] Error: {args.Error.Message}");
                        if (DebugMode || !silent)
                        {
                            DispatchToUIThread(() =>
                            {
                                MessageBox.Show(debugLog.ToString() + $"\n\nError: {args.Error.Message}",
                                    "Update Check Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            });
                        }
                        return;
                    }

                    debugLog.AppendLine($"[UpdateService] IsUpdateAvailable: {args.IsUpdateAvailable}");
                    debugLog.AppendLine($"[UpdateService] CurrentVersion (from XML): {args.CurrentVersion}");
                    debugLog.AppendLine($"[UpdateService] InstalledVersion: {args.InstalledVersion}");

                    if (args.IsUpdateAvailable)
                    {
                        debugLog.AppendLine($"[UpdateService] Update available: {args.CurrentVersion}");

                        // Additional channel filtering if AutoUpdater returns a beta release for stable users
                        if (!isBetaChannel && IsBetaRelease(args.CurrentVersion))
                        {
                            debugLog.AppendLine("[UpdateService] Skipping beta release for stable channel user");
                            if (DebugMode)
                            {
                                DispatchToUIThread(() =>
                                {
                                    MessageBox.Show(debugLog.ToString(), "Update Check Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                                });
                            }
                            return;
                        }

                        // Show update dialog (always show if update is available, silent only affects error messages)
                        debugLog.AppendLine("[UpdateService] Showing update dialog");
                        DispatchToUIThread(() =>
                        {
                            AutoUpdater.ShowUpdateForm(args);
                        });
                    }
                    else
                    {
                        debugLog.AppendLine("[UpdateService] No update available");
                        if (DebugMode || !silent)
                        {
                            DispatchToUIThread(() =>
                            {
                                MessageBox.Show(debugLog.ToString(), "Update Check Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                    }
                };

                debugLog.AppendLine("[UpdateService] Starting AutoUpdater.Start()");

                // Start update check - AutoUpdater.NET will download and parse the XML file
                AutoUpdater.Start(xmlFileUrl);
            }
            catch (Exception ex)
            {
                debugLog.AppendLine($"[UpdateService] Exception: {ex.Message}");
                debugLog.AppendLine($"[UpdateService] Stack trace: {ex.StackTrace}");

                if (!silent || DebugMode)
                {
                    var app = Application.Current;
                    if (app != null)
                    {
                        if (app.Dispatcher.CheckAccess())
                        {
                            // Already on UI thread
                            MessageBox.Show(
                                debugLog.ToString() + $"\n\nFailed to check for updates: {ex.Message}",
                                "Update Check Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                        else
                        {
                            // Dispatch to UI thread
                            app.Dispatcher.BeginInvoke(() =>
                            {
                                MessageBox.Show(
                                    debugLog.ToString() + $"\n\nFailed to check for updates: {ex.Message}",
                                    "Update Check Failed",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Manually checks for updates and shows a dialog
        /// </summary>
        public static async Task CheckForUpdatesManuallyAsync()
        {
            await CheckForUpdatesAsync(silent: false);
        }

        /// <summary>
        /// Checks if a version string indicates a beta/pre-release
        /// </summary>
        private static bool IsBetaRelease(string version)
        {
            if (string.IsNullOrEmpty(version))
                return false;

            return version.Contains("-beta", StringComparison.OrdinalIgnoreCase) ||
                   version.Contains("-alpha", StringComparison.OrdinalIgnoreCase) ||
                   version.Contains("-pre", StringComparison.OrdinalIgnoreCase) ||
                   version.Contains("beta", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the latest release information from GitHub
        /// </summary>
        public static async Task<GitHubReleaseInfo?> GetLatestReleaseAsync(bool includeBeta = false)
        {
            try
            {
                var url = $"{GitHubApiBase}/repos/{GitHubOwner}/{GitHubRepo}/releases";
                var response = await HttpClient.GetStringAsync(url);
                var releases = JsonSerializer.Deserialize<GitHubRelease[]>(response);

                if (releases == null || releases.Length == 0)
                    return null;

                // Filter releases based on channel preference
                GitHubRelease? latestRelease = null;
                foreach (var release in releases)
                {
                    if (release.Draft)
                        continue;

                    if (!includeBeta && IsBetaRelease(release.TagName))
                        continue;

                    if (latestRelease == null || 
                        CompareVersions(release.TagName, latestRelease.TagName) > 0)
                    {
                        latestRelease = release;
                    }
                }

                if (latestRelease == null)
                    return null;

                return new GitHubReleaseInfo
                {
                    Version = latestRelease.TagName.TrimStart('v'),
                    Name = latestRelease.Name,
                    Body = latestRelease.Body,
                    PublishedAt = latestRelease.PublishedAt,
                    DownloadUrl = latestRelease.Assets?.FirstOrDefault()?.BrowserDownloadUrl
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Compares two version strings
        /// </summary>
        private static int CompareVersions(string v1, string v2)
        {
            // Simple comparison - remove 'v' prefix and compare
            var version1 = v1.TrimStart('v');
            var version2 = v2.TrimStart('v');

            if (Version.TryParse(version1.Split('-')[0], out var ver1) &&
                Version.TryParse(version2.Split('-')[0], out var ver2))
            {
                return ver1.CompareTo(ver2);
            }

            return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
        }

        private class GitHubRelease
        {
            public string TagName { get; set; } = "";
            public string Name { get; set; } = "";
            public string Body { get; set; } = "";
            public DateTime PublishedAt { get; set; }
            public bool Draft { get; set; }
            public bool Prerelease { get; set; }
            public GitHubAsset[]? Assets { get; set; }
        }

        private class GitHubAsset
        {
            public string BrowserDownloadUrl { get; set; } = "";
            public string Name { get; set; } = "";
        }

        public class GitHubReleaseInfo
        {
            public string Version { get; set; } = "";
            public string Name { get; set; } = "";
            public string Body { get; set; } = "";
            public DateTime PublishedAt { get; set; }
            public string? DownloadUrl { get; set; }
        }
    }
}

