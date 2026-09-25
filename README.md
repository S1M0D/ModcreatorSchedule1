# Schedule 1 Modding Tool

Schedule 1 Modding Tool is a WPF desktop editor for building Schedule I mods on top of S1API.

It is aimed at people who want a GUI-first workflow instead of writing every mod feature by hand. The tool stores project data in its own editor format, generates real C# source that targets S1API, builds a mod project, and helps deploy or launch the game for testing.

## What It Does

The tool currently focuses on these authoring workflows:

- Quests
  - quest settings, objectives, rewards, finish behavior, trigger wiring, and generated hook scaffolds
- NPCs
  - schedules, dialogue containers/nodes/choices, dialogue injection, runtime settings, reactions, and hook scaffolds
- Items
  - item registration, shops, equippables, buildable settings, additive settings, recipes, and advanced item hooks
- Custom clothing
  - clothing-specific item flow, legality/stack settings, texture workflows, clothing studio helpers, and texture extraction/import support
- Project resources
  - embedded assets, icons, textures, and generated mod resource packaging
- Live helper workflows
  - connector-assisted game launch, runtime catalog access, and in-editor help via `?` explanations on complex fields

## How It Works

The editor is not a replacement runtime for S1API. It is an authoring layer on top of it.

The pipeline is:

1. Create or edit content in the GUI.
2. Save it as project data inside the mod creator project format.
3. Generate C# files that target S1API.
4. Generate a mod project with resources and references.
5. Build that project into a normal mod DLL.

That means the output is still real generated C# and a real buildable mod, not a closed custom format.

## Current Highlights

- S1API 3.2.0-oriented quest and trigger generation
- Expanded NPC dialogue and runtime authoring
- Broader item support with shop routing and advanced item options
- Clothing Studio with texture helpers and game-data texture extraction
- Inline field help for more confusing editor sections
- Connector workflow for live game-assisted tooling

## Project Layout

- `Models/` contains editor data models and project blueprints
- `ViewModels/` contains MVVM state and commands
- `Views/` contains the WPF editor UI
- `Services/` contains generators, runtime helpers, build/deploy services, and editor support logic
- `Utils/` contains formatting and helper utilities
- `Resources/` contains editor assets and styles
- `ModCreatorConnector/` contains the helper mod used for live editor/game workflows

## Status

This project is still in active development.

- Pre-release builds may be available before a stable release
- Some advanced features still rely on generated hook files for manual C# additions

## Support Us

- [Estonia](https://ko-fi.com/estonla)
- [Bars](https://ko-fi.com/ifbars)
