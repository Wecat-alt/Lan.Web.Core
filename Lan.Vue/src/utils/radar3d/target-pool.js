import { DroneTarget } from './drone'
import { DRONE_COLORS, DEFAULT_MAX_TRAIL } from './constants'

/**
 * 动态目标管理器
 * 替代原有的3架固定模拟无人机，支持实时数据驱动的目标生命周期管理
 */
export class TargetPool {
  constructor(scene) {
    /** @type {Map<string, DroneTarget>} */
    this._targets = new Map()
    this._scene = scene
    this._maxTrailLength = DEFAULT_MAX_TRAIL
    this._cleanupTimeout = 5 // 5秒未更新的目标自动清理
    this._colorIndex = 0

    // 定时清理
    this._cleanupTimer = setInterval(() => this._cleanup(), 1000)
  }

  /**
   * 批量应用目标更新
   * @param {Array<{targetId: string, x: number, y: number, z: number, targetType?: number, speed?: number}>} targets
   */
  applyFrame(targets) {
    if (!targets || !targets.length) return

    for (const t of targets) {
      const id = String(t.targetId)
      let drone = this._targets.get(id)

      if (!drone) {
        // 新目标: 创建 DroneTarget
        const color = DRONE_COLORS[this._colorIndex % DRONE_COLORS.length]
        this._colorIndex++
        const name = t.targetType === 2 ? `车 ${id}` : `人 ${id}`
        drone = new DroneTarget(id, name, color.hex, this._maxTrailLength, this._scene)
        this._targets.set(id, drone)
      }

      // 更新位置
      drone.setPosition(t.x, t.y, t.z || 0)
    }
  }

  /**
   * 清理超时目标
   */
  _cleanup() {
    const now = Date.now() / 1000
    const toRemove = []

    for (const [id, drone] of this._targets) {
      if (now - drone.lastUpdateTime > this._cleanupTimeout) {
        toRemove.push(id)
      }
    }

    for (const id of toRemove) {
      const drone = this._targets.get(id)
      if (drone) {
        this._scene.remove(drone.sphere)
        this._scene.remove(drone.label)
        this._scene.remove(drone.trail.line)
        drone.dispose()
        this._targets.delete(id)
      }
    }
  }

  clearAll() {
    for (const [id, drone] of this._targets) {
      this._scene.remove(drone.sphere)
      this._scene.remove(drone.label)
      this._scene.remove(drone.trail.line)
      drone.dispose()
    }
    this._targets.clear()
  }

  setMaxTrailLength(n) {
    this._maxTrailLength = n
    for (const drone of this._targets.values()) {
      drone.setTrailMaxLen(n)
    }
  }

  get targetCount() {
    return this._targets.size
  }

  getTargetIds() {
    return Array.from(this._targets.keys())
  }

  dispose() {
    clearInterval(this._cleanupTimer)
    this.clearAll()
  }
}
