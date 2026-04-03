import * as L from 'leaflet'

export class TrackManager {
  constructor(map, options = {}) {
    this.map = map
    this.targets = new Map() // 存储所有目标轨迹
    this.options = {
      historyLength: 100, // 保留的历史点数
      cleanupTimeout: 5000, // 5秒清理超时目标（毫秒）
      lineColor: 'yellow', // 默认轨迹线颜色
      lineWeight: 2, // 轨迹线宽度
      lineOpacity: 0.8, // 轨迹线透明度
      ...options,
    }

    // 启动定时清理器
    this.cleanupInterval = setInterval(() => this.cleanup(), 1000)
  }

  // 处理雷达数据
  processRadarData(serverData) {
    try {
      const {
        targetId,
        lat,
        lng,
        distance,
        azimuthAngle,
        speedY,
        northDeviationAngle,
        targetType = 1,
        radarIp,
      } = serverData

      const parsedData = {
        targetId,
        lat: parseFloat(lat),
        lng: parseFloat(lng),
        targetType,
        distance: parseFloat(distance),
        azimuthAngle: parseFloat(azimuthAngle),
        speedY: parseFloat(speedY),
        northDeviationAngle: parseFloat(northDeviationAngle),
        radarIp,
        timestamp: Date.now(),
      }

      this.updateTarget(parsedData)
      return parsedData
    } catch (error) {
      console.error('处理雷达数据失败:', error)
      return null
    }
  }

  // 更新目标轨迹
  updateTarget(targetData) {
    const { targetId, lat, lng } = targetData
    const now = Date.now()

    if (this.targets.has(targetId)) {
      this._updateExistingTarget(targetId, lat, lng, targetData, now)
    } else {
      this._createNewTarget(targetId, lat, lng, targetData, now)
    }
  }

  // 更新已存在的目标
  _updateExistingTarget(targetId, lat, lng, targetData, timestamp) {
    const target = this.targets.get(targetId)
    target.lastUpdate = timestamp

    // 添加新坐标点
    target.coordinates.push([lat, lng])

    // 更新轨迹线
    if (target.polyline) {
      target.polyline.setLatLngs(target.coordinates)
    }

    // 更新标记位置和弹窗
    if (target.marker) {
      target.marker.setLatLng([lat, lng])
      this._updateMarkerPopup(target.marker, targetData)
    }

    // 存储最新数据
    target.data = targetData

    // 限制历史轨迹长度
    this._limitHistoryLength(targetId)
  }

  // 创建新目标
  _createNewTarget(targetId, lat, lng, targetData, timestamp) {
    const coordinates = [[lat, lng]]

    // 创建轨迹线
    const polyline = L.polyline(coordinates, {
      color: this._getLineColor(targetData),
      weight: this.options.lineWeight,
      opacity: this.options.lineOpacity,
      lineJoin: 'round',
    }).addTo(this.map)

    // 创建标记
    const marker = L.marker([lat, lng], {
      icon: this._createMarkerIcon(targetData),
      title: `目标 ${targetId}`,
    }).addTo(this.map)

    // 添加弹窗
    this._updateMarkerPopup(marker, targetData)

    // 存储目标数据
    this.targets.set(targetId, {
      polyline,
      marker,
      coordinates,
      lastUpdate: timestamp,
      data: targetData,
    })
  }

  // 获取轨迹线颜色
  _getLineColor(targetData) {
    // 这里可以根据你的业务逻辑调整颜色
    // 例如：如果这是被跟踪的目标，使用特殊颜色
    const { targetType } = targetData

    // 默认颜色映射
    const colorMap = {
      1: '#00ff00', // 人员 - 绿色
      2: '#ff9900', // 车辆 - 橙色
      3: '#ffff00', // 其他 - 黄色
    }

    return colorMap[targetType] || this.options.lineColor
  }

  // 创建标记图标
  _createMarkerIcon(targetData) {
    const { targetType } = targetData
    let iconUrl, iconSize

    switch (targetType) {
      case 1: // 人员
        iconUrl = '/person.png'
        iconSize = [20, 20]
        break
      case 2: // 车辆
        iconUrl = '/car.png'
        iconSize = [24, 24]
        break
      default:
        iconUrl = '/person.png'
        iconSize = [20, 20]
    }

    return L.icon({
      iconUrl,
      iconSize,
      iconAnchor: [iconSize[0] / 2, iconSize[1] / 2],
      popupAnchor: [0, -iconSize[1] / 2],
    })
  }

  // 更新标记弹窗内容
  _updateMarkerPopup(marker, targetData) {
    const content = this._createPopupContent(targetData)
    marker.bindPopup(content)

    // 如果是被跟踪的目标，自动打开弹窗
    if (this.trackTargetId === targetData.targetId) {
      //marker.openPopup();
    }
  }

