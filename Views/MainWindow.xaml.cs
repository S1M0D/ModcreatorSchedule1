using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GridLength? _storedCodeRowHeight; // Store user's preferred height
        private bool _isStartupMode = true;

        public MainWindow(Models.QuestProject project)
        {
            try
            {
                InitializeComponent();
                var vm = new MainViewModel();
                DataContext = vm;
                
                // Set the project on the view model
                vm.CurrentProject = project;
                _isStartupMode = false; // Project is loaded, startup complete

                // Set up key bindings
                SetupKeyBindings();

                ConfigureCodeEditorTheme();

                // Subscribe to IsCodeVisible changes to fix grid divider issue
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.PropertyChanged += MainViewModel_PropertyChanged;
                }

                // Handle window closed event to shutdown app
                Closed += MainWindow_Closed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize MainWindow: {ex.Message}\n\n{ex.StackTrace}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // Clear session marker to indicate clean shutdown
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.CleanupOnClose();
            }

            // When MainWindow closes, shutdown the application
            Application.Current.Shutdown();
        }
        
        private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsCodeVisible) && sender is MainViewModel vm)
            {
                if (CodeContentRow == null) return;
                
                if (!vm.IsCodeVisible)
                {
                    // When hiding: store the current height ratio, then collapse
                    StoreCurrentHeightRatio();
                    // Set height to 0 to ensure proper collapse
                    CodeContentRow.Height = new GridLength(0);
                }
                else
                {
                    // When showing: restore stored height or default to half-and-half (1*)
                    // Use Dispatcher to ensure this happens after the binding updates
                    Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        if (CodeContentRow != null && vm.IsCodeVisible)
                        {
                            if (_storedCodeRowHeight.HasValue)
                            {
                                CodeContentRow.Height = _storedCodeRowHeight.Value;
                            }
                            else
                            {
                                // Default to half-and-half: both rows get 1*
                                CodeContentRow.Height = new GridLength(1, GridUnitType.Star);
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
        }
        
        private void StoreCurrentHeightRatio()
        {
            if (CodeContentRow == null || WorkspaceGrid == null) return;
            
            var currentHeight = CodeContentRow.Height;
            if (currentHeight.Value <= 0) return;
            
            // Get the row definitions from the workspace grid
            if (WorkspaceGrid.RowDefinitions.Count < 3) return;
            
            var topRow = WorkspaceGrid.RowDefinitions[0];
            var codeRow = WorkspaceGrid.RowDefinitions[2];
            
            // Calculate the ratio between top and code rows
            if (codeRow.Height.GridUnitType == GridUnitType.Star)
            {
                // Already in star units, store directly
                _storedCodeRowHeight = codeRow.Height;
            }
            else if (codeRow.Height.GridUnitType == GridUnitType.Pixel && topRow.Height.GridUnitType == GridUnitType.Pixel)
            {
                // Both are pixels - calculate the ratio directly
                var topPixels = topRow.Height.Value;
                var codePixels = codeRow.Height.Value;
                if (topPixels > 0)
                {
                    // Calculate star ratio: codePixels / topPixels
                    var ratio = codePixels / topPixels;
                    _storedCodeRowHeight = new GridLength(ratio, GridUnitType.Star);
                }
                else
                {
                    _storedCodeRowHeight = new GridLength(1, GridUnitType.Star);
                }
            }
            else if (codeRow.Height.GridUnitType == GridUnitType.Pixel)
            {
                // Code row is pixels, top row is stars - need to calculate star ratio based on actual rendered heights
                // Use LayoutUpdated to measure after layout completes
                WorkspaceGrid.LayoutUpdated += OnWorkspaceGridLayoutUpdated;
            }
        }
        
        private void OnWorkspaceGridLayoutUpdated(object? sender, EventArgs e)
        {
            // Unsubscribe immediately to avoid multiple calls
            if (WorkspaceGrid != null)
            {
                WorkspaceGrid.LayoutUpdated -= OnWorkspaceGridLayoutUpdated;
            }
            
            if (WorkspaceGrid == null || WorkspaceGrid.RowDefinitions.Count < 3) return;
            
            // Measure actual rendered heights
            var topRow = WorkspaceGrid.RowDefinitions[0];
            var codeRow = WorkspaceGrid.RowDefinitions[2];
            
            // Get the actual rendered height of the grid
            var gridActualHeight = WorkspaceGrid.ActualHeight;
            if (gridActualHeight <= 0) return;
            
            // Find the Grid elements in each row to measure their heights
            var topRowElement = WorkspaceGrid.Children.Cast<UIElement>()
                .FirstOrDefault(c => Grid.GetRow(c) == 0) as FrameworkElement;
            var codeRowElement = WorkspaceGrid.Children.Cast<UIElement>()
                .FirstOrDefault(c => Grid.GetRow(c) == 2) as FrameworkElement;
            
            if (topRowElement != null && codeRowElement != null)
            {
                var topActualHeight = topRowElement.ActualHeight;
                var codeActualHeight = codeRowElement.ActualHeight;
                
                if (topActualHeight > 0)
                {
                    // Calculate star ratio based on actual rendered heights
                    var ratio = codeActualHeight / topActualHeight;
                    _storedCodeRowHeight = new GridLength(ratio, GridUnitType.Star);
                }
                else
                {
                    _storedCodeRowHeight = new GridLength(1, GridUnitType.Star);
                }
            }
            else
            {
                // Fallback: if code row is pixels, estimate based on pixel value
                if (codeRow.Height.GridUnitType == GridUnitType.Pixel && topRow.Height.GridUnitType == GridUnitType.Star)
                {
                    // Estimate: assume top row takes most of the space, code row is smaller
                    // Store as 1* for half-and-half as reasonable default
                    _storedCodeRowHeight = new GridLength(1, GridUnitType.Star);
                }
            }
        }
        
        private void CodeSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // When user finishes dragging the splitter, store the new height ratio
            if (DataContext is MainViewModel vm && vm.IsCodeVisible)
            {
                StoreCurrentHeightRatio();
            }
        }

        private void ConfigureCodeEditorTheme()
        {
            if (CodeEditor == null)
            {
                return;
            }

            var defaultForeground = CreateFrozenBrush("#FFDADADA");
            CodeEditor.Foreground = defaultForeground;
            CodeEditor.Background = (System.Windows.Media.Brush)FindResource("DarkBackgroundBrush");
            CodeEditor.BorderBrush = (System.Windows.Media.Brush)FindResource("DarkBorderBrush");
            CodeEditor.LineNumbersForeground = CreateFrozenBrush("#FF5F6672");

            var selectionColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF264F78");
            var selectionBrush = new SolidColorBrush(selectionColor) { Opacity = 0.8 };
            if (selectionBrush.CanFreeze)
            {
                selectionBrush.Freeze();
            }
            CodeEditor.TextArea.SelectionBrush = selectionBrush;

            CodeEditor.TextArea.SelectionForeground = CreateFrozenBrush("#FFF7F7F7");
            CodeEditor.TextArea.TextView.LinkTextForegroundBrush = CreateFrozenBrush("#FF569CD6");

            var highlighting = HighlightingManager.Instance.GetDefinition("C#");
            if (highlighting == null)
            {
                return;
            }

            ApplyHighlightColors(highlighting);
            CodeEditor.SyntaxHighlighting = highlighting;
        }

        private static void ApplyHighlightColors(IHighlightingDefinition highlighting)
        {
            foreach (var (name, color) in s_highlightColorMap)
            {
                TrySetHighlightColor(highlighting, name, color);
            }
        }

        private static readonly IReadOnlyList<(string name, string hex)> s_highlightColorMap = new List<(string name, string hex)>
        {
            ("Default", "#FFDADADA"),
            ("Comment", "#FF6A9955"),
            ("DocumentationComment", "#FF6A9955"),
            ("DocumentationTag", "#FF4EC9B0"),
            ("DocumentationAttribute", "#FF4EC9B0"),
            ("String", "#FFCE9178"),
            ("InterpolatedStringText", "#FFCE9178"),
            ("InterpolatedStringExpression", "#FFDCDCAA"),
            ("VerbatimString", "#FFCE9178"),
            ("Char", "#FFCE9178"),
            ("StringEscape", "#FFD7BA7D"),
            ("Number", "#FFB5CEA8"),
            ("Keyword", "#FF569CD6"),
            ("C# Keyword", "#FF569CD6"),
            ("TypeKeyword", "#FF4EC9B0"),
            ("C# Type Keyword", "#FF4EC9B0"),
            ("Preprocessor", "#FFC586C0"),
            ("PreprocessorText", "#FF9CDCFE"),
            ("Type", "#FF4EC9B0"),
            ("TypeName", "#FF4EC9B0"),
            ("NamespaceName", "#FF4EC9B0"),
            ("Interface", "#FFB8D7A3"),
            ("Enum", "#FFB8D7A3"),
            ("ValueType", "#FF569CD6"),
            ("MethodCall", "#FFDCDCAA"),
            ("MethodName", "#FFDCDCAA"),
            ("FieldName", "#FF9CDCFE"),
            ("PropertyName", "#FF9CDCFE"),
            ("EventName", "#FFB8D7A3"),
            ("XmlDocTag", "#FF4EC9B0"),
            ("XmlDocAttribute", "#FF9CDCFE")
        };

        private static void TrySetHighlightColor(IHighlightingDefinition highlighting, string name, string hexColor)
        {
            if (highlighting == null || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(hexColor))
            {
                return;
            }

            var color = highlighting.NamedHighlightingColors
                .FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                       ?? highlighting.NamedHighlightingColors
                .FirstOrDefault(c => string.Equals(c.Name?.Replace(" ", string.Empty), name.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase));

            if (color != null)
            {
                var convertedColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
                color.Foreground = new SimpleHighlightingBrush(convertedColor);
            }
        }

        private static SolidColorBrush CreateFrozenBrush(string hexColor)
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }


        private void SetupKeyBindings()
        {
            // Add keyboard shortcuts
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            // F1 - New Project
            InputBindings.Add(new KeyBinding(vm.NewProjectCommand, Key.F1, ModifierKeys.None));
            
            // F2 - Open Project
            InputBindings.Add(new KeyBinding(vm.OpenProjectCommand, Key.F2, ModifierKeys.None));
            
            // Ctrl+S - Save Project
            InputBindings.Add(new KeyBinding(vm.SaveProjectCommand, Key.S, ModifierKeys.Control));
            
            // Ctrl+Shift+S - Save Project As
            InputBindings.Add(new KeyBinding(vm.SaveProjectAsCommand, Key.S, ModifierKeys.Control | ModifierKeys.Shift));
            
            // F5 - Export Project
            InputBindings.Add(new KeyBinding(vm.ExportModProjectCommand, Key.F5, ModifierKeys.None));
            
            // F6 - Build Mod
            InputBindings.Add(new KeyBinding(vm.BuildModCommand, Key.F6, ModifierKeys.None));
            
            // F7 - Build & Play
            InputBindings.Add(new KeyBinding(vm.BuildAndPlayCommand, Key.F7, ModifierKeys.None));
            
            // Ctrl+Shift+Q - New Quest
            InputBindings.Add(new KeyBinding(vm.NewQuestCommand, Key.Q, ModifierKeys.Control | ModifierKeys.Shift));
            
            // Ctrl+Shift+N - New NPC
            InputBindings.Add(new KeyBinding(vm.NewNpcCommand, Key.N, ModifierKeys.Control | ModifierKeys.Shift));
            
            // Ctrl+D - Duplicate Selected
            InputBindings.Add(new KeyBinding(vm.DuplicateSelectedCommand, Key.D, ModifierKeys.Control));
            
            // Ctrl+Z - Undo
            InputBindings.Add(new KeyBinding(vm.UndoCommand, Key.Z, ModifierKeys.Control));
            
            // Ctrl+Y - Redo
            InputBindings.Add(new KeyBinding(vm.RedoCommand, Key.Y, ModifierKeys.Control));
            
            // Delete - Delete Selected (handled via KeyDown event)
            KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete && DataContext is MainViewModel vm)
            {
                if (vm.DeleteSelectedCommand.CanExecute(null))
                {
                    vm.DeleteSelectedCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            
            // Unsubscribe from property changes
            if (vm != null)
            {
                vm.PropertyChanged -= MainViewModel_PropertyChanged;
            }
            
            // Skip save check during startup or if no project is loaded
            if (!_isStartupMode && vm != null && 
                !string.IsNullOrWhiteSpace(vm.CurrentProject.ProjectName) && 
                vm.CurrentProject.IsModified)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save them before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        vm.SaveProjectCommand.Execute(null);
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }

            base.OnClosing(e);
        }

        private void AddElementButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void AddQuestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.AvailableBlueprints.Count > 0)
            {
                vm.AddQuestCommand.Execute(vm.AvailableBlueprints[0]);
            }
        }

        private void AddNpcMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.AvailableNpcBlueprints.Count > 0)
            {
                vm.AddNpcCommand.Execute(vm.AvailableNpcBlueprints[0]);
            }
        }

        private void AddFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AddFolderCommand.Execute(null);
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OpenElementTab tab && DataContext is MainViewModel vm)
            {
                vm.CloseTab(tab);
            }
        }
    }
}