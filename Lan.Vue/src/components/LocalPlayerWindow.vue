<template>
  <div v-if="modelValue && currentWinId && isPageForeground" class="local-player-mask">
    <div ref="panelRef" class="local-player-window" :style="panelStyle" @mousedown="bringToFront">
      <div class="local-player-header" @mousedown="startDrag">
        <span class="local-player-title">{{ title }}</span>
        <button class="local-player-close" type="button" @click.stop="closePanel">×</button>
      </div>
      <div
        class="resize-handle resize-handle-top"
        @mousedown.stop="startResize($event, 'top')"
      ></div>
      <div
        class="resize-handle resize-handle-top-left"
        @mousedown.stop="startResize($event, 'top-left')"
      ></div>
      <div
        class="resize-handle resize-handle-top-right"
        @mousedown.stop="startResize($event, 'top-right')"
      ></div>
      <div
        class="resize-handle resize-handle-right"
        @mousedown.stop="startResize($event, 'right')"
      ></div>
      <div
        class="resize-handle resize-handle-bottom"
        @mousedown.stop="startResize($event, 'bottom')"
      ></div>
      <div
        class="resize-handle resize-handle-bottom-left"
        @mousedown.stop="startResize($event, 'bottom-left')"
      ></div>
      <div
        class="resize-handle resize-handle-bottom-right"
        @mousedown.stop="startResize($event, 'bottom-right')"
      ></div>
      <div
        class="resize-handle resize-handle-left"
        @mousedown.stop="startResize($event, 'left')"
      ></div>
    </div>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'

import {
  isDocumentActuallyForeground,
  usePreviewVisibility,
} from '@/composables/usePreviewVisibility'
import {
  sendCreateWinMessage,
  sendDeleteWinMessage,
  sendUpdateWinPosMessage,
} from '@/utils/localPlayerSocket'

defineOptions({
  name: 'LocalPlayerWindow',
})

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  title: {
    type: String,
    default: '视频预览',
  },
  winOptions: {
    type: Object,
    default: () => ({}),
  },
  initialRect: {
    type: Object,
    default: () => ({
      left: 24,
      top: 120,
      width: 500,
      height: 360,
    }),
  },
})

const emit = defineEmits(['update:modelValue', 'closed'])

const HEADER_HEIGHT = 32
const SHELL_PADDING = 6
const BODY_TOP_GAP = 2
const MIN_CONTENT_WIDTH = 320
const MIN_CONTENT_HEIGHT = 220
const VIEWPORT_GAP = 0

const panelRef = ref(null)
const zIndex = ref(3000)
const activeWinId = ref('')
const position = ref({
  left: props.initialRect.left ?? 0,
  top: props.initialRect.top ?? 0,
  width: 550,
  height: 360,
})

const dragState = {
  dragging: false,
  startX: 0,
  startY: 0,
  originLeft: 0,
  originTop: 0,
}

const resizeState = {
  resizing: false,
  edge: '',
  startX: 0,
  startY: 0,
  originLeft: 0,
  originTop: 0,
  originWidth: 0,
  originHeight: 0,
}

let syncTimer = null
let rafId = null

const currentWinId = computed(() => props.winOptions?.winId || activeWinId.value || '')
const shellWidth = computed(() => position.value.width + SHELL_PADDING * 2)
const shellHeight = computed(
  () => position.value.height + HEADER_HEIGHT + SHELL_PADDING * 2 + BODY_TOP_GAP,
)

const panelStyle = computed(() => ({
  left: `${position.value.left}px`,
  top: `${position.value.top}px`,
  width: `${shellWidth.value}px`,
  height: `${shellHeight.value}px`,
  zIndex: zIndex.value,
}))

function getWindowChromeOffset() {
  const horizontalOffset = Math.max((window.outerWidth - window.innerWidth) / 2, 0)
  const verticalOffset = Math.max(window.outerHeight - window.innerHeight - horizontalOffset, 0)

  return {
    left: horizontalOffset,
    top: verticalOffset,
  }
}

function getShellSize(width, height) {
  return {
    width: width + SHELL_PADDING * 2,
    height: height + HEADER_HEIGHT + SHELL_PADDING * 2 + BODY_TOP_GAP,
  }
}

