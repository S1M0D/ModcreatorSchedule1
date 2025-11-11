using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a project containing multiple quest blueprints
    /// </summary>
    public class QuestProject : ObservableObject
    {
        public const string RootFolderId = "root";

        private readonly HashSet<QuestBlueprint> _trackedQuests = new HashSet<QuestBlueprint>();
        private readonly HashSet<QuestObjective> _trackedObjectives = new HashSet<QuestObjective>();
        private readonly HashSet<QuestTrigger> _trackedTriggers = new HashSet<QuestTrigger>();
        private readonly HashSet<NpcBlueprint> _trackedNpcs = new HashSet<NpcBlueprint>();
        private readonly HashSet<ResourceAsset> _trackedResources = new HashSet<ResourceAsset>();
        private readonly HashSet<ModFolder> _trackedFolders = new HashSet<ModFolder>();
        private bool _suppressNotifications;

        private string _projectName = "";
        private string _projectDescription = "";
        private string _filePath = "";
        private bool _isModified = false;

        [JsonProperty("projectName")]
        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (SetProperty(ref _projectName, value))
                {
                    MarkAsModified();
                }
            }
        }

        [JsonProperty("projectDescription")]
        public string ProjectDescription
        {
            get => _projectDescription;
            set
            {
                if (SetProperty(ref _projectDescription, value))
                {
                    MarkAsModified();
                }
            }
        }

        [JsonIgnore]
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        [JsonIgnore]
        public bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }

        [JsonProperty("quests")]
        public ObservableCollection<QuestBlueprint> Quests { get; } = new ObservableCollection<QuestBlueprint>();

        [JsonProperty("npcs")]
        public ObservableCollection<NpcBlueprint> Npcs { get; } = new ObservableCollection<NpcBlueprint>();

        [JsonProperty("resources")]
        public ObservableCollection<ResourceAsset> Resources { get; } = new ObservableCollection<ResourceAsset>();

        [JsonProperty("folders")]
        public ObservableCollection<ModFolder> Folders { get; } = new ObservableCollection<ModFolder>();

        [JsonIgnore]
        public string DisplayTitle
        {
            get
            {
                var title = string.IsNullOrEmpty(ProjectName) ? "Untitled Project" : ProjectName;
                return IsModified ? $"{title}*" : title;
            }
        }

        public QuestProject()
        {
            _suppressNotifications = true;
            ProjectName = "New Quest Project";
            ProjectDescription = "A new quest modding project for Schedule 1";
            Quests.CollectionChanged += OnQuestsCollectionChanged;
            Npcs.CollectionChanged += OnNpcsCollectionChanged;
            Folders.CollectionChanged += OnFoldersCollectionChanged;
            Resources.CollectionChanged += OnResourcesCollectionChanged;
            EnsureRootFolder();
            _suppressNotifications = false;
        }

        public void AddQuest(QuestBlueprint quest)
        {
            Quests.Add(quest);
        }

        public void RemoveQuest(QuestBlueprint quest)
        {
            Quests.Remove(quest);
        }

        public void AddNpc(NpcBlueprint npc)
        {
            Npcs.Add(npc);
        }

        public void RemoveNpc(NpcBlueprint npc)
        {
            Npcs.Remove(npc);
        }

        public void AddResource(ResourceAsset asset)
        {
            Resources.Add(asset);
        }

        public void RemoveResource(ResourceAsset asset)
        {
            Resources.Remove(asset);
        }

        public ModFolder CreateFolder(string name, string? parentId = null)
        {
            var folder = new ModFolder
            {
                Name = string.IsNullOrWhiteSpace(name) ? "New Folder" : name,
                ParentId = string.IsNullOrWhiteSpace(parentId) ? RootFolderId : parentId
            };
            Folders.Add(folder);
            return folder;
        }

        public ModFolder? GetFolderById(string? folderId)
        {
            if (string.IsNullOrWhiteSpace(folderId))
                return null;
            return Folders.FirstOrDefault(f => f.Id == folderId);
        }

        public void MarkAsModified()
        {
            if (!_suppressNotifications)
            {
                IsModified = true;
            }

            OnPropertyChanged(nameof(DisplayTitle));
        }

        public void MarkAsSaved()
        {
            IsModified = false;
            OnPropertyChanged(nameof(DisplayTitle));
        }

        public void SaveToFile(string filePath)
        {
            FilePath = filePath;
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
            MarkAsSaved();
        }

        public static QuestProject? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            var project = JsonConvert.DeserializeObject<QuestProject>(json);
            if (project != null)
            {
                project.FilePath = filePath;
                project.AttachExistingQuestHandlers();
                project.AttachExistingNpcHandlers();
                project.AttachExistingFolderHandlers();
                project.AttachExistingResourceHandlers();
                project.EnsureRootFolder();
                project.MarkAsSaved();
                project._suppressNotifications = false;
            }
            return project;
        }

        private void OnQuestsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is QuestBlueprint quest)
                    {
                        AttachQuestHandlers(quest);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is QuestBlueprint quest)
                    {
                        DetachQuestHandlers(quest);
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetQuestTracking();
                MarkAsModified();
                return;
            }

            MarkAsModified();
            OnPropertyChanged(nameof(Quests));
        }

        private void AttachQuestHandlers(QuestBlueprint quest)
        {
            if (_trackedQuests.Contains(quest))
                return;

            _trackedQuests.Add(quest);
            quest.PropertyChanged += QuestOnPropertyChanged;
            quest.Objectives.CollectionChanged += OnObjectivesCollectionChanged;
            foreach (var objective in quest.Objectives)
            {
                AttachObjectiveHandlers(objective);
            }
            
            // Attach handlers to trigger collections
            quest.QuestTriggers.CollectionChanged += OnQuestTriggersCollectionChanged;
            foreach (var trigger in quest.QuestTriggers)
            {
                AttachTriggerHandlers(trigger);
            }
            
            quest.QuestFinishTriggers.CollectionChanged += OnQuestFinishTriggersCollectionChanged;
            foreach (var finishTrigger in quest.QuestFinishTriggers)
            {
                AttachTriggerHandlers(finishTrigger);
            }

            // Attach handlers to StartCondition
            if (quest.StartCondition != null)
            {
                quest.StartCondition.PropertyChanged += QuestOnPropertyChanged;
            }
        }

        private void AttachNpcHandlers(NpcBlueprint npc)
        {
            if (_trackedNpcs.Contains(npc))
                return;

            _trackedNpcs.Add(npc);
            npc.PropertyChanged += NpcOnPropertyChanged;

            // Attach handlers to ScheduleActions collection
            npc.ScheduleActions.CollectionChanged += OnScheduleActionsCollectionChanged;
            foreach (var action in npc.ScheduleActions)
            {
                AttachScheduleActionHandlers(action);
            }

            // Attach handlers to nested objects
            if (npc.CustomerDefaults != null)
            {
                npc.CustomerDefaults.PropertyChanged += NpcOnPropertyChanged;
                npc.CustomerDefaults.DrugAffinities.CollectionChanged += OnNpcCollectionChanged;
                npc.CustomerDefaults.PreferredProperties.CollectionChanged += OnNpcCollectionChanged;
            }

            if (npc.DealerDefaults != null)
            {
                npc.DealerDefaults.PropertyChanged += NpcOnPropertyChanged;
            }

            if (npc.RelationshipDefaults != null)
            {
                npc.RelationshipDefaults.PropertyChanged += NpcOnPropertyChanged;
                npc.RelationshipDefaults.Connections.CollectionChanged += OnNpcCollectionChanged;
            }

            if (npc.InventoryDefaults != null)
            {
                npc.InventoryDefaults.PropertyChanged += NpcOnPropertyChanged;
                npc.InventoryDefaults.StartupItems.CollectionChanged += OnNpcCollectionChanged;
            }

            if (npc.Appearance != null)
            {
                npc.Appearance.PropertyChanged += NpcOnPropertyChanged;
                npc.Appearance.FaceLayers.CollectionChanged += OnNpcCollectionChanged;
                npc.Appearance.BodyLayers.CollectionChanged += OnNpcCollectionChanged;
                npc.Appearance.AccessoryLayers.CollectionChanged += OnNpcCollectionChanged;
            }
        }

        private void DetachQuestHandlers(QuestBlueprint quest)
        {
            if (!_trackedQuests.Remove(quest))
                return;

            quest.PropertyChanged -= QuestOnPropertyChanged;
            quest.Objectives.CollectionChanged -= OnObjectivesCollectionChanged;
            foreach (var objective in quest.Objectives)
            {
                DetachObjectiveHandlers(objective);
            }
            
            // Detach handlers from trigger collections
            quest.QuestTriggers.CollectionChanged -= OnQuestTriggersCollectionChanged;
            foreach (var trigger in quest.QuestTriggers)
            {
                DetachTriggerHandlers(trigger);
            }
            
            quest.QuestFinishTriggers.CollectionChanged -= OnQuestFinishTriggersCollectionChanged;
            foreach (var finishTrigger in quest.QuestFinishTriggers)
            {
                DetachTriggerHandlers(finishTrigger);
            }

            // Detach handlers from StartCondition
            if (quest.StartCondition != null)
            {
                quest.StartCondition.PropertyChanged -= QuestOnPropertyChanged;
            }
        }

        private void DetachNpcHandlers(NpcBlueprint npc)
        {
            if (!_trackedNpcs.Remove(npc))
                return;

            npc.PropertyChanged -= NpcOnPropertyChanged;

            // Detach handlers from ScheduleActions collection
            npc.ScheduleActions.CollectionChanged -= OnScheduleActionsCollectionChanged;
            foreach (var action in npc.ScheduleActions)
            {
                DetachScheduleActionHandlers(action);
            }

            // Detach handlers from nested objects
            if (npc.CustomerDefaults != null)
            {
                npc.CustomerDefaults.PropertyChanged -= NpcOnPropertyChanged;
                npc.CustomerDefaults.DrugAffinities.CollectionChanged -= OnNpcCollectionChanged;
                npc.CustomerDefaults.PreferredProperties.CollectionChanged -= OnNpcCollectionChanged;
            }

            if (npc.DealerDefaults != null)
            {
                npc.DealerDefaults.PropertyChanged -= NpcOnPropertyChanged;
            }

            if (npc.RelationshipDefaults != null)
            {
                npc.RelationshipDefaults.PropertyChanged -= NpcOnPropertyChanged;
                npc.RelationshipDefaults.Connections.CollectionChanged -= OnNpcCollectionChanged;
            }

            if (npc.InventoryDefaults != null)
            {
                npc.InventoryDefaults.PropertyChanged -= NpcOnPropertyChanged;
                npc.InventoryDefaults.StartupItems.CollectionChanged -= OnNpcCollectionChanged;
            }

            if (npc.Appearance != null)
            {
                npc.Appearance.PropertyChanged -= NpcOnPropertyChanged;
                npc.Appearance.FaceLayers.CollectionChanged -= OnNpcCollectionChanged;
                npc.Appearance.BodyLayers.CollectionChanged -= OnNpcCollectionChanged;
                npc.Appearance.AccessoryLayers.CollectionChanged -= OnNpcCollectionChanged;
            }
        }

        private void QuestOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkAsModified();
            OnPropertyChanged(nameof(Quests));

            // If StartCondition changed, re-attach handlers
            if (sender is QuestBlueprint quest && e.PropertyName == nameof(QuestBlueprint.StartCondition))
            {
                if (quest.StartCondition != null)
                {
                    quest.StartCondition.PropertyChanged -= QuestOnPropertyChanged;
                }
                if (quest.StartCondition != null)
                {
                    quest.StartCondition.PropertyChanged += QuestOnPropertyChanged;
                }
            }
        }

        private void OnFoldersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ModFolder folder)
                    {
                        AttachFolderHandlers(folder);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is ModFolder folder)
                    {
                        DetachFolderHandlers(folder);
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var folder in _trackedFolders.ToArray())
                {
                    folder.PropertyChanged -= FolderOnPropertyChanged;
                }
                _trackedFolders.Clear();
                AttachExistingFolderHandlers();
            }

            EnsureRootFolder();
            MarkAsModified();
            OnPropertyChanged(nameof(Folders));
        }

        private void NpcOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkAsModified();

            // If a nested object property changed on the NPC itself, re-attach handlers to the new object
            if (sender is NpcBlueprint npc)
            {
                switch (e.PropertyName)
                {
                    case nameof(NpcBlueprint.CustomerDefaults):
                        // Detach old handlers
                        if (npc.CustomerDefaults != null)
                        {
                            npc.CustomerDefaults.PropertyChanged -= NpcOnPropertyChanged;
                            npc.CustomerDefaults.DrugAffinities.CollectionChanged -= OnNpcCollectionChanged;
                            npc.CustomerDefaults.PreferredProperties.CollectionChanged -= OnNpcCollectionChanged;
                        }
                        // Attach new handlers
                        if (npc.CustomerDefaults != null)
                        {
                            npc.CustomerDefaults.PropertyChanged += NpcOnPropertyChanged;
                            npc.CustomerDefaults.DrugAffinities.CollectionChanged += OnNpcCollectionChanged;
                            npc.CustomerDefaults.PreferredProperties.CollectionChanged += OnNpcCollectionChanged;
                        }
                        break;

                    case nameof(NpcBlueprint.DealerDefaults):
                        if (npc.DealerDefaults != null)
                        {
                            npc.DealerDefaults.PropertyChanged -= NpcOnPropertyChanged;
                        }
                        if (npc.DealerDefaults != null)
                        {
                            npc.DealerDefaults.PropertyChanged += NpcOnPropertyChanged;
                        }
                        break;

                    case nameof(NpcBlueprint.RelationshipDefaults):
                        if (npc.RelationshipDefaults != null)
                        {
                            npc.RelationshipDefaults.PropertyChanged -= NpcOnPropertyChanged;
                            npc.RelationshipDefaults.Connections.CollectionChanged -= OnNpcCollectionChanged;
                        }
                        if (npc.RelationshipDefaults != null)
                        {
                            npc.RelationshipDefaults.PropertyChanged += NpcOnPropertyChanged;
                            npc.RelationshipDefaults.Connections.CollectionChanged += OnNpcCollectionChanged;
                        }
                        break;

                    case nameof(NpcBlueprint.InventoryDefaults):
                        if (npc.InventoryDefaults != null)
                        {
                            npc.InventoryDefaults.PropertyChanged -= NpcOnPropertyChanged;
                            npc.InventoryDefaults.StartupItems.CollectionChanged -= OnNpcCollectionChanged;
                        }
                        if (npc.InventoryDefaults != null)
                        {
                            npc.InventoryDefaults.PropertyChanged += NpcOnPropertyChanged;
                            npc.InventoryDefaults.StartupItems.CollectionChanged += OnNpcCollectionChanged;
                        }
                        break;

                    case nameof(NpcBlueprint.Appearance):
                        if (npc.Appearance != null)
                        {
                            npc.Appearance.PropertyChanged -= NpcOnPropertyChanged;
                            npc.Appearance.FaceLayers.CollectionChanged -= OnNpcCollectionChanged;
                            npc.Appearance.BodyLayers.CollectionChanged -= OnNpcCollectionChanged;
                            npc.Appearance.AccessoryLayers.CollectionChanged -= OnNpcCollectionChanged;
                        }
                        if (npc.Appearance != null)
                        {
                            npc.Appearance.PropertyChanged += NpcOnPropertyChanged;
                            npc.Appearance.FaceLayers.CollectionChanged += OnNpcCollectionChanged;
                            npc.Appearance.BodyLayers.CollectionChanged += OnNpcCollectionChanged;
                            npc.Appearance.AccessoryLayers.CollectionChanged += OnNpcCollectionChanged;
                        }
                        break;
                }
            }
            // If sender is a nested object (CustomerDefaults, etc.), just mark as modified
            // The MarkAsModified() call above handles this case
        }

        private void OnObjectivesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RebuildObjectiveTracking();
                MarkAsModified();
                return;
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is QuestObjective objective)
                    {
                        AttachObjectiveHandlers(objective);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is QuestObjective objective)
                    {
                        DetachObjectiveHandlers(objective);
                    }
                }
            }

            MarkAsModified();
        }

        private void AttachObjectiveHandlers(QuestObjective objective)
        {
            if (_trackedObjectives.Contains(objective))
                return;

            _trackedObjectives.Add(objective);
            objective.PropertyChanged += ObjectiveOnPropertyChanged;

            // Attach handlers to trigger collections
            objective.StartTriggers.CollectionChanged += OnObjectiveTriggersCollectionChanged;
            foreach (var trigger in objective.StartTriggers)
            {
                AttachTriggerHandlers(trigger);
            }

            objective.FinishTriggers.CollectionChanged += OnObjectiveTriggersCollectionChanged;
            foreach (var trigger in objective.FinishTriggers)
            {
                AttachTriggerHandlers(trigger);
            }
        }

        private void DetachObjectiveHandlers(QuestObjective objective)
        {
            if (!_trackedObjectives.Remove(objective))
                return;

            objective.PropertyChanged -= ObjectiveOnPropertyChanged;

            // Detach handlers from trigger collections
            objective.StartTriggers.CollectionChanged -= OnObjectiveTriggersCollectionChanged;
            foreach (var trigger in objective.StartTriggers)
            {
                DetachTriggerHandlers(trigger);
            }

            objective.FinishTriggers.CollectionChanged -= OnObjectiveTriggersCollectionChanged;
            foreach (var trigger in objective.FinishTriggers)
            {
                DetachTriggerHandlers(trigger);
            }
        }

        private void ObjectiveOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkAsModified();
        }

        private void OnQuestTriggersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                MarkAsModified();
                return;
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is QuestTrigger trigger)
                    {
                        AttachTriggerHandlers(trigger);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is QuestTrigger trigger)
                    {
                        DetachTriggerHandlers(trigger);
                    }
                }
            }

            MarkAsModified();
        }

        private void OnQuestFinishTriggersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                MarkAsModified();
                return;
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is QuestTrigger trigger)
                    {
                        AttachTriggerHandlers(trigger);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is QuestTrigger trigger)
                    {
                        DetachTriggerHandlers(trigger);
                    }
                }
            }

            MarkAsModified();
        }

        private void AttachTriggerHandlers(QuestTrigger trigger)
        {
            if (_trackedTriggers.Contains(trigger))
                return;

            _trackedTriggers.Add(trigger);
            trigger.PropertyChanged += TriggerOnPropertyChanged;
        }

        private void DetachTriggerHandlers(QuestTrigger trigger)
        {
            if (!_trackedTriggers.Remove(trigger))
                return;

            trigger.PropertyChanged -= TriggerOnPropertyChanged;
        }

        private void OnNpcCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            MarkAsModified();
        }

        private void TriggerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkAsModified();
        }

        private void OnScheduleActionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                MarkAsModified();
                return;
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is NpcScheduleAction action)
                    {
                        AttachScheduleActionHandlers(action);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is NpcScheduleAction action)
                    {
                        DetachScheduleActionHandlers(action);
                    }
                }
            }

            MarkAsModified();
        }

        private void AttachScheduleActionHandlers(NpcScheduleAction action)
        {
            action.PropertyChanged += NpcOnPropertyChanged;
        }

        private void DetachScheduleActionHandlers(NpcScheduleAction action)
        {
            action.PropertyChanged -= NpcOnPropertyChanged;
        }

        private void OnObjectiveTriggersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                MarkAsModified();
                return;
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is QuestTrigger trigger)
                    {
                        AttachTriggerHandlers(trigger);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is QuestTrigger trigger)
                    {
                        DetachTriggerHandlers(trigger);
                    }
                }
            }

            MarkAsModified();
        }

        internal void AttachExistingQuestHandlers()
        {
            foreach (var quest in Quests)
            {
                AttachQuestHandlers(quest);
            }
        }

        internal void AttachExistingNpcHandlers()
        {
            foreach (var npc in Npcs)
            {
                AttachNpcHandlers(npc);
            }
        }

        internal void AttachExistingFolderHandlers()
        {
            foreach (var folder in Folders)
            {
                AttachFolderHandlers(folder);
            }
        }

        internal void AttachExistingResourceHandlers()
        {
            foreach (var asset in Resources)
            {
                AttachResourceHandlers(asset);
            }
        }

        private void ResetQuestTracking()
        {
            foreach (var quest in _trackedQuests.ToArray())
            {
                quest.PropertyChanged -= QuestOnPropertyChanged;
                quest.Objectives.CollectionChanged -= OnObjectivesCollectionChanged;
                quest.QuestTriggers.CollectionChanged -= OnQuestTriggersCollectionChanged;
                quest.QuestFinishTriggers.CollectionChanged -= OnQuestFinishTriggersCollectionChanged;
                if (quest.StartCondition != null)
                {
                    quest.StartCondition.PropertyChanged -= QuestOnPropertyChanged;
                }
            }

            foreach (var objective in _trackedObjectives.ToArray())
            {
                objective.PropertyChanged -= ObjectiveOnPropertyChanged;
                objective.StartTriggers.CollectionChanged -= OnObjectiveTriggersCollectionChanged;
                objective.FinishTriggers.CollectionChanged -= OnObjectiveTriggersCollectionChanged;
            }

            foreach (var trigger in _trackedTriggers.ToArray())
            {
                trigger.PropertyChanged -= TriggerOnPropertyChanged;
            }

            foreach (var npc in _trackedNpcs.ToArray())
            {
                DetachNpcHandlers(npc);
            }

            foreach (var folder in _trackedFolders.ToArray())
            {
                folder.PropertyChanged -= FolderOnPropertyChanged;
            }
            foreach (var asset in _trackedResources.ToArray())
            {
                asset.PropertyChanged -= ResourceOnPropertyChanged;
            }

            _trackedQuests.Clear();
            _trackedObjectives.Clear();
            _trackedTriggers.Clear();
            _trackedNpcs.Clear();
            _trackedFolders.Clear();
            _trackedResources.Clear();
            RebuildObjectiveTracking();
            AttachExistingQuestHandlers();
            AttachExistingNpcHandlers();
            AttachExistingFolderHandlers();
            AttachExistingResourceHandlers();
        }

        private void RebuildObjectiveTracking()
        {
            foreach (var objective in _trackedObjectives.ToArray())
            {
                objective.PropertyChanged -= ObjectiveOnPropertyChanged;
            }

            _trackedObjectives.Clear();

            foreach (var quest in Quests)
            {
                foreach (var objective in quest.Objectives)
                {
                    AttachObjectiveHandlers(objective);
                }
            }
        }

        private void OnNpcsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is NpcBlueprint npc)
                    {
                        AttachNpcHandlers(npc);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is NpcBlueprint npc)
                    {
                        DetachNpcHandlers(npc);
                    }
                }
            }

            MarkAsModified();
            OnPropertyChanged(nameof(Npcs));
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            _suppressNotifications = true;
            Quests.CollectionChanged -= OnQuestsCollectionChanged;
            Quests.CollectionChanged += OnQuestsCollectionChanged;
            Npcs.CollectionChanged -= OnNpcsCollectionChanged;
            Npcs.CollectionChanged += OnNpcsCollectionChanged;
            Folders.CollectionChanged -= OnFoldersCollectionChanged;
            Folders.CollectionChanged += OnFoldersCollectionChanged;
            Resources.CollectionChanged -= OnResourcesCollectionChanged;
            Resources.CollectionChanged += OnResourcesCollectionChanged;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            AttachExistingQuestHandlers();
            AttachExistingNpcHandlers();
            AttachExistingFolderHandlers();
            AttachExistingResourceHandlers();
            EnsureRootFolder();
            _suppressNotifications = false;
            OnPropertyChanged(nameof(DisplayTitle));
        }

        private void AttachFolderHandlers(ModFolder folder)
        {
            if (_trackedFolders.Contains(folder))
                return;
            _trackedFolders.Add(folder);
            folder.PropertyChanged += FolderOnPropertyChanged;
        }

        private void DetachFolderHandlers(ModFolder folder)
        {
            if (!_trackedFolders.Remove(folder))
                return;
            folder.PropertyChanged -= FolderOnPropertyChanged;
        }

        private void FolderOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkAsModified();
        }

        private void OnResourcesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ResourceAsset asset)
                    {
                        AttachResourceHandlers(asset);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is ResourceAsset asset)
                    {
                        DetachResourceHandlers(asset);
                    }
                }
            }

            MarkAsModified();
            OnPropertyChanged(nameof(Resources));
        }

        private void AttachResourceHandlers(ResourceAsset asset)
        {
            if (_trackedResources.Contains(asset))
                return;
            _trackedResources.Add(asset);
            asset.PropertyChanged += ResourceOnPropertyChanged;
        }

        private void DetachResourceHandlers(ResourceAsset asset)
        {
            if (!_trackedResources.Remove(asset))
                return;
            asset.PropertyChanged -= ResourceOnPropertyChanged;
        }

        private void ResourceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkAsModified();
        }

        internal void EnsureRootFolder()
        {
            if (!Folders.Any())
            {
                Folders.Add(new ModFolder
                {
                    Id = RootFolderId,
                    Name = "Workspace",
                    ParentId = null
                });
            }

            var root = Folders.FirstOrDefault(f => f.Id == RootFolderId);
            if (root == null)
            {
                root = new ModFolder
                {
                    Id = RootFolderId,
                    Name = "Workspace",
                    ParentId = null
                };
                Folders.Insert(0, root);
            }
            else
            {
                root.ParentId = null;
                if (string.IsNullOrWhiteSpace(root.Name))
                {
                    root.Name = "Workspace";
                }
            }

            foreach (var quest in Quests)
            {
                if (string.IsNullOrWhiteSpace(quest.FolderId))
                    quest.FolderId = RootFolderId;
            }

            foreach (var npc in Npcs)
            {
                if (string.IsNullOrWhiteSpace(npc.FolderId))
                    npc.FolderId = RootFolderId;
            }
        }
    }
}
