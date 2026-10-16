import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { GLTFExporter } from 'three/addons/exporters/GLTFExporter.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { TransformControls } from 'three/addons/controls/TransformControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';

const $ = id => document.getElementById(id);
const canvas = document.querySelector('canvas');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, preserveDrawingBuffer: false });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.outputColorSpace = THREE.SRGBColorSpace;
const scene = new THREE.Scene();
scene.background = new THREE.Color('#1c1e21');
const root = new THREE.Group();
root.name = 'ModCreator Scene';
scene.add(root);
const grid = new THREE.GridHelper(20, 20, 0x5b6067, 0x33363b);
scene.add(grid);
scene.add(new THREE.AxesHelper(1));
scene.add(new THREE.HemisphereLight(0xffffff, 0x586070, 2));
const perspectiveCamera = new THREE.PerspectiveCamera(45, 1, 0.01, 2000);
const orthographicCamera = new THREE.OrthographicCamera(-4, 4, 4, -4, 0.01, 2000);
let camera = perspectiveCamera;
let orthoViewHeight = 8;
camera.position.set(4, 3, 5);
const orbit = new OrbitControls(camera, canvas);
orbit.target.set(0, 0.5, 0);
orbit.enableDamping = true;
orbit.update();
const environmentMaker = new THREE.PMREMGenerator(renderer);
const room = new RoomEnvironment();
const environment = environmentMaker.fromScene(room);
scene.environment = environment.texture;
room.dispose();
environmentMaker.dispose();
const gizmo = new TransformControls(camera, canvas);
scene.add(gizmo.getHelper());
gizmo.addEventListener('dragging-changed', event => { orbit.enabled = !event.value; });
gizmo.addEventListener('objectChange', () => {
  if (selected) selected.scale.set(
    Math.max(0.001, selected.scale.x),
    Math.max(0.001, selected.scale.y),
    Math.max(0.001, selected.scale.z));
  syncInspector();
  markDirty();
});
const selectionBox = new THREE.BoxHelper(new THREE.Object3D(), 0xffaa4d);
selectionBox.visible = false;
scene.add(selectionBox);
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
const loader = new GLTFLoader();
const exporter = new GLTFExporter();
const library = new Map();
const modelCache = new Map();
const pending = new Map();
let requestNumber = 0;
let selected = null;
let dirty = false;
let revision = 0;
let wireframe = false;
let exporting = false;
let pointerStart = null;
let gizmoWasDragging = false;
let snapshots = [];
let historyIndex = -1;
let restoring = false;
let savedScene = '[]';
let copiedObject = null;