function buildScreenRect() {
  const chromeOffset = getWindowChromeOffset()
  const screenLeft = typeof window.screenX === 'number' ? window.screenX : window.screenLeft || 0
  const screenTop = typeof window.screenY === 'number' ? window.screenY : window.screenTop || 0

  return {
    winId: currentWinId.value,
    left: Math.round(screenLeft + chromeOffset.left + position.value.left + SHELL_PADDING),
    top: Math.round(
      screenTop +
        chromeOffset.top +
        position.value.top +
        HEADER_HEIGHT +
        SHELL_PADDING +
        BODY_TOP_GAP,
    ),
    width: Math.round(position.value.width),
    height: Math.round(position.value.height),
  }
}

function clampPosition(nextLeft, nextTop) {
  const maxLeft = Math.max(window.innerWidth - shellWidth.value - VIEWPORT_GAP, 0)
  const maxTop = Math.max(window.innerHeight - shellHeight.value - VIEWPORT_GAP, 0)

  return {
    left: Math.min(Math.max(nextLeft, 0), maxLeft),
    top: Math.min(Math.max(nextTop, 0), maxTop),
  }
}

function clampContentWidth(width, left) {
  const maxWidth = Math.max(
    window.innerWidth - left - VIEWPORT_GAP - SHELL_PADDING * 2,
    MIN_CONTENT_WIDTH,
  )
  return Math.min(Math.max(width, MIN_CONTENT_WIDTH), maxWidth)
}

function clampContentHeight(height, top) {
  const maxHeight = Math.max(
    window.innerHeight - top - VIEWPORT_GAP - HEADER_HEIGHT - SHELL_PADDING * 2 - BODY_TOP_GAP,
    MIN_CONTENT_HEIGHT,
  )
  return Math.min(Math.max(height, MIN_CONTENT_HEIGHT), maxHeight)
}

function bringToFront() {
  zIndex.value += 1
}

async function createOrRefreshWindow() {
  if (
    !props.modelValue ||
    !currentWinId.value ||
    !isPageForeground.value ||
    !isDocumentActuallyForeground()
  ) {
    return
  }

  if (activeWinId.value && activeWinId.value !== currentWinId.value) {
    await sendDeleteWinMessage({ winId: activeWinId.value })
  }

  const payload = {
    ...props.winOptions,
    ...buildScreenRect(),
  }

  await sendCreateWinMessage(payload)
  activeWinId.value = currentWinId.value
}

function scheduleSyncPosition() {
  if (!props.modelValue || !currentWinId.value) {
    return
  }

  if (syncTimer) {
    return
  }

  syncTimer = window.setTimeout(() => {
    syncTimer = null
    sendUpdateWinPosMessage(buildScreenRect())
  }, 80)
}

function handleDrag(event) {
  if (resizeState.resizing) {
    return
  }

  if (!dragState.dragging) {
    return
  }

  const deltaX = event.clientX - dragState.startX
  const deltaY = event.clientY - dragState.startY
  const nextPosition = clampPosition(dragState.originLeft + deltaX, dragState.originTop + deltaY)

  position.value = {
    ...position.value,
    left: nextPosition.left,
    top: nextPosition.top,
  }

  scheduleSyncPosition()
}

function stopDrag() {
  if (!dragState.dragging) {
    return
  }

  dragState.dragging = false
  window.removeEventListener('mousemove', handleDrag)
  window.removeEventListener('mouseup', stopDrag)
  sendUpdateWinPosMessage(buildScreenRect())
}

function handleResize(event) {
  if (!resizeState.resizing) {
    return
  }

  const deltaX = event.clientX - resizeState.startX
  const deltaY = event.clientY - resizeState.startY
  const originShell = getShellSize(resizeState.originWidth, resizeState.originHeight)
  const isLeft = resizeState.edge.includes('left')
  const isRight = resizeState.edge.includes('right')
  const isTop = resizeState.edge.includes('top')
  const isBottom = resizeState.edge.includes('bottom')

  if (isRight) {
    const nextWidth = clampContentWidth(resizeState.originWidth + deltaX, resizeState.originLeft)

    position.value = {
      ...position.value,
      width: nextWidth,
    }
  }

  if (isBottom) {
    const nextHeight = clampContentHeight(resizeState.originHeight + deltaY, resizeState.originTop)

    position.value = {
      ...position.value,
      height: nextHeight,
    }
  }

  if (isLeft) {
    const originRight = resizeState.originLeft + originShell.width
    const nextWidth = Math.min(
      Math.max(resizeState.originWidth - deltaX, MIN_CONTENT_WIDTH),
      Math.max(originRight - SHELL_PADDING * 2, MIN_CONTENT_WIDTH),
    )
    const nextShell = getShellSize(nextWidth, resizeState.originHeight)
    const nextLeft = Math.max(originRight - nextShell.width, 0)

    position.value = {
      ...position.value,
      left: nextLeft,
      width: nextWidth,
    }
  }

  if (isTop) {
    const originBottom = resizeState.originTop + originShell.height
    const nextHeight = Math.min(
      Math.max(resizeState.originHeight - deltaY, MIN_CONTENT_HEIGHT),
      Math.max(originBottom - HEADER_HEIGHT - SHELL_PADDING * 2 - BODY_TOP_GAP, MIN_CONTENT_HEIGHT),
    )
    const nextShell = getShellSize(resizeState.originWidth, nextHeight)
    const nextTop = Math.max(originBottom - nextShell.height, 0)

    position.value = {
      ...position.value,
      top: nextTop,
      height: nextHeight,
    }
  }

  scheduleSyncPosition()
}

