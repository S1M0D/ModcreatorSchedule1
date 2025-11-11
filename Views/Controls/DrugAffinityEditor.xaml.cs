using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Views.Controls
{
    /// <summary>
    /// Editor control for managing drug affinities list
    /// </summary>
    public partial class DrugAffinityEditor : UserControl
    {
        public static readonly DependencyProperty DrugAffinitiesProperty =
            DependencyProperty.Register(nameof(DrugAffinities), typeof(ObservableCollection<DrugAffinity>),
                typeof(DrugAffinityEditor), new PropertyMetadata(null));

        public ObservableCollection<DrugAffinity> DrugAffinities
        {
            get => (ObservableCollection<DrugAffinity>)GetValue(DrugAffinitiesProperty);
            set => SetValue(DrugAffinitiesProperty, value);
        }

        public DrugAffinityEditor()
        {
            InitializeComponent();
        }

        private void AddAffinity_Click(object sender, RoutedEventArgs e)
        {
            // Add directly to the bound collection (should never be null due to model initialization)
            if (DrugAffinities == null)
                return;

            DrugAffinities.Add(new DrugAffinity
            {
                DrugType = "Marijuana",
                AffinityValue = 0.5f
            });
        }

        private void RemoveAffinity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DrugAffinity affinity && DrugAffinities != null)
            {
                DrugAffinities.Remove(affinity);
            }
        }
    }
}
