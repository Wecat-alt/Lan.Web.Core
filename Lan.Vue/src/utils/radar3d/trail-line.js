import * as THREE from 'three'

export class TrailLine {
  constructor(maxLen, hexColor, maxAge = 0) {
    this.maxLen = maxLen
    this.maxAge = maxAge
    this.points = []
    this.hexColor = hexColor
    this._baseColor = new THREE.Color(hexColor)

    const arr = new Float32Array(maxLen * 3)
    const geom = new THREE.BufferGeometry()
    geom.setAttribute('position', new THREE.BufferAttribute(arr, 3))
    geom.setAttribute('color', new THREE.BufferAttribute(new Float32Array(maxLen * 3), 3))
    geom.setDrawRange(0, 0)

    this.line = new THREE.Line(
      geom,
      new THREE.LineBasicMaterial({
        vertexColors: true,
        transparent: true,
        opacity: 0.9,
        depthTest: true,
      })
    )
    this.line.frustumCulled = false
  }

  addPoint(x, y, z, time) {
    this.points.push({ x, y, z, t: time })
    // 按时间裁剪
    if (this.maxAge > 0) {
      const cutoff = time - this.maxAge
      while (this.points.length > 1 && this.points[0].t < cutoff) {
        this.points.shift()
      }
    }
    // 按数量裁剪
    while (this.points.length > this.maxLen) {
      this.points.shift()
    }
    this._flush()
  }

  setMaxLen(n) {
    this.maxLen = n
    const posAttr = this.line.geometry.attributes.position
    const colAttr = this.line.geometry.attributes.color
    posAttr.array = new Float32Array(n * 3)
    colAttr.array = new Float32Array(n * 3)
    posAttr.needsUpdate = true
    colAttr.needsUpdate = true
    while (this.points.length > n) {
      this.points.shift()
    }
    this._flush()
  }

  dispose() {
    this.line.geometry?.dispose()
    this.line.material?.dispose()
  }

  clear() {
    this.points = []
    this._flush()
  }

  get pointCount() {
    return this.points.length
  }

  _flush() {
    const n = this.points.length
    if (!n) {
      this.line.geometry.setDrawRange(0, 0)
      return
    }
    const posArr = this.line.geometry.attributes.position.array
    const colArr = this.line.geometry.attributes.color.array
    const baseColor = this._baseColor

    for (let i = 0; i < n; i++) {
      const t = n > 1 ? i / (n - 1) : 1
      const v = 0.12 + t * 0.88
      const pt = this.points[i]
      posArr[i * 3] = pt.x
      posArr[i * 3 + 1] = pt.y
      posArr[i * 3 + 2] = pt.z
      colArr[i * 3] = baseColor.r * v
      colArr[i * 3 + 1] = baseColor.g * v
      colArr[i * 3 + 2] = baseColor.b * v
    }
    this.line.geometry.attributes.position.needsUpdate = true
    this.line.geometry.attributes.color.needsUpdate = true
    this.line.geometry.setDrawRange(0, n)
  }
}
