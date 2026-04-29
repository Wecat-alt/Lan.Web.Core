<template>
  <div v-if="modelValue && currentWinId && isPageForeground" class="alarm-auto-player-mask">
    <div class="alarm-auto-player-window" :style="panelStyle">
      <div class="alarm-auto-player-header">
        <span class="alarm-auto-player-title">{{ displayTitle }}</span>
        <button class="alarm-auto-player-close" type="button" @click.stop="closePanel">×</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'

import {
  isDocumentActuallyForeground,
  usePreviewVisibility,
} from '@/composables/usePreviewVisibility'
import { sendCreateWinMessage, sendDeleteWinMessage } from '@/utils/localPlayerSocket'

defineOptions({
  name: 'AlarmAutoPlayerWindow',
})

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  title: {
    type: String,
    default: '报警视频预览',
  },
  winOptions: {
    type: Object,
    default: () => ({}),
  },
  currentIndex: {
    type: Number,
    default: 0,
  },
  remainingCount: {
    type: Number,
    default: 0,
  },
  showQueueInfo: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['update:modelValue', 'closed'])

const WINDOW_WIDTH = 420
const WINDOW_HEIGHT = 235
const HEADER_HEIGHT = 32
const PANEL_PADDING = 6
const BODY_TOP_GAP = 0
const RIGHT_GAP = 24
const BOTTOM_GAP = 24

const activeWinId = ref('')

let alarmWinSequence = 0

const currentWinId = computed(() => props.winOptions?.winId || activeWinId.value || '')

function extractHostFromUrl(url) {
  try {
    if (!url) return ''
    // try to use URL parsing for standard URLs (http/https/rtsp)
    // when URL constructor fails, fallback to regex
    try {
      const parsed = new URL(url.replace(/^rtsp:/i, 'http:'))
      return parsed.hostname || ''
    } catch {
      const m = url.match(/@?([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)|@?([a-z0-9.-]+)(?::|\/|$)/i)
      if (m) return m[1] || m[2] || ''
      return ''
    }
  } catch {
    return ''
  }
}

const displayTitle = computed(() => {
  const base = props.title || '报警视频预览'
  const opts = props.winOptions || {}
  const ipCandidates = [
    opts.cameraIp,
    opts.cameraIP,
    opts.ip,
    opts.cameraURL,
    opts.cameraUrl,
    opts.rtspUrl,
    opts.url,
  ]
  let host = ''
  for (const v of ipCandidates) {
    if (!v) continue
    host = extractHostFromUrl(v)
    if (host) break
  }

  return host ? `${base} · ${host}` : base
})

const panelStyle = computed(() => {
  const shellWidth = WINDOW_WIDTH + PANEL_PADDING * 2
  const shellHeight = WINDOW_HEIGHT + HEADER_HEIGHT + PANEL_PADDING * 2 + BODY_TOP_GAP

  return {
    left: `${Math.max(window.innerWidth - shellWidth - RIGHT_GAP, 0)}px`,
    top: `${Math.max(window.innerHeight - shellHeight - BOTTOM_GAP, 0)}px`,
    width: `${shellWidth}px`,
    height: `${shellHeight}px`,
  }
})

function createAlarmWinId(seed = '') {
  alarmWinSequence += 1
  const normalizedSeed = String(seed || Date.now())
  return `alarmPopupAuto-${normalizedSeed}-${alarmWinSequence}`
}

function getWindowChromeOffset() {
  const horizontalOffset = Math.max((window.outerWidth - window.innerWidth) / 2, 0)
  const verticalOffset = Math.max(window.outerHeight - window.innerHeight - horizontalOffset, 0)

  return {
    left: horizontalOffset,
    top: verticalOffset,
  }
}

function buildScreenRect() {
  const chromeOffset = getWindowChromeOffset()
  const screenLeft = typeof window.screenX === 'number' ? window.screenX : window.screenLeft || 0
  const screenTop = typeof window.screenY === 'number' ? window.screenY : window.screenTop || 0

  const shellWidth = WINDOW_WIDTH + PANEL_PADDING * 2
  const shellHeight = WINDOW_HEIGHT + HEADER_HEIGHT + PANEL_PADDING * 2 + BODY_TOP_GAP
  const shellLeft = Math.max(window.innerWidth - shellWidth - RIGHT_GAP, 0)
  const shellTop = Math.max(window.innerHeight - shellHeight - BOTTOM_GAP, 0)

  return {
    winId: currentWinId.value,
    left: Math.round(screenLeft + chromeOffset.left + shellLeft + PANEL_PADDING),
    top: Math.round(
      screenTop + chromeOffset.top + shellTop + HEADER_HEIGHT + PANEL_PADDING + BODY_TOP_GAP,
    ),
    width: WINDOW_WIDTH,
    height: WINDOW_HEIGHT,
  }
}

async function createOrRefreshWindow() {
  if (!props.modelValue || !isPageForeground.value || !isDocumentActuallyForeground()) {
    return
  }

  const winId = props.winOptions?.winId || createAlarmWinId(props.winOptions?.rtspUrl)
  const payload = {
    ...props.winOptions,
    ...buildScreenRect(),
    winId,
  }

  await sendCreateWinMessage(payload)
  activeWinId.value = winId
}

async function closeRemoteWindow() {
  const winId = currentWinId.value || activeWinId.value
  if (!winId) {
    return
  }

  await sendDeleteWinMessage({ winId })
  activeWinId.value = ''
}

const { isPageForeground, syncPreviewVisibility } = usePreviewVisibility({
  // 页面回到前台时，如果弹窗仍然处于显示状态，则重新创建/同步远端窗口。
  onForeground: async () => {
    if (!props.modelValue || !isDocumentActuallyForeground()) {
      return
    }

    await nextTick()
    await createOrRefreshWindow()
  },
  // 页面离开前台时，无条件发送关闭指令，避免远端窗口残留。
  onBackground: async () => {
    await closeRemoteWindow()
  },
})

async function closePanel() {
  emit('update:modelValue', false)
  emit('closed')
}

watch(
  () => props.modelValue,
  async (visible) => {
    if (visible) {
      await syncPreviewVisibility()
      return
    }

    await closeRemoteWindow()
  },
)

watch(
  () => props.winOptions,
  async () => {
    if (!props.modelValue || !isPageForeground.value) {
      return
    }

    await nextTick()
    await createOrRefreshWindow()
  },
  { deep: true },
)

onBeforeUnmount(async () => {
  await closeRemoteWindow()
})
</script>

<style scoped>
.alarm-auto-player-mask {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 12000;
}

.alarm-auto-player-window {
  position: fixed;
  border-radius: 10px;
  border: 1px solid rgba(64, 158, 255, 0.32);
  background: linear-gradient(180deg, rgba(16, 28, 45, 0.96) 0%, rgba(10, 20, 35, 0.94) 100%);
  box-shadow:
    0 20px 50px rgba(5, 16, 30, 0.42),
    inset 0 1px 0 rgba(255, 255, 255, 0.06),
    0 0 0 1px rgba(17, 87, 160, 0.08);
  backdrop-filter: blur(10px);
  pointer-events: auto;
}

.alarm-auto-player-header {
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 10px;
  color: #e8f3ff;
  border-radius: 10px 10px 0 0;
}

.alarm-auto-player-title {
  font-size: 13px;
  font-weight: 600;
}

.alarm-auto-player-close {
  border: none;
  background: transparent;
  color: #fff;
  font-size: 16px;
  line-height: 1;
  cursor: pointer;
}
</style>
