<template>
  <div class="live-preview-page">
    <div class="toolbar">
      <el-button-group>
        <el-button :type="layout === 4 ? 'primary' : 'default'" @click="setLayout(4)"
          >4宫格</el-button
        >
        <el-button :type="layout === 6 ? 'primary' : 'default'" @click="setLayout(6)"
          >6宫格</el-button
        >
        <el-button :type="layout === 9 ? 'primary' : 'default'" @click="setLayout(9)"
          >9宫格</el-button
        >
      </el-button-group>
      <el-button :loading="loadingCameras" @click="fetchCameraList">刷新相机</el-button>
      <el-switch v-model="autoSwitch" active-text="自动切换" />
    </div>

    <div class="content-wrap">
      <aside class="camera-list-panel">
        <div class="camera-list-title">相机列表</div>
        <div class="camera-list-body">
          <div v-if="!cameras.length" class="camera-list-empty">暂无相机</div>
          <div v-for="(camera, idx) in cameras" :key="camera.id || idx" class="camera-list-item">
            <span class="camera-index"> {{ idx + 1 }}.</span>
            <span class="camera-ip" :title="camera.ip || '-'">{{ camera.ip || '-' }}</span>
            <el-checkbox v-model="camera.checked" @change="handleCameraCheckedChange" />
          </div>
        </div>
      </aside>

      <div class="grid-wrap">
        <div
          class="grid"
          :class="[`layout-${layout}`, { 'is-fullscreen': isFullscreenMode }]"
          :style="gridStyle"
        >
          <div
            v-for="(slot, idx) in slots"
            :key="idx"
            class="grid-cell"
            :class="{
              'is-hidden': isFullscreenMode && fullscreenSlotIndex !== idx,
              'is-fullscreen-cell': isFullscreenMode && fullscreenSlotIndex === idx,
            }"
            :ref="(el) => setSlotRef(el, idx)"
          >
            <div class="cell-header">
              <span class="cell-title">{{ getSlotHeaderTitle(slot) }}</span>
              <div class="cell-header-right">
                <span class="cell-status">{{ getSlotStatus(slot, idx) }}</span>
                <button
                  class="cell-fullscreen-btn"
                  :disabled="fullscreenSwitching"
                  type="button"
                  :title="fullscreenSlotIndex === idx ? '退出全屏' : '全屏显示'"
                  @click="toggleFullscreen(idx)"
                >
                  {{ fullscreenSlotIndex === idx ? '🗗' : '⛶' }}
                </button>
              </div>
            </div>
            <div class="cell-body" :class="{ 'cell-body-embed': slot.type === 'embedded' }">
              <template v-if="slot.type === 'embedded'">
                <div class="embedded-leaflet-map" :ref="setMapContainerRef" />
              </template>
              <template v-else>
                <div class="line">IP: {{ slot.ip || '-' }}</div>
                <div class="line">用户: {{ slot.username || '-' }}</div>
                <div class="line">RTSP: {{ slot.rtsp || '-' }}</div>
              </template>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { listCameraPreview } from '@/api/device/camera'
import { listRadar } from '@/api/device/radar'
import {
  isDocumentActuallyForeground,
  usePreviewVisibility,
} from '@/composables/usePreviewVisibility'
import {
  sendCreateWinMessage,
  sendDeleteWinMessage,
  sendDeleteWinMessageSync,
  sendUpdateWinPosMessage,
} from '@/utils/localPlayerSocket'
import { createRadarAlertSwitcher, ints, unregisterSectorMarker } from '@/utils/mapUtils'
import { initSignalR, setSignalRReceiveEnabled } from '@/utils/signalRUtils'
import { TrackManager } from '@/utils/TrackManager'

import { ElMessage } from 'element-plus'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import {
  computed,
  getCurrentInstance,
  nextTick,
  onBeforeUnmount,
  onMounted,
  reactive,
  ref,
  watch,
} from 'vue'

const { proxy } = getCurrentInstance()

defineOptions({
  name: 'LivePreviewPage',
})

const layout = ref(4)
const autoSwitch = ref(false)
const autoTimer = ref(null)
const loadingCameras = ref(false)
const fullscreenSlotIndex = ref(null)
const fullscreenSwitching = ref(false)
const hasHandledPageExit = ref(false)

