using System.ComponentModel;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a single action in an NPC's daily schedule.
    /// </summary>
    public class NpcScheduleAction : ObservableObject
    {
        private ScheduleActionType _actionType = ScheduleActionType.WalkTo;
        private int _startTime = 900; // 9:00 AM
        private int _duration = 60; // minutes
        private float _positionX;
        private float _positionY;
        private float _positionZ;
        private string _buildingName = string.Empty;
        private string _parkingLotName = string.Empty;
        private string _vehicleId = string.Empty;
        private bool _faceDestinationDirection;
        private string _parkingAlignment = "FrontToKerb";
        private float _vehicleSpawnX;
        private float _vehicleSpawnY;
        private float _vehicleSpawnZ;
        private float _vehicleRotationX;
        private float _vehicleRotationY;
        private float _vehicleRotationZ;
        
        // WalkTo & LocationDialogue parameters
        private float _within = 1.0f;
        private bool _warpIfSkipped;
        private float _forwardX;
        private float _forwardY;
        private float _forwardZ;
        private bool _hasForward;
        
        // LocationDialogue specific
        private int _greetingOverrideToEnable = -1;
        private int _choiceToEnable = -1;
        
        // StayInBuilding parameters
        private int? _doorIndex;
        
        // UseVendingMachine & UseATM parameters
        private string _machineGUID = string.Empty;
        private string _atmGUID = string.Empty;
        
        // DriveToCarPark parameters
        private bool? _overrideParkingType;
        
        // SitAtSeatSet parameters
        private string _seatSetName = string.Empty;
        private string _seatSetPath = string.Empty;

        // LocationBased parameters
        private LocationArriveBehaviourOption _locationArriveBehaviour = LocationArriveBehaviourOption.None;
        private string _itemEquippablePath = string.Empty;
        private string _drinkEquippablePath = string.Empty;
        private string _graffitiRegion = "Downtown";
        private string _graffitiSurfaceGuid = string.Empty;

        // Slot machine parameters
        private NpcGamblingSessionMode _slotMachineSessionMode = NpcGamblingSessionMode.SingleSpin;
        private int _slotMachineEndTime = 1700;
        private int _slotMachineBetAmount = 10;
        private int _slotMachineSpinCount = 5;
        private float _slotMachineTimeBetweenSpins = 10f;
        private float _slotMachineMaxSearchDistance = 5f;
        private bool _slotMachineStopIfBroke = true;
        
        // Custom name for actions
        private string _actionName = string.Empty;

        [JsonProperty("actionType")]
        public ScheduleActionType ActionType
        {
            get => _actionType;
            set
            {
                if (SetProperty(ref _actionType, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("startTime")]
        public int StartTime
        {
            get => _startTime;
            set
            {
                if (SetProperty(ref _startTime, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("duration")]
        public int Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        [JsonProperty("positionX")]
        public float PositionX
        {
            get => _positionX;
            set => SetProperty(ref _positionX, value);
        }

        [JsonProperty("positionY")]
        public float PositionY
        {
            get => _positionY;
            set => SetProperty(ref _positionY, value);
        }

        [JsonProperty("positionZ")]
        public float PositionZ
        {
            get => _positionZ;
            set => SetProperty(ref _positionZ, value);
        }

        [JsonProperty("buildingName")]
        public string BuildingName
        {
            get => _buildingName;
            set => SetProperty(ref _buildingName, value ?? string.Empty);
        }

        [JsonProperty("parkingLotName")]
        public string ParkingLotName
        {
            get => _parkingLotName;
            set => SetProperty(ref _parkingLotName, value ?? string.Empty);
        }

        [JsonProperty("vehicleId")]
        public string VehicleId
        {
            get => _vehicleId;
            set => SetProperty(ref _vehicleId, value ?? string.Empty);
        }

        [JsonProperty("faceDestinationDirection")]
        public bool FaceDestinationDirection
        {
            get => _faceDestinationDirection;
            set => SetProperty(ref _faceDestinationDirection, value);
        }

        [JsonProperty("parkingAlignment")]
        public string ParkingAlignment
        {
            get => _parkingAlignment;
            set => SetProperty(ref _parkingAlignment, value ?? "FrontToKerb");
        }

        [JsonProperty("vehicleSpawnX")]
        public float VehicleSpawnX
        {
            get => _vehicleSpawnX;
            set => SetProperty(ref _vehicleSpawnX, value);
        }

        [JsonProperty("vehicleSpawnY")]
        public float VehicleSpawnY
        {
            get => _vehicleSpawnY;
            set => SetProperty(ref _vehicleSpawnY, value);
        }

        [JsonProperty("vehicleSpawnZ")]
        public float VehicleSpawnZ
        {
            get => _vehicleSpawnZ;
            set => SetProperty(ref _vehicleSpawnZ, value);
        }

        [JsonProperty("vehicleRotationX")]
        public float VehicleRotationX
        {
            get => _vehicleRotationX;
            set => SetProperty(ref _vehicleRotationX, value);
        }

        [JsonProperty("vehicleRotationY")]
        public float VehicleRotationY
        {
            get => _vehicleRotationY;
            set => SetProperty(ref _vehicleRotationY, value);
        }

        [JsonProperty("vehicleRotationZ")]
        public float VehicleRotationZ
        {
            get => _vehicleRotationZ;
            set => SetProperty(ref _vehicleRotationZ, value);
        }

        [JsonProperty("within")]
        public float Within
        {
            get => _within;
            set => SetProperty(ref _within, value);
        }

        [JsonProperty("warpIfSkipped")]
        public bool WarpIfSkipped
        {
            get => _warpIfSkipped;
            set => SetProperty(ref _warpIfSkipped, value);
        }

        [JsonProperty("forwardX")]
        public float ForwardX
        {
            get => _forwardX;
            set
            {
                if (SetProperty(ref _forwardX, value))
                {
                    OnPropertyChanged(nameof(HasForward));
                }
            }
        }

        [JsonProperty("forwardY")]
        public float ForwardY
        {
            get => _forwardY;
            set
            {
                if (SetProperty(ref _forwardY, value))
                {
                    OnPropertyChanged(nameof(HasForward));
                }
            }
        }

        [JsonProperty("forwardZ")]
        public float ForwardZ
        {
            get => _forwardZ;
            set
            {
                if (SetProperty(ref _forwardZ, value))
                {
                    OnPropertyChanged(nameof(HasForward));
                }
            }
        }

        [JsonIgnore]
        public bool HasForward
        {
            get => _hasForward;
            set
            {
                if (SetProperty(ref _hasForward, value))
                {
                    if (!value)
                    {
                        ForwardX = 0;
                        ForwardY = 0;
                        ForwardZ = 0;
                    }
                }
            }
        }

        [JsonProperty("greetingOverrideToEnable")]
        public int GreetingOverrideToEnable
        {
            get => _greetingOverrideToEnable;
            set => SetProperty(ref _greetingOverrideToEnable, value);
        }

        [JsonProperty("choiceToEnable")]
        public int ChoiceToEnable
        {
            get => _choiceToEnable;
            set => SetProperty(ref _choiceToEnable, value);
        }

        [JsonProperty("doorIndex")]
        public int? DoorIndex
        {
            get => _doorIndex;
            set => SetProperty(ref _doorIndex, value);
        }

        [JsonProperty("machineGUID")]
        public string MachineGUID
        {
            get => _machineGUID;
            set => SetProperty(ref _machineGUID, value ?? string.Empty);
        }

        [JsonProperty("atmGUID")]
        public string ATMGUID
        {
            get => _atmGUID;
            set => SetProperty(ref _atmGUID, value ?? string.Empty);
        }

        [JsonProperty("overrideParkingType")]
        public bool? OverrideParkingType
        {
            get => _overrideParkingType;
            set => SetProperty(ref _overrideParkingType, value);
        }

        [JsonProperty("seatSetName")]
        public string SeatSetName
        {
            get => _seatSetName;
            set => SetProperty(ref _seatSetName, value ?? string.Empty);
        }

        [JsonProperty("seatSetPath")]
        public string SeatSetPath
        {
            get => _seatSetPath;
            set => SetProperty(ref _seatSetPath, value ?? string.Empty);
        }

        [JsonProperty("locationArriveBehaviour")]
        public LocationArriveBehaviourOption LocationArriveBehaviour
        {
            get => _locationArriveBehaviour;
            set => SetProperty(ref _locationArriveBehaviour, value);
        }

        [JsonProperty("itemEquippablePath")]
        public string ItemEquippablePath
        {
            get => _itemEquippablePath;
            set => SetProperty(ref _itemEquippablePath, value ?? string.Empty);
        }

        [JsonProperty("drinkEquippablePath")]
        public string DrinkEquippablePath
        {
            get => _drinkEquippablePath;
            set => SetProperty(ref _drinkEquippablePath, value ?? string.Empty);
        }

        [JsonProperty("graffitiRegion")]
        public string GraffitiRegion
        {
            get => _graffitiRegion;
            set => SetProperty(ref _graffitiRegion, string.IsNullOrWhiteSpace(value) ? "Downtown" : value);
        }

        [JsonProperty("graffitiSurfaceGuid")]
        public string GraffitiSurfaceGuid
        {
            get => _graffitiSurfaceGuid;
            set => SetProperty(ref _graffitiSurfaceGuid, value ?? string.Empty);
        }

        [JsonProperty("slotMachineSessionMode")]
        public NpcGamblingSessionMode SlotMachineSessionMode
        {
            get => _slotMachineSessionMode;
            set => SetProperty(ref _slotMachineSessionMode, value);
        }

        [JsonProperty("slotMachineEndTime")]
        public int SlotMachineEndTime
        {
            get => _slotMachineEndTime;
            set => SetProperty(ref _slotMachineEndTime, value);
        }

        [JsonProperty("slotMachineBetAmount")]
        public int SlotMachineBetAmount
        {
            get => _slotMachineBetAmount;
            set => SetProperty(ref _slotMachineBetAmount, value < 1 ? 1 : value);
        }

        [JsonProperty("slotMachineSpinCount")]
        public int SlotMachineSpinCount
        {
            get => _slotMachineSpinCount;
            set => SetProperty(ref _slotMachineSpinCount, value < 1 ? 1 : value);
        }

        [JsonProperty("slotMachineTimeBetweenSpins")]
        public float SlotMachineTimeBetweenSpins
        {
            get => _slotMachineTimeBetweenSpins;
            set => SetProperty(ref _slotMachineTimeBetweenSpins, value <= 0f ? 1f : value);
        }

        [JsonProperty("slotMachineMaxSearchDistance")]
        public float SlotMachineMaxSearchDistance
        {
            get => _slotMachineMaxSearchDistance;
            set => SetProperty(ref _slotMachineMaxSearchDistance, value <= 0f ? 0.5f : value);
        }

        [JsonProperty("slotMachineStopIfBroke")]
        public bool SlotMachineStopIfBroke
        {
            get => _slotMachineStopIfBroke;
            set => SetProperty(ref _slotMachineStopIfBroke, value);
        }

        [JsonProperty("actionName")]
        public string ActionName
        {
            get => _actionName;
            set => SetProperty(ref _actionName, value ?? string.Empty);
        }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(ActionName)
            ? $"{StartTime:D4} - {ActionType}"
            : $"{StartTime:D4} - {ActionName}";

        public NpcScheduleAction DeepCopy()
        {
            return new NpcScheduleAction
            {
                ActionType = ActionType,
                StartTime = StartTime,
                Duration = Duration,
                PositionX = PositionX,
                PositionY = PositionY,
                PositionZ = PositionZ,
                BuildingName = BuildingName,
                ParkingLotName = ParkingLotName,
                VehicleId = VehicleId,
                FaceDestinationDirection = FaceDestinationDirection,
                ParkingAlignment = ParkingAlignment,
                VehicleSpawnX = VehicleSpawnX,
                VehicleSpawnY = VehicleSpawnY,
                VehicleSpawnZ = VehicleSpawnZ,
                VehicleRotationX = VehicleRotationX,
                VehicleRotationY = VehicleRotationY,
                VehicleRotationZ = VehicleRotationZ,
                Within = Within,
                WarpIfSkipped = WarpIfSkipped,
                ForwardX = ForwardX,
                ForwardY = ForwardY,
                ForwardZ = ForwardZ,
                HasForward = HasForward,
                GreetingOverrideToEnable = GreetingOverrideToEnable,
                ChoiceToEnable = ChoiceToEnable,
                DoorIndex = DoorIndex,
                MachineGUID = MachineGUID,
                ATMGUID = ATMGUID,
                OverrideParkingType = OverrideParkingType,
                SeatSetName = SeatSetName,
                SeatSetPath = SeatSetPath,
                LocationArriveBehaviour = LocationArriveBehaviour,
                ItemEquippablePath = ItemEquippablePath,
                DrinkEquippablePath = DrinkEquippablePath,
                GraffitiRegion = GraffitiRegion,
                GraffitiSurfaceGuid = GraffitiSurfaceGuid,
                SlotMachineSessionMode = SlotMachineSessionMode,
                SlotMachineEndTime = SlotMachineEndTime,
                SlotMachineBetAmount = SlotMachineBetAmount,
                SlotMachineSpinCount = SlotMachineSpinCount,
                SlotMachineTimeBetweenSpins = SlotMachineTimeBetweenSpins,
                SlotMachineMaxSearchDistance = SlotMachineMaxSearchDistance,
                SlotMachineStopIfBroke = SlotMachineStopIfBroke,
                ActionName = ActionName
            };
        }
    }

    public enum LocationArriveBehaviourOption
    {
        None,
        SmokeBreak,
        Graffiti,
        Drinking,
        HoldItem
    }

    public enum NpcGamblingSessionMode
    {
        SingleSpin,
        SpinCount,
        UntilTime,
        UntilBroke,
        UntilTimeOrBroke
    }

    /// <summary>
    /// Types of schedule actions supported by S1API.
    /// </summary>
    public enum ScheduleActionType
    {
        [Description("Walk to Location")]
        WalkTo,

        [Description("Stay in Building")]
        StayInBuilding,

        [Description("Location Dialogue")]
        LocationDialogue,

        [Description("Location Based Action")]
        LocationBased,

        [Description("Use Vending Machine")]
        UseVendingMachine,

        [Description("Drive to Car Park")]
        DriveToCarPark,

        [Description("Use ATM")]
        UseATM,

        [Description("Handle Deal (Dealer Only)")]
        HandleDeal,

        [Description("Ensure Deal Signal (Customer Only)")]
        EnsureDealSignal,

        [Description("Sit at Seat Set")]
        SitAtSeatSet,

        [Description("Use Slot Machine")]
        UseSlotMachine
    }
}
