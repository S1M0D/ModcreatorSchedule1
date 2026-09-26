# ModCreator Schedule 1 - 1.0.7-beta

Version 1.0.7-beta adds drug authoring, production recipes, and a project-wide 3D model library to ModCreator Schedule 1. The editor still generates a normal C# mod project using S1API, so you can inspect and extend the generated code after building it.

## Added

### Native weed strains

- Create a weed strain from **Add > Weed Strain** in the menu or workspace.
- Give each strain a permanent namespaced product ID, a display name, and one to eight effects from the supported effect list.
- Optionally choose separate main bud, secondary bud, leaf, and stem colors.
- Generated code registers the strain through S1API's native weed creation path after the save loads. The game handles its native mixing, discovery, pricing, visuals, and save behavior.

### Custom products and optional mixing

- Create a fixed custom product with its own product ID and logical product kind ID. Choose a native product as its representation template; `ogkush` is the editor default.
- Configure effects, price, legal status, base addictiveness, default quality, player and NPC effect durations, and allowed packaging.
- Choose whether the product is discovered, listed in the Product Manager, or stocked by compatible or named shops. Set an optional shop price, console give alias, and Product Manager section name and color.
- Enable native mixing for a product kind, choose its mixer map, and optionally color mixed outputs from their effects. The generated output keeps the mix name, source product kind, and source price supplied by the mixing profile.
- Custom products register a save provider during mod initialization, create their definitions before save restoration, then apply discovery and shop settings when loading completes.

Mixing is optional. A product can have a production recipe without enabling mixing. Products sharing one kind ID must use the same mixing settings; the editor now checks this before generating the mod.

### Production recipes

- Add **Production Recipes** to ordinary items and custom products. These recipes currently use the Chemistry Station.
- Set a title, cook time, output quantity, and final liquid color for each recipe.
- Add ingredient groups with their own quantities. Every group is required; multiple item IDs inside one group are alternatives for that group.
- Give recipes stable, namespaced IDs so different recipes can produce the same output quantity without colliding. New recipes for namespaced items receive a suggested ID, and existing recipes without an ID keep their previous identifier behavior.
- Set whether a recipe starts visible and unlocked. The generated mod registers the recipe after its output item exists.
- The editor checks recipe IDs, required ingredients, quantities, color values, and duplicate IDs across a project before generation.

### 3D Models library

- Use the new **3D Models** sidebar page to import a Unity AssetBundle into the project and specify the exact `GameObject` prefab asset name inside it.
- Select an imported model from item editors for custom products, ordinary items, furniture, additives, and clothing.
- The generated mod embeds the AssetBundle and loads the selected prefab at runtime. Custom products use an S1API presentation profile for their loose visual and generated icon; supported non-brick packaging can use the model as its contents.
- Furniture uses its selected model for the placed object. Ordinary items, additives, and clothing use it as a stored-item visual where their S1API item builders support that path.

## Changed and fixed

- Switched weed strains and custom products to dedicated editor layouts. This fixes the overlapping fields that appeared when changing an item's type.
- Added menu and workspace actions for creating weed strains and custom products.
- Expanded project saving, copying, resource packaging, and code generation to carry the new product, mixing, recipe, and model settings.
- Added validation for permanent product IDs, supported effects and colors, required model prefab names, recipe data, and incompatible shared mixing profiles.
- Updated generated project references and the connector build path for S1API.Forked 3.2.0.
- Rebuilt `ModCreatorConnector.dll`. The portable release keeps the connector project beside the editor because the app rebuilds it for game-connected workflows.

## Installation and requirements

1. Extract the ZIP to a writable folder, keeping all files and the `ModCreatorConnector` folder together.
2. Run `Schedule1ModdingTool.exe` and set the Schedule I installation path in Settings.
3. Create or open a project, then use the new item and model tools. Build the generated mod from the editor when ready.

This package targets Windows x64 and requires the .NET 8 Desktop Runtime. Building generated mods and running connector-assisted workflows requires the .NET SDK. Game testing requires a compatible Schedule I installation, MelonLoader, and S1API 3.2.0.

## Current limits and verification

- Production recipes currently target the Chemistry Station; other production stations are not exposed by this editor workflow.
- Model imports require Unity prefab AssetBundles built for the game's platform and Unity version. Raw OBJ, FBX, and GLB files must be converted first. Ordinary item stored models need a compatible `StoredItem` component, and worn clothing needs a compatible clothing component and rig.
- Native weed strains support custom colors but do not use the custom model profile.
- The editor Release build and connector build pass. A generated sample mod containing a custom product and Chemistry Station recipe compiled against S1API 3.2.0. The new gameplay features have not yet been verified in a running game.
