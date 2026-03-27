using System.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Builders;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.Npc
{
    public class NpcCodeGenerator : ICodeGenerator<NpcBlueprint>
    {
        private readonly NpcHeaderGenerator _headerGenerator = new();
        private readonly NpcAppearanceGenerator _appearanceGenerator = new();
        private readonly NpcScheduleGenerator _scheduleGenerator = new();

        public string GenerateCode(NpcBlueprint npc)
        {
            ArgumentNullException.ThrowIfNull(npc);

            var builder = new CodeBuilder();
            var className = IdentifierSanitizer.MakeSafeIdentifier(npc.ClassName, "GeneratedNpc");
            var targetNamespace = NamespaceNormalizer.NormalizeForNpc(npc.Namespace);
            var rootNamespace = GetRootNamespace(targetNamespace);

            _headerGenerator.Generate(builder, npc);

            var usingsBuilder = new UsingStatementsBuilder();
            usingsBuilder.AddNpcUsings();
            usingsBuilder.GenerateUsings(builder);

            builder.OpenBlock($"namespace {targetNamespace}");
            GenerateNpcClass(builder, npc, className, rootNamespace);
            builder.CloseBlock();
            return builder.Build();
        }

        public CodeGenerationValidationResult Validate(NpcBlueprint blueprint)
        {
            var result = new CodeGenerationValidationResult { IsValid = true };

            if (blueprint == null)
            {
                result.IsValid = false;
                result.Errors.Add("Blueprint cannot be null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(blueprint.ClassName))
                result.Warnings.Add("Class name is empty, will use default 'GeneratedNpc'.");
            if (string.IsNullOrWhiteSpace(blueprint.NpcId))
                result.Errors.Add("NPC ID is required.");

            foreach (var container in blueprint.DialogueContainers)
            {
                if (string.IsNullOrWhiteSpace(container.Name))
                    result.Errors.Add("Dialogue containers need a name.");
                if (container.InteractMode != NpcDialogueInteractMode.None &&
                    !container.Nodes.Any(node => string.Equals(node.NodeLabel, "ENTRY", StringComparison.OrdinalIgnoreCase)))
                {
                    result.Warnings.Add($"Dialogue container '{container.DisplayName}' is missing an ENTRY node.");
                }
            }

            foreach (var callback in blueprint.DialogueCallbacks)
            {
                if (callback.CallbackType != NpcDialogueCallbackType.ConversationStarted &&
                    string.IsNullOrWhiteSpace(callback.MatchKey))
                {
                    result.Errors.Add($"Dialogue callback '{callback.DisplayName}' needs a label.");
                }
            }

            foreach (var injection in blueprint.DialogueInjections)
            {
                if (string.IsNullOrWhiteSpace(injection.ContainerName) ||
                    string.IsNullOrWhiteSpace(injection.FromNodeGuid) ||
                    string.IsNullOrWhiteSpace(injection.ToNodeGuid) ||
                    string.IsNullOrWhiteSpace(injection.ChoiceLabel))
                {
                    result.Errors.Add($"Dialogue injection '{injection.DisplayName}' is missing required identifiers.");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private void GenerateNpcClass(ICodeBuilder builder, NpcBlueprint npc, string className, string rootNamespace)
        {
            builder.AppendBlockComment(
                $"Auto-generated NPC blueprint for \"{CodeFormatter.EscapeString(npc.DisplayName)}\".",
                "Companion partial hooks can extend runtime behavior without editing generated code."
            );

            builder.OpenBlock($"public sealed partial class {className} : NPC");
            builder.AppendLine("private bool _generatedDialogueConfigured;");
            builder.AppendLine("private bool _generatedRuntimeEventsRegistered;");
            builder.AppendLine("private static bool _generatedDialogueInjectionsRegistered;");
            builder.AppendLine();
            builder.AppendLine($"public override bool IsPhysical => {npc.IsPhysical.ToString().ToLowerInvariant()};");
            if (npc.IsDealer)
                builder.AppendLine("public override bool IsDealer => true;");
            builder.AppendLine();

            GenerateConfigurePrefabMethod(builder, npc);
            GenerateConstructor(builder, className);
            GenerateOnCreatedMethod(builder, npc);
            GenerateOnLoadedMethod(builder);
            GenerateOnResponseLoadedMethod(builder);
            GenerateRuntimeSettingsMethod(builder, npc);
            GenerateDialogueConfigurationMethod(builder, npc, rootNamespace);
            GenerateRuntimeEventsMethod(builder, npc, rootNamespace);
            GenerateDialogueInjectionMethod(builder, npc, rootNamespace);
            GenerateActionHelpers(builder);
            GeneratePartialHookMembers(builder);

            builder.CloseBlock();
        }

        private void GenerateConfigurePrefabMethod(ICodeBuilder builder, NpcBlueprint npc)
        {
            builder.OpenBlock("protected override void ConfigurePrefab(NPCPrefabBuilder builder)");
            builder.AppendLine($"builder.WithIdentity(\"{CodeFormatter.EscapeString(npc.NpcId)}\", \"{CodeFormatter.EscapeString(npc.FirstName)}\", \"{CodeFormatter.EscapeString(npc.LastName)}\")");
            _appearanceGenerator.Generate(builder, npc.Appearance);

            if (npc.HasSpawnPosition)
                builder.AppendLine($".WithSpawnPosition({CodeFormatter.FormatVector3(npc.SpawnX, npc.SpawnY, npc.SpawnZ)})");

            if (npc.EnableCustomer)
            {
                builder.AppendLine(".EnsureCustomer()");
                GenerateCustomerDefaults(builder, npc.CustomerDefaults);
            }

            if (npc.IsDealer)
            {
                builder.AppendLine(".EnsureDealer()");
                GenerateDealerDefaults(builder, npc.DealerDefaults);
            }

            GenerateRuntimePrefabFeatures(builder, npc.RuntimeSettings);

            if (ShouldGenerateRelationshipDefaults(npc.RelationshipDefaults))
                GenerateRelationshipDefaults(builder, npc.RelationshipDefaults);

            if (npc.ScheduleActions.Count > 0)
                _scheduleGenerator.Generate(builder, npc);

            if (ShouldGenerateInventoryDefaults(npc.InventoryDefaults))
                GenerateInventoryDefaults(builder, npc.InventoryDefaults);

            builder.AppendLine(";");
            builder.AppendLine("ConfigureGeneratedPrefab(builder);");
            builder.CloseBlock();
            builder.AppendLine();
        }

        private void GenerateConstructor(ICodeBuilder builder, string className)
        {
            builder.AppendLine($"public {className}() : base()");
            builder.OpenBlock();
            builder.CloseBlock();
            builder.AppendLine();
        }

        private void GenerateOnCreatedMethod(ICodeBuilder builder, NpcBlueprint npc)
        {
            builder.OpenBlock("protected override void OnCreated()");
            builder.AppendLine("base.OnCreated();");
            builder.AppendLine("Appearance.Build();");
            builder.AppendLine("Schedule.Enable();");
            builder.AppendLine("ApplyGeneratedRuntimeSettings();");
            builder.AppendLine("ConfigureGeneratedDialogue();");
            builder.AppendLine("RegisterGeneratedRuntimeEvents();");
            builder.AppendLine("RegisterGeneratedDialogueInjections();");
            builder.AppendLine("OnAfterCreatedGenerated();");
            builder.CloseBlock();
            builder.AppendLine();
        }

        private void GenerateOnLoadedMethod(ICodeBuilder builder)
        {
            builder.OpenBlock("protected override void OnLoaded()");
            builder.AppendLine("base.OnLoaded();");
            builder.AppendLine("ApplyGeneratedRuntimeSettings();");
            builder.AppendLine("OnAfterLoadedGenerated();");
            builder.CloseBlock();
            builder.AppendLine();
        }

        private void GenerateOnResponseLoadedMethod(ICodeBuilder builder)
        {
            builder.OpenBlock("protected override void OnResponseLoaded(Response response)");
            builder.AppendLine("base.OnResponseLoaded(response);");
            builder.AppendLine("OnResponseLoadedGenerated(response);");
            builder.CloseBlock();
            builder.AppendLine();
        }

        private void GenerateRuntimePrefabFeatures(ICodeBuilder builder, NpcRuntimeSettings runtimeSettings)
        {
            if (runtimeSettings.EnableSmokeBreak)
            {
                if (!string.IsNullOrWhiteSpace(runtimeSettings.SmokeBreakCigarettePath))
                {
                    builder.AppendLine(runtimeSettings.EnableSmokeBreakDebugMode
                        ? $".EnsureSmokeBreak(\"{CodeFormatter.EscapeString(runtimeSettings.SmokeBreakCigarettePath)}\", debugMode: true)"
                        : $".EnsureSmokeBreak(\"{CodeFormatter.EscapeString(runtimeSettings.SmokeBreakCigarettePath)}\")");
                }
                else if (runtimeSettings.EnableSmokeBreakDebugMode)
                {
                    builder.AppendLine(".EnsureSmokeBreak(debugMode: true)");
                }
                else
                {
                    builder.AppendLine(".EnsureSmokeBreak()");
                }
            }

            if (runtimeSettings.EnableGraffiti)
            {
                builder.AppendLine(string.IsNullOrWhiteSpace(runtimeSettings.SprayPaintEquippablePath)
                    ? ".EnsureGraffiti()"
                    : $".EnsureGraffiti(\"{CodeFormatter.EscapeString(runtimeSettings.SprayPaintEquippablePath)}\")");
            }

            if (runtimeSettings.EnableDrinking)
            {
                builder.AppendLine(string.IsNullOrWhiteSpace(runtimeSettings.DrinkEquippablePath)
                    ? ".EnsureDrinking()"
                    : $".EnsureDrinking(\"{CodeFormatter.EscapeString(runtimeSettings.DrinkEquippablePath)}\")");
            }

            if (runtimeSettings.EnableItemHolding)
            {
                builder.AppendLine(string.IsNullOrWhiteSpace(runtimeSettings.HeldItemEquippablePath)
                    ? ".EnsureItemHolding()"
                    : $".EnsureItemHolding(\"{CodeFormatter.EscapeString(runtimeSettings.HeldItemEquippablePath)}\")");
            }
        }

        private void GenerateCustomerDefaults(ICodeBuilder builder, NpcCustomerDefaults customerDefaults)
        {
            builder.OpenBlock(".WithCustomerDefaults(cd =>");
            builder.AppendLine($"cd.WithSpending({CodeFormatter.FormatFloat(customerDefaults.MinWeeklySpending)}f, {CodeFormatter.FormatFloat(customerDefaults.MaxWeeklySpending)}f)");
            builder.AppendLine($"  .WithOrdersPerWeek({customerDefaults.MinOrdersPerWeek}, {customerDefaults.MaxOrdersPerWeek})");

            var preferredDay = NormalizeComboValue(customerDefaults.PreferredOrderDay);
            if (!string.IsNullOrWhiteSpace(preferredDay))
                builder.AppendLine($"  .WithPreferredOrderDay(Day.{preferredDay})");

            builder.AppendLine($"  .WithOrderTime({customerDefaults.OrderTime})");

            var standards = NormalizeComboValue(customerDefaults.CustomerStandards);
            if (!string.IsNullOrWhiteSpace(standards))
                builder.AppendLine($"  .WithStandards(CustomerStandard.{standards})");

            builder.AppendLine($"  .AllowDirectApproach({customerDefaults.AllowDirectApproach.ToString().ToLowerInvariant()})");

            if (customerDefaults.GuaranteeFirstSample)
                builder.AppendLine("  .GuaranteeFirstSample(true)");

            if (customerDefaults.MutualRelationMinAt50 != 0 || customerDefaults.MutualRelationMaxAt100 != 0)
            {
                builder.AppendLine($"  .WithMutualRelationRequirement(minAt50: {CodeFormatter.FormatFloat(customerDefaults.MutualRelationMinAt50)}f, maxAt100: {CodeFormatter.FormatFloat(customerDefaults.MutualRelationMaxAt100)}f)");
            }

            if (customerDefaults.CallPoliceChance > 0)
                builder.AppendLine($"  .WithCallPoliceChance({CodeFormatter.FormatFloat(customerDefaults.CallPoliceChance)}f)");

            if (customerDefaults.BaseAddiction > 0 || customerDefaults.DependenceMultiplier != 1.0f)
            {
                builder.AppendLine($"  .WithDependence(baseAddiction: {CodeFormatter.FormatFloat(customerDefaults.BaseAddiction)}f, dependenceMultiplier: {CodeFormatter.FormatFloat(customerDefaults.DependenceMultiplier)}f)");
            }

            if (customerDefaults.DrugAffinities.Count > 0)
            {
                builder.AppendLine("  .WithAffinities(new[]");
                builder.AppendLine("  {");
                for (int i = 0; i < customerDefaults.DrugAffinities.Count; i++)
                {
                    var affinity = customerDefaults.DrugAffinities[i];
                    var comma = i < customerDefaults.DrugAffinities.Count - 1 ? "," : string.Empty;
                    builder.AppendLine($"      (DrugType.{affinity.DrugType}, {CodeFormatter.FormatFloat(affinity.AffinityValue)}f){comma}");
                }
                builder.AppendLine("  })");
            }

            if (customerDefaults.PreferredProperties.Count > 0)
            {
                var props = string.Join(", ", customerDefaults.PreferredProperties.Select(property => $"Property.{property}"));
                builder.AppendLine($"  .WithPreferredProperties({props})");
            }

            builder.AppendLine(";");
            builder.CloseBlock();
            builder.AppendLine(")");
        }

        private void GenerateDealerDefaults(ICodeBuilder builder, NpcDealerDefaults dealerDefaults)
        {
            builder.OpenBlock(".WithDealerDefaults(dd =>");
            builder.AppendLine($"dd.WithSigningFee({CodeFormatter.FormatFloat(dealerDefaults.SigningFee)}f)");
            builder.AppendLine($"  .WithCut({CodeFormatter.FormatFloat(dealerDefaults.CommissionCut)}f)");
            builder.AppendLine($"  .WithDealerType(DealerType.{dealerDefaults.DealerType})");

            if (!string.IsNullOrWhiteSpace(dealerDefaults.HomeName))
                builder.AppendLine($"  .WithHomeName(\"{CodeFormatter.EscapeString(dealerDefaults.HomeName)}\")");

            if (dealerDefaults.AllowInsufficientQuality)
                builder.AppendLine("  .AllowInsufficientQuality(true)");

            if (!dealerDefaults.AllowExcessQuality)
                builder.AppendLine("  .AllowExcessQuality(false)");

            if (!string.IsNullOrWhiteSpace(dealerDefaults.CompletedDealsVariable))
            {
                builder.AppendLine($"  .WithCompletedDealsVariable(\"{CodeFormatter.EscapeString(dealerDefaults.CompletedDealsVariable)}\")");
            }

            builder.AppendLine(";");
            builder.CloseBlock();
            builder.AppendLine(")");
        }

        private void GenerateRelationshipDefaults(ICodeBuilder builder, NpcRelationshipDefaults relationshipDefaults)
        {
            builder.OpenBlock(".WithRelationshipDefaults(r =>");
            builder.AppendLine($"r.WithDelta({CodeFormatter.FormatFloat(relationshipDefaults.StartingDelta)}f)");
            builder.AppendLine($"  .SetUnlocked({relationshipDefaults.StartsUnlocked.ToString().ToLowerInvariant()})");

            if (!string.IsNullOrWhiteSpace(relationshipDefaults.UnlockType))
                builder.AppendLine($"  .SetUnlockType(NPCRelationship.UnlockType.{relationshipDefaults.UnlockType})");

            if (relationshipDefaults.Connections.Count > 0)
            {
                var connections = string.Join(", ", relationshipDefaults.Connections.Select(connection => $"\"{CodeFormatter.EscapeString(connection)}\""));
                builder.AppendLine($"  .WithConnectionsById({connections})");
            }

            builder.AppendLine(";");
            builder.CloseBlock();
            builder.AppendLine(")");
        }

        private void GenerateInventoryDefaults(ICodeBuilder builder, NpcInventoryDefaults inventoryDefaults)
        {
            builder.OpenBlock(".WithInventoryDefaults(inv =>");

            if (inventoryDefaults.StartupItems.Count > 0)
            {
                var items = string.Join(", ", inventoryDefaults.StartupItems.Select(item => $"\"{CodeFormatter.EscapeString(item)}\""));
                builder.AppendLine($"inv.WithStartupItems({items})");
            }
            else
            {
                builder.AppendLine("inv");
            }

            if (inventoryDefaults.EnableRandomCash)
            {
                builder.AppendLine($"   .WithRandomCash(min: {(int)inventoryDefaults.RandomCashMin}, max: {(int)inventoryDefaults.RandomCashMax})");
            }

            if (!inventoryDefaults.ClearInventoryEachNight)
            {
                builder.AppendLine("   .WithClearInventoryEachNight(false)");
            }

            builder.AppendLine(";");
            builder.CloseBlock();
            builder.AppendLine(")");
        }
        private void GenerateRuntimeSettingsMethod(ICodeBuilder builder, NpcBlueprint npc)
        {
            builder.OpenBlock("private void ApplyGeneratedRuntimeSettings()");
            if (!npc.RuntimeSettings.SetAggressiveness &&
                !npc.RuntimeSettings.SetRegion &&
                !npc.RuntimeSettings.SetScale &&
                !npc.RuntimeSettings.OverrideRequiresRegionUnlocked)
            {
                builder.AppendLine("return;");
                builder.CloseBlock();
                builder.AppendLine();
                return;
            }

            if (npc.RuntimeSettings.SetAggressiveness)
                builder.AppendLine($"Aggressiveness = {CodeFormatter.FormatFloat(npc.RuntimeSettings.Aggressiveness)}f;");

            if (npc.RuntimeSettings.SetRegion)
                builder.AppendLine($"Region = Region.{npc.RuntimeSettings.Region};");

            if (npc.RuntimeSettings.SetScale)
                builder.AppendLine($"Scale = {CodeFormatter.FormatFloat(npc.RuntimeSettings.Scale)}f;");

            if (npc.RuntimeSettings.OverrideRequiresRegionUnlocked)
            {
                builder.AppendLine($"RequiresRegionUnlocked = {npc.RuntimeSettings.RequiresRegionUnlocked.ToString().ToLowerInvariant()};");
            }

            builder.CloseBlock();
            builder.AppendLine();
        }

        private void GenerateDialogueConfigurationMethod(ICodeBuilder builder, NpcBlueprint npc, string rootNamespace)
        {
            builder.OpenBlock("private void ConfigureGeneratedDialogue()");
            if (npc.DialogueDatabaseEntries.Count == 0 && npc.DialogueContainers.Count == 0 && npc.DialogueCallbacks.Count == 0)
            {
                builder.AppendLine("return;");
                builder.CloseBlock();
                builder.AppendLine();
                return;
            }

            builder.OpenBlock("if (_generatedDialogueConfigured)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine("_generatedDialogueConfigured = true;");
            builder.AppendLine();

            if (npc.DialogueDatabaseEntries.Count > 0 || npc.DialogueContainers.Count > 0)
            {
                builder.OpenBlock("Dialogue.BuildAndSetDatabase(db =>");
                foreach (var entry in npc.DialogueDatabaseEntries)
                {
                    var lines = FormatStringArray(entry.Text);
                    if (string.IsNullOrWhiteSpace(entry.ModuleName))
                        builder.AppendLine($"db.WithGeneric(\"{CodeFormatter.EscapeString(entry.Key)}\", {lines});");
                    else
                        builder.AppendLine($"db.WithModuleEntry(\"{CodeFormatter.EscapeString(entry.ModuleName)}\", \"{CodeFormatter.EscapeString(entry.Key)}\", {lines});");
                }
                builder.CloseBlock();
                builder.AppendLine(");");
                builder.AppendLine();

                foreach (var container in npc.DialogueContainers)
                {
                    builder.OpenBlock($"Dialogue.BuildAndRegisterContainer(\"{CodeFormatter.EscapeString(container.Name)}\", c =>");
                    builder.AppendLine($"c.SetAllowExit({container.AllowExit.ToString().ToLowerInvariant()});");
                    foreach (var node in container.Nodes)
                    {
                        if (node.Choices.Count == 0)
                        {
                            builder.AppendLine($"c.AddNode(\"{CodeFormatter.EscapeString(node.NodeLabel)}\", \"{CodeFormatter.EscapeString(node.NodeText)}\");");
                            continue;
                        }

                        builder.OpenBlock($"c.AddNode(\"{CodeFormatter.EscapeString(node.NodeLabel)}\", \"{CodeFormatter.EscapeString(node.NodeText)}\", choices =>");
                        foreach (var choice in node.Choices)
                        {
                            if (string.IsNullOrWhiteSpace(choice.TargetNodeLabel))
                                builder.AppendLine($"choices.Add(\"{CodeFormatter.EscapeString(choice.ChoiceLabel)}\", \"{CodeFormatter.EscapeString(choice.ChoiceText)}\");");
                            else
                                builder.AppendLine($"choices.Add(\"{CodeFormatter.EscapeString(choice.ChoiceLabel)}\", \"{CodeFormatter.EscapeString(choice.ChoiceText)}\", \"{CodeFormatter.EscapeString(choice.TargetNodeLabel)}\");");
                        }
                        builder.CloseBlock();
                        builder.AppendLine(");");
                    }
                    builder.CloseBlock();
                    builder.AppendLine(");");
                    builder.AppendLine();
                }

                var interactContainer = npc.DialogueContainers.FirstOrDefault(container => container.InteractMode != NpcDialogueInteractMode.None);
                if (interactContainer != null)
                {
                    builder.AppendLine(interactContainer.InteractMode == NpcDialogueInteractMode.UseOnInteractOnce
                        ? $"Dialogue.UseContainerOnInteractOnce(\"{CodeFormatter.EscapeString(interactContainer.Name)}\");"
                        : $"Dialogue.UseContainerOnInteract(\"{CodeFormatter.EscapeString(interactContainer.Name)}\");");
                    builder.AppendLine();
                }
            }

            foreach (var callback in npc.DialogueCallbacks)
            {
                switch (callback.CallbackType)
                {
                    case NpcDialogueCallbackType.ConversationStarted:
                        builder.OpenBlock("Dialogue.OnConversationStart(() =>");
                        AppendGeneratedActionInvocation(builder, callback, rootNamespace);
                        builder.CloseBlock();
                        builder.AppendLine(");");
                        break;

                    case NpcDialogueCallbackType.NodeDisplayed:
                        if (!string.IsNullOrWhiteSpace(callback.MatchKey))
                        {
                            builder.OpenBlock($"Dialogue.OnNodeDisplayed(\"{CodeFormatter.EscapeString(callback.MatchKey)}\", () =>");
                            AppendGeneratedActionInvocation(builder, callback, rootNamespace);
                            builder.CloseBlock();
                            builder.AppendLine(");");
                        }
                        break;

                    default:
                        if (!string.IsNullOrWhiteSpace(callback.MatchKey))
                        {
                            builder.OpenBlock($"Dialogue.OnChoiceSelected(\"{CodeFormatter.EscapeString(callback.MatchKey)}\", () =>");
                            AppendGeneratedActionInvocation(builder, callback, rootNamespace);
                            builder.CloseBlock();
                            builder.AppendLine(");");
                        }
                        break;
                }
            }

            builder.CloseBlock();
            builder.AppendLine();
        }
        private void GenerateRuntimeEventsMethod(ICodeBuilder builder, NpcBlueprint npc, string rootNamespace)
        {
            builder.OpenBlock("private void RegisterGeneratedRuntimeEvents()");
            builder.OpenBlock("if (_generatedRuntimeEventsRegistered)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine("_generatedRuntimeEventsRegistered = true;");
            builder.AppendLine();

            if (npc.EnableCustomer)
            {
                AppendGeneratedRuntimeEventSubscription(
                    builder,
                    npc,
                    NpcRuntimeEventType.CustomerUnlocked,
                    "Customer.OnUnlocked",
                    "()",
                    "OnCustomerUnlockedGenerated()",
                    rootNamespace);

                AppendGeneratedRuntimeEventSubscription(
                    builder,
                    npc,
                    NpcRuntimeEventType.CustomerDealCompleted,
                    "Customer.OnDealCompleted",
                    "()",
                    "OnCustomerDealCompletedGenerated()",
                    rootNamespace);

                AppendGeneratedRuntimeEventSubscription(
                    builder,
                    npc,
                    NpcRuntimeEventType.CustomerContractAssigned,
                    "Customer.OnContractAssigned",
                    "(payment, quantity, startTime, endTime)",
                    "OnCustomerContractAssignedGenerated(payment, quantity, startTime, endTime)",
                    rootNamespace);
            }

            if (npc.IsDealer)
            {
                AppendGeneratedRuntimeEventSubscription(
                    builder,
                    npc,
                    NpcRuntimeEventType.DealerRecruited,
                    "Dealer.OnRecruited",
                    "()",
                    "OnDealerRecruitedGenerated()",
                    rootNamespace);

                AppendGeneratedRuntimeEventSubscription(
                    builder,
                    npc,
                    NpcRuntimeEventType.DealerContractAccepted,
                    "Dealer.OnContractAccepted",
                    "()",
                    "OnDealerContractAcceptedGenerated()",
                    rootNamespace);

                AppendGeneratedRuntimeEventSubscription(
                    builder,
                    npc,
                    NpcRuntimeEventType.DealerRecommended,
                    "Dealer.OnRecommended",
                    "()",
                    "OnDealerRecommendedGenerated()",
                    rootNamespace);
            }

            AppendGeneratedRuntimeEventSubscription(
                builder,
                npc,
                NpcRuntimeEventType.RelationshipChanged,
                "Relationship.OnChanged",
                "delta",
                "OnRelationshipChangedGenerated(delta)",
                rootNamespace);

            AppendGeneratedRuntimeEventSubscription(
                builder,
                npc,
                NpcRuntimeEventType.RelationshipUnlocked,
                "Relationship.OnUnlocked",
                "(unlockType, alreadyUnlocked)",
                "OnRelationshipUnlockedGenerated(unlockType, alreadyUnlocked)",
                rootNamespace);

            builder.CloseBlock();
            builder.AppendLine();
        }

        private void AppendGeneratedRuntimeEventSubscription(
            ICodeBuilder builder,
            NpcBlueprint npc,
            NpcRuntimeEventType eventType,
            string eventPath,
            string lambdaSignature,
            string generatedHookCall,
            string rootNamespace)
        {
            builder.OpenBlock($"{eventPath} += {lambdaSignature} =>");
            foreach (var reaction in npc.EventReactions.Where(reaction => reaction.EventType == eventType))
            {
                AppendGeneratedActionInvocation(builder, reaction, rootNamespace);
            }

            builder.AppendLine($"{generatedHookCall};");
            builder.CloseBlock();
            builder.AppendLine(";");
            builder.AppendLine();
        }

        private void GenerateDialogueInjectionMethod(ICodeBuilder builder, NpcBlueprint npc, string rootNamespace)
        {
            builder.OpenBlock("private void RegisterGeneratedDialogueInjections()");
            if (npc.DialogueInjections.Count == 0)
            {
                builder.AppendLine("return;");
                builder.CloseBlock();
                builder.AppendLine();
                return;
            }

            builder.OpenBlock("if (_generatedDialogueInjectionsRegistered)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine("_generatedDialogueInjectionsRegistered = true;");
            builder.AppendLine();

            foreach (var injection in npc.DialogueInjections)
            {
                var targetNpcId = string.IsNullOrWhiteSpace(injection.TargetNpcId) ? npc.NpcId : injection.TargetNpcId;
                builder.OpenBlock($"DialogueInjector.Register(new DialogueInjection(\"{CodeFormatter.EscapeString(targetNpcId)}\", \"{CodeFormatter.EscapeString(injection.ContainerName)}\", \"{CodeFormatter.EscapeString(injection.FromNodeGuid)}\", \"{CodeFormatter.EscapeString(injection.ToNodeGuid)}\", \"{CodeFormatter.EscapeString(injection.ChoiceLabel)}\", \"{CodeFormatter.EscapeString(injection.ChoiceText)}\", () =>");
                builder.AppendLine($"ExecuteGeneratedActionForNpcId(\"{CodeFormatter.EscapeString(targetNpcId)}\", \"{CodeFormatter.EscapeString(injection.MessageText)}\", \"{CodeFormatter.EscapeString(injection.JumpToContainerName)}\", \"{CodeFormatter.EscapeString(injection.JumpToNodeLabel)}\", {injection.StopDialogueOverride.ToString().ToLowerInvariant()});");
                GlobalStateSetterWriter.AppendSetterInvocations(builder, injection.GlobalStateSetters, rootNamespace);
                builder.CloseBlock();
                builder.AppendLine("));");
            }

            builder.CloseBlock();
            builder.AppendLine();
        }
        private void GenerateActionHelpers(ICodeBuilder builder)
        {
            builder.OpenBlock("private void ExecuteGeneratedAction(string messageText, string jumpToContainerName, string jumpToNodeLabel, bool stopDialogueOverride)");
            builder.AppendLine("if (stopDialogueOverride)");
            builder.AppendLine("    Dialogue.StopOverride();");
            builder.AppendLine("if (!string.IsNullOrWhiteSpace(messageText))");
            builder.AppendLine("    SendTextMessage(messageText);");
            builder.AppendLine("if (!string.IsNullOrWhiteSpace(jumpToContainerName) && !string.IsNullOrWhiteSpace(jumpToNodeLabel))");
            builder.AppendLine("    Dialogue.JumpTo(jumpToContainerName, jumpToNodeLabel);");
            builder.CloseBlock();
            builder.AppendLine();

            builder.OpenBlock("private static void ExecuteGeneratedActionForNpcId(string npcId, string messageText, string jumpToContainerName, string jumpToNodeLabel, bool stopDialogueOverride)");
            builder.AppendLine("if (string.IsNullOrWhiteSpace(npcId))");
            builder.AppendLine("    return;");
            builder.AppendLine("var targetNpc = NPC.All.FirstOrDefault(npc => string.Equals(npc.ID, npcId, StringComparison.OrdinalIgnoreCase));");
            builder.AppendLine("if (targetNpc == null)");
            builder.AppendLine("    return;");
            builder.AppendLine("if (stopDialogueOverride)");
            builder.AppendLine("    targetNpc.Dialogue.StopOverride();");
            builder.AppendLine("if (!string.IsNullOrWhiteSpace(messageText))");
            builder.AppendLine("    targetNpc.SendTextMessage(messageText);");
            builder.AppendLine("if (!string.IsNullOrWhiteSpace(jumpToContainerName) && !string.IsNullOrWhiteSpace(jumpToNodeLabel))");
            builder.AppendLine("    targetNpc.Dialogue.JumpTo(jumpToContainerName, jumpToNodeLabel);");
            builder.CloseBlock();
            builder.AppendLine();
        }

        private void GeneratePartialHookMembers(ICodeBuilder builder)
        {
            builder.AppendLine("partial void ConfigureGeneratedPrefab(NPCPrefabBuilder builder);");
            builder.AppendLine("partial void OnAfterCreatedGenerated();");
            builder.AppendLine("partial void OnAfterLoadedGenerated();");
            builder.AppendLine("partial void OnResponseLoadedGenerated(Response response);");
            builder.AppendLine("partial void OnCustomerUnlockedGenerated();");
            builder.AppendLine("partial void OnCustomerDealCompletedGenerated();");
            builder.AppendLine("partial void OnCustomerContractAssignedGenerated(float payment, int quantity, int startTime, int endTime);");
            builder.AppendLine("partial void OnDealerRecruitedGenerated();");
            builder.AppendLine("partial void OnDealerContractAcceptedGenerated();");
            builder.AppendLine("partial void OnDealerRecommendedGenerated();");
            builder.AppendLine("partial void OnRelationshipChangedGenerated(float relationshipDelta);");
            builder.AppendLine("partial void OnRelationshipUnlockedGenerated(NPCRelationship.UnlockType unlockType, bool alreadyUnlocked);");
        }

        private static string NormalizeComboValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (value.Contains(":"))
                return value.Substring(value.LastIndexOf(':') + 1).Trim();

            return value.Trim();
        }

        private static string FormatStringArray(string? text)
        {
            var lines = (text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.None)
                .Select(line => $"\"{CodeFormatter.EscapeString(line)}\"")
                .ToList();

            if (lines.Count == 0)
                lines.Add("\"\"");

            return string.Join(", ", lines);
        }

        private static void AppendGeneratedActionInvocation(ICodeBuilder builder, NpcGeneratedActionBlueprint action, string rootNamespace)
        {
            builder.AppendLine($"ExecuteGeneratedAction(\"{CodeFormatter.EscapeString(action.MessageText)}\", \"{CodeFormatter.EscapeString(action.JumpToContainerName)}\", \"{CodeFormatter.EscapeString(action.JumpToNodeLabel)}\", {action.StopDialogueOverride.ToString().ToLowerInvariant()});");
            GlobalStateSetterWriter.AppendSetterInvocations(builder, action.GlobalStateSetters, rootNamespace);
        }

        private static string GetRootNamespace(string targetNamespace)
        {
            const string npcSuffix = ".NPCs";
            return targetNamespace.EndsWith(npcSuffix, StringComparison.Ordinal)
                ? targetNamespace[..^npcSuffix.Length]
                : targetNamespace;
        }

        private static bool ShouldGenerateRelationshipDefaults(NpcRelationshipDefaults relationshipDefaults)
        {
            return relationshipDefaults.StartingDelta != 0 ||
                   relationshipDefaults.StartsUnlocked ||
                   !string.IsNullOrWhiteSpace(relationshipDefaults.UnlockType) ||
                   relationshipDefaults.Connections.Count > 0;
        }

        private static bool ShouldGenerateInventoryDefaults(NpcInventoryDefaults inventoryDefaults)
        {
            return inventoryDefaults.EnableRandomCash ||
                   !inventoryDefaults.ClearInventoryEachNight ||
                   inventoryDefaults.StartupItems.Count > 0;
        }
    }
}
