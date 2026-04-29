const LOCAL_PLAYER_WS_URL = 'ws://127.0.0.1:10088'
const RECONNECT_DELAY = 10000
let winIdSequence = 0
const CREATE_WIN_GUARD_DELAY = 120

let foregroundGuardBound = false
let lastFocusAt = 0
let lastBlurAt = 0
let lastHiddenAt = 0

function ensureForegroundGuard() {
  if (foregroundGuardBound) {
    return
  }

  if (typeof window === 'undefined' || typeof document === 'undefined') {
    foregroundGuardBound = true
    return
  }

  window.addEventListener('focus', () => {
    lastFocusAt = Date.now()
  })

  window.addEventListener('blur', () => {
    lastBlurAt = Date.now()
  })

  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden' || document.hidden) {
      lastHiddenAt = Date.now()
    }
  })

  foregroundGuardBound = true
}

function canSendCreateWinNow() {
  if (typeof document === 'undefined') {
    return true
  }

  const visible = document.visibilityState === 'visible' && !document.hidden
  const hasFocus = typeof document.hasFocus === 'function' ? document.hasFocus() : true
  if (!visible || !hasFocus) {
    return false
  }

  const latestBackgroundAt = Math.max(lastBlurAt, lastHiddenAt)
  const hasFreshFocusSignal =
    lastFocusAt > latestBackgroundAt || (latestBackgroundAt === 0 && hasFocus)

  if (!hasFreshFocusSignal) {
    return false
  }

  const now = Date.now()
  if (lastFocusAt > 0 && now - lastFocusAt < CREATE_WIN_GUARD_DELAY) {
    return false
  }

  return true
}

function generateWinId() {
  winIdSequence += 1
  return `alarmVideo-${Date.now()}-${winIdSequence}`
}

class LocalPlayerSocketClient {
  constructor(url = LOCAL_PLAYER_WS_URL) {
    this.url = url
    this.socket = null
    this.connectPromise = null
    this.reconnectTimer = null
    this.shouldReconnect = false
    this.manuallyClosed = false
    this.pendingMessages = []
    this.boundBeforeUnload = false
  }

  isOpen() {
    return this.socket && this.socket.readyState === WebSocket.OPEN
  }

  isConnecting() {
    return this.socket && this.socket.readyState === WebSocket.CONNECTING
  }

  async connect() {
    this.shouldReconnect = true
    this.manuallyClosed = false

    if (this.isOpen()) {
      return this.socket
    }

    if (this.connectPromise) {
      return this.connectPromise
    }

    this.clearReconnectTimer()

    this.connectPromise = new Promise((resolve, reject) => {
      try {
        const socket = new WebSocket(this.url)
        this.socket = socket

        socket.onopen = () => {
          this.connectPromise = null
          this.flushPendingMessages()

          resolve(socket)
        }

        socket.onmessage = () => {}

        socket.onerror = (error) => {
          if (this.connectPromise) {
            this.connectPromise = null
            reject(error)
          }
        }

        socket.onclose = () => {
          this.socket = null
          this.connectPromise = null

          if (!this.manuallyClosed && this.shouldReconnect) {
            this.scheduleReconnect()
          }
        }
      } catch (error) {
        this.connectPromise = null
        reject(error)
      }
    })

    return this.connectPromise
  }

  scheduleReconnect() {
    if (this.reconnectTimer || !this.shouldReconnect) {
      return
    }

    this.reconnectTimer = window.setTimeout(() => {
      this.reconnectTimer = null
      this.connect().catch(() => {})
    }, RECONNECT_DELAY)
  }

  clearReconnectTimer() {
    if (this.reconnectTimer) {
      window.clearTimeout(this.reconnectTimer)
      this.reconnectTimer = null
    }
  }

  flushPendingMessages() {
    if (!this.isOpen() || this.pendingMessages.length === 0) {
      return
    }

    const messages = [...this.pendingMessages]
    this.pendingMessages = []
    messages.forEach((message) => {
      this.socket.send(message)
    })
  }

  async send(payload) {
    const message = typeof payload === 'string' ? payload : JSON.stringify(payload)

    if (this.isOpen()) {
      this.socket.send(message)
      return true
    }

    this.pendingMessages.push(message)

    try {
      await this.connect()
      return true
    } catch {
      return false
    }
  }

  sendSync(payload) {
    const message = typeof payload === 'string' ? payload : JSON.stringify(payload)
    const readyState = this.socket?.readyState

    if (!this.isOpen()) {
      console.warn('[LocalPlayerSocket] sendSync skipped: socket not open', {
        readyState,
        message: message.slice(0, 240),
      })
      return false
    }

    try {
      this.socket.send(message)
      console.warn('[LocalPlayerSocket] sendSync success', {
        readyState,
        message: message.slice(0, 240),
      })
      return true
    } catch {
      console.warn('[LocalPlayerSocket] sendSync failed by exception', {
        readyState,
        message: message.slice(0, 240),
      })
      return false
    }
  }

