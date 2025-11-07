using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Generates complete mod project structure with .csproj, Core.cs, Constants.cs, and quest files
    /// </summary>
    public class ModProjectGeneratorService
    {
        private readonly CodeGenerationService _codeGenService;

        public ModProjectGeneratorService()
        {
            _codeGenService = new CodeGenerationService();
        }

        /// <summary>
        /// Generates a complete mod project from a QuestProject
        /// </summary>
        public ModProjectGenerationResult GenerateModProject(QuestProject project, string outputDirectory, ModSettings? settings = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("Output directory cannot be empty", nameof(outputDirectory));

            var result = new ModProjectGenerationResult();
            var modName = MakeSafeIdentifier(project.ProjectName, "GeneratedMod");
            // Use outputDirectory directly - it's already the mod folder created by the wizard
            var modPath = outputDirectory;

            try
            {
                // Create directory structure
                Directory.CreateDirectory(modPath);
                Directory.CreateDirectory(Path.Combine(modPath, "Quests"));
                Directory.CreateDirectory(Path.Combine(modPath, "Utils"));

                // Get mod metadata from first quest or defaults
                var firstQuest = project.Quests.FirstOrDefault();
                // Use the full namespace from quest (e.g., "MyMod.Quests") or construct from project name
                var modNamespace = firstQuest?.Namespace ?? $"{MakeSafeIdentifier(project.ProjectName, "GeneratedMod")}.Quests";
                // Extract root namespace for Core.cs (remove .Quests suffix if present)
                var rootNamespace = modNamespace.Contains('.') ? modNamespace.Substring(0, modNamespace.LastIndexOf('.')) : modNamespace;
                var modAuthor = firstQuest?.ModAuthor ?? settings?.DefaultModAuthor ?? "Quest Creator";
                var modVersion = firstQuest?.ModVersion ?? settings?.DefaultModVersion ?? "1.0.0";
                var gameStudio = firstQuest?.GameDeveloper ?? "TVGS";
                var gameName = firstQuest?.GameName ?? "Schedule I";

                // Generate .csproj file
                GenerateCsprojFile(modPath, modName, result, settings);

                // Generate .sln file
                GenerateSolutionFile(modPath, modName, result);

                // Generate Constants.cs
                GenerateConstantsFile(modPath, rootNamespace, modAuthor, modVersion, gameStudio, gameName, result);

                // Generate Core.cs with quest registration
                GenerateCoreFile(modPath, modName, rootNamespace, project, result);

                // Generate quest files
                foreach (var quest in project.Quests)
                {
                    GenerateQuestFile(modPath, quest, result);
                }

                result.Success = true;
                result.OutputPath = modPath;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private void GenerateCsprojFile(string modPath, string modName, ModProjectGenerationResult result, ModSettings? settings = null)
        {
            var csprojPath = Path.Combine(modPath, $"{modName}.csproj");
            var sb = new StringBuilder();

            // Get default game path from settings or use default Steam path
            var defaultGamePath = settings?.GameInstallPath ?? "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Schedule I_alternate";
            if (!string.IsNullOrEmpty(defaultGamePath) && !defaultGamePath.EndsWith("_alternate") && !defaultGamePath.EndsWith("_alternate\\"))
            {
                // If path doesn't end with _alternate, append it
                defaultGamePath = Path.Combine(defaultGamePath, "Schedule I_alternate");
            }
            // Escape backslashes for XML
            var escapedGamePath = defaultGamePath.Replace("\\", "\\\\");

            sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine("    <TargetFramework>netstandard2.1</TargetFramework>");
            sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
            sb.AppendLine($"    <RootNamespace>{modName}</RootNamespace>");
            sb.AppendLine("    <LangVersion>default</LangVersion>");
            sb.AppendLine("    <NeutralLanguage>en-US</NeutralLanguage>");
            sb.AppendLine("    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>");
            sb.AppendLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>");
            sb.AppendLine("    <Configurations>Debug;Release;Mono;Il2cpp;CrossCompat</Configurations>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine();
            sb.AppendLine("  <!-- CrossCompat configuration (default for S1API.Forked) -->");
            sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)'=='CrossCompat'\">");
            sb.AppendLine("    <DefineConstants>CROSS_COMPAT</DefineConstants>");
            sb.AppendLine($"    <AssemblyName>{modName}</AssemblyName>");
            sb.AppendLine("    <!-- Configure these paths to point to your Schedule One Mono installation -->");
            sb.AppendLine($"    <GamePath Condition=\"'$(GamePath)'==''\">{escapedGamePath}</GamePath>");
            sb.AppendLine("    <ManagedPath Condition=\"'$(ManagedPath)'==''\">$(GamePath)\\Schedule I_Data\\Managed</ManagedPath>");
            sb.AppendLine("    <MelonLoaderPath Condition=\"'$(MelonLoaderPath)'==''\">$(GamePath)\\MelonLoader\\net35</MelonLoaderPath>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine();
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine("    <PackageReference Include=\"S1API.Forked\" Version=\"2.4.8\" />");
            sb.AppendLine("    <PackageReference Include=\"LavaGang.MelonLoader\" Version=\"0.7.0\" />");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
            sb.AppendLine("  <!-- CrossCompat Unity references (Mono without Assembly-CSharp) -->");
            sb.AppendLine("  <ItemGroup Condition=\"'$(Configuration)'=='CrossCompat'\">");
            sb.AppendLine("    <Reference Include=\"Newtonsoft.Json\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\Newtonsoft.Json.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"Unity.TextMeshPro\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\Unity.TextMeshPro.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"UnityEngine.AssetBundleModule\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\UnityEngine.AssetBundleModule.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"UnityEngine.CoreModule\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\UnityEngine.CoreModule.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"UnityEngine.JSONSerializeModule\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\UnityEngine.JSONSerializeModule.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"UnityEngine.TextRenderingModule\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\UnityEngine.TextRenderingModule.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"UnityEngine.UI\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\UnityEngine.UI.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"UnityEngine.UIElementsModule\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\UnityEngine.UIElementsModule.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"UnityEngine.UIModule\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\UnityEngine.UIModule.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"MelonLoader\">");
            sb.AppendLine("      <HintPath>$(MelonLoaderPath)\\MelonLoader.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("    <Reference Include=\"0Harmony\">");
            sb.AppendLine("      <HintPath>$(MelonLoaderPath)\\0Harmony.dll</HintPath>");
            sb.AppendLine("    </Reference>");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine("    <Compile Include=\"Core.cs\" />");
            sb.AppendLine("    <Compile Include=\"Utils\\Constants.cs\" />");
            sb.AppendLine("    <Compile Include=\"Quests\\*.cs\" />");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine("</Project>");

            File.WriteAllText(csprojPath, sb.ToString());
            result.GeneratedFiles.Add(csprojPath);
        }

        private void GenerateSolutionFile(string modPath, string modName, ModProjectGenerationResult result)
        {
            var slnPath = Path.Combine(modPath, $"{modName}.sln");
            var sb = new StringBuilder();

            var projectGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var solutionGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();

            sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            sb.AppendLine("# Visual Studio Version 17");
            sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
            sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
            sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{modName}\", \"{modName}.csproj\", \"{projectGuid}\"");
            sb.AppendLine("EndProject");
            sb.AppendLine("Global");
            sb.AppendLine("    GlobalSection(SolutionConfigurationPlatforms) = preSolution");
            sb.AppendLine("        Debug|Any CPU = Debug|Any CPU");
            sb.AppendLine("        Release|Any CPU = Release|Any CPU");
            sb.AppendLine("        Mono|Any CPU = Mono|Any CPU");
            sb.AppendLine("        Il2cpp|Any CPU = Il2cpp|Any CPU");
            sb.AppendLine("        CrossCompat|Any CPU = CrossCompat|Any CPU");
            sb.AppendLine("    EndGlobalSection");
            sb.AppendLine("    GlobalSection(ProjectConfigurationPlatforms) = postSolution");
            sb.AppendLine($"        {projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            sb.AppendLine($"        {projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            sb.AppendLine($"        {projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            sb.AppendLine($"        {projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU");
            sb.AppendLine($"        {projectGuid}.Mono|Any CPU.ActiveCfg = Mono|Any CPU");
            sb.AppendLine($"        {projectGuid}.Mono|Any CPU.Build.0 = Mono|Any CPU");
            sb.AppendLine($"        {projectGuid}.Il2cpp|Any CPU.ActiveCfg = Il2cpp|Any CPU");
            sb.AppendLine($"        {projectGuid}.Il2cpp|Any CPU.Build.0 = Il2cpp|Any CPU");
            sb.AppendLine($"        {projectGuid}.CrossCompat|Any CPU.ActiveCfg = CrossCompat|Any CPU");
            sb.AppendLine($"        {projectGuid}.CrossCompat|Any CPU.Build.0 = CrossCompat|Any CPU");
            sb.AppendLine("    EndGlobalSection");
            sb.AppendLine($"    GlobalSection(SolutionProperties) = preSolution");
            sb.AppendLine("        HideSolutionNode = FALSE");
            sb.AppendLine("    EndGlobalSection");
            sb.AppendLine($"    GlobalSection(ExtensibilityGlobals) = postSolution");
            sb.AppendLine($"        SolutionGuid = {solutionGuid}");
            sb.AppendLine("    EndGlobalSection");
            sb.AppendLine("EndGlobal");

            File.WriteAllText(slnPath, sb.ToString());
            result.GeneratedFiles.Add(slnPath);
        }

        private void GenerateConstantsFile(string modPath, string rootNamespace, string modAuthor, string modVersion, string gameStudio, string gameName, ModProjectGenerationResult result)
        {
            var constantsPath = Path.Combine(modPath, "Utils", "Constants.cs");
            var sb = new StringBuilder();

            // Extract mod name from root namespace (last part)
            var modName = rootNamespace.Contains('.') ? rootNamespace.Substring(rootNamespace.LastIndexOf('.') + 1) : rootNamespace;

            sb.AppendLine($"namespace {rootNamespace}.Utils");
            sb.AppendLine("{");
            sb.AppendLine("    public static class Constants");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Mod information");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public const string MOD_NAME = \"{EscapeString(modName)}\";");
            sb.AppendLine($"        public const string MOD_VERSION = \"{EscapeString(modVersion)}\";");
            sb.AppendLine($"        public const string MOD_AUTHOR = \"{EscapeString(modAuthor)}\";");
            sb.AppendLine("        public const string MOD_DESCRIPTION = \"Generated mod\";");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// MelonPreferences configuration");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public const string PREFERENCES_CATEGORY = \"{modName}\";");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Game-related constants");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static class Game");
            sb.AppendLine("        {");
            sb.AppendLine($"            public const string GAME_STUDIO = \"{EscapeString(gameStudio)}\";");
            sb.AppendLine($"            public const string GAME_NAME = \"{EscapeString(gameName)}\";");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(constantsPath, sb.ToString());
            result.GeneratedFiles.Add(constantsPath);
        }

        private void GenerateCoreFile(string modPath, string modName, string rootNamespace, QuestProject project, ModProjectGenerationResult result)
        {
            var corePath = Path.Combine(modPath, "Core.cs");
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using MelonLoader;");
            sb.AppendLine("using S1API.Quests;");
            sb.AppendLine("using S1API.Quests.Constants;");
            sb.AppendLine("using S1API.Entities;");
            sb.AppendLine($"using {rootNamespace}.Utils;");
            sb.AppendLine($"using {rootNamespace}.Quests;");
            sb.AppendLine();
            sb.AppendLine($"[assembly: MelonInfo(typeof({rootNamespace}.Core), Constants.MOD_NAME, Constants.MOD_VERSION, Constants.MOD_AUTHOR)]");
            sb.AppendLine("[assembly: MelonGame(Constants.Game.GAME_STUDIO, Constants.Game.GAME_NAME)]");
            sb.AppendLine();
            sb.AppendLine($"namespace {rootNamespace}");
            sb.AppendLine("{");
            sb.AppendLine("    public class Core : MelonMod");
            sb.AppendLine("    {");
            sb.AppendLine("        public static Core? Instance { get; private set; }");
            sb.AppendLine();
            sb.AppendLine("        private readonly Dictionary<string, Quest> _registeredQuests = new Dictionary<string, Quest>();");
            sb.AppendLine();
            sb.AppendLine("        public override void OnInitializeMelon()");
            sb.AppendLine("        {");
            sb.AppendLine("            Instance = this;");
            sb.AppendLine("            RegisterQuests();");
            sb.AppendLine("            SubscribeToNPCEvents();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnSceneWasInitialized(int buildIndex, string sceneName)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (sceneName == \"Main\")");
            sb.AppendLine("            {");
            sb.AppendLine("                StartSceneInitQuests();");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnApplicationQuit()");
            sb.AppendLine("        {");
            sb.AppendLine("            Instance = null;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private void RegisterQuests()");
            sb.AppendLine("        {");

            // Register all quests
            foreach (var quest in project.Quests)
            {
                var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
                var questId = string.IsNullOrWhiteSpace(quest.QuestId) ? className : EscapeString(quest.QuestId.Trim());
                var questKey = MakeSafeIdentifier(quest.ClassName, "quest");

                sb.AppendLine($"            // Register {className}");
                sb.AppendLine($"            try");
                sb.AppendLine("            {");
                sb.AppendLine($"                var {questKey} = QuestManager.CreateQuest<{className}>(\"{questId}\") as {className};");
                sb.AppendLine($"                if ({questKey} != null)");
                sb.AppendLine("                {");
                sb.AppendLine($"                    {questKey}.ConfigureQuest();");
                sb.AppendLine($"                    _registeredQuests[\"{EscapeString(quest.QuestId ?? className)}\"] = {questKey};");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine("            catch (Exception ex)");
                sb.AppendLine("            {");
                sb.AppendLine($"                MelonLogger.Error($\"Failed to register quest {className}: {{ex.Message}}\");");
                sb.AppendLine("            }");
                sb.AppendLine();
            }

            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private void SubscribeToNPCEvents()");
            sb.AppendLine("        {");

            // Subscribe to NPC events for deal-based triggers
            var npcTriggerQuests = project.Quests.Where(q => q.StartCondition?.TriggerType == QuestStartTrigger.NPCDealCompleted && !string.IsNullOrWhiteSpace(q.StartCondition.NpcId)).ToList();
            if (npcTriggerQuests.Any())
            {
                sb.AppendLine("            // Subscribe to NPC customer events for deal-based quest triggers");
                foreach (var quest in npcTriggerQuests)
                {
                    var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
                    var npcId = EscapeString(quest.StartCondition.NpcId);
                    var questKey = EscapeString(quest.QuestId ?? quest.ClassName);

                    sb.AppendLine($"            // Quest: {className} triggered by NPC: {npcId}");
                    sb.AppendLine("            try");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                var npc = NPC.All.FirstOrDefault(n => n.ID == \"{npcId}\");");
                    sb.AppendLine("                if (npc != null && npc.IsCustomer)");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    npc.Customer.OnDealCompleted(() =>");
                    sb.AppendLine("                    {");
                    sb.AppendLine($"                        if (_registeredQuests.TryGetValue(\"{questKey}\", out var quest))");
                    sb.AppendLine("                        {");
                    sb.AppendLine("                            quest.Begin();");
                    sb.AppendLine("                        }");
                    sb.AppendLine("                    });");
                    sb.AppendLine("                }");
                    sb.AppendLine("            }");
                    sb.AppendLine("            catch (Exception ex)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                MelonLogger.Warning($\"Failed to subscribe to NPC {npcId} for quest {className}: {{ex.Message}}\");");
                    sb.AppendLine("            }");
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("            // No NPC deal-based quest triggers configured");
            }

            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private void StartSceneInitQuests()");
            sb.AppendLine("        {");

            // Start scene-init quests
            var sceneInitQuests = project.Quests.Where(q => q.StartCondition?.TriggerType == QuestStartTrigger.SceneInit).ToList();
            var autoStartQuests = project.Quests.Where(q => q.StartCondition?.TriggerType == QuestStartTrigger.AutoStart).ToList();

            if (sceneInitQuests.Any() || autoStartQuests.Any())
            {
                sb.AppendLine("            // Start quests that should begin on scene initialization");
                foreach (var quest in sceneInitQuests.Concat(autoStartQuests))
                {
                    var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
                    var questKey = EscapeString(quest.QuestId ?? quest.ClassName);

                    sb.AppendLine($"            // Start {className} if not completed");
                    sb.AppendLine("            try");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                if (_registeredQuests.TryGetValue(\"{questKey}\", out var quest))");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    quest.Begin();");
                    sb.AppendLine("                }");
                    sb.AppendLine("            }");
                    sb.AppendLine("            catch (Exception ex)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                MelonLogger.Warning($\"Failed to start quest {className}: {{ex.Message}}\");");
                    sb.AppendLine("            }");
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("            // No scene-init or auto-start quests configured");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(corePath, sb.ToString());
            result.GeneratedFiles.Add(corePath);
        }

        private void GenerateQuestFile(string modPath, QuestBlueprint quest, ModProjectGenerationResult result)
        {
            var questCode = _codeGenService.GenerateQuestCode(quest);
            var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
            var questPath = Path.Combine(modPath, "Quests", $"{className}.cs");

            File.WriteAllText(questPath, questCode);
            result.GeneratedFiles.Add(questPath);
        }

        private static string MakeSafeIdentifier(string? candidate, string fallback)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return fallback;
            }

            var builder = new StringBuilder();
            foreach (var ch in candidate)
            {
                if (builder.Length == 0)
                {
                    if (char.IsLetter(ch) || ch == '_')
                    {
                        builder.Append(ch);
                    }
                    else if (char.IsDigit(ch))
                    {
                        builder.Append('_').Append(ch);
                    }
                }
                else
                {
                    if (char.IsLetterOrDigit(ch) || ch == '_')
                    {
                        builder.Append(ch);
                    }
                    else
                    {
                        builder.Append('_');
                    }
                }
            }

            var result = builder.ToString();
            return string.IsNullOrEmpty(result) ? fallback : result;
        }

        private static string EscapeString(string? input)
        {
            return input?
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") ?? string.Empty;
        }
    }

    /// <summary>
    /// Result of mod project generation
    /// </summary>
    public class ModProjectGenerationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OutputPath { get; set; }
        public List<string> GeneratedFiles { get; } = new List<string>();
    }
}

