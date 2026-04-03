import request from '../../../src/utils/request.js'

export function listCalibration(query) {
  return request({
    url: '/api/calibration',
    method: 'get',
    params: query,
  })
}
export function addCalibration(data) {
  return request({
    url: 'api/calibration',
    method: 'post',
    data: data,
  })
}

// export function updateCalibration(data) {
//     return request({
//         url: 'api/calibration',
//         method: 'PUT',
//         data: data,
//     })
// }
export function updateCalibration(a, b, c, d, e, f) {
  return request({
    url: 'api/calibration/up/' + a + '/' + b + '/' + c + '/' + d + '/' + e + '/' + f,
    method: 'get',
  })
}

export function delCalibration(pid) {
  return request({
    url: 'api/device/calibration/delete/' + pid,
    method: 'delete',
  })
}

export function setCalibrationTrack(id, istrack) {
  return request({
    url: 'api/calibration/set/' + id + '/' + istrack,
    method: 'get',
  })
}

export function getCalibrationBy(CameraIp, ZoneId) {
  return request({
    url: 'api/calibration/msg/' + CameraIp + '/' + ZoneId,
    method: 'get',
  })
}
