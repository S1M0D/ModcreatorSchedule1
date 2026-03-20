using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Builders;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.PhoneApp
{
    /// <summary>
    /// Generates S1API phone app classes from editor-authored blueprints.
    /// </summary>
    public partial class PhoneAppCodeGenerator : ICodeGenerator<PhoneAppBlueprint>
    {
        public string GenerateCode(PhoneAppBlueprint phoneApp)
        {
            ArgumentNullException.ThrowIfNull(phoneApp);

            var builder = new CodeBuilder();
            var className = IdentifierSanitizer.MakeSafeIdentifier(phoneApp.ClassName, "GeneratedPhoneApp");
            var targetNamespace = NamespaceNormalizer.NormalizeForPhoneApp(phoneApp.Namespace);
            var usingsBuilder = new UsingStatementsBuilder();
            usingsBuilder.AddPhoneAppUsings();

            builder.AppendComment("Auto-generated phone app registration class.");
            usingsBuilder.GenerateUsings(builder);
            builder.OpenBlock($"namespace {targetNamespace}");
            GeneratePhoneAppClass(builder, phoneApp, className);
            builder.CloseBlock();
            return builder.Build();
        }

        public CodeGenerationValidationResult Validate(PhoneAppBlueprint blueprint)
        {
            var result = new CodeGenerationValidationResult { IsValid = true };
            if (blueprint == null)
            {
                result.IsValid = false;
                result.Errors.Add("Phone app blueprint cannot be null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(blueprint.ClassName))
                result.Warnings.Add("Class name is empty, defaulting to 'GeneratedPhoneApp'.");
            if (string.IsNullOrWhiteSpace(blueprint.AppName))
                result.Errors.Add("App Name is required.");
            if (string.IsNullOrWhiteSpace(blueprint.AppTitle))
                result.Errors.Add("App Title is required.");
            if (string.IsNullOrWhiteSpace(blueprint.IconLabel))
                result.Errors.Add("Icon Label is required.");
            if (!blueprint.UseCustomUiBuilder)
            {
                if (blueprint.ShowPrimaryButton && string.IsNullOrWhiteSpace(blueprint.PrimaryButtonLabel))
                    result.Errors.Add("Primary button label is required when the primary button is enabled.");
                if (blueprint.ShowSecondaryButton && string.IsNullOrWhiteSpace(blueprint.SecondaryButtonLabel))
                    result.Errors.Add("Secondary button label is required when the secondary button is enabled.");
                if (blueprint.LayoutPreset == PhoneAppLayoutPresetOption.BlankCanvas &&
                    !blueprint.GenerateHookScaffold)
                {
                    result.Warnings.Add("Blank canvas apps are only useful if you plan to add custom UI in the generated hook file.");
                }
            }
            else if (blueprint.UiNodes.Count == 0)
            {
                result.Warnings.Add("Live Builder is enabled but the hierarchy is empty. The generated app will only show a placeholder message until you add UI nodes.");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private void GeneratePhoneAppClass(ICodeBuilder builder, PhoneAppBlueprint phoneApp, string className)
        {
            var customButtonHandlers = CollectCustomButtonHandlers(phoneApp.UiNodes);

            builder.AppendBlockComment(
                $"Registers the phone app \"{CodeFormatter.EscapeString(phoneApp.DisplayName)}\".",
                "S1API auto-discovers public PhoneApp subclasses when the in-game Home Screen initializes."
            );
            builder.OpenBlock($"public partial class {className} : PhoneApp");
            builder.AppendLine("private Text? _statusText;");
            builder.AppendLine();
            builder.AppendLine($"protected override string AppName => \"{CodeFormatter.EscapeString(phoneApp.AppName)}\";");
            builder.AppendLine($"protected override string AppTitle => \"{CodeFormatter.EscapeString(phoneApp.AppTitle)}\";");
            builder.AppendLine($"protected override string IconLabel => \"{CodeFormatter.EscapeString(phoneApp.IconLabel)}\";");
            builder.AppendLine($"protected override string IconFileName => \"{CodeFormatter.EscapeString(phoneApp.IconFileName)}\";");
            builder.AppendLine($"protected override EOrientation Orientation => EOrientation.{phoneApp.Orientation};");
            builder.AppendLine("protected override Sprite? IconSprite => LoadCustomIcon();");
            builder.AppendLine();
            builder.OpenBlock("protected override void OnCreated()");
            builder.AppendLine("base.OnCreated();");
            builder.AppendLine("OnAfterCreatedGenerated();");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("public override void Exit(ExitAction exit)");
            builder.AppendLine("OnExitGenerated(exit);");
            builder.AppendLine("base.Exit(exit);");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("protected override void OnPhoneClosed()");
            builder.AppendLine("OnPhoneClosedGenerated();");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("protected override void OnCreatedUI(GameObject container)");
            builder.AppendLine("BuildGeneratedUi(container);");
            builder.AppendLine("OnBuildGeneratedUi(container);");
            builder.CloseBlock();
            builder.AppendLine();
            GenerateUiMethod(builder, phoneApp, customButtonHandlers);
            builder.AppendLine();
            GenerateLayoutHelpers(builder);
            builder.AppendLine();
            GenerateButtonHandlers(builder, phoneApp);
            builder.AppendLine();
            GenerateCustomButtonHandlers(builder, customButtonHandlers);
            builder.AppendLine();
            GenerateEmbeddedIconMethod(builder, phoneApp);
            builder.AppendLine();
            GeneratePrivateHelpers(builder);
            builder.AppendLine();
            GeneratePartialHookMembers(builder);
            builder.CloseBlock();
        }

        private void GenerateUiMethod(
            ICodeBuilder builder,
            PhoneAppBlueprint phoneApp,
            IReadOnlyDictionary<string, CustomButtonHandlerDefinition> customButtonHandlers)
        {
            builder.AppendComment("Creates the generated phone app UI shell and optional preset or hierarchy-authored content.");
            builder.OpenBlock("private void BuildGeneratedUi(GameObject container)");
            builder.AppendLine($"var rootPanel = UIFactory.Panel(\"{CodeFormatter.EscapeString(phoneApp.AppName)}Root\", container.transform, {CodeFormatter.FormatColor(0.08f, 0.10f, 0.13f, 0.97f)}, fullAnchor: true);");
            builder.AppendLine("var rootRect = (RectTransform)rootPanel.transform;");
            builder.AppendLine("rootRect.offsetMin = new Vector2(18f, 18f);");
            builder.AppendLine("rootRect.offsetMax = new Vector2(-18f, -18f);");
            builder.AppendLine();

            if (phoneApp.UseCustomUiBuilder)
            {
                GenerateCustomBuilderUi(builder, phoneApp, customButtonHandlers);
                builder.CloseBlock();
                return;
            }

            if (phoneApp.LayoutPreset == PhoneAppLayoutPresetOption.BlankCanvas)
            {
                builder.AppendLine($"SetBackgroundMessage(rootPanel.transform, \"{CodeFormatter.EscapeString(phoneApp.HeaderText)}\", \"{CodeFormatter.EscapeString(phoneApp.BodyText)}\");");
                builder.AppendLine("return;");
                builder.CloseBlock();
                return;
            }

            builder.AppendLine("var header = UIFactory.Text(\"Header\", " +
                               $"\"{CodeFormatter.EscapeString(phoneApp.HeaderText)}\", rootPanel.transform, 24, TextAnchor.MiddleCenter, FontStyle.Bold);");
            builder.AppendLine("ConfigureAnchoredText((RectTransform)header.transform, new Vector2(0.08f, 0.85f), new Vector2(0.92f, 0.96f));");
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(phoneApp.SubheaderText))
            {
                builder.AppendLine("var subheader = UIFactory.Text(\"Subheader\", " +
                                   $"\"{CodeFormatter.EscapeString(phoneApp.SubheaderText)}\", rootPanel.transform, 13, TextAnchor.MiddleCenter);");
                builder.AppendLine("subheader.color = new Color(0.78f, 0.81f, 0.86f, 1f);");
                builder.AppendLine("ConfigureAnchoredText((RectTransform)subheader.transform, new Vector2(0.10f, 0.79f), new Vector2(0.90f, 0.85f));");
                builder.AppendLine();
            }

            if (phoneApp.UseScrollableBody)
            {
                builder.AppendLine("var bodyContent = UIFactory.ScrollableVerticalList(\"BodyScroll\", rootPanel.transform, out var scrollRect);");
                builder.AppendLine("ConfigureRect(scrollRect.GetComponent<RectTransform>(), new Vector2(0.07f, 0.23f), new Vector2(0.93f, 0.77f));");
                builder.AppendLine();
                builder.AppendLine("var bodyText = UIFactory.Text(\"BodyText\", " +
                                   $"\"{CodeFormatter.EscapeString(phoneApp.BodyText)}\", bodyContent, 15);");
                builder.AppendLine("ConfigureLayoutText(bodyText, 120f);");
                builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(phoneApp.FooterText))
                {
                    builder.AppendLine("var footerText = UIFactory.Text(\"FooterText\", " +
                                       $"\"{CodeFormatter.EscapeString(phoneApp.FooterText)}\", bodyContent, 12, TextAnchor.UpperLeft, FontStyle.Italic);");
                    builder.AppendLine("footerText.color = new Color(0.70f, 0.73f, 0.78f, 1f);");
                    builder.AppendLine("ConfigureLayoutText(footerText, 56f);");
                    builder.AppendLine();
                }
            }
            else
            {
                builder.AppendLine("var bodyText = UIFactory.Text(\"BodyText\", " +
                                   $"\"{CodeFormatter.EscapeString(phoneApp.BodyText)}\", rootPanel.transform, 15);");
                builder.AppendLine("bodyText.color = Color.white;");
                builder.AppendLine("ConfigureAnchoredText((RectTransform)bodyText.transform, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.76f));");
                builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(phoneApp.FooterText))
                {
                    builder.AppendLine("var footerText = UIFactory.Text(\"FooterText\", " +
                                       $"\"{CodeFormatter.EscapeString(phoneApp.FooterText)}\", rootPanel.transform, 12, TextAnchor.MiddleCenter, FontStyle.Italic);");
                    builder.AppendLine("footerText.color = new Color(0.70f, 0.73f, 0.78f, 1f);");
                    builder.AppendLine("ConfigureAnchoredText((RectTransform)footerText.transform, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.30f));");
                    builder.AppendLine();
                }
            }

            builder.AppendLine("var buttonsRoot = new GameObject(\"ButtonsRoot\");");
            builder.AppendLine("buttonsRoot.transform.SetParent(rootPanel.transform, false);");
            builder.AppendLine("var buttonsRect = buttonsRoot.AddComponent<RectTransform>();");
            builder.AppendLine("ConfigureRect(buttonsRect, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.20f));");
            builder.AppendLine("var buttonsLayout = buttonsRoot.AddComponent<HorizontalLayoutGroup>();");
            builder.AppendLine("buttonsLayout.spacing = 12;");
            builder.AppendLine("buttonsLayout.childAlignment = TextAnchor.MiddleCenter;");
            builder.AppendLine("buttonsLayout.childForceExpandWidth = false;");
            builder.AppendLine("buttonsLayout.childForceExpandHeight = false;");
            builder.AppendLine("buttonsLayout.childControlWidth = false;");
            builder.AppendLine("buttonsLayout.childControlHeight = false;");
            builder.AppendLine();

            if (phoneApp.ShowPrimaryButton)
            {
                builder.AppendLine("var (_, primaryButton, _) = UIFactory.RoundedButtonWithLabel(" +
                                   "\"PrimaryButton\", " +
                                   $"\"{CodeFormatter.EscapeString(phoneApp.PrimaryButtonLabel)}\", " +
                                   "buttonsRoot.transform, " +
                                   $"{CodeFormatter.FormatColor(0.17f, 0.45f, 0.28f, 1f)}, 148f, 42f, 16, Color.white);");
                builder.AppendLine("ButtonUtils.AddListener(primaryButton, HandlePrimaryButton);");
                builder.AppendLine();
            }

            if (phoneApp.ShowSecondaryButton)
            {
                builder.AppendLine("var (_, secondaryButton, _) = UIFactory.RoundedButtonWithLabel(" +
                                   "\"SecondaryButton\", " +
                                   $"\"{CodeFormatter.EscapeString(phoneApp.SecondaryButtonLabel)}\", " +
                                   "buttonsRoot.transform, " +
                                   $"{CodeFormatter.FormatColor(0.28f, 0.30f, 0.35f, 1f)}, 148f, 42f, 16, Color.white);");
                builder.AppendLine("ButtonUtils.AddListener(secondaryButton, HandleSecondaryButton);");
                builder.AppendLine();
            }

            builder.AppendLine("_statusText = UIFactory.Text(\"StatusText\", string.Empty, rootPanel.transform, 12, TextAnchor.MiddleCenter);");
            builder.AppendLine("_statusText.color = new Color(0.92f, 0.78f, 0.38f, 1f);");
            builder.AppendLine("ConfigureAnchoredText((RectTransform)_statusText.transform, new Vector2(0.08f, 0.01f), new Vector2(0.92f, 0.07f));");
            builder.CloseBlock();
        }

        private void GenerateLayoutHelpers(ICodeBuilder builder)
        {
            builder.AppendComment("Adds a small guidance message when the app is configured as a blank canvas.");
            builder.OpenBlock("private void SetBackgroundMessage(Transform parent, string title, string subtitle)");
            builder.AppendLine("var titleText = UIFactory.Text(\"CanvasTitle\", string.IsNullOrWhiteSpace(title) ? AppTitle : title, parent, 24, TextAnchor.MiddleCenter, FontStyle.Bold);");
            builder.AppendLine("ConfigureAnchoredText((RectTransform)titleText.transform, new Vector2(0.08f, 0.60f), new Vector2(0.92f, 0.76f));");
            builder.AppendLine();
            builder.AppendLine("var bodyText = UIFactory.Text(\"CanvasSubtitle\", string.IsNullOrWhiteSpace(subtitle)");
            builder.AppendLine("    ? \"This app is using Blank Canvas. Add programmatic UI in the generated hook file.\"");
            builder.AppendLine("    : subtitle, parent, 15, TextAnchor.MiddleCenter);");
            builder.AppendLine("bodyText.color = new Color(0.80f, 0.83f, 0.88f, 1f);");
            builder.AppendLine("ConfigureAnchoredText((RectTransform)bodyText.transform, new Vector2(0.10f, 0.35f), new Vector2(0.90f, 0.58f));");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private static void ConfigureAnchoredText(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)");
            builder.AppendLine("ConfigureRect(rectTransform, anchorMin, anchorMax);");
            builder.AppendLine("rectTransform.offsetMin = Vector2.zero;");
            builder.AppendLine("rectTransform.offsetMax = Vector2.zero;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private static void ConfigureLayoutText(Text textComponent, float minimumHeight)");
            builder.AppendLine("var rectTransform = (RectTransform)textComponent.transform;");
            builder.AppendLine("rectTransform.anchorMin = new Vector2(0f, 1f);");
            builder.AppendLine("rectTransform.anchorMax = new Vector2(1f, 1f);");
            builder.AppendLine("rectTransform.pivot = new Vector2(0.5f, 1f);");
            builder.AppendLine("textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;");
            builder.AppendLine("textComponent.verticalOverflow = VerticalWrapMode.Overflow;");
            builder.AppendLine("var fitter = textComponent.gameObject.GetComponent<ContentSizeFitter>() ?? textComponent.gameObject.AddComponent<ContentSizeFitter>();");
            builder.AppendLine("fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;");
            builder.AppendLine("fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;");
            builder.AppendLine("var layout = textComponent.gameObject.GetComponent<LayoutElement>() ?? textComponent.gameObject.AddComponent<LayoutElement>();");
            builder.AppendLine("layout.minHeight = minimumHeight;");
            builder.AppendLine("layout.flexibleWidth = 1f;");
            builder.CloseBlock();
            builder.AppendLine();
            GenerateCustomLayoutHelpers(builder);
            builder.AppendLine();
            builder.OpenBlock("private static void ConfigureRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)");
            builder.AppendLine("rectTransform.anchorMin = anchorMin;");
            builder.AppendLine("rectTransform.anchorMax = anchorMax;");
            builder.AppendLine("rectTransform.offsetMin = Vector2.zero;");
            builder.AppendLine("rectTransform.offsetMax = Vector2.zero;");
            builder.CloseBlock();
        }

        private void GenerateButtonHandlers(ICodeBuilder builder, PhoneAppBlueprint phoneApp)
        {
            builder.AppendComment("Default generated button behavior. Partial hooks can extend or replace the UX.");
            builder.OpenBlock("private void HandlePrimaryButton()");
            if (phoneApp.ShowPrimaryButton)
            {
                if (!string.IsNullOrWhiteSpace(phoneApp.PrimaryButtonResultText))
                {
                    builder.AppendLine($"SetStatusMessage(\"{CodeFormatter.EscapeString(phoneApp.PrimaryButtonResultText)}\");");
                }
                builder.AppendLine("OnPrimaryButtonPressedGenerated();");
                if (phoneApp.PrimaryButtonClosesApp)
                {
                    builder.AppendLine("CloseApp();");
                }
            }
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private void HandleSecondaryButton()");
            if (phoneApp.ShowSecondaryButton)
            {
                if (!string.IsNullOrWhiteSpace(phoneApp.SecondaryButtonResultText))
                {
                    builder.AppendLine($"SetStatusMessage(\"{CodeFormatter.EscapeString(phoneApp.SecondaryButtonResultText)}\");");
                }
                builder.AppendLine("OnSecondaryButtonPressedGenerated();");
                if (phoneApp.SecondaryButtonClosesApp)
                {
                    builder.AppendLine("CloseApp();");
                }
            }
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private void SetStatusMessage(string message)");
            builder.OpenBlock("if (_statusText != null)");
            builder.AppendLine("_statusText.text = message ?? string.Empty;");
            builder.CloseBlock();
            builder.CloseBlock();
        }

        private void GenerateEmbeddedIconMethod(ICodeBuilder builder, PhoneAppBlueprint phoneApp)
        {
            builder.AppendComment("Loads an embedded icon resource for the phone app icon when one is configured.");
            builder.OpenBlock("private static Sprite? LoadCustomIcon()");
            if (string.IsNullOrWhiteSpace(phoneApp.IconFileName))
            {
                builder.AppendLine("return null;");
                builder.CloseBlock();
                return;
            }

            builder.OpenBlock("try");
            builder.AppendLine($"var resourceName = ResolveEmbeddedResourceName(\"{CodeFormatter.EscapeString(phoneApp.IconFileName)}\");");
            builder.OpenBlock("if (resourceName == null)");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine("using var stream = assembly.GetManifestResourceStream(resourceName);");
            builder.OpenBlock("if (stream == null)");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("byte[] data = new byte[stream.Length];");
            builder.AppendLine("stream.Read(data, 0, data.Length);");
            builder.AppendLine("return ImageUtils.LoadImageRaw(data);");
            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine($"MelonLogger.Warning($\"Failed to load phone app icon '{CodeFormatter.EscapeString(phoneApp.IconFileName)}': {{ex.Message}}\");");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.CloseBlock();
        }

        private void GeneratePrivateHelpers(ICodeBuilder builder)
        {
            builder.AppendComment("Resolves an embedded resource by exact relative path or file name.");
            builder.OpenBlock("private static string? ResolveEmbeddedResourceName(string path)");
            builder.OpenBlock("if (string.IsNullOrWhiteSpace(path))");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var normalizedPath = path.Replace('\\\\', '/').TrimStart('/');");
            builder.AppendLine("var fileName = Path.GetFileName(normalizedPath);");
            builder.AppendLine("var exactSuffix = \".\" + normalizedPath.Replace('/', '.');");
            builder.AppendLine("var fileNameSuffix = \".\" + fileName;");
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine("return assembly.GetManifestResourceNames().FirstOrDefault(name =>");
            builder.AppendLine("    name.EndsWith(exactSuffix, StringComparison.OrdinalIgnoreCase)");
            builder.AppendLine("    || name.EndsWith(fileNameSuffix, StringComparison.OrdinalIgnoreCase));");
            builder.CloseBlock();
        }

        private void GeneratePartialHookMembers(ICodeBuilder builder)
        {
            builder.AppendComment("Optional hook points for generated partial companion files.");
            builder.AppendLine("partial void OnAfterCreatedGenerated();");
            builder.AppendLine("partial void OnBuildGeneratedUi(GameObject container);");
            builder.AppendLine("partial void OnPhoneClosedGenerated();");
            builder.AppendLine("partial void OnExitGenerated(ExitAction exit);");
            builder.AppendLine("partial void OnPrimaryButtonPressedGenerated();");
            builder.AppendLine("partial void OnSecondaryButtonPressedGenerated();");
            builder.AppendLine("partial void OnCustomButtonPressedGenerated(string nodeId);");
        }
    }
}
