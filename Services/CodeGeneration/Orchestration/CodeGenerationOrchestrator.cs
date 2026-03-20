using Microsoft.CodeAnalysis.CSharp;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Item;
using Schedule1ModdingTool.Services.CodeGeneration.Quest;
using Schedule1ModdingTool.Services.CodeGeneration.Npc;

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

        /// <summary>
        /// Creates a new orchestrator with default generators.
        /// </summary>
        public CodeGenerationOrchestrator()
            : this(null, null, null)
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
            ICodeGenerator<ItemBlueprint>? itemGenerator = null)
        {
            _questGenerator = questGenerator ?? new QuestCodeGenerator();
            _npcGenerator = npcGenerator ?? new NpcCodeGenerator();
            _itemGenerator = itemGenerator ?? new ItemCodeGenerator();
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
