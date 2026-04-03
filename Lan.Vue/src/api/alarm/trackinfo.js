import request from '../../../src/utils/request.js'

export function listTrackInfo(query) {
    return request({
        url: '/api/trackinfo/list',
        method: 'get',
        params: query
    })
}