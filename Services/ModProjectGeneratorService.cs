using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Orchestration;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Generates complete mod project structure with .csproj, Core.cs, Constants.cs, and quest files
    /// </summary>
    public class ModProjectGeneratorService
    {
        private readonly CodeGenerationOrchestrator _codeGenService;

        public ModProjectGeneratorService()
        {
            _codeGenService = new CodeGenerationOrchestrator();
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
                Directory.CreateDirectory(Path.Combine(modPath, "NPCs"));
                Directory.CreateDirectory(Path.Combine(modPath, "Utils"));
                Directory.CreateDirectory(Path.Combine(modPath, "Resources"));

                // Get mod metadata from first quest or defaults
                var firstQuest = project.Quests.FirstOrDefault();
                var hasQuests = project.Quests != null && project.Quests.Any();
                // Use the full namespace from quest (e.g., "MyMod.Quests") or construct from project name
                var modNamespace = firstQuest?.Namespace ?? (hasQuests 
                    ? $"{MakeSafeIdentifier(project.ProjectName, "GeneratedMod")}.Quests"
                    : MakeSafeIdentifier(project.ProjectName, "GeneratedMod"));
                // Extract root namespace for Core.cs (remove .Quests suffix if present)
                var rootNamespace = modNamespace.Contains('.') && modNamespace.EndsWith(".Quests")
                    ? modNamespace.Substring(0, modNamespace.LastIndexOf('.'))
                    : modNamespace;
                var modAuthor = firstQuest?.ModAuthor ?? settings?.DefaultModAuthor ?? "Quest Creator";
                var modVersion = firstQuest?.ModVersion ?? settings?.DefaultModVersion ?? "1.0.0";
                var gameStudio = firstQuest?.GameDeveloper ?? "TVGS";
                var gameName = firstQuest?.GameName ?? "Schedule I";

                // Generate .csproj file
                GenerateCsprojFile(modPath, modName, project.Resources, result, settings);

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

                // Generate NPC files
                foreach (var npc in project.Npcs)
                {
                    GenerateNpcFile(modPath, npc, result);
                }

                // Clean up old generated files that no longer match current NPCs/Quests
                CleanupOldGeneratedFiles(modPath, project, result);

                // Validate and copy resources
                ValidateAndCopyResources(project, modPath, result);

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

        private void GenerateCsprojFile(string modPath, string modName, IEnumerable<ResourceAsset> resources, ModProjectGenerationResult result, ModSettings? settings = null)
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
            sb.AppendLine("    <Configurations>CrossCompat</Configurations>");
            sb.AppendLine("    <Nullable>enable</Nullable>");
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
            var customS1ApiPath = settings?.S1ApiDllPath?.Trim();
            var useCustomS1Api = !string.IsNullOrWhiteSpace(customS1ApiPath);
            if (useCustomS1Api)
            {
                customS1ApiPath = customS1ApiPath!.Replace("\\", "\\\\");
            }

            sb.AppendLine("  <ItemGroup>");
            if (useCustomS1Api)
            {
                sb.AppendLine("    <!-- Using manually supplied S1API.dll -->");
                sb.AppendLine("    <Reference Include=\"S1API\">");
                sb.AppendLine($"      <HintPath>{customS1ApiPath}</HintPath>");
                sb.AppendLine("      <Private>false</Private>");
                sb.AppendLine("    </Reference>");
            }
            else
            {
                sb.AppendLine("    <PackageReference Include=\"S1API.Forked\" Version=\"*\" />");
            }
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
            sb.AppendLine("    <Reference Include=\"UnityEngine\">");
            sb.AppendLine("      <HintPath>$(ManagedPath)\\UnityEngine.dll</HintPath>");
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
            sb.AppendLine("    <Compile Include=\"NPCs\\*.cs\" />");
            sb.AppendLine("  </ItemGroup>");
            if (resources != null && resources.Any())
            {
                sb.AppendLine();
                sb.AppendLine("  <ItemGroup>");
                foreach (var resource in resources)
                {
                    var relative = (resource.RelativePath ?? string.Empty).Replace('/', '\\');
                    if (string.IsNullOrWhiteSpace(relative))
                        continue;
                    sb.AppendLine($"    <EmbeddedResource Include=\"{relative}\" />");
                }
                sb.AppendLine("  </ItemGroup>");
            }
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
            sb.AppendLine("        CrossCompat|Any CPU = CrossCompat|Any CPU");
            sb.AppendLine("    EndGlobalSection");
            sb.AppendLine("    GlobalSection(ProjectConfigurationPlatforms) = postSolution");
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
            var hasQuests = project.Quests != null && project.Quests.Any();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine("using MelonLoader;");
            if (hasQuests)
            {
                sb.AppendLine("using S1API.Quests;");
                sb.AppendLine("using S1API.Quests.Constants;");
            }
            sb.AppendLine("using S1API.Entities;");
            sb.AppendLine("using S1API.GameTime;");
            sb.AppendLine($"using {rootNamespace}.Utils;");
            if (hasQuests)
            {
                sb.AppendLine($"using {rootNamespace}.Quests;");
            }
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

            if (hasQuests)
            {
                sb.AppendLine("        private readonly Dictionary<string, Quest> _registeredQuests = new Dictionary<string, Quest>();");
                sb.AppendLine("        private bool _questsRegistered;");
                sb.AppendLine();
            }

            sb.AppendLine("        public override void OnLateInitializeMelon()");
            sb.AppendLine("        {");
            sb.AppendLine("            Instance = this;");
            if (hasQuests)
            {
                sb.AppendLine("            Player.LocalPlayerSpawned += HandleLocalPlayerSpawned;");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            if (hasQuests)
            {
                sb.AppendLine("        public override void OnSceneWasInitialized(int buildIndex, string sceneName)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (string.Equals(sceneName, \"Main\", StringComparison.OrdinalIgnoreCase))");
                sb.AppendLine("            {");
                sb.AppendLine("                _questsRegistered = false;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (string.Equals(sceneName, \"Main\", StringComparison.OrdinalIgnoreCase))");
                sb.AppendLine("            {");
                sb.AppendLine("                _questsRegistered = false;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        private void HandleLocalPlayerSpawned(Player player)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (_questsRegistered)");
                sb.AppendLine("                return;");
                sb.AppendLine();
                sb.AppendLine("            MelonCoroutines.Start(DelayedQuestRegistration());");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        private System.Collections.IEnumerator DelayedQuestRegistration()");
                sb.AppendLine("        {");
                sb.AppendLine("            if (_questsRegistered)");
                sb.AppendLine("                yield break;");
                sb.AppendLine();
                sb.AppendLine("            _questsRegistered = true;");
                sb.AppendLine("            // Wait a couple of frames to ensure Unity and S1API finish spawning Player/NPC systems");
                sb.AppendLine("            yield return null;");
                sb.AppendLine("            yield return null;");
                sb.AppendLine("            RegisterQuests();");
                sb.AppendLine("            StartSceneInitQuests();");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("        public override void OnApplicationQuit()");
            sb.AppendLine("        {");
            if (hasQuests)
            {
                sb.AppendLine("            Player.LocalPlayerSpawned -= HandleLocalPlayerSpawned;");
            }
            sb.AppendLine("            Instance = null;");
            sb.AppendLine("        }");
            sb.AppendLine();

            if (hasQuests)
            {
                sb.AppendLine("        private void RegisterQuests()");
                sb.AppendLine("        {");
                sb.AppendLine("            // Access QuestManager.Quests via reflection (it's internal)");
                sb.AppendLine("            var questsField = typeof(QuestManager).GetField(\"Quests\", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);");
                sb.AppendLine("            var questsList = questsField?.GetValue(null) as System.Collections.Generic.List<Quest>;");
                sb.AppendLine();

                // Register all quests
                foreach (var quest in project.Quests)
                {
                    var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
                    var questId = string.IsNullOrWhiteSpace(quest.QuestId) ? className : EscapeString(quest.QuestId.Trim());
                    var questKey = MakeSafeIdentifier(quest.ClassName, "quest");

                    sb.AppendLine($"            // Register {className}");
                    sb.AppendLine($"            try");
                    sb.AppendLine("            {");
                    sb.AppendLine("                // Check if quest already exists (loaded from save data)");
                    sb.AppendLine($"                Quest? existingQuest = null;");
                    sb.AppendLine("                if (questsList != null)");
                    sb.AppendLine("                {");
                    sb.AppendLine($"                    existingQuest = questsList.FirstOrDefault(q => q.GetType() == typeof({className}));");
                    sb.AppendLine("                }");
                    sb.AppendLine();
                    sb.AppendLine($"                if (existingQuest != null)");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    // Quest was already loaded from save data, skip creation");
                    sb.AppendLine($"                    _registeredQuests[\"{EscapeString(quest.QuestId ?? className)}\"] = existingQuest as {className};");
                    sb.AppendLine("                }");
                    sb.AppendLine("                else");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    // Quest doesn't exist, create it with consistent GUID");
                    sb.AppendLine($"                    var {questKey} = QuestManager.CreateQuest<{className}>(\"{EscapeString(questId)}\") as {className};");
                    sb.AppendLine($"                    if ({questKey} != null)");
                    sb.AppendLine("                    {");
                    sb.AppendLine("                        // Quest initialization happens when Unity calls Start() on the base game quest component");
                    sb.AppendLine("                        // This triggers CreateInternal() via Harmony patch, which calls InitializeQuest() to set up HUD UI");
                    sb.AppendLine("                        // For AutoBegin quests, CreateInternal() will automatically call Begin()");
                    sb.AppendLine($"                        _registeredQuests[\"{EscapeString(quest.QuestId ?? className)}\"] = {questKey};");
                    sb.AppendLine("                    }");
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
                sb.AppendLine("            // NPC event triggers are handled in individual quest classes via SubscribeToTriggers()");
                sb.AppendLine("            // This prevents duplicate subscriptions and keeps trigger logic with the quest");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        private void SubscribeToActionTriggers()");
                sb.AppendLine("        {");
                sb.AppendLine("            // Triggers are handled in individual quest classes via SubscribeToTriggers()");
                sb.AppendLine("            // This prevents duplicate subscriptions and keeps trigger logic with the quest");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        private void StartSceneInitQuests()");
                sb.AppendLine("        {");
                sb.AppendLine("            // Note: Quests with AutoBegin = true will automatically begin when CreateInternal() is called");
                sb.AppendLine("            // (which happens when the base game quest's Start() Unity method is invoked).");
                sb.AppendLine("            // We don't need to manually call Begin() here - the AutoBegin property handles it internally in S1API.");

                // Start scene-init quests
                var sceneInitQuests = project.Quests.Where(q => q.StartCondition?.TriggerType == QuestStartTrigger.SceneInit).ToList();
                var autoStartQuests = project.Quests.Where(q => q.StartCondition?.TriggerType == QuestStartTrigger.AutoStart).ToList();

                if (sceneInitQuests.Any() || autoStartQuests.Any())
                {
                    sb.AppendLine("            // Quests with AutoBegin = true will automatically begin when CreateInternal() is called");
                    sb.AppendLine("            // (which happens when the base game quest's Start() Unity method is invoked)");
                    sb.AppendLine("            // No manual Begin() calls needed - AutoBegin handles it internally in S1API");
                    foreach (var quest in sceneInitQuests.Concat(autoStartQuests))
                    {
                        var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
                        var questKey = EscapeString(quest.QuestId ?? quest.ClassName);

                        sb.AppendLine($"            // {className} will auto-begin if AutoBegin = true");
                        sb.AppendLine($"            // Quest is registered in _registeredQuests[\"{questKey}\"]");
                    }
                }
                else
                {
                    sb.AppendLine("            // No scene-init or auto-start quests configured");
                }

                sb.AppendLine("        }");
            }

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

        private void GenerateNpcFile(string modPath, NpcBlueprint npc, ModProjectGenerationResult result)
        {
            var npcCode = _codeGenService.GenerateNpcCode(npc);
            var className = MakeSafeIdentifier(npc.ClassName, "GeneratedNpc");
            var npcPath = Path.Combine(modPath, "NPCs", $"{className}.cs");

            File.WriteAllText(npcPath, npcCode);
            result.GeneratedFiles.Add(npcPath);
        }

        /// <summary>
        /// Removes old generated C# files that no longer correspond to current NPCs or Quests.
        /// This handles cases where elements were renamed or deleted.
        /// </summary>
        private void CleanupOldGeneratedFiles(string modPath, QuestProject project, ModProjectGenerationResult result)
        {
            try
            {
                // Collect expected file names from current NPCs and Quests
                var expectedNpcFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var expectedQuestFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var npc in project.Npcs)
                {
                    var className = MakeSafeIdentifier(npc.ClassName, "GeneratedNpc");
                    expectedNpcFiles.Add($"{className}.cs");
                }

                foreach (var quest in project.Quests)
                {
                    var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
                    expectedQuestFiles.Add($"{className}.cs");
                }

                // Clean up NPC files
                var npcsDir = Path.Combine(modPath, "NPCs");
                if (Directory.Exists(npcsDir))
                {
                    var existingNpcFiles = Directory.GetFiles(npcsDir, "*.cs");
                    foreach (var filePath in existingNpcFiles)
                    {
                        var fileName = Path.GetFileName(filePath);
                        if (!expectedNpcFiles.Contains(fileName))
                        {
                            try
                            {
                                File.Delete(filePath);
                                Debug.WriteLine($"[ModProjectGenerator] Deleted old NPC file: {fileName}");
                                result.Warnings.Add($"Removed old NPC file: {fileName}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[ModProjectGenerator] Failed to delete old NPC file '{fileName}': {ex.Message}");
                                result.Warnings.Add($"Could not remove old NPC file '{fileName}': {ex.Message}");
                            }
                        }
                    }
                }

                // Clean up Quest files
                var questsDir = Path.Combine(modPath, "Quests");
                if (Directory.Exists(questsDir))
                {
                    var existingQuestFiles = Directory.GetFiles(questsDir, "*.cs");
                    foreach (var filePath in existingQuestFiles)
                    {
                        var fileName = Path.GetFileName(filePath);
                        if (!expectedQuestFiles.Contains(fileName))
                        {
                            try
                            {
                                File.Delete(filePath);
                                Debug.WriteLine($"[ModProjectGenerator] Deleted old Quest file: {fileName}");
                                result.Warnings.Add($"Removed old Quest file: {fileName}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[ModProjectGenerator] Failed to delete old Quest file '{fileName}': {ex.Message}");
                                result.Warnings.Add($"Could not remove old Quest file '{fileName}': {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModProjectGenerator] Error during cleanup of old files: {ex.Message}");
                result.Warnings.Add($"Error cleaning up old generated files: {ex.Message}");
            }
        }

        private void ValidateAndCopyResources(QuestProject project, string modPath, ModProjectGenerationResult result)
        {
            if (project.Resources == null || project.Resources.Count == 0)
            {
                Debug.WriteLine("[ModProjectGenerator] No resources to copy");
                return;
            }

            if (string.IsNullOrWhiteSpace(project.FilePath))
            {
                result.Warnings.Add("Project file path is not set. Cannot copy resources.");
                Debug.WriteLine("[ModProjectGenerator] Missing project file path; skipping resource copy");
                return;
            }

            var projectDir = Path.GetDirectoryName(project.FilePath);
            if (string.IsNullOrWhiteSpace(projectDir))
            {
                result.Warnings.Add("Could not determine project directory. Cannot copy resources.");
                Debug.WriteLine("[ModProjectGenerator] Could not determine project directory");
                return;
            }

            // First, validate all resources exist
            var missingResources = new List<string>();
            var validResources = new List<ResourceAsset>();

            foreach (var asset in project.Resources)
            {
                var relative = asset.RelativePath;
                Debug.WriteLine($"[ModProjectGenerator] Validating resource '{asset.DisplayName}' ({relative ?? "null"})");
                if (string.IsNullOrWhiteSpace(relative))
                {
                    result.Warnings.Add($"Resource '{asset.DisplayName}' has no path specified");
                    continue;
                }

                if (!ResourcePathHelper.ResourceExists(relative, projectDir))
                {
                    var expectedPath = Path.Combine(projectDir, relative.Replace('/', Path.DirectorySeparatorChar));
                    missingResources.Add($"  • {asset.DisplayName} ({relative})\n    Expected at: {expectedPath}");
                    Debug.WriteLine($"[ModProjectGenerator] Missing resource '{relative}' expected at '{expectedPath}'");
                }
                else
                {
                    validResources.Add(asset);
                }
            }

            // Report missing resources
            if (missingResources.Count > 0)
            {
                var errorMessage = $"Error copying resource {(missingResources.Count == 1 ? "file" : "files")}. " +
                                 $"The following resource {(missingResources.Count == 1 ? "file" : "files")} could not be found:\n\n" +
                                 string.Join("\n", missingResources) +
                                 "\n\nThese resources will be excluded from the exported mod. " +
                                 "Please ensure all resource files exist in your project's Resources folder before exporting.";

                result.Errors.Add(errorMessage);
            }

            Debug.WriteLine($"[ModProjectGenerator] Copying {validResources.Count} resource(s) into '{modPath}'");
            // Copy valid resources
            foreach (var asset in validResources)
            {
                var relative = asset.RelativePath;
                var source = Path.Combine(projectDir, relative.Replace('/', Path.DirectorySeparatorChar));
                var destination = Path.Combine(modPath, relative.Replace('/', Path.DirectorySeparatorChar));

                var sourceFullPath = Path.GetFullPath(source);
                var destinationFullPath = Path.GetFullPath(destination);

                // When exporting into the same folder the project already lives in (the default flow),
                // the resource already sits at the correct path. Copying it onto itself is unnecessary
                // and was causing the exporter to delete the original PNG before attempting to read it,
                // leaving the preview blank and the export operation complaining about locked files.
                if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Nothing to copy – simply ensure the directory exists and continue.
                    var existingDir = Path.GetDirectoryName(sourceFullPath);
                    if (!string.IsNullOrEmpty(existingDir))
                    {
                        Directory.CreateDirectory(existingDir);
                    }
                    Debug.WriteLine($"[ModProjectGenerator] Skipping copy for '{relative}' (source equals destination '{sourceFullPath}')");
                    continue;
                }

                var destinationDir = Path.GetDirectoryName(destinationFullPath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                Debug.WriteLine($"[ModProjectGenerator] Copying '{sourceFullPath}' -> '{destinationFullPath}'");
                if (!TryCopyFileWithRetry(sourceFullPath, destinationFullPath, result, relative))
                {
                    result.Errors.Add($"Failed to copy resource after retries: {relative}. The file may be locked by another process. Please close any applications using this file and try again.");
                }
                else
                {
                    result.GeneratedFiles.Add(destinationFullPath);
                }
            }
        }

        private static bool TryCopyFileWithRetry(string source, string destination, ModProjectGenerationResult result, string relativePath, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[ModProjectGenerator] Resource copy attempt {attempt}/{maxRetries}: '{source}' -> '{destination}'");
                    // If destination exists and is locked, try to delete it first
                    if (File.Exists(destination))
                    {
                        try
                        {
                            File.Delete(destination);
                            Debug.WriteLine($"[ModProjectGenerator] Destination delete failed (attempt {attempt}): {destination}");
                        }
                        catch (IOException)
                        {
                            // Destination file is locked, wait and retry
                            if (attempt < maxRetries)
                            {
                                Thread.Sleep(100 * attempt); // Exponential backoff: 100ms, 200ms, 300ms
                                continue;
                            }
                            return false;
                        }
                    }

                    // Use FileStream with FileShare.ReadWrite to allow reading even if file is open elsewhere
                    // This allows copying files that might be open in Explorer preview or image viewers
                    using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                    Debug.WriteLine($"[ModProjectGenerator] Copy succeeded: '{destination}'");
                    return true;
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process") || ex.Message.Contains("cannot access"))
                {
                    if (attempt < maxRetries)
                    {
                        Debug.WriteLine($"[ModProjectGenerator] Resource locked (attempt {attempt}): {relativePath} - {ex.Message}");
                        result.Errors.Add($"Resource file locked (attempt {attempt}/{maxRetries}): {relativePath}. Retrying...");
                        Thread.Sleep(100 * attempt); // Exponential backoff: 100ms, 200ms, 300ms
                        continue;
                    }
                    Debug.WriteLine($"[ModProjectGenerator] Resource locked after {maxRetries} attempts: {relativePath}");
                    result.Errors.Add($"Resource file locked after {maxRetries} attempts: {relativePath}");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ModProjectGenerator] Copy failed for '{relativePath}': {ex.Message}");
                    result.Errors.Add($"Error copying resource {relativePath}: {ex.Message}");
                    return false;
                }
            }

            return false;
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
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
    }
}
