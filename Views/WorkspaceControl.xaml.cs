using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using DataObject = System.Windows.DataObject;
using Color = System.Windows.Media.Color;

namespace Schedule1ModdingTool.Views
{
    public partial class WorkspaceControl : UserControl
    {
        private readonly DispatcherTimer _doubleClickTimer;
        private object? _lastClickedItem;
        private Point _dragStartPoint;
        private bool _isDragging;

        public WorkspaceControl()
        {
            InitializeComponent();
            _doubleClickTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _doubleClickTimer.Tick += (_, _) =>
            {
                _doubleClickTimer.Stop();
                _lastClickedItem = null;
            };
        }

        private MainViewModel? GetMainViewModel() => Window.GetWindow(this)?.DataContext as MainViewModel;

        private void NewQuest_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetMainViewModel();
            if (vm != null && vm.AvailableBlueprints.Count > 0)
            {
                vm.AddQuestCommand.Execute(vm.AvailableBlueprints[0]);
            }
        }

        private void NewNpc_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetMainViewModel();
            if (vm != null && vm.AvailableNpcBlueprints.Count > 0)
            {
                vm.AddNpcCommand.Execute(vm.AvailableNpcBlueprints[0]);
            }
        }

        private void NewItem_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetMainViewModel();
            if (vm != null && vm.AvailableItemBlueprints.Count > 0)
            {
                vm.AddItemCommand.Execute(vm.AvailableItemBlueprints[0]);
            }
        }

        private void NewCustomClothing_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetMainViewModel();
            if (vm != null)
            {
                vm.NewCustomClothingCommand.Execute(null);
            }
        }

        private void NewPhoneCall_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetMainViewModel();
            if (vm != null && vm.AvailablePhoneCallBlueprints.Count > 0)
            {
                vm.AddPhoneCallCommand.Execute(vm.AvailablePhoneCallBlueprints[0]);
            }
        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.AddFolderCommand.Execute(null);
        }

        private void FolderTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ModFolder folder)
                return;

            HandleTileInteraction(folder, () =>
            {
                var vm = GetMainViewModel();
                vm?.WorkspaceViewModel.NavigateToFolder(folder);
            });
        }

        private void QuestTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not QuestBlueprint quest)
                return;

            _dragStartPoint = e.GetPosition(null);
            HandleTileInteraction(quest, () =>
            {
                var vm = GetMainViewModel();
                vm?.OpenQuestInTab(quest);
            });
        }

        private void NpcTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not NpcBlueprint npc)
                return;

            _dragStartPoint = e.GetPosition(null);
            HandleTileInteraction(npc, () =>
            {
                GetMainViewModel()?.OpenNpcInTab(npc);
            });
        }

        private void ItemTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ItemBlueprint item)
                return;

            _dragStartPoint = e.GetPosition(null);
            HandleTileInteraction(item, () =>
            {
                GetMainViewModel()?.OpenItemInTab(item);
            });
        }

        private void PhoneAppTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not PhoneAppBlueprint phoneApp)
                return;

            _dragStartPoint = e.GetPosition(null);
            HandleTileInteraction(phoneApp, () =>
            {
                GetMainViewModel()?.OpenPhoneAppInTab(phoneApp);
            });
        }

        private void PhoneCallTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not PhoneCallBlueprint phoneCall)
                return;

            _dragStartPoint = e.GetPosition(null);
            HandleTileInteraction(phoneCall, () =>
            {
                GetMainViewModel()?.OpenPhoneCallInTab(phoneCall);
            });
        }

        private void BreadcrumbButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ModFolder folder)
            {
                GetMainViewModel()?.WorkspaceViewModel.NavigateToFolder(folder);
            }
        }

        private void NavigateUp_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.WorkspaceViewModel.GoToParentFolder();
        }

        private void HandleTileInteraction(object item, Action onDoubleClick)
        {
            if (_isDragging)
                return;

            var vm = GetMainViewModel();
            if (vm == null)
                return;

            switch (item)
            {
                case QuestBlueprint quest:
                    vm.SelectedQuest = quest;
                    break;
                case NpcBlueprint npc:
                    vm.SelectedNpc = npc;
                    break;
                case ItemBlueprint itemBlueprint:
                    vm.SelectedItemBlueprint = itemBlueprint;
                    break;
                case PhoneCallBlueprint phoneCall:
                    vm.SelectedPhoneCall = phoneCall;
                    break;
                case PhoneAppBlueprint phoneApp:
                    vm.SelectedPhoneApp = phoneApp;
                    break;
            }

            if (_lastClickedItem == item && _doubleClickTimer.IsEnabled)
            {
                _doubleClickTimer.Stop();
                _lastClickedItem = null;
                onDoubleClick?.Invoke();
            }
            else
            {
                _lastClickedItem = item;
                _doubleClickTimer.Start();
            }
        }

        private void QuestTile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is QuestBlueprint quest)
            {
                var vm = GetMainViewModel();
                if (vm != null)
                {
                    vm.SelectedQuest = quest;
                }
            }
        }

        private void NpcTile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is NpcBlueprint npc)
            {
                var vm = GetMainViewModel();
                if (vm != null)
                {
                    vm.SelectedNpc = npc;
                }
            }
        }

        private void FolderTile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Context menu will handle selection
            e.Handled = false;
        }

        private void ItemTile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ItemBlueprint item)
            {
                var vm = GetMainViewModel();
                if (vm != null)
                {
                    vm.SelectedItemBlueprint = item;
                }
            }
        }

        private void PhoneAppTile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PhoneAppBlueprint phoneApp)
            {
                var vm = GetMainViewModel();
                if (vm != null)
                {
                    vm.SelectedPhoneApp = phoneApp;
                }
            }
        }

        private void PhoneCallTile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PhoneCallBlueprint phoneCall)
            {
                var vm = GetMainViewModel();
                if (vm != null)
                {
                    vm.SelectedPhoneCall = phoneCall;
                }
            }
        }

        private T? GetDataContextFromMenuItem<T>(MenuItem menuItem) where T : class
        {
            // Try to get from menu item's DataContext first
            if (menuItem.DataContext is T item)
                return item;

            // Try to get from parent ContextMenu's PlacementTarget
            var contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu?.PlacementTarget is FrameworkElement target)
            {
                if (target.DataContext is T targetItem)
                    return targetItem;
            }

            return null;
        }

        private void OpenQuest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var quest = GetDataContextFromMenuItem<QuestBlueprint>(menuItem);
                if (quest != null)
                {
                    GetMainViewModel()?.OpenQuestInTab(quest);
                }
            }
        }

        private void OpenNpc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var npc = GetDataContextFromMenuItem<NpcBlueprint>(menuItem);
                if (npc != null)
                {
                    GetMainViewModel()?.OpenNpcInTab(npc);
                }
            }
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var item = GetDataContextFromMenuItem<ItemBlueprint>(menuItem);
                if (item != null)
                {
                    GetMainViewModel()?.OpenItemInTab(item);
                }
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var folder = GetDataContextFromMenuItem<ModFolder>(menuItem);
                if (folder != null)
                {
                    GetMainViewModel()?.WorkspaceViewModel.NavigateToFolder(folder);
                }
            }
        }

        private void DuplicateQuest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var quest = GetDataContextFromMenuItem<QuestBlueprint>(menuItem);
                if (quest != null)
                {
                    GetMainViewModel()?.DuplicateQuestCommand.Execute(quest);
                }
            }
        }

        private void DuplicateNpc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var npc = GetDataContextFromMenuItem<NpcBlueprint>(menuItem);
                if (npc != null)
                {
                    GetMainViewModel()?.DuplicateNpcCommand.Execute(npc);
                }
            }
        }

        private void OpenPhoneApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var phoneApp = GetDataContextFromMenuItem<PhoneAppBlueprint>(menuItem);
                if (phoneApp != null)
                {
                    GetMainViewModel()?.OpenPhoneAppInTab(phoneApp);
                }
            }
        }

        private void OpenPhoneCall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var phoneCall = GetDataContextFromMenuItem<PhoneCallBlueprint>(menuItem);
                if (phoneCall != null)
                {
                    GetMainViewModel()?.OpenPhoneCallInTab(phoneCall);
                }
            }
        }

        private void DuplicateItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var item = GetDataContextFromMenuItem<ItemBlueprint>(menuItem);
                if (item != null)
                {
                    GetMainViewModel()?.DuplicateItemCommand.Execute(item);
                }
            }
        }

        private void DuplicateFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var folder = GetDataContextFromMenuItem<ModFolder>(menuItem);
                if (folder != null)
                {
                    GetMainViewModel()?.DuplicateFolderCommand.Execute(folder);
                }
            }
        }

        private void DeleteQuest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var quest = GetDataContextFromMenuItem<QuestBlueprint>(menuItem);
                if (quest != null)
                {
                    var vm = GetMainViewModel();
                    if (vm != null)
                    {
                        vm.SelectedQuest = quest;
                        vm.RemoveQuestCommand.Execute(null);
                    }
                }
            }
        }

        private void DeleteNpc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var npc = GetDataContextFromMenuItem<NpcBlueprint>(menuItem);
                if (npc != null)
                {
                    var vm = GetMainViewModel();
                    if (vm != null)
                    {
                        vm.SelectedNpc = npc;
                        vm.RemoveNpcCommand.Execute(null);
                    }
                }
            }
        }

        private void DuplicatePhoneApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var phoneApp = GetDataContextFromMenuItem<PhoneAppBlueprint>(menuItem);
                if (phoneApp != null)
                {
                    GetMainViewModel()?.DuplicatePhoneAppCommand.Execute(phoneApp);
                }
            }
        }

        private void DuplicatePhoneCall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var phoneCall = GetDataContextFromMenuItem<PhoneCallBlueprint>(menuItem);
                if (phoneCall != null)
                {
                    GetMainViewModel()?.DuplicatePhoneCallCommand.Execute(phoneCall);
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var item = GetDataContextFromMenuItem<ItemBlueprint>(menuItem);
                if (item != null)
                {
                    var vm = GetMainViewModel();
                    if (vm != null)
                    {
                        vm.SelectedItemBlueprint = item;
                        vm.RemoveItemCommand.Execute(null);
                    }
                }
            }
        }

        private void DeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var folder = GetDataContextFromMenuItem<ModFolder>(menuItem);
                if (folder != null)
                {
                    GetMainViewModel()?.DeleteFolderCommand.Execute(folder);
                }
            }
        }

        private void RenameFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var folder = GetDataContextFromMenuItem<ModFolder>(menuItem);
                if (folder != null)
                {
                    var newName = ShowInputDialog("Rename Folder", "Enter new folder name:", folder.Name);
                    if (!string.IsNullOrWhiteSpace(newName) && newName != folder.Name)
                    {
                        folder.Name = newName;
                    }
                }
            }
        }

        private void RenameQuest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var quest = GetDataContextFromMenuItem<QuestBlueprint>(menuItem);
                if (quest != null)
                {
                    var newName = ShowInputDialog("Rename Quest", "Enter new quest title:", quest.QuestTitle);
                    if (!string.IsNullOrWhiteSpace(newName) && newName != quest.QuestTitle)
                    {
                        quest.QuestTitle = newName;
                    }
                }
            }
        }

        private void RenameNpc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var npc = GetDataContextFromMenuItem<NpcBlueprint>(menuItem);
                if (npc != null)
                {
                    var currentName = $"{npc.FirstName} {npc.LastName}";
                    var newName = ShowInputDialog("Rename NPC", "Enter new NPC name:", currentName);
                    if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
                    {
                        var parts = newName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        npc.FirstName = parts.Length > 0 ? parts[0] : newName;
                        npc.LastName = parts.Length > 1 ? parts[1] : "";
                    }
                }
            }
        }

        private void DeletePhoneApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var phoneApp = GetDataContextFromMenuItem<PhoneAppBlueprint>(menuItem);
                if (phoneApp != null)
                {
                    var vm = GetMainViewModel();
                    if (vm != null)
                    {
                        vm.SelectedPhoneApp = phoneApp;
                        vm.RemovePhoneAppCommand.Execute(null);
                    }
                }
            }
        }

        private void DeletePhoneCall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var phoneCall = GetDataContextFromMenuItem<PhoneCallBlueprint>(menuItem);
                if (phoneCall != null)
                {
                    var vm = GetMainViewModel();
                    if (vm != null)
                    {
                        vm.SelectedPhoneCall = phoneCall;
                        vm.RemovePhoneCallCommand.Execute(null);
                    }
                }
            }
        }

        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var item = GetDataContextFromMenuItem<ItemBlueprint>(menuItem);
                if (item != null)
                {
                    var newName = ShowInputDialog("Rename Item", "Enter new item name:", item.ItemName);
                    if (!string.IsNullOrWhiteSpace(newName) && newName != item.ItemName)
                    {
                        item.ItemName = newName;
                    }
                }
            }
        }

        private void RenamePhoneApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var phoneApp = GetDataContextFromMenuItem<PhoneAppBlueprint>(menuItem);
                if (phoneApp != null)
                {
                    var newName = ShowInputDialog("Rename Phone App", "Enter new app title:", phoneApp.AppTitle);
                    if (!string.IsNullOrWhiteSpace(newName) && newName != phoneApp.AppTitle)
                    {
                        phoneApp.AppTitle = newName;
                    }
                }
            }
        }

        private void RenamePhoneCall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var phoneCall = GetDataContextFromMenuItem<PhoneCallBlueprint>(menuItem);
                if (phoneCall != null)
                {
                    var newName = ShowInputDialog("Rename Phone Call", "Enter new phone call title:", phoneCall.CallTitle);
                    if (!string.IsNullOrWhiteSpace(newName) && newName != phoneCall.CallTitle)
                    {
                        phoneCall.CallTitle = newName;
                    }
                }
            }
        }

        private string? ShowInputDialog(string title, string prompt, string defaultValue)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Background = Application.Current.TryFindResource("DarkBackgroundBrush") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.White
            };

            var grid = new Grid
            {
                Margin = new Thickness(16)
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var promptText = new TextBlock
            {
                Text = prompt,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = Application.Current.TryFindResource("LightTextBrush") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Black
            };
            Grid.SetRow(promptText, 0);

            var inputBox = new System.Windows.Controls.TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 0, 0, 16),
                VerticalAlignment = VerticalAlignment.Top,
                Style = Application.Current.TryFindResource("DarkTextBoxStyle") as Style
            };
            Grid.SetRow(inputBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true,
                Style = Application.Current.TryFindResource("IconButtonStyle") as Style
            };
            okButton.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true,
                Style = Application.Current.TryFindResource("IconButtonStyle") as Style
            };
            cancelButton.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(promptText);
            grid.Children.Add(inputBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            inputBox.Focus();
            inputBox.SelectAll();

            return dialog.ShowDialog() == true ? inputBox.Text : null;
        }

        private void QuestTile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                if (!_isDragging)
                {
                    var position = e.GetPosition(null);
                    if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        if (element.DataContext is QuestBlueprint quest)
                        {
                            _isDragging = true;
                            _doubleClickTimer.Stop();
                            _lastClickedItem = null;
                            var dataObject = new DataObject("QuestBlueprint", quest);
                            DragDrop.DoDragDrop(element, dataObject, DragDropEffects.Move);
                            _isDragging = false;
                        }
                    }
                }
            }
        }

        private void NpcTile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                if (!_isDragging)
                {
                    var position = e.GetPosition(null);
                    if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        if (element.DataContext is NpcBlueprint npc)
                        {
                            _isDragging = true;
                            _doubleClickTimer.Stop();
                            _lastClickedItem = null;
                            var dataObject = new DataObject("NpcBlueprint", npc);
                            DragDrop.DoDragDrop(element, dataObject, DragDropEffects.Move);
                            _isDragging = false;
                        }
                    }
                }
            }
        }

        private void ItemTile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                if (!_isDragging)
                {
                    var position = e.GetPosition(null);
                    if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        if (element.DataContext is ItemBlueprint item)
                        {
                            _isDragging = true;
                            _doubleClickTimer.Stop();
                            _lastClickedItem = null;
                            var dataObject = new DataObject("ItemBlueprint", item);
                            DragDrop.DoDragDrop(element, dataObject, DragDropEffects.Move);
                            _isDragging = false;
                        }
                    }
                }
            }
        }

        private void PhoneAppTile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                if (!_isDragging)
                {
                    var position = e.GetPosition(null);
                    if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        if (element.DataContext is PhoneAppBlueprint phoneApp)
                        {
                            _isDragging = true;
                            _doubleClickTimer.Stop();
                            _lastClickedItem = null;
                            var dataObject = new DataObject("PhoneAppBlueprint", phoneApp);
                            DragDrop.DoDragDrop(element, dataObject, DragDropEffects.Move);
                            _isDragging = false;
                        }
                    }
                }
            }
        }

        private void PhoneCallTile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                if (!_isDragging)
                {
                    var position = e.GetPosition(null);
                    if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        if (element.DataContext is PhoneCallBlueprint phoneCall)
                        {
                            _isDragging = true;
                            _doubleClickTimer.Stop();
                            _lastClickedItem = null;
                            var dataObject = new DataObject("PhoneCallBlueprint", phoneCall);
                            DragDrop.DoDragDrop(element, dataObject, DragDropEffects.Move);
                            _isDragging = false;
                        }
                    }
                }
            }
        }

        private void FolderTile_DragOver(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ModFolder folder)
            {
                if (e.Data.GetDataPresent("QuestBlueprint") || e.Data.GetDataPresent("NpcBlueprint") || e.Data.GetDataPresent("ItemBlueprint") || e.Data.GetDataPresent("PhoneCallBlueprint") || e.Data.GetDataPresent("PhoneAppBlueprint"))
                {
                    e.Effects = DragDropEffects.Move;
                    e.Handled = true;
                }
            }
        }

        private void FolderTile_Drop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ModFolder folder)
            {
                var vm = GetMainViewModel();
                if (vm == null)
                    return;

                if (e.Data.GetData("QuestBlueprint") is QuestBlueprint quest)
                {
                    quest.FolderId = folder.Id;
                    e.Handled = true;
                }
                else if (e.Data.GetData("NpcBlueprint") is NpcBlueprint npc)
                {
                    npc.FolderId = folder.Id;
                    e.Handled = true;
                }
                else if (e.Data.GetData("ItemBlueprint") is ItemBlueprint item)
                {
                    item.FolderId = folder.Id;
                    e.Handled = true;
                }
                else if (e.Data.GetData("PhoneCallBlueprint") is PhoneCallBlueprint phoneCall)
                {
                    phoneCall.FolderId = folder.Id;
                    e.Handled = true;
                }
                else if (e.Data.GetData("PhoneAppBlueprint") is PhoneAppBlueprint phoneApp)
                {
                    phoneApp.FolderId = folder.Id;
                    e.Handled = true;
                }
            }
        }

        private void FolderTile_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                if (e.Data.GetDataPresent("QuestBlueprint") || e.Data.GetDataPresent("NpcBlueprint") || e.Data.GetDataPresent("ItemBlueprint") || e.Data.GetDataPresent("PhoneCallBlueprint") || e.Data.GetDataPresent("PhoneAppBlueprint"))
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(200, 100, 150, 255));
                    border.BorderThickness = new Thickness(2);
                }
            }
        }

        private void FolderTile_DragLeave(object sender, EventArgs e)
        {
            if (sender is Border border)
            {
                // Reset to style defaults
                border.ClearValue(Border.BorderBrushProperty);
                border.ClearValue(Border.BorderThicknessProperty);
            }
        }

        private void UpButton_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("QuestBlueprint") || e.Data.GetDataPresent("NpcBlueprint") || e.Data.GetDataPresent("ItemBlueprint") || e.Data.GetDataPresent("PhoneCallBlueprint") || e.Data.GetDataPresent("PhoneAppBlueprint"))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void UpButton_Drop(object sender, DragEventArgs e)
        {
            var vm = GetMainViewModel();
            if (vm?.WorkspaceViewModel?.SelectedFolder == null)
                return;

            var parentId = vm.WorkspaceViewModel.SelectedFolder.ParentId;
            if (string.IsNullOrWhiteSpace(parentId))
                parentId = QuestProject.RootFolderId;

            if (e.Data.GetData("QuestBlueprint") is QuestBlueprint quest)
            {
                quest.FolderId = parentId;
                e.Handled = true;
            }
            else if (e.Data.GetData("NpcBlueprint") is NpcBlueprint npc)
            {
                npc.FolderId = parentId;
                e.Handled = true;
            }
            else if (e.Data.GetData("ItemBlueprint") is ItemBlueprint item)
            {
                item.FolderId = parentId;
                e.Handled = true;
            }
            else if (e.Data.GetData("PhoneCallBlueprint") is PhoneCallBlueprint phoneCall)
            {
                phoneCall.FolderId = parentId;
                e.Handled = true;
            }
            else if (e.Data.GetData("PhoneAppBlueprint") is PhoneAppBlueprint phoneApp)
            {
                phoneApp.FolderId = parentId;
                e.Handled = true;
            }
        }

        private void UpButton_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Button button)
            {
                if (e.Data.GetDataPresent("QuestBlueprint") || e.Data.GetDataPresent("NpcBlueprint") || e.Data.GetDataPresent("ItemBlueprint") || e.Data.GetDataPresent("PhoneCallBlueprint") || e.Data.GetDataPresent("PhoneAppBlueprint"))
                {
                    button.Background = new SolidColorBrush(Color.FromArgb(100, 100, 150, 255));
                }
            }
        }

        private void UpButton_DragLeave(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.Background = null;
            }
        }

        private void BreadcrumbButton_DragOver(object sender, DragEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ModFolder folder)
            {
                if (e.Data.GetDataPresent("QuestBlueprint") || e.Data.GetDataPresent("NpcBlueprint") || e.Data.GetDataPresent("ItemBlueprint") || e.Data.GetDataPresent("PhoneCallBlueprint") || e.Data.GetDataPresent("PhoneAppBlueprint"))
                {
                    e.Effects = DragDropEffects.Move;
                    e.Handled = true;
                }
            }
        }

        private void BreadcrumbButton_Drop(object sender, DragEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ModFolder folder)
            {
                if (e.Data.GetData("QuestBlueprint") is QuestBlueprint quest)
                {
                    quest.FolderId = folder.Id;
                    e.Handled = true;
                }
                else if (e.Data.GetData("NpcBlueprint") is NpcBlueprint npc)
                {
                    npc.FolderId = folder.Id;
                    e.Handled = true;
                }
                else if (e.Data.GetData("ItemBlueprint") is ItemBlueprint item)
                {
                    item.FolderId = folder.Id;
                    e.Handled = true;
                }
                else if (e.Data.GetData("PhoneCallBlueprint") is PhoneCallBlueprint phoneCall)
                {
                    phoneCall.FolderId = folder.Id;
                    e.Handled = true;
                }
                else if (e.Data.GetData("PhoneAppBlueprint") is PhoneAppBlueprint phoneApp)
                {
                    phoneApp.FolderId = folder.Id;
                    e.Handled = true;
                }
            }
        }

        private void BreadcrumbButton_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Button button)
            {
                if (e.Data.GetDataPresent("QuestBlueprint") || e.Data.GetDataPresent("NpcBlueprint") || e.Data.GetDataPresent("ItemBlueprint") || e.Data.GetDataPresent("PhoneCallBlueprint") || e.Data.GetDataPresent("PhoneAppBlueprint"))
                {
                    button.Background = new SolidColorBrush(Color.FromArgb(100, 100, 150, 255));
                }
            }
        }

        private void BreadcrumbButton_DragLeave(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.Background = null;
            }
        }
    }
}
