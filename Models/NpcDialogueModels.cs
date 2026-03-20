using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    public enum NpcDialogueInteractMode
    {
        None,
        UseOnInteract,
        UseOnInteractOnce
    }

    public enum NpcDialogueCallbackType
    {
        ChoiceSelected,
        NodeDisplayed,
        ConversationStarted
    }

    public enum NpcRuntimeEventType
    {
        CustomerUnlocked,
        CustomerDealCompleted,
        CustomerContractAssigned,
        DealerRecruited,
        DealerContractAccepted,
        DealerRecommended,
        RelationshipChanged,
        RelationshipUnlocked
    }

    public abstract class NpcGeneratedActionBlueprint : ObservableObject
    {
        private string _messageText = string.Empty;
        private string _jumpToContainerName = string.Empty;
        private string _jumpToNodeLabel = string.Empty;
        private bool _stopDialogueOverride;

        [JsonProperty("messageText")]
        public string MessageText
        {
            get => _messageText;
            set => SetProperty(ref _messageText, value ?? string.Empty);
        }

        [JsonProperty("jumpToContainerName")]
        public string JumpToContainerName
        {
            get => _jumpToContainerName;
            set => SetProperty(ref _jumpToContainerName, value ?? string.Empty);
        }

        [JsonProperty("jumpToNodeLabel")]
        public string JumpToNodeLabel
        {
            get => _jumpToNodeLabel;
            set => SetProperty(ref _jumpToNodeLabel, value ?? string.Empty);
        }

        [JsonProperty("stopDialogueOverride")]
        public bool StopDialogueOverride
        {
            get => _stopDialogueOverride;
            set => SetProperty(ref _stopDialogueOverride, value);
        }

        protected void CopyActionFieldsTo(NpcGeneratedActionBlueprint target)
        {
            target.MessageText = MessageText;
            target.JumpToContainerName = JumpToContainerName;
            target.JumpToNodeLabel = JumpToNodeLabel;
            target.StopDialogueOverride = StopDialogueOverride;
        }
    }

    public class NpcDialogueDatabaseEntryBlueprint : ObservableObject
    {
        private string _moduleName = string.Empty;
        private string _key = string.Empty;
        private string _text = string.Empty;

        [JsonProperty("moduleName")]
        public string ModuleName
        {
            get => _moduleName;
            set
            {
                if (SetProperty(ref _moduleName, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("key")]
        public string Key
        {
            get => _key;
            set
            {
                if (SetProperty(ref _key, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("text")]
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value ?? string.Empty);
        }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(ModuleName)
            ? $"Generic:{Key}"
            : $"{ModuleName}:{Key}";

        public NpcDialogueDatabaseEntryBlueprint DeepCopy()
        {
            return new NpcDialogueDatabaseEntryBlueprint
            {
                ModuleName = ModuleName,
                Key = Key,
                Text = Text
            };
        }
    }

    public class NpcDialogueChoiceBlueprint : ObservableObject
    {
        private string _choiceLabel = string.Empty;
        private string _choiceText = string.Empty;
        private string _targetNodeLabel = string.Empty;

        [JsonProperty("choiceLabel")]
        public string ChoiceLabel
        {
            get => _choiceLabel;
            set
            {
                if (SetProperty(ref _choiceLabel, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("choiceText")]
        public string ChoiceText
        {
            get => _choiceText;
            set => SetProperty(ref _choiceText, value ?? string.Empty);
        }

        [JsonProperty("targetNodeLabel")]
        public string TargetNodeLabel
        {
            get => _targetNodeLabel;
            set => SetProperty(ref _targetNodeLabel, value ?? string.Empty);
        }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(ChoiceLabel)
            ? "(New Choice)"
            : ChoiceLabel;

        public NpcDialogueChoiceBlueprint DeepCopy()
        {
            return new NpcDialogueChoiceBlueprint
            {
                ChoiceLabel = ChoiceLabel,
                ChoiceText = ChoiceText,
                TargetNodeLabel = TargetNodeLabel
            };
        }
    }

    public class NpcDialogueNodeBlueprint : ObservableObject
    {
        private string _nodeLabel = "ENTRY";
        private string _nodeText = string.Empty;

        [JsonProperty("nodeLabel")]
        public string NodeLabel
        {
            get => _nodeLabel;
            set
            {
                if (SetProperty(ref _nodeLabel, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("nodeText")]
        public string NodeText
        {
            get => _nodeText;
            set => SetProperty(ref _nodeText, value ?? string.Empty);
        }

        [JsonProperty("choices")]
        public ObservableCollection<NpcDialogueChoiceBlueprint> Choices { get; } = new();

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(NodeLabel)
            ? "(New Node)"
            : NodeLabel;

        public NpcDialogueNodeBlueprint DeepCopy()
        {
            var copy = new NpcDialogueNodeBlueprint
            {
                NodeLabel = NodeLabel,
                NodeText = NodeText
            };

            foreach (var choice in Choices)
                copy.Choices.Add(choice.DeepCopy());

            return copy;
        }
    }

    public class NpcDialogueContainerBlueprint : ObservableObject
    {
        private string _name = "DialogueContainer";
        private bool _allowExit = true;
        private NpcDialogueInteractMode _interactMode;

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("allowExit")]
        public bool AllowExit
        {
            get => _allowExit;
            set => SetProperty(ref _allowExit, value);
        }

        [JsonProperty("interactMode")]
        public NpcDialogueInteractMode InteractMode
        {
            get => _interactMode;
            set => SetProperty(ref _interactMode, value);
        }

        [JsonProperty("nodes")]
        public ObservableCollection<NpcDialogueNodeBlueprint> Nodes { get; } = new();

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(Name)
            ? "(New Container)"
            : Name;

        public NpcDialogueContainerBlueprint DeepCopy()
        {
            var copy = new NpcDialogueContainerBlueprint
            {
                Name = Name,
                AllowExit = AllowExit,
                InteractMode = InteractMode
            };

            foreach (var node in Nodes)
                copy.Nodes.Add(node.DeepCopy());

            return copy;
        }
    }

    public class NpcDialogueCallbackBlueprint : NpcGeneratedActionBlueprint
    {
        private NpcDialogueCallbackType _callbackType = NpcDialogueCallbackType.ChoiceSelected;
        private string _matchKey = string.Empty;

        [JsonProperty("callbackType")]
        public NpcDialogueCallbackType CallbackType
        {
            get => _callbackType;
            set
            {
                if (SetProperty(ref _callbackType, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("matchKey")]
        public string MatchKey
        {
            get => _matchKey;
            set
            {
                if (SetProperty(ref _matchKey, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonIgnore]
        public string DisplayName => CallbackType == NpcDialogueCallbackType.ConversationStarted
            ? "Conversation Started"
            : $"{CallbackType}: {MatchKey}";

        public NpcDialogueCallbackBlueprint DeepCopy()
        {
            var copy = new NpcDialogueCallbackBlueprint
            {
                CallbackType = CallbackType,
                MatchKey = MatchKey
            };
            CopyActionFieldsTo(copy);
            return copy;
        }
    }

    public class NpcDialogueInjectionBlueprint : NpcGeneratedActionBlueprint
    {
        private string _targetNpcId = string.Empty;
        private string _containerName = string.Empty;
        private string _fromNodeGuid = string.Empty;
        private string _toNodeGuid = string.Empty;
        private string _choiceLabel = string.Empty;
        private string _choiceText = string.Empty;

        [JsonProperty("targetNpcId")]
        public string TargetNpcId
        {
            get => _targetNpcId;
            set
            {
                if (SetProperty(ref _targetNpcId, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("containerName")]
        public string ContainerName
        {
            get => _containerName;
            set => SetProperty(ref _containerName, value ?? string.Empty);
        }

        [JsonProperty("fromNodeGuid")]
        public string FromNodeGuid
        {
            get => _fromNodeGuid;
            set => SetProperty(ref _fromNodeGuid, value ?? string.Empty);
        }

        [JsonProperty("toNodeGuid")]
        public string ToNodeGuid
        {
            get => _toNodeGuid;
            set => SetProperty(ref _toNodeGuid, value ?? string.Empty);
        }

        [JsonProperty("choiceLabel")]
        public string ChoiceLabel
        {
            get => _choiceLabel;
            set
            {
                if (SetProperty(ref _choiceLabel, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("choiceText")]
        public string ChoiceText
        {
            get => _choiceText;
            set => SetProperty(ref _choiceText, value ?? string.Empty);
        }

        [JsonIgnore]
        public string DisplayName => $"{TargetNpcId}: {ChoiceLabel}";

        public NpcDialogueInjectionBlueprint DeepCopy()
        {
            var copy = new NpcDialogueInjectionBlueprint
            {
                TargetNpcId = TargetNpcId,
                ContainerName = ContainerName,
                FromNodeGuid = FromNodeGuid,
                ToNodeGuid = ToNodeGuid,
                ChoiceLabel = ChoiceLabel,
                ChoiceText = ChoiceText
            };
            CopyActionFieldsTo(copy);
            return copy;
        }
    }

    public class NpcRuntimeEventReactionBlueprint : NpcGeneratedActionBlueprint
    {
        private NpcRuntimeEventType _eventType;

        [JsonProperty("eventType")]
        public NpcRuntimeEventType EventType
        {
            get => _eventType;
            set
            {
                if (SetProperty(ref _eventType, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonIgnore]
        public string DisplayName => EventType.ToString();

        public NpcRuntimeEventReactionBlueprint DeepCopy()
        {
            var copy = new NpcRuntimeEventReactionBlueprint
            {
                EventType = EventType
            };
            CopyActionFieldsTo(copy);
            return copy;
        }
    }
}
