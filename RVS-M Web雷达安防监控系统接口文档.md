# RVS-M Web 雷达安防监控系统 · 接口文档

> 版本：V1.0
> 适用系统：RVS-M Web 安防监控平台（Web API）
> 文档说明：本文档为系统对外提供的 HTTP 接口说明，供第三方系统集成及客户联调参考。

---

## 1. 通用说明

### 1.1 服务地址

| 环境 | 地址 |
|------|------|
| 生产/测试环境 | `http://<服务器IP>:8080` |

所有接口统一在域名/IP 后拼接路由，例如：

```
http://<服务器IP>:8080/api/radar/list
```

### 1.2 数据格式

- 请求体（Body）：`application/json`，JSON 属性名统一使用 **camelCase（首字母小写）**。
- 响应体：JSON 格式，字段名 camelCase。
- 时间格式：`yyyy-MM-dd HH:mm:ss`。
- 编码：UTF-8。

### 1.3 统一响应结构

所有接口返回统一信封结构：

```json
{
  "code": 200,
  "msg": "success",
  "data": { }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `code` | int | 状态码，见下表 |
| `msg` | string | 状态描述 |
| `data` | object | 业务数据；当无数据时可能不返回该字段 |

> 接口封装说明：查询类接口（`Message()` 封装）在 `data` 为 `null` 时返回 `code=210`；写入类接口（`ToResponse()` 封装）操作影响行数 `>0` 时返回 `code=200`，否则返回 `code=1`（操作失败）。

### 1.4 状态码说明

| code | 名称 | 说明 |
|------|------|------|
| 200 | SUCCESS | 成功 |
| 210 | NO_DATA | 没有更多数据 |
| 1 | FAIL | 操作失败 |
| 101 | PARAM_ERROR | 参数错误 |
| 102 | DATA_REPEAT | 数据重复（新增时 IP/名称 已存在） |
| 103 | CAPTCHA_ERROR | 验证码错误 |
| 104 | RepetitionJudgment | 数据已存在（重复判断命中） |
| 105 | LOGIN_ERROR | 登录错误 |
| 110 | CUSTOM_ERROR | 自定义异常 |
| 116 | INVALID_REQUEST | 非法请求 |
| 201 | OAUTH_FAIL | 授权失败 |
| 400 | BAD_REQUEST | Bad Request |
| 401 | DENY | 未授权 |
| 403 | FORBIDDEN | 授权访问失败 |
| 500 | GLOBAL_ERROR | 服务端出错 |

### 1.5 分页参数（公共）

分页查询接口通用以下参数：

| 参数 | 类型 | 必填 | 默认 | 说明 |
|------|------|------|------|------|
| `pageNum` | int | 否 | 1 | 当前页码 |
| `pageSize` | int | 否 | 20 | 每页条数 |
| `sort` | string | 否 | 空 | 排序字段名 |
| `sortType` | string | 否 | 空 | 排序方式：`ascending` / `descending` |
| `totalNum` | int | 否 | - | 总记录数（响应返回） |

分页响应 `data` 结构：

```json
{
  "pageNum": 1,
  "pageSize": 20,
  "totalNum": 105,
  "result": [ ]
}
```

---

## 2. 系统配置

基础路由：`api/config`

### 2.1 配置列表

- **URL：** `GET /api/config/list`
- **说明：** 分页查询系统配置。

**请求参数（Query）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `configName` | string | 否 | 配置名称 |
| `configKey` | string | 否 | 配置键 |
| `pageNum` / `pageSize` | - | 否 | 公共分页参数 |

### 2.2 按 Key 查询配置值

- **URL：** `GET /api/config/configKey/{configKey}`
- **说明：** 按配置键查询配置值，无需登录。常用于前端读取 `mapCenter`、`mapZoom` 等运行参数。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `configKey` | string | 是 | 配置键 |

**响应示例（data 为配置值字符串）：**

```json
{
  "code": 200,
  "msg": "success",
  "data": "113.256730,23.123400"
}
```

### 2.3 配置详情

- **URL：** `GET /api/config/{configId}`
- **说明：** 按配置 ID 查询。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `configId` | int | 是 | 配置 ID |

### 2.4 新增配置

- **URL：** `POST /api/config`

**请求参数（Body）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `configName` | string | 是 | 配置名称 |
| `configKey` | string | 是 | 配置键 |
| `configValue` | string | 是 | 配置值 |
| `configType` | string | 否 | 配置类型 |

### 2.5 修改配置

- **URL：** `PUT /api/config`
- **说明：** 修改配置。若修改的是服务端缓存的运行配置项（如地图中心、缩放等），会同步刷新缓存。

**请求参数（Body）：** 同「2.4 新增配置」，需携带 `configId`。

### 2.6 删除配置

- **URL：** `DELETE /api/config/{ids}`
- **说明：** 按 ID 删除，支持逗号分隔批量删除。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | string | 是 | 配置 ID，多个以英文逗号分隔，如 `1,2,3` |

---

## 3. 雷达设备

基础路由：`api/radar`

### 3.1 雷达列表

- **URL：** `GET /api/radar/list`
- **说明：** 分页查询雷达设备。

**请求参数（Query）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ip` | string | 否 | 按雷达 IP 查询 |
| `pageNum` / `pageSize` | - | 否 | 公共分页参数 |