const cameras = ref([])
const mapContainerRef = ref(null)
const mapInstance = ref(null)
const trackManager = ref(null)
const trackedTargetId = ref(null)
const radarOptions = ref([])
const sectors = ref([])
const connection = ref(null)
let unsubscribeSignalR = null
const defaultRadarIconUrl = '/status/radar_lan.png'
const alertRadarIconUrl = '/status/radar_red.png'
const radarAlertSwitcher = createRadarAlertSwitcher({
  sectors,
  defaultIconUrl: defaultRadarIconUrl,
  alertIconUrl: alertRadarIconUrl,
  timeout: 3000,
})

const longLinkApi = window.__APP_CONFIG__.VITE_SIGNALR_URL
const longLinkMsg = 'ReceiveTargetData'
const longLinkSendMsg = 'ReceiveTargetData'

const slotRefs = ref([])
const playingMap = reactive({})
const activeWinMap = reactive({})
const activePreviewSignatureMap = reactive({})

const cols = computed(() => {
  if (layout.value === 4) return 2
  return 3
})

const checkedCameras = computed(() =>
  cameras.value
    .map((camera, idx) => ({ ...camera, originIndex: idx }))
    .filter((camera) => camera.checked !== false),
)

const slots = computed(() => {
  const count = layout.value
  return Array.from({ length: count }, (_, idx) => {
    if (idx === 0) {
      return {
        type: 'embedded',
        label: '1',
        title: 'GIS地图',
      }
    }

    const camera = checkedCameras.value[idx - 1] || {}
    return {
      ...camera,
      type: 'camera',
      label: String(idx + 1),
      sourceIndex: camera.originIndex ?? idx - 1,
    }
  })
})

const gridStyle = computed(() => ({
  gridTemplateColumns: `repeat(${cols.value}, minmax(0, 1fr))`,
}))

const isFullscreenMode = computed(() => typeof fullscreenSlotIndex.value === 'number')

function isCameraSlot(slot = {}) {
  return slot.type === 'camera'
}

function getSlotHeaderTitle(slot = {}) {
  if (slot.type === 'embedded') {
    return `${slot.label || ''} | GIS地图`.trim()
  }

  const zoneText = slot.zoneName || '-'
  const ipText = slot.ip || '-'
  return `${slot.label || ''} | ${zoneText} ip：${ipText}`.trim()
}

function getSlotStatus(slot = {}, idx) {
  if (slot.type === 'embedded') {
    return '地图显示中'
  }
  return playingMap[idx] ? '自动预览中' : '未配置RTSP'
}

function initEmbeddedLeafletMap() {
  if (mapInstance.value || !mapContainerRef.value) {
    return
  }

  const runtimeConfig = window.__APP_CONFIG__ || {}
  const tileUrl = runtimeConfig.VITE_MAP_TILE_MAP_URL || '/maptile_gaode/{z}/{x}/{y}.jpg'

  mapInstance.value = L.map(mapContainerRef.value, {
    center: [28.112612, 112.874397],
    zoom: 14,
    attributionControl: false,
  })

  L.tileLayer(tileUrl).addTo(mapInstance.value)

  if (!mapInstance.value.getPane('bottomMarkers')) {
    mapInstance.value.createPane('bottomMarkers').style.zIndex = 3000
  }

  proxy.getConfigKey('mapCenter').then((response) => {
    var ss = response.data.data.split(',')
    var newLatLng = L.latLng(parseFloat(ss[0]), parseFloat(ss[1]))
    mapInstance.value.setView(newLatLng)
  })

  initTrackManager()
}

function setMapContainerRef(el) {
  if (!el) return
  mapContainerRef.value = el
}

async function refreshEmbeddedMapSize() {
  if (!mapInstance.value) {
    return
  }
  await nextTick()
  mapInstance.value.invalidateSize()
}

