import { ref, onBeforeUnmount } from 'vue'
import * as THREE from 'three'
import { setupScene, resizeRenderer } from '@/utils/radar3d/scene-setup'
import {
  addStaticElements,
  rebuildDynamicElements,
  createDefaultTexture,
  applyMapTexture,
  setGroundOpacity,
} from '@/utils/radar3d/scene-static'
import { RadarBeam } from '@/utils/radar3d/radar-beam'
import { TargetPool } from '@/utils/radar3d/target-pool'
import { radarConfig } from '@/utils/radar3d/config'
import { DEFAULT_MAX_TRAIL, DEFAULT_SIM_SPEED } from '@/utils/radar3d/constants'

/**
 * Vue 组合式函数: 管理 Three.js 3D 雷达场景的完整生命周期
 *
 * @param {import('vue').Ref<HTMLElement|null>} containerRef - 渲染容器
 * @returns {{ targetPool, targetCount, updateConfig, setTargets, clearTrails, setPaused, setMaxTrail }}
 */
export function useRadar3D(containerRef) {
  /** @type {import('vue').Ref<number>} */
  const targetCount = ref(0)
  const isPaused = ref(false)
  const simSpeed = ref(DEFAULT_SIM_SPEED)

  let ctx = null
  let targetPool = null
  let radarBeam = null
  let animationId = 0
  let clock = null
  let _disposed = false

  function init() {
    const container = containerRef.value
    if (!container) return

    // 初始化场景
    ctx = setupScene(container)
    const { scene, groundMat, renderer, camera, controls } = ctx

    // 设置画布尺寸
    const resize = () => {
      const w = container.clientWidth
      const h = container.clientHeight
      if (w > 0 && h > 0) {
        resizeRenderer(ctx, w, h)
      }
    }
    resize()
    window.addEventListener('resize', resize)

    // 默认纹理
    const defaultTex = createDefaultTexture()
    groundMat.map = defaultTex
    groundMat.needsUpdate = true

    // 静态元素
    addStaticElements(ctx)
    rebuildDynamicElements(ctx)

    // 雷达波束
    radarBeam = new RadarBeam()
    scene.add(radarBeam.group)

    // 目标管理器
    targetPool = new TargetPool(scene)

    // 动画循环
    clock = new THREE.Clock()
    function animate() {
      if (_disposed) return
      animationId = requestAnimationFrame(animate)

      const rawDt = Math.min(clock.getDelta(), 0.1)
      const dt = isPaused.value ? 0 : rawDt * simSpeed.value

      controls.update()
      radarBeam.update(rawDt)

      // 目标无需 dt 驱动（位置由 setTargets 直接设置）
      // 但 TrailLine 的 time-based 裁剪通过 targetPool.applyFrame 中的 setPosition 触发

      renderer.render(scene, camera)
      targetCount.value = targetPool.targetCount
    }
    animate()
  }

  /**
   * 批量更新目标位置
   * @param {Array} targets - TargetData 数组
   */
  function setTargets(targets) {
    targetPool?.applyFrame(targets)
  }

  /**
   * 更新雷达配置（探测范围）
   * @param {{range?: number, height?: number}} config
   */
  function updateConfig({ range, height } = {}) {
    if (range != null) radarConfig.range = range
    if (height != null) radarConfig.height = height
    if (ctx) {
      rebuildDynamicElements(ctx)
    }
  }

  /**
   * 设置地图纹理
   * @param {string|HTMLImageElement} src - 图片URL或Image元素
   */
  function setMapTexture(src) {
    if (ctx) {
      applyMapTexture(ctx.groundMat, src)
    }
  }

  /**
   * 设置地图不透明度
   * @param {number} opacity - 0-1
   */
  function setMapOpacity(opacity) {
    if (ctx) {
      setGroundOpacity(ctx.groundMat, opacity)
    }
  }

  /**
   * 清除所有轨迹
   */
  function clearTrails() {
    targetPool?.clearAll()
  }

  /**
   * 暂停/恢复
   * @param {boolean} val
   */
  function setPaused(val) {
    isPaused.value = val
  }

  /**
   * 设置尾迹最大长度
   * @param {number} n
   */
  function setMaxTrail(n) {
    targetPool?.setMaxTrailLength(n)
  }

  /**
   * 设置仿真速度
   * @param {number} speed
   */
  function setSimSpeed(speed) {
    simSpeed.value = speed
  }

  // 页面销毁时清理
  onBeforeUnmount(() => {
    _disposed = true
    if (animationId) cancelAnimationFrame(animationId)
    targetPool?.dispose()
    if (ctx) {
      ctx.renderer.dispose()
      ctx.controls.dispose()
    }
  })

  return {
    targetCount,
    init,
    setTargets,
    updateConfig,
    setMapTexture,
    setMapOpacity,
    clearTrails,
    setPaused,
    setMaxTrail,
    setSimSpeed,
    getContext: () => ctx,
  }
}
