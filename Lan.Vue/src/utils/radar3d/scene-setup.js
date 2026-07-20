import * as THREE from 'three'
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js'

export function setupScene(container) {
  // ── Renderer ──
  const renderer = new THREE.WebGLRenderer({ antialias: true })
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
  renderer.shadowMap.enabled = true
  renderer.shadowMap.type = THREE.PCFSoftShadowMap
  container.appendChild(renderer.domElement)

  // ── Scene ──
  const scene = new THREE.Scene()
  scene.background = new THREE.Color(0x05080d)
  scene.fog = new THREE.Fog(0x05080d, 40, 120)

  // ── Camera ──
  const camera = new THREE.PerspectiveCamera(
    50, 1, 0.5, 500
  )
  camera.position.set(0, 18, -45)
  camera.lookAt(0, 0, 0)

  // ── OrbitControls ──
  const controls = new OrbitControls(camera, renderer.domElement)
  controls.target.set(0, 0, 0)
  controls.enableDamping = true
  controls.dampingFactor = 0.08
  controls.minDistance = 3
  controls.maxDistance = 80
  controls.maxPolarAngle = Math.PI * 0.48
  controls.update()

  // ── Lights ──
  scene.add(new THREE.AmbientLight(0x404060, 0.8))

  const sunLight = new THREE.DirectionalLight(0xffeedd, 2.5)
  sunLight.position.set(20, 35, 15)
  sunLight.castShadow = true
  sunLight.shadow.mapSize.set(1024, 1024)
  sunLight.shadow.camera.near = 0.5
  sunLight.shadow.camera.far = 120
  sunLight.shadow.camera.left = -40
  sunLight.shadow.camera.right = 40
  sunLight.shadow.camera.top = 40
  sunLight.shadow.camera.bottom = -5
  scene.add(sunLight)

  const fillLight = new THREE.DirectionalLight(0x4488cc, 0.6)
  fillLight.position.set(-10, 5, -10)
  scene.add(fillLight)

  // ── Ground material ──
  const groundMat = new THREE.MeshStandardMaterial({
    map: null,
    roughness: 0.85,
    metalness: 0.05,
    transparent: true,
    opacity: 0.90,
  })

  return { scene, camera, renderer, controls, groundMat }
}

export function resizeRenderer(ctx, width, height) {
  ctx.camera.aspect = width / height
  ctx.camera.updateProjectionMatrix()
  ctx.renderer.setSize(width, height)
}
