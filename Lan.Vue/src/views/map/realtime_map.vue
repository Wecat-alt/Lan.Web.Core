<template>
  <div class="map-canvas">
    <div class="full-size" id="map-container" ref="mapContainerRef"></div>
    <div class="map-on-con">
      <div class="toggle-btn-group">
        <button class="toggle-btn" @click="visible = true">
          <el-icon><Histogram /></el-icon>
        </button>
      </div>
      <el-drawer v-model="visible" :show-close="false">
        <template #header="{ close, titleId, titleClass }">
          <h4 :id="titleId" :class="titleClass">{{ $t('gis.map_msg') }}</h4>
          <el-button @click="setCenter">{{ $t('common.mapCenterSet') }}</el-button>

          <el-button type="danger" @click="close">
            <el-icon class="el-icon--left"><CircleCloseFilled /></el-icon>
            {{ $t('common.close') }}
          </el-button>
        </template>
        <div class="panel-content">
          <div class="map-inform-con">
            <el-form label-width="auto" style="max-width: 600px">
              <el-form-item :label="$t('radar.ip')">
                <el-select clearable v-model="queryParams.id" @change="handleselect">
                  <el-option
                    v-for="dict in radarOptions"
                    :key="dict.id"
                    :label="dict.ip"
                    :value="dict.id"
                  />
                </el-select>
              </el-form-item>
              <el-form-item :label="$t('radar.radar_loc')">
                <el-input-number
                  v-model="queryParams.radarLat"
                  :min="0"
                  :max="360"
                  controls-position="right"
                  @change="handleChangeLan"
                />
                <span style="margin: 0 10px">-</span>
                <el-input-number
                  v-model="queryParams.radarLon"
                  :min="0"
                  :max="360"
                  controls-position="right"
                  @change="handleChangeLon"
                />
              </el-form-item>
              <el-form-item :label="$t('radar.defenceAngle')">
                <el-slider v-model="queryParams.angle" @change="saveAngle" show-input />
              </el-form-item>
              <el-form-item :label="$t('radar.defenceRadius')">
                <el-slider
                  v-model="queryParams.radius"
                  @change="saveRadius"
                  show-input
                  max="1000"
                />
              </el-form-item>
              <el-form-item :label="$t('radar.northDeviationAngle')">
                <el-slider
                  v-model="queryParams.northDeviationAngle"
                  @change="saveDirection"
                  show-input
                  max="360"
                />
              </el-form-item>
            </el-form>
          </div>
        </div>
      </el-drawer>
    </div>
    <!--maponcon-end-->

    <!-- Video popup (bottom-right) -->
    <div v-if="showVideoPopup" class="video-popup">
      <button class="video-close" @click="closeVideoPopup">×</button>
      <video id="popupVideo" :src="videoSrc" controls autoplay muted playsinline></video>
    </div>

    <LocalPlayerWindow
      v-model="previewVisible"
      :title="$t('gis.cameraPreview')"
      :win-options="previewWinOptions"
      :initial-rect="previewRect"
      @closed="handlePreviewClosed"
    />

    <!-- 报警视频弹窗（右下角） -->
    <LocalPlayerWindow
      v-model="alarmPopupVisible"
      :title="alarmPopupTitle"
      :win-options="alarmPopupWinOptions"
      :initial-rect="alarmPopupRect"
      @closed="closeAlarmPopup"
    />

    <!-- Element Plus 对话框选择 -->
    <el-dialog
      v-model="showTypeDialog"
      :title="$t('message.regionType')"
      width="500px"
      :close-on-click-modal="false"
      :close-on-press-escape="false"
      :show-close="false"
    >
      <el-radio-group v-model="selectedType" @change="onTypeSelected">
        <el-radio :label="1" size="large">{{ $t('common.Alarm_Area') }}</el-radio>
        <el-radio :label="2" size="large">{{ $t('common.Filter_Area') }}</el-radio>
        <el-radio :label="3" size="large">{{ $t('common.Warning_Area') }}</el-radio>
        <el-radio :label="4" size="large">{{ $t('common.Auto_Calibration') }}</el-radio>
      </el-radio-group>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="cancelDrawing">{{ $t('common.Cancel_Drawing') }}</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { getCurrentInstance, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'

import { listRadar, updateLatLng, updateRadar } from '@/api/device/radar'
import { addDrawPolygon, delDrawPolygon, listDrawPolygon } from '@/api/map/map'
import { updateConfig } from '@/api/system/config'
import LocalPlayerWindow from '@/components/LocalPlayerWindow.vue'
import '@geoman-io/leaflet-geoman-free'
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'

import { CircleCloseFilled } from '@element-plus/icons-vue'

import { createRadarAlertSwitcher, ints, unregisterSectorMarker } from '@/utils/mapUtils'
import { initSignalR, setSignalRReceiveEnabled } from '@/utils/signalRUtils'
import { TrackManager } from '@/utils/TrackManager'

const trackManager = ref(null)

const visible = ref(false)

const { proxy } = getCurrentInstance()

var mapUrl = window.__APP_CONFIG__.VITE_MAP_TILE_MAP_URL || '/maptile_gaode/{z}/{x}/{y}.jpg'

let sectors = ref([])
const mapCenter_lat = ref(0)
const mapCenter_lng = ref(0)
const mapZoom = ref(16)

let map = ref(null)
const mapContainerRef = ref(null)

let unsubscribeSignalR = null
// 长链接数据接口
const longLinkApi = window.__APP_CONFIG__.VITE_SIGNALR_URL
// 长链接接受数据
const longLinkMsg = 'ReceiveTargetData'
const longLinkSendMsg = 'ReceiveTargetData'

const y_Id = ref(0)

const queryParams = reactive({
  id: undefined,
  angle: undefined,
  radius: undefined,
  northDeviationAngle: undefined,
  radarLat: undefined,
  radarLon: undefined,
})

const form = {
  drawId: 0,
  defenceAreaId: 0,
  pointListLatLng: '',
  status: 1,
  pointType: 0,
}

const drawPolygon = []

const radarOptions = ref([])

// 存储状态的 ref
const showTypeDialog = ref(false)
const selectedType = ref(0) // 默认选择第一个

// 视频弹窗相关状态
const showVideoPopup = ref(false)
const videoSrc = ref('')
const previewVisible = ref(false)
const previewRect = Object.freeze({
  left: 0,
  top: 0,
  width: 550,
  height: 360,
})
const previewWinOptions = ref({})
const activePreviewKey = ref('')

// 报警视频弹窗
const alarmPopupVisible = ref(false)
const alarmPopupWinOptions = ref({})
const alarmPopupTitle = ref('报警视频')
const alarmPopupRect = Object.freeze({
  left: Math.max(0, window.innerWidth - 515),
  top: Math.max(0, window.innerHeight - 285),
  width: 500,
  height: 220,
})
let alarmPopupTimer = null
let latestAlarmTime = null
let currentAlarmRadarIp = null
let alarmCooldownTimer = null
const alarmAutoPopupEnabled = ref(false)

const defaultRadarIconUrl = '/status/radar_lan.png'
const alertRadarIconUrl = '/status/radar_red.png'
const radarAlertSwitcher = createRadarAlertSwitcher({
  sectors,
  defaultIconUrl: defaultRadarIconUrl,
  alertIconUrl: alertRadarIconUrl,
  timeout: 3000,
})

const closeVideoPopup = () => {
  showVideoPopup.value = false
  try {
    const v = document.getElementById('popupVideo')
    if (v) {
      v.pause()
      v.removeAttribute('src')
      v.load()
    }
  } catch (e) {
    console.log('closeVideoPopup error', e)
  }
}

function openLocalPlayerPreview({ cameraIp, username, password, cameraURL }) {
  const normalizedCameraIp = cameraIp || ''
  const normalizedCameraUrl = cameraURL || ''
  const normalizedUserName = username || ''
  const nextPreviewKey = [normalizedCameraIp, normalizedCameraUrl, normalizedUserName]
    .filter(Boolean)
    .join('|')

  if (!nextPreviewKey) {
    return
  }

  if (previewVisible.value && activePreviewKey.value === nextPreviewKey) {
    previewVisible.value = false
    activePreviewKey.value = ''
    return
  }

  previewWinOptions.value = {
    winId: `alarmVideo-${btoa(unescape(encodeURIComponent(nextPreviewKey))).replace(/=+$/g, '')}`,
    rtspUrl: normalizedCameraUrl,
    username: normalizedUserName,
    password: password || '',
  }
  activePreviewKey.value = nextPreviewKey
  previewVisible.value = true
}

function handlePreviewClosed() {
  previewVisible.value = false
  activePreviewKey.value = ''
}

// 报警视频弹窗 - 开启15秒循环定时器
function startAlarmPopupTimer() {
  if (alarmPopupTimer) clearTimeout(alarmPopupTimer)
  alarmPopupTimer = setTimeout(() => {
    closeAlarmPopup()
  }, 15000)
}

// 报警视频弹窗 - 打开
function openAlarmPopup(radarIp) {
  // 自动弹窗已关闭 → 不弹窗
  if (!alarmAutoPopupEnabled.value) return

  // 冷却期 → 不弹窗
  if (alarmCooldownTimer) return

  // 已有弹窗显示中 → 不管几个雷达报警，只弹一次，不切换
  if (alarmPopupVisible.value) return

  const radar = radarOptions.value.find((item) => item.ip === radarIp)
  if (!radar) return

  const { cameraIp, username, password, cameraURL } = radar
  if (!cameraIp || !username || !password || !cameraURL) return

  const uniqueKey = [cameraIp, cameraURL, username].filter(Boolean).join('|')
  alarmPopupWinOptions.value = {
    winId: `alarmPopup-${btoa(unescape(encodeURIComponent(uniqueKey))).replace(/=+$/g, '')}-${Date.now()}`,
    rtspUrl: cameraURL,
    username,
    password,
  }
  alarmPopupTitle.value = '报警视频 - ' + radarIp
  currentAlarmRadarIp = radarIp
  alarmPopupVisible.value = true

  startAlarmPopupTimer()
}

// 报警视频弹窗 - 关闭（2秒冷却后若报警持续则自动重开）
function closeAlarmPopup() {
  if (alarmPopupTimer) {
    clearTimeout(alarmPopupTimer)
    alarmPopupTimer = null
  }

  const savedRadarIp = currentAlarmRadarIp
  alarmPopupVisible.value = false
  currentAlarmRadarIp = null

  if (alarmCooldownTimer) clearTimeout(alarmCooldownTimer)

  alarmCooldownTimer = setTimeout(() => {
    alarmCooldownTimer = null
    if (savedRadarIp && Date.now() - latestAlarmTime < 15000) {
      openAlarmPopup(savedRadarIp)
    }
  }, 2000)
}

onMounted(async () => {
  await nextTick()

  // 先加载配置，确保 initMap 直接用正确的中心点和缩放
  const centerRes = await proxy.getConfigKey('mapCenter')
  if (centerRes?.data?.data) {
    const ss = centerRes.data.data.split(',')
    mapCenter_lat.value = parseFloat(ss[0])
    mapCenter_lng.value = parseFloat(ss[1])
  }
  const zoomRes = await proxy.getConfigKey('mapZoom')
  if (zoomRes?.data?.data) {
    mapZoom.value = parseInt(zoomRes.data.data)
  }
  const isOpenRes = await proxy.getConfigKey('isOpen')
  if (isOpenRes?.data?.data) {
    alarmAutoPopupEnabled.value = String(isOpenRes.data.data) === '1'
  }

  initDrawPolygon()
  initMap()

  // 地图就绪后再启动 SignalR 和绘制雷达扇区
  handall()
  init(longLinkApi, longLinkMsg, longLinkSendMsg)
})
function handleselect() {
  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  queryParams.angle = tt.defenceAngle
  queryParams.radius = tt.defenceRadius
  queryParams.northDeviationAngle = parseFloat(tt.northDeviationAngle)
  queryParams.radarLat = tt.latitude
  queryParams.radarLon = tt.longitude
  y_Id.value = queryParams.id
}
const saveAngle = () => {
  clearAll()
  var tt = radarOptions.value.find((item) => item.id == queryParams.id)
  tt.defenceAngle = queryParams.angle
  updateRadar(tt).then(() => {
    handall()
  })
}
function saveRadius() {
  clearAll()

  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  tt.defenceRadius = queryParams.radius

  updateRadar(tt).then(() => {
    handall()
  })
}
function saveDirection() {
  clearAll()

  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  tt.northDeviationAngle = JSON.stringify(queryParams.northDeviationAngle)

  updateRadar(tt).then(() => {
    handall()
  })
}
function handleChangeLan() {
  clearAll()

  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  tt.latitude = JSON.stringify(queryParams.radarLat)

  updateRadar(tt).then(() => {
    handall()
  })
}
function handleChangeLon() {
  clearAll()

  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  tt.longitude = JSON.stringify(queryParams.radarLon)

  updateRadar(tt).then(() => {
    handall()
  })
}
function handall() {
  listRadar().then((response) => {
    if (response.data.data.length == 0) {
      return
    }

    radarOptions.value = response.data.data

    if (y_Id.value != 0) {
      queryParams.id = y_Id.value
    } else {
      queryParams.id = radarOptions.value[0].id
      y_Id.value = radarOptions.value[0].id
    }

    var tt = radarOptions.value.find((item) => item.id == queryParams.id)

    queryParams.angle = tt.defenceAngle
    queryParams.radius = tt.defenceRadius
    queryParams.northDeviationAngle = parseFloat(tt.northDeviationAngle)
    queryParams.radarLat = tt.latitude
    queryParams.radarLon = tt.longitude

    for (let item of radarOptions.value) {
      let begin = parseFloat(item.northDeviationAngle) - item.defenceAngle / 2
      let end = parseFloat(item.northDeviationAngle) + item.defenceAngle / 2
      const cameraIp = item.cameraIp
      const username = item.username
      const password = item.password
      const cameraURL = item.cameraURL
      const sectorLayer = ints({
        map: map.value,
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
          draggable: true,
          ip: item.ip,
        },
        markerProperties: {
          cameraIp,
          username,
          password,
          cameraURL,
        },
        iconUrl: defaultRadarIconUrl,
        label: item.ip,
        onMarkerClick: ({ properties }) => {
          const { cameraIp, username, password, cameraURL } = properties

          if (cameraIp != null && username != null && password != null && cameraURL != null) {
            openLocalPlayerPreview({
              cameraIp,
              username,
              password,
              cameraURL,
            })
          }
        },
        onMarkerDragEnd: ({ event }) => {
          var lat1 = parseFloat(event.target._latlng.lat).toFixed(6)
          var lng1 = parseFloat(event.target._latlng.lng).toFixed(6)
          const ip = event.target.options.ip

          updateLatLng(ip, lat1, lng1).then((res) => {
            const { status } = res
            if (status == 200) {
              // 同步雷达新位置到 TrackManager，刷新跟踪扇形原点
              if (trackManager.value) {
                trackManager.value.updateRadarPosition(ip, parseFloat(lat1), parseFloat(lng1))
              }
              clearAll()
              proxy.$modal.msgSuccess(proxy.$t('message.success'))
              handall()
            }
          })
        },
      })

      if (sectorLayer) {
        sectors.value.push(sectorLayer)
      }
    }

    // 同步雷达位置到 TrackManager（供跟踪扇形使用）
    if (trackManager.value) {
      radarOptions.value.forEach((item) => {
        trackManager.value.updateRadarPosition(item.ip, item.latitude, item.longitude)
      })
    }
  })
}

