<template>
  <div class="map-canvas">
    <div class="full-size" id="map-container"></div>
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

            <div class="con-title" style="display: none">地图信息设置</div>
            <div class="tool-box" style="display: none">
              <el-table :data="tableData" height="390" style="width: 100%">
                <el-table-column prop="id" width="100" label="报警时间" />
                <el-table-column prop="areaName" width="100" label="报警防区" />
                <el-table-column prop="dateTime" label="跟踪相机" />
              </el-table>
            </div>
          </div>
        </div>
      </el-drawer>

      <!-- <div class="inquiry-div">

        <button class="toggle-btn">
          <el-icon><Search /></el-icon>
        </button>
        <div class="inquiry-box">
          <div class="inquiry-input-line">
            <el-input-tag type="text" class="inquiry-input" placeholder="请输入关键词" />
            <el-icon class="close-ico"><Close /></el-icon>
          </div>
          <div class="inquiry-con">
            <div class="inquiry-tab-btn">
              <button class="inquiry-btn"><el-icon><HelpFilled /></el-icon>找设备</button>
              <button class="inquiry-btn"><el-icon><StarFilled /></el-icon>找标绘</button>
              <button class="inquiry-btn"><el-icon><HomeFilled /></el-icon>找站点</button>
            </div>
            <div class="regular-con">
              <div class="inquiry-equ-list">
                <ul>
                  <li>
                    <label class="order-label">监控站</label>
                    默认监控点<el-icon class="el-icon-arrow-right"><ArrowRight /></el-icon>
                  </li>
                  <li>
                    <label class="order-label">设备</label>
                    <label class="order-label-red">离线</label>SP200VF-5<el-icon class="el-icon-arrow-right"><ArrowRight /></el-icon>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div> -->
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
import { getCurrentInstance, onBeforeUnmount, onMounted, reactive, ref } from 'vue'

import { listRadar, updateLatLng, updateRadar } from '@/api/device/radar'
import { addDrawPolygon, delDrawPolygon, listDrawPolygon, sendMsgClose } from '@/api/map/map'
import { updateConfig } from '@/api/system/config'
import LocalPlayerWindow from '@/components/LocalPlayerWindow.vue'
import '@geoman-io/leaflet-geoman-free'
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css'
import * as signalR from '@microsoft/signalr'
import L from 'leaflet'
import 'leaflet.motion/dist/leaflet.motion.js'
import 'leaflet/dist/leaflet.css'

import { CircleCloseFilled } from '@element-plus/icons-vue'

import { TrackManager } from '@/utils/TrackManager'

const trackManager = ref(null)
const targetList = ref([])
const activeTargetCount = ref(0)
const trackedTargetId = ref(null)

const visible = ref(false)

const { proxy } = getCurrentInstance()

var trackRoute = null //json目标数据
var mapUrl = window.__APP_CONFIG__.VITE_MAP_TILE_MAP_URL || '/maptile_gaode/{z}/{x}/{y}.jpg'

let sectors = ref([])
const mapCenter_lat = ref(0)
const mapCenter_lng = ref(0)
const mapZoom = ref(16)

let map = ref(null)
let timer1 = null
let timer2 = null
let timer4 = null

let timerOpen = null

let seqGroup = null
let seqGroupLatLon = []
let seqGroups = []

let his_seqGroups = []

let alarmData = ref({
  alarmRadarIp: null,
  alarmTime: null,
})

let connection = ref(null)
// 长链接数据接口
const longLinkApi = window.__APP_CONFIG__.VITE_SIGNALR_URL
// 长链接接受数据
const longLinkMsg = 'ReceiveTargetData'
const longLinkSendMsg = 'ReceiveTargetData'
const serverData = []

//let  trackTarget=ref('');
// 查询参数
const defenceareaOptions = []

const y_Id = ref(0)

const queryParams = reactive({
  pageNum: 1,
  pageSize: 10,
  sort: 'id',
  sortType: 'asc',
  ip: undefined,
})

const tableData = []

const form = {
  drawId: 0,
  defenceAreaId: 0,
  pointListLatLng: '',
  status: 1,
  pointType: 0,
}

const drawPolygon = []

//Radar扇形图和扇形图集合
const radarDevice = null
const radarDevices = []

