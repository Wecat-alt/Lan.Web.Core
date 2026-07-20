import * as THREE from 'three'
import { COLORS } from './constants'
import { radarConfig, computeRangeRings, toDisplay, DISPLAY_BASE, RADAR_ORIGIN_Z } from './config'

// ═══════════════════════ Dynamic Objects Container ═══════════════════════
const _dynamic = []

function _add(obj) {
  _dynamic.push(obj)
}

function _clearDynamic(scene, groundMat) {
  for (const obj of _dynamic) {
    scene.remove(obj)
    if (obj instanceof THREE.Line || obj instanceof THREE.Points || obj instanceof THREE.Mesh) {
      obj.geometry?.dispose()
      if (Array.isArray(obj.material)) {
        obj.material.forEach((m) => { if (m !== groundMat) m.dispose() })
      } else {
        if (obj.material !== groundMat) obj.material?.dispose()
      }
    }
  }
  _dynamic.length = 0
}

export function getDynamicCount() {
  return _dynamic.length
}

// ═══════════════════════ Label Sprite ═══════════════════════
export function createLabelSprite(text, colorHex) {
  const canvas = document.createElement('canvas')
  canvas.width = 128
  canvas.height = 64
  const ctx = canvas.getContext('2d')
  ctx.fillStyle = '#' + new THREE.Color(colorHex).getHexString()
  ctx.font = 'bold 40px "Segoe UI", sans-serif'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'middle'
  ctx.fillText(text, 64, 32)
  const tex = new THREE.CanvasTexture(canvas)
  tex.minFilter = THREE.LinearFilter
  return new THREE.Sprite(
    new THREE.SpriteMaterial({ map: tex, depthTest: false, depthWrite: false })
  )
}

// ═══════════════════════ Axis ═══════════════════════
function addAxis(scene, p1, p2, color) {
  scene.add(
    new THREE.Line(
      new THREE.BufferGeometry().setFromPoints([p1, p2]),
      new THREE.LineBasicMaterial({ color })
    )
  )
  const cone = new THREE.Mesh(
    new THREE.ConeGeometry(0.3, 1.2, 8),
    new THREE.MeshStandardMaterial({ color, emissive: color, emissiveIntensity: 0.5, roughness: 0.3 })
  )
  cone.position.copy(p2)
  cone.setRotationFromQuaternion(
    new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0), p2.clone().sub(p1).normalize())
  )
  cone.castShadow = true
  scene.add(cone)
}

// ═══════════════════════ Arc Ring ═══════════════════════
function createCircleRing(radius, color, opacity, y = 0.01) {
  const pts = []
  const segments = 128
  const oz = RADAR_ORIGIN_Z
  for (let i = 0; i <= segments; i++) {
    const a = (i / segments) * Math.PI * 2
    pts.push(new THREE.Vector3(Math.cos(a) * radius, y, Math.sin(a) * radius + oz))
  }
  return new THREE.Line(
    new THREE.BufferGeometry().setFromPoints(pts),
    new THREE.LineBasicMaterial({ color, transparent: true, opacity, depthTest: true })
  )
}

// ═══════════════════════ Default Texture ═══════════════════════
export function createDefaultTexture() {
  const size = 512, c = size / 2
  const canvas = document.createElement('canvas')
  canvas.width = size; canvas.height = size
  const ctx = canvas.getContext('2d')
  ctx.fillStyle = '#2d3a2a'; ctx.fillRect(0, 0, size, size)
  let seed = 42
  const rng = () => { seed = (seed * 16807) % 2147483647; return (seed - 1) / 2147483646 }
  for (let i = 0; i < 100; i++) {
    const sh = 20 + rng() * 35
    ctx.fillStyle = `rgb(${(35 + sh) | 0},${(55 + sh) | 0},${(30 + sh * 0.6) | 0})`
    ctx.beginPath()
    ctx.ellipse(rng() * size, rng() * size, 30 + rng() * 180, 30 + rng() * 180, rng() * Math.PI, 0, Math.PI * 2)
    ctx.fill()
  }
  ctx.strokeStyle = 'rgba(255,255,255,0.06)'; ctx.lineWidth = 0.5
  for (let i = 0; i <= 50; i++) {
    const p = (i * size) / 50
    ctx.beginPath(); ctx.moveTo(p, 0); ctx.lineTo(p, size); ctx.stroke()
    ctx.beginPath(); ctx.moveTo(0, p); ctx.lineTo(size, p); ctx.stroke()
  }
  ctx.fillStyle = 'rgba(80,140,255,0.04)'
  ctx.beginPath(); ctx.arc(c, c, size * 0.48, 0, Math.PI * 2); ctx.fill()
  ctx.fillStyle = 'rgba(255,255,255,0.25)'; ctx.font = '16px sans-serif'; ctx.textAlign = 'center'
  ctx.fillText('360° 全方位探测', c, c + size * 0.46)
  const tex = new THREE.CanvasTexture(canvas)
  tex.wrapS = tex.wrapT = THREE.ClampToEdgeWrapping
  tex.colorSpace = THREE.SRGBColorSpace
  tex.minFilter = THREE.LinearMipmapLinearFilter
  tex.magFilter = THREE.LinearFilter
  tex.generateMipmaps = true
  return tex
}

