using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views.Controls
{
    /// <summary>
    /// Editor control for schedule actions with dynamic fields based on action type
    /// </summary>
    public partial class ScheduleActionEditor : UserControl
    {
        private ObservableCollection<BuildingInfo> _availableBuildings;

        public ObservableCollection<BuildingInfo> AvailableBuildings
        {
            get
            {
                if (_availableBuildings == null)
                {
                    _availableBuildings = new ObservableCollection<BuildingInfo>(BuildingRegistryService.GetAllBuildings());
                }
                return _availableBuildings;
            }
        }

        public ScheduleActionEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += ScheduleActionEditor_Loaded;
        }

        private void ScheduleActionEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // Set AvailableBuildings on BuildingSelector after it's loaded
            // This ensures the building list is populated, similar to how NPC combo boxes work
            if (BuildingSelector != null)
            {
                BuildingSelector.AvailableBuildings = AvailableBuildings;
                // Ensure the building list is updated after setting AvailableBuildings
                // This is similar to how NpcComboBox_Loaded sets ItemsSource directly
                BuildingSelector.UpdateBuildingList();
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is NpcScheduleAction action)
            {
                UpdateVisibleFields(action.ActionType);
                UpdateNullableBindings(action);
            }
        }

        private void UpdateNullableBindings(NpcScheduleAction action)
        {
            // Update DoorIndex textbox
            if (DoorIndexTextBox != null)
            {
                DoorIndexTextBox.Text = action.DoorIndex?.ToString() ?? string.Empty;
            }

            // Update OverrideParkingType combobox
            if (OverrideParkingTypeComboBox != null)
            {
                if (action.OverrideParkingType == null)
                {
                    OverrideParkingTypeComboBox.SelectedIndex = 0; // (Default)
                }
                else if (action.OverrideParkingType == true)
                {
                    OverrideParkingTypeComboBox.SelectedIndex = 1; // True
                }
                else
                {
                    OverrideParkingTypeComboBox.SelectedIndex = 2; // False
                }
            }
        }

        private void DoorIndexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is NpcScheduleAction action && sender is System.Windows.Controls.TextBox textBox)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    action.DoorIndex = null;
                }
                else if (int.TryParse(textBox.Text, out int value))
                {
                    action.DoorIndex = value;
                }
                else
                {
                    textBox.Text = action.DoorIndex?.ToString() ?? string.Empty;
                }
            }
        }

        private void OverrideParkingTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is NpcScheduleAction action && sender is System.Windows.Controls.ComboBox comboBox)
            {
                if (comboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    if (item.Tag.ToString() == "True")
                    {
                        action.OverrideParkingType = true;
                    }
                    else if (item.Tag.ToString() == "False")
                    {
                        action.OverrideParkingType = false;
                    }
                }
                else
                {
                    action.OverrideParkingType = null;
                }
            }
        }

        private void ActionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is NpcScheduleAction action)
            {
                UpdateVisibleFields(action.ActionType);
            }
        }

        private void UpdateVisibleFields(ScheduleActionType actionType)
        {
            // Hide all dynamic field panels
            WalkToFields.Visibility = Visibility.Collapsed;
            StayInBuildingFields.Visibility = Visibility.Collapsed;
            LocationDialogueFields.Visibility = Visibility.Collapsed;
            DriveToCarParkFields.Visibility = Visibility.Collapsed;
            SitAtSeatSetFields.Visibility = Visibility.Collapsed;
            UseVendingMachineFields.Visibility = Visibility.Collapsed;
            UseATMFields.Visibility = Visibility.Collapsed;
            NoExtraFields.Visibility = Visibility.Collapsed;

            // Show relevant panel based on action type
            switch (actionType)
            {
                case ScheduleActionType.WalkTo:
                    WalkToFields.Visibility = Visibility.Visible;
                    break;

                case ScheduleActionType.StayInBuilding:
                    StayInBuildingFields.Visibility = Visibility.Visible;
                    break;

                case ScheduleActionType.LocationDialogue:
                    LocationDialogueFields.Visibility = Visibility.Visible;
                    break;

                case ScheduleActionType.DriveToCarPark:
                    DriveToCarParkFields.Visibility = Visibility.Visible;
                    break;

                case ScheduleActionType.SitAtSeatSet:
                    SitAtSeatSetFields.Visibility = Visibility.Visible;
                    break;

                case ScheduleActionType.UseVendingMachine:
                    UseVendingMachineFields.Visibility = Visibility.Visible;
                    break;

                case ScheduleActionType.UseATM:
                    UseATMFields.Visibility = Visibility.Visible;
                    break;

                case ScheduleActionType.HandleDeal:
                case ScheduleActionType.EnsureDealSignal:
                    NoExtraFields.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
