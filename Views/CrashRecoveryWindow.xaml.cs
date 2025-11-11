using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Dialog window for recovering projects after a crash.
    /// </summary>
    public partial class CrashRecoveryWindow : Window
    {
        private readonly CrashRecoveryService _crashRecoveryService;
        private List<RecoverableProject> _recoverableProjects;

        public QuestProject? RecoveredProject { get; private set; }
        public bool UserSkippedRecovery { get; private set; }

        public ICommand RecoverCommand { get; }
        public ICommand DiscardCommand { get; }

        public CrashRecoveryWindow(CrashRecoveryService crashRecoveryService)
        {
            InitializeComponent();

            _crashRecoveryService = crashRecoveryService;
            _recoverableProjects = new List<RecoverableProject>();

            RecoverCommand = new RelayCommand<RecoverableProject>(RecoverProject);
            DiscardCommand = new RelayCommand<RecoverableProject>(DiscardProject);

            DataContext = this;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadRecoverableProjects();
        }

        private void LoadRecoverableProjects()
        {
            _recoverableProjects = _crashRecoveryService.GetRecoverableProjects();

            if (_recoverableProjects.Any())
            {
                RecoverableProjectsList.ItemsSource = _recoverableProjects;
                NoProjectsMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                RecoverableProjectsList.ItemsSource = null;
                NoProjectsMessage.Visibility = Visibility.Visible;
            }
        }

        private void RecoverProject(RecoverableProject? recoverableProject)
        {
            if (recoverableProject == null)
                return;

            var project = _crashRecoveryService.RecoverProject(recoverableProject.AutoSaveFilePath);
            if (project != null)
            {
                RecoveredProject = project;

                // Clear the session marker
                _crashRecoveryService.ClearSessionMarker();

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "Failed to recover the project. The auto-save file may be corrupted.",
                    "Recovery Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Remove from list
                _recoverableProjects.Remove(recoverableProject);
                LoadRecoverableProjects();
            }
        }

        private void DiscardProject(RecoverableProject? recoverableProject)
        {
            if (recoverableProject == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to discard the auto-save for '{recoverableProject.ProjectName}'? This cannot be undone.",
                "Confirm Discard",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _crashRecoveryService.DiscardRecovery(recoverableProject.AutoSaveFilePath);

                // Remove from list
                _recoverableProjects.Remove(recoverableProject);
                LoadRecoverableProjects();

                // If no more projects, clear session marker
                if (!_recoverableProjects.Any())
                {
                    _crashRecoveryService.ClearSessionMarker();
                }
            }
        }

        private void SkipRecovery_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to skip recovery? Auto-saved projects will remain available for recovery later.",
                "Skip Recovery",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                UserSkippedRecovery = true;
                DialogResult = false;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Clear session marker if all projects handled
            if (!_recoverableProjects.Any())
            {
                _crashRecoveryService.ClearSessionMarker();
            }

            DialogResult = false;
            Close();
        }
    }
}
