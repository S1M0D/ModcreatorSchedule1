using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// ViewModel for displaying build logs
    /// </summary>
    public class BuildLogViewModel : ObservableObject
    {
        private string _title = "Build Log";
        private string _logContent = "";
        private string? _logFilePath;
        private string _searchFilter = "";
        private int _selectedTabIndex = 0;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string LogContent
        {
            get => _logContent;
            set
            {
                if (SetProperty(ref _logContent, value))
                {
                    ParseLogContent();
                }
            }
        }

        public string? LogFilePath
        {
            get => _logFilePath;
            set => SetProperty(ref _logFilePath, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    FilterEntries();
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public ObservableCollection<BuildLogEntry> AllErrors { get; } = new ObservableCollection<BuildLogEntry>();
        public ObservableCollection<BuildLogEntry> AllWarnings { get; } = new ObservableCollection<BuildLogEntry>();
        public ObservableCollection<BuildLogEntry> FilteredErrors { get; } = new ObservableCollection<BuildLogEntry>();
        public ObservableCollection<BuildLogEntry> FilteredWarnings { get; } = new ObservableCollection<BuildLogEntry>();

        public int ErrorCount => AllErrors.Count;
        public int WarningCount => AllWarnings.Count;

        private ICommand? _saveLogCommand;
        private ICommand? _closeCommand;
        private ICommand? _clearFilterCommand;

        public ICommand SaveLogCommand => _saveLogCommand!;
        public ICommand CloseCommand => _closeCommand!;
        public ICommand ClearFilterCommand => _clearFilterCommand!;

        public BuildLogViewModel()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _saveLogCommand = new RelayCommand(SaveLog);
            _closeCommand = new RelayCommand(() => CloseRequested?.Invoke());
            _clearFilterCommand = new RelayCommand(() => SearchFilter = "");
        }

        private void ParseLogContent()
        {
            AllErrors.Clear();
            AllWarnings.Clear();

            if (string.IsNullOrWhiteSpace(LogContent))
            {
                OnPropertyChanged(nameof(ErrorCount));
                OnPropertyChanged(nameof(WarningCount));
                FilterEntries();
                return;
            }

            var lines = LogContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            
            // Parse MSBuild/CS compiler error format: file(line,col): error CS####: message
            // Example: Program.cs(12,34): error CS1002: ; expected
            // Also handles: file(line): error CS####: message (no column)
            var errorPattern = new Regex(@"^\s*(.+?)\((\d+)(?:,(\d+))?\)\s*:\s*(error|warning)\s+([A-Z]+\d+)?\s*:\s*(.+)$", RegexOptions.IgnoreCase);
            
            // Also look for simpler error patterns without file location
            var simpleErrorPattern = new Regex(@"^\s*(error|warning)\s+([A-Z]+\d+)?\s*:\s*(.+)$", RegexOptions.IgnoreCase);
            
            // Pattern for errors that span multiple lines (common in MSBuild)
            var multiLineErrorPattern = new Regex(@"^\s*(.+?)\s*:\s*(error|warning)\s+([A-Z]+\d+)?\s*:\s*(.+)$", RegexOptions.IgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i]?.Trim() ?? "";
                if (string.IsNullOrEmpty(line)) continue;

                var match = errorPattern.Match(line);
                int continuationLines = 0;
                
                if (match.Success)
                {
                    var filePath = match.Groups[1].Value.Trim();
                    var lineNum = int.TryParse(match.Groups[2].Value, out var ln) ? (int?)ln : null;
                    var colNum = match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out var cn) ? (int?)cn : null;
                    var isError = match.Groups[4].Value.Equals("error", StringComparison.OrdinalIgnoreCase);
                    var errorCode = match.Groups[5].Success ? match.Groups[5].Value : null;
                    var message = match.Groups[6].Value.Trim();

                    // Get full text including continuation lines (until next error/warning or empty line)
                    var fullText = line;
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var nextLine = lines[j]?.Trim() ?? "";
                        if (string.IsNullOrEmpty(nextLine)) break;
                        if (errorPattern.IsMatch(nextLine) || simpleErrorPattern.IsMatch(nextLine) || multiLineErrorPattern.IsMatch(nextLine))
                            break;
                        fullText += "\n" + lines[j];
                        continuationLines++;
                    }

                    var entry = new BuildLogEntry
                    {
                        Message = message,
                        FilePath = filePath,
                        LineNumber = lineNum,
                        ColumnNumber = colNum,
                        ErrorCode = errorCode,
                        FullText = fullText,
                        IsError = isError
                    };

                    if (isError)
                    {
                        AllErrors.Add(entry);
                    }
                    else
                    {
                        AllWarnings.Add(entry);
                    }

                    // Skip continuation lines in the main loop
                    i += continuationLines;
                }
                else
                {
                    // Try simple pattern
                    var simpleMatch = simpleErrorPattern.Match(line);
                    if (simpleMatch.Success)
                    {
                        var isError = simpleMatch.Groups[1].Value.Equals("error", StringComparison.OrdinalIgnoreCase);
                        var errorCode = simpleMatch.Groups[2].Success ? simpleMatch.Groups[2].Value : null;
                        var message = simpleMatch.Groups[3].Value.Trim();

                        // Get continuation lines
                        var fullText = line;
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var nextLine = lines[j]?.Trim() ?? "";
                            if (string.IsNullOrEmpty(nextLine)) break;
                            if (errorPattern.IsMatch(nextLine) || simpleErrorPattern.IsMatch(nextLine) || multiLineErrorPattern.IsMatch(nextLine))
                                break;
                            fullText += "\n" + lines[j];
                            continuationLines++;
                        }

                        var entry = new BuildLogEntry
                        {
                            Message = message,
                            FullText = fullText,
                            ErrorCode = errorCode,
                            IsError = isError
                        };

                        if (isError)
                        {
                            AllErrors.Add(entry);
                        }
                        else
                        {
                            AllWarnings.Add(entry);
                        }

                        // Skip continuation lines in the main loop
                        i += continuationLines;
                    }
                    else
                    {
                        // Try multi-line error pattern (for errors without file location in first line)
                        var multiMatch = multiLineErrorPattern.Match(line);
                        if (multiMatch.Success)
                        {
                            var filePathOrContext = multiMatch.Groups[1].Value.Trim();
                            var isError = multiMatch.Groups[2].Value.Equals("error", StringComparison.OrdinalIgnoreCase);
                            var errorCode = multiMatch.Groups[3].Success ? multiMatch.Groups[3].Value : null;
                            var message = multiMatch.Groups[4].Value.Trim();

                            // Try to extract file path if it looks like one
                            string? filePath = null;
                            int? lineNum = null;
                            if (filePathOrContext.Contains("\\") || filePathOrContext.Contains("/"))
                            {
                                filePath = filePathOrContext;
                            }

                            var fullText = line;
                            for (int j = i + 1; j < lines.Length; j++)
                            {
                                var nextLine = lines[j]?.Trim() ?? "";
                                if (string.IsNullOrEmpty(nextLine)) break;
                                if (errorPattern.IsMatch(nextLine) || simpleErrorPattern.IsMatch(nextLine) || multiLineErrorPattern.IsMatch(nextLine))
                                    break;
                                fullText += "\n" + lines[j];
                                continuationLines++;
                            }

                            var entry = new BuildLogEntry
                            {
                                Message = message,
                                FilePath = filePath,
                                LineNumber = lineNum,
                                FullText = fullText,
                                ErrorCode = errorCode,
                                IsError = isError
                            };

                            if (isError)
                            {
                                AllErrors.Add(entry);
                            }
                            else
                            {
                                AllWarnings.Add(entry);
                            }

                            // Skip continuation lines in the main loop
                            i += continuationLines;
                        }
                    }
                }
            }

            // If no structured errors found, check for common error indicators
            if (AllErrors.Count == 0 && AllWarnings.Count == 0)
            {
                var lowerContent = LogContent.ToLowerInvariant();
                if (lowerContent.Contains("error") || lowerContent.Contains("failed") || lowerContent.Contains("exception"))
                {
                    // Add the entire log as a single error entry
                    AllErrors.Add(new BuildLogEntry
                    {
                        Message = "Build failed - see full output for details",
                        FullText = LogContent,
                        IsError = true
                    });
                }
            }

            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            FilterEntries();
            
            // Auto-select Errors tab if there are errors, otherwise Warnings tab if there are warnings
            if (AllErrors.Count > 0)
            {
                SelectedTabIndex = 0; // Errors tab
            }
            else if (AllWarnings.Count > 0)
            {
                SelectedTabIndex = 1; // Warnings tab
            }
            else
            {
                SelectedTabIndex = 2; // Full Log tab
            }
        }

        private void FilterEntries()
        {
            FilteredErrors.Clear();
            FilteredWarnings.Clear();

            var filter = SearchFilter?.Trim().ToLowerInvariant() ?? "";

            foreach (var error in AllErrors)
            {
                if (string.IsNullOrEmpty(filter) ||
                    error.Message.ToLowerInvariant().Contains(filter) ||
                    error.FilePath?.ToLowerInvariant().Contains(filter) == true ||
                    error.ErrorCode?.ToLowerInvariant().Contains(filter) == true ||
                    error.FullText.ToLowerInvariant().Contains(filter))
                {
                    FilteredErrors.Add(error);
                }
            }

            foreach (var warning in AllWarnings)
            {
                if (string.IsNullOrEmpty(filter) ||
                    warning.Message.ToLowerInvariant().Contains(filter) ||
                    warning.FilePath?.ToLowerInvariant().Contains(filter) == true ||
                    warning.ErrorCode?.ToLowerInvariant().Contains(filter) == true ||
                    warning.FullText.ToLowerInvariant().Contains(filter))
                {
                    FilteredWarnings.Add(warning);
                }
            }
        }

        private void SaveLog()
        {
            if (string.IsNullOrWhiteSpace(LogContent))
            {
                AppUtils.ShowWarning("No log content to save.");
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = "build_log.txt",
                    DefaultExt = ".txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, LogContent);
                    AppUtils.ShowInfo($"Build log saved to:\n{dialog.FileName}");
                    LogFilePath = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Failed to save log file: {ex.Message}");
            }
        }

        public event System.Action? CloseRequested;
    }
}

