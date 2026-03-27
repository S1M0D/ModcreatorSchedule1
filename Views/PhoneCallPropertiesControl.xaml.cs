using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for PhoneCallPropertiesControl.xaml.
    /// </summary>
    public partial class PhoneCallPropertiesControl : UserControl
    {
        public PhoneCallPropertiesControl()
        {
            InitializeComponent();
        }

        public IReadOnlyList<BaseGameQuestDefinition> BaseGameQuests => BaseGameQuestCatalogService.GetQuests();

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private PhoneCallBlueprint? CurrentPhoneCall => ViewModel?.SelectedPhoneCall;

        private void AddStage_Click(object sender, RoutedEventArgs e)
        {
            var phoneCall = CurrentPhoneCall;
            if (phoneCall == null)
            {
                return;
            }

            phoneCall.Stages.Add(new PhoneCallStageBlueprint
            {
                Name = $"Stage {phoneCall.Stages.Count + 1}",
                Text = "New phone call stage."
            });
        }

        private void DuplicateStage_Click(object sender, RoutedEventArgs e)
        {
            var phoneCall = CurrentPhoneCall;
            var stage = GetTag<PhoneCallStageBlueprint>(sender);
            if (phoneCall == null || stage == null)
            {
                return;
            }

            var index = phoneCall.Stages.IndexOf(stage);
            if (index < 0)
            {
                return;
            }

            var copy = stage.DeepCopy();
            if (!string.IsNullOrWhiteSpace(copy.Name))
            {
                copy.Name += " Copy";
            }

            phoneCall.Stages.Insert(index + 1, copy);
        }

        private void RemoveStage_Click(object sender, RoutedEventArgs e)
        {
            var phoneCall = CurrentPhoneCall;
            var stage = GetTag<PhoneCallStageBlueprint>(sender);
            if (phoneCall == null || stage == null)
            {
                return;
            }

            phoneCall.Stages.Remove(stage);
        }

        private void AddStartTrigger_Click(object sender, RoutedEventArgs e)
        {
            var stage = GetTag<PhoneCallStageBlueprint>(sender);
            if (stage == null)
            {
                return;
            }

            stage.StartTriggers.Add(new PhoneCallSystemTriggerBlueprint
            {
                Name = $"Start Trigger {stage.StartTriggers.Count + 1}"
            });
        }

        private void AddDoneTrigger_Click(object sender, RoutedEventArgs e)
        {
            var stage = GetTag<PhoneCallStageBlueprint>(sender);
            if (stage == null)
            {
                return;
            }

            stage.DoneTriggers.Add(new PhoneCallSystemTriggerBlueprint
            {
                Name = $"Done Trigger {stage.DoneTriggers.Count + 1}"
            });
        }

        private void RemoveTrigger_Click(object sender, RoutedEventArgs e)
        {
            var phoneCall = CurrentPhoneCall;
            var trigger = GetTag<PhoneCallSystemTriggerBlueprint>(sender);
            if (phoneCall == null || trigger == null)
            {
                return;
            }

            var collection = FindTriggerCollection(phoneCall, trigger);
            collection?.Remove(trigger);
        }

        private void AddVariableSetter_Click(object sender, RoutedEventArgs e)
        {
            var trigger = GetTag<PhoneCallSystemTriggerBlueprint>(sender);
            if (trigger == null)
            {
                return;
            }

            trigger.VariableSetters.Add(new PhoneCallVariableSetterBlueprint
            {
                VariableName = "call_flag",
                NewValue = "true"
            });
        }

        private void RemoveVariableSetter_Click(object sender, RoutedEventArgs e)
        {
            var phoneCall = CurrentPhoneCall;
            var setter = GetTag<PhoneCallVariableSetterBlueprint>(sender);
            if (phoneCall == null || setter == null)
            {
                return;
            }

            var collection = FindVariableSetterCollection(phoneCall, setter);
            collection?.Remove(setter);
        }

        private void AddGlobalStateSetter_Click(object sender, RoutedEventArgs e)
        {
            var trigger = GetTag<PhoneCallSystemTriggerBlueprint>(sender);
            if (trigger == null)
            {
                return;
            }

            var setter = new GlobalStateSetterBlueprint();
            var defaultReference = ViewModel?.AvailableGlobalStateFieldReferences.FirstOrDefault();
            if (defaultReference != null)
            {
                setter.ApplyReference(defaultReference);
                setter.NewValue = GetDefaultValueForFieldType(defaultReference.FieldType);
            }

            trigger.GlobalStateSetters.Add(setter);
        }

        private void RemoveGlobalStateSetter_Click(object sender, RoutedEventArgs e)
        {
            var phoneCall = CurrentPhoneCall;
            var setter = GetTag<GlobalStateSetterBlueprint>(sender);
            if (phoneCall == null || setter == null)
            {
                return;
            }

            var collection = FindGlobalStateSetterCollection(phoneCall, setter);
            collection?.Remove(setter);
        }

        private void AddQuestSetter_Click(object sender, RoutedEventArgs e)
        {
            var trigger = GetTag<PhoneCallSystemTriggerBlueprint>(sender);
            if (trigger == null)
            {
                return;
            }

            var defaultQuestId = ViewModel?.CurrentProject.Quests.FirstOrDefault()?.QuestId
                                 ?? BaseGameQuests.FirstOrDefault()?.IdentifierName
                                 ?? string.Empty;

            trigger.QuestSetters.Add(new PhoneCallQuestSetterBlueprint
            {
                QuestId = defaultQuestId
            });
        }

        private void RemoveQuestSetter_Click(object sender, RoutedEventArgs e)
        {
            var phoneCall = CurrentPhoneCall;
            var setter = GetTag<PhoneCallQuestSetterBlueprint>(sender);
            if (phoneCall == null || setter == null)
            {
                return;
            }

            var collection = FindQuestSetterCollection(phoneCall, setter);
            collection?.Remove(setter);
        }

        private static T? GetTag<T>(object sender) where T : class
        {
            return (sender as FrameworkElement)?.Tag as T;
        }

        private static ObservableCollection<PhoneCallSystemTriggerBlueprint>? FindTriggerCollection(
            PhoneCallBlueprint phoneCall,
            PhoneCallSystemTriggerBlueprint trigger)
        {
            foreach (var stage in phoneCall.Stages)
            {
                if (stage.StartTriggers.Contains(trigger))
                {
                    return stage.StartTriggers;
                }

                if (stage.DoneTriggers.Contains(trigger))
                {
                    return stage.DoneTriggers;
                }
            }

            return null;
        }

        private static ObservableCollection<PhoneCallVariableSetterBlueprint>? FindVariableSetterCollection(
            PhoneCallBlueprint phoneCall,
            PhoneCallVariableSetterBlueprint setter)
        {
            foreach (var trigger in EnumerateTriggers(phoneCall))
            {
                if (trigger.VariableSetters.Contains(setter))
                {
                    return trigger.VariableSetters;
                }
            }

            return null;
        }

        private static ObservableCollection<PhoneCallQuestSetterBlueprint>? FindQuestSetterCollection(
            PhoneCallBlueprint phoneCall,
            PhoneCallQuestSetterBlueprint setter)
        {
            foreach (var trigger in EnumerateTriggers(phoneCall))
            {
                if (trigger.QuestSetters.Contains(setter))
                {
                    return trigger.QuestSetters;
                }
            }

            return null;
        }

        private static ObservableCollection<GlobalStateSetterBlueprint>? FindGlobalStateSetterCollection(
            PhoneCallBlueprint phoneCall,
            GlobalStateSetterBlueprint setter)
        {
            foreach (var trigger in EnumerateTriggers(phoneCall))
            {
                if (trigger.GlobalStateSetters.Contains(setter))
                {
                    return trigger.GlobalStateSetters;
                }
            }

            return null;
        }

        private void GlobalStateFieldComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.ComboBox comboBox || comboBox.Tag is not GlobalStateSetterBlueprint setter || ViewModel == null)
            {
                return;
            }

            var selectedReference = ViewModel.AvailableGlobalStateFieldReferences.FirstOrDefault(reference =>
                string.Equals(reference.GlobalStateClassName, setter.GlobalStateClassName, System.StringComparison.Ordinal) &&
                string.Equals(reference.FieldSaveKey, setter.FieldSaveKey, System.StringComparison.Ordinal));

            if (selectedReference != null)
            {
                comboBox.SelectedItem = selectedReference;
            }
        }

        private void GlobalStateFieldComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not System.Windows.Controls.ComboBox comboBox ||
                comboBox.Tag is not GlobalStateSetterBlueprint setter ||
                comboBox.SelectedItem is not GlobalStateFieldReferenceInfo reference)
            {
                return;
            }

            var previousType = setter.FieldType;
            setter.ApplyReference(reference);
            if (string.IsNullOrWhiteSpace(setter.NewValue) || previousType != reference.FieldType)
            {
                setter.NewValue = GetDefaultValueForFieldType(reference.FieldType);
            }
        }

        private static IEnumerable<PhoneCallSystemTriggerBlueprint> EnumerateTriggers(PhoneCallBlueprint phoneCall)
        {
            foreach (var stage in phoneCall.Stages)
            {
                foreach (var trigger in stage.StartTriggers)
                {
                    yield return trigger;
                }

                foreach (var trigger in stage.DoneTriggers)
                {
                    yield return trigger;
                }
            }
        }

        private static string GetDefaultValueForFieldType(DataClassFieldType fieldType)
        {
            return fieldType switch
            {
                DataClassFieldType.Bool => "true",
                DataClassFieldType.Int => "0",
                DataClassFieldType.Float => "0",
                DataClassFieldType.ListString => "item_a,item_b",
                _ => string.Empty
            };
        }
    }
}
