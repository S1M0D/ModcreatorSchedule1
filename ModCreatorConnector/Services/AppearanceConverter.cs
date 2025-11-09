using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MelonLoader;
using S1API.Avatar;

namespace ModCreatorConnector.Services
{
    /// <summary>
    /// Converts JSON-serialized NpcAppearanceSettings to Unity AvatarSettings.
    /// </summary>
    public static class AppearanceConverter
    {
        /// <summary>
        /// Converts JSON string to AvatarSettings wrapper.
        /// </summary>
        /// <param name="json">JSON string containing appearance data</param>
        /// <param name="baseSettings">Optional base settings to copy from (preserves default layers). If null, creates new settings.</param>
        public static AvatarSettings ConvertJsonToAvatarSettings(string json, AvatarSettings? baseSettings = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

            var jobj = JObject.Parse(json);
            
            // Always create a fresh AvatarSettings to avoid reference issues
            var avatarSettings = AvatarSettings.Create();

            // Basic properties - use JSON values or fall back to baseSettings defaults
            avatarSettings.Gender = GetFloat(jobj, "gender", baseSettings?.Gender ?? 0.5f);
            avatarSettings.Height = GetFloat(jobj, "height", baseSettings?.Height ?? 1.0f);
            avatarSettings.Weight = GetFloat(jobj, "weight", baseSettings?.Weight ?? 0.5f);
            var skinColorJson = GetString(jobj, "skinColor", null);
            avatarSettings.SkinColor = skinColorJson != null 
                ? HexToColor(skinColorJson) 
                : (baseSettings?.SkinColor ?? new Color32(150, 120, 95, 255));

            // Hair properties
            avatarSettings.HairPath = GetString(jobj, "hairPath", baseSettings?.HairPath ?? string.Empty);
            var hairColorJson = GetString(jobj, "hairColor", null);
            avatarSettings.HairColor = hairColorJson != null 
                ? HexToColor(hairColorJson) 
                : (baseSettings?.HairColor ?? Color.black);

            // Eye properties
            var leftEyeLidColorJson = GetString(jobj, "leftEyeLidColor", null);
            avatarSettings.LeftEyeLidColor = leftEyeLidColorJson != null 
                ? HexToColor(leftEyeLidColorJson) 
                : (baseSettings?.LeftEyeLidColor ?? avatarSettings.SkinColor);
            
            var rightEyeLidColorJson = GetString(jobj, "rightEyeLidColor", null);
            avatarSettings.RightEyeLidColor = rightEyeLidColorJson != null 
                ? HexToColor(rightEyeLidColorJson) 
                : (baseSettings?.RightEyeLidColor ?? avatarSettings.SkinColor);
            
            var eyeBallTintJson = GetString(jobj, "eyeBallTint", null);
            avatarSettings.EyeBallTint = eyeBallTintJson != null 
                ? HexToColor(eyeBallTintJson) 
                : (baseSettings?.EyeBallTint ?? Color.white);
            
            avatarSettings.PupilDilation = GetFloat(jobj, "pupilDilation", baseSettings?.PupilDilation ?? 0.5f);
            avatarSettings.EyeballMaterialIdentifier = GetString(jobj, "eyeballMaterialId", baseSettings?.EyeballMaterialIdentifier ?? "Default");

            // Eyebrow properties
            avatarSettings.EyebrowScale = GetFloat(jobj, "eyebrowScale", baseSettings?.EyebrowScale ?? 1.0f);
            avatarSettings.EyebrowThickness = GetFloat(jobj, "eyebrowThickness", baseSettings?.EyebrowThickness ?? 1.0f);
            avatarSettings.EyebrowRestingHeight = GetFloat(jobj, "eyebrowRestingHeight", baseSettings?.EyebrowRestingHeight ?? 0.0f);
            avatarSettings.EyebrowRestingAngle = GetFloat(jobj, "eyebrowRestingAngle", baseSettings?.EyebrowRestingAngle ?? 0.0f);

            // Eye lid configurations
            avatarSettings.LeftEyeRestingState = new AvatarSettings.EyeLidConfiguration
            {
                TopLidOpen = GetFloat(jobj, "leftEyeTop", baseSettings?.LeftEyeRestingState?.TopLidOpen ?? 0.5f),
                BottomLidOpen = GetFloat(jobj, "leftEyeBottom", baseSettings?.LeftEyeRestingState?.BottomLidOpen ?? 0.5f)
            };

            avatarSettings.RightEyeRestingState = new AvatarSettings.EyeLidConfiguration
            {
                TopLidOpen = GetFloat(jobj, "rightEyeTop", baseSettings?.RightEyeRestingState?.TopLidOpen ?? 0.5f),
                BottomLidOpen = GetFloat(jobj, "rightEyeBottom", baseSettings?.RightEyeRestingState?.BottomLidOpen ?? 0.5f)
            };

            // Create lists for layers
            var faceLayerList = new List<AvatarSettings.LayerSetting>();
            var bodyLayerList = new List<AvatarSettings.LayerSetting>();
            var accessoryList = new List<AvatarSettings.AccessorySetting>();

            // Face layers - always replace if the property exists in JSON (even if empty array)
            var faceLayers = jobj["faceLayers"] as JArray;
            if (faceLayers != null)
            {
                // Process layers from JSON
                foreach (var layer in faceLayers)
                {
                    var path = GetString(layer, "path", string.Empty);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        var color = HexToColor(GetString(layer, "color", "#FFFFFFFF"));
                        faceLayerList.Add(new AvatarSettings.LayerSetting
                        {
                            LayerPath = path,
                            LayerTint = color
                        });
                    }
                }
            }
            else if (baseSettings != null)
            {
                // Preserve existing layers from baseSettings if JSON doesn't have faceLayers property
                faceLayerList.AddRange(baseSettings.GetFaceLayers());
            }

