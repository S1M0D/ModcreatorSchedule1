using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.ViewModels;
using ComboBox = System.Windows.Controls.ComboBox;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for PropertiesControl.xaml
    /// </summary>
    public partial class PropertiesControl : UserControl
    {
        private ObservableCollection<TriggerMetadata> _availableTriggers;
        private ObservableCollection<NpcInfo> _availableNpcs;
        private ObservableCollection<QuestInfo> _availableQuests;
        private Dictionary<string, ObservableCollection<QuestEntryInfo>> _questEntriesCache = new Dictionary<string, ObservableCollection<QuestEntryInfo>>();

        public PropertiesControl()
        {
            InitializeComponent();
            Loaded += PropertiesControl_Loaded;
            DataContextChanged += PropertiesControl_DataContextChanged;
        }

        private void PropertiesControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTriggerData();
        }

        private void PropertiesControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainViewModel vm && vm.SelectedQuest != null)
            {
                SyncTriggerMetadata(vm.SelectedQuest);
            }
        }

        private void InitializeTriggerData()
        {
            // Load available triggers from TriggerRegistryService
            var triggers = TriggerRegistryService.GetAvailableTriggers();
            _availableTriggers = new ObservableCollection<TriggerMetadata>(triggers);

            // Build NPC list from project NPCs and base game NPCs
            var npcList = new List<NpcInfo>();

            if (DataContext is MainViewModel vm)
            {
                // Add project NPCs
                if (vm.CurrentProject?.Npcs != null)
                {
                    foreach (var npc in vm.CurrentProject.Npcs)
                    {
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

                _availableNpcs = new ObservableCollection<NpcInfo>(npcList.OrderBy(n => n.DisplayName));

                // Build Quest list from project Quests and base game Quests
                var questList = new List<QuestInfo>();
                
                // Add base game quests first
                AddBaseGameQuests(questList);
                
                // Add project quests
                if (vm.CurrentProject?.Quests != null)
                {
                    foreach (var quest in vm.CurrentProject.Quests)
                    {
                        questList.Add(new QuestInfo
                        {
                            Id = string.IsNullOrWhiteSpace(quest.QuestId) ? quest.ClassName : quest.QuestId,
                            DisplayName = quest.DisplayName,
                            IsModQuest = true
                        });
                    }
                }
                _availableQuests = new ObservableCollection<QuestInfo>(questList.OrderBy(q => q.DisplayName));

                // Sync triggers for current quest
                if (vm.SelectedQuest != null)
                {
                    SyncTriggerMetadata(vm.SelectedQuest);
                }
            }
        }

        private void SyncTriggerMetadata(QuestBlueprint quest)
        {
            if (_availableTriggers == null) return;

            foreach (var trigger in quest.QuestTriggers)
            {
                if (!string.IsNullOrWhiteSpace(trigger.TargetAction))
                {
                    // Match by both TargetAction AND TriggerType to preserve the saved trigger type
                    var metadata = _availableTriggers.FirstOrDefault(t => 
                        t.TargetAction == trigger.TargetAction && 
                        t.TriggerType == trigger.TriggerType);
                    
                    // If no exact match, try to find by TargetAction only but preserve TriggerType
                    if (metadata == null)
                    {
                        metadata = _availableTriggers.FirstOrDefault(t => t.TargetAction == trigger.TargetAction);
                        // Only set if found and TriggerType matches (to avoid overwriting)
                        if (metadata != null && metadata.TriggerType == trigger.TriggerType)
                        {
                            trigger.SelectedTriggerMetadata = metadata;
                        }
                    }
                    else
                    {
                        trigger.SelectedTriggerMetadata = metadata;
                    }
                }
            }

            foreach (var trigger in quest.QuestFinishTriggers)
            {
                if (!string.IsNullOrWhiteSpace(trigger.TargetAction))
                {
                    // Match by both TargetAction AND TriggerType to preserve the saved trigger type
                    var metadata = _availableTriggers.FirstOrDefault(t => 
                        t.TargetAction == trigger.TargetAction && 
                        t.TriggerType == trigger.TriggerType);
                    
                    // If no exact match, try to find by TargetAction only but preserve TriggerType
                    if (metadata == null)
                    {
                        metadata = _availableTriggers.FirstOrDefault(t => t.TargetAction == trigger.TargetAction);
                        // Only set if found and TriggerType matches (to avoid overwriting)
                        if (metadata != null && metadata.TriggerType == trigger.TriggerType)
                        {
                            trigger.SelectedTriggerMetadata = metadata;
                        }
                    }
                    else
                    {
                        trigger.SelectedTriggerMetadata = metadata;
                    }
                }
            }
        }

        private static string ConvertDisplayNameToGameId(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "";

            // Split by space and convert to lowercase
            var parts = displayName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0)
                return "";
            
            if (parts.Length == 1)
            {
                // Single name like "Ming" -> "ming"
                return parts[0].ToLowerInvariant();
            }
            
            // Multiple parts: join with underscore and lowercase
            // e.g., "Kyle Cooley" -> "kyle_cooley", "Officer Bailey" -> "officer_bailey"
            return string.Join("_", parts.Select(p => p.ToLowerInvariant()));
        }

        private void AddBaseGameNpcs(List<NpcInfo> npcList)
        {
            // Same list as in QuestEditViewModel - all base game NPCs
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
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Cooper"), DisplayName = "Officer Cooper", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Green"), DisplayName = "Officer Green", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Howard"), DisplayName = "Officer Howard", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Jackson"), DisplayName = "Officer Jackson", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Lee"), DisplayName = "Officer Lee", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Lopez"), DisplayName = "Officer Lopez", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Murphy"), DisplayName = "Officer Murphy", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Oakley"), DisplayName = "Officer Oakley", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Dan Samwell"), DisplayName = "Dan Samwell", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Igor Romanovich"), DisplayName = "Igor Romanovich", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Manny Oakfield"), DisplayName = "Manny Oakfield", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Oscar Holland"), DisplayName = "Oscar Holland", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Stan Carney"), DisplayName = "Stan Carney", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Uncle Nelson"), DisplayName = "Uncle Nelson", IsModNpc = false }
            };
            npcList.AddRange(baseNpcs);
        }

        private void AddBaseGameQuests(List<QuestInfo> questList)
        {
            // Base game quests from Schedule One
            // Quest IDs are the quest's StaticGUID (set in Unity inspector)
            // Display names are derived from class names (Quest_ClassName -> "Class Name")
            // Note: StaticGUIDs need to be looked up in-game or from Unity inspector
            // For now, using class-name-based identifiers that users can replace with actual StaticGUIDs
            var baseQuests = new[]
            {
                new QuestInfo { Id = "Quest_Botanists", DisplayName = "Botanists", IsModQuest = false },
                new QuestInfo { Id = "Quest_WeNeedToCook", DisplayName = "We Need To Cook", IsModQuest = false },
                new QuestInfo { Id = "Quest_WelcomeToHylandPoint", DisplayName = "Welcome To Hyland Point", IsModQuest = false },
                new QuestInfo { Id = "Quest_Warehouse", DisplayName = "Warehouse", IsModQuest = false },
                new QuestInfo { Id = "Quest_UnfavourableAgreements", DisplayName = "Unfavourable Agreements", IsModQuest = false },
                new QuestInfo { Id = "Quest_TheDeepEnd", DisplayName = "The Deep End", IsModQuest = false },
                new QuestInfo { Id = "Quest_GearingUp", DisplayName = "Gearing Up", IsModQuest = false },
                new QuestInfo { Id = "Quest_ExpandingOperations", DisplayName = "Expanding Operations", IsModQuest = false },
                new QuestInfo { Id = "Quest_DownToBusiness", DisplayName = "Down To Business", IsModQuest = false },
                new QuestInfo { Id = "Quest_DefeatCartel", DisplayName = "Defeat Cartel", IsModQuest = false },
                new QuestInfo { Id = "Quest_DealForCartel", DisplayName = "Deal For Cartel", IsModQuest = false },
                new QuestInfo { Id = "Quest_SinkOrSwim", DisplayName = "Sink Or Swim", IsModQuest = false },
                new QuestInfo { Id = "Quest_OnTheGrind", DisplayName = "On The Grind", IsModQuest = false },
                new QuestInfo { Id = "Quest_NeedingTheGreen", DisplayName = "Needing The Green", IsModQuest = false },
                new QuestInfo { Id = "Quest_SecuringSupplies", DisplayName = "Securing Supplies", IsModQuest = false },
                new QuestInfo { Id = "Quest_Packagers", DisplayName = "Packagers", IsModQuest = false },
                new QuestInfo { Id = "Quest_MovingUp", DisplayName = "Moving Up", IsModQuest = false },
                new QuestInfo { Id = "Quest_Connections", DisplayName = "Connections", IsModQuest = false },
                new QuestInfo { Id = "Quest_GettingStarted", DisplayName = "Getting Started", IsModQuest = false },
                new QuestInfo { Id = "Quest_CleanCash", DisplayName = "Clean Cash", IsModQuest = false },
                new QuestInfo { Id = "Quest_Cleaners", DisplayName = "Cleaners", IsModQuest = false },
                new QuestInfo { Id = "Quest_Chemists", DisplayName = "Chemists", IsModQuest = false },
            };
            questList.AddRange(baseQuests);
        }

        private void AddObjective_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
            {
                vm.SelectedQuest.AddObjective();
                vm.CurrentProject.MarkAsModified();
            }
        }

        private void RemoveObjective_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is QuestObjective objective)
            {
                if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
                {
                    vm.SelectedQuest.RemoveObjective(objective);
                    vm.CurrentProject.MarkAsModified();
                }
            }
        }

        private void AddQuestStartTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
            {
                var defaultTrigger = _availableTriggers?.FirstOrDefault(t => t.TargetAction == "TimeManager.OnDayPass");
                var trigger = new QuestTrigger
                {
                    TriggerType = QuestTriggerType.ActionTrigger,
                    TriggerTarget = QuestTriggerTarget.QuestStart,
                    TargetAction = defaultTrigger?.TargetAction ?? "TimeManager.OnDayPass",
                    SelectedTriggerMetadata = defaultTrigger
                };
                vm.SelectedQuest.QuestTriggers.Add(trigger);
                vm.CurrentProject.MarkAsModified();
            }
        }

        private void RemoveQuestStartTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuestTrigger trigger)
            {
                if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
                {
                    vm.SelectedQuest.QuestTriggers.Remove(trigger);
                    vm.CurrentProject.MarkAsModified();
                }
            }
        }

        private void AddQuestFinishTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
            {
                var defaultTrigger = _availableTriggers?.FirstOrDefault(t => t.TargetAction == "TimeManager.OnDayPass");
                var trigger = new QuestFinishTrigger
                {
                    TriggerType = QuestTriggerType.ActionTrigger,
                    TriggerTarget = QuestTriggerTarget.QuestFinish,
                    TargetAction = defaultTrigger?.TargetAction ?? "TimeManager.OnDayPass",
                    FinishType = QuestFinishType.Complete,
                    SelectedTriggerMetadata = defaultTrigger
                };
                vm.SelectedQuest.QuestFinishTriggers.Add(trigger);
                vm.CurrentProject.MarkAsModified();
            }
        }

        private void RemoveQuestFinishTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuestFinishTrigger trigger)
            {
                if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
                {
                    vm.SelectedQuest.QuestFinishTriggers.Remove(trigger);
                    vm.CurrentProject.MarkAsModified();
                }
            }
        }

        private void TriggerComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is QuestTrigger trigger)
            {
                UpdateTriggerComboBoxItemsSource(comboBox, trigger);
                
                // Also listen for TriggerType changes to update the dropdown
                trigger.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(QuestTrigger.TriggerType))
                    {
                        UpdateTriggerComboBoxItemsSource(comboBox, trigger);
                    }
                };
            }
        }

        private void TriggerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is QuestTrigger trigger)
            {
                if (comboBox.SelectedItem is TriggerMetadata metadata)
                {
                    trigger.SelectedTriggerMetadata = metadata;
                }
            }
        }

        private void UpdateTriggerComboBoxItemsSource(ComboBox comboBox, QuestTrigger trigger)
        {
            if (_availableTriggers == null) return;

            List<TriggerMetadata> filteredTriggers;
            
            if (trigger.TriggerType == QuestTriggerType.NPCEventTrigger)
            {
                // For NPCEventTrigger, show only NPC-related actions (NPC.*, NPCCustomer.*, NPCDealer.*, etc.)
                filteredTriggers = _availableTriggers
                    .Where(t => t.TriggerType == QuestTriggerType.NPCEventTrigger ||
                               t.TargetAction.StartsWith("NPC.", StringComparison.OrdinalIgnoreCase) ||
                               t.TargetAction.StartsWith("NPCCustomer.", StringComparison.OrdinalIgnoreCase) ||
                               t.TargetAction.StartsWith("NPCDealer.", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else if (trigger.TriggerType == QuestTriggerType.QuestEventTrigger)
            {
                // For QuestEventTrigger, show only Quest-related actions (Quest.*, QuestEntry.*)
                filteredTriggers = _availableTriggers
                    .Where(t => t.TriggerType == QuestTriggerType.QuestEventTrigger ||
                               t.TargetAction.StartsWith("Quest.", StringComparison.OrdinalIgnoreCase) ||
                               t.TargetAction.StartsWith("QuestEntry.", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                // For ActionTrigger, show only non-NPC, non-Quest triggers
                filteredTriggers = _availableTriggers
                    .Where(t => t.TriggerType == QuestTriggerType.ActionTrigger &&
                               !t.TargetAction.StartsWith("NPC.", StringComparison.OrdinalIgnoreCase) &&
                               !t.TargetAction.StartsWith("NPCCustomer.", StringComparison.OrdinalIgnoreCase) &&
                               !t.TargetAction.StartsWith("NPCDealer.", StringComparison.OrdinalIgnoreCase) &&
                               !t.TargetAction.StartsWith("Quest.", StringComparison.OrdinalIgnoreCase) &&
                               !t.TargetAction.StartsWith("QuestEntry.", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            comboBox.ItemsSource = filteredTriggers;
        }

        private void NpcComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // Ensure NPCs are initialized
                if (_availableNpcs == null)
                {
                    InitializeTriggerData();
                }
                
                if (_availableNpcs != null)
                {
                    comboBox.ItemsSource = _availableNpcs;
                    
                    // Migrate existing NPC ID if needed
                    MigrateNpcIdInTrigger(comboBox);
                }
            }
        }

        private void NpcComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // When user finishes editing, migrate the NPC ID to correct format
                MigrateNpcIdInTrigger(comboBox);
            }
        }

        private void QuestComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[QuestComboBox_Loaded] QuestComboBox Loaded event fired");
            
            if (sender is ComboBox comboBox)
            {
                System.Diagnostics.Debug.WriteLine($"[QuestComboBox_Loaded] ComboBox DataContext: {comboBox.DataContext}");
                
                if (comboBox.DataContext is QuestTrigger trigger)
                {
                    System.Diagnostics.Debug.WriteLine($"[QuestComboBox_Loaded] Trigger.TriggerType: {trigger.TriggerType}");
                    System.Diagnostics.Debug.WriteLine($"[QuestComboBox_Loaded] Trigger.TargetQuestId: {trigger.TargetQuestId}");
                }
                
                // Ensure Quests are initialized
                if (_availableQuests == null)
                {
                    System.Diagnostics.Debug.WriteLine("[QuestComboBox_Loaded] _availableQuests is null, initializing trigger data");
                    InitializeTriggerData();
                }
                
                if (_availableQuests != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[QuestComboBox_Loaded] Setting ItemsSource with {_availableQuests.Count} quests");
                    comboBox.ItemsSource = _availableQuests;
                    
                    // Migrate existing Quest ID if needed
                    MigrateQuestIdInTrigger(comboBox);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[QuestComboBox_Loaded] WARNING: _availableQuests is still null after initialization");
                }
            }
        }

        private void QuestComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // When user finishes editing, migrate the Quest ID to correct format
                MigrateQuestIdInTrigger(comboBox);
            }
        }

        private void QuestComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is QuestTrigger trigger)
            {
                // Update TargetQuestId when selection changes
                if (comboBox.SelectedItem is QuestInfo questInfo)
                {
                    trigger.TargetQuestId = questInfo.Id;
                }
                else if (comboBox.SelectedValue is string questId)
                {
                    trigger.TargetQuestId = questId;
                }

                // Update QuestEntry ComboBox when quest selection changes
                // Find the QuestEntry ComboBox in the same container
                var container = FindVisualParent<Border>(comboBox);
                if (container != null)
                {
                    var questEntryComboBox = FindVisualChild<ComboBox>(container, c => 
                        c != comboBox && c.DataContext == trigger && c.SelectedValuePath == "Index");
                    if (questEntryComboBox != null)
                    {
                        UpdateQuestEntryComboBox(trigger, questEntryComboBox);
                    }
                }
            }
        }

        private void QuestEntryComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is QuestTrigger trigger)
            {
                UpdateQuestEntryComboBox(trigger, comboBox);
            }
        }

        private void QuestEntryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is QuestTrigger trigger)
            {
                // Update TargetQuestEntryIndex when selection changes
                if (comboBox.SelectedValue is int index)
                {
                    // int.MinValue means "All Entries"
                    trigger.TargetQuestEntryIndex = index == int.MinValue ? null : index;
                }
                else if (comboBox.SelectedItem is QuestEntryInfo entryInfo)
                {
                    trigger.TargetQuestEntryIndex = entryInfo.Index == int.MinValue ? null : entryInfo.Index;
                }
            }
        }

        private void QuestEntryComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is QuestTrigger trigger)
            {
                // Validate and normalize entry index
                if (comboBox.SelectedValue is int index)
                {
                    // int.MinValue means "All Entries"
                    trigger.TargetQuestEntryIndex = index == int.MinValue ? null : index;
                }
                else if (comboBox.Text != null && int.TryParse(comboBox.Text, out int parsedIndex))
                {
                    trigger.TargetQuestEntryIndex = parsedIndex;
                }
                else if (string.IsNullOrWhiteSpace(comboBox.Text) || comboBox.Text == "All Entries")
                {
                    trigger.TargetQuestEntryIndex = null; // All entries
                }
            }
        }

        private void UpdateQuestEntryComboBox(QuestTrigger trigger, ComboBox questEntryComboBox)
        {
            if (questEntryComboBox == null)
                return;

            if (string.IsNullOrWhiteSpace(trigger.TargetQuestId))
            {
                questEntryComboBox.ItemsSource = null;
                return;
            }

            var questId = trigger.TargetQuestId.Trim();

            // Check cache first
            if (!_questEntriesCache.TryGetValue(questId, out var entries))
            {
                entries = new ObservableCollection<QuestEntryInfo>();

                // Try to find the quest in available quests
                var questInfo = _availableQuests?.FirstOrDefault(q => q.Id.Equals(questId, StringComparison.OrdinalIgnoreCase));

                if (questInfo != null && questInfo.IsModQuest)
                {
                    // For mod quests, get objectives from the project
                    if (DataContext is MainViewModel vm && vm.CurrentProject?.Quests != null)
                    {
                        var questBlueprint = vm.CurrentProject.Quests.FirstOrDefault(q =>
                            (string.IsNullOrWhiteSpace(q.QuestId) ? q.ClassName : q.QuestId).Equals(questId, StringComparison.OrdinalIgnoreCase));

                        if (questBlueprint?.Objectives != null)
                        {
                            for (int i = 0; i < questBlueprint.Objectives.Count; i++)
                            {
                                var objective = questBlueprint.Objectives[i];
                                entries.Add(new QuestEntryInfo
                                {
                                    Index = i,
                                    DisplayName = $"Entry {i}: {objective.Title}",
                                    QuestId = questId
                                });
                            }
                        }
                    }
                }

                // Always add "All Entries" option (null index) - use null instead of -1
                entries.Insert(0, new QuestEntryInfo
                {
                    Index = int.MinValue, // Use sentinel value for "all entries" display
                    DisplayName = "All Entries",
                    QuestId = questId
                });

                _questEntriesCache[questId] = entries;
            }

            questEntryComboBox.ItemsSource = entries;
            
            // Set selected value if trigger has an entry index
            if (trigger.TargetQuestEntryIndex.HasValue)
            {
                var selectedEntry = entries.FirstOrDefault(e => e.Index == trigger.TargetQuestEntryIndex.Value);
                if (selectedEntry != null)
                {
                    questEntryComboBox.SelectedValue = selectedEntry.Index;
                }
            }
            else
            {
                // Select "All Entries" (int.MinValue)
                var allEntriesOption = entries.FirstOrDefault(e => e.Index == int.MinValue);
                if (allEntriesOption != null)
                {
                    questEntryComboBox.SelectedValue = int.MinValue;
                }
            }
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (predicate == null || predicate(t)))
                {
                    return t;
                }
                var childOfChild = FindVisualChild<T>(child, predicate);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        private void MigrateQuestIdInTrigger(ComboBox comboBox)
        {
            if (_availableQuests == null || comboBox.DataContext is not QuestTrigger trigger || string.IsNullOrWhiteSpace(trigger.TargetQuestId))
                return;

            var questId = trigger.TargetQuestId.Trim();

            // Try to find matching Quest by ID (case-insensitive)
            var match = _availableQuests.FirstOrDefault(q => 
                q.Id.Equals(questId, StringComparison.OrdinalIgnoreCase));
            
            if (match != null && match.Id != questId)
            {
                // Fix case sensitivity
                trigger.TargetQuestId = match.Id;
                return;
            }

            // Try to find by display name (case-insensitive)
            match = _availableQuests.FirstOrDefault(q => 
                q.DisplayName.Equals(questId, StringComparison.OrdinalIgnoreCase));
            
            if (match != null)
            {
                trigger.TargetQuestId = match.Id;
            }
        }

        private void MigrateNpcIdInTrigger(ComboBox comboBox)
        {
            if (_availableNpcs == null || comboBox.DataContext is not QuestTrigger trigger || string.IsNullOrWhiteSpace(trigger.TargetNpcId))
                return;

            var npcId = trigger.TargetNpcId.Trim();

            // First, validate the format - if invalid, try to normalize
            if (!ValidationHelpers.IsValidNpcId(npcId))
            {
                // Try to normalize the ID
                var normalized = ValidationHelpers.NormalizeNpcId(npcId);
                
                // Try to find matching NPC by normalized ID (case-insensitive)
                var match = _availableNpcs.FirstOrDefault(n => 
                    n.Id.Equals(normalized, StringComparison.OrdinalIgnoreCase));
                
                if (match != null)
                {
                    trigger.TargetNpcId = match.Id;
                    return;
                }

                // Try to find by original ID (case-insensitive) before normalization
                match = _availableNpcs.FirstOrDefault(n => 
                    n.Id.Equals(npcId, StringComparison.OrdinalIgnoreCase));
                
                if (match != null)
                {
                    trigger.TargetNpcId = match.Id;
                    return;
                }

                // Try to find by display name without spaces (e.g., "KyleCooley" matches "Kyle Cooley")
                match = _availableNpcs.FirstOrDefault(n => 
                    n.DisplayName.Replace(" ", "").Equals(npcId, StringComparison.OrdinalIgnoreCase));
                
                if (match != null)
                {
                    trigger.TargetNpcId = match.Id;
                    return;
                }

                // If normalized ID is valid format, use it
                if (ValidationHelpers.IsValidNpcId(normalized))
                {
                    trigger.TargetNpcId = normalized;
                }
                // Otherwise, keep original but it will be invalid (user will see validation error)
            }
            else
            {
                // ID is already valid format, but check if it matches an actual NPC (case-insensitive)
                var match = _availableNpcs.FirstOrDefault(n => 
                    n.Id.Equals(npcId, StringComparison.OrdinalIgnoreCase));
                
                if (match != null && match.Id != npcId)
                {
                    // Fix case sensitivity
                    trigger.TargetNpcId = match.Id;
                }
            }
        }

        private void TriggerTypeComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // Filter out CustomTrigger - only show ActionTrigger, NPCEventTrigger, and QuestEventTrigger
                var triggerTypes = Enum.GetValues(typeof(QuestTriggerType))
                    .Cast<QuestTriggerType>()
                    .Where(t => t != QuestTriggerType.CustomTrigger)
                    .ToList();
                comboBox.ItemsSource = triggerTypes;
            }
        }

        private void TriggerTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged] SelectionChanged event fired");
            
            if (sender is ComboBox comboBox && comboBox.DataContext is QuestTrigger trigger)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged] ComboBox DataContext is QuestTrigger");
                System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged] SelectedItem: {comboBox.SelectedItem}");
                System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged] Trigger.TriggerType: {trigger.TriggerType}");
                
                // Explicitly notify that TriggerType has changed to ensure all bindings update
                trigger.OnPropertyChanged(nameof(QuestTrigger.TriggerType));
                
                // Force update visibility bindings by finding and updating all related ComboBoxes
                var parent = VisualTreeHelper.GetParent(comboBox);
                while (parent != null)
                {
                    if (parent is StackPanel stackPanel)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged] Found StackPanel with {stackPanel.Children.Count} children");
                        
                        // Find all ComboBoxes in this StackPanel to check visibility and force update bindings
                        int comboBoxIndex = 0;
                        foreach (var child in stackPanel.Children)
                        {
                            if (child is ComboBox childComboBox && childComboBox.DataContext == trigger)
                            {
                                comboBoxIndex++;
                                System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged] Found ComboBox #{comboBoxIndex}: Name={childComboBox.Name ?? "Unnamed"}, SelectedValuePath={childComboBox.SelectedValuePath}");
                                
                                // Check visibility binding for ALL ComboBoxes
                                var binding = System.Windows.Data.BindingOperations.GetBinding(childComboBox, ComboBox.VisibilityProperty);
                                if (binding != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged]   ComboBox #{comboBoxIndex} Visibility Binding - ConverterParameter: {binding.ConverterParameter}, Path: {binding.Path?.Path}");
                                    
                                    // Identify ComboBox type by ConverterParameter
                                    if (binding.ConverterParameter?.ToString() == "QuestEventTrigger")
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged]   *** This is the Quest ComboBox! ***");
                                        // Force update the visibility binding for Quest ComboBox
                                        System.Windows.Data.BindingOperations.GetBindingExpression(childComboBox, ComboBox.VisibilityProperty)?.UpdateTarget();
                                    }
                                    else if (binding.ConverterParameter?.ToString() == "NPCEventTrigger")
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged]   *** This is the NPC ComboBox! ***");
                                        // Force update the visibility binding for NPC ComboBox
                                        System.Windows.Data.BindingOperations.GetBindingExpression(childComboBox, ComboBox.VisibilityProperty)?.UpdateTarget();
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged]   ComboBox #{comboBoxIndex} has no Visibility binding");
                                }
                            }
                        }
                        
                        // Also check Labels with visibility bindings to ensure they update
                        foreach (var child in stackPanel.Children)
                        {
                            if (child is System.Windows.Controls.Label label && label.DataContext == trigger)
                            {
                                var labelBinding = System.Windows.Data.BindingOperations.GetBinding(label, System.Windows.Controls.Label.VisibilityProperty);
                                if (labelBinding != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[TriggerTypeComboBox_SelectionChanged] Found Label with Visibility Binding - ConverterParameter: {labelBinding.ConverterParameter}");
                                    System.Windows.Data.BindingOperations.GetBindingExpression(label, System.Windows.Controls.Label.VisibilityProperty)?.UpdateTarget();
                                }
                            }
                        }
                        
                        // Find the ComboBox that has Loaded="TriggerComboBox_Loaded" (the Target Action ComboBox)
                        // It should be the next ComboBox after the TriggerType ComboBox
                        bool foundTriggerType = false;
                        foreach (var child in stackPanel.Children)
                        {
                            if (child == comboBox)
                            {
                                foundTriggerType = true;
                                continue;
                            }
                            
                            if (foundTriggerType && child is ComboBox actionComboBox)
                            {
                                // This should be the Target Action ComboBox
                                UpdateTriggerComboBoxItemsSource(actionComboBox, trigger);
                                // Clear selection if current selection is no longer valid
                                if (actionComboBox.SelectedItem is TriggerMetadata selected)
                                {
                                    bool isValid = false;
                                    if (trigger.TriggerType == QuestTriggerType.NPCEventTrigger)
                                    {
                                        isValid = selected.TriggerType == QuestTriggerType.NPCEventTrigger ||
                                                 selected.TargetAction.StartsWith("NPC.", StringComparison.OrdinalIgnoreCase) ||
                                                 selected.TargetAction.StartsWith("NPCCustomer.", StringComparison.OrdinalIgnoreCase) ||
                                                 selected.TargetAction.StartsWith("NPCDealer.", StringComparison.OrdinalIgnoreCase);
                                    }
                                    else if (trigger.TriggerType == QuestTriggerType.QuestEventTrigger)
                                    {
                                        isValid = selected.TriggerType == QuestTriggerType.QuestEventTrigger ||
                                                 selected.TargetAction.StartsWith("Quest.", StringComparison.OrdinalIgnoreCase) ||
                                                 selected.TargetAction.StartsWith("QuestEntry.", StringComparison.OrdinalIgnoreCase);
                                    }
                                    else
                                    {
                                        isValid = selected.TriggerType == QuestTriggerType.ActionTrigger &&
                                                 !selected.TargetAction.StartsWith("NPC.", StringComparison.OrdinalIgnoreCase) &&
                                                 !selected.TargetAction.StartsWith("NPCCustomer.", StringComparison.OrdinalIgnoreCase) &&
                                                 !selected.TargetAction.StartsWith("NPCDealer.", StringComparison.OrdinalIgnoreCase) &&
                                                 !selected.TargetAction.StartsWith("Quest.", StringComparison.OrdinalIgnoreCase) &&
                                                 !selected.TargetAction.StartsWith("QuestEntry.", StringComparison.OrdinalIgnoreCase);
                                    }
                                    
                                    if (!isValid)
                                    {
                                        actionComboBox.SelectedItem = null;
                                        trigger.SelectedTriggerMetadata = null;
                                        trigger.TargetAction = "";
                                    }
                                }
                                return;
                            }
                        }
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
        }

        private void FinishTypeComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.ItemsSource = Enum.GetValues(typeof(QuestFinishType));
            }
        }

        private void AddObjectiveStartTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuestObjective objective)
            {
                var defaultTrigger = _availableTriggers?.FirstOrDefault(t => t.TargetAction == "TimeManager.OnDayPass");
                var trigger = new QuestObjectiveTrigger
                {
                    TriggerType = QuestTriggerType.ActionTrigger,
                    TriggerTarget = QuestTriggerTarget.ObjectiveStart,
                    TargetAction = defaultTrigger?.TargetAction ?? "TimeManager.OnDayPass",
                    ObjectiveName = objective.Name,
                    SelectedTriggerMetadata = defaultTrigger
                };
                objective.StartTriggers.Add(trigger);
                if (DataContext is MainViewModel vm)
                {
                    vm.CurrentProject.MarkAsModified();
                }
            }
        }

        private void RemoveObjectiveStartTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuestObjectiveTrigger trigger)
            {
                if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
                {
                    var objective = vm.SelectedQuest.Objectives.FirstOrDefault(obj => obj.StartTriggers.Contains(trigger));
                    if (objective != null)
                    {
                        objective.StartTriggers.Remove(trigger);
                        vm.CurrentProject.MarkAsModified();
                    }
                }
            }
        }

        private void AddObjectiveFinishTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuestObjective objective)
            {
                var defaultTrigger = _availableTriggers?.FirstOrDefault(t => t.TargetAction == "TimeManager.OnDayPass");
                var trigger = new QuestObjectiveTrigger
                {
                    TriggerType = QuestTriggerType.ActionTrigger,
                    TriggerTarget = QuestTriggerTarget.ObjectiveFinish,
                    TargetAction = defaultTrigger?.TargetAction ?? "TimeManager.OnDayPass",
                    ObjectiveName = objective.Name,
                    SelectedTriggerMetadata = defaultTrigger
                };
                objective.FinishTriggers.Add(trigger);
                if (DataContext is MainViewModel vm)
                {
                    vm.CurrentProject.MarkAsModified();
                }
            }
        }

        private void RemoveObjectiveFinishTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuestObjectiveTrigger trigger)
            {
                if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
                {
                    var objective = vm.SelectedQuest.Objectives.FirstOrDefault(obj => obj.FinishTriggers.Contains(trigger));
                    if (objective != null)
                    {
                        objective.FinishTriggers.Remove(trigger);
                        vm.CurrentProject.MarkAsModified();
                    }
                }
            }
        }

        private void AddReward_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
            {
                vm.SelectedQuest.AddReward();
                vm.CurrentProject.MarkAsModified();
            }
        }

        private void RemoveReward_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuestReward reward)
            {
                if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
                {
                    vm.SelectedQuest.RemoveReward(reward);
                    vm.CurrentProject.MarkAsModified();
                }
            }
        }

        private void RewardTypeComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.ItemsSource = Enum.GetValues(typeof(QuestRewardType));
            }
        }

        private void AddDataClassField_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
            {
                vm.SelectedQuest.AddDataClassField();
                vm.CurrentProject.MarkAsModified();
            }
        }

        private void RemoveDataClassField_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DataClassField field)
            {
                if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
                {
                    vm.SelectedQuest.RemoveDataClassField(field);
                    vm.CurrentProject.MarkAsModified();
                }
            }
        }

        private void DataClassFieldTypeComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.ItemsSource = Enum.GetValues(typeof(DataClassFieldType));
            }
        }

        private void DataClassFieldTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // When FieldType changes, the Tag binding on ValidatedTextBox will update automatically,
            // which triggers OnPropertyChanged and revalidates. No additional action needed.
            if (DataContext is MainViewModel vm)
            {
                vm.CurrentProject.MarkAsModified();
            }
        }
    }
}