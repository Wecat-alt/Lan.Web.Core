// 官方文档：https://docs.microsoft.com/zh-cn/aspnet/core/signalr/javascript-client?view=aspnetcore-6.0&viewFallbackFrom=aspnetcore-2.2&tabs=visual-studio
import * as signalR from "@microsoft/signalr";
import { ElMessage } from 'element-plus'
import analysis from '@/signalr/analysis'

export default {
  // signalR对象
  SR: {},
  // 失败连接重试次数
  failNum: 4,
  init(url) {
    console.log(url) 
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(url, {})
          .withAutomaticReconnect([1000, 4000, 1000, 4000])// 断线自动重连
          .configureLogging(signalR.LogLevel.Error)
          .build()
        //自动重连成功后的处理
        connection.onreconnected((connectionId) => {
          console.log(connectionId, '自动重新连接成功')
        })
        // 开始
        if (connection.state !== signalR.HubConnectionState.Connected) {
          connection.start().then((res) => {
            console.log('启动即时通信成功')
            // connection.invoke()
          })
        }
        // 生命周期
        connection.onreconnecting(error => {
          console.log(acceptMsg, +'**', sendMsg, '重新连接ing', error)
          console.log(1)
          console.log(connection.state)
          console.log(connection.state === signalR.HubConnectionState.Reconnecting)
        })
        // (默认4次重连)，任何一次只要回调成功，调用
        connection.onreconnected(connectionId => {
          console.log('链接id', connectionId)
          console.log(2)
          console.log(connection.state)
          console.log(connection.state === signalR.HubConnectionState.Connected)
          if (connection.state === signalR.HubConnectionState.Connected) {
            console.log(acceptMsg, +'**', sendMsg, '重连')
            // connection.invoke()
          }
          // init()
        })
        connection.onclose(error => {
          console.log('关闭', error)
        })

    analysis.onMessage(connection)
    // 启动
    // this.start();
  },
  /**
   * 调用 this.signalR.start().then(async () => { await this.SR.invoke("method")})
   * @returns
   */
  async start() {
    try {
      console.log('signalR-1', this.SR.state)
      //使用async和await 或 promise的then 和catch 处理来自服务端的异常
      if (this.SR.state === signalR.HubConnectionState.Disconnected) {
        await this.SR.start()
      }

      console.log('signalR-2', this.SR.state)
      return true
    } catch (error) {
      console.error(error)
      this.failNum--
      // console.log(`失败重试剩余次数${that.failNum}`, error)
      if (this.failNum > 0 && this.SR.state.Disconnected) {
        setTimeout(async () => {
          await this.start()
        }, 5000)
      }
      return false
    }
  }
}
