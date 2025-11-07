using System;
using System.IO;
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

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        public string? LogFilePath
        {
            get => _logFilePath;
            set => SetProperty(ref _logFilePath, value);
        }

        private ICommand? _saveLogCommand;
        private ICommand? _closeCommand;

        public ICommand SaveLogCommand => _saveLogCommand!;
        public ICommand CloseCommand => _closeCommand!;

        public BuildLogViewModel()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _saveLogCommand = new RelayCommand(SaveLog);
            _closeCommand = new RelayCommand(() => CloseRequested?.Invoke());
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

