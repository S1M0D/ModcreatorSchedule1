import { writeFile } from 'node:fs/promises';
import * as THREE from 'three';
import { GLTFExporter } from 'three/addons/exporters/GLTFExporter.js';

globalThis.FileReader = class {
  readAsArrayBuffer(blob) {
    blob.arrayBuffer().then(value => { this.result = value; this.onloadend?.(); });
  }
};

const scene = new THREE.Group();
scene.add(new THREE.Mesh(new THREE.BoxGeometry(1, 1, 1), new THREE.MeshStandardMaterial({ color: '#d0d0d0' })));
const second = new THREE.Group();
second.position.set(1.5, 0.25, -0.5);
second.rotation.y = Math.PI / 3;
second.add(new THREE.Mesh(new THREE.SphereGeometry(0.5, 16, 12), new THREE.MeshStandardMaterial({ color: '#ef794e' })));
scene.add(second);
const torus = new THREE.Mesh(new THREE.TorusGeometry(0.38, 0.12, 16, 48),
  new THREE.MeshStandardMaterial({ color: '#74a9d6', metalness: 0.6, roughness: 0.25 }));
torus.position.set(-1.5, 0.5, 0);
scene.add(torus);
const hidden = new THREE.Mesh(new THREE.BoxGeometry(2, 2, 2), new THREE.MeshStandardMaterial({ color: '#ff0000' }));
hidden.visible = false;
scene.add(hidden);
const bytes = await new GLTFExporter().parseAsync(scene, { binary: true, trs: true, onlyVisible: true });
await writeFile(new URL('../Schedule1ModdingTool.Tests/Fixtures/scene-editor-materials.glb', import.meta.url), Buffer.from(bytes));
console.log(`GLB bytes: ${bytes.byteLength}`);
