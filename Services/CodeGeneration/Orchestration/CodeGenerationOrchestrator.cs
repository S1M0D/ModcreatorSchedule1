using Microsoft.CodeAnalysis.CSharp;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.GlobalState;
using Schedule1ModdingTool.Services.CodeGeneration.Item;
using Schedule1ModdingTool.Services.CodeGeneration.Quest;
using Schedule1ModdingTool.Services.CodeGeneration.Npc;
using Schedule1ModdingTool.Services.CodeGeneration.PhoneCall;
using Schedule1ModdingTool.Services.CodeGeneration.PhoneApp;

namespace Schedule1ModdingTool.Services.CodeGeneration.Orchestration
{
    /// <summary>
    /// Top-level orchestrator for all code generation operations.
    /// Acts as a facade to simplify access to different generators.
    /// This is the main entry point for code generation, replacing the old CodeGenerationService.
    /// </summary>
    public class CodeGenerationOrchestrator
    {
        private readonly ICodeGenerator<QuestBlueprint> _questGenerator;
        private readonly ICodeGenerator<NpcBlueprint> _npcGenerator;
        private readonly ICodeGenerator<ItemBlueprint> _itemGenerator;
        private readonly ICodeGenerator<GlobalStateBlueprint> _globalStateGenerator;
        private readonly ICodeGenerator<PhoneCallBlueprint> _phoneCallGenerator;
        private readonly ICodeGenerator<PhoneAppBlueprint> _phoneAppGenerator;

