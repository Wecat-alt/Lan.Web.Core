import * as THREE from 'three'
import { COLORS } from './constants'
import { DISPLAY_BASE, RADAR_ORIGIN_Z } from './config'

/**
 * 360° 旋转雷达扫描波束
 * - 旋转周期: 3 秒/圈
 * - 半透明扇形 mesh，绕 Y 轴旋转
 */
export class RadarBeam {
  constructor() {
    this.group = new THREE.Group()

    // ── 扫描扇形 ──
    const radius = DISPLAY_BASE * 0.95
    const segments = 64
    const sweepAngle = Math.PI / 6 // 30° 扇形宽度

    const shape = new THREE.Shape()
    shape.moveTo(0, 0)
    for (let i = 0; i <= segments; i++) {
      const a = (i / segments) * sweepAngle - sweepAngle / 2
      shape.lineTo(Math.cos(a) * radius, Math.sin(a) * radius)
    }
    shape.closePath()

    const geom = new THREE.ShapeGeometry(shape)
    const mat = new THREE.MeshBasicMaterial({
      color: COLORS.sweep,
      side: THREE.DoubleSide,
      transparent: true,
      opacity: 0.12,
      depthWrite: false,
    })
    const sweepMesh = new THREE.Mesh(geom, mat)
    sweepMesh.rotation.x = -Math.PI / 2
    sweepMesh.position.y = 0.03
    this.group.add(sweepMesh)

    // ── 扫描线 ──
    const linePts = []
    const lineSegments = 100
    for (let i = 0; i <= lineSegments; i++) {
      const r = (i / lineSegments) * radius
      linePts.push(new THREE.Vector3(r, 0.04, 0))
    }
    const lineGeom = new THREE.BufferGeometry().setFromPoints(linePts)
    const lineMat = new THREE.LineBasicMaterial({
      color: COLORS.sweep,
      transparent: true,
      opacity: 0.6,
      depthTest: true,
    })
    const line = new THREE.Line(lineGeom, lineMat)
    this.group.add(line)

    // ── 距离圈标记点 ──
    const markerGeom = new THREE.SphereGeometry(0.15, 8, 8)
    const markerMat = new THREE.MeshBasicMaterial({ color: COLORS.sweep })
    const markers = [0.25, 0.5, 0.75, 1.0]
    markers.forEach((frac) => {
      const m = new THREE.Mesh(markerGeom, markerMat)
      m.position.set(frac * radius, 0.05, 0)
      this.group.add(m)
    })

    // ── 旋转速度: 2π / 3秒 = 2.094 rad/s ──
    this._rotationSpeed = (2 * Math.PI) / 3

    // 设置到雷达原点
    this.group.position.set(0, 0.01, RADAR_ORIGIN_Z)
  }

  update(dt) {
    this.group.rotation.y += this._rotationSpeed * dt
    if (this.group.rotation.y > Math.PI * 2) {
      this.group.rotation.y -= Math.PI * 2
    }
  }
}
