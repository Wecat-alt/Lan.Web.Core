import * as signalR from '@microsoft/signalr'

import { reactive, readonly } from 'vue'

let sharedConnection = null
let startPromise = null
let currentApi = ''
let listenerSeed = 0

const listeners = new Map()
const registeredAcceptMsgs = new Set()

const signalRState = reactive({
  connected: false,
  reconnecting: false,
  receiveEnabled: false,
  lastAcceptMessage: null,
  lastTrackTargetData: null,
  lastUpdatedAt: 0,
  error: null,
})

/**
 * 分发普通接收消息到所有订阅者。
 * @param {string} acceptMsg - 消息名称。
 * @param {any} payload - 服务端推送的数据。
 */
function dispatchAcceptMessage(acceptMsg, payload) {
  if (!signalRState.receiveEnabled) {
    return
  }

  signalRState.lastAcceptMessage = {
    acceptMsg,
    payload,
    timestamp: Date.now(),
  }
  signalRState.lastUpdatedAt = Date.now()

  listeners.forEach((listener) => {
    if (listener.acceptMsg === acceptMsg && typeof listener.onAcceptMessage === 'function') {
      listener.onAcceptMessage(payload)
    }
  })
}

/**
 * 分发 TrackTargetData 消息到所有订阅者。
 * @param {any} payload - 服务端推送的数据。
 */
function dispatchTrackTargetData(payload) {
  if (!signalRState.receiveEnabled) {
    return
  }

  signalRState.lastTrackTargetData = {
    payload,
    timestamp: Date.now(),
  }
  signalRState.lastUpdatedAt = Date.now()

  listeners.forEach((listener) => {
    if (typeof listener.onTrackTargetData === 'function') {
      listener.onTrackTargetData(payload)
    }
  })
}

/**
 * 确保指定消息已绑定到当前共享连接。
 * @param {string} messageName - 需要监听的消息名。
 */
function ensureAcceptHandler(messageName) {
  if (!sharedConnection || registeredAcceptMsgs.has(messageName)) {
    return
  }

  sharedConnection.on(messageName, (payload) => {
    dispatchAcceptMessage(messageName, payload)
  })
  registeredAcceptMsgs.add(messageName)
}

/**
 * 绑定连接生命周期回调，并同步更新全局状态。
 * @param {Object} options - 生命周期回调配置。
 * @param {(connectionId:string)=>void} [options.onReconnected] - 重连成功回调。
 * @param {(error:any)=>void} [options.onReconnecting] - 正在重连回调。
 * @param {(error:any)=>void} [options.onClose] - 连接关闭回调。
 */
function bindLifecycleHandlers({ onReconnected, onReconnecting, onClose } = {}) {
  if (!sharedConnection) {
    return
  }

  sharedConnection.onreconnected((connectionId) => {
    signalRState.connected = true
    signalRState.reconnecting = false
    signalRState.error = null
    if (typeof onReconnected === 'function') {
      onReconnected(connectionId)
    }
  })

  sharedConnection.onreconnecting((error) => {
    signalRState.connected = false
    signalRState.reconnecting = true
    signalRState.error = error || null
    if (typeof onReconnecting === 'function') {
      onReconnecting(error)
    }
  })

  sharedConnection.onclose((error) => {
    signalRState.connected = false
    signalRState.reconnecting = false
    signalRState.error = error || null
    if (typeof onClose === 'function') {
      onClose(error)
    }
  })
}

/**
 * 设置全局是否允许分发 SignalR 数据。
 * @param {boolean} enabled - 是否启用数据接收。
 */
export function setSignalRReceiveEnabled(enabled) {
  signalRState.receiveEnabled = Boolean(enabled)
}

/**
 * 获取只读的 SignalR 全局状态。
 * @returns {Readonly<Record<string, any>>} 只读状态对象。
 */
export function useSignalRState() {
  return readonly(signalRState)
}

/**
 * 订阅共享 SignalR 连接的数据分发。
 * @param {Object} options - 订阅配置。
 * @param {string} [options.acceptMsg='ReceiveTargetData'] - 普通消息名。
 * @param {(payload:any)=>void} [options.onAcceptMessage] - 普通消息回调。
 * @param {(payload:any)=>void} [options.onTrackTargetData] - TrackTargetData 回调。
 * @returns {()=>void} 取消订阅函数。
 */