### 3.2 全部雷达

- **URL：** `GET /api/radar/all`
- **说明：** 查询所有雷达设备（不分页）。

### 3.3 雷达详情

- **URL：** `GET /api/radar/{id}`
- **说明：** 按雷达 ID 查询。

### 3.4 新增雷达

- **URL：** `POST /api/radar`
- **说明：** 新增雷达设备。若 IP 已存在返回 `code=102`。

**请求参数（Body）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ip` | string | 是 | 雷达 IP 地址 |
| `port` | int | 否 | 通信端口（默认 50000） |
| `bindingAreaId` | int | 否 | 绑定防区 ID |
| `status` | int | 否 | 状态（0/1） |
| `latitude` | string | 否 | 纬度 |
| `longitude` | string | 否 | 经度 |
| `northDeviationAngle` | string | 否 | 正北偏角 |
| `defenceRadius` | int | 否 | 防区半径 |
| `defenceAngle` | int | 否 | 防区角度 |
| `radarType` | string | 否 | 雷达型号（如 NSR100W、SUC261） |
| `defenceEnable` | int | 否 | 防区是否启用 |
| `cameraIp` | string | 否 | 联动摄像头 IP |
| `username` | string | 否 | 摄像头用户名 |
| `password` | string | 否 | 摄像头密码 |
| `cameraUrl` | string | 否 | 摄像头流地址 |

**请求示例：**

```json
{
  "ip": "192.168.1.101",
  "port": 50000,
  "defenceRadius": 500,
  "defenceAngle": 90,
  "radarType": "NSR100W"
}
```

### 3.5 修改雷达

- **URL：** `PUT /api/radar`
- **说明：** 修改雷达设备，Body 同「3.4」，需携带 `id`。

### 3.6 设置雷达经纬度

- **URL：** `GET /api/radar/setLatLng/{Ip}/{Lat}/{Lng}`
- **说明：** 直接设置雷达安装经纬度（用于标定联动），成功返回 `data="OK"`。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Ip` | string | 是 | 雷达 IP |
| `Lat` | string | 是 | 纬度 |
| `Lng` | string | 是 | 经度 |

### 3.7 删除雷达

- **URL：** `DELETE /api/radar/delete/{ids}`
- **说明：** 按 ID 删除，支持逗号分隔批量删除。

### 3.8 按防区查询雷达

- **URL：** `GET /api/radar/listby/{AreaId}`
- **说明：** 按防区 ID 查询该防区绑定的雷达列表。

### 3.9 防区雷达重复判断（新增）

- **URL：** `POST /api/radar/rjadd`
- **说明：** 新增防区时校验所选雷达是否已绑定其他防区。若命中重复返回 `code=104` 及重复提示。

**请求参数（Body）：**

```json
{ "radarIds": [1, 2, 3] }
```