function status(message, error = false) {
  $('status').textContent = message;
  $('status').classList.toggle('error', error);
}
function markDirty() {
  dirty = JSON.stringify(serialize()) !== savedScene;
  revision++;
  window.chrome.webview.postMessage({ type: 'dirtyChanged', dirty });
  if (!exporting) status(dirty ? 'Unsaved scene changes. Save scene or export a new GLB.' : 'Scene matches the saved project.');
}
function updateHistoryButtons() {
  $('undo').disabled = historyIndex <= 0 || restoring;
  $('redo').disabled = historyIndex >= snapshots.length - 1 || restoring;
}
function commitHistory() {
  if (restoring) return;
  const state = JSON.stringify({ objects: serialize(), selection: selected?.userData.sceneObjectId || null });
  if (snapshots[historyIndex] === state) return;
  snapshots = snapshots.slice(0, historyIndex + 1);
  snapshots.push(state);
  if (snapshots.length > 100) snapshots.shift();
  historyIndex = snapshots.length - 1;
  updateHistoryButtons();
}
async function travelHistory(direction) {
  const next = historyIndex + direction;
  if (restoring || next < 0 || next >= snapshots.length) return;
  restoring = true;
  updateHistoryButtons();
  try {
    const state = JSON.parse(snapshots[next]);
    const added = [];
    for (const object of state.objects) added.push(await buildObject(object));
    gizmo.detach();
    for (const old of [...root.children]) { root.remove(old); disposePrimitive(old); }
    for (const object of added) root.add(object);
    historyIndex = next;
    selectObject(root.children.find(object => object.userData.sceneObjectId === state.selection) || null);
    markDirty();
  } catch (error) { status(`Undo/redo failed: ${error.message}`, true); }
  finally { restoring = false; updateHistoryButtons(); }
}
function id() { return crypto.randomUUID().replaceAll('-', ''); }
function round(value) { return Math.round(value * 1e6) / 1e6; }
function requestHost(type, data = {}) {
  const token = ++requestNumber;
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(token); reject(new Error('The editor did not respond.')); }, 30000);
    pending.set(token, { resolve, reject, timer });
    window.chrome.webview.postMessage({ type, token, ...data });
  });
}
async function loadModel(modelId) {
  if (!modelCache.has(modelId)) {
    modelCache.set(modelId, (async () => {
      const reply = await requestHost('requestModel', { modelId });
      return (await loader.loadAsync(reply.url)).scene;
    })());
  }
  try { return (await modelCache.get(modelId)).clone(true); }
  catch (error) { modelCache.delete(modelId); throw error; }
}
function primitive(kind, color) {
  const geometry = {
    cube: () => new THREE.BoxGeometry(1, 1, 1),
    sphere: () => new THREE.SphereGeometry(0.5, 32, 20),
    cylinder: () => new THREE.CylinderGeometry(0.5, 0.5, 1, 32),
    cone: () => new THREE.ConeGeometry(0.5, 1, 32),
    plane: () => new THREE.PlaneGeometry(1, 1)
  }[kind]?.();
  if (!geometry) throw new Error(`Unsupported shape: ${kind}`);
  const mesh = new THREE.Mesh(geometry, new THREE.MeshStandardMaterial({ color, side: THREE.DoubleSide, roughness: 0.72 }));
  if (kind === 'plane') mesh.rotation.x = -Math.PI / 2;
  return mesh;
}
async function buildObject(data) {
  const group = new THREE.Group();
  const kind = data.kind || 'cube';
  const modelId = data.modelId || null;
  const color = /^#[0-9a-f]{6}$/i.test(data.color || '') ? data.color : '#d0d0d0';
  if (kind === 'model') {
    if (!library.has(modelId)) throw new Error(`A scene model is missing from the library: ${modelId}`);
    group.add(await loadModel(modelId));
  } else group.add(primitive(kind, color));
  group.userData.sceneObjectId = data.id || id();
  group.userData.kind = kind;
  group.userData.modelId = modelId;
  group.userData.color = color;
  group.name = data.name || (kind === 'model' ? library.get(modelId).name : kind[0].toUpperCase() + kind.slice(1));
  group.position.fromArray(data.position || [0, 0, 0]);
  group.rotation.set(...(data.rotation || [0, 0, 0]).map(THREE.MathUtils.degToRad));
  group.scale.fromArray(data.scale || [1, 1, 1]);
  group.visible = data.visible !== false;
  if (wireframe) group.traverse(object => {
    if (!object.isMesh) return;
    for (const material of Array.isArray(object.material) ? object.material : [object.material]) material.wireframe = true;
  });
  return group;
}
async function addObject(data, select = true) {
  const group = await buildObject(data);
  root.add(group);
  refreshOutliner();
  if (select) selectObject(group);
  return group;
}
function serialize() {
  return root.children.map(group => ({
    id: group.userData.sceneObjectId,
    name: group.name,
    kind: group.userData.kind,
    modelId: group.userData.modelId,
    color: group.userData.color,
    visible: group.visible,
    position: group.position.toArray().map(round),
    rotation: [group.rotation.x, group.rotation.y, group.rotation.z].map(value => round(THREE.MathUtils.radToDeg(value))),
    scale: group.scale.toArray().map(round)
  }));
}
function refreshOutliner() {
  const list = $('objects');
  list.replaceChildren();
  $('object-count').textContent = `${root.children.length} object${root.children.length === 1 ? '' : 's'}`;
  for (const group of root.children) {
    const row = document.createElement('div');
    row.className = 'object-row';
    const button = document.createElement('button');
    button.textContent = group.name;
    button.title = group.name;
    button.setAttribute('aria-pressed', String(group === selected));
    button.addEventListener('click', () => selectObject(group));
    const visibility = document.createElement('button');
    visibility.className = 'visibility';
    visibility.title = group.visible ? 'Hide object' : 'Show object';
    visibility.setAttribute('aria-label', visibility.title);
    visibility.textContent = group.visible ? '◉' : '○';
    visibility.addEventListener('click', () => {
      group.visible = !group.visible;
      if (selected === group) selectionBox.visible = group.visible;
      refreshOutliner(); markDirty(); commitHistory();
    });
    row.append(button, visibility);
    list.append(row);
  }
  if (!root.children.length) {
    const message = document.createElement('p');
    message.className = 'muted';
    message.textContent = 'Empty scene. Add a shape or library GLB.';
    list.append(message);
  }
}
function selectObject(group) {
  selected = group;
  if (group) {
    gizmo.attach(group);
    selectionBox.setFromObject(group);
    selectionBox.visible = group.visible;
  } else {
    gizmo.detach();
    selectionBox.visible = false;
  }
  $('selected-controls').hidden = !group;
  $('nothing-selected').hidden = !!group;
  $('object-actions').hidden = !group;
  refreshOutliner();
  syncInspector();
}
function syncInspector() {
  if (!selected) return;
  selectionBox.setFromObject(selected);
  const fields = {
    px: selected.position.x, py: selected.position.y, pz: selected.position.z,
    rx: THREE.MathUtils.radToDeg(selected.rotation.x),
    ry: THREE.MathUtils.radToDeg(selected.rotation.y),
    rz: THREE.MathUtils.radToDeg(selected.rotation.z),
    sx: selected.scale.x, sy: selected.scale.y, sz: selected.scale.z
  };
  for (const [key, value] of Object.entries(fields))
    if (document.activeElement !== $(key)) $(key).value = String(round(value));
  if (document.activeElement !== $('name')) $('name').value = selected.name;
  $('color-row').hidden = selected.userData.kind === 'model';
  $('color').value = selected.userData.color;
}
function frame() {
  const target = selected || root;
  const box = new THREE.Box3().setFromObject(target);
  if (box.isEmpty()) { orbit.target.set(0, 0, 0); camera.position.set(4, 3, 5); }
  else {
    const center = box.getCenter(new THREE.Vector3());
    const radius = Math.max(box.getSize(new THREE.Vector3()).length(), 1);
    orbit.target.copy(center);
    camera.position.copy(center).add(new THREE.Vector3(1, 0.7, 1).normalize().multiplyScalar(radius * 1.8));
    camera.near = Math.max(radius / 1000, 0.001);
    camera.far = Math.max(radius * 100, 100);
    if (camera.isOrthographicCamera) { orthoViewHeight = radius * 1.8; camera.zoom = 1; updateProjection(); }
    camera.updateProjectionMatrix();
  }
  orbit.update();
}
function updateProjection() {
  const aspect = canvas.clientWidth / Math.max(canvas.clientHeight, 1);
  perspectiveCamera.aspect = aspect;
  perspectiveCamera.updateProjectionMatrix();
  orthographicCamera.left = -orthoViewHeight * aspect / 2;
  orthographicCamera.right = orthoViewHeight * aspect / 2;
  orthographicCamera.top = orthoViewHeight / 2;
  orthographicCamera.bottom = -orthoViewHeight / 2;
  orthographicCamera.updateProjectionMatrix();
}
function setProjection(orthographic) {
  if (camera.isOrthographicCamera === orthographic) return;
  const distance = Math.max(camera.position.distanceTo(orbit.target), 0.1);
  const direction = camera.position.clone().sub(orbit.target).normalize();
  if (orthographic) {
    orthoViewHeight = 2 * distance * Math.tan(THREE.MathUtils.degToRad(perspectiveCamera.fov / 2));
    orthographicCamera.zoom = 1;
    orthographicCamera.position.copy(camera.position);
    orthographicCamera.up.copy(camera.up);
    camera = orthographicCamera;
  } else {
    const height = orthoViewHeight / orthographicCamera.zoom;
    const projectedDistance = height / (2 * Math.tan(THREE.MathUtils.degToRad(perspectiveCamera.fov / 2)));
    perspectiveCamera.position.copy(orbit.target).addScaledVector(direction, projectedDistance);
    perspectiveCamera.up.copy(camera.up);
    camera = perspectiveCamera;
  }
  orbit.object = camera;
  gizmo.camera = camera;
  updateProjection();
  orbit.update();
  $('view-ortho').setAttribute('aria-pressed', String(orthographic));
}
function setMode(mode) {
  gizmo.setMode(mode === 'move' ? 'translate' : mode);
  for (const value of ['move', 'rotate', 'scale']) $(value).setAttribute('aria-pressed', String(value === mode));
}
function setSnap(enabled) {
  gizmo.setTranslationSnap(enabled ? 0.5 : null);
  gizmo.setRotationSnap(enabled ? THREE.MathUtils.degToRad(15) : null);
  gizmo.setScaleSnap(enabled ? 0.1 : null);
  $('snap').setAttribute('aria-pressed', String(enabled));
}
function setView(view) {
  setProjection(view !== 'perspective');
  const target = selected ? new THREE.Box3().setFromObject(selected).getCenter(new THREE.Vector3()) : orbit.target.clone();
  if (!Number.isFinite(target.x)) target.set(0, 0, 0);
  const distance = Math.max(camera.position.distanceTo(orbit.target), 3);
  const direction = { front: [0, 0, 1], right: [1, 0, 0], top: [0, 1, 0], perspective: [1, 0.7, 1] }[view];
  camera.up.set(0, view === 'top' ? 0 : 1, view === 'top' ? -1 : 0);
  orbit.target.copy(target);
  camera.position.copy(target).add(new THREE.Vector3(...direction).normalize().multiplyScalar(distance));
  camera.lookAt(target);
  orbit.update();
  for (const name of ['perspective', 'front', 'right', 'top'])
    $(`view-${name}`).setAttribute('aria-pressed', String(name === view));
}
function removeSelected() {
  if (!selected) return;
  const removed = selected;
  root.remove(selected);
  selectObject(null);
  disposePrimitive(removed);
  markDirty();
  commitHistory();
}
function disposePrimitive(group) {
  if (group.userData.kind === 'model') return;
  group.traverse(object => {
    if (!object.isMesh) return;
    object.geometry.dispose();
    for (const material of Array.isArray(object.material) ? object.material : [object.material]) material.dispose();
  });
}
async function duplicateSelected() {
  if (!selected) return;
  const source = serialize().find(item => item.id === selected.userData.sceneObjectId);
  const clone = await addObject({ ...source, id: id(), name: `${source.name} Copy`, position: [source.position[0] + 0.5, ...source.position.slice(1)] });
  selectObject(clone);
  markDirty();
  commitHistory();
}
function copySelected() {
  if (!selected) return;
  copiedObject = serialize().find(item => item.id === selected.userData.sceneObjectId);
  status(`Copied ${copiedObject.name}. Paste to add another instance.`);
}
async function pasteObject() {
  if (!copiedObject) { status('Select and copy an object first.'); return; }
  const source = copiedObject;
  const group = await addObject({ ...source, id: id(), name: `${source.name} Copy`, position: [source.position[0] + 0.5, source.position[1], source.position[2]] });
  selectObject(group);
  markDirty();
  commitHistory();
}
function resetTransform() {
  if (!selected) return;
  selected.position.set(0, 0, 0);
  selected.rotation.set(0, 0, 0);
  selected.scale.set(1, 1, 1);
  syncInspector();
  markDirty();
  commitHistory();
}
async function saveScene() {
  const savedRevision = revision;
  const objects = serialize();
  await requestHost('saveScene', { objects });
  savedScene = JSON.stringify(objects);
  if (revision === savedRevision) {
    dirty = false;
    window.chrome.webview.postMessage({ type: 'dirtyChanged', dirty: false });
    status('Scene saved to the project. Export as GLB to use the composition on an item.');
  } else status('Scene changed while saving. Save again to keep the latest edits.');
}
async function exportGlb() {
  if (exporting || !root.children.length) return;
  exporting = true;
  $('export').disabled = true;
  try {
    await saveScene();
    status('Building GLB from scene...');
    const bytes = new Uint8Array(await exporter.parseAsync(root, { binary: true, trs: true, onlyVisible: true }));
    if (bytes.length > 64 * 1024 * 1024) throw new Error('The exported GLB exceeds the 64 MB model limit.');
    const exportId = id();
    await requestHost('exportBegin', { exportId, byteLength: bytes.length });
    const chunkSize = 128 * 1024;
    for (let offset = 0; offset < bytes.length; offset += chunkSize) {
      const part = bytes.subarray(offset, Math.min(offset + chunkSize, bytes.length));
      let binary = '';
      for (const byte of part) binary += String.fromCharCode(byte);
      await requestHost('exportChunk', { exportId, data: btoa(binary) });
      status(`Exporting GLB... ${Math.round(100 * Math.min(offset + part.length, bytes.length) / bytes.length)}%`);
    }
    const result = await requestHost('exportFinish', { exportId });
    library.set(result.modelId, { id: result.modelId, name: result.name });
    const option = document.createElement('option');
    option.value = result.modelId;
    option.textContent = result.name;
    $('model-list').append(option);
    status(`Exported ${result.name} to the 3D Models library. Select it on an item to use it in a mod.`);
  } catch (error) { status(`Export failed: ${error.message}`, true); }
  finally { exporting = false; $('export').disabled = false; }
}

