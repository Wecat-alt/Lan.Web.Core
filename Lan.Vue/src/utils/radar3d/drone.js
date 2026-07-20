import * as THREE from 'three'
import { TrailLine } from './trail-line'
import { toDisplay, RADAR_ORIGIN_Z, radarConfig } from './config'

export class DroneTarget {
  constructor(id, name, hexColor, maxTrailLength, scene) {
    this.id = id
    this.name = name
    this.hexColor = hexColor
    this.time = Math.random() * 100
    this.pos = { x: 0, y: 0, z: 0 }
    this._hexString = '#' + new THREE.Color(hexColor).getHexString()
    this._lastUpdateTime = Date.now() / 1000

    const color = new THREE.Color(hexColor)

    // Main sphere
    this.sphere = new THREE.Mesh(
      new THREE.SphereGeometry(0.45, 24, 24),
      new THREE.MeshStandardMaterial({
        color,
        emissive: color,
        emissiveIntensity: 0.5,
        roughness: 0.25,
      })
    )
    this.sphere.castShadow = true
    scene.add(this.sphere)

    // Glow halo
    this.sphere.add(
      new THREE.Mesh(
        new THREE.SphereGeometry(0.7, 16, 16),
        new THREE.MeshBasicMaterial({
          color,
          transparent: true,
          opacity: 0.18,
          depthWrite: false,
        })
      )
    )

    // Label (Canvas-based, shows name + height)
    this._labelCanvas = document.createElement('canvas')
    this._labelCanvas.width = 384; this._labelCanvas.height = 160
    this._labelCtx = this._labelCanvas.getContext('2d')
    this._labelTex = new THREE.CanvasTexture(this._labelCanvas)
    this._labelTex.minFilter = THREE.LinearFilter
    this.label = new THREE.Sprite(
      new THREE.SpriteMaterial({ map: this._labelTex, depthTest: false, depthWrite: false })
    )
    this.label.scale.set(3.6, 1.5, 1)
    this._drawLabel(name, 0)
    scene.add(this.label)

    // Trail (3秒历史轨迹)
    this.trail = new TrailLine(maxTrailLength, hexColor, 3)
    scene.add(this.trail.line)
  }

  _drawLabel(name, height) {
    const ctx = this._labelCtx
    const c = this._labelCanvas
    ctx.clearRect(0, 0, c.width, c.height)
    ctx.fillStyle = this._hexString
    ctx.font = 'bold 44px "Segoe UI", sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.fillText(name, 192, 40)
    ctx.font = 'bold 52px "Consolas", monospace'
    ctx.fillText(`${height.toFixed(0)}m`, 192, 110)
    this._labelTex.needsUpdate = true
  }

  /**
   * 直接设置真实世界坐标 (替代 TrajectoryFn 模拟)
   * @param {number} realX - 东向米
   * @param {number} realY - 北向米
   * @param {number} realZ - 高度米
   */
  setPosition(realX, realY, realZ) {
    this.pos = { x: realX, y: realY, z: realZ }
    this._lastUpdateTime = Date.now() / 1000

    // 范围检测
    const dist = Math.sqrt(realX * realX + realY * realY)
    const inRange = dist <= radarConfig.range && realZ <= radarConfig.height

    this.sphere.visible = inRange
    this.label.visible = inRange
    if (!inRange) return

    // 更新高度标签
    this._drawLabel(this.name, realZ)

    // 坐标转换: 用户坐标 → Three.js
    const sx = toDisplay(realX)   // 东 → X
    const sy = toDisplay(realZ)   // 高 → Y
    const sz = toDisplay(realY) + RADAR_ORIGIN_Z  // 北 → Z
    this.sphere.position.set(sx, sy, sz)
    this.label.position.set(sx, sy + 1.4, sz)
    this.trail.addPoint(sx, sy, sz, this._lastUpdateTime)
  }

  setTrailMaxLen(n) {
    this.trail.setMaxLen(n)
  }

  clearTrail() {
    this.trail.clear()
  }

  get trailPointCount() {
    return this.trail.pointCount
  }

  get lastUpdateTime() {
    return this._lastUpdateTime
  }

  dispose() {
    this.sphere.geometry?.dispose()
    this.sphere.material?.dispose()
    const glow = this.sphere.children[0]
    glow?.geometry?.dispose()
    glow?.material?.dispose()
    this.label.material?.map?.dispose()
    this.label.material?.dispose()
    this._labelTex?.dispose()
    this.trail.dispose()
  }
}
