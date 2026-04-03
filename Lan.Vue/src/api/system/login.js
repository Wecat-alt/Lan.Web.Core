import request from '../../../src/utils/request.js'

// 登录方法
export function login(username, password) {
  const data = {
    username,
    password
  }
  return request({
    url: 'api/login',
    method: 'POST',
    data: data,
    headers: {
      userName: username
    }
  })
}

// 获取用户详细信息
export function getInfo() {
  return request({
    url: '/getInfo',
    method: 'get'
  })
}

// 退出方法
export function logout() {
  return request({
    url: '/LogOut',
    method: 'POST'
  })
}

// 获取验证码
export function getCodeImg() {
  return request({
    url: '/captchaImage',
    method: 'get'
  })
}
/**
 * 注册
 * @returns
 */
export function register(data) {
  return request({
    url: '/register',
    method: 'post',
    data: data
  })
}

/**
 * 三方授权回调
 * @param {*} data
 * @param {*} params
 * @returns
 */
export function oauthCallback(data, params) {
  return request({
    url: '/auth/callback',
    method: 'post',
    data: data,
    params: params
  })
}


// 登录方法
export function phoneLogin(data) {
  return request({
    url: '/phoneLogin',
    method: 'POST',
    data: data
  })
}
