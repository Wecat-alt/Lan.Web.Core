import request from '../../../src/utils/request.js'

export function listRadar(query) {
    return request({
        url: '/api/radar/list',
        method: 'get',
        params: query
    })
}
export function addRadar(data) {
    return request({
        url: 'api/radar',
        method: 'post',
        data: data,
    })
}

export function updateRadar(data) {
    return request({
        url: '/api/radar',
        method: 'PUT',
        data: data,
    })
}

export function updateLatLng(ip,lat,lng) {
	return request({
		url: 'api/radar/setLatLng/' + ip+'/'+lat+'/'+lng,
		method: 'get'
	})
}

export function getRadar(id) {
    return request({
        url: '/api/radar/' + id,
        method: 'get'
    })
}
  
export function delRadar(pid) {
    return request({
      url: '/api/radar/delete/' + pid,
      method: 'delete'
    })
}

export function listRadarByAreaId(id) {
    return request({
        url: '/api/radar/listby/'+id,
        method: 'get'
    })
}

export function getRadarRepetitionJudgmentAdd(data) {
  return request({
    url: 'api/radar/rjadd',
    method: 'post',
    data: data,
  })
}
export function getRadarRepetitionJudgmentEdit(data) {
  return request({
    url: 'api/radar/rjedit',
    method: 'post',
    data: data,
  })
}