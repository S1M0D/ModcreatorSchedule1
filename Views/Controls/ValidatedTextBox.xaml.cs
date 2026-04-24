using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.Views.Controls
{
    /// <summary>
    /// A text input control with real-time validation and visual feedback.
    /// Displays validation icons (checkmark/alert) and error messages based on the ValidationType.
    /// </summary>
    public partial class ValidatedTextBox : UserControl
    {
        /// <summary>
        /// Identifies the Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ValidatedTextBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        /// <summary>
        /// Identifies the ValidationType dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationTypeProperty =
            DependencyProperty.Register(nameof(ValidationType), typeof(ValidationType), typeof(ValidatedTextBox),
                new PropertyMetadata(ValidationType.NpcId, OnValidationTypeChanged));

        /// <summary>
        /// Identifies the IsValid dependency property.
        /// </summary>
        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register(nameof(IsValid), typeof(bool), typeof(ValidatedTextBox),
                new PropertyMetadata(true));

        /// <summary>
        /// Identifies the ErrorMessage dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(ValidatedTextBox),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the AutoCorrect dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoCorrectProperty =
            DependencyProperty.Register(nameof(AutoCorrect), typeof(bool), typeof(ValidatedTextBox),
                new PropertyMetadata(true));

        /// <summary>
        /// Initializes a new instance of the ValidatedTextBox control.
        /// </summary>
        public ValidatedTextBox()
        {
            InitializeComponent();
            Loaded += ValidatedTextBox_Loaded;
        }

        /// <summary>
        /// Gets or sets the text content of the textbox.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the type of validation to apply to the text.
        /// </summary>
        public ValidationType ValidationType
        {
            get => (ValidationType)GetValue(ValidationTypeProperty);
            set => SetValue(ValidationTypeProperty, value);
        }

        /// <summary>
        /// Gets whether the current text passes validation.
        /// </summary>
        public bool IsValid
        {
            get => (bool)GetValue(IsValidProperty);
            private set => SetValue(IsValidProperty, value);
        }

        /// <summary>
        /// Gets the error message if validation failed.
        /// </summary>
        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            private set => SetValue(ErrorMessageProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to auto-correct invalid values on focus loss.
        /// </summary>
        public bool AutoCorrect
        {
            get => (bool)GetValue(AutoCorrectProperty);
            set => SetValue(AutoCorrectProperty, value);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.Property == TagProperty && ValidationType == ValidationType.DataClassDefaultValue)
            {
                Validate();
            }
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

        /// <summary>
        /// Validates the current text against the ValidationType and updates visual state.
        /// </summary>
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
                    break;
            }

            if (!string.IsNullOrEmpty(corrected) && corrected != Text)
            {
                Text = corrected;
            }
        }

        private void UpdateVisualState()
        {
            if (InnerTextBox == null || ValidationIcon == null) return;

            if (IsValid && !string.IsNullOrWhiteSpace(Text))
            {
                InnerTextBox.BorderBrush = Application.Current.Resources["SuccessBrush"] as System.Windows.Media.Brush;
                InnerTextBox.BorderThickness = new Thickness(1);
                ValidationIcon.Visibility = Visibility.Visible;
            }
            else if (!IsValid)
            {
                InnerTextBox.BorderBrush = Application.Current.Resources["ErrorBrush"] as System.Windows.Media.Brush;
                InnerTextBox.BorderThickness = new Thickness(2);
                ValidationIcon.Visibility = Visibility.Visible;
            }
            else
            {
                InnerTextBox.BorderBrush = Application.Current.Resources["DarkBorderBrush"] as System.Windows.Media.Brush;
                InnerTextBox.BorderThickness = new Thickness(1);
                ValidationIcon.Visibility = Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    /// Specifies the validation rules for ValidatedTextBox.
    /// </summary>
    public enum ValidationType
    {
        /// <summary>
        /// Validates NPC identifiers (e.g., "npc_drugdealer_01").
        /// </summary>
        NpcId,

        /// <summary>
        /// Validates quest identifiers (e.g., "quest_intro_01").
        /// </summary>
        QuestId,

        /// <summary>
        /// Validates C# class names (must start with letter, alphanumeric).
        /// </summary>
        ClassName,

        /// <summary>
        /// Validates default values for data class fields based on field type.
        /// </summary>
        DataClassDefaultValue
    }
}

