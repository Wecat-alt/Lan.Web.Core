import signalR from '@/signalr/signalr'
const useSocketStore = defineStore('socket', {
  persist: {
    paths: ['chatMessage', 'chatList', 'sessionList', 'newChat', 'noticeIdArr', 'newNotice', 'globalErrorMsg'] //存储指定key
  },
  state: () => ({
    onlineNum: 0,
    onlineUsers: [],
    noticeList: [],
    //在线用户信息
    onlineInfo: {},
    //目标小心
    targetInfo: {},
    // 聊天数据
    chatList: {},
    leaveUser: {},
    sessionList: {},
    newChat: 0,
    newNotice: 0,
    noticeIdArr: [],
    // 全局错误提醒
    globalErrorMsg: {}
  }),
  // getters: {
  //   /**
  //    * 返回当前会话的消息
  //    * @param {*} state
  //    * @returns
  //    */
  //   getMessageList(state) {
  //     return (conversationId) => state.chatList[conversationId]
  //   },
  //   getSessionList(state) {
  //     return (userid) => state.sessionList[userid] || []
  //   },
  //   getAllDotNum(state) {
  //     return () => state.newChat + state.newNotice
  //   }
  // },
  actions: {
  
    setGlobalError(data) {
      this.globalErrorMsg = data
    },
    getTarget(data) {
      this.targetInfo = data
    }
  }
})
export default useSocketStore