function handall() {
  if (!mapInstance.value) {
    return
  }

  clearRadarSectors()

  listRadar().then((response) => {
    if (response.data.data.length == 0) {
      return
    }

    radarOptions.value = response.data.data

    for (let item of radarOptions.value) {
      let begin = parseFloat(item.northDeviationAngle) - item.defenceAngle / 2
      let end = parseFloat(item.northDeviationAngle) + item.defenceAngle / 2

      const sectorLayer = ints({
        map: mapInstance.value,
        lat: item.latitude,
        lon: item.longitude,
        radius: parseFloat(item.defenceRadius),
        startAngle: begin,
        endAngle: end,
        color: item.defenceEnable == 1 ? 'Yellow' : 'red',
        fillLength: item.status == 1 ? 0.1 : 0.1,
        markerKey: item.ip,
        markerOptions: {
          pane: 'bottomMarkers',
          draggable: false,
          ip: item.ip,
        },
        iconUrl: defaultRadarIconUrl,
      })

      if (sectorLayer) {
        sectors.value.push(sectorLayer)
      }
    }
  })
}

function clearRadarSectors() {
  if (!mapInstance.value) {
    sectors.value = []
    return
  }

  sectors.value.forEach((sector) => {
    if (sector?.polygon) {
      mapInstance.value.removeLayer(sector.polygon)
    }
    if (sector?.marker) {
      unregisterSectorMarker(mapInstance.value, sector.marker)
      mapInstance.value.removeLayer(sector.marker)
    }
  })
  sectors.value = []
}

async function init(api, acceptMsg, sendMsg) {
  console.log('signalRapi 请求地址：', api)

  const { connection: sharedConnection, unsubscribe } = await initSignalR({
    api,
    acceptMsg,
    sendMsg,
    onAcceptMessage: (res) => {
      radarAlertSwitcher.handlePayload(res)
      handleRadarData(res)
    },
    onTrackTargetData: (res) => {
      trackTarget(res)
    },
  })

  connection.value = sharedConnection
  unsubscribeSignalR = unsubscribe
  setSignalRReceiveEnabled(true)
}

const initTrackManager = () => {
  if (!mapInstance.value) return

  trackManager.value = new TrackManager(mapInstance.value, {
    historyLength: 50,
    cleanupTimeout: 15000,
    lineColor: '#3498db',
    lineWeight: 3,
    lineOpacity: 0.7,
  })

  trackManager.value.onTargetTracked = (targetId) => {
    trackedTargetId.value = targetId
  }
}

const handleRadarData = (res) => {
  try {
    const serverData = JSON.parse(res)
    if (trackManager.value) {
      trackManager.value.processRadarData(serverData)
    }
  } catch (error) {
    console.log('处理雷达数据失败:', error)
  }
}

const trackTarget = (targetId) => {
  if (trackManager.value) {
    trackManager.value.setTrackTarget(targetId)
  }
}
function normalizeCamera(item = {}, idx = 0) {
  return {
    id: item.id ?? item.cameraId ?? item.pid ?? idx + 1,
    zoneName: `防区: ${item.zoneName}`,
    ip: item.ip,
    username: item.username,
    password: item.password,
    rtsp: item.cameraURL,
    channelId: Number(item.channelId ?? item.channel ?? item.channelNo ?? 0),
    checked: item.checked !== false,
  }
}

function extractCameraList(response) {
  const data = response?.data
  if (Array.isArray(data?.data)) return data.data
  if (Array.isArray(data?.rows)) return data.rows
  if (Array.isArray(data)) return data
  return []
}

async function fetchCameraList() {
  try {
    loadingCameras.value = true
    const prevCheckedById = new Map(
      cameras.value.map((camera) => [String(camera.id), camera.checked !== false]),
    )
    const response = await listCameraPreview()
    const list = extractCameraList(response)
    cameras.value = list.map((item, idx) => {
      const normalized = normalizeCamera(item, idx)
      const key = String(normalized.id)
      if (prevCheckedById.has(key)) {
        normalized.checked = prevCheckedById.get(key)
      }
      return normalized
    })
    await refreshAutoPreview()
    if (!cameras.value.length) {
      ElMessage.warning('未获取到可预览相机')
    }
  } catch (error) {
    console.error('获取预览相机失败：', error)
    ElMessage.error('获取预览相机失败')
  } finally {
    loadingCameras.value = false
  }
}

function setLayout(n) {
  // 如果用户手动切换布局，停止自动轮换以锁定当前布局
  if (autoSwitch.value) {
    autoSwitch.value = false
    stopAuto()
  }
  layout.value = n
}

