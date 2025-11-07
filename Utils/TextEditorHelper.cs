using System;
using System.Windows;
using ICSharpCode.AvalonEdit;

namespace Schedule1ModdingTool.Utils
{
    /// <summary>
    /// Enables binding to AvalonEdit's Text property by exposing an attached dependency property.
    /// </summary>
    public static class TextEditorHelper
    {
        public static readonly DependencyProperty BindableTextProperty =
            DependencyProperty.RegisterAttached(
                "BindableText",
                typeof(string),
                typeof(TextEditorHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindableTextChanged));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(TextEditorHelper),
                new PropertyMetadata(false));

        public static string GetBindableText(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableTextProperty);
        }

        public static void SetBindableText(DependencyObject obj, string value)
        {
            obj.SetValue(BindableTextProperty, value);
        }

        private static bool GetIsUpdating(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject obj, bool value)
        {
            obj.SetValue(IsUpdatingProperty, value);
        }

        private static void OnBindableTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextEditor editor)
            {
                return;
            }

            editor.TextChanged -= OnEditorTextChanged;

            if (!GetIsUpdating(editor))
            {
                var newText = e.NewValue as string ?? string.Empty;
                if (!string.Equals(editor.Text, newText, StringComparison.Ordinal))
                {
                    editor.Text = newText;
                }
            }

            editor.TextChanged += OnEditorTextChanged;
        }

        private static void OnEditorTextChanged(object sender, EventArgs e)
        {
            if (sender is not TextEditor editor)
            {
                return;
            }

            SetIsUpdating(editor, true);
            SetBindableText(editor, editor.Text);
            SetIsUpdating(editor, false);
        }
    }
}
