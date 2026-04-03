import request from '@/utils/request.js'

export function listAlarm(query) {
    return request({
        url: '/api/alarm/list',
        method: 'get',
        params: query
    })
}
export function listAlarmRef(query) {
    return request({
        url: '/api/alarm/listref',
        method: 'get',
        params: query
    })
}

export function getAlarm(id) {
    return request({
        url: '/api/alarm/' + id,
        method: 'get'
    })
}

export function updateAlarm(pid) {
  return request({
    url: 'api/alarm/update/' + pid,
    method: 'delete'
  })
}
export function updateAllAlarm(query) {
    return request({
        url: '/api/alarm/list1',
        method: 'get',
        params: query
    })
}