// ═══════════════════════ Static Elements (no range dependency) ═══════════════════════
export function addStaticElements(ctx) {
  const { scene } = ctx

  // ── Z 高度轴 ──
  const oz = RADAR_ORIGIN_Z
  addAxis(scene, new THREE.Vector3(0, 0.02, oz), new THREE.Vector3(0, 22, oz), COLORS.axes.y)

  const s = createLabelSprite('Z 高', COLORS.axes.y)
  s.position.set(0, 23.5, oz); s.scale.set(2.5, 1.25, 1)
  scene.add(s)

  // ── Radar Position Sphere ──
  const radarSphere = new THREE.Mesh(
    new THREE.SphereGeometry(0.4, 32, 32),
    new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0xffffff, emissiveIntensity: 0.6, roughness: 0.2 })
  )
  radarSphere.position.set(0, 0, oz)
  scene.add(radarSphere)

  // ── Stars ──
  const starsGeom = new THREE.BufferGeometry()
  const starsPos = new Float32Array(400 * 3)
  for (let i = 0; i < 400; i++) {
    const phi = Math.random() * Math.PI * 2
    const theta = Math.random() * Math.PI * 0.45
    const r = 40 + Math.random() * 25
    starsPos[i * 3] = r * Math.cos(theta) * Math.cos(phi)
    starsPos[i * 3 + 1] = r * Math.sin(theta) + 8
    starsPos[i * 3 + 2] = r * Math.cos(theta) * Math.sin(phi)
  }
  starsGeom.setAttribute('position', new THREE.BufferAttribute(starsPos, 3))
  scene.add(new THREE.Points(starsGeom,
    new THREE.PointsMaterial({ color: COLORS.stars, size: 0.25, sizeAttenuation: true, transparent: true, opacity: 0.7, depthWrite: false })
  ))
}

// ═══════════════════════ Dynamic Elements (range-dependent) ═══════════════════════
export function rebuildDynamicElements(ctx) {
  const { scene, groundMat } = ctx
  const rangeRings = computeRangeRings(radarConfig.range)
  const oz = RADAR_ORIGIN_Z

  _clearDynamic(scene, groundMat)

  // ── Ground Plane ──
  const size = DISPLAY_BASE * 2
  const groundPlane = new THREE.Mesh(new THREE.PlaneGeometry(size, size), groundMat)
  groundPlane.rotation.x = -Math.PI / 2
  groundPlane.position.y = -0.02
  groundPlane.receiveShadow = true
  scene.add(groundPlane)
  _add(groundPlane)

  // ── Grid ──
  const divisions = Math.max(20, Math.round(DISPLAY_BASE))
  const gridHelper = new THREE.GridHelper(size, divisions, 0x6688aa, 0x446688)
  gridHelper.position.y = 0.0
  const gridMat = gridHelper.material
  gridMat.transparent = true; gridMat.opacity = 0.25; gridMat.depthTest = true
  scene.add(gridHelper)
  _add(gridHelper)

  // ── Range Rings ──
  rangeRings.forEach((realR, i) => {
    const r = toDisplay(realR)
    const ring = createCircleRing(r, COLORS.rings, 0.15 + i * 0.06)
    scene.add(ring); _add(ring)
  })
}

// ═══════════════════════ Map Image ═══════════════════════
export function applyMapTexture(groundMat, imageOrUrl) {
  if (typeof imageOrUrl === 'string') {
    const img = new Image()
    img.crossOrigin = 'anonymous'
    img.onload = () => applyImageTexture(groundMat, img)
    img.src = imageOrUrl
  } else {
    applyImageTexture(groundMat, imageOrUrl)
  }
}

function applyImageTexture(groundMat, img) {
  const tex = new THREE.CanvasTexture(img)
  tex.wrapS = tex.wrapT = THREE.ClampToEdgeWrapping
  tex.colorSpace = THREE.SRGBColorSpace
  tex.minFilter = THREE.LinearMipmapLinearFilter
  tex.magFilter = THREE.LinearFilter
  tex.generateMipmaps = true
  tex.offset.set(0, 0); tex.repeat.set(1, 1)
  groundMat.map?.dispose?.()
  groundMat.map = tex
  groundMat.needsUpdate = true
}

export function setGroundOpacity(groundMat, opacity) {
  groundMat.opacity = opacity
  groundMat.needsUpdate = true
}