function setSlotRef(el, idx) {
  if (!el) return
  slotRefs.value[idx] = el
}

function getWindowChromeOffset() {
  const horizontalOffset = Math.max((window.outerWidth - window.innerWidth) / 2, 0)
  const verticalOffset = Math.max(window.outerHeight - window.innerHeight - horizontalOffset, 0)
  return { left: horizontalOffset, top: verticalOffset }
}

function buildWinRectForSlot(idx) {
  const el = slotRefs.value[idx]
  if (!el) return null

  const bodyEl = el.querySelector('.cell-body') || el
  const rect = bodyEl.getBoundingClientRect()
  const chromeOffset = getWindowChromeOffset()
  const screenLeft = typeof window.screenX === 'number' ? window.screenX : window.screenLeft || 0
  const screenTop = typeof window.screenY === 'number' ? window.screenY : window.screenTop || 0

  return {
    left: Math.round(screenLeft + chromeOffset.left + rect.left),
    top: Math.round(screenTop + chromeOffset.top + rect.top),
    width: Math.round(rect.width),
    height: Math.round(rect.height),
  }
}

function buildWinOptions(idx) {
  const slot = slots.value[idx] || {}
  if (!isCameraSlot(slot)) return null
  const rect = buildWinRectForSlot(idx)
  if (!rect) return null

  const sourceIndex = Number(slot.sourceIndex ?? idx)
  const winId = `livePreview-camera-${sourceIndex + 1}`
  return {
    winId,
    deviceName: slot.zoneName || slot.ip || '',
    rtspUrl: slot.rtsp || '',
    username: slot.username || '',
    password: slot.password || '',
    deviceId: String(slot.id ?? idx + 1),
    channelId: Number(slot.channelId ?? 0),
    crossVisible: 0,
    ...rect,
  }
}

async function openPreview(idx, options = {}) {
  const { markPlaying = true, showError = true } = options
  const slot = slots.value[idx] || {}
  if (!isCameraSlot(slot)) {
    delete activeWinMap[idx]
    delete activePreviewSignatureMap[idx]
    return
  }

  if (!slot.rtsp) {
    if (showError) {
      ElMessage.error('当前相机未配置 RTSP 地址')
    }
    if (markPlaying) {
      playingMap[idx] = false
    }
    delete activeWinMap[idx]
    return
  }

  if (markPlaying) {
    playingMap[idx] = true
  }

  if (!isPageForeground.value || !isDocumentActuallyForeground()) {
    return
  }

  const winOptions = buildWinOptions(idx)
  if (!winOptions) return

  await sendCreateWinMessage(winOptions)
  activeWinMap[idx] = winOptions.winId
  activePreviewSignatureMap[idx] = getSlotPreviewSignature(slot)
}

function getSlotPreviewSignature(slot = {}) {
  if (!isCameraSlot(slot)) return ''
  return [
    slot.sourceIndex ?? '',
    slot.id ?? '',
    slot.channelId ?? '',
    slot.rtsp ?? '',
    slot.username ?? '',
  ].join('|')
}

async function closePreviewByIndexes(indexes = []) {
  const payload = indexes
    .map((idx) => ({ idx, winId: activeWinMap[idx] }))
    .filter((item) => item.winId)
    .map((item) => ({ winId: item.winId }))

  if (payload.length) {
    await sendDeleteWinMessage(payload)
  }

  indexes.forEach((idx) => {
    delete activeWinMap[idx]
    delete activePreviewSignatureMap[idx]
    playingMap[idx] = false
  })
}