function stopResize() {
  if (!resizeState.resizing) {
    return
  }

  resizeState.resizing = false
  resizeState.edge = ''
  window.removeEventListener('mousemove', handleResize)
  window.removeEventListener('mouseup', stopResize)
  sendUpdateWinPosMessage(buildScreenRect())
}

function startDrag(event) {
  if (resizeState.resizing) {
    return
  }

  dragState.dragging = true
  dragState.startX = event.clientX
  dragState.startY = event.clientY
  dragState.originLeft = position.value.left
  dragState.originTop = position.value.top

  bringToFront()
  window.addEventListener('mousemove', handleDrag)
  window.addEventListener('mouseup', stopDrag)
}

function startResize(event, edge) {
  if (dragState.dragging) {
    return
  }

  resizeState.resizing = true
  resizeState.edge = edge
  resizeState.startX = event.clientX
  resizeState.startY = event.clientY
  resizeState.originLeft = position.value.left
  resizeState.originTop = position.value.top
  resizeState.originWidth = position.value.width
  resizeState.originHeight = position.value.height

  bringToFront()
  window.addEventListener('mousemove', handleResize)
  window.addEventListener('mouseup', stopResize)
}

async function closeRemoteWindow() {
  if (!activeWinId.value) {
    return
  }

  await sendDeleteWinMessage({ winId: activeWinId.value })
  activeWinId.value = ''
}

async function closePanel() {
  emit('update:modelValue', false)
  emit('closed')
}

function resetPosition() {
  position.value = {
    left: props.initialRect.left ?? 0,
    top: props.initialRect.top ?? 0,
    width: props.initialRect.width ?? 550,
    height: props.initialRect.height ?? 360,
  }
}

const { isPageForeground, syncPreviewVisibility } = usePreviewVisibility({
  // 页面回到前台后，仅在手动预览处于显示状态时恢复远端窗口。
  onForeground: async () => {
    if (!props.modelValue || !isDocumentActuallyForeground()) {
      return
    }

    await nextTick()
    await createOrRefreshWindow()
  },
  // 页面失焦或隐藏时立即关闭远端窗口，确保本地与远端状态一致。
  onBackground: async () => {
    await closeRemoteWindow()
  },
})

