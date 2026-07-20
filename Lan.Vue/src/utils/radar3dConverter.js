/**
 * 雷达3D坐标转换工具
 * 将 SignalR 实时数据 (lat/lng/distance/azimuthAngle) 转为以雷达为原点的局部笛卡尔坐标
 */

const EARTH_RADIUS = 6371000 // 米

function toRad(deg) {
  return (deg * Math.PI) / 180
}

/**
 * WGS84 经纬度 → 相对雷达原点的米制坐标
 * @param {number} lat - 目标纬度
 * @param {number} lng - 目标经度
 * @param {number} radarLat - 雷达纬度
 * @param {number} radarLng - 雷达经度
 * @returns {{x: number, y: number}} 东向米, 北向米
 */
export function geoToMeters(lat, lng, radarLat, radarLng) {
  const dLat = toRad(lat - radarLat)
  const dLng = toRad(lng - radarLng)
  const cosLat = Math.cos(toRad((lat + radarLat) / 2))
  return {
    x: dLng * EARTH_RADIUS * cosLat,  // 东
    y: dLat * EARTH_RADIUS,             // 北
  }
}

/**
 * 极坐标 (距离+方位角) → 米制坐标
 * @param {number} distance - 距离 (米)
 * @param {number} azimuthAngleDeg - 方位角 (度, 从北顺时针)
 * @returns {{x: number, y: number}}
 */
export function polarToMeters(distance, azimuthAngleDeg) {
  const azimuthRad = toRad(azimuthAngleDeg)
  return {
    x: distance * Math.sin(azimuthRad),  // 东
    y: distance * Math.cos(azimuthRad),  // 北
  }
}

/**
 * 将 SignalR 原始目标数据转换为 3D 场景可用的 TargetData
 * @param {object} raw - SignalR 推送的原始目标数据
 * @param {number} raw.targetId
 * @param {number|string} raw.lat
 * @param {number|string} raw.lng
 * @param {number|string} raw.distance
 * @param {number|string} raw.azimuthAngle
 * @param {number|string} raw.speedY
 * @param {number} raw.targetType - 1=人, 2=车
 * @param {string} raw.radarIp
 * @param {number} radarLat - 雷达原点纬度
 * @param {number} radarLng - 雷达原点经度
 * @returns {{targetId: string, x: number, y: number, z: number, targetType: number, speed: number, radarIp: string, distance: number, azimuthAngle: number}}
 */
export function convertSignalRToTargetData(raw, radarLat, radarLng) {
  const targetType = parseInt(raw.targetType) || 1
  const distance = parseFloat(raw.distance) || 0
  const azimuthAngle = parseFloat(raw.azimuthAngle) || 0
  const speed = parseFloat(raw.speedY) || 0

  let x, y

  // 优先用经纬度
  const lat = parseFloat(raw.lat)
  const lng = parseFloat(raw.lng)
  if (!isNaN(lat) && !isNaN(lng) && radarLat != null && radarLng != null) {
    const pos = geoToMeters(lat, lng, radarLat, radarLng)
    x = pos.x
    y = pos.y
  } else if (distance > 0) {
    // 回退: 用极坐标
    const pos = polarToMeters(distance, azimuthAngle)
    x = pos.x
    y = pos.y
  } else {
    x = 0
    y = 0
  }

  return {
    targetId: String(raw.targetId),
    x,
    y,
    z: 0, // SignalR 数据目前无高度字段, 默认 0
    targetType,
    speed,
    radarIp: raw.radarIp || '',
    distance,
    azimuthAngle,
  }
}