for (const mode of ['move', 'rotate', 'scale']) $(mode).addEventListener('click', () => setMode(mode));
$('space').addEventListener('click', () => {
  const local = gizmo.space !== 'local';
  gizmo.setSpace(local ? 'local' : 'world');
  $('space').textContent = local ? 'Local' : 'World';
});
$('snap').addEventListener('click', () => setSnap($('snap').getAttribute('aria-pressed') !== 'true'));
$('undo').addEventListener('click', () => travelHistory(-1));
$('redo').addEventListener('click', () => travelHistory(1));
for (const view of ['perspective', 'front', 'right', 'top']) $(`view-${view}`).addEventListener('click', () => setView(view));
$('frame').addEventListener('click', frame);
$('grid').addEventListener('click', () => {
  grid.visible = !grid.visible;
  $('grid').setAttribute('aria-pressed', String(grid.visible));
});
$('view-ortho').addEventListener('click', () => setProjection(!camera.isOrthographicCamera));
$('wireframe').addEventListener('click', event => {
  wireframe = !wireframe;
  event.target.setAttribute('aria-pressed', String(wireframe));
  root.traverse(object => {
    if (!object.isMesh) return;
    for (const material of Array.isArray(object.material) ? object.material : [object.material]) material.wireframe = wireframe;
  });
});
$('add-model').addEventListener('click', async () => {
  const modelId = $('model-list').value;
  if (!modelId) return;
  try { await addObject({ kind: 'model', modelId }); markDirty(); commitHistory(); }
  catch (error) { status(error.message, true); }
});
for (const kind of ['cube', 'sphere', 'cylinder', 'cone', 'plane'])
  $(`add-${kind}`).addEventListener('click', async () => { await addObject({ kind }); markDirty(); commitHistory(); });
