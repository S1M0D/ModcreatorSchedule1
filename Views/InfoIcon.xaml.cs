using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for InfoIcon.xaml
    /// </summary>
    public partial class InfoIcon : UserControl
    {
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.Register(nameof(PropertyName), typeof(string), typeof(InfoIcon),
                new PropertyMetadata(null, OnPropertyNameChanged));

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(InfoIcon));

        public static readonly DependencyProperty DocumentationUrlProperty =
            DependencyProperty.Register(nameof(DocumentationUrl), typeof(string), typeof(InfoIcon));

        public static readonly DependencyProperty ExperienceLevelProperty =
            DependencyProperty.Register(nameof(ExperienceLevel), typeof(ExperienceLevel?), typeof(InfoIcon),
                new PropertyMetadata(null, OnExperienceLevelChanged));

        public string PropertyName
        {
            get => (string)GetValue(PropertyNameProperty);
            set => SetValue(PropertyNameProperty, value);
        }

        public string TooltipText
        {
            get => (string)GetValue(TooltipTextProperty);
            set => SetValue(TooltipTextProperty, value);
        }

        public string DocumentationUrl
        {
            get => (string)GetValue(DocumentationUrlProperty);
            set => SetValue(DocumentationUrlProperty, value);
        }

        public ExperienceLevel? ExperienceLevel
        {
            get => (ExperienceLevel?)GetValue(ExperienceLevelProperty);
            set => SetValue(ExperienceLevelProperty, value);
        }

        public InfoIcon()
        {
            InitializeComponent();
        }

        private static void OnPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InfoIcon icon)
            {
                icon.UpdateTooltipInfo();
            }
        }

        private static void OnExperienceLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InfoIcon icon && !string.IsNullOrWhiteSpace(icon.PropertyName))
            {
                icon.UpdateTooltipInfo();
            }
        }

        private void UpdateTooltipInfo()
        {
            if (string.IsNullOrWhiteSpace(PropertyName))
                return;

            var tooltipInfo = TooltipInfoExtractor.GetTooltipInfo(PropertyName, null, null, ExperienceLevel);
            
            if (tooltipInfo.HasContent)
            {
                var tooltipText = tooltipInfo.Text ?? string.Empty;
                
                if (!string.IsNullOrWhiteSpace(tooltipInfo.DocumentationUrl))
                {
                    if (!string.IsNullOrWhiteSpace(tooltipText))
                    {
                        tooltipText += "\n\nClick icon to open documentation";
                    }
                    else
                    {
                        tooltipText = "Click icon to open documentation";
                    }
                }

                TooltipText = tooltipText;
                DocumentationUrl = tooltipInfo.DocumentationUrl;
                
                // Show the icon
                Visibility = Visibility.Visible;
            }
            else
            {
                // Hide the icon if no tooltip info available
                Visibility = Visibility.Collapsed;
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(DocumentationUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DocumentationUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open documentation URL: {ex.Message}", 
                        "Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            }
        }
    }
}

