using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Editor-side blueprint for an S1API phone call definition.
    /// </summary>
    public class PhoneCallBlueprint : ObservableObject
    {
        private readonly HashSet<PhoneCallStageBlueprint> _trackedStages = new();
        private string _className = "GeneratedPhoneCall";
        private string _namespace = "Schedule1Mods.PhoneCalls";
        private string _callId = "generated_phone_call";
        private string _callTitle = "Generated Phone Call";
        private PhoneCallCallerMode _callerMode = PhoneCallCallerMode.CustomCaller;
        private string _callerName = "Unknown Caller";
        private string _callerNpcId = string.Empty;
        private string _callerIconResourcePath = string.Empty;
        private PhoneCallQueueMode _queueMode = PhoneCallQueueMode.Manual;
        private double _queueDelaySeconds;
        private bool _generateHookScaffold = true;
        private string _folderId = QuestProject.RootFolderId;
        private string _modName = "Schedule 1 Phone Calls";
        private string _modAuthor = "Phone Call Creator";
        private string _modVersion = "1.0.0";
        private string _gameDeveloper = "TVGS";
        private string _gameName = "Schedule I";

        public PhoneCallBlueprint()
        {
            Stages.CollectionChanged += StagesOnCollectionChanged;
        }

        [JsonProperty("className")]
        public string ClassName
        {
            get => _className;
            set => SetProperty(ref _className, value ?? string.Empty);
        }

        [JsonProperty("namespace")]
        public string Namespace
        {
            get => _namespace;
            set => SetProperty(ref _namespace, value ?? string.Empty);
        }

        [JsonProperty("callId")]
        public string CallId
        {
            get => _callId;
            set => SetProperty(ref _callId, value ?? string.Empty);
        }

        [JsonProperty("callTitle")]
        public string CallTitle
        {
            get => _callTitle;
            set
            {
                if (SetProperty(ref _callTitle, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("callerMode")]
        public PhoneCallCallerMode CallerMode
        {
            get => _callerMode;
            set
            {
                if (SetProperty(ref _callerMode, value))
                {
                    OnPropertyChanged(nameof(UsesCustomCaller));
                    OnPropertyChanged(nameof(UsesNpcCaller));
                    OnPropertyChanged(nameof(DisplayCallerSummary));
                }
            }
        }

        [JsonProperty("callerName")]
        public string CallerName
        {
            get => _callerName;
            set
            {
                if (SetProperty(ref _callerName, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayCallerSummary));
                }
            }
        }

        [JsonProperty("callerNpcId")]
        public string CallerNpcId
        {
            get => _callerNpcId;
            set
            {
                if (SetProperty(ref _callerNpcId, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayCallerSummary));
                }
            }
        }

        [JsonProperty("callerIconResourcePath")]
        public string CallerIconResourcePath
        {
            get => _callerIconResourcePath;
            set => SetProperty(ref _callerIconResourcePath, value ?? string.Empty);
        }

        [JsonProperty("queueMode")]
        public PhoneCallQueueMode QueueMode
        {
            get => _queueMode;
            set
            {
                if (SetProperty(ref _queueMode, value))
                {
                    OnPropertyChanged(nameof(IsAutoQueued));
                }
            }
        }

        [JsonProperty("queueDelaySeconds")]
        public double QueueDelaySeconds
        {
            get => _queueDelaySeconds;
            set => SetProperty(ref _queueDelaySeconds, Math.Max(0d, value));
        }

        [JsonProperty("generateHookScaffold")]
        public bool GenerateHookScaffold
        {
            get => _generateHookScaffold;
            set => SetProperty(ref _generateHookScaffold, value);
        }

        [JsonProperty("folderId")]
        public string FolderId
        {
            get => _folderId;
            set => SetProperty(ref _folderId, string.IsNullOrWhiteSpace(value) ? QuestProject.RootFolderId : value);
        }

        [JsonProperty("modName")]
        public string ModName
        {
            get => _modName;
            set => SetProperty(ref _modName, value ?? string.Empty);
        }

        [JsonProperty("modAuthor")]
        public string ModAuthor
        {
            get => _modAuthor;
            set => SetProperty(ref _modAuthor, value ?? string.Empty);
        }

        [JsonProperty("modVersion")]
        public string ModVersion
        {
            get => _modVersion;
            set => SetProperty(ref _modVersion, value ?? string.Empty);
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

        [JsonProperty("stages")]
        public ObservableCollection<PhoneCallStageBlueprint> Stages { get; } = new();

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(CallTitle) ? CallId : CallTitle;

        [JsonIgnore]
        public bool UsesCustomCaller => CallerMode == PhoneCallCallerMode.CustomCaller;

        [JsonIgnore]
        public bool UsesNpcCaller => CallerMode == PhoneCallCallerMode.NpcCaller;

        [JsonIgnore]
        public bool IsAutoQueued => QueueMode != PhoneCallQueueMode.Manual;

        [JsonIgnore]
        public string DisplayCallerSummary => UsesNpcCaller
            ? (string.IsNullOrWhiteSpace(CallerNpcId) ? "NPC caller not set" : $"NPC: {CallerNpcId}")
            : (string.IsNullOrWhiteSpace(CallerName) ? "Custom caller not set" : CallerName);

        public void CopyFrom(PhoneCallBlueprint source)
        {
            ArgumentNullException.ThrowIfNull(source);

            ClassName = source.ClassName;
            Namespace = source.Namespace;
            CallId = source.CallId;
            CallTitle = source.CallTitle;
            CallerMode = source.CallerMode;
            CallerName = source.CallerName;
            CallerNpcId = source.CallerNpcId;
            CallerIconResourcePath = source.CallerIconResourcePath;
            QueueMode = source.QueueMode;
            QueueDelaySeconds = source.QueueDelaySeconds;
            GenerateHookScaffold = source.GenerateHookScaffold;
            FolderId = source.FolderId;
            ModName = source.ModName;
            ModAuthor = source.ModAuthor;
            ModVersion = source.ModVersion;
            GameDeveloper = source.GameDeveloper;
            GameName = source.GameName;

            Stages.Clear();
            foreach (var stage in source.Stages)
            {
                Stages.Add(stage.DeepCopy());
            }
        }

        public PhoneCallBlueprint DeepCopy()
        {
            var copy = new PhoneCallBlueprint();
            copy.CopyFrom(this);
            return copy;
        }

        private void StagesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removedStage in e.OldItems.OfType<PhoneCallStageBlueprint>())
                {
                    DetachStageHandlers(removedStage);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var addedStage in e.NewItems.OfType<PhoneCallStageBlueprint>())
                {
                    AttachStageHandlers(addedStage);
                }
            }

            OnPropertyChanged(nameof(Stages));
        }

        private void AttachStageHandlers(PhoneCallStageBlueprint stage)
        {
            if (_trackedStages.Add(stage))
            {
                stage.PropertyChanged += StageOnPropertyChanged;
            }
        }

        private void DetachStageHandlers(PhoneCallStageBlueprint stage)
        {
            if (_trackedStages.Remove(stage))
            {
                stage.PropertyChanged -= StageOnPropertyChanged;
            }
        }

        private void StageOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Stages));
        }
    }

    /// <summary>
    /// Represents a single phone call stage and its generated trigger data.
    /// </summary>
    public class PhoneCallStageBlueprint : ObservableObject
    {
        private readonly HashSet<PhoneCallSystemTriggerBlueprint> _trackedStartTriggers = new();
        private readonly HashSet<PhoneCallSystemTriggerBlueprint> _trackedDoneTriggers = new();
        private string _name = "Stage";
        private string _text = "New phone call stage.";

        public PhoneCallStageBlueprint()
        {
            StartTriggers.CollectionChanged += StartTriggersOnCollectionChanged;
            DoneTriggers.CollectionChanged += DoneTriggersOnCollectionChanged;
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("text")]
        public string Text
        {
            get => _text;
            set
            {
                if (SetProperty(ref _text, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("startTriggers")]
        public ObservableCollection<PhoneCallSystemTriggerBlueprint> StartTriggers { get; } = new();

        [JsonProperty("doneTriggers")]
        public ObservableCollection<PhoneCallSystemTriggerBlueprint> DoneTriggers { get; } = new();

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(Name)
            ? (string.IsNullOrWhiteSpace(Text) ? "Stage" : Text)
            : Name;

        public void CopyFrom(PhoneCallStageBlueprint source)
        {
            ArgumentNullException.ThrowIfNull(source);

            Name = source.Name;
            Text = source.Text;

            StartTriggers.Clear();
            foreach (var trigger in source.StartTriggers)
            {
                StartTriggers.Add(trigger.DeepCopy());
            }

            DoneTriggers.Clear();
            foreach (var trigger in source.DoneTriggers)
            {
                DoneTriggers.Add(trigger.DeepCopy());
            }
        }

        public PhoneCallStageBlueprint DeepCopy()
        {
            var copy = new PhoneCallStageBlueprint();
            copy.CopyFrom(this);
            return copy;
        }

        private void StartTriggersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removed in e.OldItems.OfType<PhoneCallSystemTriggerBlueprint>())
                {
                    DetachTriggerHandlers(removed, _trackedStartTriggers);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var added in e.NewItems.OfType<PhoneCallSystemTriggerBlueprint>())
                {
                    AttachTriggerHandlers(added, _trackedStartTriggers);
                }
            }

            OnPropertyChanged(nameof(StartTriggers));
        }

        private void DoneTriggersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removed in e.OldItems.OfType<PhoneCallSystemTriggerBlueprint>())
                {
                    DetachTriggerHandlers(removed, _trackedDoneTriggers);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var added in e.NewItems.OfType<PhoneCallSystemTriggerBlueprint>())
                {
                    AttachTriggerHandlers(added, _trackedDoneTriggers);
                }
            }

            OnPropertyChanged(nameof(DoneTriggers));
        }

        private void AttachTriggerHandlers(PhoneCallSystemTriggerBlueprint trigger, ISet<PhoneCallSystemTriggerBlueprint> trackedTriggers)
        {
            if (trackedTriggers.Add(trigger))
            {
                trigger.PropertyChanged += TriggerOnPropertyChanged;
            }
        }

        private void DetachTriggerHandlers(PhoneCallSystemTriggerBlueprint trigger, ISet<PhoneCallSystemTriggerBlueprint> trackedTriggers)
        {
            if (trackedTriggers.Remove(trigger))
            {
                trigger.PropertyChanged -= TriggerOnPropertyChanged;
            }
        }

        private void TriggerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(StartTriggers));
            OnPropertyChanged(nameof(DoneTriggers));
        }
    }

    /// <summary>
    /// Represents one generated S1API system trigger inside a phone call stage.
    /// </summary>
    public class PhoneCallSystemTriggerBlueprint : ObservableObject
    {
        private readonly HashSet<PhoneCallVariableSetterBlueprint> _trackedVariableSetters = new();
        private readonly HashSet<GlobalStateSetterBlueprint> _trackedGlobalStateSetters = new();
        private readonly HashSet<PhoneCallQuestSetterBlueprint> _trackedQuestSetters = new();
        private string _name = "Trigger";

        public PhoneCallSystemTriggerBlueprint()
        {
            VariableSetters.CollectionChanged += VariableSettersOnCollectionChanged;
            GlobalStateSetters.CollectionChanged += GlobalStateSettersOnCollectionChanged;
            QuestSetters.CollectionChanged += QuestSettersOnCollectionChanged;
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("variableSetters")]
        public ObservableCollection<PhoneCallVariableSetterBlueprint> VariableSetters { get; } = new();

        [JsonProperty("globalStateSetters")]
        public ObservableCollection<GlobalStateSetterBlueprint> GlobalStateSetters { get; } = new();

        [JsonProperty("questSetters")]
        public ObservableCollection<PhoneCallQuestSetterBlueprint> QuestSetters { get; } = new();

        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    return Name;
                }

                var setterCount = VariableSetters.Count + GlobalStateSetters.Count + QuestSetters.Count;
                return setterCount == 0 ? "Trigger" : $"Trigger ({setterCount} setters)";
            }
        }

        public void CopyFrom(PhoneCallSystemTriggerBlueprint source)
        {
            ArgumentNullException.ThrowIfNull(source);

            Name = source.Name;

            VariableSetters.Clear();
            foreach (var variableSetter in source.VariableSetters)
            {
                VariableSetters.Add(variableSetter.DeepCopy());
            }

            GlobalStateSetters.Clear();
            foreach (var setter in source.GlobalStateSetters)
            {
                GlobalStateSetters.Add(setter.DeepCopy());
            }

            QuestSetters.Clear();
            foreach (var questSetter in source.QuestSetters)
            {
                QuestSetters.Add(questSetter.DeepCopy());
            }
        }

        public PhoneCallSystemTriggerBlueprint DeepCopy()
        {
            var copy = new PhoneCallSystemTriggerBlueprint();
            copy.CopyFrom(this);
            return copy;
        }

        private void VariableSettersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removed in e.OldItems.OfType<PhoneCallVariableSetterBlueprint>())
                {
                    DetachVariableSetterHandlers(removed);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var added in e.NewItems.OfType<PhoneCallVariableSetterBlueprint>())
                {
                    AttachVariableSetterHandlers(added);
                }
            }

            OnPropertyChanged(nameof(VariableSetters));
            OnPropertyChanged(nameof(DisplayName));
        }

        private void QuestSettersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removed in e.OldItems.OfType<PhoneCallQuestSetterBlueprint>())
                {
                    DetachQuestSetterHandlers(removed);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var added in e.NewItems.OfType<PhoneCallQuestSetterBlueprint>())
                {
                    AttachQuestSetterHandlers(added);
                }
            }

            OnPropertyChanged(nameof(QuestSetters));
            OnPropertyChanged(nameof(DisplayName));
        }

        private void GlobalStateSettersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removed in e.OldItems.OfType<GlobalStateSetterBlueprint>())
                {
                    DetachGlobalStateSetterHandlers(removed);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var added in e.NewItems.OfType<GlobalStateSetterBlueprint>())
                {
                    AttachGlobalStateSetterHandlers(added);
                }
            }

            OnPropertyChanged(nameof(GlobalStateSetters));
            OnPropertyChanged(nameof(DisplayName));
        }

        private void AttachVariableSetterHandlers(PhoneCallVariableSetterBlueprint variableSetter)
        {
            if (_trackedVariableSetters.Add(variableSetter))
            {
                variableSetter.PropertyChanged += SetterOnPropertyChanged;
            }
        }

        private void DetachVariableSetterHandlers(PhoneCallVariableSetterBlueprint variableSetter)
        {
            if (_trackedVariableSetters.Remove(variableSetter))
            {
                variableSetter.PropertyChanged -= SetterOnPropertyChanged;
            }
        }

        private void AttachGlobalStateSetterHandlers(GlobalStateSetterBlueprint setter)
        {
            if (_trackedGlobalStateSetters.Add(setter))
            {
                setter.PropertyChanged += SetterOnPropertyChanged;
            }
        }

        private void DetachGlobalStateSetterHandlers(GlobalStateSetterBlueprint setter)
        {
            if (_trackedGlobalStateSetters.Remove(setter))
            {
                setter.PropertyChanged -= SetterOnPropertyChanged;
            }
        }

        private void AttachQuestSetterHandlers(PhoneCallQuestSetterBlueprint questSetter)
        {
            if (_trackedQuestSetters.Add(questSetter))
            {
                questSetter.PropertyChanged += SetterOnPropertyChanged;
            }
        }

        private void DetachQuestSetterHandlers(PhoneCallQuestSetterBlueprint questSetter)
        {
            if (_trackedQuestSetters.Remove(questSetter))
            {
                questSetter.PropertyChanged -= SetterOnPropertyChanged;
            }
        }

        private void SetterOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(VariableSetters));
            OnPropertyChanged(nameof(GlobalStateSetters));
            OnPropertyChanged(nameof(QuestSetters));
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    /// <summary>
    /// Represents a variable setter attached to a phone call trigger.
    /// </summary>
    public class PhoneCallVariableSetterBlueprint : ObservableObject
    {
        private PhoneCallTriggerEvaluationOption _evaluation = PhoneCallTriggerEvaluationOption.PassOnTrue;
        private string _variableName = "tutorial_seen";
        private string _newValue = "true";

        [JsonProperty("evaluation")]
        public PhoneCallTriggerEvaluationOption Evaluation
        {
            get => _evaluation;
            set => SetProperty(ref _evaluation, value);
        }

        [JsonProperty("variableName")]
        public string VariableName
        {
            get => _variableName;
            set => SetProperty(ref _variableName, value ?? string.Empty);
        }

        [JsonProperty("newValue")]
        public string NewValue
        {
            get => _newValue;
            set => SetProperty(ref _newValue, value ?? string.Empty);
        }

        public PhoneCallVariableSetterBlueprint DeepCopy()
        {
            return new PhoneCallVariableSetterBlueprint
            {
                Evaluation = Evaluation,
                VariableName = VariableName,
                NewValue = NewValue
            };
        }
    }

    /// <summary>
    /// Represents a quest setter attached to a phone call trigger.
    /// </summary>
    public class PhoneCallQuestSetterBlueprint : ObservableObject
    {
        private PhoneCallTriggerEvaluationOption _evaluation = PhoneCallTriggerEvaluationOption.PassOnTrue;
        private PhoneCallQuestTargetMode _questTargetMode = PhoneCallQuestTargetMode.ProjectQuest;
        private string _questId = string.Empty;
        private PhoneCallQuestActionOption _questAction = PhoneCallQuestActionOption.Begin;
        private bool _setQuestAction = true;
        private bool _setQuestEntryState;
        private int _questEntryIndex;
        private PhoneCallQuestStateOption _questEntryState = PhoneCallQuestStateOption.Active;

        [JsonProperty("evaluation")]
        public PhoneCallTriggerEvaluationOption Evaluation
        {
            get => _evaluation;
            set => SetProperty(ref _evaluation, value);
        }

        [JsonProperty("questTargetMode")]
        public PhoneCallQuestTargetMode QuestTargetMode
        {
            get => _questTargetMode;
            set => SetProperty(ref _questTargetMode, value);
        }

        [JsonProperty("questId")]
        public string QuestId
        {
            get => _questId;
            set => SetProperty(ref _questId, value ?? string.Empty);
        }

        [JsonProperty("questAction")]
        public PhoneCallQuestActionOption QuestAction
        {
            get => _questAction;
            set => SetProperty(ref _questAction, value);
        }

        [JsonProperty("setQuestAction")]
        public bool SetQuestAction
        {
            get => _setQuestAction;
            set => SetProperty(ref _setQuestAction, value);
        }

        [JsonProperty("setQuestEntryState")]
        public bool SetQuestEntryState
        {
            get => _setQuestEntryState;
            set => SetProperty(ref _setQuestEntryState, value);
        }

        [JsonProperty("questEntryIndex")]
        public int QuestEntryIndex
        {
            get => _questEntryIndex;
            set => SetProperty(ref _questEntryIndex, Math.Max(0, value));
        }

        [JsonProperty("questEntryState")]
        public PhoneCallQuestStateOption QuestEntryState
        {
            get => _questEntryState;
            set => SetProperty(ref _questEntryState, value);
        }

        [JsonIgnore]
        public bool UsesProjectQuest => QuestTargetMode == PhoneCallQuestTargetMode.ProjectQuest;

        [JsonIgnore]
        public bool UsesBaseGameQuest => QuestTargetMode == PhoneCallQuestTargetMode.BaseGameQuest;

        public PhoneCallQuestSetterBlueprint DeepCopy()
        {
            return new PhoneCallQuestSetterBlueprint
            {
                Evaluation = Evaluation,
                QuestTargetMode = QuestTargetMode,
                QuestId = QuestId,
                QuestAction = QuestAction,
                SetQuestAction = SetQuestAction,
                SetQuestEntryState = SetQuestEntryState,
                QuestEntryIndex = QuestEntryIndex,
                QuestEntryState = QuestEntryState
            };
        }
    }

    public enum PhoneCallCallerMode
    {
        CustomCaller,
        NpcCaller
    }

    public enum PhoneCallQueueMode
    {
        Manual,
        OnMainSceneLoaded,
        OnLocalPlayerSpawned
    }

    public enum PhoneCallTriggerEvaluationOption
    {
        PassOnTrue,
        PassOnFalse
    }

    public enum PhoneCallQuestTargetMode
    {
        ProjectQuest,
        BaseGameQuest
    }

    public enum PhoneCallQuestActionOption
    {
        Begin,
        Success,
        Fail,
        Expire,
        Cancel
    }

    public enum PhoneCallQuestStateOption
    {
        Inactive,
        Active,
        Completed,
        Failed,
        Expired,
        Cancelled
    }

    /// <summary>
    /// Supplies binding-friendly option lists for phone call editors.
    /// </summary>
    public static class PhoneCallBlueprintOptions
    {
        public static IReadOnlyList<PhoneCallCallerMode> CallerModes { get; } =
            Enum.GetValues(typeof(PhoneCallCallerMode)).Cast<PhoneCallCallerMode>().ToArray();

        public static IReadOnlyList<PhoneCallQueueMode> QueueModes { get; } =
            Enum.GetValues(typeof(PhoneCallQueueMode)).Cast<PhoneCallQueueMode>().ToArray();

        public static IReadOnlyList<PhoneCallTriggerEvaluationOption> TriggerEvaluations { get; } =
            Enum.GetValues(typeof(PhoneCallTriggerEvaluationOption)).Cast<PhoneCallTriggerEvaluationOption>().ToArray();

        public static IReadOnlyList<PhoneCallQuestTargetMode> QuestTargetModes { get; } =
            Enum.GetValues(typeof(PhoneCallQuestTargetMode)).Cast<PhoneCallQuestTargetMode>().ToArray();

        public static IReadOnlyList<PhoneCallQuestActionOption> QuestActions { get; } =
            Enum.GetValues(typeof(PhoneCallQuestActionOption)).Cast<PhoneCallQuestActionOption>().ToArray();

        public static IReadOnlyList<PhoneCallQuestStateOption> QuestStates { get; } =
            Enum.GetValues(typeof(PhoneCallQuestStateOption)).Cast<PhoneCallQuestStateOption>().ToArray();
    }
}
