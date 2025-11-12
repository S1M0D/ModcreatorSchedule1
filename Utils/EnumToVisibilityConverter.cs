using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace Schedule1ModdingTool.Utils
{
    /// <summary>
    /// Converter that shows visibility when an enum value matches a parameter
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Get stack trace to see which element is calling this
            var stackTrace = new System.Diagnostics.StackTrace();
            var callingMethod = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
            var callingType = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
            
            Debug.WriteLine($"[EnumToVisibilityConverter] Convert called - value: {value} (type: {value?.GetType().Name}), parameter: {parameter}");
            Debug.WriteLine($"[EnumToVisibilityConverter] Called from: {callingType}.{callingMethod}");
            
            if (value == null || parameter == null)
            {
                Debug.WriteLine("[EnumToVisibilityConverter] Returning Collapsed - value or parameter is null");
                return System.Windows.Visibility.Collapsed;
            }

            // Try to parse as enum first (more reliable than string comparison)
            if (value is Enum enumValue)
            {
                string enumParamStr = parameter.ToString() ?? "";
                Debug.WriteLine($"[EnumToVisibilityConverter] Value is Enum: {enumValue}, Parameter string: '{enumParamStr}'");
                
                // Support multiple values separated by pipe (e.g., "XP|Money")
                if (enumParamStr.Contains("|"))
                {
                    var values = enumParamStr.Split('|');
                    foreach (var param in values)
                    {
                        if (Enum.TryParse(enumValue.GetType(), param.Trim(), true, out var parsedEnum))
                        {
                            Debug.WriteLine($"[EnumToVisibilityConverter] Comparing {enumValue} with {parsedEnum}");
                            if (enumValue.Equals(parsedEnum))
                            {
                                Debug.WriteLine("[EnumToVisibilityConverter] Match found! Returning Visible");
                                return System.Windows.Visibility.Visible;
                            }
                        }
                    }
                    Debug.WriteLine("[EnumToVisibilityConverter] No match in pipe-separated values, returning Collapsed");
                    return System.Windows.Visibility.Collapsed;
                }
                
                // Single value comparison
                if (Enum.TryParse(enumValue.GetType(), enumParamStr, true, out var parsedValue))
                {
                    bool matches = enumValue.Equals(parsedValue);
                    Debug.WriteLine($"[EnumToVisibilityConverter] Single value comparison: {enumValue} == {parsedValue}? {matches}");
                    var result = matches 
                        ? System.Windows.Visibility.Visible 
                        : System.Windows.Visibility.Collapsed;
                    Debug.WriteLine($"[EnumToVisibilityConverter] Returning {result}");
                    return result;
                }
                else
                {
                    Debug.WriteLine($"[EnumToVisibilityConverter] Failed to parse '{enumParamStr}' as enum type {enumValue.GetType().Name}");
                }
            }

            // Fallback to string comparison for non-enum types
            string valueStr = value.ToString() ?? "";
            string paramStr = parameter.ToString() ?? "";
            Debug.WriteLine($"[EnumToVisibilityConverter] Fallback to string comparison - valueStr: '{valueStr}', paramStr: '{paramStr}'");
            
            // Support multiple values separated by pipe (e.g., "XP|Money")
            if (paramStr.Contains("|"))
            {
                var values = paramStr.Split('|');
                foreach (var param in values)
                {
                    if (valueStr.Equals(param.Trim(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        Debug.WriteLine($"[EnumToVisibilityConverter] String match found in pipe-separated values, returning Visible");
                        return System.Windows.Visibility.Visible;
                    }
                }
                Debug.WriteLine("[EnumToVisibilityConverter] No string match in pipe-separated values, returning Collapsed");
                return System.Windows.Visibility.Collapsed;
            }
            
            bool stringMatches = valueStr.Equals(paramStr, StringComparison.InvariantCultureIgnoreCase);
            var stringResult = stringMatches 
                ? System.Windows.Visibility.Visible 
                : System.Windows.Visibility.Collapsed;
            Debug.WriteLine($"[EnumToVisibilityConverter] String comparison result: '{valueStr}' == '{paramStr}'? {stringMatches}, returning {stringResult}");
            return stringResult;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse converter that shows visibility when an enum value does NOT match a parameter
    /// </summary>
    public class InverseEnumToVisibilityConverter : IValueConverter
    {
        private readonly EnumToVisibilityConverter _baseConverter = new EnumToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = _baseConverter.Convert(value, targetType, parameter, culture);
            if (result is System.Windows.Visibility visibility)
            {
                return visibility == System.Windows.Visibility.Visible 
                    ? System.Windows.Visibility.Collapsed 
                    : System.Windows.Visibility.Visible;
            }
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that shows Quest Entry fields when TriggerType is QuestEventTrigger AND TargetAction starts with "QuestEntry."
    /// </summary>
    public class QuestEntryVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"[QuestEntryVisibilityConverter] Convert called with {values?.Length ?? 0} values");
            
            if (values == null || values.Length < 2)
            {
                Debug.WriteLine("[QuestEntryVisibilityConverter] Returning Collapsed - values is null or length < 2");
                return System.Windows.Visibility.Collapsed;
            }

            // Check TriggerType
            if (values[0] == null)
            {
                Debug.WriteLine("[QuestEntryVisibilityConverter] Returning Collapsed - values[0] is null");
                return System.Windows.Visibility.Collapsed;
            }

            string triggerTypeStr = values[0].ToString() ?? "";
            Debug.WriteLine($"[QuestEntryVisibilityConverter] TriggerType: '{triggerTypeStr}'");
            
            if (!triggerTypeStr.Equals("QuestEventTrigger", StringComparison.InvariantCultureIgnoreCase))
            {
                Debug.WriteLine($"[QuestEntryVisibilityConverter] Returning Collapsed - TriggerType '{triggerTypeStr}' != 'QuestEventTrigger'");
                return System.Windows.Visibility.Collapsed;
            }

            // Check TargetAction starts with "QuestEntry."
            Debug.WriteLine($"[QuestEntryVisibilityConverter] TargetAction (values[1]): {values[1]} (type: {values[1]?.GetType().Name})");
            
            if (values[1] is string targetAction && !string.IsNullOrWhiteSpace(targetAction))
            {
                Debug.WriteLine($"[QuestEntryVisibilityConverter] TargetAction string: '{targetAction}'");
                if (targetAction.StartsWith("QuestEntry.", StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.WriteLine("[QuestEntryVisibilityConverter] TargetAction starts with 'QuestEntry.', returning Visible");
                    return System.Windows.Visibility.Visible;
                }
                else
                {
                    Debug.WriteLine($"[QuestEntryVisibilityConverter] TargetAction '{targetAction}' does NOT start with 'QuestEntry.', returning Collapsed");
                }
            }
            else
            {
                Debug.WriteLine("[QuestEntryVisibilityConverter] TargetAction is null/empty or not a string, returning Collapsed");
            }

            Debug.WriteLine("[QuestEntryVisibilityConverter] Returning Collapsed (final fallback)");
            return System.Windows.Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

