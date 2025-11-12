using System;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.Views.Controls
{
    /// <summary>
    /// Interaction logic for ValidatedTextBox.xaml
    /// </summary>
    public partial class ValidatedTextBox : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ValidatedTextBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty ValidationTypeProperty =
            DependencyProperty.Register(nameof(ValidationType), typeof(ValidationType), typeof(ValidatedTextBox),
                new PropertyMetadata(ValidationType.NpcId, OnValidationTypeChanged));

        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register(nameof(IsValid), typeof(bool), typeof(ValidatedTextBox),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(ValidatedTextBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty AutoCorrectProperty =
            DependencyProperty.Register(nameof(AutoCorrect), typeof(bool), typeof(ValidatedTextBox),
                new PropertyMetadata(true));

        public ValidatedTextBox()
        {
            InitializeComponent();
            Loaded += ValidatedTextBox_Loaded;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            // Revalidate when Tag (FieldType) changes for DataClassDefaultValue validation
            if (e.Property == TagProperty && ValidationType == ValidationType.DataClassDefaultValue)
            {
                Validate();
            }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public ValidationType ValidationType
        {
            get => (ValidationType)GetValue(ValidationTypeProperty);
            set => SetValue(ValidationTypeProperty, value);
        }

        public bool IsValid
        {
            get => (bool)GetValue(IsValidProperty);
            private set => SetValue(IsValidProperty, value);
        }

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            private set => SetValue(ErrorMessageProperty, value);
        }

        public bool AutoCorrect
        {
            get => (bool)GetValue(AutoCorrectProperty);
            set => SetValue(AutoCorrectProperty, value);
        }

        private void ValidatedTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            Validate();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValidatedTextBox control)
            {
                control.Validate();
            }
        }

        private static void OnValidationTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValidatedTextBox control)
            {
                control.Validate();
            }
        }

        private void InnerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Validate();
        }

        private void InnerTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsValid && AutoCorrect)
            {
                AutoCorrectValue();
            }
            Validate();
        }

        public void Validate()
        {
            bool isValid;
            string errorMessage = string.Empty;

            switch (ValidationType)
            {
                case ValidationType.NpcId:
                    isValid = ValidationHelpers.IsValidNpcId(Text);
                    if (!isValid)
                    {
                        errorMessage = ValidationHelpers.GetNpcIdErrorMessage(Text);
                    }
                    break;

                case ValidationType.QuestId:
                    isValid = ValidationHelpers.IsValidQuestId(Text);
                    if (!isValid)
                    {
                        errorMessage = ValidationHelpers.GetQuestIdErrorMessage(Text);
                    }
                    break;

                case ValidationType.ClassName:
                    isValid = ValidationHelpers.IsValidClassName(Text);
                    if (!isValid)
                    {
                        errorMessage = ValidationHelpers.GetClassNameErrorMessage(Text);
                    }
                    break;

                case ValidationType.DataClassDefaultValue:
                    // Get FieldType from Tag property
                    var fieldType = Tag as DataClassFieldType?;
                    isValid = ValidationHelpers.IsValidDefaultValue(Text, fieldType);
                    if (!isValid)
                    {
                        errorMessage = ValidationHelpers.GetDefaultValueErrorMessage(Text, fieldType);
                    }
                    break;

                default:
                    isValid = true;
                    break;
            }

            IsValid = isValid;
            ErrorMessage = errorMessage;

            // Update visual state
            UpdateVisualState();
        }

        private void AutoCorrectValue()
        {
            string corrected = string.Empty;

            switch (ValidationType)
            {
                case ValidationType.NpcId:
                    corrected = ValidationHelpers.NormalizeNpcId(Text);
                    break;

                case ValidationType.QuestId:
                    corrected = ValidationHelpers.NormalizeNpcId(Text);
                    break;

                case ValidationType.ClassName:
                    corrected = ValidationHelpers.NormalizeClassName(Text);
                    break;

                case ValidationType.DataClassDefaultValue:
                    // Don't auto-correct default values - let user fix manually
                    break;
            }

            if (!string.IsNullOrEmpty(corrected) && corrected != Text)
            {
                Text = corrected;
            }
        }

        private void UpdateVisualState()
        {
            if (InnerTextBox == null) return;

            if (IsValid)
            {
                InnerTextBox.BorderBrush = Application.Current.Resources["DarkBorderBrush"] as System.Windows.Media.Brush;
                InnerTextBox.BorderThickness = new Thickness(1);
            }
            else
            {
                InnerTextBox.BorderBrush = Application.Current.Resources["ErrorBrush"] as System.Windows.Media.Brush;
                InnerTextBox.BorderThickness = new Thickness(2);
            }
        }
    }

    /// <summary>
    /// Enumeration of validation types for ValidatedTextBox
    /// </summary>
    public enum ValidationType
    {
        NpcId,
        QuestId,
        ClassName,
        DataClassDefaultValue
    }
}

