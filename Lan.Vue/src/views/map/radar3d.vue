<template>
  <div class="radar3d-page">
    <!-- 顶部工具栏 -->
    <div class="radar3d-toolbar">
      <div class="toolbar-left">
        <el-select
          v-model="selectedRadarId"
          :placeholder="$t('radar.selectRadar') || '选择雷达'"
          clearable
          @change="onRadarChange"
          style="width: 200px"
        >
          <el-option
            v-for="r in radarOptions"
            :key="r.id"
            :label="r.ip"
            :value="r.id"
          />
        </el-select>
        <el-tag v-if="targetCount > 0" type="success" style="margin-left: 12px">
          目标: {{ targetCount }}
        </el-tag>
      </div>

      <div class="toolbar-right">
        <!-- 暂停/播放 -->
        <el-button-group>
          <el-button :type="paused ? 'primary' : 'default'" @click="togglePause" size="small">
            {{ paused ? '▶ 播放' : '⏸ 暂停' }}
          </el-button>
          <el-button @click="handleClear" size="small">清除轨迹</el-button>
        </el-button-group>

        <!-- 视角切换 -->
        <el-button-group style="margin-left: 8px">
          <el-button @click="setCameraView('default')" size="small">默认视角</el-button>
          <el-button @click="setCameraView('top')" size="small">俯视</el-button>
          <el-button @click="setCameraView('front')" size="small">前视</el-button>
        </el-button-group>

        <!-- 设置 -->
        <el-popover placement="bottom" :width="260" trigger="click">
          <template #reference>
            <el-button size="small" style="margin-left: 8px">⚙ 设置</el-button>
          </template>
          <div class="settings-panel">
            <div class="setting-item">
              <label>探测范围: {{ radarRange }}m</label>
              <el-slider v-model="radarRange" :min="100" :max="2000" :step="100"
                @change="onRangeChange" show-input />
            </div>
            <div class="setting-item">
              <label>探测高度: {{ radarHeight }}m</label>
              <el-slider v-model="radarHeight" :min="100" :max="2000" :step="100"
                @change="onHeightChange" show-input />
            </div>
            <div class="setting-item">
              <label>轨迹长度: {{ maxTrail }}</label>
              <el-slider v-model="maxTrail" :min="200" :max="5000" :step="200"
                @change="onTrailChange" />
            </div>
            <div class="setting-item">
              <label>地图透明度: {{ Math.round(mapOpacity * 100) }}%</label>
              <el-slider v-model="mapOpacity" :min="10" :max="100" :step="5"
                @change="onOpacityChange" />
            </div>
          </div>
        </el-popover>
      </div>
    </div>

    <!-- 3D 渲染容器 -->
    <div ref="containerRef" class="radar3d-container"></div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { listRadar } from '@/api/device/radar'
import { initSignalR, setSignalRReceiveEnabled } from '@/utils/signalRUtils'
import { convertSignalRToTargetData } from '@/utils/radar3dConverter'
import { useRadar3D } from '@/composables/useRadar3D'
import { DEFAULT_MAX_TRAIL, DEFAULT_MAP_OPACITY, CAMERA_PRESETS } from '@/utils/radar3d/constants'

// ═══════════════ SignalR Config ═══════════════
const longLinkApi = window.__APP_CONFIG__?.VITE_SIGNALR_URL || ''
const longLinkMsg = 'ReceiveTargetData'

// ═══════════════ Radar Options ═══════════════
const radarOptions = ref([])
const selectedRadarId = ref(null)
const selectedRadar = ref(null)

// ═══════════════ 3D Scene ═══════════════
const containerRef = ref(null)
const {
  targetCount,
  init,
  setTargets,
  updateConfig,
  setMapOpacity,
  clearTrails,
  setPaused,
  setMaxTrail,
  getContext,
} = useRadar3D(containerRef)

// ═══════════════ Control State ═══════════════
const paused = ref(false)
const radarRange = ref(500)
const radarHeight = ref(500)
const maxTrail = ref(DEFAULT_MAX_TRAIL)
const mapOpacity = ref(Math.round(DEFAULT_MAP_OPACITY * 100))

// ═══════════════ SignalR (same pattern as calibration/index) ═══════════════
let connection = ref(null)
let unsubscribeSignalR = null

