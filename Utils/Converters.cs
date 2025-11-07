using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace Schedule1ModdingTool.Utils
{
    /// <summary>
    /// Converter that returns Collapsed visibility when count is 0
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for boolean to visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Converter for inverting boolean values
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converter for string to visibility (visible if not null/empty)
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for icon key string to PackIconKind enum value
    /// </summary>
    public class IconKeyToPackIconKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string iconKey && !string.IsNullOrEmpty(iconKey))
            {
                // Map icon keys to MaterialDesign PackIconKind values
                return iconKey switch
                {
                    "PlusIcon" => PackIconKind.Plus,
                    "FolderIcon" => PackIconKind.Folder,
                    "PadlockIcon" => PackIconKind.Lock,
                    "GlobeIcon" => PackIconKind.Web,
                    "SearchIcon" => PackIconKind.Magnify,
                    "FilterIcon" => PackIconKind.Filter,
                    "SortIcon" => PackIconKind.Sort,
                    "SettingsIcon" => PackIconKind.Cog,
                    "BuildIcon" => PackIconKind.Hammer,
                    "SaveIcon" => PackIconKind.ContentSave,
                    "DocumentIcon" => PackIconKind.FileDocument,
                    "FolderOpenIcon" => PackIconKind.FolderOpen,
                    "CubeIcon" => PackIconKind.Cube,
                    "QuestIcon" => PackIconKind.ClipboardCheck,
                    "NPCsIcon" => PackIconKind.AccountGroup,
                    "PhoneAppsIcon" => PackIconKind.Cellphone,
                    "ItemsIcon" => PackIconKind.Package,
                    "NewProjectIcon" => PackIconKind.FileDocumentPlus,
                    "SaveAsIcon" => PackIconKind.ContentSaveAll,
                    "CodeRefreshIcon" => PackIconKind.FileRefresh,
                    "DeleteIcon" => PackIconKind.Delete,
                    "EditIcon" => PackIconKind.Pencil,
                    "RefreshIcon" => PackIconKind.Refresh,
                    "ExportIcon" => PackIconKind.Download,
                    "CopyIcon" => PackIconKind.ContentCopy,
                    "EyeIcon" => PackIconKind.Eye,
                    _ => PackIconKind.QuestionMark
                };
            }
            return PackIconKind.QuestionMark;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for icon key string to Geometry resource (deprecated - kept for backward compatibility)
    /// </summary>
    public class IconKeyToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string iconKey && !string.IsNullOrEmpty(iconKey))
            {
                var geometry = Application.Current.TryFindResource(iconKey) as Geometry;
                return geometry ?? Geometry.Empty;
            }
            return Geometry.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for boolean to navigation item style (selected vs normal)
    /// </summary>
    public class BooleanToNavigationItemStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return Application.Current.TryFindResource(
                    isSelected ? "SelectedNavigationItemStyle" : "NavigationItemStyle");
            }
            return Application.Current.TryFindResource("NavigationItemStyle");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for boolean to opacity (disabled items are semi-transparent)
    /// </summary>
    public class BooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? 1.0 : 0.5;
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse boolean to visibility converter
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for null to boolean (false if null, true if not null)
    /// </summary>
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for null to visibility (Visible if null, Collapsed if not null)
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for count to visibility inverse (Visible when count is 0, Collapsed otherwise)
    /// </summary>
    public class CountToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for boolean to tab foreground (orange when selected, gray otherwise)
    /// </summary>
    public class BooleanToTabForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return Application.Current.TryFindResource(
                    isSelected ? "AccentOrangeBrush" : "MediumTextBrush") ?? System.Windows.Media.Brushes.Gray;
            }
            return System.Windows.Media.Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for boolean to GridLength (Star when true, zero pixels when false)
    /// </summary>
    public class BooleanToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            }
            return new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength gridLength)
            {
                return gridLength.GridUnitType == GridUnitType.Star && gridLength.Value > 0;
            }
            return false;
        }
    }
}