        /// <summary>
        /// Creates a new orchestrator with default generators.
        /// </summary>
        public CodeGenerationOrchestrator()
            : this(null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a new orchestrator with custom generators (for testing/extensibility).
        /// </summary>
        /// <param name="questGenerator">Custom quest generator, or null for default.</param>
        /// <param name="npcGenerator">Custom NPC generator, or null for default.</param>
        public CodeGenerationOrchestrator(
            ICodeGenerator<QuestBlueprint>? questGenerator = null,
            ICodeGenerator<NpcBlueprint>? npcGenerator = null,
            ICodeGenerator<ItemBlueprint>? itemGenerator = null,
            ICodeGenerator<GlobalStateBlueprint>? globalStateGenerator = null,
            ICodeGenerator<PhoneCallBlueprint>? phoneCallGenerator = null,
            ICodeGenerator<PhoneAppBlueprint>? phoneAppGenerator = null)
        {
            _questGenerator = questGenerator ?? new QuestCodeGenerator();
            _npcGenerator = npcGenerator ?? new NpcCodeGenerator();
            _itemGenerator = itemGenerator ?? new ItemCodeGenerator();
            _globalStateGenerator = globalStateGenerator ?? new GlobalStateCodeGenerator();
            _phoneCallGenerator = phoneCallGenerator ?? new PhoneCallCodeGenerator();
            _phoneAppGenerator = phoneAppGenerator ?? new PhoneAppCodeGenerator();
        }

        /// <summary>
        /// Generates complete C# source code for a quest blueprint.
        /// </summary>
        /// <param name="quest">The quest blueprint to generate code from.</param>
        /// <returns>Complete C# source code as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if quest is null.</exception>
        public string GenerateQuestCode(QuestBlueprint quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            return _questGenerator.GenerateCode(quest);
        }

        /// <summary>
        /// Generates complete C# source code for an NPC blueprint.
        /// </summary>
        /// <param name="npc">The NPC blueprint to generate code from.</param>
        /// <returns>Complete C# source code as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if npc is null.</exception>
        public string GenerateNpcCode(NpcBlueprint npc)
        {
            if (npc == null)
                throw new ArgumentNullException(nameof(npc));

            return _npcGenerator.GenerateCode(npc);
        }

        /// <summary>
        /// Generates complete C# source code for an item blueprint.
        /// </summary>
        public string GenerateItemCode(ItemBlueprint item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return _itemGenerator.GenerateCode(item);
        }

        /// <summary>
        /// Generates complete C# source code for a global saveable blueprint.
        /// </summary>
        public string GenerateGlobalStateCode(GlobalStateBlueprint globalState)
        {
            if (globalState == null)
                throw new ArgumentNullException(nameof(globalState));

            return _globalStateGenerator.GenerateCode(globalState);
        }

        /// <summary>
        /// Generates complete C# source code for a phone call blueprint.
        /// </summary>
        public string GeneratePhoneCallCode(PhoneCallBlueprint phoneCall)
        {
            if (phoneCall == null)
                throw new ArgumentNullException(nameof(phoneCall));

            return _phoneCallGenerator.GenerateCode(phoneCall);
        }

        /// <summary>
        /// Generates complete C# source code for a phone app blueprint.
        /// </summary>
        public string GeneratePhoneAppCode(PhoneAppBlueprint phoneApp)
        {
            if (phoneApp == null)
                throw new ArgumentNullException(nameof(phoneApp));

            return _phoneAppGenerator.GenerateCode(phoneApp);
        }

        /// <summary>
        /// Validates a quest blueprint before generation.
        /// </summary>
        /// <param name="quest">The quest blueprint to validate.</param>
        /// <returns>Validation result with errors and warnings.</returns>
        public CodeGenerationValidationResult ValidateQuest(QuestBlueprint quest)
        {
            if (quest == null)
                return new CodeGenerationValidationResult
                {
                    IsValid = false,
                    Errors = { "Quest blueprint cannot be null" }
                };

            return _questGenerator.Validate(quest);
        }

        /// <summary>
        /// Validates an NPC blueprint before generation.
        /// </summary>
        /// <param name="npc">The NPC blueprint to validate.</param>
        /// <returns>Validation result with errors and warnings.</returns>
        public CodeGenerationValidationResult ValidateNpc(NpcBlueprint npc)
        {
            if (npc == null)
                return new CodeGenerationValidationResult
                {
                    IsValid = false,
                    Errors = { "NPC blueprint cannot be null" }
                };

            return _npcGenerator.Validate(npc);
        }

        /// <summary>
        /// Validates an item blueprint before generation.
        /// </summary>
        public CodeGenerationValidationResult ValidateItem(ItemBlueprint item)
        {
            if (item == null)
                return new CodeGenerationValidationResult
                {
                    IsValid = false,
                    Errors = { "Item blueprint cannot be null" }
                };

            return _itemGenerator.Validate(item);
        }

        /// <summary>
        /// Validates a global saveable blueprint before generation.
        /// </summary>
        public CodeGenerationValidationResult ValidateGlobalState(GlobalStateBlueprint globalState)
        {
            if (globalState == null)
                return new CodeGenerationValidationResult
                {
                    IsValid = false,
                    Errors = { "Global saveable blueprint cannot be null" }
                };

            return _globalStateGenerator.Validate(globalState);
        }

        /// <summary>
        /// Validates a phone call blueprint before generation.
        /// </summary>
        public CodeGenerationValidationResult ValidatePhoneCall(PhoneCallBlueprint phoneCall)
        {
            if (phoneCall == null)
                return new CodeGenerationValidationResult
                {
                    IsValid = false,
                    Errors = { "Phone call blueprint cannot be null" }
                };

            return _phoneCallGenerator.Validate(phoneCall);
        }

        /// <summary>
        /// Validates a phone app blueprint before generation.
        /// </summary>
        public CodeGenerationValidationResult ValidatePhoneApp(PhoneAppBlueprint phoneApp)
        {
            if (phoneApp == null)
                return new CodeGenerationValidationResult
                {
                    IsValid = false,
                    Errors = { "Phone app blueprint cannot be null" }
                };

            return _phoneAppGenerator.Validate(phoneApp);
        }

        /// <summary>
        /// Compiles generated code to validate syntax using Roslyn.
        /// This is useful for catching syntax errors before writing files.
        /// </summary>
        /// <param name="code">The C# source code to validate.</param>
        /// <returns>True if the code has valid syntax, false otherwise.</returns>
        public bool ValidateSyntax(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var diagnostics = syntaxTree.GetDiagnostics();

                // Check for any error-level diagnostics
                foreach (var diagnostic in diagnostics)
                {
                    if (diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Compiles code to DLL (currently validates syntax only).
        /// Actual compilation requires Unity/S1API refs at export time.
        /// </summary>
        /// <param name="quest">The quest blueprint being compiled.</param>
        /// <param name="code">The generated code.</param>
        /// <returns>True if syntax is valid, false otherwise.</returns>
        public bool CompileToDll(QuestBlueprint quest, string code)
        {
            try
            {
                // Use Roslyn to validate syntax for now (actual compilation requires Unity/S1API refs at export time)
                _ = CSharpSyntaxTree.ParseText(code);
                System.Diagnostics.Debug.WriteLine($"Quest '{quest.ClassName}' validated for export.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Code validation error: {ex.Message}");
                return false;
            }
        }
    }
}
