namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Built-in starter templates for generated S1API phone apps.
    /// </summary>
    public static class PhoneAppBlueprintTemplates
    {
        public static PhoneAppBlueprint CreatePayphoneScaffold()
        {
            var blueprint = new PhoneAppBlueprint
            {
                ClassName = "PayphoneScaffoldApp",
                AppName = "payphone_scaffold",
                AppTitle = "Payphone",
                IconLabel = "CALL",
                Orientation = PhoneAppOrientationOption.Vertical,
                LayoutPreset = PhoneAppLayoutPresetOption.BlankCanvas,
                HeaderText = "Payphone",
                BodyText = "Use the generated hook file to wire dial input, call queuing, and payphone-specific behavior.",
                FooterText = "S1API wraps PhoneCalls.CallManager, but not the game's internal PayPhone types.",
                ShowPrimaryButton = false,
                ShowSecondaryButton = false,
                UseCustomUiBuilder = true,
                GenerateHookScaffold = true
            };

            blueprint.UiNodes.Add(CreatePayphoneRoot());
            return blueprint;
        }

        public static PhoneAppBlueprint CreateSamplePhoneApp()
        {
            return new PhoneAppBlueprint
            {
                ClassName = "SamplePhoneApp",
                AppName = "sample_phone_app",
                AppTitle = "Sample Phone App",
                IconLabel = "Sample",
                HeaderText = "Sample App",
                BodyText = "This sample app shows the generated S1API phone app shell."
            };
        }

        private static PhoneAppUiNodeBlueprint CreatePayphoneRoot()
        {
            var root = CreatePanel(
                "PayphoneShell",
                "#E6121820",
                padding: 10,
                spacing: 10,
                expandWidth: true,
                expandHeight: false);

            root.Children.Add(CreateText(
                "PayphoneTitle",
                "PAYPHONE",
                "#FFF4E3A1",
                fontSize: 22,
                alignment: PhoneAppUiTextAlignment.MiddleCenter,
                bold: true));
            root.Children.Add(CreateText(
                "PayphoneHint",
                "Scaffolded with Live Builder. Handle buttons in OnCustomButtonPressedGenerated(...).",
                "#FFB9C0CC",
                fontSize: 12,
                alignment: PhoneAppUiTextAlignment.MiddleCenter));
            root.Children.Add(CreateDisplayPanel());
            root.Children.Add(CreateKeypadPanel());
            root.Children.Add(CreateActionRow());
            root.Children.Add(CreateText(
                "PayphoneFooter",
                "Suggested next step: map digit_* buttons to a buffer, then queue calls through S1API.PhoneCalls.CallManager.",
                "#FF93A0B0",
                fontSize: 11,
                alignment: PhoneAppUiTextAlignment.MiddleCenter));

            return root;
        }

        private static PhoneAppUiNodeBlueprint CreateDisplayPanel()
        {
            var display = CreatePanel(
                "DisplayPanel",
                "#CC0B1015",
                padding: 12,
                spacing: 6,
                expandWidth: true,
                expandHeight: false);

            display.Children.Add(CreateText(
                "DialBuffer",
                "Number: 555-0100",
                "#FFFFFFFF",
                fontSize: 18,
                alignment: PhoneAppUiTextAlignment.MiddleCenter,
                bold: true));
            display.Children.Add(CreateText(
                "DialStatus",
                "Status: Idle",
                "#FF9AC2A1",
                fontSize: 12,
                alignment: PhoneAppUiTextAlignment.MiddleCenter));

            return display;
        }

        private static PhoneAppUiNodeBlueprint CreateKeypadPanel()
        {
            var keypad = CreatePanel(
                "Keypad",
                "#00000000",
                padding: 0,
                spacing: 8,
                expandWidth: true,
                expandHeight: false);

            keypad.Children.Add(CreateButtonRow(("digit_1", "1"), ("digit_2", "2"), ("digit_3", "3")));
            keypad.Children.Add(CreateButtonRow(("digit_4", "4"), ("digit_5", "5"), ("digit_6", "6")));
            keypad.Children.Add(CreateButtonRow(("digit_7", "7"), ("digit_8", "8"), ("digit_9", "9")));
            keypad.Children.Add(CreateButtonRow(("digit_star", "*"), ("digit_0", "0"), ("digit_hash", "#")));

            return keypad;
        }

        private static PhoneAppUiNodeBlueprint CreateActionRow()
        {
            var actions = CreateHorizontalPanel("Actions", spacing: 8);
            actions.Children.Add(CreateButton(
                "call_button",
                "Call",
                "Call button pressed. Queue a PhoneCallDefinition here.",
                "#FF2A6F4F",
                preferredHeight: 42));
            actions.Children.Add(CreateButton(
                "clear_button",
                "Clear",
                "Clear button pressed. Reset the dial buffer here.",
                "#FF5A6472",
                preferredHeight: 42));
            actions.Children.Add(CreateButton(
                "hangup_button",
                "Hang Up",
                "Hang up requested.",
                "#FF7B3838",
                preferredHeight: 42));
            return actions;
        }

        private static PhoneAppUiNodeBlueprint CreateButtonRow(
            (string Id, string Label) first,
            (string Id, string Label) second,
            (string Id, string Label) third)
        {
            var row = CreateHorizontalPanel("KeypadRow", spacing: 8);
            row.Children.Add(CreateButton(first.Id, first.Label, $"Pressed {first.Label}.", "#FF243648"));
            row.Children.Add(CreateButton(second.Id, second.Label, $"Pressed {second.Label}.", "#FF243648"));
            row.Children.Add(CreateButton(third.Id, third.Label, $"Pressed {third.Label}.", "#FF243648"));
            return row;
        }

        private static PhoneAppUiNodeBlueprint CreateHorizontalPanel(string name, double spacing)
        {
            return new PhoneAppUiNodeBlueprint
            {
                Name = name,
                NodeType = PhoneAppUiNodeType.Panel,
                LayoutDirection = PhoneAppUiLayoutDirection.Horizontal,
                BackgroundColorHex = "#00000000",
                Padding = 0,
                Spacing = spacing,
                ExpandWidth = true,
                ExpandHeight = false
            };
        }

        private static PhoneAppUiNodeBlueprint CreatePanel(
            string name,
            string backgroundColorHex,
            int padding,
            double spacing,
            bool expandWidth,
            bool expandHeight)
        {
            return new PhoneAppUiNodeBlueprint
            {
                Name = name,
                NodeType = PhoneAppUiNodeType.Panel,
                BackgroundColorHex = backgroundColorHex,
                Padding = padding,
                Spacing = spacing,
                ExpandWidth = expandWidth,
                ExpandHeight = expandHeight
            };
        }

        private static PhoneAppUiNodeBlueprint CreateText(
            string name,
            string text,
            string textColorHex,
            double fontSize,
            PhoneAppUiTextAlignment alignment,
            bool bold = false)
        {
            return new PhoneAppUiNodeBlueprint
            {
                Name = name,
                NodeType = PhoneAppUiNodeType.Text,
                Text = text,
                TextColorHex = textColorHex,
                FontSize = fontSize,
                TextAlignment = alignment,
                Bold = bold,
                ExpandWidth = true
            };
        }

        private static PhoneAppUiNodeBlueprint CreateButton(
            string id,
            string label,
            string statusMessage,
            string backgroundColorHex,
            double preferredHeight = 44)
        {
            return new PhoneAppUiNodeBlueprint
            {
                Id = id,
                Name = $"{label}Button",
                NodeType = PhoneAppUiNodeType.Button,
                Text = label,
                StatusMessage = statusMessage,
                BackgroundColorHex = backgroundColorHex,
                TextColorHex = "#FFFFFFFF",
                FontSize = 15,
                PreferredHeight = preferredHeight,
                ExpandWidth = true
            };
        }
    }
}