### 3.10 防区雷达重复判断（修改）

- **URL：** `POST /api/radar/rjedit`

**请求参数（Body）：**

```json
{ "bindingAreaId": 2, "radarIds": [1, 2, 3] }
```

---

## 4. 摄像头

基础路由：`api/camera`

### 4.1 摄像头列表

- **URL：** `GET /api/camera/list`
- **说明：** 分页查询摄像头。

**请求参数（Query）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ip` | string | 否 | 按摄像头 IP 查询 |
| `defenceareaId` | int | 否 | 按防区 ID 查询 |
| `pageNum` / `pageSize` | - | 否 | 公共分页参数 |

### 4.2 摄像头预览列表

- **URL：** `GET /api/camera/preview`
- **说明：** 查询所有摄像头预览信息（含流地址）。

### 4.3 摄像头详情

- **URL：** `GET /api/camera/{id}`
- **说明：** 按摄像头 ID 查询。

### 4.4 新增摄像头

- **URL：** `POST /api/camera`
- **说明：** 新增摄像头。系统会通过 ONVIF 协议自动探测摄像头能力并获取 RTSP 流地址；若获取失败返回 `code=101`，若 IP 已存在返回 `code=102`。

**请求参数（Body）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | 否 | 摄像头名称 |
| `ip` | string | 是 | 摄像头 IP |
| `port` | int | 否 | ONVIF 端口（默认 80） |
| `username` | string | 是 | ONVIF 用户名 |
| `password` | string | 是 | ONVIF 密码 |
| `bindingAreaId` | int | 否 | 绑定防区 ID |
| `cameraHeight` | float | 否 | 摄像头安装高度（米） |
| `trackparid` | int | 否 | 跟踪参数 ID |
| `trackMode` | int | 否 | 跟踪模式 |
| `minViewAngle` | float | 否 | 最小视角 |
| `maxViewAngle` | float | 否 | 最大视角 |
| `maxZoom` | int | 否 | 最大变焦倍数 |
| `isTrack` | int | 否 | 是否启用跟踪（0/1） |
| `cameraUrl` | string | 否 | 摄像头流地址（由系统自动获取） |
| `minZoomPan` / `minZoomTilt` / `maxZoomPan` / `maxZoomTilt` | float | 否 | PTZ 限位角度参数 |
| `counterclockwise` | int | 否 | 是否逆时针（0/1） |

### 4.5 修改摄像头

- **URL：** `PUT /api/camera`
- **说明：** 修改摄像头，Body 同「4.4」，需携带 `id`。

### 4.6 删除摄像头

- **URL：** `DELETE /api/camera/delete/{ids}`
- **说明：** 按 ID 删除，支持逗号分隔批量删除。

### 4.7 防区摄像头重复判断（新增）

- **URL：** `POST /api/camera/rjadd`

**请求参数（Body）：**

```json
{ "cameraIds": [1, 2, 3] }
```

### 4.8 防区摄像头重复判断（修改）

- **URL：** `POST /api/camera/rjedit`

**请求参数（Body）：**

```json
{ "bindingAreaId": 2, "cameraIds": [1, 2, 3] }
```

### 4.9 按防区查询摄像头

- **URL：** `GET /api/camera/dcamera/{DefenceAreaId}`
- **说明：** 按防区 ID 查询该防区绑定的摄像头列表。

### 4.10 设置最小变焦 PTZ 限位

- **URL：** `GET /api/camera/min/{Id}/{Ip}`
- **说明：** 将当前云台位置保存为摄像头最小变焦限位，成功返回 `data="OK"`。

### 4.11 设置最大变焦 PTZ 限位

- **URL：** `GET /api/camera/max/{Id}/{Ip}`
- **说明：** 将当前云台位置保存为摄像头最大变焦限位，成功返回 `data="OK"`。

---

## 5. 防区

基础路由：`api/defencearea`

### 5.1 防区列表

- **URL：** `GET /api/defencearea/list`
- **说明：** 分页查询防区。

**请求参数（Query）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | 否 | 按防区名称查询 |
| `pageNum` / `pageSize` | - | 否 | 公共分页参数 |

### 5.2 防区详情

- **URL：** `GET /api/defencearea/{id}`
- **说明：** 按防区 ID 查询。返回所有摄像头、所有雷达，以及该防区的绑定信息（ID>0 时）。

**响应 `data` 结构：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `cameras` | array | 全部摄像头列表 |
| `radars` | array | 全部雷达列表 |
| `defencearea` | object | 防区信息（ID>0 时返回） |
| `cameraIds` | array | 已绑定摄像头 ID |
| `radarIds` | array | 已绑定雷达 ID |

### 5.3 新增防区

- **URL：** `POST /api/defencearea`
- **说明：** 新增防区。若名称重复返回 `code=102`。

**请求参数（Body）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | 是 | 防区名称 |
| `defenceEnable` | int | 否 | 防区是否启用（0/1） |
| `defenceRadius` | int | 否 | 防区半径 |
| `latitude` | string | 否 | 纬度 |
| `longitude` | string | 否 | 经度 |
| `cameraIds` | array | 否 | 绑定摄像头 ID 数组 |
| `radarIds` | array | 否 | 绑定雷达 ID 数组 |

### 5.4 修改防区

- **URL：** `PUT /api/defencearea`
- **说明：** 修改防区，Body 同「5.3」，需携带 `id`。

### 5.5 启停防区

- **URL：** `PUT /api/defencearea/enable/{status}`
- **说明：** 批量启用/停用所有防区（联动雷达布防/撤防）。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `status` | int | 是 | 启用状态（1 启用 / 0 停用） |

### 5.6 绑定设备

- **URL：** `PUT /api/defencearea/bindDevices`
- **说明：** 为防区绑定/解绑摄像头与雷达设备。

**请求参数（Body）：**

```json
{
  "defenceAreaId": 1,
  "cameraIds": [1, 2],
  "radarIds": [3]
}
```

### 5.7 删除防区

- **URL：** `DELETE /api/defencearea/delete/{ids}`
- **说明：** 按 ID 删除，支持逗号分隔批量删除。

### 5.8 全部防区

- **URL：** `GET /api/defencearea/all`
- **说明：** 查询所有防区（不分页）。

### 5.9 防区树选择

- **URL：** `GET /api/defencearea/treeselect`
- **说明：** 查询防区树形结构，根节点为「All」。

---

## 6. 报警管理

基础路由：`api/alarm`

### 6.1 报警列表

- **URL：** `GET /api/alarm/list`
- **说明：** 分页查询报警记录。

**请求参数（Query）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ip` | string | 否 | 按雷达/摄像头 IP 查询 |
| `areaId` | int | 否 | 按防区 ID 查询 |
| `startTime` | string | 否 | 开始时间（`yyyy-MM-dd HH:mm:ss`） |
| `endTime` | string | 否 | 结束时间 |
| `pageNum` / `pageSize` | - | 否 | 公共分页参数 |

