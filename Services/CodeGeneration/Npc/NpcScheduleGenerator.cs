using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.Npc
{
    /// <summary>
    /// Generates schedule configuration code for NPCs.
    /// </summary>
    public class NpcScheduleGenerator
    {
        public void Generate(ICodeBuilder builder, NpcBlueprint npc)
        {
            if (npc.ScheduleActions == null || npc.ScheduleActions.Count == 0)
                return;

            builder.AppendComment("Generated from: Npc.ScheduleActions[]");
            builder.OpenBlock(".WithSchedule(plan =>");

            bool hasCustomerOrDealer = npc.EnableCustomer || npc.IsDealer;
            bool hasDealSignal = false;

            for (int i = 0; i < npc.ScheduleActions.Count; i++)
            {
                var action = npc.ScheduleActions[i];
                builder.AppendComment($"From: ScheduleActions[{i}] - Type: {action.ActionType}, StartTime: {action.StartTime}");

                switch (action.ActionType)
                {
                    case ScheduleActionType.EnsureDealSignal:
                        if (!hasDealSignal && hasCustomerOrDealer)
                        {
                            builder.AppendLine("    plan.EnsureDealSignal();");
                            hasDealSignal = true;
                        }
                        break;

                    case ScheduleActionType.WalkTo:
                        GenerateWalkTo(builder, action);
                        break;

                    case ScheduleActionType.StayInBuilding:
                        GenerateStayInBuilding(builder, action);
                        break;

                    case ScheduleActionType.LocationDialogue:
                        GenerateLocationDialogue(builder, action);
                        break;

                    case ScheduleActionType.LocationBased:
                        GenerateLocationBased(builder, action);
                        break;

                    case ScheduleActionType.UseVendingMachine:
                        GenerateUseVendingMachine(builder, action);
                        break;

                    case ScheduleActionType.DriveToCarPark:
                        GenerateDriveToCarPark(builder, action);
                        break;

                    case ScheduleActionType.UseATM:
                        GenerateUseATM(builder, action);
                        break;

                    case ScheduleActionType.HandleDeal:
                        if (npc.IsDealer)
                        {
                            GenerateHandleDeal(builder, action);
                        }
                        break;

                    case ScheduleActionType.SitAtSeatSet:
                        GenerateSitAtSeatSet(builder, action);
                        break;

                    case ScheduleActionType.UseSlotMachine:
                        GenerateUseSlotMachine(builder, action);
                        break;
                }
            }

            builder.CloseBlock();
            builder.AppendLine(")");
        }

        private void GenerateWalkTo(ICodeBuilder builder, NpcScheduleAction action)
        {
            var pos = CodeFormatter.FormatVector3(action.PositionX, action.PositionY, action.PositionZ);

            var parameters = new List<string>
            {
                pos,
                action.StartTime.ToString()
            };

            if (!action.FaceDestinationDirection)
                parameters.Add("faceDestinationDir: false");
            else if (action.Within != 1.0f || action.WarpIfSkipped || action.HasForward)
                parameters.Add("faceDestinationDir: true");

            if (action.Within != 1.0f)
                parameters.Add($"within: {CodeFormatter.FormatFloat(action.Within)}f");

            if (action.WarpIfSkipped)
                parameters.Add("warpIfSkipped: true");

            if (action.HasForward)
            {
                var forward = CodeFormatter.FormatVector3(action.ForwardX, action.ForwardY, action.ForwardZ);
                parameters.Add($"forward: {forward}");
            }

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: \"{CodeFormatter.EscapeString(action.ActionName)}\"");

            builder.AppendLine($"    plan.WalkTo({string.Join(", ", parameters)});");
        }

        private void GenerateStayInBuilding(ICodeBuilder builder, NpcScheduleAction action)
        {
            if (string.IsNullOrWhiteSpace(action.BuildingName))
            {
                builder.AppendLine($"    // .StayInBuilding(building, {action.StartTime}, {action.Duration})");
                return;
            }

            var buildingTypeName = NormalizeBuildingTypeName(action.BuildingName);
            var parameters = new List<string>
            {
                $"Building.Get<{buildingTypeName}>()",
                action.StartTime.ToString()
            };

            if (action.Duration > 0 && action.Duration != 60)
                parameters.Add($"durationMinutes: {action.Duration}");

            if (action.DoorIndex.HasValue)
                parameters.Add($"doorIndex: {action.DoorIndex.Value}");

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: \"{CodeFormatter.EscapeString(action.ActionName)}\"");

            builder.AppendLine($"    plan.StayInBuilding({string.Join(", ", parameters)});");
        }

        private void GenerateLocationDialogue(ICodeBuilder builder, NpcScheduleAction action)
        {
            var pos = CodeFormatter.FormatVector3(action.PositionX, action.PositionY, action.PositionZ);

            var parameters = new List<string>
            {
                pos,
                action.StartTime.ToString()
            };

            if (!action.FaceDestinationDirection)
                parameters.Add("faceDestinationDir: false");
            else if (action.Within != 1.0f || action.WarpIfSkipped || action.GreetingOverrideToEnable != -1 || action.ChoiceToEnable != -1)
                parameters.Add("faceDestinationDir: true");

            if (action.Within != 1.0f)
                parameters.Add($"within: {CodeFormatter.FormatFloat(action.Within)}f");

            if (action.WarpIfSkipped)
                parameters.Add("warpIfSkipped: true");

            if (action.GreetingOverrideToEnable != -1)
                parameters.Add($"greetingOverrideToEnable: {action.GreetingOverrideToEnable}");

            if (action.ChoiceToEnable != -1)
                parameters.Add($"choiceToEnable: {action.ChoiceToEnable}");

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: \"{CodeFormatter.EscapeString(action.ActionName)}\"");

            builder.AppendLine($"    plan.LocationDialogue({string.Join(", ", parameters)});");
        }

        private void GenerateLocationBased(ICodeBuilder builder, NpcScheduleAction action)
        {
            var pos = CodeFormatter.FormatVector3(action.PositionX, action.PositionY, action.PositionZ);
            builder.AppendLine($"    plan.LocationBased({pos}, {action.StartTime}, durationMinutes: {Math.Max(1, action.Duration)})");

            if (action.Within != 1.0f)
                builder.AppendLine($"        .Within({CodeFormatter.FormatFloat(action.Within)}f)");

            if (action.FaceDestinationDirection)
                builder.AppendLine("        .FaceDestinationDirection()");

            if (action.WarpIfSkipped)
                builder.AppendLine("        .WarpIfSkipped()");

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                builder.AppendLine($"        .Named(\"{CodeFormatter.EscapeString(action.ActionName)}\")");

            switch (action.LocationArriveBehaviour)
            {
                case LocationArriveBehaviourOption.Graffiti:
                    if (!string.IsNullOrWhiteSpace(action.GraffitiSurfaceGuid))
                        builder.AppendLine($"        .WithSpraySurface(new Guid(\"{CodeFormatter.EscapeString(action.GraffitiSurfaceGuid)}\"))");
                    else if (!string.IsNullOrWhiteSpace(action.GraffitiRegion))
                        builder.AppendLine($"        .WithSpraySurfaceInRegion(Region.{action.GraffitiRegion})");
                    builder.AppendLine("        .OnArriveGraffiti();");
                    break;

                case LocationArriveBehaviourOption.Drinking:
                    if (!string.IsNullOrWhiteSpace(action.DrinkEquippablePath))
                        builder.AppendLine($"        .WithDrink(\"{CodeFormatter.EscapeString(action.DrinkEquippablePath)}\")");
                    builder.AppendLine("        .OnArriveDrinking();");
                    break;

                case LocationArriveBehaviourOption.HoldItem:
                    if (!string.IsNullOrWhiteSpace(action.ItemEquippablePath))
                        builder.AppendLine($"        .WithItem(\"{CodeFormatter.EscapeString(action.ItemEquippablePath)}\")");
                    builder.AppendLine("        .OnArriveHoldItem();");
                    break;

                case LocationArriveBehaviourOption.SmokeBreak:
                    builder.AppendLine("        .OnArriveSmokeBreak();");
                    break;

                default:
                    builder.AppendLine("        .OnArriveNone();");
                    break;
            }
        }

        private void GenerateDriveToCarPark(ICodeBuilder builder, NpcScheduleAction action)
        {
            if (string.IsNullOrWhiteSpace(action.ParkingLotName) || string.IsNullOrWhiteSpace(action.VehicleId))
            {
                builder.AppendLine($"    // .DriveToCarParkWithCreateVehicle(parkingLot, vehicleId, {action.StartTime}, spawnPos, rotation, ParkingAlignment.{action.ParkingAlignment})");
                return;
            }

            var spawnPos = CodeFormatter.FormatVector3(action.VehicleSpawnX, action.VehicleSpawnY, action.VehicleSpawnZ);
            var rotation = $"Quaternion.Euler({CodeFormatter.FormatFloat(action.VehicleRotationX)}f, {CodeFormatter.FormatFloat(action.VehicleRotationY)}f, {CodeFormatter.FormatFloat(action.VehicleRotationZ)}f)";
            var parameters = new List<string>
            {
                $"\"{CodeFormatter.EscapeString(action.ParkingLotName)}\"",
                $"\"{CodeFormatter.EscapeString(action.VehicleId)}\"",
                action.StartTime.ToString(),
                spawnPos,
                rotation
            };

            if (action.ParkingAlignment != "FrontToKerb")
                parameters.Add($"alignment: ParkingAlignment.{action.ParkingAlignment}");

            if (action.OverrideParkingType.HasValue)
                parameters.Add($"overrideParkingType: {action.OverrideParkingType.Value.ToString().ToLowerInvariant()}");

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: \"{CodeFormatter.EscapeString(action.ActionName)}\"");

            builder.AppendLine($"    plan.DriveToCarParkWithCreateVehicle({string.Join(", ", parameters)});");
        }

        private void GenerateUseVendingMachine(ICodeBuilder builder, NpcScheduleAction action)
        {
            var parameters = new List<string>
            {
                action.StartTime.ToString()
            };

            if (!string.IsNullOrWhiteSpace(action.MachineGUID))
                parameters.Add($"machineGUID: \"{CodeFormatter.EscapeString(action.MachineGUID)}\"");

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: \"{CodeFormatter.EscapeString(action.ActionName)}\"");

            builder.AppendLine($"    plan.UseVendingMachine({string.Join(", ", parameters)});");
        }

        private void GenerateUseATM(ICodeBuilder builder, NpcScheduleAction action)
        {
            var parameters = new List<string>
            {
                action.StartTime.ToString()
            };

            if (!string.IsNullOrWhiteSpace(action.ATMGUID))
                parameters.Add($"atmGUID: \"{CodeFormatter.EscapeString(action.ATMGUID)}\"");

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: \"{CodeFormatter.EscapeString(action.ActionName)}\"");

            builder.AppendLine($"    plan.UseATM({string.Join(", ", parameters)});");
        }

        private void GenerateHandleDeal(ICodeBuilder builder, NpcScheduleAction action)
        {
            var parameters = new List<string>
            {
                action.StartTime.ToString()
            };

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: \"{CodeFormatter.EscapeString(action.ActionName)}\"");

            builder.AppendLine($"    plan.HandleDeal({string.Join(", ", parameters)});");
        }

        private void GenerateSitAtSeatSet(ICodeBuilder builder, NpcScheduleAction action)
        {
            if (string.IsNullOrWhiteSpace(action.SeatSetName) && string.IsNullOrWhiteSpace(action.SeatSetPath))
            {
                builder.AppendLine($"    // .SitAtSeatSet(seatSetName, {action.StartTime})");
                return;
            }

            var parameters = new List<string>
            {
                string.IsNullOrWhiteSpace(action.SeatSetName)
                    ? "null"
                    : $"\"{CodeFormatter.EscapeString(action.SeatSetName)}\"",
                action.StartTime.ToString()
            };

            if (action.Duration > 0 && (action.Duration != 60 || action.WarpIfSkipped || !string.IsNullOrWhiteSpace(action.ActionName) || !string.IsNullOrWhiteSpace(action.SeatSetPath)))
                parameters.Add($"durationMinutes: {action.Duration}");

            if (action.WarpIfSkipped)
                parameters.Add("warpIfSkipped: true");

            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: \"{CodeFormatter.EscapeString(action.ActionName)}\"");

            if (!string.IsNullOrWhiteSpace(action.SeatSetPath))
                parameters.Add($"seatSetPath: \"{CodeFormatter.EscapeString(action.SeatSetPath)}\"");

            builder.AppendLine($"    plan.SitAtSeatSet({string.Join(", ", parameters)});");
        }

        private void GenerateUseSlotMachine(ICodeBuilder builder, NpcScheduleAction action)
        {
            var position = CodeFormatter.FormatVector3(action.PositionX, action.PositionY, action.PositionZ);
            var optionalName = string.IsNullOrWhiteSpace(action.ActionName)
                ? string.Empty
                : $", name: \"{CodeFormatter.EscapeString(action.ActionName)}\"";

            switch (action.SlotMachineSessionMode)
            {
                case NpcGamblingSessionMode.SpinCount:
                    builder.AppendLine($"    plan.UseSlotMachineMultipleTimes({action.StartTime}, {position}, spinCount: {action.SlotMachineSpinCount}, betAmount: {action.SlotMachineBetAmount}, timeBetweenSpins: {CodeFormatter.FormatFloat(action.SlotMachineTimeBetweenSpins)}f, maxSearchDistance: {CodeFormatter.FormatFloat(action.SlotMachineMaxSearchDistance)}f{optionalName});");
                    break;

                case NpcGamblingSessionMode.UntilTime:
                    builder.AppendLine($"    plan.UseSlotMachineUntilTime({action.StartTime}, {action.SlotMachineEndTime}, {position}, betAmount: {action.SlotMachineBetAmount}, timeBetweenSpins: {CodeFormatter.FormatFloat(action.SlotMachineTimeBetweenSpins)}f, stopIfBroke: false, maxSearchDistance: {CodeFormatter.FormatFloat(action.SlotMachineMaxSearchDistance)}f{optionalName});");
                    break;

                case NpcGamblingSessionMode.UntilTimeOrBroke:
                    builder.AppendLine($"    plan.UseSlotMachineUntilTime({action.StartTime}, {action.SlotMachineEndTime}, {position}, betAmount: {action.SlotMachineBetAmount}, timeBetweenSpins: {CodeFormatter.FormatFloat(action.SlotMachineTimeBetweenSpins)}f, stopIfBroke: true, maxSearchDistance: {CodeFormatter.FormatFloat(action.SlotMachineMaxSearchDistance)}f{optionalName});");
                    break;

                case NpcGamblingSessionMode.UntilBroke:
                    builder.AppendLine($"    plan.UseSlotMachineUntilBroke({action.StartTime}, {position}, betAmount: {action.SlotMachineBetAmount}, timeBetweenSpins: {CodeFormatter.FormatFloat(action.SlotMachineTimeBetweenSpins)}f, maxSearchDistance: {CodeFormatter.FormatFloat(action.SlotMachineMaxSearchDistance)}f{optionalName});");
                    break;

                default:
                    builder.AppendLine($"    plan.UseSlotMachine({action.StartTime}, {position}, betAmount: {action.SlotMachineBetAmount}, sessionMode: GamblingSessionMode.SingleSpin, maxSearchDistance: {CodeFormatter.FormatFloat(action.SlotMachineMaxSearchDistance)}f{optionalName});");
                    break;
            }
        }

        private static string NormalizeBuildingTypeName(string buildingTypeName)
        {
            if (buildingTypeName.Contains("(") && buildingTypeName.Contains(")"))
            {
                var startParen = buildingTypeName.LastIndexOf('(');
                var endParen = buildingTypeName.LastIndexOf(')');
                if (startParen >= 0 && endParen > startParen)
                {
                    return buildingTypeName.Substring(startParen + 1, endParen - startParen - 1).Trim();
                }
            }

            return buildingTypeName;
        }
    }
}