function initMap() {
  const container = mapContainerRef.value
  if (!container) {
    console.error('地图容器不存在')
    return
  }

  // 彻底清理容器（防止 Leaflet 残留 DOM 状态）
  container.innerHTML = ''
  container.removeAttribute('style')
  container.classList.forEach((cls) => {
    if (cls.startsWith('leaflet-')) container.classList.remove(cls)
  })

  // 确保容器有高度，否则 Leaflet 会渲染白屏
  if (container.clientHeight === 0) {
    console.warn('地图容器高度为0，延迟初始化')
    setTimeout(() => initMap(), 100)
    return
  }

  console.log('地图URL：', mapUrl)

  map.value = L.map(container, {
    center: [mapCenter_lat.value, mapCenter_lng.value],
    zoom: mapZoom.value,
    attributionControl: false,
    zoomControl: true,
  })

  L.tileLayer(mapUrl).addTo(map.value)

  // 强制刷新尺寸，防止容器高度变化导致白屏
  setTimeout(() => map.value?.invalidateSize(), 50)

  // 初始化轨迹管理器
  initTrackManager()

  if (!map.value.getPane('bottomMarkers')) {
    map.value.createPane('bottomMarkers').style.zIndex = 3000
  }

  if (!map.value.getPane('top')) {
    map.value.createPane('top').style.zIndex = 9999
  }

  map.value.pm.setLang('en')

  map.value.pm.addControls({
    position: 'topleft',
    drawMarker: false,
    drawCircle: false,
    drawPolyline: false,
    drawCircleMarker: false,
    drawText: false,
    editMode: false,
    dragMode: false,
    cutPolygon: false,
    rotateMode: false,
  })

  map.value.on('pm:drawstart', (e) => {
    console.log('pm:drawstart' + e)

    selectedType.value = 0
    showTypeDialog.value = true // 显示选择对话框
  })

  map.value.on('pm:create', (e) => {
    console.log('pm:create' + e)

    var ss = e.layer._latlngs[0]
    let jsonString = JSON.stringify(ss)
    if (e.shape == 'Line') {
      jsonString = JSON.stringify(e.layer._latlngs)
    }

    form.pointListLatLng = jsonString
    form.defenceAreaId = queryParams.id
    form.pointType = selectedType.value
    addDrawPolygon(form).then(() => {
      proxy.$modal.msgSuccess(proxy.$t('message.addSuccess'))
    })
    map.value.pm.setPathOptions({
      color: 'blue',
      fillColor: 'blue',
      fillOpacity: 0.1,
    })
  })

  map.value.on('pm:remove', (e) => {
    //通过移除模式移除图层时触发
    console.log('1' + e)
    var ss = e.layer._latlngs[0]
    const jsonString = JSON.stringify(ss)
    console.log(jsonString)

    const Ids = jsonString
    proxy
      .$confirm(proxy.$t('message.deleteConfirm', { id: Ids }), proxy.$t('common.warning'), {
        confirmButtonText: proxy.$t('common.confirm'),
        cancelButtonText: proxy.$t('common.cancel'),
        type: 'warning',
      })
      .then(function () {
        return delDrawPolygon(encodeURIComponent(jsonString))
      })
      .then(() => {
        proxy.$modal.msgSuccess(proxy.$t('message.deleteSuccess'))
      })
  })

  map.value.pm.setPathOptions({
    color: 'blue',
    fillColor: 'blue',
    fillOpacity: 0.1,
  })
}