watch(
  () => props.modelValue,
  async (visible) => {
    if (visible) {
      resetPosition()
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

function handleWindowResize() {
  if (!props.modelValue || !isPageForeground.value) {
    return
  }

  const nextPosition = clampPosition(position.value.left, position.value.top)
  position.value = {
    ...position.value,
    left: nextPosition.left,
    top: nextPosition.top,
  }

  if (rafId) {
    window.cancelAnimationFrame(rafId)
  }

  rafId = window.requestAnimationFrame(() => {
    sendUpdateWinPosMessage(buildScreenRect())
  })
}

window.addEventListener('resize', handleWindowResize)

onBeforeUnmount(async () => {
  if (syncTimer) {
    window.clearTimeout(syncTimer)
  }

  if (rafId) {
    window.cancelAnimationFrame(rafId)
  }

  stopDrag()
  stopResize()
  window.removeEventListener('resize', handleWindowResize)
  await closeRemoteWindow()
})
</script>

<style scoped>
.local-player-mask {
  position: fixed;
  inset: 0;
  background: radial-gradient(ellipse at top right, rgba(64, 158, 255, 0.05), transparent 65%);
  pointer-events: none;
  z-index: 3000;
}

.local-player-window {
  position: fixed;
  background: linear-gradient(180deg, rgba(14, 24, 40, 0.97) 0%, rgba(9, 18, 33, 0.96) 100%);
  border: 1px solid rgba(64, 158, 255, 0.18);
  border-radius: 12px;
  box-shadow:
    0 4px 24px rgba(0, 0, 0, 0.35),
    0 0 0 1px rgba(64, 158, 255, 0.06),
    inset 0 1px 0 rgba(255, 255, 255, 0.04);
  backdrop-filter: blur(16px);
  overflow: visible;
  pointer-events: auto;
  user-select: none;
  transition: box-shadow 0.25s ease, border-color 0.25s ease;
}

.local-player-window:hover {
  border-color: rgba(64, 158, 255, 0.28);
  box-shadow:
    0 8px 32px rgba(0, 0, 0, 0.4),
    0 0 60px rgba(64, 158, 255, 0.08),
    0 0 0 1px rgba(64, 158, 255, 0.1),
    inset 0 1px 0 rgba(255, 255, 255, 0.05);
}

/* ── Header ── */
.local-player-header {
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 10px 0 14px;
  background: linear-gradient(135deg, rgba(25, 90, 185, 0.92) 0%, rgba(16, 55, 125, 0.95) 100%);
  border-bottom: 1px solid rgba(140, 200, 255, 0.15);
  cursor: move;
}

.local-player-title {
  position: relative;
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.3px;
  color: #e8f2ff;
  padding-left: 14px;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}

.local-player-title::before {
  content: '';
  position: absolute;
  left: 0;
  top: 50%;
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #6dd4a8;
  box-shadow: 0 0 8px rgba(109, 212, 168, 0.7);
  transform: translateY(-50%);
  animation: player-pulse 2s ease-in-out infinite;
}

@keyframes player-pulse {
  0%, 100% { opacity: 1; box-shadow: 0 0 8px rgba(109, 212, 168, 0.7); }
  50% { opacity: 0.55; box-shadow: 0 0 3px rgba(109, 212, 168, 0.3); }
}

.local-player-close {
  width: 24px;
  height: 24px;
  border: none;
  border-radius: 50%;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.85);
  background: rgba(255, 255, 255, 0.1);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.22s ease;
}

.local-player-close:hover {
  background: rgba(245, 80, 80, 0.88);
  color: #fff;
  transform: rotate(90deg);
  box-shadow: 0 4px 14px rgba(200, 60, 60, 0.35);
}

.local-player-close:focus-visible {
  outline: 2px solid rgba(180, 220, 255, 0.9);
  outline-offset: 1px;
}

/* ── Resize Handles ── */
.resize-handle {
  position: absolute;
  z-index: 3;
}

.resize-handle-top,
.resize-handle-bottom {
  left: 18px;
  right: 18px;
  height: 10px;
}

.resize-handle-top { top: -5px; cursor: ns-resize; }
.resize-handle-bottom { bottom: -5px; cursor: ns-resize; }

.resize-handle-left,
.resize-handle-right {
  top: 18px;
  bottom: 18px;
  width: 10px;
}

.resize-handle-left { left: -5px; cursor: ew-resize; }
.resize-handle-right { right: -5px; cursor: ew-resize; }

.resize-handle-top-left,
.resize-handle-top-right,
.resize-handle-bottom-left,
.resize-handle-bottom-right {
  width: 16px;
  height: 16px;
  z-index: 4;
}

.resize-handle-top-left { top: -7px; left: -7px; cursor: nwse-resize; }
.resize-handle-top-right { top: -7px; right: -7px; cursor: nesw-resize; }
.resize-handle-bottom-left { bottom: -7px; left: -7px; cursor: nesw-resize; }
.resize-handle-bottom-right { bottom: -7px; right: -7px; cursor: nwse-resize; }

/* 四角可见拖拽指示块 */
.resize-handle-top-left::after,
.resize-handle-top-right::after,
.resize-handle-bottom-left::after,
.resize-handle-bottom-right::after {
  content: '';
  position: absolute;
  width: 7px;
  height: 7px;
  border-radius: 2px;
  background: rgba(64, 158, 255, 0.35);
  transition: background 0.2s ease;
}

.resize-handle-top-left::after { bottom: 2px; right: 2px; }
.resize-handle-top-right::after { bottom: 2px; left: 2px; }
.resize-handle-bottom-left::after { top: 2px; right: 2px; }
.resize-handle-bottom-right::after { top: 2px; left: 2px; }

.local-player-window:hover .resize-handle-top-left::after,
.local-player-window:hover .resize-handle-top-right::after,
.local-player-window:hover .resize-handle-bottom-left::after,
.local-player-window:hover .resize-handle-bottom-right::after {
  background: rgba(64, 158, 255, 0.6);
}
</style>
