import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';

const status = document.querySelector('#status');
async function startViewer() {
  const renderer = new THREE.WebGLRenderer({ canvas: document.querySelector('canvas'), antialias: true });
  renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  const scene = new THREE.Scene();
  scene.background = new THREE.Color('#202124');
  const camera = new THREE.PerspectiveCamera(40, 1, 0.01, 1000);
  const controls = new OrbitControls(camera, renderer.domElement);
  const pmrem = new THREE.PMREMGenerator(renderer);
  const room = new RoomEnvironment();
  const environment = pmrem.fromScene(room);
  scene.environment = environment.texture;
  room.dispose();
  pmrem.dispose();
  scene.add(new THREE.HemisphereLight(0xffffff, 0x5b6070, 2));
  const grid = new THREE.GridHelper(10, 10, 0x737780, 0x363a40);
  scene.add(grid);
  scene.add(new THREE.AxesHelper(0.5));
  const placement = new THREE.Group();
  scene.add(placement);
  let model;
  let pendingTransform;

  function render() { renderer.render(scene, camera); }
  function frame() {
    if (!model) return;
    const box = new THREE.Box3().setFromObject(placement);
    if (box.isEmpty()) return;
    const center = box.getCenter(new THREE.Vector3());
    const size = Math.max(box.getSize(new THREE.Vector3()).length(), 0.01);
    controls.target.copy(center);
    camera.position.copy(center).add(new THREE.Vector3(1, 0.65, 1).normalize().multiplyScalar(size * 1.8));
    camera.near = Math.max(size / 1000, 0.0001);
    camera.far = Math.max(size * 100, 100);
    camera.updateProjectionMatrix();
    controls.update();
    render();
  }
  function transform(value) {
    pendingTransform = value;
    if (!model || !value) return;
    const numbers = ['scale', 'rx', 'ry', 'rz', 'x', 'y', 'z'].map(key => value[key]);
    if (!numbers.every(Number.isFinite) || value.scale <= 0 || value.scale > 1000) return;
    placement.scale.setScalar(value.scale);
    // MAPI reflects X when importing glTF into Unity. Mirror the authored Unity
    // transform back into glTF space, preserving Unity's Z-X-Y Euler application.
    placement.position.set(-value.x, value.y, value.z);
    placement.quaternion.setFromEuler(new THREE.Euler(
      THREE.MathUtils.degToRad(value.rx), THREE.MathUtils.degToRad(-value.ry),
      THREE.MathUtils.degToRad(-value.rz), 'YXZ'));
    render();
  }
  let initialTransformReceived = false;
  window.chrome?.webview?.addEventListener('message', event => {
    transform(event.data);
    if (!initialTransformReceived) { initialTransformReceived = true; frame(); }
  });
  controls.addEventListener('change', render);
  document.querySelector('#frame').addEventListener('click', frame);
  document.querySelector('#wireframe').addEventListener('click', event => {
    const enabled = event.target.getAttribute('aria-pressed') !== 'true';
    event.target.setAttribute('aria-pressed', String(enabled));
    model?.traverse(object => {
      for (const material of object.material ? (Array.isArray(object.material) ? object.material : [object.material]) : [])
        material.wireframe = enabled;
    });
    render();
  });
  const observer = new ResizeObserver(() => {
    renderer.setSize(innerWidth, innerHeight, false);
    camera.aspect = innerWidth / Math.max(innerHeight, 1);
    camera.updateProjectionMatrix();
    render();
  });
  observer.observe(document.body);

  try {
    const manager = new THREE.LoadingManager();
    let resourceFailed = false;
    manager.onError = () => { resourceFailed = true; };
    const gltf = await new GLTFLoader(manager).loadAsync('model.glb');
    if (resourceFailed) throw new Error('An embedded model texture could not be loaded.');
    model = gltf.scene;
    placement.add(model);
    transform(pendingTransform);
    frame();
    let triangles = 0;
    model.traverse(object => {
      if (object.isMesh) triangles += (object.geometry.index?.count ?? object.geometry.attributes.position.count) / 3;
    });
    status.textContent = `${triangles.toLocaleString()} triangles · Geometry and material preview; game lighting may differ.`;
    window.chrome?.webview?.postMessage('ready');
  } catch (error) {
    status.textContent = `Could not preview this model: ${error.message}`;
  }
  window.addEventListener('pagehide', () => {
    observer.disconnect();
    controls.dispose();
    const geometries = new Set(), materials = new Set(), textures = new Set();
    scene.traverse(object => {
      if (object.geometry) geometries.add(object.geometry);
      for (const material of object.material ? (Array.isArray(object.material) ? object.material : [object.material]) : []) {
        materials.add(material);
        for (const value of Object.values(material)) if (value?.isTexture) textures.add(value);
      }
    });
    for (const resource of [...geometries, ...materials, ...textures]) resource.dispose();
    environment.dispose();
    renderer.dispose();
  });

}
startViewer().catch(error => { status.textContent = `The 3D preview could not start: ${error.message}`; });
