# 1.1.0 release notes

## New in the Scene Editor

- Name each exported GLB before adding it to the 3D Models library. Each export creates a separate model asset, so earlier exports remain available.
- Create torus shapes alongside cubes, spheres, cylinders, cones, and planes.
- Edit shape color, metalness, and roughness. These material settings are saved in the project and included in exported GLBs. Existing scenes open with the previous material defaults.
- Place a selected object on the ground with one click, aligning the lowest point of its mesh bounding box to Y = 0.
- Isolate the selected object, then use Show all to restore the rest of the scene. Visibility is saved with the scene and controls which objects are exported.

## Improved

- Prevent a scene with missing or unreadable library GLBs from being saved with those objects silently removed. Restore the GLBs and reopen the editor to continue.
- Show a clear error when exporting a scene with no visible objects or adding more than 200 objects.
- Keep duplicate and pasted object names within the supported project limit.

## Requirements

- The offline GLB viewer and Scene Editor require Microsoft Edge WebView2 Runtime.
- Generated mods that use GLB models require S1MAPI 2.0.1 or a compatible newer version in the game's `UserLibs` folder, with the DLL matching the game's Mono or IL2CPP runtime.