function handleRadarData(res) {
  if (!selectedRadar.value) {
    // 未选择雷达，跳过
    return
  }

  let serverData
  try {
    serverData = JSON.parse(res)
  } catch {
    return
  }

  const radarLat = selectedRadar.value.lat
  const radarLng = selectedRadar.value.lng

  const target = convertSignalRToTargetData(serverData, radarLat, radarLng)
  // 只要 targetId 有效就送入 3D 场景，范围检测由 DroneTarget.setPosition 处理
  if (target.targetId) {
    setTargets([target])
  }
}

function handleTrackTarget(targetId) {
  console.log('[Radar3D] Track target:', targetId)
}

function initSignalRConnection(longLinkApi, acceptMsg, longLinkSendMsg) {
  initSignalR({
    api: longLinkApi,
    acceptMsg,
    sendMsg: longLinkSendMsg,
    onAcceptMessage: handleRadarData,
    onTrackTargetData: handleTrackTarget,
  }).then(({ connection: sharedConnection, unsubscribe }) => {
    connection.value = sharedConnection
    unsubscribeSignalR = unsubscribe
    setSignalRReceiveEnabled(true)
  })
}

function onRadarChange(id) {
  const radar = radarOptions.value.find((r) => r.id === id)
  selectedRadar.value = radar || null

  if (radar) {
    // 同步雷达配置到3D场景
    const range = radar.range || 500
    const height = radar.range || 500
    radarRange.value = range
    radarHeight.value = height
    updateConfig({ range, height })
  }
}

// ═══════════════ Controls ═══════════════

function togglePause() {
  paused.value = !paused.value
  setPaused(paused.value)
}

function handleClear() {
  clearTrails()
}

function onRangeChange(val) {
  updateConfig({ range: val })
}

function onHeightChange(val) {
  updateConfig({ height: val })
}

function onTrailChange(val) {
  setMaxTrail(val)
}

function onOpacityChange(val) {
  mapOpacity.value = val
  setMapOpacity(val / 100)
}

function setCameraView(preset) {
  const cfg = CAMERA_PRESETS[preset]
  if (!cfg) return
  const ctx = getContext()
  if (!ctx) return
  const [px, py, pz] = cfg.pos
  const [tx, ty, tz] = cfg.target
  ctx.camera.position.set(px, py, pz)
  ctx.controls.target.set(tx, ty, tz)
  ctx.controls.update()
}

// ═══════════════ Keyboard Shortcuts ═══════════════
function onKeyDown(e) {
  if (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT') return
  switch (e.key.toLowerCase()) {
    case ' ':
      e.preventDefault()
      togglePause()
      break
    case 'r':
      setCameraView('default')
      break
    case 't':
      setCameraView('top')
      break
    case 'f':
      setCameraView('front')
      break
    case 'c':
      handleClear()
      break
  }
}

// ═══════════════ Init ═══════════════
// ═══════════════ Init ═══════════════
onMounted(async () => {
  await nextTick()
  init() // 初始化 Three.js 场景（先显示空场景）

  // 获取雷达列表，加载完成后启动 SignalR
  listRadar().then((response) => {
    const list = response.data.data
    if (!list || list.length === 0) {
      console.warn('[Radar3D] 没有雷达数据')
      return
    }

    radarOptions.value = list.map((r) => ({
      id: r.id,
      ip: r.ip,
      lat: parseFloat(r.latitude) || 0,
      lng: parseFloat(r.longitude) || 0,
      range: r.defenceRadius || 500,
      height: r.defenceRadius || 500,
      northDeviationAngle: parseFloat(r.northDeviationAngle) || 0,
    }))

    // 默认选中第一个雷达
    selectedRadarId.value = radarOptions.value[0].id
    onRadarChange(selectedRadarId.value)

    console.log('[Radar3D] 雷达列表加载完成:', radarOptions.value.length, '个')
  }).catch((err) => {
    console.error('[Radar3D] 获取雷达列表失败:', err)
  })

  initSignalRConnection(longLinkApi, longLinkMsg, longLinkMsg)
  window.addEventListener('keydown', onKeyDown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKeyDown)
  if (typeof unsubscribeSignalR === 'function') {
    unsubscribeSignalR()
    unsubscribeSignalR = null
  }
})
</script>

<style scoped>
.radar3d-page {
  position: relative;
  width: 100%;
  height: 100%;
  overflow: hidden;
  background: #05080d;
}

.radar3d-toolbar {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  z-index: 10;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 16px;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.radar3d-container {
  width: 100%;
  height: 100%;
}

.settings-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.setting-item label {
  display: block;
  font-size: 13px;
  color: #aabbcc;
  margin-bottom: 4px;
}
</style>
