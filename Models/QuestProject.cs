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
        private readonly HashSet<QuestBlueprint> _trackedQuests = new HashSet<QuestBlueprint>();
        private readonly HashSet<QuestObjective> _trackedObjectives = new HashSet<QuestObjective>();
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
        }

        private void QuestOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkAsModified();
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
        }

        private void DetachObjectiveHandlers(QuestObjective objective)
        {
            if (!_trackedObjectives.Remove(objective))
                return;

            objective.PropertyChanged -= ObjectiveOnPropertyChanged;
        }

        private void ObjectiveOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkAsModified();
        }

        private void AttachExistingQuestHandlers()
        {
            foreach (var quest in Quests)
            {
                AttachQuestHandlers(quest);
            }
        }

        private void ResetQuestTracking()
        {
            foreach (var quest in _trackedQuests.ToArray())
            {
                quest.PropertyChanged -= QuestOnPropertyChanged;
                quest.Objectives.CollectionChanged -= OnObjectivesCollectionChanged;
            }

            _trackedQuests.Clear();
            RebuildObjectiveTracking();
            AttachExistingQuestHandlers();
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

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            _suppressNotifications = true;
            Quests.CollectionChanged -= OnQuestsCollectionChanged;
            Quests.CollectionChanged += OnQuestsCollectionChanged;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            AttachExistingQuestHandlers();
            _suppressNotifications = false;
            OnPropertyChanged(nameof(DisplayTitle));
        }
    }
}
