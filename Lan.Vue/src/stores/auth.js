import { login as apiLogin } from '@/api/system/login'
import { removeToken, setToken } from '@/utils/auth'
import { closeLocalPlayerSocket, initializeLocalPlayerSocket } from '@/utils/localPlayerSocket'
import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    isAuthenticated: localStorage.getItem('isAuthenticated') === 'true',
  }),
  actions: {
    async login(username, password) {
      try {
        const res = await apiLogin(username, password)
        const { code, data } = res.data || {}
        if (code == 200) {
          const token = data && (data.token || data.accessToken || data.Authorization)
          if (token) setToken(token)
          this.isAuthenticated = true
          localStorage.setItem('isAuthenticated', 'true')
          initializeLocalPlayerSocket().catch(() => {})
          return true
        }
        return false
      } catch (err) {
        return false
      }
    },
    logout() {
      closeLocalPlayerSocket()
      this.isAuthenticated = false
      localStorage.removeItem('isAuthenticated')
      removeToken()
    },
  },
})
