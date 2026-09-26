using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for DonateDialog.xaml
    /// </summary>
    public partial class DonateDialog : Window
    {
        public ObservableCollection<Contributor> Contributors { get; } = new ObservableCollection<Contributor>();

        public DonateDialog()
        {
            InitializeComponent();
            DataContext = this;
            LoadContributors();
        }

        private void LoadContributors()
        {
            Contributors.Add(new Contributor
            {
                Name = "Bars",
                ProfilePicturePath = GetProfilePicturePath("IfBars.png"),
                KoFiUrl = "https://ko-fi.com/ifbars"
            });

            Contributors.Add(new Contributor
            {
                Name = "Estonia",
                ProfilePicturePath = GetProfilePicturePath("Estonia.png"),
                KoFiUrl = "https://ko-fi.com/estonla"
            });

            Contributors.Add(new Contributor
            {
                Name = "SirTidez",
                ProfilePicturePath = GetProfilePicturePath("SirTidez.png"),
                KoFiUrl = "https://ko-fi.com/sirtidez"
            });
        }

        private string GetProfilePicturePath(string filename)
        {
            // Use pack URI for WPF resources
            // Images should be added to Resources/Contributors/ folder with Build Action = "Resource"
            return $"pack://application:,,,/Resources/Contributors/{filename}";
        }

        private void KoFi_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Contributor contributor && !string.IsNullOrEmpty(contributor.KoFiUrl))
            {
                OpenUrl(contributor.KoFiUrl);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Silently fail if URL cannot be opened
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Only drag if clicking on non-interactive elements
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                // Check if the original source or its parent is a button or interactive control
                var originalSource = e.OriginalSource as DependencyObject;
                if (originalSource != null)
                {
                    // Walk up the visual tree to check if we clicked on a button
                    var current = originalSource;
                    while (current != null)
                    {
                        if (current is Button)
                        {
                            return; // Don't drag if clicking on a button
                        }
                        current = VisualTreeHelper.GetParent(current);
                    }
                }
                
                // Verify mouse button is still pressed before calling DragMove
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    e.Handled = true;
                    DragMove();
                }
            }
        }
    }
}

