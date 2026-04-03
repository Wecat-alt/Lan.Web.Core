import request from '../../../src/utils/request.js'

export function getTrackParameter(id) {
  return request({
    url: 'api/trackparameter/' + id,
    method: 'get'
  })
}