  // 创建弹窗内容
  _createPopupContent(targetData) {
    const { targetId, distance, azimuthAngle, speedY, targetType } = targetData

    const typeText = this._getTargetTypeText(targetType)
    const timeText = new Date(targetData.timestamp).toLocaleTimeString()

    return `
      <div style="min-width: 180px; font-size: 12px;">
        <div style="display: flex; align-items: center; margin-bottom: 5px;">
          <img src="${this._getIconPath(
            targetType,
          )}" style="width: 16px; height: 16px; margin-right: 5px;">
          <b>[ID: ${targetId}]</b>
        </div>
        <hr style="margin: 5px 0;">
        <div style="line-height: 1.5;">
          <div><b>D:</b> ${parseFloat(distance).toFixed(2)} m</div>
          <div><b>A:</b> ${parseFloat(azimuthAngle).toFixed(2)}°</div>
          <div><b>S:</b> ${parseFloat(speedY).toFixed(2)} m/s</div>
        </div>
        <hr style="margin: 5px 0;">
        <div style="font-size: 12px; ">
          Time: ${timeText}<br>
          Radar IP: ${targetData.radarIp}
        </div>
      </div>
    `

    // return `
    //   <div style="min-width: 180px; font-size: 12px;">
    //     <div style="display: flex; align-items: center; margin-bottom: 5px;">
    //       <img src="${this._getIconPath(targetType)}" style="width: 16px; height: 16px; margin-right: 5px;">
    //       <b>${typeText} [ID: ${targetId}]</b>
    //     </div>
    //     <hr style="margin: 5px 0;">
    //     <div style="line-height: 1.5;">
    //       <div><b>距离:</b> ${parseFloat(distance).toFixed(2)} m</div>
    //       <div><b>角度:</b> ${parseFloat(azimuthAngle).toFixed(2)}°</div>
    //       <div><b>速度:</b> ${parseFloat(speedY).toFixed(2)} m/s</div>
    //       <div><b>方位角:</b> ${parseFloat(targetData.northDeviationAngle).toFixed(2)}°</div>
    //     </div>
    //     <hr style="margin: 5px 0;">
    //     <div style="font-size: 12px; ">
    //       更新时间: ${timeText}<br>
    //       雷达IP: ${targetData.radarIp}
    //     </div>
    //   </div>
    // `;
  }

  // 获取图标路径
  _getIconPath(targetType) {
    switch (targetType) {
      case 1:
        return '/person.png'
      case 2:
        return '/car.png'
      default:
        return '/person.png'
    }
  }

  // 获取目标类型文本
  _getTargetTypeText(type) {
    switch (type) {
      case 1:
        return '人员'
      case 2:
        return '车辆'
      default:
        return '未知目标'
    }
  }

  // 限制历史轨迹长度
  _limitHistoryLength(targetId) {
    const target = this.targets.get(targetId)
    if (target && target.coordinates.length > this.options.historyLength) {
      target.coordinates = target.coordinates.slice(-this.options.historyLength)
    }
  }

  // 清理超时目标
  cleanup() {
    const now = Date.now()
    const removedTargets = []

    for (const [targetId, target] of this.targets.entries()) {
      if (now - target.lastUpdate > this.options.cleanupTimeout) {
        this._removeTargetFromMap(target)
        this.targets.delete(targetId)
        removedTargets.push(targetId)
      }
    }

    // 如果有目标被移除，触发事件
    if (removedTargets.length > 0 && this.onTargetRemoved) {
      this.onTargetRemoved(removedTargets)
    }

    return removedTargets
  }

  // 从地图移除目标
  _removeTargetFromMap(target) {
    if (target.polyline) {
      this.map.removeLayer(target.polyline)
    }
    if (target.marker) {
      this.map.removeLayer(target.marker)
    }
  }

  // 设置跟踪目标（高亮显示）
  setTrackTarget(targetId) {
    // 重置之前跟踪目标的样式
    if (this.trackTargetId && this.trackTargetId !== targetId) {
      const prevTarget = this.targets.get(this.trackTargetId)
      if (prevTarget && prevTarget.polyline) {
        prevTarget.polyline.setStyle({
          color: this._getLineColor(prevTarget.data),
          weight: this.options.lineWeight,
        })
      }
    }

    // 设置新的跟踪目标
    this.trackTargetId = targetId
    const target = this.targets.get(targetId)

    if (target) {
      // 高亮显示
      if (target.polyline) {
        target.polyline.setStyle({
          color: '#ff0000', // 红色高亮
          weight: this.options.lineWeight + 1, // 加粗
        })
      }

      // 打开弹窗并居中显示
      // if (target.marker) {
      //   target.marker.openPopup();
      //   this.map.panTo(target.marker.getLatLng());
      // }

      // 触发跟踪事件
      if (this.onTargetTracked) {
        this.onTargetTracked(targetId, target.data)
      }
    }
  }

  // 清除所有目标
  clearAll() {
    for (const [targetId, target] of this.targets.entries()) {
      this._removeTargetFromMap(target)
    }
    this.targets.clear()
    this.trackTargetId = null

    if (this.onAllCleared) {
      this.onAllCleared()
    }
  }

  // 获取目标信息
  getTarget(targetId) {
    const target = this.targets.get(targetId)
    return target ? { ...target.data, coordinates: target.coordinates } : null
  }

  // 获取所有目标ID
  getAllTargetIds() {
    return Array.from(this.targets.keys())
  }

  // 获取所有目标数据
  getAllTargets() {
    const result = []
    for (const [targetId, target] of this.targets.entries()) {
      result.push({
        targetId,
        ...target.data,
        coordinates: target.coordinates,
        lastUpdate: target.lastUpdate,
      })
    }
    return result
  }

  // 获取活跃目标数量
  getActiveTargetCount() {
    return this.targets.size
  }

  // 销毁管理器
  destroy() {
    this.clearAll()
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval)
    }
  }

  // 事件回调（外部可以设置这些回调函数）
  onTargetAdded = null // 目标添加时触发
  onTargetUpdated = null // 目标更新时触发
  onTargetRemoved = null // 目标移除时触发
  onTargetTracked = null // 目标被跟踪时触发
  onAllCleared = null // 所有目标清除时触发
}
