import request from '../../../src/utils/request.js'

export function listDrawPolygon() {
    return request({
        url: '/api/drawpolygon/list',
        method: 'get'
    })
}
export function addDrawPolygon(data) {
    return request({
        url: 'api/drawpolygon',
        method: 'post',
        data: data,
    })
}

export function updateDrawPolygon(data) {
    return request({
        url: '/api/drawpolygon',
        method: 'PUT',
        data: data,
    })
}

export function getDrawPolygon(id) {
    return request({
        url: '/api/drawpolygon/' + id,
        method: 'get'
    })
}
  
export function delDrawPolygon(pid) {
    return request({
      url: '/api/drawpolygon/delete',
      method: 'delete',
      data: pid,
    })
}

export function sendMsgOpen(id) {
    return request({
        url: '/api/commoninterface/open/' + id,
        method: 'get'
    })
}

export function sendMsgClose() {
    return request({
        url: '/api/commoninterface/close',
        method: 'get'
    })
}
