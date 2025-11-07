using System.Windows;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for BuildLogWindow.xaml
    /// </summary>
    public partial class BuildLogWindow : Window
    {
        public BuildLogWindow()
        {
            InitializeComponent();
        }

        public BuildLogWindow(BuildLogViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.CloseRequested += () => Close();
        }
    }
}

