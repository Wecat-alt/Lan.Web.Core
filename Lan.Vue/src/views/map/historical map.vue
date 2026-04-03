<template>
  <div class="container">
    <div class="left" id="map-container"></div>
    <div class="right">
      <video
        ref="videoPlayer"
        controls
        :src="videoSrc"
        type="video/mp4"
        autoplay
        muted
        loop
        style="width: 100%; max-width: 800px"
      ></video>
    </div>
  </div>
</template>

<script setup>
import { listTrackInfo } from '@/api/alarm/trackinfo'
import { listRadarByAreaId } from '@/api/device/radar'
import L from 'leaflet'
import 'leaflet.motion/dist/leaflet.motion.js'
import 'leaflet/dist/leaflet.css'
import { onMounted, onUnmounted, ref } from 'vue'

// 常量定义
const mapCenter_lat = 28.202612
const mapCenter_lng = 112.874397
const mapUrl = '/maptile_gaode/{z}/{x}/{y}.jpg'
const mapZoom = 15

// 响应式数据
const videoSrc = ref('')
const videoPlayer = ref(null)
const map = ref(null)
const timer = ref(null)

const queryParams = ref({
  alarmId: 0,
  areaId: 0,
  time: '',
})

// 方法定义
const degreeToRadian = (degree) => {
  return ((90 - degree) * Math.PI) / 180
}

const drawSector = (lat, lon, radius, startAngle, endAngle, color, fillLength) => {
  const startRad = degreeToRadian(startAngle)
  const endRad = degreeToRadian(endAngle)

  const center = L.latLng(parseFloat(lat), parseFloat(lon))

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

  const sector = L.polygon(points, {
    color: color,
    fillColor: color,
    fillOpacity: fillLength,
    weight: 2,
  }).addTo(map.value)
}

const ints = (lat, lon, radius, startAngle, endAngle, color, fillLength) => {
  drawSector(lat, lon, radius, startAngle, endAngle, color, fillLength)
}

const initMap = () => {
  map.value = L.map('map-container', {
    center: [mapCenter_lat, mapCenter_lng],
    zoom: mapZoom,
    attributionControl: false,
    zoomControl: true,
  })

  L.tileLayer(mapUrl).addTo(map.value)
}

const initAlarm = async () => {
  const queryParams1 = new URLSearchParams(window.location.search)
  const param1 = queryParams1.get('alarmid')
  const param2 = queryParams1.get('vn')
  const latitude = queryParams1.get('lat')
  const longitude = queryParams1.get('lon')
  const areaid = queryParams1.get('areaid')
  const time = queryParams1.get('time')

  try {
    const radarRes = await listRadarByAreaId(areaid)
    radarRes.data.data.forEach((e) => {
      const begin = parseFloat(e.northDeviationAngle) - e.defenceAngle / 2
      const end = parseFloat(e.northDeviationAngle) + e.defenceAngle / 2
      ints(
        e.latitude,
        e.longitude,
        parseFloat(e.defenceRadius),
        begin,
        end,
        e.status == 1 ? 'Yellow' : 'red',
        e.status == 1 ? 0.1 : 0.1,
      )
    })

    const newLatLng = L.latLng(parseFloat(latitude), parseFloat(longitude))
    map.value.setView(newLatLng)

    videoSrc.value = param2
    queryParams.value.alarmId = param1
    queryParams.value.areaId = areaid
    queryParams.value.time = time

    const trackRes = await listTrackInfo(queryParams.value)

    for (const key in trackRes.data.data) {
      if (trackRes.data.data.hasOwnProperty(key)) {
        let t1

        if (trackRes.data.data[key].length == 0) {
          continue
        }

        if (trackRes.data.data[key].length == 1) {
          t1 = JSON.parse(
            `[{"lat":${trackRes.data.data[key][0].lat},"lng":${trackRes.data.data[key][0].lng}},{"lat":${trackRes.data.data[key][0].lat},"lng":${trackRes.data.data[key][0].lng}}]`,
          )
        } else {
          t1 = JSON.parse(JSON.stringify(trackRes.data.data[key]))
        }

        const HseqGroup = L.motion
          .seq([
            L.motion
              .polyline(
                t1,
                {
                  color: 'orangered',
                  weight: 5,
                },
                {
                  easing: L.Motion.Ease.easeInOutQuad,
                },
                {
                  removeOnEnd: false,
                  icon: L.divIcon({
                    html: "<img src='marker-icon.png' class='leaflet-marker-icon lmap-icon leaflet-zoom-animated leaflet-interactive' alt='Marker' tabindex='0' role='button' style='margin-left: -14px; margin-top: -45px; width: 25; height: 41px;  z-index: 369;'>",
                    iconSize: L.point(0, 0),
                  }),
                },
              )
              .motionDuration(10000),
          ])
          .addTo(map.value)
          .motionStart()
      }
    }
  } catch (error) {}
}

// 生命周期
onMounted(() => {
  initMap()
  initAlarm()
})

onUnmounted(() => {
  // 清理工作
  if (timer.value) {
    clearTimeout(timer.value)
  }
})
</script>

<style scoped>
.container {
  display: flex;
  width: 100%;
  height: 100vh;
  margin: 0;
  padding: 0;
}

.left,
.right {
  padding: 10px;
}

.left {
  flex: 1;
}

.right {
  flex: 1;
}
</style>
