const LOCAL_PLAYER_WS_URL = 'ws://127.0.0.1:10088'
const RECONNECT_DELAY = 10000
let winIdSequence = 0

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

          try {
            const msg = buildSetCommonInfoMessage({ language: 1 })
            socket.send(JSON.stringify(msg))
          } catch {
            // ignore if builder not available or send fails
          }

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

    const closeSocket = () => {
      this.disconnect()
    }

    window.addEventListener('beforeunload', closeSocket)
    window.addEventListener('pagehide', closeSocket)
    this.boundBeforeUnload = true
  }
}

const localPlayerSocketClient = new LocalPlayerSocketClient()

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
  } = options

  return {
    winId: winId || generateWinId(),
    left,
    top,
    width,
    height,
    playType,
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

function normalizeSetCommonInfoItem(options = {}) {
  const {
    winId,
    cmsUrl = '',
    recordPath = '',
    picPath = '',
    language = 1,
    playPerform = 1,
  } = options

  const item = {
    cmsUrl,
    recordPath,
    picPath,
    language,
    playPerform,
  }

  if (winId) {
    item.winId = winId
  }

  return item
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

export function buildSetCommonInfoMessage(windowOptions) {
  const windows = normalizeWindowOptions(windowOptions)

  return {
    cmd: 'setCommonInfo',
    data: windows.map((item) => normalizeSetCommonInfoItem(item)),
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
  const windows = normalizeCreateWinItems(windowOptions)
  const message = {
    cmd: 'createWin',
    data: windows,
  }

  const ok = await sendLocalPlayerMessage(message)

  try {
    const items = windows.map((item) => {
      return {
        winId: item.winId,
        cmsUrl: '',
        recordPath: '',
        picPath: '',
        language: 1,
        playPerform: 1,
      }
    })

    await sendSetCommonInfoMessage(items)
  } catch {
    // ignore failures for the follow-up command
  }

  return ok
}

export async function sendUpdateWinPosMessage(windowOptions) {
  const message = buildUpdateWinPosMessage(windowOptions)
  return sendLocalPlayerMessage(message)
}

export async function sendDeleteWinMessage(windowOptions) {
  const message = buildDeleteWinMessage(windowOptions)
  return sendLocalPlayerMessage(message)
}

export async function sendSetCommonInfoMessage(windowOptions) {
  const message = buildSetCommonInfoMessage(windowOptions)
  return sendLocalPlayerMessage(message)
}

export { LOCAL_PLAYER_WS_URL, RECONNECT_DELAY }