async function syncPreviewBySlots() {
  const desiredEntries = []
  const desiredIndexes = new Set()

  for (let idx = 1; idx < layout.value; idx += 1) {
    const slot = slots.value[idx] || {}
    if (!isCameraSlot(slot)) continue
    if (!slot.rtsp) {
      playingMap[idx] = false
      continue
    }

    const signature = getSlotPreviewSignature(slot)
    desiredEntries.push({ idx, signature })
    desiredIndexes.add(idx)
    playingMap[idx] = true
  }

  Object.keys(playingMap).forEach((key) => {
    const idx = Number(key)
    if (!desiredIndexes.has(idx)) {
      playingMap[idx] = false
    }
  })

  const signatureToActiveIndex = new Map()
  Object.keys(activePreviewSignatureMap).forEach((key) => {
    const idx = Number(key)
    const signature = activePreviewSignatureMap[idx]
    if (!signature) return
    if (!signatureToActiveIndex.has(signature)) {
      signatureToActiveIndex.set(signature, idx)
    }
  })

  const movedIndexes = []
  desiredEntries.forEach(({ idx, signature }) => {
    if (activePreviewSignatureMap[idx] === signature && activeWinMap[idx]) {
      return
    }

    const oldIdx = signatureToActiveIndex.get(signature)
    if (typeof oldIdx === 'number' && oldIdx !== idx && activeWinMap[oldIdx]) {
      activeWinMap[idx] = activeWinMap[oldIdx]
      activePreviewSignatureMap[idx] = activePreviewSignatureMap[oldIdx]
      delete activeWinMap[oldIdx]
      delete activePreviewSignatureMap[oldIdx]
      movedIndexes.push(idx)
      signatureToActiveIndex.set(signature, idx)
    }
  })

  const indexesToClose = Object.keys(activeWinMap)
    .map((key) => Number(key))
    .filter((idx) => !desiredIndexes.has(idx))

  await closePreviewByIndexes(indexesToClose)

  if (!isPageForeground.value) {
    return
  }

  await nextTick()
  for (const idx of movedIndexes) {
    const winId = activeWinMap[idx]
    const rect = buildWinRectForSlot(idx)
    if (!winId || !rect) continue
    await sendUpdateWinPosMessage([{ winId, ...rect }])
  }

  for (const { idx, signature } of desiredEntries) {
    if (activePreviewSignatureMap[idx] === signature && activeWinMap[idx]) {
      continue
    }

    if (activeWinMap[idx]) {
      await closePreviewByIndexes([idx])
    }
    await openPreview(idx, { markPlaying: true, showError: false })
  }
}

async function applyFullscreenPreview(nextIndex) {
  // 先关闭当前预览窗口，再按新状态重开
  await closeAllRemoteWindows(true)
  fullscreenSlotIndex.value = nextIndex

  await nextTick()

  if (!isPageForeground.value) {
    return
  }

  if (typeof nextIndex === 'number') {
    if (playingMap[nextIndex]) {
      await openPreview(nextIndex, { markPlaying: true, showError: false })
    }
    return
  }

  await restorePlayingWindows()
}

async function toggleFullscreen(idx) {
  if (fullscreenSwitching.value) {
    return
  }

  const slot = slots.value[idx] || {}
  if (isCameraSlot(slot) && !slot.rtsp) {
    ElMessage.warning('当前窗口未配置 RTSP 地址')
    return
  }

  try {
    fullscreenSwitching.value = true
    const nextIndex = fullscreenSlotIndex.value === idx ? null : idx
    await applyFullscreenPreview(nextIndex)
  } finally {
    fullscreenSwitching.value = false
  }
}

async function closeAllRemoteWindows(preservePlaying = false) {
  const winIds = Object.values(activeWinMap)

  if (winIds.length) {
    await sendDeleteWinMessage(winIds.map((winId) => ({ winId })))
  }

  Object.keys(activeWinMap).forEach((key) => {
    delete activeWinMap[key]
  })
  Object.keys(activePreviewSignatureMap).forEach((key) => {
    delete activePreviewSignatureMap[key]
  })

  if (!preservePlaying) {
    Object.keys(playingMap).forEach((key) => {
      playingMap[key] = false
    })
  }
}

function closeAllRemoteWindowsSync(preservePlaying = false) {
  const winIds = Object.values(activeWinMap)

  if (winIds.length) {
    sendDeleteWinMessageSync(winIds.map((winId) => ({ winId })))
  }

  Object.keys(activeWinMap).forEach((key) => {
    delete activeWinMap[key]
  })
  Object.keys(activePreviewSignatureMap).forEach((key) => {
    delete activePreviewSignatureMap[key]
  })

  if (!preservePlaying) {
    Object.keys(playingMap).forEach((key) => {
      playingMap[key] = false
    })
  }
}