**响应 `data` 中记录字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | int | 报警 ID |
| `areaId` | int | 防区 ID |
| `areaName` | string | 防区名称 |
| `dateTime` | datetime | 报警时间 |
| `dealWith` | string | 处理状态/描述 |
| `videoName` | string | 录像文件名 |
| `cameraIp` | string | 摄像头 IP |
| `level` | string | 报警等级 |
| `latitude` / `longitude` | float | 报警目标经纬度 |
| `radarIp` | string | 报警目标的来源雷达 IP |

### 6.2 报警列表（更新版）

- **URL：** `GET /api/alarm/list1`
- **说明：** 分页查询报警列表（含未处理报警更新逻辑），查询参数同「6.1」。

### 6.3 报警引用列表

- **URL：** `GET /api/alarm/listref`
- **说明：** 分页查询报警（含关联引用数据），查询参数同「6.1」。

### 6.4 报警详情

- **URL：** `GET /api/alarm/{id}`
- **说明：** 按报警 ID 查询详情。

### 6.5 处理报警

- **URL：** `DELETE /api/alarm/update/{ids}`
- **说明：** 将报警标记为已处理，支持逗号分隔批量处理。

---

## 7. 标定

基础路由：`api/calibration`

> 标定模块用于雷达-摄像头联动标定，摄像头云台自动转动至目标位置。

