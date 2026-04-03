import '@geoman-io/leaflet-geoman-free'
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css'
import L from 'leaflet'

const DEFAULT_CENTER = [28.210508, 112.894302]
const DEFAULT_ZOOM = 16
const MIN_TILE_ZOOM = 1
const MAX_TILE_ZOOM = 18
const GAODE_SATELLITE_TILE_URL =
  'https://webst0{s}.is.autonavi.com/appmaptile?style=6&x={x}&y={y}&z={z}'

const DEFAULT_SUBDOMAINS = ['1', '2', '3', '4']
export const DEFAULT_TILE_URL = GAODE_SATELLITE_TILE_URL

const normalizeTileUrl = (tileUrl) => {
  if (typeof tileUrl !== 'string') {
    return DEFAULT_TILE_URL
  }

  const nextTileUrl = tileUrl.trim()
  return nextTileUrl || DEFAULT_TILE_URL
}

const normalizeZoom = (value) => {
  const zoom = Number.parseInt(value, 10)
  if (Number.isNaN(zoom)) {
    return MIN_TILE_ZOOM
  }
  return Math.max(MIN_TILE_ZOOM, Math.min(MAX_TILE_ZOOM, zoom))
}

const normalizeZoomRange = (minZoom, maxZoom) => {
  const min = normalizeZoom(minZoom)
  const max = normalizeZoom(maxZoom)
  return min <= max ? [min, max] : [max, min]
}

const clamp = (value, min, max) => Math.max(min, Math.min(max, value))

const longitudeToTileX = (lng, zoom) => {
  const n = 2 ** zoom
  return Math.floor(((lng + 180) / 360) * n)
}

const latitudeToTileY = (lat, zoom) => {
  const n = 2 ** zoom
  const latRad = (lat * Math.PI) / 180
  return Math.floor(((1 - Math.log(Math.tan(latRad) + 1 / Math.cos(latRad)) / Math.PI) / 2) * n)
}

const normalizeBounds = (bounds) => {
  if (!bounds) {
    return null
  }

  if (typeof bounds.getNorth === 'function') {
    return {
      north: bounds.getNorth(),
      south: bounds.getSouth(),
      east: bounds.getEast(),
      west: bounds.getWest(),
    }
  }

  const { north, south, east, west } = bounds
  if ([north, south, east, west].some((value) => Number.isNaN(Number.parseFloat(value)))) {
    return null
  }

  return {
    north: Number(north),
    south: Number(south),
    east: Number(east),
    west: Number(west),
  }
}

const getTileRange = (bounds, zoom) => {
  const worldMax = 2 ** zoom - 1
  const leftLng = Math.min(bounds.west, bounds.east)
  const rightLng = Math.max(bounds.west, bounds.east)
  const topLat = Math.max(bounds.north, bounds.south)
  const bottomLat = Math.min(bounds.north, bounds.south)

  const minX = clamp(longitudeToTileX(leftLng, zoom), 0, worldMax)
  const maxX = clamp(longitudeToTileX(rightLng, zoom), 0, worldMax)
  const minY = clamp(latitudeToTileY(topLat, zoom), 0, worldMax)
  const maxY = clamp(latitudeToTileY(bottomLat, zoom), 0, worldMax)

  return {
    minX: Math.min(minX, maxX),
    maxX: Math.max(minX, maxX),
    minY: Math.min(minY, maxY),
    maxY: Math.max(minY, maxY),
  }
}

const resolveTileUrl = (template, z, x, y, subdomains) => {
  const subdomainList =
    Array.isArray(subdomains) && subdomains.length > 0 ? subdomains : DEFAULT_SUBDOMAINS
  const s = subdomainList[(x + y) % subdomainList.length]

  return template
    .replaceAll('{s}', String(s))
    .replaceAll('{z}', String(z))
    .replaceAll('{x}', String(x))
    .replaceAll('{y}', String(y))
}

const inferFileExt = (contentType, url) => {
  if (contentType?.includes('png')) {
    return 'png'
  }
  if (contentType?.includes('webp')) {
    return 'webp'
  }
  if (contentType?.includes('jpeg') || contentType?.includes('jpg')) {
    return 'jpg'
  }

  const matched = url.match(/\.([a-zA-Z0-9]+)(?:\?|$)/)
  if (matched?.[1]) {
    return matched[1].toLowerCase()
  }

  return 'jpg'
}

const getOrCreateNestedDirectory = async (baseDir, paths) => {
  let current = baseDir
  for (const segment of paths) {
    current = await current.getDirectoryHandle(segment, { create: true })
  }
  return current
}

const boundsFromLayer = (layer) => {
  if (!layer || typeof layer.getBounds !== 'function') {
    return null
  }
  return normalizeBounds(layer.getBounds())
}

