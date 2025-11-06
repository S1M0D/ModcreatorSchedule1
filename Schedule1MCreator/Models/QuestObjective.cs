using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a quest objective with validation attributes
    /// </summary>
    public class QuestObjective : ObservableObject
    {
        private string _name = "";
        private string _title = "";
        private int _requiredProgress = 1;
        private bool _hasLocation = false;
        private float _locationX = 0f;
        private float _locationY = 0f;
        private float _locationZ = 0f;

        [Required(ErrorMessage = "Objective name is required")]
        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [Required(ErrorMessage = "Objective title is required")]
        [JsonProperty("title")]
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        [Range(1, int.MaxValue, ErrorMessage = "Required progress must be at least 1")]
        [JsonProperty("requiredProgress")]
        public int RequiredProgress
        {
            get => _requiredProgress;
            set => SetProperty(ref _requiredProgress, value);
        }

        [JsonProperty("hasLocation")]
        public bool HasLocation
        {
            get => _hasLocation;
            set => SetProperty(ref _hasLocation, value);
        }

        [JsonProperty("locationX")]
        public float LocationX
        {
            get => _locationX;
            set => SetProperty(ref _locationX, value);
        }

        [JsonProperty("locationY")]
        public float LocationY
        {
            get => _locationY;
            set => SetProperty(ref _locationY, value);
        }

        [JsonProperty("locationZ")]
        public float LocationZ
        {
            get => _locationZ;
            set => SetProperty(ref _locationZ, value);
        }

        [JsonIgnore]
        public string LocationText => HasLocation ? $"({LocationX:F2}, {LocationY:F2}, {LocationZ:F2})" : "No Location";

        public QuestObjective()
        {
        }

        public QuestObjective(string name, string title)
        {
            Name = name;
            Title = title;
        }

        public override string ToString()
        {
            return $"{Title} ({Name})";
        }
    }
}