### 7.1 新增标定数据

- **URL：** `POST /api/calibration`

**请求参数（Body）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `cameraIp` | string | 是 | 摄像头 IP |
| `defenceareaId` | int | 是 | 防区 ID |
| `calibrationDistance` | float | 是 | 标定距离 |
| `cameraPointX` | float | 是 | 摄像头坐标 X |
| `cameraPointY` | float | 是 | 摄像头坐标 Y |
| `cameraHeight` | float | 是 | 摄像头安装高度 |
| `camerarPointAngle` | float | 是 | 摄像头角度 |

### 7.2 摄像头 PTZ 控制

- **URL：** `GET /api/calibration/{Type}/{ips}/{speed}`
- **说明：** 控制摄像头云台转动，成功返回 `data="OK"`。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Type` | string | 是 | 控制类型（如上下左右、变焦等指令类型） |
| `ips` | string | 是 | 摄像头 IP，多个以逗号分隔 |
| `speed` | string | 是 | 转动速度 |

### 7.3 停止 PTZ

- **URL：** `GET /api/calibration/stop/{ips}`
- **说明：** 停止指定摄像头的云台转动，成功返回 `data="OK"`。

### 7.4 开始/结束跟踪

- **URL：** `GET /api/calibration/set/{Id}/{IsTrack}`
- **说明：** 设置摄像头的跟踪状态。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | int | 是 | 摄像头 ID |
| `IsTrack` | int | 是 | 1 开始跟踪 / 0 结束跟踪 |

### 7.5 查询标定信息

- **URL：** `GET /api/calibration/msg/{CameraIp}/{ZoneId}`
- **说明：** 按摄像头 IP 与防区 ID 查询标定数据。

### 7.6 更新标定数据

- **URL：** `GET /api/calibration/up/{a}/{b}/{c}/{d}/{e}/{f}`
- **说明：** 更新标定数据，成功返回 `data="OK"`。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `a` | string | 是 | 摄像头 IP |
| `b` | int | 是 | 防区 ID |
| `c` | float | 是 | 标定参数 c |
| `d` | float | 是 | 标定参数 d |
| `e` | float | 是 | 标定参数 e |
| `f` | float | 是 | 标定参数 f |

---

## 8. 轨迹信息

基础路由：`api/trackinfo`

### 8.1 按报警查询轨迹

- **URL：** `GET /api/trackinfo/list`
- **说明：** 按报警 ID 查询该次报警对应的目标轨迹点列表。

**请求参数（Query）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `alarmId` | int | 是 | 报警 ID |
| `areaId` | int | 否 | 防区 ID |
| `time` | string | 否 | 时间条件 |

**响应 `data` 中轨迹点字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `trackId` | int | 轨迹 ID |
| `alarmId` | int | 报警 ID |
| `updateTime` | datetime | 更新时间 |
| `lat` / `lng` | double | 经纬度 |
| `targetId` | int | 目标 ID |
| `x` / `y` | float | 雷达平面坐标 |

---

## 9. 多边形绘制

基础路由：`api/drawpolygon`

### 9.1 多边形列表

- **URL：** `GET /api/drawpolygon/list`
- **说明：** 查询所有已绘制的多边形。

### 9.2 多边形详情

- **URL：** `GET /api/drawpolygon/{id}`
- **说明：** 按 ID 查询多边形。

### 9.3 新增多边形

- **URL：** `POST /api/drawpolygon`

**请求参数（Body）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `defenceAreaId` | int | 否 | 关联防区 ID |
| `pointListLatLng` | string | 否 | 多边形顶点经纬度串（如 `lng,lat;lng,lat;...`） |
| `status` | int | 否 | 状态 |
| `pointType` | int | 否 | 多边形类型 |

### 9.4 修改多边形

- **URL：** `PUT /api/drawpolygon`
- **说明：** Body 同「9.3」，需携带 `drawId`。

### 9.5 删除多边形

- **URL：** `DELETE /api/drawpolygon/delete`
- **说明：** 删除多边形。ID 以 JSON 字符串放入请求体（URL 编码）。

**请求参数（Body）：**

```json
"1,2,3"
```

---

## 10. 瓦片下载

基础路由：`api/tiledownload`

### 10.1 启动瓦片下载任务

- **URL：** `POST /api/tiledownload/start`
- **说明：** 启动服务器端地图瓦片下载任务。接口立即返回，下载在服务器后台异步执行（进度通过 SignalR 推送）。`tileUrl` 与 `targetFolder` 为必填项，缺失返回 `code=1`。

**请求参数（Body）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `north` | double | 否 | 北纬度 |
| `south` | double | 否 | 南纬度 |
| `east` | double | 否 | 东经度 |
| `west` | double | 否 | 西经度 |
| `minZoom` | int | 否 | 最小缩放层级 |
| `maxZoom` | int | 否 | 最大缩放层级 |
| `tileUrl` | string | 是 | 瓦片 URL 模板（含 `{z}` `{x}` `{y}` 占位符） |
| `targetFolder` | string | 是 | 服务器目标文件夹路径 |

**请求示例：**

```json
{
  "north": 23.5,
  "south": 23.0,
  "east": 113.5,
  "west": 113.0,
  "minZoom": 1,
  "maxZoom": 18,
  "tileUrl": "http://map.example.com/tiles/{z}/{x}/{y}.png",
  "targetFolder": "D:/map_tiles"
}
```

---

## 11. 跟踪参数

基础路由：`api/trackparameter`

### 11.1 查询跟踪参数

- **URL：** `GET /api/trackparameter/{id}`
- **说明：** 按 ID 查询目标跟踪参数。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | int | 是 | 跟踪参数 ID |

---

## 12. 通用接口（视频窗口控制）

基础路由：`api/commoninterface`

> 该组接口通过 SignalR 向前端推送视频播放控制指令，前端订阅对应消息后播放/关闭摄像头视频窗口。

### 12.1 打开摄像头视频

- **URL：** `GET /api/commoninterface/open/{Ip}`
- **说明：** 打开指定 IP 摄像头的视频窗口。系统查询摄像头信息后，通过 SignalR 推送 `PlayStream`（视频流地址）与 `ShowWindow`（显示窗口）消息，成功返回 `data="OK"`。

**路径参数：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Ip` | string | 是 | 摄像头 IP |