const radarDraw = {
  radius: 500, //探测距离
  angle: 60,
  northDeviationAngle: 90, //旋转角度
  lat: 28.202616,
  lng: 112.894302,
  step: 4,
}

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
let videoAutoCloseTimer = null
let externalOpenTimer = null

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
  if (videoAutoCloseTimer) {
    clearTimeout(videoAutoCloseTimer)
    videoAutoCloseTimer = null
  }
  if (externalOpenTimer) {
    clearTimeout(externalOpenTimer)
    externalOpenTimer = null
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

handall()
getList()
init(longLinkApi, longLinkMsg, longLinkSendMsg)

onMounted(() => {
  initDrawPolygon()
  initMap()
  openVideo()
  clear_Target()
  clear_HisTarget()
  clear_SeqGroupLatLon()
})

function getList() {
  // listAlarmRef(queryParams).then(res => {
  //   if (res.data.code == 200) {
  //     tableData = res.data.data
  //   }
  // })
}
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
  updateRadar(tt).then((res) => {
    handall()
  })
}
function saveRadius(newValue) {
  clearAll()

  radarDevices.forEach((item, i) => {
    map.value.removeLayer(item)
  })
  radarDevices.value = []

  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  tt.defenceRadius = queryParams.radius

  updateRadar(tt).then((res) => {
    handall()
  })
}
function saveDirection(newValue) {
  clearAll()

  radarDevices.forEach((item, i) => {
    map.value.removeLayer(item)
  })
  radarDevices.value = []

  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  tt.northDeviationAngle = JSON.stringify(queryParams.northDeviationAngle)

  updateRadar(tt).then((res) => {
    handall()
  })
}
function handleChangeLan(newValue) {
  clearAll()

  radarDevices.forEach((item, i) => {
    map.value.removeLayer(item)
  })
  radarDevices.value = []

  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  tt.latitude = JSON.stringify(queryParams.radarLat)

  updateRadar(tt).then((response) => {
    handall()
  })
}
function handleChangeLon(newValue) {
  clearAll()
  //number | undefined
  radarDevices.forEach((item, i) => {
    map.value.removeLayer(item)
  })
  radarDevices.value = []

  var tt = radarOptions.value.find((item) => item.id === queryParams.id)
  tt.longitude = JSON.stringify(queryParams.radarLon)

  updateRadar(tt).then((response) => {
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
      ;((radarDraw.radius = item.defenceRadius), //探测距离
        (radarDraw.angle = item.defenceAngle),
        (radarDraw.northDeviationAngle = parseFloat(item.northDeviationAngle)), //旋转角度
        (radarDraw.lat = item.latitude),
        (radarDraw.lng = item.longitude),
        (radarDraw.step = item.status == 1 ? 4 : 0))

      let begin = parseFloat(item.northDeviationAngle) - item.defenceAngle / 2
      let end = parseFloat(item.northDeviationAngle) + item.defenceAngle / 2
      const cameraIp = item.cameraIp
      const username = item.username
      const password = item.password
      const cameraURL = item.cameraURL
      ints(
        item.latitude,
        item.longitude,
        parseFloat(item.defenceRadius),
        begin,
        end,
        item.defenceEnable == 1 ? 'Yellow' : 'red',
        item.status == 1 ? 0.1 : 0.1,
        cameraIp,
        username,
        password,
        cameraURL,
        item.ip,
      )
    }
  })
}
function initMap() {
  console.log('地图URL：', mapUrl)

  map.value = L.map('map-container', {
    center: [mapCenter_lat.value, mapCenter_lng.value], // 中心位置
    //center: [28.210508, 112.894302], // 中心位置
    zoom: mapZoom.value, // 缩放等级
    attributionControl: false, // 版权控件
    zoomControl: true, //缩放控件
  })

  L.tileLayer(
    //"http://localhost:1122?style=6&x={x}&y={y}&z={z}"
    //'https://webst01.is.autonavi.com/appmaptile?style=6&x={x}&y={y}&z={z}', //在线天地图
    mapUrl,
  ).addTo(map.value) // 加载底图

  proxy.getConfigKey('mapCenter').then((response) => {
    var ss = response.data.data.split(',')
    var newLatLng = L.latLng(parseFloat(ss[0]), parseFloat(ss[1]))
    map.value.setView(newLatLng)
  })

  proxy.getConfigKey('mapZoom').then((response) => {
    map.value.setZoom(parseInt(response.data.data))
  })

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

    // if (e.shape == "Line") {
    //   console.log("Line" + e);
    // }

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
    addDrawPolygon(form).then((response) => {
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
        // getList()
        proxy.$modal.msgSuccess(proxy.$t('message.deleteSuccess'))
      })
  })

  map.value.on('pm:globalremovalmodetoggled', (e) => {
    //点击开始/结束 删除时出发
    //console.log("4"+e);
  })

  // map.on("layerremove", (e) => {
  //    //删除任何层时触发
  // });
  //initAlarm();

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
function openVideo() {
  timerOpen = setInterval(() => {
    if (alarmData.value.alarmTime != undefined) {
      var time_now = Date.now()
      var ss = time_now - alarmData.value.alarmTime
      if (ss > 3000) {
        alarmData.value.radarIp = null
        alarmData.value.alarmTime = null

        sendMsgClose().then((res) => {
          if (res.data.code == 200) {
            console.log('三秒没有报警信息，则关闭窗口')
          }
        })
      }
    }
  }, 1000)
}
function clear_Target() {
  timer1 = setInterval(() => {
    seqGroups.forEach((element, index) => {
      var time_now = Date.now()
      var ss = time_now - element.timeClear
      if (ss > 15000) {
        map.value.removeLayer(element.seqGroup)
        seqGroups.splice(index, 1)
      }
    })
  }, 1000)
}
function clear_SeqGroupLatLon() {
  timer4 = setInterval(() => {
    seqGroupLatLon.forEach((element, index) => {
      var time_now = Date.now()
      var ss = time_now - element.timeClear
      if (ss > 5000) {
        seqGroupLatLon.splice(index, 1)
      }
    })
  }, 1000)
}
function clear_HisTarget() {
  timer2 = setInterval(() => {
    his_seqGroups.forEach((element, index, arr) => {
      var time_now = Date.now()
      var ss = time_now - element.timeClear
      if (ss > 2000) {
        map.value.removeLayer(element.seqGroup)
        his_seqGroups.splice(index, 1)
      }
    })
  }, 1000)
}
function init(api, acceptMsg, sendMsg) {
  console.log('signalRapi 请求地址：', api)

  connection.value = new signalR.HubConnectionBuilder()
    .withUrl(api, {})
    .withAutomaticReconnect([1000, 4000, 1000, 4000]) // 断线自动重连
    .configureLogging(signalR.LogLevel.Error)
    .build()

  connection.value.on(acceptMsg, (res) => {
    handleRadarData(res)
  })

  connection.value.on('TrackTargetData', (res) => {
    //this.trackTarget=res;

    trackTarget(res)
    //console.log("TrackTargetData", '获取数据：', res);
  })

  //自动重连成功后的处理
  connection.value.onreconnected((connectionId) => {
    console.log(connectionId, '自动重新连接成功')
  })
  // 开始
  if (connection.value.state !== signalR.HubConnectionState.Connected) {
    connection.value.start().then((res) => {
      console.log('启动即时通信成功')
      // connection.invoke()
    })
  }
  // 生命周期
  connection.value.onreconnecting((error) => {
    console.log(acceptMsg, +'**', sendMsg, '重新连接ing', error)
    console.log(1)
    console.log(connection.value.state)
    console.log(connection.value.state === signalR.HubConnectionState.Reconnecting)
  })
  // (默认4次重连)，任何一次只要回调成功，调用
  connection.value.onreconnected((connectionId) => {
    console.log('链接id', connectionId)
    console.log(2)
    console.log(connection.value.state)
    console.log(connection.value.state === signalR.HubConnectionState.Connected)
    if (connection.value.state === signalR.HubConnectionState.Connected) {
      console.log(acceptMsg, +'**', sendMsg, '重连')
      // connection.invoke()
    }
  })
  connection.value.onclose((error) => {
    console.log('关闭', error)
  })
}
function ints(
  lat,
  lon,
  radius,
  startAngle,
  endAngle,
  color,
  fillLength,
  cameraIp,
  username,
  password,
  cameraURL,
  radarIp,
) {
  const degreeToRadian = (degree) => {
    // 将角度转换为弧度，并调整0°为正北方向（数学坐标系）
    // 同时转换为顺时针方向（地理坐标系）
    return ((90 - degree) * Math.PI) / 180
  }
  // 绘制扇形
  const drawSector = (lat, lon, radius, startAngle, endAngle, color, fillLength) => {
    // 将角度转换为弧度（修正后的）
    const startRad = degreeToRadian(startAngle)
    const endRad = degreeToRadian(endAngle)

    var center = L.latLng(parseFloat(lat), parseFloat(lon))

    // 计算扇形点
    const points = [center]
    const steps = Math.max(16, Math.floor(Math.abs(endAngle - startAngle) / 5))

    for (let i = 0; i <= steps; i++) {
      const angle = startRad + (endRad - startRad) * (i / steps)
      const point = L.latLng(
        center.lat + (radius * Math.sin(angle)) / 111320,
        center.lng + (radius * Math.cos(angle)) / (111320 * Math.cos((center.lat * Math.PI) / 180)),
      )
      points.push(point)
    }

    points.push(center)

    // 创建扇形多边形
    const sector = L.polygon(points, {
      interactive: false,
      color: color,
      fillColor: color,
      fillOpacity: fillLength,
      weight: 1,
    }).addTo(map.value)

    const customIcon = L.icon({
      iconUrl: '/radar_lan.png', // 图标路径
      iconSize: [25, 25], // 图标大小，根据实际图片调整
      iconAnchor: [12, 25], // 图标锚点，即图标底部中间点
    })
    const properties = {
      cameraIp: cameraIp,
      username: username,
      password: password,
      cameraURL: cameraURL,
    }
    // 添加扇形中心标记
    const marker = L.marker(center, {
      icon: customIcon,
      pane: 'bottomMarkers',
      draggable: true,
      ip: radarIp,
    }).addTo(map.value)
    marker.properties = properties

    // marker.bindPopup(`
    //   <div>
    //     <strong>扇形信息</strong><br>
    //     中心位置: ${center.lat.toFixed(4)}, ${center.lng.toFixed(4)}<br>
    //     半径: ${radius}米<br>
    //     角度: ${startAngle}°-${endAngle}°<br>
    //     方向: ${getDirectionDescription(startAngle, endAngle)}
    //   </div>
    // `);
    marker.on('click', function (e) {
      const { cameraIp, username, password, cameraURL } = properties

      if (cameraIp != null && username != null && password != null && cameraURL != null) {
        openLocalPlayerPreview({
          cameraIp,
          username,
          password,
          cameraURL,
        })
      }
    })

    marker.on('dragend', function (event) {
      var lat1 = parseFloat(event.target._latlng.lat).toFixed(6)
      var lng1 = parseFloat(event.target._latlng.lng).toFixed(6)

      updateLatLng(event.target.options.ip, lat1, lng1).then((res) => {
        const { status, data } = res
        if (status == 200) {
          //经纬度位置更新成功
          clearAll()
          proxy.$modal.msgSuccess(proxy.$t('message.success'))
          handall()
        }
      })
    })
    sectors.value.push({ polygon: sector, marker: marker })
  }
  //
  //获取方向描述
  const getDirectionDescription = (start, end) => {
    const directions = [
      { angle: 0, name: '北' },
      { angle: 45, name: '东北' },
      { angle: 90, name: '东' },
      { angle: 135, name: '东南' },
      { angle: 180, name: '南' },
      { angle: 225, name: '西南' },
      { angle: 270, name: '西' },
      { angle: 315, name: '西北' },
    ]

    const getDirection = (angle) => {
      let closest = directions[0]
      for (const dir of directions) {
        if (Math.abs(angle - dir.angle) < Math.abs(angle - closest.angle)) {
          closest = dir
        }
      }
      return closest.name
    }

    return `${getDirection(start)} 到 ${getDirection(end)}`
  }

  drawSector(
    lat,
    lon,
    radius,
    startAngle,
    endAngle,
    color,
    fillLength,
    cameraIp,
    username,
    password,
    cameraURL,
  )
}
function clearAll() {
  sectors.value.forEach((sector) => {
    map.value.removeLayer(sector.polygon)
    map.value.removeLayer(sector.marker)
  })
  sectors.value = []

  // if (currentClickListener) {
  //   map.off('click', currentClickListener);
  //   currentClickListener = null;
  // }
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

// 获取标签类型
const getTagType = (type) => {
  const tagTypeMap = {
    1: 'danger', // 报警区域用红色
    2: 'info', // 过滤区域用蓝色
    3: 'warning', // 预警区域用黄色
  }
  return tagTypeMap[type] || ''
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
  connection.value.stop()

  clearInterval(timer1)
  clearInterval(timer2)
  clearInterval(timer4)
  clearInterval(timerOpen)

  sendMsgClose().then((res) => {
    if (res.data.code == 200) {
      console.log('三秒没有报警信息，则关闭窗口')
    }
  })
  map.value.remove()
  map.value = null
})

