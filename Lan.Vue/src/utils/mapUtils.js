import L from 'leaflet'

const iconCache = new Map()

const getIconCacheKey = (iconUrl, iconSize = [25, 25], iconAnchor = [12, 25]) => {
  const sizeKey = Array.isArray(iconSize) ? iconSize.join(',') : ''
  const anchorKey = Array.isArray(iconAnchor) ? iconAnchor.join(',') : ''
  return `${iconUrl}|${sizeKey}|${anchorKey}`
}

const getOrCreateIcon = (iconUrl, iconSize = [25, 25], iconAnchor = [12, 25]) => {
  const key = getIconCacheKey(iconUrl, iconSize, iconAnchor)
  if (iconCache.has(key)) {
    return iconCache.get(key)
  }

  const icon = L.icon({
    iconUrl,
    iconSize,
    iconAnchor,
  })

  iconCache.set(key, icon)
  return icon
}

const degreeToRadian = (degree) => {
  return ((90 - degree) * Math.PI) / 180
}

const createSectorPoints = (lat, lon, radius, startAngle, endAngle) => {
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

  return { center, points }
}

/**
 * 创建跟踪目标扇形（4°，半透明红色虚线）
 * @param {L.Map} map - Leaflet 地图实例
 * @param {number} radarLat - 雷达纬度
 * @param {number} radarLng - 雷达经度
 * @param {number} azimuthAngle - 目标方位角（度，0=北，90=东）
 * @param {number} distance - 目标距离（米）
 * @returns {L.Polygon} 跟踪扇形 Leaflet 多边形
 */
export function createTrackingSector(map, radarLat, radarLng, azimuthAngle, distance) {
  const startAngle = azimuthAngle - 4
  const endAngle = azimuthAngle + 4
  const radius = distance * 1.2
  const { points } = createSectorPoints(radarLat, radarLng, radius, startAngle, endAngle)
  return L.polygon(points, {
    color: '#ff0000',
    fillColor: '#ff0000',
    fillOpacity: 0.12,
    weight: 1.5,
    interactive: false,
  }).addTo(map)
}

/**
 * 更新已有跟踪扇形位置（不删除重建，直接 setLatLngs）
 * @param {L.Polygon} polygon - 已有的跟踪扇形多边形
 * @param {number} radarLat - 雷达纬度
 * @param {number} radarLng - 雷达经度
 * @param {number} azimuthAngle - 目标方位角（度）
 * @param {number} distance - 目标距离（米）
 */
export function updateTrackingSector(polygon, radarLat, radarLng, azimuthAngle, distance) {
  if (!polygon) return
  const startAngle = azimuthAngle - 4
  const endAngle = azimuthAngle + 4
  const radius = distance * 1.2
  const { points } = createSectorPoints(radarLat, radarLng, radius, startAngle, endAngle)
  polygon.setLatLngs(points)
}

const getMarkerStore = (map) => {
  if (!map.__sectorMarkerStore) {
    Object.defineProperty(map, '__sectorMarkerStore', {
      value: new Map(),
      writable: true,
      configurable: true,
    })
  }
  return map.__sectorMarkerStore
}

export function setSectorMarkerIcon(marker, iconUrl, iconSize = [25, 25], iconAnchor = [12, 25]) {
  if (!marker || !iconUrl) return

  const nextIconKey = getIconCacheKey(iconUrl, iconSize, iconAnchor)
  if (marker.__sectorIconKey === nextIconKey) {
    return
  }

  marker.setIcon(getOrCreateIcon(iconUrl, iconSize, iconAnchor))
  marker.__sectorIconKey = nextIconKey
}

