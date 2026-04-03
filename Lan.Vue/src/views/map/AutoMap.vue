<template>
  <div class="auto-map-page">
    <div class="download-panel">
      <h3>{{ $t('autoMap.title') }}</h3>
      <p class="hint">{{ $t('autoMap.hint') }}</p>

      <div class="field-row">
        <label>{{ $t('autoMap.minZoom') }}</label>
        <input v-model.number="minZoom" type="number" min="1" max="18" />
      </div>
      <div class="field-row">
        <label>{{ $t('autoMap.maxZoom') }}</label>
        <input v-model.number="maxZoom" type="number" min="1" max="18" />
      </div>

      <div class="summary">
        <div>
          {{ $t('autoMap.rectangleStatus') }}：{{
            hasSelection ? $t('autoMap.rectangleSelected') : $t('autoMap.rectangleNotSelected')
          }}
        </div>
        <div>{{ $t('autoMap.estimatedTiles') }}：{{ estimatedCount }}</div>
      </div>

      <button class="download-btn" :disabled="!hasSelection || downloading" @click="handleDownload">
        {{ downloading ? $t('autoMap.downloading') : $t('autoMap.downloadToLocalFolder') }}
      </button>

      <div class="progress" v-if="progress.total > 0">
        <div>{{ $t('autoMap.progress') }}：{{ progress.done }}/{{ progress.total }}</div>
        <div>
          {{ $t('autoMap.success') }}：{{ progress.success }}，{{ $t('autoMap.failed') }}：{{
            progress.failed
          }}
        </div>
        <div>{{ $t('autoMap.current') }}：{{ progress.current }}</div>
      </div>

      <div class="status" v-if="statusMessage">{{ statusMessage }}</div>
    </div>

    <div class="tile-url-panel">
      <label class="tile-url-label" for="tile-url-input">{{ $t('autoMap.tileUrl') }}</label>
      <input
        id="tile-url-input"
        v-model.trim="tileUrlInput"
        class="tile-url-input"
        type="text"
        :title="tileUrlInput"
        @keyup.enter="applyTileUrl"
      />
    </div>

    <div ref="mapContainer" class="map-container"></div>
  </div>
</template>

<script setup>
import 'leaflet/dist/leaflet.css'
import { computed, getCurrentInstance, onBeforeUnmount, onMounted, ref } from 'vue'
import {
  createAutoMap,
  DEFAULT_TILE_URL,
  destroyAutoMap,
  downloadTilesByBounds,
  estimateTileCountByBounds,
  updateAutoMapTileUrl,
} from './AutoMap'
const { proxy } = getCurrentInstance()
const mapContainer = ref(null)
const selectedBounds = ref(null)
const minZoom = ref(1)
const maxZoom = ref(18)
const tileUrlInput = ref(DEFAULT_TILE_URL)
const currentTileUrl = ref(DEFAULT_TILE_URL)
const downloading = ref(false)
const statusMessage = ref('')
const progress = ref({
  total: 0,
  done: 0,
  success: 0,
  failed: 0,
  current: '-',
})

let mapInstance = null

const hasSelection = computed(() => Boolean(selectedBounds.value))
const estimatedCount = computed(() => {
  if (!selectedBounds.value) {
    return 0
  }
  return estimateTileCountByBounds(selectedBounds.value, minZoom.value, maxZoom.value)
})

const normalizeZoomValue = (value) => {
  const num = Number.parseInt(value, 10)
  if (Number.isNaN(num)) {
    return 1
  }
  return Math.max(1, Math.min(18, num))
}

const onRectangleSelected = (bounds) => {
  selectedBounds.value = bounds
  statusMessage.value = proxy.$t('autoMap.rectangleUpdated')
}

const applyTileUrl = () => {
  const nextTileUrl = tileUrlInput.value?.trim() || DEFAULT_TILE_URL
  tileUrlInput.value = nextTileUrl
  currentTileUrl.value = nextTileUrl
  updateAutoMapTileUrl(mapInstance, nextTileUrl)
  statusMessage.value = proxy.$t('autoMap.tileUrlUpdated')
}

const handleDownload = async () => {
  if (!selectedBounds.value || downloading.value) {
    return
  }

  const min = normalizeZoomValue(minZoom.value)
  const max = normalizeZoomValue(maxZoom.value)
  minZoom.value = Math.min(min, max)
  maxZoom.value = Math.max(min, max)

  downloading.value = true
  statusMessage.value = proxy.$t('autoMap.rectangleUpdated')
  progress.value = {
    total: 0,
    done: 0,
    success: 0,
    failed: 0,
    current: '-',
  }

  try {
    const result = await downloadTilesByBounds({
      bounds: selectedBounds.value,
      minZoom: minZoom.value,
      maxZoom: maxZoom.value,
      tileUrl: currentTileUrl.value,
      onProgress: (nextProgress) => {
        progress.value = nextProgress
      },
    })
    statusMessage.value = proxy.$t('autoMap.downloadCompleted', {
      success: result.success,
      failed: result.failed,
    })
  } catch (error) {
    statusMessage.value = proxy.$t('autoMap.downloadFailed', {
      message: error?.message || '未知错误',
    })
  } finally {
    downloading.value = false
  }
}

onMounted(() => {
  mapInstance = createAutoMap(mapContainer.value, {
    tileUrl: currentTileUrl.value,
    onRectangleSelected,
  })
})

onBeforeUnmount(() => {
  destroyAutoMap(mapInstance)
  mapInstance = null
})
</script>

<style scoped>
.auto-map-page {
  position: relative;
  width: 100%;
  height: 100%;
}

.tile-url-panel {
  position: absolute;
  z-index: 1000;
  top: 12px;
  left: 108px;
  width: 480px;
  max-width: calc(100vw - 420px);
  padding: 10px 12px;
  border-radius: 8px;
  background: rgba(24, 24, 24, 0.88);
  color: #fff;
}

.tile-url-label {
  display: block;
  margin-bottom: 6px;
  font-size: 13px;
}

.tile-url-input {
  width: 100%;
  padding: 6px 8px;
  border: 1px solid #6b6b6b;
  border-radius: 4px;
  background: #303030;
  color: #fff;
  font-size: 13px;
}

.download-panel {
  position: absolute;
  z-index: 1000;
  top: 12px;
  right: 12px;
  width: 280px;
  padding: 12px;
  border-radius: 8px;
  background: rgba(24, 24, 24, 0.9);
  color: #fff;
  font-size: 13px;
  line-height: 1.5;
}

.download-panel h3 {
  margin: 0 0 8px;
  font-size: 16px;
}

.hint {
  margin: 0 0 10px;
  color: #d7d7d7;
}

.field-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.field-row input {
  width: 88px;
  padding: 4px 6px;
  border: 1px solid #6b6b6b;
  border-radius: 4px;
  background: #303030;
  color: #fff;
}

.summary {
  margin-bottom: 10px;
}

.download-btn {
  width: 100%;
  margin-bottom: 10px;
  padding: 8px;
  border: none;
  border-radius: 6px;
  background: #409eff;
  color: #fff;
  cursor: pointer;
}

.download-btn:disabled {
  background: #7f8c8d;
  cursor: not-allowed;
}

.progress,
.status {
  color: #d5e9ff;
  word-break: break-all;
}

.map-container {
  width: 100%;
  height: 100vh;
  min-height: 500px;
}

@media (max-width: 960px) {
  .tile-url-panel {
    left: 12px;
    right: 12px;
    width: auto;
    max-width: none;
    top: auto;
    bottom: 12px;
  }
}
</style>