// 初始化轨迹管理器
const initTrackManager = () => {
  if (!map.value) return

  trackManager.value = new TrackManager(map.value, {
    historyLength: 50, // 保留50个历史点
    cleanupTimeout: 15000, // 5秒清理
    lineColor: '#3498db', // 默认蓝色
    lineWeight: 3, // 线宽
    lineOpacity: 0.7, // 透明度
  })

  // 设置事件回调
  trackManager.value.onTargetAdded = (targetId, targetData) => {
    console.log(`目标 ${targetId} 已添加`, targetData)
    updateTargetList()
  }

  trackManager.value.onTargetUpdated = (targetId, targetData) => {
    console.log(`目标 ${targetId} 已更新`, targetData)
    updateTargetList()
  }

  trackManager.value.onTargetRemoved = (removedTargetIds) => {
    console.log(`目标已移除: ${removedTargetIds.join(', ')}`)
    updateTargetList()
  }

  trackManager.value.onTargetTracked = (targetId, targetData) => {
    console.log(`正在跟踪目标 ${targetId}`, targetData)
    trackedTargetId.value = targetId
  }
}
// 更新目标列表
const updateTargetList = () => {
  if (!trackManager.value) return

  activeTargetCount.value = trackManager.value.getActiveTargetCount()
  targetList.value = trackManager.value.getAllTargets().sort((a, b) => b.timestamp - a.timestamp) // 按时间倒序
}
// 处理雷达数据（SignalR 回调）
const handleRadarData = (res) => {
  try {
    const serverData = JSON.parse(res)

    // 使用轨迹管理器处理数据
    if (trackManager.value) {
      const processedData = trackManager.value.processRadarData(serverData)

      if (processedData) {
        // 这里可以添加其他业务逻辑，比如报警、相机控制等
        //handleAdditionalLogic(processedData)
      }
    }
  } catch (error) {
    console.log('处理雷达数据失败:', error)
  }
}