  disconnect() {
    this.shouldReconnect = false
    this.manuallyClosed = true
    this.clearReconnectTimer()
    this.pendingMessages = []

    if (this.socket) {
      this.socket.close()
      this.socket = null
    }

    this.connectPromise = null
  }

  bindBeforeUnload() {
    if (this.boundBeforeUnload) {
      return
    }

    const closeSocket = (event) => {
      console.warn('[LocalPlayerSocket] mark socket no-reconnect on unload event', {
        eventType: event?.type,
        readyState: this.socket?.readyState,
      })

      // 关闭页面时如果立刻 disconnect，会导致其它 beforeunload/pagehide 回调
      // 中的 deleteWin 指令来不及发送。
      // 这里只关闭重连能力，交给业务层在退出流程里先发 deleteWin。
      this.shouldReconnect = false
      this.manuallyClosed = true
      this.clearReconnectTimer()
    }

    window.addEventListener('beforeunload', closeSocket)
    window.addEventListener('pagehide', closeSocket)
    this.boundBeforeUnload = true
  }
}

const localPlayerSocketClient = new LocalPlayerSocketClient()
ensureForegroundGuard()

function normalizeWindowOptions(windowOptions) {
  return Array.isArray(windowOptions) ? windowOptions : [windowOptions]
}

function ensureRequiredWinId(options, commandName) {
  if (typeof options === 'string' && options) {
    return options
  }

  if (options?.winId) {
    return options.winId
  }

  throw new Error(`winId is required for ${commandName}`)
}

function normalizeCreateWinItem(options = {}) {
  const {
    winId,
    left = 911,
    top = 540,
    width = 550,
    height = 360,
    playType = 0,
    deviceName = '',
    rtspUrl = '',
    fileName = '',
    winType = 1,
    deviceId = '0',
    channelId = 0,
    username = '',
    password = '',
    crossVisible = 0,
    crossSwitch = 1,
    ptzShow = '0',
    recordShow = '0',
    playbackShow = '0',
    renderShow = '0',
    captureShow = '0',
    enableAutoStreamSwitch = '0',
    language = 1,
  } = options

  return {
    winId: winId || generateWinId(),
    left,
    top,
    width,
    height,
    playType,
    deviceName,
    rtspUrl,
    fileName,
    winType,
    deviceId,
    channelId,
    username,
    password,
    crossVisible,
    crossSwitch,
    ptzShow,
    recordShow,
    playbackShow,
    renderShow,
    captureShow,
    enableAutoStreamSwitch,
    language,
  }
}

function normalizeUpdateWinPosItem(options = {}) {
  const { left = 0, top = 0, width = 550, height = 360 } = options

  return {
    winId: ensureRequiredWinId(options, 'updateWinPos'),
    left,
    top,
    width,
    height,
  }
}

function normalizeDeleteWinItem(options = {}) {
  return {
    winId: ensureRequiredWinId(options, 'deleteWin'),
  }
}

function normalizeCreateWinItems(windowOptions) {
  return normalizeWindowOptions(windowOptions).map((item) => normalizeCreateWinItem(item))
}

export function buildCreateWinMessage(windowOptions) {
  const windows = normalizeCreateWinItems(windowOptions)

  return {
    cmd: 'createWin',
    data: windows,
  }
}

export function buildUpdateWinPosMessage(windowOptions) {
  const windows = normalizeWindowOptions(windowOptions)

  return {
    cmd: 'updateWinPos',
    data: windows.map((item) => normalizeUpdateWinPosItem(item)),
  }
}

export function buildDeleteWinMessage(windowOptions) {
  const windows = normalizeWindowOptions(windowOptions)

  return {
    cmd: 'deleteWin',
    data: windows.map((item) => normalizeDeleteWinItem(item)),
  }
}

export function initializeLocalPlayerSocket() {
  localPlayerSocketClient.bindBeforeUnload()
  return localPlayerSocketClient.connect()
}

export function closeLocalPlayerSocket() {
  localPlayerSocketClient.disconnect()
}

export async function sendLocalPlayerMessage(payload) {
  return localPlayerSocketClient.send(payload)
}

export async function sendCreateWinMessage(windowOptions) {
  if (!canSendCreateWinNow()) {
    return false
  }

  const windows = normalizeCreateWinItems(windowOptions)
  const message = {
    cmd: 'createWin',
    data: windows,
  }

  const ok = await sendLocalPlayerMessage(message)
  return ok
}

export async function sendUpdateWinPosMessage(windowOptions) {
  const message = buildUpdateWinPosMessage(windowOptions)
  return sendLocalPlayerMessage(message)
}

export async function sendDeleteWinMessage(windowOptions) {
  console.log('sendDeleteWinMessage called with options', windowOptions)
  const message = buildDeleteWinMessage(windowOptions)
  return sendLocalPlayerMessage(message)
}

export function sendDeleteWinMessageSync(windowOptions) {
  console.log('sendDeleteWinMessageSync called with options', windowOptions)
  const message = buildDeleteWinMessage(windowOptions)
  return localPlayerSocketClient.sendSync(message)
}

export { LOCAL_PLAYER_WS_URL, RECONNECT_DELAY }
