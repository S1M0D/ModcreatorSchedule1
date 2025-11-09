# S1APITemplate

A template project for creating Schedule One mods using S1API, supporting both Mono and IL2CPP builds with a cross-compatibility option.

## About

This template provides a ready-to-use foundation for developing mods for Schedule One using [S1API](https://github.com/ifBars/S1API). S1API is a cross-compatibility layer that allows you to create mods that work on both Mono and IL2CPP versions of the game without needing to reference the game's Assembly-CSharp.dll directly.

## Features

- **Cross-Platform Compatibility**: Build configurations for Mono, IL2CPP, and Cross-Compat modes
- **Automatic Deployment**: Post-build script that copies your mod to the Mods folder and launches the game
- **Harmony Integration**: Boilerplate Harmony patches class
- **S1API Integration**: Uses the latest S1API.Forked package for enhanced compatibility

## Prerequisites

- **Schedule One** game installed (both versions if you want to test cross-compatibility)
- **MelonLoader** installed for your game version(s)
- **Visual Studio 2022** or later with .NET development workload
- **Git** for cloning this template

## Project Structure

```
S1APITemplate/
├── Core.cs              # Main mod entry point and initialization
├── Integrations/
│   └── HarmonyPatches.cs # Harmony patches for game modifications
├── Utils/
│   └── Constants.cs     # Constants and configuration values
└── S1APITemplate.csproj # Project configuration with build settings
```

## Build Configurations

### Mono
- Targets the alternate Schedule One version (Mono backend)
- Includes Assembly-CSharp references for direct game integration
- Assembly name: `{ProjectName}_Mono.dll`

### IL2CPP
- Targets the main Schedule One version (IL2CPP backend)
- Uses IL2CPP-compatible assemblies
- Assembly name: `{ProjectName}_Il2cpp.dll`

### CrossCompat
- Uses Mono settings but with clean assembly name
- **Excludes Assembly-CSharp references** for cross-compatibility between Mono and IL2CPP
- Assembly name: `{ProjectName}.dll` (no suffix)
- Define constant: `CROSS_COMPAT` available for conditional compilation

## Setup Instructions

1. **Clone or Download** this template repository

2. **Configure Game Paths** (in `S1APITemplate.csproj`):
   ```xml
   <!-- For Mono/CrossCompat -->
   <GamePath>D:\SteamLibrary\steamapps\common\Schedule I_alternate</GamePath>

   <!-- For IL2CPP -->
   <GamePath>D:\SteamLibrary\steamapps\common\Schedule I_main</GamePath>
   ```

3. **Update Assembly Name** (optional):
   Replace `$safeprojectname$` in the project file with your desired mod name

4. **Restore NuGet Packages**:
   ```bash
   dotnet restore
   ```

## Development Workflow

### 1. Choose Your Build Configuration

Choose the appropriate build configuration based on your mod's compatibility needs:

- **CrossCompat Configuration** (Recommended for new mods):
  - Use this for mods that should work on both Mono and IL2CPP versions
  - **Only use S1API abstractions** - do not reference Assembly-CSharp.dll directly
  - For events and other Il2Cpp cases, use S1API.Internal namespace (e.g. EventHelper)
  - This ensures cross-compatibility across game versions
  - Assembly name: `{ProjectName}.dll` (clean, no suffix)

- **Mono Configuration**:
  - Use this for mods targeting only the Mono version of Schedule One
  - Can use S1API as a "tag-along" while also directly accessing game namespaces
  - Includes Mono assembly references for direct game integration
  - Assembly name: `{ProjectName}_Mono.dll`

- **IL2CPP Configuration**:
  - Use this for mods targeting only the IL2CPP version of Schedule One
  - Can use S1API as a "tag-along" while also directly accessing game namespaces
  - Includes IL2CPP assembly references for direct game integration
  - Assembly name: `{ProjectName}_Il2cpp.dll`

### 2. Configure Assembly Access (Mono builds only)

By default, the Mono configuration uses the regular `Assembly-CSharp.dll`. For easier access to private/internal game members:

1. **Install BepInEx Publicizer**:
   ```bash
   # Install via NuGet or download from GitHub
   dotnet tool install -g BepInEx.Publicizer.Cli
   ```

2. **Publicize the Assembly**:
   ```bash
   # Navigate to your Mono game's Managed folder
   cd "D:\SteamLibrary\steamapps\common\Schedule I_alternate\Schedule I_Data\Managed"

   # Publicize Assembly-CSharp.dll
   assembly-publicizer Assembly-CSharp.dll
   ```

3. **Enable Publicized Assembly in Project**:
   In `S1APITemplate.csproj`, change:
   ```xml
   <UsePublicized>false</UsePublicized>
   ```
   to:
   ```xml
   <UsePublicized>true</UsePublicized>
   ```

This will make the project reference `Assembly-CSharp-publicized.dll` instead of the regular assembly, giving you access to private members.

### 3. Implement Your Mod Logic

- **Core.cs**: Main mod class with MelonLoader attributes
- **HarmonyPatches.cs**: Add your Harmony patches here
- **Constants.cs**: Define any constants your mod needs

### 4. Build and Test

```bash
# Build for your chosen configuration
dotnet build --configuration CrossCompat

# Or use Visual Studio's build menu
```

The post-build script will:
- Kill any running game instances
- Copy your mod DLL to the Mods folder
- Launch the game automatically

## Deployment

The template includes automatic deployment via the post-build event. Simply build your project and it will:
1. Stop any running game instances
2. Copy your mod to the Mods folder
3. Launch the game

For manual deployment, copy your built DLL from `bin/{Configuration}/netstandard2.1/` to your game's `Mods/` folder.

## Contributing

This is a template project. Feel free to modify it for your specific modding needs. If you find improvements or fixes, consider contributing back to the community.

## License

This template follows the same MIT license as S1API. See the S1API repository for details.

## Support

- [S1API Documentation](https://ifbars.github.io/S1API-docs/)
- [S1API GitHub Repository](https://github.com/ifBars/S1API)