async function restorePlayingWindows() {
  if (!isDocumentActuallyForeground()) {
    return
  }

  const indexes = Object.keys(playingMap)
    .filter((key) => playingMap[key])
    .map((key) => Number(key))

  for (const idx of indexes) {
    await openPreview(idx, { markPlaying: true, showError: false })
  }
}

async function refreshAutoPreview() {
  await syncPreviewBySlots()
}

async function handleCameraCheckedChange() {
  if (typeof fullscreenSlotIndex.value === 'number') {
    await applyFullscreenPreview(fullscreenSlotIndex.value)
    return
  }
  await syncPreviewBySlots()
}

const { isPageForeground, syncPreviewVisibility } = usePreviewVisibility({
  onForeground: async () => {
    if (!isDocumentActuallyForeground()) {
      return
    }

    await nextTick()
    if (typeof fullscreenSlotIndex.value === 'number') {
      await applyFullscreenPreview(fullscreenSlotIndex.value)
      return
    }
    await restorePlayingWindows()
  },
  onBackground: async () => {
    await closeAllRemoteWindows(true)
  },
})

async function syncPlayingWindowsRect() {
  const updates = []
  Object.keys(activeWinMap).forEach((key) => {
    const idx = Number(key)
    const winId = activeWinMap[idx]
    const rect = buildWinRectForSlot(idx)
    if (!winId || !rect) return
    updates.push({ winId, ...rect })
  })

  if (updates.length) {
    await sendUpdateWinPosMessage(updates)
  }
}

function waitForAnimationFrame() {
  return new Promise((resolve) => {
    window.requestAnimationFrame(() => resolve())
  })
}

async function syncPlayingWindowsRectAfterLayoutChange() {
  if (!isPageForeground.value) {
    return
  }

  // 仅 nextTick 在 6 -> 9（列数不变，仅行高变化）场景下可能拿到旧尺寸
  // 多等待 1~2 帧，确保 CSS Grid 完成重排后再下发窗口坐标
  await nextTick()
  await waitForAnimationFrame()
  await syncPlayingWindowsRect()
  await waitForAnimationFrame()
  await syncPlayingWindowsRect()
}

function getLayoutRowCount(layoutCount) {
  if (layoutCount === 9) return 3
  return 2
}

async function recreatePlayingWindowsForLayoutChange() {
  await closeAllRemoteWindows(true)

  if (!isPageForeground.value) {
    return
  }

  await nextTick()

  if (typeof fullscreenSlotIndex.value === 'number') {
    if (playingMap[fullscreenSlotIndex.value]) {
      await openPreview(fullscreenSlotIndex.value, { markPlaying: true, showError: false })
    }
    return
  }

  await restorePlayingWindows()
}

function startAuto() {
  stopAuto()
  autoTimer.value = window.setInterval(() => {
    if (layout.value === 4) layout.value = 6
    else if (layout.value === 6) layout.value = 9
    else layout.value = 4
  }, 5000)
}

function stopAuto() {
  if (!autoTimer.value) return
  window.clearInterval(autoTimer.value)
  autoTimer.value = null
}

async function handleResize() {
  if (!isPageForeground.value) {
    return
  }
  await nextTick()
  await syncPlayingWindowsRect()
}

function handlePageExit() {
  if (hasHandledPageExit.value) {
    return
  }

  hasHandledPageExit.value = true
  stopAuto()
  window.removeEventListener('resize', handleResize)
  closeAllRemoteWindowsSync(true)
}

watch(autoSwitch, (val) => {
  if (val) startAuto()
  else stopAuto()
})

watch(layout, async (newLayout, oldLayout) => {
  await refreshAutoPreview()

  const rowCountChanged = getLayoutRowCount(newLayout) !== getLayoutRowCount(oldLayout)
  if (rowCountChanged) {
    // 某些本地播放器在仅高度缩小时 updateWinPos 不稳定（典型：6 -> 9）
    // 行数变化时强制重建窗口，确保尺寸与位置完全一致
    await recreatePlayingWindowsForLayoutChange()
  }

  await syncPlayingWindowsRectAfterLayoutChange()
  await refreshEmbeddedMapSize()
})