export const estimateTileCountByBounds = (
  boundsInput,
  minZoom = MIN_TILE_ZOOM,
  maxZoom = MAX_TILE_ZOOM,
) => {
  const bounds = normalizeBounds(boundsInput)
  if (!bounds) {
    return 0
  }

  const [startZoom, endZoom] = normalizeZoomRange(minZoom, maxZoom)
  let total = 0

  for (let zoom = startZoom; zoom <= endZoom; zoom += 1) {
    const range = getTileRange(bounds, zoom)
    total += (range.maxX - range.minX + 1) * (range.maxY - range.minY + 1)
  }

  return total
}

export const downloadTilesByBounds = async ({
  bounds: boundsInput,
  minZoom = MIN_TILE_ZOOM,
  maxZoom = MAX_TILE_ZOOM,
  tileUrl = GAODE_SATELLITE_TILE_URL,
  subdomains = DEFAULT_SUBDOMAINS,
  folderName = 'maptile_gaode_satellite',
  onProgress,
}) => {
  const bounds = normalizeBounds(boundsInput)
  if (!bounds) {
    throw new Error('Please complete the area selection first.')
  }

  if (typeof window.showDirectoryPicker !== 'function') {
    throw new Error(
      'The current browser does not support writing to local folders. Please use the latest version of Edge/Chrome.',
    )
  }

  const [startZoom, endZoom] = normalizeZoomRange(minZoom, maxZoom)
  const tasks = []

  for (let zoom = startZoom; zoom <= endZoom; zoom += 1) {
    const range = getTileRange(bounds, zoom)
    for (let x = range.minX; x <= range.maxX; x += 1) {
      for (let y = range.minY; y <= range.maxY; y += 1) {
        tasks.push({ z: zoom, x, y })
      }
    }
  }

  if (tasks.length === 0) {
    throw new Error('There are no downloadable tiles in the selected area.')
  }

  const rootDir = await window.showDirectoryPicker({ mode: 'readwrite' })
  const tileRootDir = await rootDir.getDirectoryHandle(folderName, { create: true })

  let done = 0
  let success = 0
  let failed = 0

  for (const task of tasks) {
    const { z, x, y } = task
    const url = resolveTileUrl(tileUrl, z, x, y, subdomains)

    try {
      const response = await fetch(url)
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }

      const blob = await response.blob()
      const ext = inferFileExt(response.headers.get('content-type'), url)
      const dir = await getOrCreateNestedDirectory(tileRootDir, [String(z), String(x)])
      const fileHandle = await dir.getFileHandle(`${y}.${ext}`, { create: true })
      const writable = await fileHandle.createWritable()
      await writable.write(blob)
      await writable.close()

      success += 1
    } catch (error) {
      failed += 1
      console.warn(`瓦片下载失败 z=${z} x=${x} y=${y}`, error)
    } finally {
      done += 1
      if (typeof onProgress === 'function') {
        onProgress({
          total: tasks.length,
          done,
          success,
          failed,
          current: `z${z}/x${x}/y${y}`,
        })
      }
    }
  }

  return {
    total: tasks.length,
    success,
    failed,
    folderName,
    zoomRange: [startZoom, endZoom],
  }
}

export const createAutoMap = (container, options = {}) => {
  if (!container) {
    return null
  }

  const map = L.map(container, {
    center: options.center || DEFAULT_CENTER,
    zoom: options.zoom || DEFAULT_ZOOM,
    attributionControl: false,
    zoomControl: true,
  })

  const tileLayer = L.tileLayer(normalizeTileUrl(options.tileUrl), {
    subdomains: ['1', '2', '3', '4'],
    maxZoom: 20,
    ...options.tileLayerOptions,
  }).addTo(map)

  map.__autoMapTileLayer = tileLayer

  map.pm.addControls({
    position: options.geomanPosition || 'topleft',
    drawMarker: false,
    drawCircleMarker: false,
    drawPolyline: false,
    drawRectangle: true,
    drawPolygon: false,
    drawCircle: false,
    drawText: false,
    editMode: false,
    dragMode: false,
    cutPolygon: false,
    removalMode: true,
    rotateMode: false,
  })

  let selectionLayer = null

  map.on('pm:create', (event) => {
    const { shape, layer } = event
    if (shape !== 'Rectangle' || !layer) {
      return
    }

    if (selectionLayer && selectionLayer !== layer) {
      map.removeLayer(selectionLayer)
    }

    selectionLayer = layer

    if (typeof options.onRectangleSelected === 'function') {
      options.onRectangleSelected(boundsFromLayer(layer))
    }
  })

  return map
}

export const updateAutoMapTileUrl = (mapInstance, tileUrl) => {
  if (!mapInstance?.__autoMapTileLayer) {
    return
  }

  mapInstance.__autoMapTileLayer.setUrl(normalizeTileUrl(tileUrl))
}

export const destroyAutoMap = (mapInstance) => {
  if (mapInstance) {
    mapInstance.remove()
  }
}
