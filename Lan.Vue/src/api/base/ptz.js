import request from '../../../src/utils/request.js'

export function sendPTZ(type, ips,speed) {
	return request({
		url: 'api/calibration/' + type + '/' + ips+ '/' + speed,
		method: 'get'
	})
}
export function sendStop(ips) {
	return request({
		url: 'api/calibration/stop/' + ips,
		method: 'get'
	})
}