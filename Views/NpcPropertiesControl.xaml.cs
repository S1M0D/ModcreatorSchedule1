using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Data;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.ViewModels;
using Schedule1ModdingTool.Views.Controls;

namespace Schedule1ModdingTool.Views
{
    public partial class NpcPropertiesControl : UserControl
    {
        public static readonly DependencyProperty AvailableNpcsProperty =
            DependencyProperty.Register(nameof(AvailableNpcs), typeof(ObservableCollection<NpcInfo>), typeof(NpcPropertiesControl),
                new PropertyMetadata(null));

        public ObservableCollection<NpcInfo> AvailableNpcs
        {
            get => (ObservableCollection<NpcInfo>)GetValue(AvailableNpcsProperty);
            set => SetValue(AvailableNpcsProperty, value);
        }

        public NpcPropertiesControl()
        {
            InitializeComponent();
            Loaded += NpcPropertiesControl_Loaded;
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;
        private NpcBlueprint? CurrentNpc => ViewModel?.SelectedNpc;

        private void NpcPropertiesControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Validate mutual exclusivity on load
            ValidateCustomerDealerExclusivity();
            // Initialize NPC list
            InitializeNpcList();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // Validate mutual exclusivity on load
            ValidateCustomerDealerExclusivity();
        }

        private void ValidateCustomerDealerExclusivity()
        {
            if (CurrentNpc == null)
                return;

            // If both are checked, prefer Customer over Dealer
            if (CurrentNpc.EnableCustomer && CurrentNpc.IsDealer)
            {
                CurrentNpc.IsDealer = false;
            }
        }

        private void PickAppearanceColor_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null || sender is not Button btn || btn.Tag is not string property)
                return;

            var appearance = CurrentNpc.Appearance;
            var currentHex = property switch
            {
                "SkinColor" => appearance.SkinColor,
                "LeftEyeLidColor" => appearance.LeftEyeLidColor,
                "RightEyeLidColor" => appearance.RightEyeLidColor,
                "EyeBallTint" => appearance.EyeBallTint,
                "HairColor" => appearance.HairColor,
                _ => "#FFFFFFFF"
            };

            var picked = PickColor(currentHex);
            if (picked == null)
                return;