### 12.2 关闭视频窗口

- **URL：** `GET /api/commoninterface/close`
- **说明：** 关闭视频窗口。通过 SignalR 推送 `CloseStream` 与 `HideWindow` 消息，成功返回 `data="OK"`。

---

## 13. 部署配置更新

基础路由：`api/ansyc`

> 该组接口用于服务器部署时自动更新前端 `config.js` 中的 IP 地址，一般仅实施人员使用。

### 13.1 更新前端配置 IP

- **URL：** `GET /api/ansyc?ip={ip}`
- **说明：** 将前端 `config.js` 中的 `localhost` 替换为指定 IP 并更新应用池。`ip` 参数必填，为空或非法时返回错误提示。

**请求参数（Query）：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ip` | string | 是 | 本机对外 IP 地址 |

### 13.2 查询本机 IP

- **URL：** `GET /api/ansyc/ip`
- **说明：** 返回服务器本机所有可用的 IPv4 地址列表。

---

## 14. 附录：SignalR 实时推送

系统通过 SignalR 向前端实时推送雷达目标、报警等数据，供二次开发参考。

- **Hub 地址：** `http://<服务器IP>:8080/hubs/stock`

**服务端推送消息（客户端订阅）：**

| 消息名 | 说明 | 数据内容 |
|--------|------|----------|
| `ReceiveTargetData` | 雷达目标实时数据（每帧推送） | 目标列表（含目标 ID、类型、坐标、速度等） |
| `TrackTargetData` | 跟踪目标通知 | 跟踪目标数据 |
| `AlarmPopup` | 报警弹窗 | 报警信息 |
| `ReceiveCommand` | 远程控制指令 | 指令内容 |
| `PlayStream` | 播放视频流 | 视频流 JSON（摄像头 IP、用户名、密码、流地址） |
| `CloseStream` | 关闭视频流 | 关闭指令 |
| `ShowWindow` | 显示视频窗口 | 显示指令 |
| `HideWindow` | 隐藏视频窗口 | 隐藏指令 |

