using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Provides curated equippable presets for common Schedule One item presentations.
    /// </summary>
    public static class ItemEquippablePresetService
    {
        public static IReadOnlyList<ItemEquippablePreset> Presets { get; } = new[]
        {
            new ItemEquippablePreset
            {
                Name = "Small Tool",
                EquippableType = EquippableTypeOption.Viewmodel,
                PositionX = 0.2f,
                PositionY = -0.15f,
                PositionZ = 0.3f,
                RotationX = 0f,
                RotationY = 0f,
                RotationZ = 0f,
                ScaleX = 1f,
                ScaleY = 1f,
                ScaleZ = 1f,
                AvatarEquippableAssetPath = "avatar/equippables/Hammer",
                AvatarHand = AvatarHandOption.Right,
                AvatarAnimationTrigger = "RightArm_Hold_ClosedHand"
            },
            new ItemEquippablePreset
            {
                Name = "Knife Style",
                EquippableType = EquippableTypeOption.Viewmodel,
                PositionX = 0.22f,
                PositionY = -0.16f,
                PositionZ = 0.34f,
                RotationX = 0f,
                RotationY = 0f,
                RotationZ = 0f,
                ScaleX = 1f,
                ScaleY = 1f,
                ScaleZ = 1f,
                AvatarEquippableAssetPath = "avatar/equippables/Knife",
                AvatarHand = AvatarHandOption.Right,
                AvatarAnimationTrigger = "RightArm_Hold_ClosedHand"
            },
            new ItemEquippablePreset
            {
                Name = "Bottle / Drink",
                EquippableType = EquippableTypeOption.Viewmodel,
                PositionX = 0.18f,
                PositionY = -0.18f,
                PositionZ = 0.28f,
                RotationX = 0f,
                RotationY = 0f,
                RotationZ = 0f,
                ScaleX = 1f,
                ScaleY = 1f,
                ScaleZ = 1f,
                AvatarEquippableAssetPath = "avatar/equippables/Beer",
                AvatarHand = AvatarHandOption.Right,
                AvatarAnimationTrigger = "RightArm_Hold_ClosedHand"
            },
            new ItemEquippablePreset
            {
                Name = "Phone Raised",
                EquippableType = EquippableTypeOption.Viewmodel,
                PositionX = 0.15f,
                PositionY = -0.1f,
                PositionZ = 0.24f,
                RotationX = 12f,
                RotationY = 0f,
                RotationZ = 0f,
                ScaleX = 1f,
                ScaleY = 1f,
                ScaleZ = 1f,
                AvatarEquippableAssetPath = "avatar/equippables/PhoneRaised",
                AvatarHand = AvatarHandOption.Right,
                AvatarAnimationTrigger = "RightArm_Hold_ClosedHand"
            },
            new ItemEquippablePreset
            {
                Name = "Basic Holdable",
                EquippableType = EquippableTypeOption.Basic,
                PositionX = 0f,
                PositionY = 0f,
                PositionZ = 0f,
                RotationX = 0f,
                RotationY = 0f,
                RotationZ = 0f,
                ScaleX = 1f,
                ScaleY = 1f,
                ScaleZ = 1f,
                AvatarEquippableAssetPath = string.Empty,
                AvatarHand = AvatarHandOption.Right,
                AvatarAnimationTrigger = "RightArm_Hold_ClosedHand"
            }
        };
    }

    public class ItemEquippablePreset
    {
        public string Name { get; set; } = string.Empty;
        public EquippableTypeOption EquippableType { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float ScaleX { get; set; } = 1f;
        public float ScaleY { get; set; } = 1f;
        public float ScaleZ { get; set; } = 1f;
        public string AvatarEquippableAssetPath { get; set; } = string.Empty;
        public AvatarHandOption AvatarHand { get; set; } = AvatarHandOption.Right;
        public string AvatarAnimationTrigger { get; set; } = "RightArm_Hold_ClosedHand";
    }
}