export function createOrUpdateSectorMarker({
  map,
  center,
  markerKey,
  markerOptions = {},
  markerProperties = {},
  iconUrl = '/status/radar_lan.png',
  iconSize = [25, 25],
  iconAnchor = [12, 25],
  onMarkerClick,
  onMarkerDragEnd,
}) {
  if (!map || !center) return null

  const store = getMarkerStore(map)
  let marker = markerKey ? store.get(markerKey) : null

  if (!marker) {
    const resolvedMarkerOptions = {
      ...markerOptions,
    }
    if (iconUrl) {
      resolvedMarkerOptions.icon = getOrCreateIcon(iconUrl, iconSize, iconAnchor)
    }
    marker = L.marker(center, resolvedMarkerOptions).addTo(map)
    if (iconUrl) {
      marker.__sectorIconKey = getIconCacheKey(iconUrl, iconSize, iconAnchor)
    }
    if (markerKey) {
      store.set(markerKey, marker)
    }
  } else {
    marker.setLatLng(center)
    if (iconUrl) {
      setSectorMarkerIcon(marker, iconUrl, iconSize, iconAnchor)
    }
  }

  marker.properties = markerProperties
  marker.off('click')
  marker.off('dragend')

  if (typeof onMarkerClick === 'function') {
    marker.on('click', (event) => {
      onMarkerClick({ event, marker, center, properties: markerProperties })
    })
  }

  if (typeof onMarkerDragEnd === 'function') {
    marker.on('dragend', (event) => {
      onMarkerDragEnd({ event, marker, center, properties: markerProperties })
    })
  }

  return marker
}

export function unregisterSectorMarker(map, markerOrKey) {
  if (!map || !map.__sectorMarkerStore || !markerOrKey) return

  const store = map.__sectorMarkerStore
  if (typeof markerOrKey === 'string') {
    store.delete(markerOrKey)
    return
  }

  for (const [key, marker] of store.entries()) {
    if (marker === markerOrKey) {
      store.delete(key)
      return
    }
  }
}

export function parseSignalrPayload(payload) {
  if (typeof payload !== 'string') {
    return payload
  }

  try {
    return JSON.parse(payload)
  } catch {
    return null
  }
}

export function normalizeIp(ip) {
  return String(ip || '')
    .trim()
    .toLowerCase()
}

export function extractHost(ip) {
  const value = normalizeIp(ip)
  if (!value) return ''
  return value.split(':')[0]
}

export function getRadarIpsFromPayload(payload) {
  const data = parseSignalrPayload(payload)
  if (!data) return []

  const result = []
  const pickIp = (item) =>
    item?.radarIp || item?.radarIP || item?.RadarIp || item?.RadarIP || item?.ip || item?.Ip

  if (Array.isArray(data)) {
    data.forEach((item) => {
      const ip = pickIp(item)
      if (ip) result.push(ip)
    })
    return result
  }

  if (typeof data === 'object') {
    const ip = pickIp(data)
    if (ip) result.push(ip)
  }

  return result
}

function resolveSectors(sectorsOrRef) {
  if (Array.isArray(sectorsOrRef)) {
    return sectorsOrRef
  }
  return sectorsOrRef?.value || []
}

export function findSectorMarkerByRadarIp(sectorsOrRef, radarIp) {
  const sectors = resolveSectors(sectorsOrRef)
  const incomingIp = normalizeIp(radarIp)
  const incomingHost = extractHost(radarIp)

  return sectors.find((sector) => {
    const markerIp = sector?.marker?.options?.ip
    if (!markerIp) return false

    const markerIpNorm = normalizeIp(markerIp)
    const markerHost = extractHost(markerIp)

    return (
      markerIpNorm === incomingIp ||
      markerIpNorm === incomingHost ||
      markerHost === incomingIp ||
      markerHost === incomingHost
    )
  })?.marker
}