function initDrawPolygon() {
  listDrawPolygon().then((res) => {
    if (res.data.code == 200) {
      for (let index = 0; index < res.data.data.length; index++) {
        drawPolygon.push({
          drawidd: res.data.data[index].drawId,
          latlng: res.data.data[index].pointListLatLng,
          pointType: res.data.data[index].pointType,
        })
      }
      for (let index = 0; index < drawPolygon.length; index++) {
        var color = '#ff0000'
        switch (drawPolygon[index].pointType) {
          case 1:
            color = '#ff0000' // 绿色
            break
          case 2:
            color = '#67c23a'
            break
          case 3:
            color = '#8c8c8c' // 深灰色
            break
        }

        if (drawPolygon[index].pointType == 4) {
          var polygonPoints = JSON.parse(drawPolygon[index].latlng)
          L.polyline(polygonPoints, {
            color: color, //"#ff0000",
            weight: 2,
          }).addTo(map.value)
        } else {
          var polygonPoints1 = JSON.parse(drawPolygon[index].latlng)
          L.polygon(polygonPoints1, {
            color: color, //"#ff0000",
            weight: 2,
            fill: true,
            // 填充颜色
            fillColor: color,
            // 填充透明度（0 到 1）
            fillOpacity: 0.2,
          }).addTo(map.value)
        }
      }
    }
  })
}
async function init(api, acceptMsg, sendMsg) {
  console.log('signalRapi 请求地址：', api)

  const { unsubscribe } = await initSignalR({
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

  unsubscribeSignalR = unsubscribe
  setSignalRReceiveEnabled(true)
}
function clearAll() {
  sectors.value.forEach((sector) => {
    map.value.removeLayer(sector.polygon)
    if (sector.marker) {
      unregisterSectorMarker(map.value, sector.marker)
      map.value.removeLayer(sector.marker)
    }
    if (sector.labelTooltip) {
      if (sector.labelTooltip.__zoomHandler) {
        map.value.off('zoomend', sector.labelTooltip.__zoomHandler)
      }
      map.value.removeLayer(sector.labelTooltip)
    }
  })
  sectors.value = []
}

// 类型选择处理
const onTypeSelected = (type) => {
  selectedType.value = type

  proxy.$modal.msgSuccess(`已选择: ${getTypeName(type)}`)
  showTypeDialog.value = false

  // 可以在这里添加其他逻辑，比如更改绘制样式
  if (map.value) {
    // 设置绘制样式（可选）
    map.value.pm.setPathOptions({
      color: getLineColor(type),
      weight: 4,
      opacity: 0.7,
    })
  }
}

const cancelDrawing = () => {
  showTypeDialog.value = false

  // 禁用绘制模式
  if (map.value) {
    map.value.pm.disableDraw('Polyline')
  }

  proxy.$modal.msgSuccess(proxy.$t('common.Cancel_Drawing'))
}

// 获取类型名称
const getTypeName = (type) => {
  const typeMap = {
    1: '报警区域',
    2: '过滤区域',
    3: '预警区域',
  }
  return typeMap[type] || '未知区域'
}

// 获取线条颜色
const getLineColor = (type) => {
  const colorMap = {
    1: 'red', // 绿色
    2: '#67c23a', // 红色
    3: '#8c8c8c', // 黄色
  }
  return colorMap[type] || '#67c23a'
}

onBeforeUnmount(() => {
  if (typeof unsubscribeSignalR === 'function') {
    unsubscribeSignalR()
    unsubscribeSignalR = null
  }

  closeAlarmPopup()
  if (alarmCooldownTimer) {
    clearTimeout(alarmCooldownTimer)
    alarmCooldownTimer = null
  }
  radarAlertSwitcher.resetAll()

  if (map.value) {
    map.value.remove()
    map.value = null
  }
  // 清理容器 DOM，防止残留状态影响下次初始化
  if (mapContainerRef.value) {
    mapContainerRef.value.innerHTML = ''
  }
})

// 初始化轨迹管理器
const initTrackManager = () => {
  if (!map.value) return

  // 构建雷达位置映射（radarIp → {lat, lng}）
  const radarPositions = new Map()
  if (radarOptions.value && radarOptions.value.length > 0) {
    radarOptions.value.forEach((item) => {
      radarPositions.set(item.ip, { lat: item.latitude, lng: item.longitude })
    })
  }

  trackManager.value = new TrackManager(map.value, {
    historyLength: 50, // 保留50个历史点
    cleanupTimeout: 15000, // 5秒清理
    lineColor: '#3498db', // 默认蓝色
    lineWeight: 3, // 线宽
    lineOpacity: 0.7, // 透明度
    radarPositions, // 传入雷达位置映射，供跟踪扇形使用
  })

  // 设置事件回调
  trackManager.value.onTargetAdded = (targetId, targetData) => {
    console.log(`目标 ${targetId} 已添加`, targetData)
  }

  trackManager.value.onTargetUpdated = (targetId, targetData) => {
    console.log(`目标 ${targetId} 已更新`, targetData)
  }

  trackManager.value.onTargetRemoved = (removedTargetIds) => {
    console.log(`目标已移除: ${removedTargetIds.join(', ')}`)
  }

  trackManager.value.onTargetTracked = (targetId, targetData) => {
    console.log(`正在跟踪目标 ${targetId}`, targetData)
  }
}
// 处理雷达数据（SignalR 回调）
const handleRadarData = (res) => {
  try {
    const serverData = JSON.parse(res)
    // 帧级批量：服务端发送的是数组
    const targets = Array.isArray(serverData) ? serverData : [serverData]

    if (trackManager.value) {
      targets.forEach((item) => {
        const processedData = trackManager.value.processRadarData(item)
        if (processedData) {
          latestAlarmTime = Date.now()
          if (processedData.radarIp) {
            openAlarmPopup(processedData.radarIp)
          }
        }
      })
    }
  } catch (error) {
    console.log('处理雷达数据失败:', error)
  }
}

// 跟踪特定目标
const trackTarget = (targetId) => {
  if (trackManager.value) {
    trackManager.value.setTrackTarget(targetId)
  }
}

const setCenter = () => {
  const centerPoint = map.value.getCenter()
  const lat = centerPoint.lat.toFixed(6)
  const lng = centerPoint.lng.toFixed(6)

  const form = {
    configId: '111',
    configName: 'mapCenter',
    configKey: 'mapCenter',
    configValue: lat + ',' + lng,
    configType: '1',
  }

  updateConfig(form).then(() => {
    proxy.$modal.msgSuccess(proxy.$t('message.success'))
  })
}
</script>

<style :scoped lang="scss">
.leaflet-popup-content-wrapper {
  background: transparent !important;
  box-shadow: none !important;
  border: none !important;
  border-radius: 0 !important;
}

.leaflet-popup-content {
  margin: 0 !important;
  background: rgba(255, 255, 255, 0.7) !important; /* 完全透明 */
  /*backdrop-filter: blur(10px) !important;  毛玻璃效果 */
  border-radius: 8px !important;
  padding: 15px !important;
  border: 1px solid rgba(255, 255, 255, 0.5) !important;
}

.leaflet-popup-tip-container {
  display: none !important; /* 隐藏箭头 */
}
/* video popup 样式 */
.video-popup {
  position: fixed;
  right: 20px;
  bottom: 20px;
  z-index: 12000;
  background: rgba(0, 0, 0, 0.6);
  padding: 8px;
  border-radius: 8px;
  display: flex;
  align-items: center;
}
.video-popup video {
  width: 320px;
  height: 180px;
  border-radius: 6px;
  background: #000;
}
.video-popup .video-close {
  position: absolute;
  top: 6px;
  right: 8px;
  background: transparent;
  color: #fff;
  border: none;
  font-size: 18px;
  cursor: pointer;
}
</style>