### 14.1 连接方式

使用标准 SignalR 客户端（协议版本 ≥ 1.0，支持 JSON）连接：

```
http://<服务器IP>:8080/hubs/stock
```

**连接示例（JavaScript / `@microsoft/signalr`）：**

```js
import * as signalR from '@microsoft/signalr'

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://<服务器IP>:8080/hubs/stock')
  .withAutomaticReconnect([1000, 4000, 1000, 4000])   // 断线自动重连
  .build()

await connection.start()
```

### 14.2 ReceiveTargetData — 雷达目标实时数据

- **推送频率：** 按雷达 IP 独立计时，**每台雷达每 1 秒最多推送一帧**，一帧为该雷达当前全部目标。
- **数据内容：** **JSON 字符串**（camelCase），需调用方 `JSON.parse` 解析，解析后为**目标对象数组**（帧级批量）。数组元素结构如下：

| 字段 | 类型 | 说明 |
|------|------|------|
| `targetId` | int | 目标 ID |
| `lat` | double | 纬度 |
| `lng` | double | 经度 |
| `targetType` | int | 目标类型（取值见下表） |
| `dateTime` | string | 目标时间（`yyyy-MM-dd HH:mm:ss`） |
| `distance` | string | 目标距离 |
| `azimuthAngle` | string | 方位角 |
| `speedX` | string | 速度 X 分量 |
| `speedY` | string | 速度 Y 分量 |
| `northDeviationAngle` | string | 正北偏角 |
| `radarIp` | string | 雷达 IP（用于区分多台雷达） |
| `axesX` | float | 雷达平面坐标 X |
| `axesY` | float | 雷达平面坐标 Y |
| `axesZ` | float | 雷达平面坐标 Z（高度） |
| `areaId` | int | 防区 ID |

**`targetType` 取值说明：**

| targetType | 类型 |
|-----------|------|
| 1 | 人 |
| 2 | 车 |
| 3 | 树 |
| 4 | 船 |
| 6 | 小船 |
| 7 | 中船 |
| 8 | 大船 |
| 其他值 | 其他 |

**推送示例（`JSON.parse` 后的数组）：**

```json
[
  {
    "targetId": 1001,
    "lat": 23.125634,
    "lng": 113.263482,
    "targetType": 1,
    "dateTime": "2026-08-04 10:23:45",
    "distance": "135.2",
    "azimuthAngle": "45.0",
    "speedX": "1.2",
    "speedY": "0.3",
    "northDeviationAngle": "5.0",
    "radarIp": "192.168.1.101",
    "axesX": 12.34,
    "axesY": 56.78,
    "axesZ": 0.0,
    "areaId": 1
  }
]
```

**订阅示例：**

```js
connection.on('ReceiveTargetData', (payload) => {
  const targets = JSON.parse(payload)   // 解析后为目标对象数组
  targets.forEach((t) => {
    // 渲染：t.targetId / t.lat / t.lng / t.targetType / t.radarIp ...
  })
})
```

### 14.3 TrackTargetData — 跟踪目标通知

- **数据内容：** **单个目标 ID（整型）**，标识当前正被摄像头跟踪的目标，用于前端联动高亮/跟踪该目标。

**订阅示例：**

```js
connection.on('TrackTargetData', (targetId) => {
  // targetId：被跟踪目标 ID（int），例如 1001
})
```

---

## 15. 常见调用流程示例

### 15.1 雷达目标实时数据链路

```
雷达设备 → 系统（TCP 采集）→ 坐标转换/防区过滤 → SignalR 推送
前端订阅 ReceiveTargetData → 地图渲染目标轨迹
```

---

*— 文档结束 —*
