import { onBeforeUnmount, onMounted, ref } from 'vue'

const FOREGROUND_SYNC_DELAY = 120

export function isDocumentActuallyForeground() {
  if (typeof document === 'undefined') {
    return true
  }

  const hasFocus = typeof document.hasFocus === 'function' ? document.hasFocus() : true
  return document.visibilityState === 'visible' && !document.hidden && hasFocus
}

function getDefaultPageForegroundState() {
  return isDocumentActuallyForeground()
}

async function runSafely(task, taskName) {
  if (typeof task !== 'function') {
    return
  }

  try {
    await task()
  } catch (error) {
    console.error(`[usePreviewVisibility] ${taskName} 执行失败`, error)
  }
}

/**
 * 统一管理预览窗口的前后台状态。
 *
 * 设计目标：
 * 1. 浏览器获得焦点时，通知调用方恢复预览；
 * 2. 浏览器失焦、切标签页、页面隐藏或 pagehide 时，通知调用方主动关闭远端窗口；
 * 3. 事件回调内部即使抛错，也不会把异常继续抛到浏览器事件栈里。
 */
export function usePreviewVisibility(options = {}) {
  const { onForeground, onBackground } = options
  const isPageForeground = ref(getDefaultPageForegroundState())
  let foregroundTimer = null

  async function syncPreviewVisibility(forceForeground) {
    const nextForegroundState =
      typeof forceForeground === 'boolean' ? forceForeground : getDefaultPageForegroundState()

    isPageForeground.value = nextForegroundState

    if (nextForegroundState) {
      await runSafely(onForeground, 'onForeground')
      return
    }

    await runSafely(onBackground, 'onBackground')
  }

  function clearForegroundTimer() {
    if (!foregroundTimer) {
      return
    }

    window.clearTimeout(foregroundTimer)
    foregroundTimer = null
  }

  function scheduleForegroundSync() {
    clearForegroundTimer()
    foregroundTimer = window.setTimeout(() => {
      foregroundTimer = null
      void syncPreviewVisibility()
    }, FOREGROUND_SYNC_DELAY)
  }

  function handleWindowFocus() {
    scheduleForegroundSync()
  }

  function handleWindowBlur() {
    clearForegroundTimer()
    void syncPreviewVisibility(false)
  }

  function handleVisibilityChange() {
    if (document.visibilityState === 'visible') {
      if (!isDocumentActuallyForeground()) {
        clearForegroundTimer()
        void syncPreviewVisibility(false)
        return
      }

      scheduleForegroundSync()
      return
    }

    clearForegroundTimer()
    void syncPreviewVisibility(false)
  }

  function handlePageHide() {
    clearForegroundTimer()
    void syncPreviewVisibility(false)
  }

  onMounted(() => {
    window.addEventListener('focus', handleWindowFocus)
    window.addEventListener('blur', handleWindowBlur)
    window.addEventListener('pagehide', handlePageHide)
    document.addEventListener('visibilitychange', handleVisibilityChange)
  })

  onBeforeUnmount(() => {
    clearForegroundTimer()
    window.removeEventListener('focus', handleWindowFocus)
    window.removeEventListener('blur', handleWindowBlur)
    window.removeEventListener('pagehide', handlePageHide)
    document.removeEventListener('visibilitychange', handleVisibilityChange)
  })

  return {
    isPageForeground,
    syncPreviewVisibility,
  }
}
