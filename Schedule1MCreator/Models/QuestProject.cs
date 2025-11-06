using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a project containing multiple quest blueprints
    /// </summary>
    public class QuestProject : ObservableObject
    {
        private string _projectName = "";
        private string _projectDescription = "";
        private string _filePath = "";
        private bool _isModified = false;

        [JsonProperty("projectName")]
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        [JsonProperty("projectDescription")]
        public string ProjectDescription
        {
            get => _projectDescription;
            set => SetProperty(ref _projectDescription, value);
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
            ProjectName = "New Quest Project";
            ProjectDescription = "A new quest modding project for Schedule 1";
        }

        public void AddQuest(QuestBlueprint quest)
        {
            Quests.Add(quest);
            MarkAsModified();
        }

        public void RemoveQuest(QuestBlueprint quest)
        {
            Quests.Remove(quest);
            MarkAsModified();
        }

        public void MarkAsModified()
        {
            IsModified = true;
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
                project.MarkAsSaved();
            }
            return project;
        }
    }
}