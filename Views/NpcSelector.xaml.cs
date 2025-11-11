using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for NpcSelector.xaml
    /// </summary>
    public partial class NpcSelector : UserControl
    {
        public static readonly DependencyProperty SelectedNpcIdProperty =
            DependencyProperty.Register(nameof(SelectedNpcId), typeof(string), typeof(NpcSelector),
                new PropertyMetadata(null, OnSelectedNpcIdChanged));

        public static readonly DependencyProperty AvailableNpcsProperty =
            DependencyProperty.Register(nameof(AvailableNpcs), typeof(System.Collections.ObjectModel.ObservableCollection<NpcInfo>), typeof(NpcSelector),
                new PropertyMetadata(null, OnAvailableNpcsChanged));

        public string SelectedNpcId
        {
            get => (string)GetValue(SelectedNpcIdProperty);
            set => SetValue(SelectedNpcIdProperty, value);
        }

        public System.Collections.ObjectModel.ObservableCollection<NpcInfo> AvailableNpcs
        {
            get => (System.Collections.ObjectModel.ObservableCollection<NpcInfo>)GetValue(AvailableNpcsProperty);
            set => SetValue(AvailableNpcsProperty, value);
        }

        public NpcSelector()
        {
            InitializeComponent();
            Loaded += NpcSelector_Loaded;
            NpcComboBox.SelectionChanged += NpcComboBox_SelectionChanged;
            NpcComboBox.LostFocus += NpcComboBox_LostFocus;
        }

        private void NpcSelector_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateNpcList();
        }

        private void NpcComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NpcComboBox.SelectedItem is NpcInfo npc)
            {
                SelectedNpcId = npc.Id;
            }
        }

        private void NpcComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // If an item is selected, preserve it
            if (NpcComboBox.SelectedItem is NpcInfo selectedNpc)
            {
                // Ensure SelectedNpcId matches the selected item
                if (SelectedNpcId != selectedNpc.Id)
                {
                    SelectedNpcId = selectedNpc.Id;
                }
                return;
            }

            // Handle manual entry when ComboBox loses focus (only if no item is selected)
            if (NpcComboBox.IsEditable && !string.IsNullOrWhiteSpace(NpcComboBox.Text))
            {
                var npc = AvailableNpcs?.FirstOrDefault(n => n.Id == NpcComboBox.Text || n.DisplayName == NpcComboBox.Text);
                if (npc != null)
                {
                    SelectedNpcId = npc.Id;
                    NpcComboBox.SelectedItem = npc;
                }
                else
                {
                    // Allow custom entry
                    SelectedNpcId = NpcComboBox.Text;
                }
            }
        }

        private static void OnSelectedNpcIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NpcSelector selector)
            {
                // Clear selection if value is null or empty
                if (e.NewValue == null || (e.NewValue is string str && string.IsNullOrWhiteSpace(str)))
                {
                    selector.NpcComboBox.SelectedItem = null;
                    if (selector.NpcComboBox.IsEditable)
                    {
                        selector.NpcComboBox.Text = string.Empty;
                    }
                    return;
                }

                // Update selection if needed
                if (selector.AvailableNpcs != null && e.NewValue is string npcId && !string.IsNullOrWhiteSpace(npcId))
                {
                    var npc = selector.AvailableNpcs.FirstOrDefault(n => n.Id == npcId);
                    if (npc != null && selector.NpcComboBox.SelectedItem != npc)
                    {
                        selector.NpcComboBox.SelectedItem = npc;
                    }
                    else if (npc == null && selector.NpcComboBox.IsEditable)
                    {
                        // Custom entry - set text
                        selector.NpcComboBox.Text = npcId;
                    }
                }
            }
        }

        private static void OnAvailableNpcsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NpcSelector selector)
            {
                selector.UpdateNpcList();
            }
        }

        private void UpdateNpcList()
        {
            NpcComboBox.ItemsSource = AvailableNpcs;
        }
    }
}