watch(fullscreenSlotIndex, async () => {
  await refreshEmbeddedMapSize()
})

onMounted(async () => {
  await nextTick()
  initEmbeddedLeafletMap()
  handall()
  await init(longLinkApi, longLinkMsg, longLinkSendMsg)
  await fetchCameraList()
  await syncPreviewVisibility()
  window.addEventListener('resize', handleResize)
  window.addEventListener('beforeunload', handlePageExit)
  window.addEventListener('pagehide', handlePageExit)
  await refreshEmbeddedMapSize()
})

onBeforeUnmount(() => {
  handlePageExit()
  window.removeEventListener('beforeunload', handlePageExit)
  window.removeEventListener('pagehide', handlePageExit)
  clearRadarSectors()
  radarAlertSwitcher.resetAll()
  if (trackManager.value) {
    trackManager.value.clearAll()
    if (trackManager.value.cleanupInterval) {
      clearInterval(trackManager.value.cleanupInterval)
    }
    trackManager.value = null
  }
  if (typeof unsubscribeSignalR === 'function') {
    unsubscribeSignalR()
    unsubscribeSignalR = null
  }
  connection.value = null
  if (mapInstance.value) {
    mapInstance.value.remove()
    mapInstance.value = null
  }
  mapContainerRef.value = null
})
</script>

<style scoped>
.live-preview-page {
  height: 100vh;
  min-height: 560px;
  padding: 10px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  box-sizing: border-box;
}

.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.content-wrap {
  flex: 1 1 auto;
  min-height: 0;
  display: flex;
  gap: 10px;
}

.camera-list-panel {
  width: 240px;
  min-width: 240px;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: #fff;
}

.camera-list-title {
  padding: 10px;
  font-size: 14px;
  font-weight: 600;
  border-bottom: 1px solid #eef2f7;
}

.camera-list-body {
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
  padding: 6px;
}

.camera-list-empty {
  color: #94a3b8;
  font-size: 13px;
  padding: 8px;
}

.camera-list-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 8px;
  border-radius: 4px;
}

.camera-list-item:hover {
  background: #f8fafc;
}

.camera-index {
  color: #64748b;
  width: 26px;
  flex: 0 0 26px;
}

.camera-ip {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.grid-wrap {
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
}

.grid {
  width: 100%;
  height: 100%;
  display: grid;
  gap: 8px;
}

.grid.is-fullscreen {
  grid-template-columns: 1fr !important;
  grid-template-rows: 1fr !important;
}

.grid.layout-4 {
  grid-template-rows: repeat(2, minmax(300px, 1fr));
}

.grid.layout-6 {
  grid-template-rows: repeat(2, minmax(210px, 1fr));
}

.grid.layout-9 {
  grid-template-rows: repeat(3, minmax(160px, 1fr));
}

.grid-cell {
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  display: flex;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
}

.grid-cell.is-hidden {
  display: none;
}

.grid-cell.is-fullscreen-cell {
  min-height: 0;
}

.cell-header {
  height: 36px;
  padding: 6px 8px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid #eef2f7;
}

.cell-title {
  font-size: 13px;
  font-weight: 500;
  color: #0b1220;
  display: inline-block;
  max-width: calc(100% - 120px);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cell-status {
  font-size: 12px;
  color: #64748b;
}

.cell-header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.cell-fullscreen-btn {
  border: 1px solid #cbd5e1;
  background: #fff;
  color: #334155;
  width: 22px;
  height: 22px;
  border-radius: 4px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 12px;
  line-height: 1;
}

.cell-fullscreen-btn:hover:not(:disabled) {
  border-color: #94a3b8;
}

.cell-fullscreen-btn:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.cell-body {
  flex: 1 1 auto;
  min-height: 0;
  background: #0f172a;
  color: #dbeafe;
  padding: 10px;
  font-size: 12px;
  line-height: 1.5;
}

.cell-body-embed {
  padding: 0;
  position: relative;
  background: #f8fafc;
}

.embedded-leaflet-map {
  width: 100%;
  height: 100%;
  border: 0;
  display: block;
  background: #fff;
}

.line {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.editor {
  border-top: 1px solid #eef2f7;
  padding-top: 8px;
}
</style>
