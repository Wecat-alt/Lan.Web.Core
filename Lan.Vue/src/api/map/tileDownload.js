import request from '@/utils/request'

/**
 * 启动服务端瓦片下载任务
 * @param {Object} params - 下载参数
 * @param {Object} params.bounds - 矩形区域 { north, south, east, west }
 * @param {number} params.minZoom - 最小层级
 * @param {number} params.maxZoom - 最大层级
 * @param {string} params.tileUrl - 瓦片 URL 模板
 * @param {string} params.targetFolder - 服务器目标文件夹
 */
export function startTileDownload(params) {
  return request({
    url: '/api/tiledownload/start',
    method: 'post',
    data: {
      north: params.bounds.north,
      south: params.bounds.south,
      east: params.bounds.east,
      west: params.bounds.west,
      minZoom: params.minZoom,
      maxZoom: params.maxZoom,
      tileUrl: params.tileUrl,
      targetFolder: params.targetFolder,
    },
  })
}
