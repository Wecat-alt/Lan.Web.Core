import { getToken } from '@/utils/auth'
import axios from 'axios'

axios.defaults.headers['Content-Type'] = 'application/json;charset=utf-8'

const runtime = window.__APP_CONFIG__ || {}
// const baseURL =
//   runtime.VITE_APP_API_BASE_URL || import.meta.env.VITE_APP_API_BASE_URL;

const baseURL = runtime.VITE_APP_API_BASE_URL
console.log('API 请求地址：', baseURL)
// 创建实例
const request = axios.create({
  baseURL,
  timeout: 15000,
})

// 请求拦截器：自动附带 Authorization
request.interceptors.request.use(
  (config) => {
    try {
      const token = getToken()
      if (token) {
        config.headers = config.headers || {}
        config.headers['Authorization'] = `Bearer ${token}`
      }
    } catch (e) {
      // ignore
    }
    return config
  },
  (error) => Promise.reject(error),
)

// 响应拦截器：统一错误处理（保留原 response，交由调用方判断 code）
request.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('网络请求错误：', error)
    return Promise.reject(error)
  },
)

export default request
