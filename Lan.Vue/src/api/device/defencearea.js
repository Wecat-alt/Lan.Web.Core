import request from '../../../src/utils/request.js'

export function listDefencearea(query) {
  return request({
    url: 'api/defencearea/list',
    method: 'get',
    params: query,
  })
}

export function addDefencearea(data) {
  return request({
    url: 'api/defencearea',
    method: 'post',
    data: data,
  })
}

export function updateDefencearea(data) {
  return request({
    url: 'api/defencearea',
    method: 'PUT',
    data: data,
  })
}

export function getDefencearea(id) {
  return request({
    url: 'api/defencearea/' + id,
    method: 'get'
  })
}

export function delDefencearea(pid) {
  return request({
    url: 'api/defencearea/delete/' + pid,
    method: 'delete'
  })
}
export function enableDefencearea(status) {
  return request({
    url: 'api/defencearea/enable/' + status,
    method: 'PUT'
  })
}

export function allDefencearea() {
  return request({
    url: 'api/defencearea/all',
    method: 'get',
  })
}

export function treeselect() {
  return request({
    url: 'api/defencearea/treeselect',
    method: 'get'
  })
}