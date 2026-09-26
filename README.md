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
- Weed strains
  - native S1API 3.2.0 weed products with a stable namespaced ID, 1–8 effects, and optional main, secondary, leaf, and stem colors
- Custom products
  - S1API products with a logical product kind, optional native mixing, price, legality, addictiveness, quality, packaging, effect durations, discovery, shop stock, console alias, and optional Product Manager section
- Project resources
  - embedded assets, icons, textures, and generated mod resource packaging
- 3D Models
  - project library for Unity AssetBundles, with model selection for custom products, furniture, clothing, additives, and ordinary items
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

### Create a weed strain

Use **Add > Weed Strain** in the menu or workspace, then set a permanent ID such as `mymod:blue_dream`, a name, and 1–8 distinct effects such as `Euphoric, Calming`. Optionally set the four weed color channels. The generated mod registers the strain on S1API's load-complete event; S1API's native weed creator handles mixing, price, discovery, visuals, persistence, and network replication. The ID is saved in game data, so changing it later can break existing saves.

Use **Add > Custom Product** for a product such as a tablet or powder. Set its product ID and kind ID, choose a live game product as the representation template (the default is `ogkush`), then configure effects, price, legal status, base addictiveness, quality, durations, packaging, discovery, shop stock, an optional console alias, and an optional Product Manager section. Enable mixing to select a native mixer map and optionally color generated mixes from their effects. Mixing settings belong to the Product Kind ID, so every product with the same kind ID must use the same settings. Generated outputs keep the player's mix name, the source product's kind, and its source price. The generated mod registers a save provider during initialization, builds the definition before save restoration, and optionally discovers or lists it after the save loads. Production station recipes remain separate from mixing. See the [S1API mixing profile API](https://ifbars.github.io/S1API/api/S1API.Products.ProductMixingProfileBuilder.html) for runtime details.

### Add a production recipe

Open an ordinary item or custom product and use **Production Recipes** to add a Chemistry Station recipe. Set a stable namespaced recipe ID, title, cook time, output quantity, and liquid color. Add ingredient groups; each group requires its own quantity, and its item IDs are alternatives. Choose whether the recipe starts visible and unlocked. The generated mod registers the recipe through S1API after its output item exists. Custom products can have production recipes without enabling mixing. S1API currently exposes this recipe builder for the Chemistry Station; other production stations are not included in this editor workflow.

### Use a 3D model

Open **3D Models** in the sidebar, import a Unity AssetBundle containing a `GameObject` prefab, and enter the prefab's exact asset name. Select that model in an item editor's **3D Model** section. The editor copies the bundle into the project and embeds it in the generated mod.

Custom products use S1API presentation profiles for loose, held, stored, station, and generated icon visuals. Non-brick packaging also uses the selected model as its contents. Furniture uses S1API's furniture builder for the placed object and placement ghost. Ordinary items, additives, and clothing use the prefab as a stored-item visual; these prefabs must contain a `StoredItem` component. Worn clothing also needs a compatible clothing component and rig. Native weed strains currently support custom colors but do not have a S1API custom-model hook. Raw OBJ, FBX, and GLB files must first be converted into a Unity prefab AssetBundle.

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
- [SirTidez](https://ko-fi.com/sirtidez)
