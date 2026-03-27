using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a generated assignment into a project global saveable field.
    /// </summary>
    public class GlobalStateSetterBlueprint : ObservableObject
    {
        private string _globalStateClassName = string.Empty;
        private string _globalStateDisplayName = string.Empty;
        private string _fieldName = string.Empty;
        private string _fieldSaveKey = string.Empty;
        private DataClassFieldType _fieldType = DataClassFieldType.Bool;
        private PhoneCallTriggerEvaluationOption _evaluation = PhoneCallTriggerEvaluationOption.PassOnTrue;
        private string _newValue = "true";
        private bool _requestSave = true;

        [JsonProperty("globalStateClassName")]
        public string GlobalStateClassName
        {
            get => _globalStateClassName;
            set
            {
                if (SetProperty(ref _globalStateClassName, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("globalStateDisplayName")]
        public string GlobalStateDisplayName
        {
            get => _globalStateDisplayName;
            set
            {
                if (SetProperty(ref _globalStateDisplayName, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("fieldName")]
        public string FieldName
        {
            get => _fieldName;
            set
            {
                if (SetProperty(ref _fieldName, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("fieldSaveKey")]
        public string FieldSaveKey
        {
            get => _fieldSaveKey;
            set
            {
                if (SetProperty(ref _fieldSaveKey, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("fieldType")]
        public DataClassFieldType FieldType
        {
            get => _fieldType;
            set => SetProperty(ref _fieldType, value);
        }

        [JsonProperty("evaluation")]
        public PhoneCallTriggerEvaluationOption Evaluation
        {
            get => _evaluation;
            set => SetProperty(ref _evaluation, value);
        }

        [JsonProperty("newValue")]
        public string NewValue
        {
            get => _newValue;
            set => SetProperty(ref _newValue, value ?? string.Empty);
        }

        [JsonProperty("requestSave")]
        public bool RequestSave
        {
            get => _requestSave;
            set => SetProperty(ref _requestSave, value);
        }

        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                var stateName = string.IsNullOrWhiteSpace(GlobalStateDisplayName) ? GlobalStateClassName : GlobalStateDisplayName;
                var fieldName = string.IsNullOrWhiteSpace(FieldName) ? FieldSaveKey : FieldName;

                if (string.IsNullOrWhiteSpace(stateName) && string.IsNullOrWhiteSpace(fieldName))
                {
                    return "(Select save field)";
                }

                return $"{stateName}.{fieldName}";
            }
        }

        public void ApplyReference(GlobalStateFieldReferenceInfo reference)
        {
            ArgumentNullException.ThrowIfNull(reference);

            GlobalStateClassName = reference.GlobalStateClassName;
            GlobalStateDisplayName = reference.GlobalStateDisplayName;
            FieldName = reference.FieldName;
            FieldSaveKey = reference.FieldSaveKey;
            FieldType = reference.FieldType;
        }

        public GlobalStateSetterBlueprint DeepCopy()
        {
            return new GlobalStateSetterBlueprint
            {
                GlobalStateClassName = GlobalStateClassName,
                GlobalStateDisplayName = GlobalStateDisplayName,
                FieldName = FieldName,
                FieldSaveKey = FieldSaveKey,
                FieldType = FieldType,
                Evaluation = Evaluation,
                NewValue = NewValue,
                RequestSave = RequestSave
            };
        }
    }

    /// <summary>
    /// Flattened editor-side reference for selecting a generated global saveable field.
    /// </summary>
    public class GlobalStateFieldReferenceInfo
    {
        public string GlobalStateClassName { get; init; } = string.Empty;
        public string GlobalStateDisplayName { get; init; } = string.Empty;
        public string FieldName { get; init; } = string.Empty;
        public string FieldSaveKey { get; init; } = string.Empty;
        public DataClassFieldType FieldType { get; init; }

        public string IdentityKey => $"{GlobalStateClassName}|{FieldSaveKey}";

        public string DisplayLabel
        {
            get
            {
                var stateName = string.IsNullOrWhiteSpace(GlobalStateDisplayName) ? GlobalStateClassName : GlobalStateDisplayName;
                var fieldName = string.IsNullOrWhiteSpace(FieldName) ? FieldSaveKey : FieldName;
                return $"{stateName} / {fieldName} [{FieldType}]";
            }
        }
    }
}