            switch (property)
            {
                case "SkinColor":
                    appearance.SkinColor = picked;
                    break;
                case "LeftEyeLidColor":
                    appearance.LeftEyeLidColor = picked;
                    break;
                case "RightEyeLidColor":
                    appearance.RightEyeLidColor = picked;
                    break;
                case "EyeBallTint":
                    appearance.EyeBallTint = picked;
                    break;
                case "HairColor":
                    appearance.HairColor = picked;
                    break;
            }
        }

        private void PickLayerColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not NpcAppearanceLayer layer)
                return;

            var picked = PickColor(layer.ColorHex);
            if (picked != null)
            {
                layer.ColorHex = picked;
            }
        }

        private void AddFaceLayer_Click(object sender, RoutedEventArgs e)
        {
            var defaultPath = AppearancePresets.FaceLayers.Count > 0
                ? AppearancePresets.FaceLayers[0].Path
                : "Avatar/Layers/Face/Face_Neutral";

            CurrentNpc?.Appearance.FaceLayers.Add(new NpcAppearanceLayer
            {
                LayerPath = defaultPath,
                ColorHex = "#FFFFFFFF"
            });
        }

        private void AddBodyLayer_Click(object sender, RoutedEventArgs e)
        {
            var defaultPath = AppearancePresets.BodyLayers.Count > 0
                ? AppearancePresets.BodyLayers[0].Path
                : "Avatar/Layers/Top/T-Shirt";

            CurrentNpc?.Appearance.BodyLayers.Add(new NpcAppearanceLayer
            {
                LayerPath = defaultPath,
                ColorHex = "#FFFFFFFF"
            });
        }

        private void AddAccessoryLayer_Click(object sender, RoutedEventArgs e)
        {
            var defaultPath = AppearancePresets.AccessoryLayers.Count > 0
                ? AppearancePresets.AccessoryLayers[0].Path
                : "Avatar/Accessories/Feet/Sneakers/Sneakers";

            CurrentNpc?.Appearance.AccessoryLayers.Add(new NpcAppearanceLayer
            {
                LayerPath = defaultPath,
                ColorHex = "#FFFFFFFF"
            });
        }

        private void RemoveFaceLayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NpcAppearanceLayer layer)
            {
                CurrentNpc?.Appearance.FaceLayers.Remove(layer);
            }
        }

        private void RemoveBodyLayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NpcAppearanceLayer layer)
            {
                CurrentNpc?.Appearance.BodyLayers.Remove(layer);
            }
        }

        private void RemoveAccessoryLayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NpcAppearanceLayer layer)
            {
                CurrentNpc?.Appearance.AccessoryLayers.Remove(layer);
            }
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

            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
                return null;

            return $"#{dialog.Color.A:X2}{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }

        private void EnableCustomer_Checked(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc != null && CurrentNpc.IsDealer)
            {
                CurrentNpc.IsDealer = false;
            }
        }

        private void EnableCustomer_Unchecked(object sender, RoutedEventArgs e)
        {
            // Allow unchecking without restrictions
        }

        private void IsDealer_Checked(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc != null && CurrentNpc.EnableCustomer)
            {
                CurrentNpc.EnableCustomer = false;
            }
        }

        private void IsDealer_Unchecked(object sender, RoutedEventArgs e)
        {
            // Allow unchecking without restrictions
        }

        private void InitializeNpcList()
        {
            // Build NPC list from project NPCs and base game NPCs
            var npcList = new System.Collections.Generic.List<NpcInfo>();

            if (ViewModel != null)
            {
                // Add project NPCs (excluding the current NPC to avoid self-connection)
                if (ViewModel.CurrentProject?.Npcs != null)
                {
                    foreach (var npc in ViewModel.CurrentProject.Npcs)
                    {
                        // Skip the current NPC to prevent self-connection
                        if (CurrentNpc != null && npc.NpcId == CurrentNpc.NpcId)
                            continue;

                        npcList.Add(new NpcInfo
                        {
                            Id = npc.NpcId,
                            DisplayName = npc.DisplayName,
                            IsModNpc = true
                        });
                    }
                }

                // Add base game NPCs
                AddBaseGameNpcs(npcList);

                AvailableNpcs = new ObservableCollection<NpcInfo>(npcList.OrderBy(n => n.DisplayName));
                
                // Set AvailableNpcs on the NpcSelector control
                if (ConnectionNpcSelector != null)
                {
                    ConnectionNpcSelector.AvailableNpcs = AvailableNpcs;
                }
            }
        }

        private static void AddBaseGameNpcs(System.Collections.Generic.List<NpcInfo> npcList)
        {
            // Same list as in PropertiesControl - all base game NPCs
            // NPC IDs are in game format: firstname_lastname (lowercase with underscore)
            var baseNpcs = new[]
            {
                new NpcInfo { Id = ConvertDisplayNameToGameId("Anna Chesterfield"), DisplayName = "Anna Chesterfield", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Billy Kramer"), DisplayName = "Billy Kramer", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Cranky Frank"), DisplayName = "Cranky Frank", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Genghis Barn"), DisplayName = "Genghis Barn", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jane Lucero"), DisplayName = "Jane Lucero", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Javier Perez"), DisplayName = "Javier Perez", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Lisa Gardener"), DisplayName = "Lisa Gardener", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Mac Cooper"), DisplayName = "Mac Cooper", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Marco Baron"), DisplayName = "Marco Baron", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Melissa Wood"), DisplayName = "Melissa Wood", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Salvador Moreno"), DisplayName = "Salvador Moreno", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Brad Crosby"), DisplayName = "Brad Crosby", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Elizabeth Homley"), DisplayName = "Elizabeth Homley", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Eugene Buckley"), DisplayName = "Eugene Buckley", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Greg Fliggle"), DisplayName = "Greg Fliggle", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jeff Gilmore"), DisplayName = "Jeff Gilmore", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jennifer Rivera"), DisplayName = "Jennifer Rivera", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Kevin Oakley"), DisplayName = "Kevin Oakley", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Louis Fourier"), DisplayName = "Louis Fourier", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Lucy Pennington"), DisplayName = "Lucy Pennington", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Philip Wentworth"), DisplayName = "Philip Wentworth", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Randy Caulfield"), DisplayName = "Randy Caulfield", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Albert Hoover"), DisplayName = "Albert Hoover", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Austin Steiner"), DisplayName = "Austin Steiner", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Benji Coleman"), DisplayName = "Benji Coleman", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Beth Penn"), DisplayName = "Beth Penn", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Chloe Bowers"), DisplayName = "Chloe Bowers", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Donna Martin"), DisplayName = "Donna Martin", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Geraldine Poon"), DisplayName = "Geraldine Poon", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jessi Waters"), DisplayName = "Jessi Waters", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Kathy Henderson"), DisplayName = "Kathy Henderson", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Kyle Cooley"), DisplayName = "Kyle Cooley", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Ludwig Meyer"), DisplayName = "Ludwig Meyer", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Mick Lubbin"), DisplayName = "Mick Lubbin", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Ming"), DisplayName = "Ming", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Peggy Myers"), DisplayName = "Peggy Myers", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Peter File"), DisplayName = "Peter File", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Sam Thompson"), DisplayName = "Sam Thompson", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Alison Knight"), DisplayName = "Alison Knight", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Carl Bundy"), DisplayName = "Carl Bundy", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Chris Sullivan"), DisplayName = "Chris Sullivan", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Dennis Kennedy"), DisplayName = "Dennis Kennedy", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Hank Stevenson"), DisplayName = "Hank Stevenson", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Harold Colt"), DisplayName = "Harold Colt", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jackie Stevenson"), DisplayName = "Jackie Stevenson", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jack Knight"), DisplayName = "Jack Knight", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jeremy Wilkinson"), DisplayName = "Jeremy Wilkinson", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Karen Kennedy"), DisplayName = "Karen Kennedy", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Wei Long"), DisplayName = "Wei Long", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Fiona Hancock"), DisplayName = "Fiona Hancock", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Herbert Bleuball"), DisplayName = "Herbert Bleuball", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jen Heard"), DisplayName = "Jen Heard", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Leo Rivers"), DisplayName = "Leo Rivers", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Lily Turner"), DisplayName = "Lily Turner", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Michael Boog"), DisplayName = "Michael Boog", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Pearl Moore"), DisplayName = "Pearl Moore", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Ray Hoffman"), DisplayName = "Ray Hoffman", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Tobias Wentworth"), DisplayName = "Tobias Wentworth", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Walter Cussler"), DisplayName = "Walter Cussler", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Charles Rowland"), DisplayName = "Charles Rowland", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Dean Webster"), DisplayName = "Dean Webster", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Doris Lubbin"), DisplayName = "Doris Lubbin", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("George Greene"), DisplayName = "George Greene", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Jerry Montero"), DisplayName = "Jerry Montero", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Joyce Ball"), DisplayName = "Joyce Ball", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Keith Wagner"), DisplayName = "Keith Wagner", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Kim Delaney"), DisplayName = "Kim Delaney", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Meg Cooley"), DisplayName = "Meg Cooley", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Molly Presley"), DisplayName = "Molly Presley", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Shirley Watts"), DisplayName = "Shirley Watts", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Trent Sherman"), DisplayName = "Trent Sherman", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Bailey"), DisplayName = "Officer Bailey", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Bobby Cooley"), DisplayName = "Bobby Cooley", IsModNpc = false }
            };

            npcList.AddRange(baseNpcs);
        }

        private static string ConvertDisplayNameToGameId(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "";

            // Split by space and convert to lowercase
            var parts = displayName.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join("_", parts.Select(p => p.ToLowerInvariant()));
        }

        // Relationship Defaults Handlers
        private void AddConnection_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null || ConnectionNpcSelector == null || string.IsNullOrWhiteSpace(ConnectionNpcSelector.SelectedNpcId))
                return;

            var npcId = ConnectionNpcSelector.SelectedNpcId.Trim();

            if (!CurrentNpc.RelationshipDefaults.Connections.Contains(npcId))
            {
                CurrentNpc.RelationshipDefaults.Connections.Add(npcId);
                // Clear the selector
                ConnectionNpcSelector.SelectedNpcId = string.Empty;
            }
        }

        private void RemoveConnection_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null)
                return;

            if (ConnectionsListBox?.SelectedItem is string selectedConnection)
            {
                CurrentNpc.RelationshipDefaults.Connections.Remove(selectedConnection);
            }
        }

        // Schedule Action Handlers
        private void AddScheduleAction_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null)
                return;

            var newAction = new NpcScheduleAction
            {
                ActionType = ScheduleActionType.WalkTo,
                StartTime = 10 // Use time 10 (0:10 AM) instead of 0 to avoid sort comparison issues
            };

            CurrentNpc.ScheduleActions.Add(newAction);
            
            // Sort actions by time after adding
            var sorted = CurrentNpc.ScheduleActions.OrderBy(a => a.StartTime).ToList();
            CurrentNpc.ScheduleActions.Clear();
            foreach (var action in sorted)
            {
                CurrentNpc.ScheduleActions.Add(action);
            }
            
            // Update ViewModel's SelectedScheduleAction to trigger binding
            // The newAction reference is preserved since we're re-adding the same objects
            if (ViewModel != null)
            {
                ViewModel.SelectedScheduleAction = newAction;
            }
            
            // Warn if time is 0
            if (newAction.StartTime == 0)
            {
                AppUtils.ShowWarning("Warning: Scheduling actions at time 0 (midnight) can cause sort comparison issues.\n\nConsider using time 1 or later (e.g., 10 minutes = 0:10 AM).");
            }
        }

        private void RemoveScheduleAction_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null || ScheduleActionsListBox.SelectedItem == null)
                return;

            if (ScheduleActionsListBox.SelectedItem is NpcScheduleAction action)
            {
                CurrentNpc.ScheduleActions.Remove(action);
            }
        }

        // Customer Settings Handlers
        private void AddPreferredProperty_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null || PreferredPropertyComboBox.SelectedItem == null)
                return;

            if (PreferredPropertyComboBox.SelectedItem is ComboBoxItem item)
            {
                var property = item.Content.ToString();
                if (property != null && !CurrentNpc.CustomerDefaults.PreferredProperties.Contains(property))
                {
                    CurrentNpc.CustomerDefaults.PreferredProperties.Add(property);
                }
            }
        }

        private void RemovePreferredProperty_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null)
                return;

            // Find the ListBox by walking the visual tree or by name
            var listBox = FindListBoxInVisualTree("PreferredProperties");
            if (listBox?.SelectedItem is string selectedProperty)
            {
                CurrentNpc.CustomerDefaults.PreferredProperties.Remove(selectedProperty);
            }
        }

        // Inventory Defaults Handlers
        private void AddStartupItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null || string.IsNullOrWhiteSpace(StartupItemTextBox.Text))
                return;

            var itemId = StartupItemTextBox.Text.Trim();
            if (!CurrentNpc.InventoryDefaults.StartupItems.Contains(itemId))
            {
                CurrentNpc.InventoryDefaults.StartupItems.Add(itemId);
                StartupItemTextBox.Clear();
            }
        }

        private void RemoveStartupItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNpc == null)
                return;

            var listBox = FindListBoxInVisualTree("StartupItems");
            if (listBox?.SelectedItem is string selectedItem)
            {
                CurrentNpc.InventoryDefaults.StartupItems.Remove(selectedItem);
            }
        }

        // Helper method to find ListBox in visual tree
        private System.Windows.Controls.ListBox? FindListBoxInVisualTree(string partialName)
        {
            // This is a simplified approach - in a real app you might want to use VisualTreeHelper
            // For now, we'll rely on the ListBox selection being available through data context
            return null; // Placeholder - WPF binding will handle selection automatically
        }
    }
}