// 新增：向固定本地接口 POST 摄像头信息
const handleAdditionalLogic = async (targetData) => {
  try {
    const payload = {
      CameraIp:
        targetData.cameraIp ||
        targetData.CameraIp ||
        (targetData.properties && targetData.properties.cameraIp) ||
        '',
      UserName:
        targetData.username ||
        targetData.UserName ||
        (targetData.properties && targetData.properties.username) ||
        '',
      PassWord:
        targetData.password ||
        targetData.PassWord ||
        (targetData.properties && targetData.properties.password) ||
        '',
      CameraURL:
        targetData.cameraURL ||
        targetData.CameraURL ||
        (targetData.properties && targetData.properties.cameraURL) ||
        '',
    }

    await fetch('http://127.0.0.1:56569/data', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    })
      .then((res) => {
        if (!res.ok) console.error('POST /mac failed', res.status)
      })
      .catch((e) => {
        console.error('POST /mac error', e)
      })
  } catch (error) {
    console.error('handleAdditionalLogic error', error)
  }
}

// 跟踪特定目标
const trackTarget = (targetId) => {
  if (trackManager.value) {
    trackManager.value.setTrackTarget(targetId)
  }
}

// 清除所有轨迹
const clearAllTracks = () => {
  if (trackManager.value) {
    trackManager.value.clearAll()
    targetList.value = []
    activeTargetCount.value = 0
    trackedTargetId.value = null
  }
}

