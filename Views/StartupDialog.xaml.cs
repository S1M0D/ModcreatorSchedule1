using System.Windows;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for StartupDialog.xaml
    /// </summary>
    public partial class StartupDialog : Window
    {
        public enum StartupAction
        {
            None,
            CreateNew,
            OpenExisting,
            Exit
        }

        public StartupAction SelectedAction { get; private set; } = StartupAction.None;

        public StartupDialog()
        {
            InitializeComponent();
        }

        private void CreateNewProject_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = StartupAction.CreateNew;
            DialogResult = true;
            Close();
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = StartupAction.OpenExisting;
            DialogResult = true;
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = StartupAction.Exit;
            DialogResult = false;
            Close();
        }
    }
}