$('delete').addEventListener('click', removeSelected);
$('clear-scene').addEventListener('click', () => {
  if (!root.children.length || !confirm('Remove every object from this scene?')) return;
  for (const object of root.children) disposePrimitive(object);
  root.clear();
  selectObject(null);
  markDirty();
  commitHistory();
});
$('duplicate').addEventListener('click', () => duplicateSelected().catch(error => status(error.message, true)));
$('copy-object').addEventListener('click', copySelected);
$('paste-object').addEventListener('click', () => pasteObject().catch(error => status(error.message, true)));
$('reset-transform').addEventListener('click', resetTransform);
$('save').addEventListener('click', () => saveScene().catch(error => status(`Save failed: ${error.message}`, true)));
$('export').addEventListener('click', exportGlb);
$('name').addEventListener('input', event => { if (!selected) return; selected.name = event.target.value.slice(0, 80); refreshOutliner(); markDirty(); });
$('name').addEventListener('change', commitHistory);
$('color').addEventListener('input', event => {
  if (!selected || selected.userData.kind === 'model') return;
  selected.userData.color = event.target.value;
  selected.traverse(object => { if (object.isMesh) object.material.color.set(event.target.value); });
  markDirty();
  commitHistory();
});
for (const key of ['px', 'py', 'pz', 'rx', 'ry', 'rz', 'sx', 'sy', 'sz'])
  $(key).addEventListener('change', event => {
    if (!selected) return;
    const value = Number(event.target.value);
    if (!Number.isFinite(value) || Math.abs(value) > 10000 || (key[0] === 's' && (value <= 0 || value > 1000))) { syncInspector(); return; }
    const axis = key[1];
    if (key[0] === 'p') selected.position[axis] = value;
    else if (key[0] === 'r') selected.rotation[axis] = THREE.MathUtils.degToRad(value);
    else selected.scale[axis] = value;
    syncInspector();
    markDirty();
    commitHistory();
  });