// 获取目标类型文本
const getTypeText = (type) => {
  switch (type) {
    case 1:
      return '人员'
    case 2:
      return '车辆'
    default:
      return '未知'
  }
}

// 格式化时间
const formatTime = (timestamp) => {
  const date = new Date(timestamp)
  return `${date.getHours().toString().padStart(2, '0')}:${date
    .getMinutes()
    .toString()
    .padStart(2, '0')}:${date.getSeconds().toString().padStart(2, '0')}`
}

const setCenter = () => {
  //connection.invoke('SendToClientId', 'ReceiveMessage', 'Hello from Browser')

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
.tool-box {
  float: left;
  width: 100%;
  height: 30%;
  box-sizing: border-box;
  text-align: left;
  background-color: #f0f8ff;
}

.slider-demo-block {
  display: flex;
  align-items: center;
}

.slider-demo-block .el-slider {
  margin-top: 0;
  margin-left: 12px;
}

.floating-panel {
  position: fixed;
  top: 0;
  right: 0;
  width: 450px;
  height: 100vh;
  background: #fff;
  box-shadow: -5px 0 25px #00000026;
  z-index: 999;
  transition: transform 0.4s cubic-bezier(0.25, 0.46, 0.45, 0.94);
  transform: translate(100%);
  overflow-y: auto;
}

.floating-panel.active {
  transform: translate(0);
}

@media (max-width: 768px) {
  .content {
    grid-template-columns: 1fr;
  }

  .floating-panel {
    width: 300px;
  }
}

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
