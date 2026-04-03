import request from "@/utils/request.js";

export function listCamera(query) {
  return request({
    url: "api/camera/list",
    method: "get",
    params: query,
  });
}

export function addCamera(data) {
  return request({
    url: "api/camera",
    method: "post",
    data: data,
  });
}

export function updateCamera(data) {
  return request({
    url: "api/camera",
    method: "PUT",
    data: data,
  });
}

export function getCamera(id) {
  return request({
    url: "api/camera/" + id,
    method: "get",
  });
}

export function delCamera(pid) {
  return request({
    url: "api/camera/delete/" + pid,
    method: "delete",
  });
}

export function getCameraByDefenceAreaId(defenceAreaId) {
  return request({
    url: "api/camera/dcamera/" + defenceAreaId,
    method: "get",
  });
}

export function getCameraRepetitionJudgmentAdd(data) {
  return request({
    url: "api/camera/rjadd",
    method: "post",
    data: data,
  });
}
export function getCameraRepetitionJudgmentEdit(data) {
  return request({
    url: "api/camera/rjedit",
    method: "post",
    data: data,
  });
}

export function getMinZoomPTEdit(id, ip) {
  return request({
    url: "api/camera/min/" + id + "/" + ip,
    method: "get",
  });
}
export function getMaxZoomPTEdit(id, ip) {
  return request({
    url: "api/camera/max/" + id + "/" + ip,
    method: "get",
  });
}

export function probeUnicast(data) {
  return request({
    url: "api/camera/probeUnicast",
    method: "post",
    data: data,
  });
}
