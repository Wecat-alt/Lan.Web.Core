import request from '../../../src/utils/request.js'
import { praseStrZero } from '@/utils/ruoyi'

// 查询用户列表
export function listUser(query) {
  return request({
    url: 'api/system/user/list',
    method: 'get',
    params: query
  })
}

// 查询用户详细
export function getUser(userId) {
  return request({
    url: 'api/system/user/' + praseStrZero(userId),
    method: 'get'
  })
}

// 新增用户
export function addUser(data) {
  return request({
    url: 'api/system/user',
    method: 'post',
    data: data
  })
}

// 修改用户
export function updateUser(data) {
  return request({
    url: 'api/system/user',
    method: 'put',
    data: data
  })
}

// 删除用户
export function delUser(userId) {
  return request({
    url: 'api/system/user/' + userId,
    method: 'delete'
  })
}
