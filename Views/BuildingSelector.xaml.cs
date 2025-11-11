using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for BuildingSelector.xaml
    /// </summary>
    public partial class BuildingSelector : UserControl
    {
        public static readonly DependencyProperty SelectedBuildingTypeNameProperty =
            DependencyProperty.Register(nameof(SelectedBuildingTypeName), typeof(string), typeof(BuildingSelector),
                new PropertyMetadata(null, OnSelectedBuildingTypeNameChanged));

        public static readonly DependencyProperty AvailableBuildingsProperty =
            DependencyProperty.Register(nameof(AvailableBuildings), typeof(System.Collections.ObjectModel.ObservableCollection<BuildingInfo>), typeof(BuildingSelector),
                new PropertyMetadata(null, OnAvailableBuildingsChanged));

        public string SelectedBuildingTypeName
        {
            get => (string)GetValue(SelectedBuildingTypeNameProperty);
            set => SetValue(SelectedBuildingTypeNameProperty, value);
        }

        public System.Collections.ObjectModel.ObservableCollection<BuildingInfo> AvailableBuildings
        {
            get => (System.Collections.ObjectModel.ObservableCollection<BuildingInfo>)GetValue(AvailableBuildingsProperty);
            set => SetValue(AvailableBuildingsProperty, value);
        }

        public BuildingSelector()
        {
            InitializeComponent();
            Loaded += BuildingSelector_Loaded;
            BuildingComboBox.SelectionChanged += BuildingComboBox_SelectionChanged;
            BuildingComboBox.LostFocus += BuildingComboBox_LostFocus;
            BuildingComboBox.DropDownClosed += BuildingComboBox_DropDownClosed;
        }

        private void BuildingSelector_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBuildingList();
            // Sync initial selection if SelectedBuildingTypeName is already set
            if (!string.IsNullOrWhiteSpace(SelectedBuildingTypeName) && AvailableBuildings != null)
            {
                var building = AvailableBuildings.FirstOrDefault(b => b.TypeName == SelectedBuildingTypeName);
                if (building != null && BuildingComboBox.SelectedItem != building)
                {
                    BuildingComboBox.SelectedItem = building;
                    if (BuildingComboBox.IsEditable)
                    {
                        BuildingComboBox.Text = building.DisplayName;
                    }
                }
            }
        }

        private void BuildingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only update if the selection actually changed to avoid circular updates
            if (BuildingComboBox.SelectedItem is BuildingInfo building)
            {
                if (SelectedBuildingTypeName != building.TypeName)
                {
                    SelectedBuildingTypeName = building.TypeName;
                }
                // Ensure text displays the display name
                if (BuildingComboBox.IsEditable && BuildingComboBox.Text != building.DisplayName)
                {
                    BuildingComboBox.Text = building.DisplayName;
                }
            }
            else if (BuildingComboBox.SelectedItem == null && !string.IsNullOrWhiteSpace(SelectedBuildingTypeName))
            {
                // If selection was cleared but we still have a type name, try to restore it
                var foundBuilding = AvailableBuildings?.FirstOrDefault(b => b.TypeName == SelectedBuildingTypeName);
                if (foundBuilding != null)
                {
                    BuildingComboBox.SelectedItem = foundBuilding;
                    if (BuildingComboBox.IsEditable)
                    {
                        BuildingComboBox.Text = foundBuilding.DisplayName;
                    }
                }
            }
        }

        private void BuildingComboBox_DropDownClosed(object sender, EventArgs e)
        {
            // When dropdown closes, ensure the selection is preserved
            if (BuildingComboBox.SelectedItem is BuildingInfo selectedBuilding)
            {
                SelectedBuildingTypeName = selectedBuilding.TypeName;
            }
        }

        private void BuildingComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Handle manual entry when ComboBox loses focus
            if (BuildingComboBox.IsEditable)
            {
                // If there's a selected item, preserve it
                if (BuildingComboBox.SelectedItem is BuildingInfo selectedBuilding)
                {
                    SelectedBuildingTypeName = selectedBuilding.TypeName;
                    // Ensure the text displays correctly
                    BuildingComboBox.Text = selectedBuilding.DisplayName;
                    return;
                }

                // If text is entered, try to match it
                if (!string.IsNullOrWhiteSpace(BuildingComboBox.Text))
                {
                    var building = AvailableBuildings?.FirstOrDefault(b => 
                        b.TypeName.Equals(BuildingComboBox.Text, StringComparison.OrdinalIgnoreCase) || 
                        b.DisplayName.Equals(BuildingComboBox.Text, StringComparison.OrdinalIgnoreCase));
                    if (building != null)
                    {
                        SelectedBuildingTypeName = building.TypeName;
                        BuildingComboBox.SelectedItem = building;
                        BuildingComboBox.Text = building.DisplayName;
                    }
                    else
                    {
                        // Allow custom entry - keep the text as-is
                        SelectedBuildingTypeName = BuildingComboBox.Text;
                    }
                }
                // If text is empty but we have a SelectedBuildingTypeName, restore the selection
                else if (!string.IsNullOrWhiteSpace(SelectedBuildingTypeName))
                {
                    var building = AvailableBuildings?.FirstOrDefault(b => b.TypeName == SelectedBuildingTypeName);
                    if (building != null)
                    {
                        BuildingComboBox.SelectedItem = building;
                        BuildingComboBox.Text = building.DisplayName;
                    }
                }
            }
        }

        private static void OnSelectedBuildingTypeNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BuildingSelector selector)
            {
                // Skip if the value hasn't actually changed
                if (e.OldValue?.ToString() == e.NewValue?.ToString())
                    return;

                // Update selection if needed
                if (selector.AvailableBuildings != null && e.NewValue is string typeName && !string.IsNullOrWhiteSpace(typeName))
                {
                    var building = selector.AvailableBuildings.FirstOrDefault(b => b.TypeName == typeName);
                    if (building != null)
                    {
                        // Only update if the selected item is different to avoid circular updates
                        if (selector.BuildingComboBox.SelectedItem != building)
                        {
                            selector.BuildingComboBox.SelectedItem = building;
                            // Set the text to display name for better UX
                            if (selector.BuildingComboBox.IsEditable)
                            {
                                selector.BuildingComboBox.Text = building.DisplayName;
                            }
                        }
                    }
                    else if (selector.BuildingComboBox.IsEditable)
                    {
                        // Custom entry - set text only if it's different
                        if (selector.BuildingComboBox.Text != typeName)
                        {
                            selector.BuildingComboBox.Text = typeName;
                        }
                    }
                }
                else if (e.NewValue == null || string.IsNullOrWhiteSpace(e.NewValue.ToString()))
                {
                    // Value was cleared - clear selection
                    selector.BuildingComboBox.SelectedItem = null;
                    if (selector.BuildingComboBox.IsEditable)
                    {
                        selector.BuildingComboBox.Text = string.Empty;
                    }
                }
            }
        }

        private static void OnAvailableBuildingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BuildingSelector selector)
            {
                selector.UpdateBuildingList();
            }
        }

        public void UpdateBuildingList()
        {
            BuildingComboBox.ItemsSource = AvailableBuildings;
        }
    }
}