export function createRadarAlertSwitcher({
  sectors,
  defaultIconUrl = '/status/radar_lan.png',
  alertIconUrl = '/status/radar_red.png',
  timeout = 3000,
  iconSize = [25, 25],
  iconAnchor = [12, 25],
}) {
  const timers = new Map()

  const setMarkerAlertByIp = (radarIp, isAlert) => {
    if (!radarIp) return
    const marker = findSectorMarkerByRadarIp(sectors, radarIp)
    if (!marker) return
    setSectorMarkerIcon(marker, isAlert ? alertIconUrl : defaultIconUrl, iconSize, iconAnchor)
  }

  const handlePayload = (payload) => {
    const radarIps = getRadarIpsFromPayload(payload)
    if (!radarIps.length) return

    radarIps.forEach((radarIp) => {
      const key = normalizeIp(radarIp)
      if (!key) return

      setMarkerAlertByIp(key, true)

      const oldTimer = timers.get(key)
      if (oldTimer) {
        clearTimeout(oldTimer)
      }

      const timerId = setTimeout(() => {
        setMarkerAlertByIp(key, false)
        timers.delete(key)
      }, timeout)

      timers.set(key, timerId)
    })
  }

  const resetAll = () => {
    timers.forEach((timerId) => clearTimeout(timerId))
    timers.clear()
    const markerList = resolveSectors(sectors)
    markerList.forEach((sector) => {
      if (sector?.marker) {
        setSectorMarkerIcon(sector.marker, defaultIconUrl, iconSize, iconAnchor)
      }
    })
  }

  return {
    handlePayload,
    resetAll,
  }
}

export function ints({
  map,
  lat,
  lon,
  radius,
  startAngle,
  endAngle,
  color = 'red',
  fillLength = 0.1,
  weight = 1,
  interactive = false,
  showMarker = true,
  markerKey,
  markerOptions = {},
  markerProperties = {},
  iconUrl = '/status/radar_lan.png',
  iconSize = [25, 25],
  iconAnchor = [12, 25],
  onMarkerClick,
  onMarkerDragEnd,
  label,
}) {
  if (!map) {
    return null
  }

  const { center, points } = createSectorPoints(lat, lon, radius, startAngle, endAngle)

  const polygon = L.polygon(points, {
    interactive,
    color,
    fillColor: color,
    fillOpacity: fillLength,
    weight,
  }).addTo(map)

  let marker = null
  let labelTooltip = null

  if (showMarker) {
    marker = createOrUpdateSectorMarker({
      map,
      center,
      markerKey,
      markerOptions,
      markerProperties,
      iconUrl,
      iconSize,
      iconAnchor,
      onMarkerClick: ({ event, marker, center, properties }) => {
        if (typeof onMarkerClick === 'function') {
          onMarkerClick({ event, marker, polygon, center, properties })
        }
      },
      onMarkerDragEnd: ({ event, marker, center, properties }) => {
        if (typeof onMarkerDragEnd === 'function') {
          onMarkerDragEnd({ event, marker, polygon, center, properties })
        }
      },
    })
  }

  // 扇形圆心标签 — 始终在扇形朝向的反方向，不遮挡防区
  if (label) {
    const midAngle = (startAngle + endAngle) / 2
    const cLat = parseFloat(lat)
    const cLng = parseFloat(lon)

    const updateLabelPosition = () => {
      const zoom = map.getZoom()
      // 基准 17 级：水平80m，垂直30m；每升一级减50m，不低于10m
      const offsetX = Math.max(10, 80 - (zoom - 17) * 50)
      const offsetY = Math.max(10, 30 - (zoom - 17) * 50)
      const oppositeRad = degreeToRadian(midAngle + 180)
      const labelLat = cLat + (offsetY * Math.sin(oppositeRad)) / 111320
      const labelLng = cLng + (offsetX * Math.cos(oppositeRad)) / (111320 * Math.cos(cLat * Math.PI / 180))
      labelTooltip.setLatLng([labelLat, labelLng])
    }

    labelTooltip = L.tooltip({
      permanent: true,
      direction: 'center',
      className: 'sector-label-tooltip',
    })
      .setContent(label)
      .setLatLng([cLat, cLng])
      .addTo(map)

    updateLabelPosition()
    map.on('zoomend', updateLabelPosition)
    // 存引用以便销毁时解绑
    labelTooltip.__zoomHandler = updateLabelPosition
  }

  return {
    polygon,
    marker,
    center,
    labelTooltip,
  }
}

export const createSectorArea = ints
