function normalizeAlarmPayload(payload) {
  if (!payload || typeof payload !== 'object') {
    return null
  }

  const cameraIp = payload.CameraIp || payload.cameraIp || ''
  const username = payload.Username || payload.username || ''
  const password = payload.Password || payload.password || ''
  const cameraURL = payload.CameraURL || payload.cameraURL || ''

  if (!cameraURL) {
    return null
  }

  return {
    cameraIp,
    username,
    password,
    cameraURL,
  }
}

export function createAlarmPopupQueue(options = {}) {
  const duration = Number(options.duration || 15000)
  const onOpen = typeof options.onOpen === 'function' ? options.onOpen : null
  const onClose = typeof options.onClose === 'function' ? options.onClose : null
  const onStateChange =
    typeof options.onStateChange === 'function' ? options.onStateChange : () => {}
  const isPreviewVisible =
    typeof options.isPreviewVisible === 'function' ? options.isPreviewVisible : () => false

  const queue = []
  let currentItem = null
  let closeTimer = null
  let destroyed = false
  let playedCount = 0

  function emitState() {
    onStateChange({
      active: !!currentItem,
      currentIndex: currentItem ? playedCount : 0,
      remainingCount: queue.length,
      totalCount: queue.length + (currentItem ? 1 : 0),
      currentItem,
    })
  }

  function clearCloseTimer() {
    if (closeTimer) {
      clearTimeout(closeTimer)
      closeTimer = null
    }
  }

  function scheduleAutoClose() {
    clearCloseTimer()
    closeTimer = setTimeout(() => {
      closeTimer = null
      closeCurrentAndContinue()
    }, duration)
  }

  function showNextIfIdle() {
    if (destroyed || currentItem || isPreviewVisible() || queue.length === 0 || !onOpen) {
      return
    }

    currentItem = queue.shift()
    playedCount += 1
    emitState()
    onOpen(currentItem)
    scheduleAutoClose()
  }

  function enqueue(rawMessage) {
    if (destroyed) {
      return false
    }

    let parsedMessage = rawMessage
    if (typeof rawMessage === 'string') {
      try {
        parsedMessage = JSON.parse(rawMessage)
      } catch (error) {
        console.error('AlarmPopup 消息解析失败:', error, rawMessage)
        return false
      }
    }

    const normalizedMessage = normalizeAlarmPayload(parsedMessage)
    if (!normalizedMessage) {
      return false
    }

    queue.push(normalizedMessage)
    emitState()
    showNextIfIdle()
    return true
  }

  function closeCurrentAndContinue() {
    if (destroyed) {
      return
    }

    clearCloseTimer()

    if (currentItem) {
      currentItem = null
      emitState()
      if (onClose) {
        onClose()
      }
    }

    queueMicrotask(() => {
      showNextIfIdle()
    })
  }

  function notifyClosed() {
    clearCloseTimer()

    if (currentItem) {
      currentItem = null
      emitState()
    }

    queueMicrotask(() => {
      showNextIfIdle()
    })
  }

  function resume() {
    queueMicrotask(() => {
      showNextIfIdle()
    })
  }

  function destroy() {
    destroyed = true
    clearCloseTimer()
    queue.length = 0
    currentItem = null
    playedCount = 0
    emitState()
  }

  function getQueueLength() {
    return queue.length + (currentItem ? 1 : 0)
  }

  return {
    enqueue,
    notifyClosed,
    resume,
    destroy,
    getQueueLength,
  }
}
