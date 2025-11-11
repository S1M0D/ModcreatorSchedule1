using System.Linq;
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

            builder.AppendComment("🔧 Generated from: Npc.ScheduleActions[] (daily schedule with 8 action types)");
            builder.OpenBlock(".WithSchedule(plan =>");

            bool hasCustomerOrDealer = npc.EnableCustomer || npc.IsDealer;
            bool hasDealSignal = false;

            for (int i = 0; i < npc.ScheduleActions.Count; i++)
            {
                var action = npc.ScheduleActions[i];
                builder.AppendComment($"🔧 From: ScheduleActions[{i}] - Type: {action.ActionType}, StartTime: {action.StartTime}");

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
                }
            }

            builder.CloseBlock();
            builder.AppendLine(")");
        }

        private void GenerateWalkTo(ICodeBuilder builder, NpcScheduleAction action)
        {
            var pos = CodeFormatter.FormatVector3(action.PositionX, action.PositionY, action.PositionZ);
            
            var parameters = new System.Collections.Generic.List<string>();
            parameters.Add($"{pos}");
            parameters.Add($"{action.StartTime}");
            
            if (!action.FaceDestinationDirection)
                parameters.Add("faceDestinationDir: false");
            else if (action.Within != 1.0f || action.WarpIfSkipped || action.HasForward)
                parameters.Add("faceDestinationDir: true");
            
            if (action.Within != 1.0f)
                parameters.Add($"within: {action.Within}f");
            
            if (action.WarpIfSkipped)
                parameters.Add("warpIfSkipped: true");
            
            if (action.HasForward)
            {
                var forward = CodeFormatter.FormatVector3(action.ForwardX, action.ForwardY, action.ForwardZ);
                parameters.Add($"forward: {forward}");
            }
            
            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: {CodeFormatter.EscapeString(action.ActionName)}");
            
            builder.AppendLine($"    plan.WalkTo({string.Join(", ", parameters)});");
        }

        private void GenerateStayInBuilding(ICodeBuilder builder, NpcScheduleAction action)
        {
            if (string.IsNullOrWhiteSpace(action.BuildingName))
            {
                // If no building specified, generate a comment
                builder.AppendLine($"    // .StayInBuilding(building, {action.StartTime}, {action.Duration})");
                return;
            }

            // BuildingName is a type name (e.g., "NorthApartments"), not a string literal
            var buildingTypeName = action.BuildingName;

            // Handle case where BuildingName might be "Display Name (TypeName)" format
            if (buildingTypeName.Contains("(") && buildingTypeName.Contains(")"))
            {
                // Extract content from parentheses: "Apartment Building (ApartmentBuilding)" → "ApartmentBuilding"
                var startParen = buildingTypeName.LastIndexOf('(');
                var endParen = buildingTypeName.LastIndexOf(')');
                if (startParen >= 0 && endParen > startParen)
                {
                    buildingTypeName = buildingTypeName.Substring(startParen + 1, endParen - startParen - 1).Trim();
                }
            }

            var parameters = new System.Collections.Generic.List<string>();
            parameters.Add($"Building.Get<{buildingTypeName}>()");
            parameters.Add($"{action.StartTime}");
            
            if (action.Duration > 0 && action.Duration != 60)
                parameters.Add($"durationMinutes: {action.Duration}");
            else if (action.Duration > 0)
                parameters.Add($"{action.Duration}");
            
            if (action.DoorIndex.HasValue)
                parameters.Add($"doorIndex: {action.DoorIndex.Value}");
            
            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: {CodeFormatter.EscapeString(action.ActionName)}");
            
            builder.AppendLine($"    plan.StayInBuilding({string.Join(", ", parameters)});");
        }

        private void GenerateLocationDialogue(ICodeBuilder builder, NpcScheduleAction action)
        {
            var pos = CodeFormatter.FormatVector3(action.PositionX, action.PositionY, action.PositionZ);
            
            var parameters = new System.Collections.Generic.List<string>();
            parameters.Add($"{pos}");
            parameters.Add($"{action.StartTime}");
            
            if (!action.FaceDestinationDirection)
                parameters.Add("faceDestinationDir: false");
            else if (action.Within != 1.0f || action.WarpIfSkipped || action.GreetingOverrideToEnable != -1 || action.ChoiceToEnable != -1)
                parameters.Add("faceDestinationDir: true");
            
            if (action.Within != 1.0f)
                parameters.Add($"within: {action.Within}f");
            
            if (action.WarpIfSkipped)
                parameters.Add("warpIfSkipped: true");
            
            if (action.GreetingOverrideToEnable != -1)
                parameters.Add($"greetingOverrideToEnable: {action.GreetingOverrideToEnable}");
            
            if (action.ChoiceToEnable != -1)
                parameters.Add($"choiceToEnable: {action.ChoiceToEnable}");
            
            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: {CodeFormatter.EscapeString(action.ActionName)}");
            
            builder.AppendLine($"    plan.LocationDialogue({string.Join(", ", parameters)});");
        }

        private void GenerateDriveToCarPark(ICodeBuilder builder, NpcScheduleAction action)
        {
            if (string.IsNullOrWhiteSpace(action.ParkingLotName) || string.IsNullOrWhiteSpace(action.VehicleId))
            {
                // If incomplete, generate a comment
                builder.AppendLine($"    // .DriveToCarParkWithCreateVehicle(parkingLot, vehicleId, {action.StartTime}, spawnPos, rotation, ParkingAlignment.{action.ParkingAlignment})");
                return;
            }

            var safeParkingLot = CodeFormatter.EscapeString(action.ParkingLotName);
            var safeVehicle = CodeFormatter.EscapeString(action.VehicleId);
            var spawnPos = CodeFormatter.FormatVector3(action.VehicleSpawnX, action.VehicleSpawnY, action.VehicleSpawnZ);
            var rotation = $"Quaternion.Euler({action.VehicleRotationX}f, {action.VehicleRotationY}f, {action.VehicleRotationZ}f)";

            var parameters = new System.Collections.Generic.List<string>();
            parameters.Add($"\"{safeParkingLot}\"");
            parameters.Add($"\"{safeVehicle}\"");
            parameters.Add($"{action.StartTime}");
            parameters.Add($"{spawnPos}");
            parameters.Add($"{rotation}");
            
            if (action.ParkingAlignment != "FrontToKerb")
                parameters.Add($"alignment: ParkingAlignment.{action.ParkingAlignment}");
            
            if (action.OverrideParkingType.HasValue)
                parameters.Add($"overrideParkingType: {action.OverrideParkingType.Value.ToString().ToLower()}");
            
            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: {CodeFormatter.EscapeString(action.ActionName)}");

            builder.AppendLine($"    plan.DriveToCarParkWithCreateVehicle({string.Join(", ", parameters)});");
        }

        private void GenerateUseVendingMachine(ICodeBuilder builder, NpcScheduleAction action)
        {
            var parameters = new System.Collections.Generic.List<string>();
            parameters.Add($"{action.StartTime}");
            
            if (!string.IsNullOrWhiteSpace(action.MachineGUID))
                parameters.Add($"machineGUID: {CodeFormatter.EscapeString(action.MachineGUID)}");
            
            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: {CodeFormatter.EscapeString(action.ActionName)}");
            
            builder.AppendLine($"    plan.UseVendingMachine({string.Join(", ", parameters)});");
        }

        private void GenerateUseATM(ICodeBuilder builder, NpcScheduleAction action)
        {
            var parameters = new System.Collections.Generic.List<string>();
            parameters.Add($"{action.StartTime}");
            
            if (!string.IsNullOrWhiteSpace(action.ATMGUID))
                parameters.Add($"atmGUID: {CodeFormatter.EscapeString(action.ATMGUID)}");
            
            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: {CodeFormatter.EscapeString(action.ActionName)}");
            
            builder.AppendLine($"    plan.UseATM({string.Join(", ", parameters)});");
        }

        private void GenerateHandleDeal(ICodeBuilder builder, NpcScheduleAction action)
        {
            var parameters = new System.Collections.Generic.List<string>();
            parameters.Add($"{action.StartTime}");
            
            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: {CodeFormatter.EscapeString(action.ActionName)}");
            
            builder.AppendLine($"    plan.HandleDeal({string.Join(", ", parameters)});");
        }

        private void GenerateSitAtSeatSet(ICodeBuilder builder, NpcScheduleAction action)
        {
            if (string.IsNullOrWhiteSpace(action.SeatSetName))
            {
                builder.AppendLine($"    // .SitAtSeatSet(seatSetName, {action.StartTime})");
                return;
            }

            var safeSeatSet = CodeFormatter.EscapeString(action.SeatSetName);
            var parameters = new System.Collections.Generic.List<string>();
            parameters.Add($"\"{safeSeatSet}\"");
            parameters.Add($"{action.StartTime}");
            
            if (action.WarpIfSkipped)
                parameters.Add("warpIfSkipped: true");
            
            if (!string.IsNullOrWhiteSpace(action.ActionName))
                parameters.Add($"name: {CodeFormatter.EscapeString(action.ActionName)}");
            
            builder.AppendLine($"    plan.SitAtSeatSet({string.Join(", ", parameters)});");
        }
    }
}
