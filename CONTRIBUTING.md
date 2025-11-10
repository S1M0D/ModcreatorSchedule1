# Contributing to Schedule1 Modding Tool

Welcome potential contributor! Thank you for your interest in helping improve the Schedule1 Modding Tool.
This document will guide you through the contribution process and help set expectations.

## Table of Contents

- [Important Guidelines](#important-guidelines)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [How to Contribute](#how-to-contribute)
- [Code Review Process](#code-review-process)
- [Testing Guidelines](#testing-guidelines)
- [Common Development Workflows](#common-development-workflows)

## Important Guidelines

### Must Read Before Contributing

1. **Read [CODING_STANDARDS.md](CODING_STANDARDS.md) thoroughly** - All contributions must follow the established coding standards.
2. **Review existing code** - Familiarize yourself with the codebase structure and conventions before making changes.
3. **Test your changes** - Ensure your changes build successfully and don't break existing functionality.

### Code Quality Expectations

- Follow MVVM pattern consistently
- Write clean, self-documenting code
- Add XML documentation for public APIs
- Maintain Material Design UI consistency
- Validate user input and handle errors gracefully
- Generate valid, well-formatted C# code from blueprints

## Getting Started

### Prerequisites

- **Visual Studio 2022** or **JetBrains Rider** (recommended IDEs)
- **.NET 6.0 SDK or later** - [Download here](https://dotnet.microsoft.com/download)
- **Git** for version control
- **Basic WPF/XAML knowledge** - Understanding of data binding, MVVM, and Material Design is helpful

### Optional Tools

- **GitHub Desktop** - If you prefer a GUI for Git operations
- **Visual Studio Code** - With C# and XAML extensions for lightweight editing

## Development Setup

### 1. Clone the Repository

```bash
git clone https://github.com/ESTONlA/ModcreatorSchedule1.git
cd ModcreatorSchedule1
```

### 2. Restore NuGet Packages

**Visual Studio/Rider:**
- Open `Schedule1ModdingTool.sln`
- The IDE will automatically restore packages on first load

**Command Line:**
```bash
dotnet restore Schedule1ModdingTool.sln
```

### 3. Build the Project

**Visual Studio/Rider:**
- Press `Ctrl+Shift+B` (Visual Studio) or `Ctrl+F9` (Rider)

**Command Line:**
```bash
dotnet build Schedule1ModdingTool.sln
```

### 4. Run the Application

**Visual Studio/Rider:**
- Press `F5` to run with debugging or `Ctrl+F5` to run without debugging

**Command Line:**
```bash
dotnet run --project Schedule1ModdingTool.csproj
```

### 5. Verify Setup

- The application should launch showing the main window
- Try creating a new project
- Try adding an NPC or Quest blueprint
- Try generating code from a blueprint
- Verify the generated code is valid C#

## Project Structure

Understanding the project structure will help you navigate and contribute effectively:

```
ModcreatorSchedule1/
├── Models/                          # Data models and blueprints
│   ├── NpcBlueprint.cs             # Main NPC data model
│   ├── NpcAppearance.cs            # Appearance configuration
│   ├── NpcScheduleAction.cs        # Schedule action definitions
│   ├── NpcCustomerDefaults.cs      # Customer behavior settings
│   ├── NpcDealerDefaults.cs        # Dealer configuration
│   ├── NpcRelationshipDefaults.cs  # Relationship configuration
│   └── NpcInventoryDefaults.cs     # Inventory settings
│
├── ViewModels/                      # MVVM ViewModels
│   └── MainViewModel.cs            # Main application ViewModel
│
├── Views/                           # WPF Views and Controls
│   ├── MainWindow.xaml             # Main application window
│   ├── NpcPropertiesControl.xaml   # NPC editor UI (6 tabs)
│   ├── QuestPropertiesControl.xaml # Quest editor UI
│   └── Controls/                   # Reusable custom controls
│       ├── Vector3Input.xaml       # X/Y/Z coordinate input
│       ├── TimeInput.xaml          # Time picker (HHMM format)
│       ├── ScheduleActionEditor.xaml # Dynamic action editor
│       └── DrugAffinityEditor.xaml # Drug affinity list editor
│
├── Services/                        # Business logic services
│   ├── CodeGeneration/             # Code generation pipeline
│   │   ├── Abstractions/           # Interfaces (ICodeBuilder, ICodeGenerator)
│   │   ├── Builders/               # Code building utilities
│   │   ├── Common/                 # Shared generation utilities
│   │   ├── Npc/                    # NPC code generators
│   │   ├── Quest/                  # Quest code generators
│   │   ├── Triggers/               # Trigger generators
│   │   └── Orchestration/          # High-level orchestration
│   └── ProjectService.cs           # Project save/load logic
│
├── Utils/                           # Utility classes
│   ├── IdentifierSanitizer.cs      # Safe identifier generation
│   ├── CodeFormatter.cs            # Code formatting helpers
│   ├── NamespaceNormalizer.cs      # Namespace validation
│   └── ColorUtils.cs               # Color conversion utilities
│
├── Data/                            # Static data and presets
│   └── AppearancePresets.cs        # Avatar appearance presets
│
├── Resources/                       # Application resources
│   └── Icons/                      # Icon assets
│
├── CODING_STANDARDS.md             # This file's companion
├── CONTRIBUTING.md                 # This file
└── README.md                        # User-facing documentation
```

### Key Architectural Patterns

- **MVVM**: Models, Views, ViewModels strictly separated
- **Observable Pattern**: All models inherit from `ObservableObject` for data binding
- **Builder Pattern**: Code generation uses fluent `ICodeBuilder` interface
- **Composition**: Complex generators compose smaller, specialized generators
- **Dependency Properties**: Custom WPF controls use standard DP patterns

## How to Contribute

### Types of Contributions

We welcome various types of contributions:

1. **Bug Fixes** - Fix issues in existing functionality
2. **New Features** - Add support for new S1API features or UI improvements
3. **Code Generation** - Extend or improve code generation logic
4. **UI/UX Improvements** - Enhance the user interface or user experience
5. **Documentation** - Improve code comments, XML docs, or markdown files
6. **Testing** - Add test cases or improve test coverage (when testing framework is added)
7. **Performance** - Optimize slow operations or reduce memory usage
8. **Refactoring** - Improve code quality without changing functionality

### Contribution Workflow

#### 1. Find or Create an Issue

- Check existing GitHub Issues to avoid duplicate work
- If you're fixing a bug or adding a feature, create an issue first to discuss the approach
- For small fixes (typos, minor bugs), you can skip this step

#### 2. Fork and Branch

```bash
# Fork the repository on GitHub, then clone your fork
git clone https://github.com/ESTONlA/ModcreatorSchedule1.git
cd ModcreatorSchedule1

# Create a feature branch
git checkout -b feature/your-feature-name
# or for bug fixes
git checkout -b fix/bug-description
```

**Branch Naming Conventions:**
- `feature/` - New features (e.g., `feature/location-blueprint`)
- `fix/` - Bug fixes (e.g., `fix/schedule-validation`)
- `refactor/` - Code refactoring (e.g., `refactor/code-generator`)
- `ui/` - UI/UX improvements (e.g., `ui/material-design-cards`)

#### 3. Make Your Changes

- Follow [CODING_STANDARDS.md](CODING_STANDARDS.md) strictly
- Write clean, self-documenting code
- Add XML documentation for public members
- Keep commits focused and atomic
- Write clear commit messages

**Good Commit Message Examples:**
```
Add support for Location blueprint generation

- Create LocationBlueprint model with ObservableObject base
- Implement LocationCodeGenerator following ICodeGenerator pattern
- Add LocationPropertiesControl with Material Design layout
- Update MainViewModel to handle Location blueprints
```

```
Fix schedule action time validation

The time input was accepting invalid times like 2567. Now validates:
- Hours: 0-23
- Minutes: 0-59
- Rejects invalid combinations
```

**Bad Commit Message Examples:**
```
Fixed stuff
```

```
WIP
```

```
Changes
```

#### 4. Test Your Changes

Before submitting a PR, ensure:

- ✅ Project builds without errors: `dotnet build`
- ✅ Application launches without crashes
- ✅ New features work as expected
- ✅ Existing features still work (regression testing)
- ✅ Generated code is valid C# and compiles
- ✅ UI changes look good in both light and dark themes (if applicable)
- ✅ Data binding works correctly
- ✅ Save/Load preserves your new data correctly
- ✅ No XAML designer errors or warnings

#### 5. Update Documentation

If your changes affect:

- **Public API** → Update XML documentation comments
- **User-facing features** → Update [README.md](README.md)
- **Development process** → Update [CONTRIBUTING.md](CONTRIBUTING.md) or [CODING_STANDARDS.md](CODING_STANDARDS.md)

#### 6. Commit and Push

```bash
# Stage your changes
git add .

# Commit with a clear message
git commit -m "Add support for Location blueprint generation"

# Push to your fork
git push origin feature/your-feature-name
```

#### 7. Create a Pull Request

1. Go to the original repository on GitHub
2. Click "New Pull Request"
3. Select your fork and branch
4. Fill out the PR template with:
   - **Description**: What does this PR do?
   - **Motivation**: Why is this change needed?
   - **Changes**: List of key changes made
   - **Testing**: How did you test this?
   - **Screenshots**: If UI changes, include before/after screenshots
   - **Related Issues**: Link to any related GitHub issues

**PR Title Format:**
```
[Feature] Add Location blueprint support
[Fix] Resolve schedule time validation bug
[Refactor] Simplify NpcCodeGenerator structure
[UI] Improve NPC editor tab layout
```

## Code Review Process

### What to Expect

1. **Initial Review** - A maintainer will review your PR within a few days
2. **Feedback** - You may receive comments requesting changes
3. **Iteration** - Make requested changes and push updates to the same branch
4. **Approval** - Once approved, your PR will be merged
5. **Credit** - Your contribution will be credited in the commit history

### Review Criteria

PRs are evaluated based on:

- ✅ **Follows coding standards** - Adheres to CODING_STANDARDS.md
- ✅ **Code quality** - Clean, readable, maintainable code
- ✅ **MVVM compliance** - Proper separation of concerns
- ✅ **Documentation** - XML docs for public APIs
- ✅ **Testing** - Changes have been tested
- ✅ **No regressions** - Doesn't break existing functionality
- ✅ **UI consistency** - Follows Material Design patterns
- ✅ **Generated code quality** - Produces valid, well-formatted code

### Responding to Feedback

- Be responsive to review comments
- Ask questions if feedback is unclear
- Make requested changes promptly
- Mark conversations as resolved once addressed
- Be open to suggestions and alternative approaches
- Maintain a professional and collaborative tone

## Testing Guidelines

### Manual Testing Checklist

For **all changes**, verify:

- [ ] Application builds without errors or warnings
- [ ] Application launches successfully
- [ ] Main window loads and is responsive
- [ ] No console errors or exceptions

For **NPC-related changes**, verify:

- [ ] Can create new NPC blueprint
- [ ] Can edit NPC properties across all tabs
- [ ] Can add/remove schedule actions
- [ ] Can add/remove drug affinities
- [ ] Can add/remove connections
- [ ] Customer/Dealer tabs toggle visibility correctly
- [ ] Generated code is valid C#
- [ ] Generated code compiles when added to a Schedule I mod project
- [ ] Save project preserves NPC data
- [ ] Load project restores NPC data correctly

For **Quest-related changes**, verify:

- [ ] Can create new Quest blueprint
- [ ] Can edit quest properties
- [ ] Can add/remove triggers
- [ ] Generated code is valid C#
- [ ] Save/load works correctly

For **UI changes**, verify:

- [ ] Layout looks correct at different window sizes
- [ ] Material Design styling is consistent
- [ ] Controls are accessible via keyboard (Tab navigation)
- [ ] Tooltips work (if added)
- [ ] Data binding updates UI correctly
- [ ] No XAML designer warnings

### Code Generation Testing

To test generated code:

1. Create a test NPC/Quest in the tool
2. Generate code (use the Generate button)
3. Copy generated code to a Schedule I mod project
4. Attempt to build the mod project
5. Verify no compilation errors
6. Load the mod in Schedule I (if possible)
7. Verify the NPC/Quest appears in-game with correct properties

## Common Development Workflows

### Adding a New NPC Feature

1. **Update Model** (`Models/NpcBlueprint.cs` or create new model class)
   - Add property with backing field
   - Use `SetProperty` in setter
   - Add `[JsonProperty]` attribute
   - Initialize with sensible default

2. **Update Code Generator** (`Services/CodeGeneration/Npc/`)
   - Add generation logic in appropriate generator class
   - Use `ICodeBuilder.OpenBlock()` and `CloseBlock()` for structure
   - Use `CodeFormatter.EscapeString()` for string literals
   - Generate code conditionally based on blueprint properties

3. **Update UI** (`Views/NpcPropertiesControl.xaml`)
   - Add UI controls in appropriate tab
   - Use Material Design components
   - Bind to ViewModel/Model properties
   - Add event handlers if needed (code-behind)

4. **Test End-to-End**
   - Create NPC with new feature
   - Generate code
   - Verify generated code is valid
   - Save and reload project

5. **Update Documentation**

### Creating a Reusable Custom Control

1. **Create Control Files**
   - Add `.xaml` and `.xaml.cs` files in `Views/Controls/`
   - Inherit from `UserControl`

2. **Define Dependency Properties**
   ```csharp
   public static readonly DependencyProperty MyProperty =
       DependencyProperty.Register(
           nameof(MyValue),
           typeof(MyType),
           typeof(MyControl),
           new FrameworkPropertyMetadata(
               defaultValue,
               FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
   ```

3. **Design XAML Layout**
   - Use Material Design components
   - Bind internal elements to Dependency Properties
   - Keep self-contained

4. **Add Validation** (if needed)
   - Implement property changed callbacks
   - Validate and coerce values

5. **Document Usage**
   - Add XML summary to class
   - Document each Dependency Property

### Extending Code Generation

1. **Identify S1API Pattern**
   - Review S1API documentation or examples
   - Understand the builder pattern or method signature

2. **Create/Update Generator**
   - Add method to appropriate generator class
   - Use `ICodeBuilder` exclusively
   - Follow existing patterns

3. **Handle Edge Cases**
   - Check for null/empty values
   - Generate conditionally when appropriate
   - Provide sensible defaults

4. **Format Output**
   - Ensure proper indentation (via `ICodeBuilder`)
   - Add appropriate comments if complex
   - Match S1API code style

5. **Test Generated Code**
   - Generate with various blueprint configurations
   - Verify compilation
   - Test in Schedule I if possible

## Debugging Tips

### WPF Binding Issues

- Enable binding error output in Output window (Debug → Windows → Output)
- Check for `System.Windows.Data Error` messages
- Verify property names match exactly (case-sensitive)
- Ensure `INotifyPropertyChanged` is implemented correctly
- Use Snoop or WPF Inspector tools for runtime inspection

### Code Generation Issues

- Add breakpoints in generator methods
- Inspect `ICodeBuilder` state during generation
- Print/log intermediate builder state
- Verify sanitization functions work correctly
- Test with edge case inputs (empty strings, special characters, etc.)

### XAML Designer Issues

- Check for missing namespaces
- Verify resource references are correct
- Rebuild project if designer shows stale errors
- Check Output window for detailed error messages

## Getting Help

If you need help or have questions:

1. **Check Documentation**
   - [CODING_STANDARDS.md](CODING_STANDARDS.md) - Code style
   - [README.md](README.md) - User documentation

2. **Search Existing Issues**
   - Someone may have encountered the same problem
   - Check closed issues too

3. **Ask in Discussions**
   - Use GitHub Discussions for general questions
   - Describe what you're trying to do and what's not working

4. **Create an Issue**
   - For bugs or unclear documentation
   - Provide reproduction steps and context

## Recognition

All contributors will be recognized in:

- Git commit history
- GitHub contributors page
- Potential CONTRIBUTORS.md file (if created)

Thank you for contributing to the Schedule1 Modding Tool! Your efforts help make modding Schedule I more accessible and enjoyable for everyone.

## Quick Reference

### Build Commands

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Build specific project
dotnet build Schedule1ModdingTool.csproj

# Run application
dotnet run --project Schedule1ModdingTool.csproj

# Clean build artifacts
dotnet clean
```

### Useful Git Commands

```bash
# Create feature branch
git checkout -b feature/my-feature

# Stage all changes
git add .

# Commit with message
git commit -m "Description of changes"

# Push to your fork
git push origin feature/my-feature

# Update your branch with latest main
git checkout main
git pull upstream main
git checkout feature/my-feature
git rebase main

# Squash last 3 commits (interactive)
git rebase -i HEAD~3
```

### File Locations Quick Reference

- **Models**: `Models/`
- **ViewModels**: `ViewModels/`
- **Views (XAML)**: `Views/` and `Views/Controls/`
- **Code Generation**: `Services/CodeGeneration/`
- **Utilities**: `Utils/`
- **Documentation**: `CODING_STANDARDS.md`, `CONTRIBUTING.md`

## License

By contributing to this project, you agree that your contributions will be licensed under the same license as the project (see [LICENSE](LICENSE) file).