export function subscribeSignalR({
  acceptMsg = 'ReceiveTargetData',
  onAcceptMessage,
  onTrackTargetData,
} = {}) {
  const id = ++listenerSeed
  listeners.set(id, {
    acceptMsg,
    onAcceptMessage,
    onTrackTargetData,
  })

  ensureAcceptHandler(acceptMsg)

  return () => {
    listeners.delete(id)
  }
}

/**
 * 确保共享 SignalR 连接已创建并处于可用状态。
 * @param {Object} options - 连接配置。
 * @param {string} [options.api] - Hub 地址。
 * @param {(connectionId:string)=>void} [options.onReconnected] - 重连成功回调。
 * @param {(error:any)=>void} [options.onReconnecting] - 正在重连回调。
 * @param {(error:any)=>void} [options.onClose] - 连接关闭回调。
 * @returns {Promise<signalR.HubConnection>} 共享连接实例。
 */
export async function ensureSignalRConnection({
  api,
  onReconnected,
  onReconnecting,
  onClose,
} = {}) {
  if (!api && !sharedConnection) {
    throw new Error('SignalR api is required')
  }

  if (sharedConnection && currentApi && api && currentApi !== api) {
    throw new Error('SignalR connection already initialized with different api')
  }

  if (sharedConnection && sharedConnection.state === signalR.HubConnectionState.Connected) {
    return sharedConnection
  }

  if (!sharedConnection) {
    currentApi = api
    sharedConnection = new signalR.HubConnectionBuilder()
      .withUrl(api, {})
      .withAutomaticReconnect([1000, 4000, 1000, 4000])
      .configureLogging(signalR.LogLevel.Error)
      .build()

    bindLifecycleHandlers({ onReconnected, onReconnecting, onClose })

    sharedConnection.on('TrackTargetData', (payload) => {
      dispatchTrackTargetData(payload)
    })
  }

  if (
    sharedConnection.state === signalR.HubConnectionState.Connecting ||
    sharedConnection.state === signalR.HubConnectionState.Reconnecting
  ) {
    return sharedConnection
  }

  if (!startPromise) {
    startPromise = sharedConnection
      .start()
      .then(() => {
        signalRState.connected = true
        signalRState.reconnecting = false
        signalRState.error = null

        listeners.forEach((listener) => {
          ensureAcceptHandler(listener.acceptMsg)
        })

        return sharedConnection
      })
      .finally(() => {
        startPromise = null
      })
  }

  await startPromise
  return sharedConnection
}

/**
 * 主动停止共享 SignalR 连接。
 * @returns {Promise<void>}
 */
export async function stopSignalRConnection() {
  if (!sharedConnection) {
    return
  }

  await sharedConnection.stop()
  signalRState.connected = false
  signalRState.reconnecting = false
}

/**
 * 通用 SignalR 初始化
 * @param {Object} options
 * @param {string} options.api - Hub 地址
 * @param {string} [options.acceptMsg='ReceiveTargetData'] - 主接收消息名
 * @param {string} [options.sendMsg='ReceiveTargetData'] - 发送/日志标识消息名
 * @param {(payload:any)=>void} [options.onAcceptMessage] - 主消息回调
 * @param {(payload:any)=>void} [options.onTrackTargetData] - TrackTargetData 回调
 * @param {(connectionId:string)=>void} [options.onReconnected]
 * @param {(error:any)=>void} [options.onReconnecting]
 * @param {(error:any)=>void} [options.onClose]
 * @returns {Promise<{connection: signalR.HubConnection, unsubscribe: ()=>void}>}
 */
export async function initSignalR({
  api,
  acceptMsg = 'ReceiveTargetData',
  sendMsg = 'ReceiveTargetData',
  onAcceptMessage,
  onTrackTargetData,
  onReconnected,
  onReconnecting,
  onClose,
}) {
  if (sendMsg) {
    // 兼容历史参数，保留调用端传参行为
  }

  const connection = await ensureSignalRConnection({
    api,
    onReconnected,
    onReconnecting,
    onClose,
  })

  const unsubscribe = subscribeSignalR({
    acceptMsg,
    onAcceptMessage,
    onTrackTargetData,
  })

  return {
    connection,
    unsubscribe,
  }
}