            // Body layers - always replace if the property exists in JSON (even if empty array)
            var bodyLayers = jobj["bodyLayers"] as JArray;
            if (bodyLayers != null)
            {
                // Process layers from JSON
                foreach (var layer in bodyLayers)
                {
                    var path = GetString(layer, "path", string.Empty);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        bodyLayerList.Add(new AvatarSettings.LayerSetting
                        {
                            LayerPath = path,
                            LayerTint = HexToColor(GetString(layer, "color", "#FFFFFFFF"))
                        });
                    }
                }
            }
            else if (baseSettings != null)
            {
                // Preserve existing layers from baseSettings if JSON doesn't have bodyLayers property
                bodyLayerList.AddRange(baseSettings.GetBodyLayers());
            }

            // Accessory layers - always replace if the property exists in JSON (even if empty array)
            var accessoryLayers = jobj["accessoryLayers"] as JArray;
            if (accessoryLayers != null)
            {
                // Process layers from JSON
                foreach (var layer in accessoryLayers)
                {
                    var path = GetString(layer, "path", string.Empty);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        accessoryList.Add(new AvatarSettings.AccessorySetting
                        {
                            Path = path,
                            Color = HexToColor(GetString(layer, "color", "#FFFFFFFF"))
                        });
                    }
                }
            }
            else if (baseSettings != null)
            {
                // Preserve existing layers from baseSettings if JSON doesn't have accessoryLayers property
                accessoryList.AddRange(baseSettings.GetAccessories());
            }

            // Set layers using the wrapper methods
            avatarSettings.SetFaceLayers(faceLayerList);
            avatarSettings.SetBodyLayers(bodyLayerList);
            avatarSettings.SetAccessories(accessoryList);

            MelonLogger.Msg($"AppearanceConverter: Created settings with {faceLayerList.Count} face layers, {bodyLayerList.Count} body layers, {accessoryList.Count} accessories");

            return avatarSettings;
        }

        private static float GetFloat(JToken token, string propertyName, float defaultValue)
        {
            var value = token[propertyName];
            if (value == null)
                return defaultValue;

            return value.Type == JTokenType.Float || value.Type == JTokenType.Integer
                ? value.Value<float>()
                : defaultValue;
        }

        private static string GetString(JToken token, string propertyName, string defaultValue)
        {
            var value = token[propertyName];
            return value?.Value<string>() ?? defaultValue;
        }

        private static Color HexToColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Color.white;

            // Remove # if present
            hex = hex.TrimStart('#');

            // Parse hex string
            if (hex.Length == 8)
            {
                // ARGB format
                var a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                var r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color32(r, g, b, a);
            }
            else if (hex.Length == 6)
            {
                // RGB format (assume opaque)
                var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color32(r, g, b, 255);
            }

            return Color.white;
        }
    }
}
