using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Services.CodeGeneration.Orchestration;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    public class QuestEditViewModel : ObservableObject
    {
        private readonly MainViewModel _parentViewModel;
        private QuestBlueprint _originalQuest;
        private QuestBlueprint _quest;
        private string _generatedCode;
        private string _windowTitle;

        public event EventHandler<bool> CloseRequested;

        public QuestEditViewModel(QuestBlueprint quest, MainViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
            _originalQuest = quest ?? throw new ArgumentNullException(nameof(quest));
            
            // Create a deep copy of the quest for editing (to allow cancellation)
            _quest = quest.DeepCopy();
            
            WindowTitle = $"Edit Quest: {_quest.DisplayName}";
            
            // Initialize trigger and NPC data
            InitializeTriggerData();
            
            // Initialize commands
            AddObjectiveCommand = new RelayCommand(AddObjective);
            RemoveObjectiveCommand = new RelayCommand<QuestObjective>(RemoveObjective);
            AddQuestStartTriggerCommand = new RelayCommand(AddQuestStartTrigger);
            RemoveQuestStartTriggerCommand = new RelayCommand<QuestTrigger>(RemoveQuestStartTrigger);
            AddQuestFinishTriggerCommand = new RelayCommand(AddQuestFinishTrigger);
            RemoveQuestFinishTriggerCommand = new RelayCommand<QuestFinishTrigger>(RemoveQuestFinishTrigger);
            RegenerateCodeCommand = new RelayCommand(RegenerateCode);
            CopyCodeCommand = new RelayCommand(CopyCode);
            ApplyChangesCommand = new RelayCommand(ApplyChanges);
            CancelCommand = new RelayCommand(Cancel);
            
            // Generate initial code
            RegenerateCode();
            
            // Listen for property changes to auto-regenerate code
            _quest.PropertyChanged += (s, e) => RegenerateCode();
        }

        public QuestBlueprint Quest
        {
            get => _quest;
            set => SetProperty(ref _quest, value);
        }

        public string GeneratedCode
        {
            get => _generatedCode;
            set => SetProperty(ref _generatedCode, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public ICommand AddObjectiveCommand { get; }
        public ICommand RemoveObjectiveCommand { get; }
        public ICommand AddQuestStartTriggerCommand { get; }
        public ICommand RemoveQuestStartTriggerCommand { get; }
        public ICommand AddQuestFinishTriggerCommand { get; }
        public ICommand RemoveQuestFinishTriggerCommand { get; }
        public ICommand RegenerateCodeCommand { get; }
        public ICommand CopyCodeCommand { get; }
        public ICommand ApplyChangesCommand { get; }
        public ICommand CancelCommand { get; }

        public ObservableCollection<QuestTrigger> QuestStartTriggers => _quest.QuestTriggers;
        public ObservableCollection<QuestFinishTrigger> QuestFinishTriggers => _quest.QuestFinishTriggers;

        private ObservableCollection<TriggerMetadata> _availableTriggers;
        public ObservableCollection<TriggerMetadata> AvailableTriggers
        {
            get => _availableTriggers;
            private set => SetProperty(ref _availableTriggers, value);
        }

        private ObservableCollection<NpcInfo> _availableNpcs;
        public ObservableCollection<NpcInfo> AvailableNpcs
        {
            get => _availableNpcs;
            private set => SetProperty(ref _availableNpcs, value);
        }

        public List<string> AvailableNpcIds => AvailableNpcs?.Select(n => n.Id).ToList() ?? new List<string>();

        public ObservableCollection<TriggerMetadata> GetFilteredTriggers(QuestTriggerType triggerType)
        {
            if (AvailableTriggers == null)
                return new ObservableCollection<TriggerMetadata>();

            return new ObservableCollection<TriggerMetadata>(
                AvailableTriggers.Where(t => t.TriggerType == triggerType));
        }

        private void AddObjective()
        {
            try
            {
                _quest.AddObjective();
                RegenerateCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add objective: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveObjective(QuestObjective objective)
        {
            if (objective == null) return;

            try
            {
                _quest.RemoveObjective(objective);
                RegenerateCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to remove objective: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegenerateCode()
        {
            try
            {
                var codeService = new CodeGenerationOrchestrator();
                GeneratedCode = codeService.GenerateQuestCode(_quest);
            }
            catch (Exception ex)
            {
                GeneratedCode = $"// Error generating code:\n// {ex.Message}";
            }
        }

        private void CopyCode()
        {
            try
            {
                if (!string.IsNullOrEmpty(GeneratedCode))
                {
                    Clipboard.SetText(GeneratedCode);
                    MessageBox.Show("Code copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy code: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyChanges()
        {
            try
            {
                // Validate the quest data
                if (string.IsNullOrWhiteSpace(_quest.ClassName))
                {
                    MessageBox.Show("Class Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_quest.QuestId))
                {
                    MessageBox.Show("Quest ID is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_quest.QuestTitle))
                {
                    MessageBox.Show("Quest Title is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Apply changes back to the original quest
                _originalQuest.CopyFrom(_quest);
                
                // Mark the project as modified
                _parentViewModel.CurrentProject.MarkAsModified();
                
                // Regenerate the main project code
                _parentViewModel.RegenerateCodeCommand.Execute(null);
                
                // Close the window with success result
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to apply changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            // Close the window without applying changes
            CloseRequested?.Invoke(this, false);
        }

        private void AddQuestStartTrigger()
        {
            try
            {
                var defaultTrigger = AvailableTriggers?.FirstOrDefault(t => t.TargetAction == "TimeManager.OnDayPass");
                var trigger = new QuestTrigger
                {
                    TriggerType = QuestTriggerType.ActionTrigger,
                    TriggerTarget = QuestTriggerTarget.QuestStart,
                    TargetAction = defaultTrigger?.TargetAction ?? "TimeManager.OnDayPass",
                    SelectedTriggerMetadata = defaultTrigger
                };
                _quest.QuestTriggers.Add(trigger);
                RegenerateCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add quest start trigger: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveQuestStartTrigger(QuestTrigger trigger)
        {
            if (trigger == null) return;

            try
            {
                _quest.QuestTriggers.Remove(trigger);
                RegenerateCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to remove quest start trigger: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddQuestFinishTrigger()
        {
            try
            {
                var defaultTrigger = AvailableTriggers?.FirstOrDefault(t => t.TargetAction == "TimeManager.OnDayPass");
                var trigger = new QuestFinishTrigger
                {
                    TriggerType = QuestTriggerType.ActionTrigger,
                    TriggerTarget = QuestTriggerTarget.QuestFinish,
                    TargetAction = defaultTrigger?.TargetAction ?? "TimeManager.OnDayPass",
                    FinishType = QuestFinishType.Complete,
                    SelectedTriggerMetadata = defaultTrigger
                };
                _quest.QuestFinishTriggers.Add(trigger);
                RegenerateCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add quest finish trigger: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveQuestFinishTrigger(QuestFinishTrigger trigger)
        {
            if (trigger == null) return;

            try
            {
                _quest.QuestFinishTriggers.Remove(trigger);
                RegenerateCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to remove quest finish trigger: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeTriggerData()
        {
            // Load available triggers from TriggerRegistryService
            var triggers = TriggerRegistryService.GetAvailableTriggers();
            AvailableTriggers = new ObservableCollection<TriggerMetadata>(triggers);

            // Build NPC list from project NPCs and base game NPCs
            var npcList = new List<NpcInfo>();

            // Add project NPCs
            if (_parentViewModel.CurrentProject?.Npcs != null)
            {
                foreach (var npc in _parentViewModel.CurrentProject.Npcs)
                {
                    npcList.Add(new NpcInfo
                    {
                        Id = npc.NpcId,
                        DisplayName = npc.DisplayName,
                        IsModNpc = true
                    });
                }
            }

            // Add base game NPCs from S1API.Entities.NPCs namespace
            AddBaseGameNpcs(npcList);

            AvailableNpcs = new ObservableCollection<NpcInfo>(npcList.OrderBy(n => n.DisplayName));

            // Migrate old NPC IDs to new format before syncing
            MigrateNpcIdsInTriggers();

            // Sync SelectedTriggerMetadata for existing triggers
            SyncTriggerMetadata();
        }

        /// <summary>
        /// Migrates old PascalCase NPC IDs to new game format for all triggers in the quest.
        /// </summary>
        private void MigrateNpcIdsInTriggers()
        {
            // Migrate quest start triggers
            foreach (var trigger in _quest.QuestTriggers)
            {
                if (!string.IsNullOrWhiteSpace(trigger.TargetNpcId))
                {
                    var migratedId = MigrateNpcIdToGameFormat(trigger.TargetNpcId);
                    if (migratedId != trigger.TargetNpcId)
                    {
                        trigger.TargetNpcId = migratedId;
                    }
                }
            }

            // Migrate quest finish triggers
            foreach (var trigger in _quest.QuestFinishTriggers)
            {
                if (!string.IsNullOrWhiteSpace(trigger.TargetNpcId))
                {
                    var migratedId = MigrateNpcIdToGameFormat(trigger.TargetNpcId);
                    if (migratedId != trigger.TargetNpcId)
                    {
                        trigger.TargetNpcId = migratedId;
                    }
                }
            }

            // Migrate objective triggers
            if (_quest.Objectives != null)
            {
                foreach (var objective in _quest.Objectives)
                {
                    if (objective.StartTriggers != null)
                    {
                        foreach (var trigger in objective.StartTriggers)
                        {
                            if (!string.IsNullOrWhiteSpace(trigger.TargetNpcId))
                            {
                                var migratedId = MigrateNpcIdToGameFormat(trigger.TargetNpcId);
                                if (migratedId != trigger.TargetNpcId)
                                {
                                    trigger.TargetNpcId = migratedId;
                                }
                            }
                        }
                    }

                    if (objective.FinishTriggers != null)
                    {
                        foreach (var trigger in objective.FinishTriggers)
                        {
                            if (!string.IsNullOrWhiteSpace(trigger.TargetNpcId))
                            {
                                var migratedId = MigrateNpcIdToGameFormat(trigger.TargetNpcId);
                                if (migratedId != trigger.TargetNpcId)
                                {
                                    trigger.TargetNpcId = migratedId;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SyncTriggerMetadata()
        {
            if (AvailableTriggers == null) return;

            foreach (var trigger in _quest.QuestTriggers)
            {
                SyncSingleTriggerMetadata(trigger);
            }

            foreach (var trigger in _quest.QuestFinishTriggers)
            {
                SyncSingleTriggerMetadata(trigger);
            }

            // Sync objective-level triggers
            if (_quest.Objectives != null)
            {
                foreach (var objective in _quest.Objectives)
                {
                    if (objective.StartTriggers != null)
                    {
                        foreach (var trigger in objective.StartTriggers)
                        {
                            SyncSingleTriggerMetadata(trigger);
                        }
                    }

                    if (objective.FinishTriggers != null)
                    {
                        foreach (var trigger in objective.FinishTriggers)
                        {
                            SyncSingleTriggerMetadata(trigger);
                        }
                    }
                }
            }
        }

        private void SyncSingleTriggerMetadata(QuestTrigger trigger)
        {
            if (AvailableTriggers == null || string.IsNullOrWhiteSpace(trigger.TargetAction))
                return;

            // Match by both TargetAction AND TriggerType to preserve the saved trigger type
            var metadata = AvailableTriggers.FirstOrDefault(t => 
                t.TargetAction == trigger.TargetAction && 
                t.TriggerType == trigger.TriggerType);
            
            // If no exact match, try to find by TargetAction only but preserve TriggerType
            if (metadata == null)
            {
                metadata = AvailableTriggers.FirstOrDefault(t => t.TargetAction == trigger.TargetAction);
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

        /// <summary>
        /// Converts a display name (e.g., "Kyle Cooley") to game ID format (e.g., "kyle_cooley").
        /// Handles single names, two-part names, and special cases like "Officer Bailey" -> "officer_bailey".
        /// </summary>
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

        /// <summary>
        /// Migrates old PascalCase NPC IDs (e.g., "KyleCooley") to new game format (e.g., "kyle_cooley").
        /// Also handles already-correct format IDs.
        /// </summary>
        private string MigrateNpcIdToGameFormat(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return npcId;

            // If already in correct format (contains underscore and lowercase), return as-is
            if (npcId.Contains("_") && npcId == npcId.ToLowerInvariant())
                return npcId;

            // First, try to find exact match in AvailableNpcs (case-insensitive)
            if (AvailableNpcs != null)
            {
                var exactMatch = AvailableNpcs.FirstOrDefault(n => 
                    n.Id.Equals(npcId, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null)
                {
                    return exactMatch.Id; // Return the correct format from the list
                }
            }

            // Try to find matching NPC by display name conversion
            // Convert PascalCase to display name format, then to game ID
            // e.g., "KyleCooley" -> try to find "Kyle Cooley" -> "kyle_cooley"
            
            // Simple heuristic: split PascalCase into words
            // "KyleCooley" -> ["Kyle", "Cooley"] -> "Kyle Cooley" -> "kyle_cooley"
            var words = new System.Text.StringBuilder();
            for (int i = 0; i < npcId.Length; i++)
            {
                if (i > 0 && char.IsUpper(npcId[i]))
                {
                    words.Append(' ');
                }
                words.Append(npcId[i]);
            }
            
            var displayName = words.ToString().Trim();
            var convertedId = ConvertDisplayNameToGameId(displayName);

            // Try to find match in AvailableNpcs by the converted ID
            if (AvailableNpcs != null)
            {
                var match = AvailableNpcs.FirstOrDefault(n => 
                    n.Id.Equals(convertedId, StringComparison.OrdinalIgnoreCase) ||
                    n.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match.Id; // Return the correct format from the list
                }
            }

            // Fallback: return the converted ID
            return convertedId;
        }

        private void AddBaseGameNpcs(List<NpcInfo> npcList)
        {
            // Add known base game NPCs from S1API.Entities.NPCs namespace
            // These are the NPCs available in the base game
            // NPC IDs are in game format: firstname_lastname (lowercase with underscore)
            var baseNpcs = new[]
            {
                // Docks NPCs
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
                
                // Downtown NPCs
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
                
                // Northtown NPCs
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
                
                // Suburbia NPCs
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
                
                // Uptown NPCs
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
                
                // Westville NPCs
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
                
                // Police Officers
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Bailey"), DisplayName = "Officer Bailey", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Cooper"), DisplayName = "Officer Cooper", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Green"), DisplayName = "Officer Green", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Howard"), DisplayName = "Officer Howard", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Jackson"), DisplayName = "Officer Jackson", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Lee"), DisplayName = "Officer Lee", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Lopez"), DisplayName = "Officer Lopez", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Murphy"), DisplayName = "Officer Murphy", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Officer Oakley"), DisplayName = "Officer Oakley", IsModNpc = false },
                
                // Other NPCs
                new NpcInfo { Id = ConvertDisplayNameToGameId("Dan Samwell"), DisplayName = "Dan Samwell", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Igor Romanovich"), DisplayName = "Igor Romanovich", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Manny Oakfield"), DisplayName = "Manny Oakfield", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Oscar Holland"), DisplayName = "Oscar Holland", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Stan Carney"), DisplayName = "Stan Carney", IsModNpc = false },
                new NpcInfo { Id = ConvertDisplayNameToGameId("Uncle Nelson"), DisplayName = "Uncle Nelson", IsModNpc = false }
            };

            npcList.AddRange(baseNpcs);
        }

        public void PopulateTriggerFromMetadata(QuestTrigger trigger, TriggerMetadata metadata)
        {
            if (trigger == null || metadata == null)
                return;

            trigger.TriggerType = metadata.TriggerType;
            trigger.TargetAction = metadata.TargetAction;
        }

        public string GetTriggerDescription(QuestTrigger trigger)
        {
            if (trigger == null || string.IsNullOrWhiteSpace(trigger.TargetAction))
                return "";

            var metadata = AvailableTriggers?.FirstOrDefault(t => t.TargetAction == trigger.TargetAction);
            return metadata?.Description ?? "";
        }
    }

    /// <summary>
    /// Information about an NPC for selection in the UI
    /// </summary>
    public class NpcInfo
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsModNpc { get; set; }

        public override string ToString()
        {
            var source = IsModNpc ? "Mod" : "Base Game";
            return $"{DisplayName} ({Id}) - {source}";
        }
    }

    /// <summary>
    /// Information about a quest for UI display
    /// </summary>
        public class QuestInfo
        {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public bool IsModQuest { get; set; }

            public override string ToString()
            {
                var source = IsModQuest ? "Mod" : "Base Game";
                return $"{DisplayName} ({Id}) - {source}";
            }
        }

        public class QuestEntryInfo
        {
            public int Index { get; set; }
            public string DisplayName { get; set; } = "";
            public string QuestId { get; set; } = "";

            public override string ToString()
            {
                return $"Entry {Index}: {DisplayName}";
            }
        }
}