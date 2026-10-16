# 1.0.8 release notes

## Added

- Import supported `.glb` models directly into the 3D Models library and use them with custom products and furniture. Generated mods embed the model and load it through S1MAPI.
- Inspect GLB files in the offline 3D viewer with orbit, zoom, pan, framing, and wireframe controls.
- Adjust an item's GLB scale, XYZ rotation, and XYZ offset in the editor.
- Open a 3D Scene Editor from the model library. Add multiple imported GLBs and basic shapes (cube, sphere, cylinder, cone, plane) to one scene. Select objects in the viewport or object list, then move, rotate, and scale them with colored gizmos or numeric fields. Rename, recolor, duplicate, delete, frame, and view them in wireframe.
- Use undo/redo, position/rotation/scale snapping, world or local transform axes, and front/right/top camera views while editing. Hide individual scene objects from the preview and exported GLB using the scene outliner.
- Refined the scene editor into a docked modeling workspace with a dedicated scene collection, larger viewport, and object inspector. Added orthographic projection, grid visibility, reset transform, and object copy/paste (including Ctrl+C, Ctrl+V, and Ctrl+D shortcuts).
- Save the editable scene in the `.s1proj` project file and reopen it later. Export the composition as a new GLB in the 3D Models library, then assign that GLB to a Custom Product or Furniture item. Exports are checked against the same 64 MB GLB import profile.
- Keep the quick offline GLB preview and item placement controls for users who only need one model.

## Fixed

- Closing a changed scene now offers Save, Discard, and Cancel. Save writes the latest scene before closing; Discard closes without saving; Cancel keeps the editor open.
- Removed the unfinished live in-game model placement bridge and its launch controls. Model placement remains editable and savable in the offline preview; generated mods still apply those values.
- GLB validation accepts `KHR_materials_specular`, so files using that optional material extension can be imported. The game may render simpler specular highlights than the offline preview.
- Invalid or unsupported GLB features continue to produce export guidance instead of failing later during mod generation.

## Requirements

- Generated GLB mods require S1MAPI 2.0.1 or a compatible newer version in the game's `UserLibs` folder. Use the DLL matching the game's Mono or IL2CPP runtime and restart the game after installing it.