canvas.addEventListener('pointerdown', event => { pointerStart = { x: event.clientX, y: event.clientY }; gizmoWasDragging = false; });
gizmo.addEventListener('dragging-changed', event => {
  if (event.value) gizmoWasDragging = true;
  else commitHistory();
});
canvas.addEventListener('pointerup', event => {
  if (!pointerStart || gizmoWasDragging || Math.hypot(event.clientX - pointerStart.x, event.clientY - pointerStart.y) > 4) return;
  const rect = canvas.getBoundingClientRect();
  pointer.set((event.clientX - rect.left) / rect.width * 2 - 1, -(event.clientY - rect.top) / rect.height * 2 + 1);
  raycaster.setFromCamera(pointer, camera);
  const hit = raycaster.intersectObjects(root.children, true)[0]?.object;
  let group = hit;
  while (group && group.parent !== root) group = group.parent;
  selectObject(group?.parent === root ? group : null);
});
window.addEventListener('keydown', event => {
  if (['INPUT', 'SELECT'].includes(document.activeElement?.tagName)) return;
  const key = event.key.toLowerCase();
  if (event.ctrlKey && key === 'z') { event.preventDefault(); travelHistory(event.shiftKey ? 1 : -1); }
  else if (event.ctrlKey && key === 'y') { event.preventDefault(); travelHistory(1); }
  else if (event.ctrlKey && key === 'c') { event.preventDefault(); copySelected(); }
  else if (event.ctrlKey && key === 'v') { event.preventDefault(); pasteObject().catch(error => status(error.message, true)); }
  else if (event.ctrlKey && key === 'd') { event.preventDefault(); duplicateSelected().catch(error => status(error.message, true)); }
  else if (key === 'g') setMode('move');
  else if (key === 'r') setMode('rotate');
  else if (key === 's' && !event.ctrlKey) setMode('scale');
  else if (key === 'f') frame();
  else if (key === 'delete' || key === 'backspace') removeSelected();
  else if (key === 's' && event.ctrlKey) { event.preventDefault(); saveScene().catch(error => status(error.message, true)); }
});
window.chrome.webview.addEventListener('message', async event => {
  const message = event.data;
  if (message.type === 'ack') {
    const entry = pending.get(message.token);
    if (!entry) return;
    clearTimeout(entry.timer);
    pending.delete(message.token);
    if (message.error) entry.reject(new Error(message.error));
    else entry.resolve(message);
  } else if (message.type === 'initialize') {
    try {
      for (const model of message.models || []) library.set(model.id, model);
      const list = $('model-list');
      for (const model of library.values()) {
        const option = document.createElement('option');
        option.value = model.id;
        option.textContent = model.name;
        list.append(option);
      }
      let skipped = 0;
      for (const object of message.objects || []) {
        try { await addObject(object, false); }
        catch (error) { skipped++; status(`Skipped ${object.name || 'object'}: ${error.message}`, true); }
      }
      selectObject(root.children[0] || null);
      savedScene = JSON.stringify(message.objects || []);
      commitHistory();
      if (message.addModelId) { await addObject({ kind: 'model', modelId: message.addModelId }); markDirty(); commitHistory(); }
      else if (skipped) status(`${skipped} scene object(s) could not load. Restore their library GLBs before saving.`, true);
      else status('Scene ready. Select an object or add a model or shape.');
      frame();
    } catch (error) { status(`Scene could not load: ${error.message}`, true); }
  } else if (message.type === 'saveAndClose') {
    try {
      await saveScene();
      if (dirty) throw new Error('The scene changed during saving. Save again before closing.');
      window.chrome.webview.postMessage({ type: 'closeEditor' });
    } catch (error) {
      status(`Save failed; editor remains open: ${error.message}`, true);
      window.chrome.webview.postMessage({ type: 'closeSaveFailed' });
    }
  }
});
new ResizeObserver(() => {
  const width = canvas.clientWidth;
  const height = canvas.clientHeight;
  renderer.setSize(width, height, false);
  updateProjection();
}).observe(canvas);
function animate() {
  requestAnimationFrame(animate);
  orbit.update();
  if (selectionBox.visible) selectionBox.update();
  renderer.render(scene, camera);
}
animate();
window.chrome.webview.postMessage({ type: 'ready' });
