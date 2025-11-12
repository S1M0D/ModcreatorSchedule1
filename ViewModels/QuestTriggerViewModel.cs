using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// ViewModel for configuring quest triggers in the UI
    /// </summary>
    public class QuestTriggerViewModel : ObservableObject
    {
        private QuestTrigger _trigger;
        private readonly QuestBlueprint _questBlueprint;
        private readonly QuestObjective _objective; // null if this is a quest-level trigger

        public QuestTriggerViewModel(QuestTrigger trigger, QuestBlueprint questBlueprint, QuestObjective objective = null)
        {
            _trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
            _questBlueprint = questBlueprint ?? throw new ArgumentNullException(nameof(questBlueprint));
            _objective = objective;

            AvailableTriggers = TriggerRegistryService.GetAvailableTriggers();
            AvailableNpcs = GetAvailableNpcs();
            
            RemoveCommand = new RelayCommand(() => RemoveRequested?.Invoke(this));
        }

        public QuestTrigger Trigger
        {
            get => _trigger;
            set => SetProperty(ref _trigger, value);
        }

        public List<TriggerMetadata> AvailableTriggers { get; }

        public List<string> AvailableNpcs { get; }

        public bool RequiresNpcId => Trigger.TriggerType == QuestTriggerType.NPCEventTrigger;

        public bool RequiresQuestId => Trigger.TriggerType == QuestTriggerType.QuestEventTrigger;

        public bool RequiresObjectiveIndex => Trigger.TriggerTarget == QuestTriggerTarget.ObjectiveStart || 
                                             Trigger.TriggerTarget == QuestTriggerTarget.ObjectiveFinish;

        public event Action<QuestTriggerViewModel> RemoveRequested;

        public ICommand RemoveCommand { get; }

        private List<string> GetAvailableNpcs()
        {
            var npcs = new List<string>();
            
            // Get NPCs from the current project
            // Note: This assumes we have access to the project's NPCs
            // For now, return empty list - will be populated from project context if available
            return npcs;
        }

        public void UpdateNpcList(List<string> npcIds)
        {
            // This can be called when NPCs are available from the project
            // For now, we'll rely on manual entry
        }
    }
}

