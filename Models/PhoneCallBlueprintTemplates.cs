namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Starter templates for generated S1API phone calls.
    /// </summary>
    public static class PhoneCallBlueprintTemplates
    {
        public static PhoneCallBlueprint CreateTutorialCall()
        {
            var blueprint = new PhoneCallBlueprint
            {
                ClassName = "TutorialPhoneCall",
                Namespace = "Schedule1Mods.PhoneCalls",
                CallId = "tutorial_phone_call",
                CallTitle = "Tutorial Call",
                CallerMode = PhoneCallCallerMode.CustomCaller,
                CallerName = "Guide Bot",
                QueueMode = PhoneCallQueueMode.Manual,
                GenerateHookScaffold = true
            };

            var introStage = new PhoneCallStageBlueprint
            {
                Name = "Introduction",
                Text = "Welcome to your first generated phone call."
            };
            introStage.StartTriggers.Add(new PhoneCallSystemTriggerBlueprint
            {
                Name = "Mark Tutorial Seen",
                VariableSetters =
                {
                    new PhoneCallVariableSetterBlueprint
                    {
                        Evaluation = PhoneCallTriggerEvaluationOption.PassOnTrue,
                        VariableName = "tutorial_seen",
                        NewValue = "true"
                    }
                }
            });

            blueprint.Stages.Add(introStage);
            blueprint.Stages.Add(new PhoneCallStageBlueprint
            {
                Name = "Follow Up",
                Text = "Use stage triggers to set variables or move quests when the player reads through the call."
            });

            return blueprint;
        }

        public static PhoneCallBlueprint CreateNpcCallerCall()
        {
            var blueprint = new PhoneCallBlueprint
            {
                ClassName = "NpcCallerPhoneCall",
                Namespace = "Schedule1Mods.PhoneCalls",
                CallId = "npc_caller_phone_call",
                CallTitle = "NPC Caller Example",
                CallerMode = PhoneCallCallerMode.NpcCaller,
                CallerNpcId = "sample_npc",
                QueueMode = PhoneCallQueueMode.OnLocalPlayerSpawned,
                QueueDelaySeconds = 2d,
                GenerateHookScaffold = true
            };

            blueprint.Stages.Add(new PhoneCallStageBlueprint
            {
                Name = "Greeting",
                Text = "This call uses an NPC caller. Replace the NPC ID with one of your authored NPCs or a base-game NPC ID."
            });

            return blueprint;
        }
    }
}
