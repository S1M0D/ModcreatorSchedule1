using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Builders;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.PhoneApp
{
    public partial class PhoneAppCodeGenerator
    {
        private sealed class CustomButtonHandlerDefinition
        {
            public string NodeId { get; init; } = string.Empty;
            public string MethodName { get; init; } = string.Empty;
            public string StatusMessage { get; init; } = string.Empty;
            public bool CloseAppOnClick { get; init; }
        }

        private static IReadOnlyDictionary<string, CustomButtonHandlerDefinition> CollectCustomButtonHandlers(IEnumerable<PhoneAppUiNodeBlueprint> nodes)
        {
            var handlers = new Dictionary<string, CustomButtonHandlerDefinition>(StringComparer.Ordinal);
            var counter = 0;
            CollectCustomButtonHandlersRecursive(nodes, handlers, ref counter);
            return handlers;
        }

        private static void CollectCustomButtonHandlersRecursive(
            IEnumerable<PhoneAppUiNodeBlueprint> nodes,
            IDictionary<string, CustomButtonHandlerDefinition> handlers,
            ref int counter)
        {
            foreach (var node in nodes)
            {
                if (node.NodeType == PhoneAppUiNodeType.Button)
                {
                    counter++;
                    var nodeId = string.IsNullOrWhiteSpace(node.Id) ? Guid.NewGuid().ToString("N") : node.Id;
                    handlers[nodeId] = new CustomButtonHandlerDefinition
                    {
                        NodeId = nodeId,
                        MethodName = $"HandleCustomButton{counter}",
                        StatusMessage = node.StatusMessage,
                        CloseAppOnClick = node.CloseAppOnClick
                    };
                }

                if (node.Children.Count > 0)
                {
                    CollectCustomButtonHandlersRecursive(node.Children, handlers, ref counter);
                }
            }
        }

        private void GenerateCustomBuilderUi(
            ICodeBuilder builder,
            PhoneAppBlueprint phoneApp,
            IReadOnlyDictionary<string, CustomButtonHandlerDefinition> customButtonHandlers)
        {
            builder.AppendLine("var liveBuilderContent = UIFactory.ScrollableVerticalList(\"LiveBuilderContent\", rootPanel.transform, out var liveBuilderScrollRect);");
            builder.AppendLine("ConfigureRect(liveBuilderScrollRect.GetComponent<RectTransform>(), new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f));");
            builder.AppendLine("ConfigureStackContainer(liveBuilderContent.gameObject, true, 12f, 12);");
            builder.AppendLine();

            if (phoneApp.UiNodes.Count == 0)
            {
                builder.AppendLine("SetBackgroundMessage(liveBuilderContent, AppTitle, \"Live Builder is enabled, but the hierarchy is still empty.\");");
            }
            else
            {
                var rootIndex = 0;
                foreach (var rootNode in phoneApp.UiNodes)
                {
                    GenerateCustomUiNode(builder, rootNode, "liveBuilderContent", $"Root{++rootIndex}", customButtonHandlers);
                    builder.AppendLine();
                }
            }

            builder.AppendLine("_statusText = UIFactory.Text(\"StatusText\", string.Empty, rootPanel.transform, 12, TextAnchor.MiddleCenter);");
            builder.AppendLine("_statusText.color = new Color(0.92f, 0.78f, 0.38f, 1f);");
            builder.AppendLine("ConfigureAnchoredText((RectTransform)_statusText.transform, new Vector2(0.08f, 0.01f), new Vector2(0.92f, 0.07f));");
            builder.AppendLine("SetStatusMessage(\"Live Builder UI ready.\");");
            builder.AppendLine("return;");
        }

        private void GenerateCustomUiNode(
            ICodeBuilder builder,
            PhoneAppUiNodeBlueprint node,
            string parentExpression,
            string nodePath,
            IReadOnlyDictionary<string, CustomButtonHandlerDefinition> customButtonHandlers)
        {
            var safeNodeName = IdentifierSanitizer.MakeSafeIdentifier(nodePath, "UiNode");
            var displayName = CodeFormatter.EscapeString(string.IsNullOrWhiteSpace(node.Name) ? node.NodeType.ToString() : node.Name);

            switch (node.NodeType)
            {
                case PhoneAppUiNodeType.Panel:
                    var panelVariable = $"panel{safeNodeName}";
                    builder.AppendLine($"var {panelVariable} = UIFactory.Panel(\"{displayName}\", {parentExpression}, {CodeFormatter.FormatColorFromHex(node.BackgroundColorHex)}, fullAnchor: false);");
                    builder.AppendLine($"ConfigureStackContainer({panelVariable}, {(node.LayoutDirection == PhoneAppUiLayoutDirection.Vertical ? "true" : "false")}, {CodeFormatter.FormatFloat(node.Spacing)}f, {node.Padding});");
                    builder.AppendLine($"ApplyLayoutSizing({panelVariable}, {FormatLayoutSize(node.PreferredWidth)}, {FormatLayoutSize(node.PreferredHeight)}, {(node.ExpandWidth ? "true" : "false")}, {(node.ExpandHeight ? "true" : "false")});");
                    if (node.Children.Count == 0)
                    {
                        builder.AppendLine($"SetBackgroundMessage({panelVariable}.transform, \"{CodeFormatter.EscapeString(node.Name)}\", \"Empty panel. Add child text, buttons, or nested containers in the editor.\");");
                    }
                    else
                    {
                        var childIndex = 0;
                        foreach (var child in node.Children)
                        {
                            GenerateCustomUiNode(builder, child, $"{panelVariable}.transform", $"{nodePath}_{++childIndex}", customButtonHandlers);
                        }
                    }
                    break;

                case PhoneAppUiNodeType.Text:
                    var textVariable = $"text{safeNodeName}";
                    builder.AppendLine($"var {textVariable} = UIFactory.Text(\"{displayName}\", \"{CodeFormatter.EscapeString(node.Text)}\", {parentExpression}, {Math.Max(8, (int)Math.Round(node.FontSize))}, {MapTextAlignment(node.TextAlignment)}, {(node.Bold ? "FontStyle.Bold" : "FontStyle.Normal")});");
                    builder.AppendLine($"{textVariable}.color = {CodeFormatter.FormatColorFromHex(node.TextColorHex)};");
                    builder.AppendLine($"ConfigureLayoutText({textVariable}, {FormatMinimumTextHeight(node.PreferredHeight, node.FontSize)});");
                    builder.AppendLine($"ApplyLayoutSizing({textVariable}.gameObject, {FormatLayoutSize(node.PreferredWidth)}, {FormatLayoutSize(node.PreferredHeight)}, {(node.ExpandWidth ? "true" : "false")}, {(node.ExpandHeight ? "true" : "false")});");
                    break;

                case PhoneAppUiNodeType.Button:
                    var buttonVariable = $"button{safeNodeName}";
                    var labelVariable = $"buttonLabel{safeNodeName}";
                    var handler = customButtonHandlers.TryGetValue(node.Id, out var customHandler)
                        ? customHandler
                        : new CustomButtonHandlerDefinition
                        {
                            NodeId = node.Id,
                            MethodName = "HandleMissingCustomButton",
                            StatusMessage = node.StatusMessage,
                            CloseAppOnClick = node.CloseAppOnClick
                        };
                    builder.AppendLine($"var (_, {buttonVariable}, {labelVariable}) = UIFactory.RoundedButtonWithLabel(\"{displayName}\", \"{CodeFormatter.EscapeString(node.Text)}\", {parentExpression}, {CodeFormatter.FormatColorFromHex(node.BackgroundColorHex)}, {FormatButtonSize(node.PreferredWidth, 148d)}, {FormatButtonSize(node.PreferredHeight, 42d)}, {Math.Max(8, (int)Math.Round(node.FontSize))}, {CodeFormatter.FormatColorFromHex(node.TextColorHex)});");
                    builder.AppendLine($"{labelVariable}.alignment = TextAnchor.MiddleCenter;");
                    builder.AppendLine($"{labelVariable}.fontStyle = {(node.Bold ? "FontStyle.Bold" : "FontStyle.Normal")};");
                    builder.AppendLine($"ApplyLayoutSizing({buttonVariable}.gameObject, {FormatLayoutSize(node.PreferredWidth)}, {FormatLayoutSize(node.PreferredHeight)}, {(node.ExpandWidth ? "true" : "false")}, {(node.ExpandHeight ? "true" : "false")});");
                    builder.AppendLine($"ButtonUtils.AddListener({buttonVariable}, {handler.MethodName});");
                    break;

                case PhoneAppUiNodeType.Spacer:
                    var spacerVariable = $"spacer{safeNodeName}";
                    builder.AppendLine($"var {spacerVariable} = new GameObject(\"{displayName}\");");
                    builder.AppendLine($"{spacerVariable}.transform.SetParent({parentExpression}, false);");
                    builder.AppendLine($"{spacerVariable}.AddComponent<RectTransform>();");
                    builder.AppendLine($"ApplyLayoutSizing({spacerVariable}, {FormatLayoutSize(node.PreferredWidth, 0d)}, {FormatLayoutSize(node.PreferredHeight, 18d)}, {(node.ExpandWidth ? "true" : "false")}, {(node.ExpandHeight ? "true" : "false")});");
                    break;
            }
        }

        private void GenerateCustomButtonHandlers(ICodeBuilder builder, IReadOnlyDictionary<string, CustomButtonHandlerDefinition> customButtonHandlers)
        {
            builder.AppendComment("Generated button handlers for Live Builder hierarchy nodes.");

            if (customButtonHandlers.Count == 0)
            {
                builder.OpenBlock("private void HandleMissingCustomButton()");
                builder.AppendLine("SetStatusMessage(\"No custom button handlers were generated.\");");
                builder.CloseBlock();
                return;
            }

            foreach (var handler in customButtonHandlers.Values)
            {
                builder.OpenBlock($"private void {handler.MethodName}()");
                if (!string.IsNullOrWhiteSpace(handler.StatusMessage))
                {
                    builder.AppendLine($"SetStatusMessage(\"{CodeFormatter.EscapeString(handler.StatusMessage)}\");");
                }
                builder.AppendLine($"OnCustomButtonPressedGenerated(\"{CodeFormatter.EscapeString(handler.NodeId)}\");");
                if (handler.CloseAppOnClick)
                {
                    builder.AppendLine("CloseApp();");
                }
                builder.CloseBlock();
                builder.AppendLine();
            }
        }

        private void GenerateCustomLayoutHelpers(ICodeBuilder builder)
        {
            builder.OpenBlock("private static void ConfigureStackContainer(GameObject target, bool vertical, float spacing, int padding)");
            builder.OpenBlock("if (vertical)");
            builder.AppendLine("var layout = target.GetComponent<VerticalLayoutGroup>() ?? target.AddComponent<VerticalLayoutGroup>();");
            builder.AppendLine("layout.spacing = spacing;");
            builder.AppendLine("layout.padding = new RectOffset(padding, padding, padding, padding);");
            builder.AppendLine("layout.childAlignment = TextAnchor.UpperLeft;");
            builder.AppendLine("layout.childControlWidth = true;");
            builder.AppendLine("layout.childControlHeight = true;");
            builder.AppendLine("layout.childForceExpandWidth = false;");
            builder.AppendLine("layout.childForceExpandHeight = false;");
            builder.CloseBlock();
            builder.AppendLine("else");
            builder.OpenBlock("");
            builder.AppendLine("var layout = target.GetComponent<HorizontalLayoutGroup>() ?? target.AddComponent<HorizontalLayoutGroup>();");
            builder.AppendLine("layout.spacing = spacing;");
            builder.AppendLine("layout.padding = new RectOffset(padding, padding, padding, padding);");
            builder.AppendLine("layout.childAlignment = TextAnchor.MiddleLeft;");
            builder.AppendLine("layout.childControlWidth = true;");
            builder.AppendLine("layout.childControlHeight = true;");
            builder.AppendLine("layout.childForceExpandWidth = false;");
            builder.AppendLine("layout.childForceExpandHeight = false;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var fitter = target.GetComponent<ContentSizeFitter>() ?? target.AddComponent<ContentSizeFitter>();");
            builder.AppendLine("fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;");
            builder.AppendLine("fitter.horizontalFit = vertical ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private static void ApplyLayoutSizing(GameObject target, float preferredWidth, float preferredHeight, bool expandWidth, bool expandHeight)");
            builder.AppendLine("var layout = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();");
            builder.AppendLine("layout.preferredWidth = preferredWidth > 0f ? preferredWidth : -1f;");
            builder.AppendLine("layout.preferredHeight = preferredHeight > 0f ? preferredHeight : -1f;");
            builder.AppendLine("layout.flexibleWidth = expandWidth ? 1f : 0f;");
            builder.AppendLine("layout.flexibleHeight = expandHeight ? 1f : 0f;");
            builder.CloseBlock();
        }

        private static string MapTextAlignment(PhoneAppUiTextAlignment alignment)
        {
            return alignment switch
            {
                PhoneAppUiTextAlignment.UpperLeft => "TextAnchor.UpperLeft",
                PhoneAppUiTextAlignment.UpperCenter => "TextAnchor.UpperCenter",
                PhoneAppUiTextAlignment.MiddleCenter => "TextAnchor.MiddleCenter",
                PhoneAppUiTextAlignment.MiddleRight => "TextAnchor.MiddleRight",
                PhoneAppUiTextAlignment.LowerCenter => "TextAnchor.LowerCenter",
                _ => "TextAnchor.MiddleLeft"
            };
        }

        private static string FormatLayoutSize(double value, double defaultValue = 0d)
        {
            var resolved = value > 0d ? value : defaultValue;
            return $"{CodeFormatter.FormatFloat(resolved)}f";
        }

        private static string FormatButtonSize(double value, double defaultValue)
        {
            return $"{CodeFormatter.FormatFloat(value > 0d ? value : defaultValue)}f";
        }

        private static string FormatMinimumTextHeight(double preferredHeight, double fontSize)
        {
            var minimumHeight = preferredHeight > 0d
                ? preferredHeight
                : Math.Max(26d, fontSize + 12d);
            return $"{CodeFormatter.FormatFloat(minimumHeight)}f";
        }
    }
}
