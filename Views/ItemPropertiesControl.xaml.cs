using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for ItemPropertiesControl.xaml.
    /// </summary>
    public partial class ItemPropertiesControl : UserControl
    {
        public ItemPropertiesControl()
        {
            InitializeComponent();
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private ItemBlueprint? SelectedItem => ViewModel?.SelectedItemBlueprint;

        private void AddSpecificShop_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            var shopName = SpecificShopNameComboBox.Text?.Trim();
            if (item == null || string.IsNullOrWhiteSpace(shopName))
                return;

            if (!item.ShopNames.Any(existing => string.Equals(existing, shopName, StringComparison.OrdinalIgnoreCase)))
            {
                item.ShopNames.Add(shopName);
            }

            SpecificShopNameComboBox.Text = string.Empty;
        }

        private void RemoveSpecificShop_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || SpecificShopsListBox.SelectedItem is not string selectedShop)
                return;

            item.ShopNames.Remove(selectedShop);
        }

        private void AddBlockedSlot_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || BlockedSlotComboBox.SelectedItem is not ClothingSlotOption slot)
                return;

            if (!item.BlockedClothingSlots.Contains(slot))
            {
                item.BlockedClothingSlots.Add(slot);
            }
        }

        private void RemoveBlockedSlot_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || BlockedSlotsListBox.SelectedItem is not ClothingSlotOption slot)
                return;

            item.BlockedClothingSlots.Remove(slot);
        }
    }
}
