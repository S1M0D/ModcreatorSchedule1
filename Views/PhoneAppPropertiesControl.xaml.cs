using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for PhoneAppPropertiesControl.xaml.
    /// </summary>
    public partial class PhoneAppPropertiesControl : UserControl
    {
        public static readonly DependencyProperty SelectedUiNodeProperty =
            DependencyProperty.Register(nameof(SelectedUiNode), typeof(PhoneAppUiNodeBlueprint), typeof(PhoneAppPropertiesControl),
                new PropertyMetadata(null));

        private MainViewModel? _subscribedViewModel;

        public PhoneAppUiNodeBlueprint? SelectedUiNode
        {
            get => (PhoneAppUiNodeBlueprint?)GetValue(SelectedUiNodeProperty);
            set => SetValue(SelectedUiNodeProperty, value);
        }

        public PhoneAppPropertiesControl()
        {
            InitializeComponent();
            Loaded += PhoneAppPropertiesControl_Loaded;
            Unloaded += PhoneAppPropertiesControl_Unloaded;
            DataContextChanged += PhoneAppPropertiesControl_DataContextChanged;
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private PhoneAppBlueprint? CurrentPhoneApp => ViewModel?.SelectedPhoneApp;

        private void PhoneAppPropertiesControl_Loaded(object sender, RoutedEventArgs e)
        {
            AttachToViewModel(ViewModel);
            SyncSelectionToCurrentPhoneApp();
        }

        private void PhoneAppPropertiesControl_Unloaded(object sender, RoutedEventArgs e)
        {
            AttachToViewModel(null);
        }

        private void PhoneAppPropertiesControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachToViewModel(e.NewValue as MainViewModel);
            SyncSelectionToCurrentPhoneApp();
        }

        private void AttachToViewModel(MainViewModel? viewModel)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            }

            _subscribedViewModel = viewModel;

            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged += ViewModelOnPropertyChanged;
            }
        }

        private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedPhoneApp))
            {
                SyncSelectionToCurrentPhoneApp();
            }
        }

        private void SyncSelectionToCurrentPhoneApp()
        {
            var app = CurrentPhoneApp;
            if (app == null)
            {
                SelectedUiNode = null;
                return;
            }

            if (SelectedUiNode != null && ContainsNode(app.UiNodes, SelectedUiNode))
            {
                return;
            }

            SelectedUiNode = app.UiNodes.FirstOrDefault();
        }

        private void UiHierarchyTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedUiNode = e.NewValue as PhoneAppUiNodeBlueprint;
        }

        private void AddRootPanel_Click(object sender, RoutedEventArgs e) => AddRootNode(PhoneAppUiNodeType.Panel);

        private void AddRootText_Click(object sender, RoutedEventArgs e) => AddRootNode(PhoneAppUiNodeType.Text);

        private void AddRootButton_Click(object sender, RoutedEventArgs e) => AddRootNode(PhoneAppUiNodeType.Button);

        private void AddRootSpacer_Click(object sender, RoutedEventArgs e) => AddRootNode(PhoneAppUiNodeType.Spacer);

        private void AddChildPanel_Click(object sender, RoutedEventArgs e) => AddChildNode(PhoneAppUiNodeType.Panel);

        private void AddChildText_Click(object sender, RoutedEventArgs e) => AddChildNode(PhoneAppUiNodeType.Text);

        private void AddChildButton_Click(object sender, RoutedEventArgs e) => AddChildNode(PhoneAppUiNodeType.Button);

        private void AddChildSpacer_Click(object sender, RoutedEventArgs e) => AddChildNode(PhoneAppUiNodeType.Spacer);

        private void DuplicateNode_Click(object sender, RoutedEventArgs e)
        {
            var app = CurrentPhoneApp;
            var node = SelectedUiNode;
            if (app == null || node == null)
                return;

            var siblings = FindContainingCollection(app.UiNodes, node);
            if (siblings == null)
                return;

            var clone = node.DeepCopy();
            var index = siblings.IndexOf(node);
            siblings.Insert(index + 1, clone);
            SelectedUiNode = clone;
            app.UseCustomUiBuilder = true;
        }

        private void RemoveNode_Click(object sender, RoutedEventArgs e)
        {
            var app = CurrentPhoneApp;
            var node = SelectedUiNode;
            if (app == null || node == null)
                return;

            var siblings = FindContainingCollection(app.UiNodes, node);
            if (siblings == null)
                return;

            var index = siblings.IndexOf(node);
            siblings.Remove(node);
            SelectedUiNode = index > 0
                ? siblings.ElementAtOrDefault(index - 1)
                : siblings.FirstOrDefault() ?? app.UiNodes.FirstOrDefault();
        }

        private void MoveNodeUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedNode(-1);
        }

        private void MoveNodeDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedNode(1);
        }

        private void PickBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUiNode == null)
                return;

            var picked = PickColor(SelectedUiNode.BackgroundColorHex);
            if (picked != null)
            {
                SelectedUiNode.BackgroundColorHex = picked;
            }
        }

        private void PickTextColor_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUiNode == null)
                return;

            var picked = PickColor(SelectedUiNode.TextColorHex);
            if (picked != null)
            {
                SelectedUiNode.TextColorHex = picked;
            }
        }

        private void AddRootNode(PhoneAppUiNodeType nodeType)
        {
            var app = CurrentPhoneApp;
            if (app == null)
                return;

            var node = CreateDefaultNode(nodeType);
            app.UiNodes.Add(node);
            app.UseCustomUiBuilder = true;
            SelectedUiNode = node;
        }

        private void AddChildNode(PhoneAppUiNodeType nodeType)
        {
            var app = CurrentPhoneApp;
            var selectedNode = SelectedUiNode;
            if (app == null || selectedNode == null || !selectedNode.SupportsChildren)
                return;

            var node = CreateDefaultNode(nodeType);
            selectedNode.Children.Add(node);
            app.UseCustomUiBuilder = true;
            SelectedUiNode = node;
        }

        private void MoveSelectedNode(int direction)
        {
            var app = CurrentPhoneApp;
            var node = SelectedUiNode;
            if (app == null || node == null)
                return;

            var siblings = FindContainingCollection(app.UiNodes, node);
            if (siblings == null)
                return;

            var currentIndex = siblings.IndexOf(node);
            var targetIndex = currentIndex + direction;
            if (currentIndex < 0 || targetIndex < 0 || targetIndex >= siblings.Count)
                return;

            siblings.Move(currentIndex, targetIndex);
            SelectedUiNode = node;
        }

        private static PhoneAppUiNodeBlueprint CreateDefaultNode(PhoneAppUiNodeType nodeType)
        {
            return nodeType switch
            {
                PhoneAppUiNodeType.Panel => new PhoneAppUiNodeBlueprint
                {
                    Name = "Container",
                    NodeType = PhoneAppUiNodeType.Panel,
                    BackgroundColorHex = "#331C2430",
                    Padding = 12,
                    Spacing = 10,
                    ExpandWidth = true
                },
                PhoneAppUiNodeType.Text => new PhoneAppUiNodeBlueprint
                {
                    Name = "Text",
                    NodeType = PhoneAppUiNodeType.Text,
                    Text = "New text",
                    TextColorHex = "#FFFFFFFF",
                    FontSize = 16,
                    ExpandWidth = true
                },
                PhoneAppUiNodeType.Button => new PhoneAppUiNodeBlueprint
                {
                    Name = "Button",
                    NodeType = PhoneAppUiNodeType.Button,
                    Text = "Press Me",
                    StatusMessage = "Button pressed.",
                    BackgroundColorHex = "#FF2E5E88",
                    TextColorHex = "#FFFFFFFF",
                    FontSize = 15,
                    PreferredHeight = 42,
                    ExpandWidth = true
                },
                _ => new PhoneAppUiNodeBlueprint
                {
                    Name = "Spacer",
                    NodeType = PhoneAppUiNodeType.Spacer,
                    PreferredHeight = 18,
                    ExpandWidth = true,
                    BackgroundColorHex = "#00000000"
                }
            };
        }

        private static ObservableCollection<PhoneAppUiNodeBlueprint>? FindContainingCollection(
            ObservableCollection<PhoneAppUiNodeBlueprint> roots,
            PhoneAppUiNodeBlueprint target)
        {
            if (roots.Contains(target))
                return roots;

            foreach (var node in roots)
            {
                var result = FindContainingCollection(node.Children, target);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static bool ContainsNode(IEnumerable<PhoneAppUiNodeBlueprint> nodes, PhoneAppUiNodeBlueprint target)
        {
            foreach (var node in nodes)
            {
                if (ReferenceEquals(node, target))
                    return true;

                if (ContainsNode(node.Children, target))
                    return true;
            }

            return false;
        }

        private static string? PickColor(string currentHex)
        {
            var (a, r, g, b) = ColorUtils.ParseHex(currentHex);
            using var dialog = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true,
                Color = Color.FromArgb(a, r, g, b)
            };

            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? $"#{dialog.Color.A:X2}{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"
                : null;
        }
    }
}
