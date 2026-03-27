using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents an authored global saveable state definition.
    /// </summary>
    public class GlobalStateBlueprint : ObservableObject
    {
        private readonly HashSet<GlobalStateFieldBlueprint> _trackedFields = new();
        private string _className = "GlobalState";
        private string _stateName = "Global Save Variables";
        private string _namespace = "Schedule1Mods.Saveables";
        private string _modName = "Schedule 1 Mod Pack";
        private string _modVersion = "1.0.0";
        private string _modAuthor = "Mod Creator";
        private string _gameDeveloper = "TVGS";
        private string _gameName = "Schedule I";
        private bool _generateHookScaffold = true;
        private SaveableLoadOrderOption _loadOrder = SaveableLoadOrderOption.AfterBaseGame;
        private string _folderId = QuestProject.RootFolderId;

        public GlobalStateBlueprint()
        {
            Fields.CollectionChanged += FieldsOnCollectionChanged;
        }

        [Required(ErrorMessage = "Class name is required")]
        [JsonProperty("className")]
        public string ClassName
        {
            get => _className;
            set
            {
                if (SetProperty(ref _className, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(Summary));
                }
            }
        }

        [Required(ErrorMessage = "State name is required")]
        [JsonProperty("stateName")]
        public string StateName
        {
            get => _stateName;
            set
            {
                if (SetProperty(ref _stateName, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(Summary));
                }
            }
        }

        [Required(ErrorMessage = "Namespace is required")]
        [JsonProperty("namespace")]
        public string Namespace
        {
            get => _namespace;
            set => SetProperty(ref _namespace, value ?? string.Empty);
        }

        [Required(ErrorMessage = "Mod name is required")]
        [JsonProperty("modName")]
        public string ModName
        {
            get => _modName;
            set => SetProperty(ref _modName, value ?? string.Empty);
        }

        [Required(ErrorMessage = "Mod version is required")]
        [JsonProperty("modVersion")]
        public string ModVersion
        {
            get => _modVersion;
            set => SetProperty(ref _modVersion, value ?? string.Empty);
        }

        [Required(ErrorMessage = "Mod author is required")]
        [JsonProperty("modAuthor")]
        public string ModAuthor
        {
            get => _modAuthor;
            set => SetProperty(ref _modAuthor, value ?? string.Empty);
        }

        [JsonProperty("gameDeveloper")]
        public string GameDeveloper
        {
            get => _gameDeveloper;
            set => SetProperty(ref _gameDeveloper, value ?? string.Empty);
        }

        [JsonProperty("gameName")]
        public string GameName
        {
            get => _gameName;
            set => SetProperty(ref _gameName, value ?? string.Empty);
        }

        [JsonProperty("generateHookScaffold")]
        public bool GenerateHookScaffold
        {
            get => _generateHookScaffold;
            set => SetProperty(ref _generateHookScaffold, value);
        }

        [JsonProperty("loadOrder")]
        public SaveableLoadOrderOption LoadOrder
        {
            get => _loadOrder;
            set => SetProperty(ref _loadOrder, value);
        }

        [JsonProperty("folderId")]
        public string FolderId
        {
            get => _folderId;
            set => SetProperty(ref _folderId, string.IsNullOrWhiteSpace(value) ? QuestProject.RootFolderId : value);
        }

        [JsonProperty("fields")]
        public ObservableCollection<GlobalStateFieldBlueprint> Fields { get; } = new ObservableCollection<GlobalStateFieldBlueprint>();

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(StateName) ? ClassName : StateName;

        [JsonIgnore]
        public string Summary => $"{DisplayName} ({Fields.Count} fields)";

        public void CopyFrom(GlobalStateBlueprint source)
        {
            ArgumentNullException.ThrowIfNull(source);

            ClassName = source.ClassName;
            StateName = source.StateName;
            Namespace = source.Namespace;
            ModName = source.ModName;
            ModVersion = source.ModVersion;
            ModAuthor = source.ModAuthor;
            GameDeveloper = source.GameDeveloper;
            GameName = source.GameName;
            GenerateHookScaffold = source.GenerateHookScaffold;
            LoadOrder = source.LoadOrder;
            FolderId = source.FolderId;

            Fields.Clear();
            foreach (var field in source.Fields)
            {
                Fields.Add(field.DeepCopy());
            }
        }

        public GlobalStateBlueprint DeepCopy()
        {
            var copy = new GlobalStateBlueprint();
            copy.CopyFrom(this);
            return copy;
        }

        private void FieldsOnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removedField in e.OldItems.OfType<GlobalStateFieldBlueprint>())
                {
                    if (_trackedFields.Remove(removedField))
                    {
                        removedField.PropertyChanged -= FieldOnPropertyChanged;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var addedField in e.NewItems.OfType<GlobalStateFieldBlueprint>())
                {
                    if (_trackedFields.Add(addedField))
                    {
                        addedField.PropertyChanged += FieldOnPropertyChanged;
                    }
                }
            }

            OnPropertyChanged(nameof(Fields));
            OnPropertyChanged(nameof(Summary));
        }

        private void FieldOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Fields));
            OnPropertyChanged(nameof(Summary));
        }
    }

    /// <summary>
    /// Represents one persisted field on a generated saveable class.
    /// </summary>
    public class GlobalStateFieldBlueprint : ObservableObject
    {
        private string _fieldName = string.Empty;
        private string _saveKey = string.Empty;
        private DataClassFieldType _fieldType = DataClassFieldType.Bool;
        private string _defaultValue = string.Empty;
        private string _comment = string.Empty;

        [Required(ErrorMessage = "Field name is required")]
        [JsonProperty("fieldName")]
        public string FieldName
        {
            get => _fieldName;
            set => SetProperty(ref _fieldName, value ?? string.Empty);
        }

        [JsonProperty("saveKey")]
        public string SaveKey
        {
            get => _saveKey;
            set => SetProperty(ref _saveKey, value ?? string.Empty);
        }

        [JsonProperty("fieldType")]
        public DataClassFieldType FieldType
        {
            get => _fieldType;
            set => SetProperty(ref _fieldType, value);
        }

        [JsonProperty("defaultValue")]
        public string DefaultValue
        {
            get => _defaultValue;
            set => SetProperty(ref _defaultValue, value ?? string.Empty);
        }

        [JsonProperty("comment")]
        public string Comment
        {
            get => _comment;
            set => SetProperty(ref _comment, value ?? string.Empty);
        }

        [JsonIgnore]
        public string ResolvedSaveKey => string.IsNullOrWhiteSpace(SaveKey) ? FieldName : SaveKey;

        public GlobalStateFieldBlueprint DeepCopy()
        {
            return new GlobalStateFieldBlueprint
            {
                FieldName = FieldName,
                SaveKey = SaveKey,
                FieldType = FieldType,
                DefaultValue = DefaultValue,
                Comment = Comment
            };
        }
    }

    public enum SaveableLoadOrderOption
    {
        BeforeBaseGame,
        AfterBaseGame
    }

    public static class GlobalStateBlueprintOptions
    {
        public static IReadOnlyList<DataClassFieldType> FieldTypes { get; } =
            Enum.GetValues(typeof(DataClassFieldType)).Cast<DataClassFieldType>().ToArray();

        public static IReadOnlyList<SaveableLoadOrderOption> LoadOrders { get; } =
            Enum.GetValues(typeof(SaveableLoadOrderOption)).Cast<SaveableLoadOrderOption>().ToArray();
    }
}
