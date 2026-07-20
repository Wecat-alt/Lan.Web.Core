# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 仓库概览

这是 **RVS-M Web** 雷达安防监控系统，通过雷达设备进行周界监控，集成 ONVIF 摄像头进行录像，并通过 SignalR 实时推送目标数据到前端。

```
D:\WebCore\
├── Lan.NetCore/          # .NET 10 后端
│   └── Lan.Application/  # 解决方案根目录
│       ├── Lan.Application/     # Web API 层（Controllers、Program.cs、Swagger）
│       ├── Lan.ServiceCore/     # 业务逻辑层（Service、BackgroundService、SignalR Hub、设备管理单例）
│       ├── Lan.Repository/      # 数据访问层（SqlSugar ORM + MySQL）
│       ├── Lan.Model/           # 领域模型、DTO、VO、分页模型
│       ├── Lan.Infrastructure/  # 基础设施（BaseController、ONVIF/RBTrack SDK 封装、地理计算、异常处理）
│       └── Lan.RadarSdk.Core/   # 新版雷达 SDK（TCP 通信协议、RadarClient）
├── Lan.Vue/              # Vue 3 前端（GIS 地图、实时轨迹、设备管理）
│   └── src/
│       ├── api/          # 按业务域划分的 API 模块（system/device/alarm/map/config）
│       ├── components/   # 公共组件（AppSidebar、Pagination、DictTag、SvgIcon 等）
│       ├── composables/  # Vue 组合式函数
│       ├── i18n/         # 国际化配置与中/英文语言文件
│       ├── plugins/      # 全局插件（auth、cache、modal、download、tab）
│       ├── router/       # Vue Router 路由配置
│       ├── signalr/      # 旧版 SignalR 实现
│       ├── stores/       # Pinia 状态管理
│       ├── utils/        # 工具函数（request、auth、permission、TrackManager、signalRUtils 等）
│       └── views/        # 页面组件（map/device/query/system/livePreview/dataScreen）
└── Lan.Android/          # Android 原生 App（Kotlin + Jetpack Compose）
    └── app/src/main/java/com/lan/android/
        ├── data/api/         # Retrofit API 接口 + OkHttp 客户端
        ├── data/model/       # 数据模型（RadarModel、TargetData 等）
        ├── data/signalr/     # SignalR 客户端（接收实时目标数据）
        └── ui/
            ├── map/          # 地图页（OSMDroid + 雷达扇形 + 目标标记）
            ├── alarm/        # 报警列表页
            ├── device/       # 设备状态页
            ├── settings/     # 设置页（服务器地址配置）
            ├── navigation/   # 底部导航（4 个 Tab）
            └── theme/        # Material3 主题（配色对齐 Web 端 Element Plus）
```

> **已移除:** `Lan.NsrRadarSdk` 旧版雷达 SDK 项目已删除。其核心类型（`IRvs_Target`、`RVS_Target_List`、`RVS_DeviceAddress` 枚举）已迁移至 `Lan.ServiceCore/TargetCollection/RadarLegacyTypes.cs`，供下游管线继续使用。

## 构建与运行

### 后端（.NET 10.0，x64 only）

```sh
# 构建
dotnet build Lan.NetCore/Lan.Application/Lan.Application.sln

# 运行
dotnet run --project Lan.NetCore/Lan.Application/Lan.Application/Lan.Application.csproj

# 发布 Windows x64
dotnet publish -c Release -r win-x64

# 发布 Linux x64
dotnet publish -c Release -r linux-x64
```

**注意：本项目没有测试项目。** 所有验证依赖手动测试或运行时观察。

### 前端（Vue 3 + Vite）

```sh
cd Lan.Vue
npm install        # 安装依赖（Node 20+）
npm run dev        # Vite 开发服务器（端口 5122，热更新）
npm run build      # 生产构建 → dist/
npm run preview    # 本地预览生产构建
npm run lint       # ESLint 检查 + 自动修复（带缓存）
npm run format     # Prettier 格式化 src/
```

### 前端 Docker 部署

```sh
cd Lan.Vue
docker compose up -d    # 构建并启动，映射 8080:80
```

Docker 镜像在容器启动时通过 `docker/entrypoint/40-env-config.sh` 根据环境变量动态生成 `config.js`，实现同一镜像多环境部署。

---

## 后端架构

### 解决方案结构（7 个项目）

| 项目 | 职责 |
|------|------|
| `Lan.Application` | ASP.NET Core Web API 入口，Program.cs、Controllers |
| `Lan.ServiceCore` | 业务逻辑、BackgroundService、SignalR Hub、Manager 单例 |
| `Lan.Repository` | SqlSugar ORM 数据访问，泛型 `Repository<T>` |
| `Lan.Infrastructure` | BaseController、ONVIF/RBTrack SDK 封装、`[AppService]` DI、异常处理 |
| `Lan.Model` | 领域模型（SqlSugar `[SugarTable]`）、DTO、VO |
| `Lan.RadarSdk.Core` | 新版雷达 TCP 通信 SDK（`RadarClient`） |

### 分层与依赖链路

```
Application → ServiceCore → Repository → Infrastructure + Model
                ↑
           RadarSdk.Core（项目引用）
```

### 核心设计模式

- **`[AppService]` 自动 DI 注册**：`ServiceExtensions.AddService()` 扫描 `Lan.Repository` 和 `Lan.ServiceCore` 程序集，自动将带有 `[AppService]` 特性的类注册到 DI 容器（支持 Transient/Scoped/Singleton）。`ServiceType` 指定接口类型，`InterfaceServiceType=true` 自动取第一个接口。
- **Repository 泛型模式**：所有 Service 继承 `Repository<T>`（泛型），后者继承 `DbContext<T>`（封装 SqlSugarClient）。Service 同时承担业务逻辑和数据访问。
- **Manager 单例**：`CameraManager`、`DefenceAreaManager`、`RadarManager` 通过 `GetInstance()` 静态方法管理设备生命周期。
- **`RadarClientManager`** — `BackgroundService` + `Singleton` 双注册，管理所有雷达的 TCP 连接、目标接收、状态轮询和在线状态缓存。由于 DI 双注册会产生两个实例，状态缓存 `_statusCache` 使用 `static` 字段确保共享。支持运行时动态 `StartClient`/`StopClientAsync`。
- **Producer-Consumer**：`AlarmBackgroundService` 使用 `BlockingCollection<AlarmEvent>` 收集报警事件；`RadarDataChannelService` 使用 `Channel<TrackInfo>`（容量 10,000，批大小 200，10s 超时）处理跟踪数据；`Worker` 使用 `ConcurrentQueue<SendMS>` 每 10ms 去重推送 SignalR 消息。
- **按月分表**：`trackinfo` 表按 `yyyyMM` 后缀自动分表（如 `trackinfo202501`），分表逻辑在 `Repository<T>.BatchInsertAsync()` 中实现。**分表不自动创建索引**，基表已维护索引。

### BaseController 响应格式

所有 Controller 继承 `BaseController`（`Lan.Infrastructure/Controller/BaseController.cs`），统一返回 `ApiResult` JSON 格式（camelCase 命名，日期格式 `yyyy-MM-dd HH:mm:ss`）。通过 `ToResponse()` / `Message()` 方法封装，`ResultCode` 枚举用 `[Description]` 特性标注消息文本。

### API 路由总览

| 路由前缀 | Controller | 说明 |
|----------|-----------|------|
| `api/login` | `SysLoginController` | 用户登录 |
| `api/system/user` | `SysUserController` | 用户 CRUD |
| `api/config` | `SysConfigController` | 系统配置（含 `configKey/{key}` 按 key 查询） |
| `api/system/dict/data` | `SysDictDataController` | 字典数据（含 `type/{dictType}` 按类型查询） |
| `api/ystem/dict/type` | `SysDictTypeController` | 字典类型 |
| `api/radar` | `RadarController` | 雷达 CRUD + `setLatLng` 坐标设置 |
| `api/camera` | `CameraController` | 摄像头 CRUD + PTZ `min`/`max` 限位 |
| `api/defencearea` | `DefenceAreaController` | 防区 CRUD + `enable/{status}` 启停 |
| `api/alarm` | `AlarmController` | 告警查询 |
| `api/calibration` | `CalibrationController` | 标定（含 `stop/{ips}` 停止标定） |
| `api/trackinfo` | `TrackInfoController` | 轨迹信息查询 |
| `api/drawpolygon` | `DrawPolygonController` | 多边形绘制 |
| `api/tiledownload` | `TileDownloadController` | 地图瓦片下载 |
| `api/trackparameter` | `TrackParameterController` | 跟踪参数 |
| `api/commoninterface` | `CommonInterfaceController` | 通用接口（摄像头 `open/{Ip}` / `close`） |
| `api/ansyc` | `ConfigJsUpdaterController` | 部署配置更新 + 本机 IP 查询 |

### 全部 BackgroundService（8 个）

| 类 | 职责 |
|----|------|
| `RadarClientManager` | 管理所有雷达 TCP 连接、目标接收、状态轮询（0x0A）、自动重连 |
| `RadarBackgroundService` | 启动时初始化 `RadarManager` 和 `DefenceAreaManager` 雷达事件 |
| `AlarmBackgroundService` | 消费报警队列 → 录像控制 → SignalR `AlarmPopup` 推送 |
| `AlarmAndRadarBackgroundService` | 合并版报警+雷达数据处理（Channel 批处理 + BlockingCollection） |
| `RadarDataChannelService` | Channel 批量写入 `TrackInfo` 到数据库 |
| `Worker` | 每 10ms 从 ConcurrentQueue 去重推送 `ReceiveTargetData` 到 SignalR |
| `TrackTarget` | 推送 `TrackTargetData` 到 SignalR |
| `RadarTrack` | 空壳（死循环无操作，疑似废弃代码） |

### SignalR Hub

**唯一 Hub：** `MessageHub`（`Lan.ServiceCore/Signalr/MessageHub.cs`），映射在 `/hubs/stock`。

**服务端方法（客户端可调用）：**
- `SendMessage(string message)` → 广播 `ReceiveMessage`
- `ControlClient(string command)` → 广播 `ReceiveCommand`
- `PlayVideo(string streamUrl)` → 广播 `PlayStream`
- `ShowWindow()` → 广播 `ShowWindow`
- `HideWindow()` → 广播 `HideWindow`

**服务端推送方法（客户端订阅）：**
- `ReceiveTargetData` — 雷达目标实时数据（由 `Worker` 推送）
- `TrackTargetData` — 跟踪目标通知（由 `TrackTarget` 推送）
- `AlarmPopup` — 报警弹窗（由 `AlarmBackgroundService` 推送）
- `ReceiveCommand` — 远程控制指令（由 `CommonInterfaceController` 触发）
- `PlayStream` / `ShowWindow` / `HideWindow` — 视频窗口控制

### 关键业务流程

1. **雷达数据流**：雷达设备 → `RadarClientManager` (TCP 连接 + 目标接收) → `RadarTargetAdapter`（适配为 `IRvs_Target`）→ `WRadar.RadarTargets` → `RadarManager.OnTargetDetect()` → `TargetCollection.AddTarget()` → 坐标转换、多边形过滤 → 写入报警队列和跟踪数据 Channel
2. **雷达状态监控**：`RadarClientManager` 连接成功后立即发送 0x0A 状态读取命令，之后每 5 秒定时轮询。收到 0xA2 ACK 即标记在线，同时从状态帧 byte 17-18 解析雷达型号（如 NSR100W、SUC261）。发送失败则主动断开触发重连（5s 延迟）。
3. **报警流程**：`AlarmBackgroundService` 消费报警队列 → 开始录像 → 生成 `AlarmModel` 记录 → 通过 SignalR `MessageHub` 推送弹窗
4. **实时推送**：`Worker` 每 10ms 从 `ConcurrentQueue` 去重并推送 `SendMS` 到 SignalR Hub（`ReceiveTargetData`）

### 原生 SDK 集成

项目强依赖 Windows x64 原生 DLL，在 `Program.cs` 中通过 `NativeLibrary.SetDllImportResolver` 动态加载：

| SDK | 路径 | 用途 |
|-----|------|------|
| ONVIF SDK | `onvifSdk/x64/OnvifClient.dll` | 摄像头 PTZ 控制 |
| NovaPlayer SDK | `NovaPlayer/x64/` | 视频解码和录像 |
| RBTrack SDK | `RBTrackSdk/x64/RBTrack.dll` | 目标跟踪算法 |
| RadarToLonLat | `RadarToLonLat.dll` | 雷达坐标转经纬度 |

Linux 下使用对应的 `.so` 文件。NovaPlayer 在 Linux 上的路径明确为 `NovaPlayer/lib/libNovaPlayer.so`，不要为 Linux 添加 `x64` 回退路径。

**雷达通信 SDK:** 新版 `Lan.RadarSdk.Core`（`RadarClient`）通过 TCP 直连雷达（默认端口 50000），替代了旧版 `Lan.NsrRadarSdk`（已移除）。旧 SDK 的核心类型（`IRvs_Target`、`RVS_Target_List` 等）保留在 `Lan.ServiceCore/TargetCollection/RadarLegacyTypes.cs` 中，供下游目标处理管线使用。

### 数据库

- MySQL，连接字符串在 `Lan.Application/appsettings.json` → `ConnectionStrings:conn`
- SqlSugarCore ORM，`InitKeyType = InitKeyType.Attribute`（通过特性标记主键）
- 日志：NLog + Serilog

### 数据库表

| 表名 | 模型 | 说明 |
|------|------|------|
| `radar` | `RadarModel` | 雷达设备 |
| `camera` | `CameraModel` | 摄像头（含 PTZ 跟踪角度参数） |
| `defencearea` | `DefenceareaModel` | 防区（`[SugarColumn(IsIgnore=true)]` 导航属性关联雷达/摄像头） |
| `alarm` | `AlarmModel` | 报警记录 |
| `trackinfo` | `TrackInfo` | 跟踪数据（按月分表） |
| `track` | `TrackModel` | 原始跟踪数据 |
| `calibration` | `Calibration` | 标定数据 |
| `drawpolygon` | `DrawPolygon` | 多边形数据 |
| `sys_user` | `SysUser` | 系统用户 |
| `sys_config` | `SysConfig` | 系统配置键值对 |
| `sys_dict_type` | `SysDictType` | 字典类型 |
| `sys_dict_data` | `SysDictData` | 字典数据 |
| `sys_logininfor` | `SysLogininfor` | 登录日志 |

### appsettings.json 关键配置

| 路径 | 说明 |
|------|------|
| `ConnectionStrings:conn` | MySQL 连接字符串 |
| `dbConfigs` | 额外数据库配置数组（`Conn`, `DbType`: 0=MySql/1=SqlServer），含 `CAT_RADAR` DB |
| `CorsUrls` | 允许的 CORS 来源数组 |
| `NLog` | NLog 日志配置 |

### ConfigJsUpdater（部署配置自动更新）

`ConfigJsUpdater` 服务（`Lan.ServiceCore/Public/ConfigJsUpdater.cs`）在部署时通过 API 触发：
- `UpdateConfigJs(ip)` — 将 `D:/RVS_WEB/lan/config.js` 中所有 `localhost` 替换为本机 IP
- `UpdateAppSettings(ip, path)` — 更新 `appsettings.json` 中 `CorsUrls` 的 IP
- `RecycleAppPool(webConfigPath)` — 触碰 `web.config` 触发 IIS 应用池回收
- `GetAllLocalIPv4()` — 获取本机所有物理网卡 IPv4（排除回环、隧道、172.x）

触发端点：`GET api/ansyc?ip=...` 和 `GET api/ansyc/ip`。

### Program.cs 中间件管道

```
Swagger → SwaggerUI → HttpsRedirection → Authorization → CORS → Routing
  → Endpoints（SignalR /hubs/stock）→ 异常处理 lambda → MapControllers
```

启动后初始化顺序：`CameraInit()` → `RadarInit()` → `DefenceAreaInit()` → `OnvifManage.Init()` → `RBTRACK_Init(256)` → `DefenceAreaManager.EnbaleRadarEvent()`。

---

## 前端架构

### 技术栈

- **Vue 3**（Composition API，`<script setup>` 与 Options API 混用）
- **Vite 7**（开发端口 5122）+ `@vitejs/plugin-vue`
- **Element Plus** 2.x — UI 组件库，`unplugin-vue-components` 自动按需导入
- **unplugin-auto-import** — 自动导入 `vue`/`vue-router`/`pinia` API，**无需手动 `import { ref, computed } from 'vue'`**
- **Pinia** 3.x + `pinia-plugin-persistedstate` — 状态管理及持久化
- **Vue Router** 4.x — 路由懒加载
- **vue-i18n** 11.x — 中/英文国际化
- **Leaflet** 1.x + `@geoman-io/leaflet-geoman-free` + `leaflet-trackplayer` + `leaflet.motion` — GIS 地图与轨迹可视化
- **OpenLayers** (`ol`) 10.x — 备用地图引擎
- **Three.js** 0.x — 3D 雷达可视化（`/radar3d` 路由）
- **SignalR** (`@microsoft/signalr`) — 实时雷达目标数据推送
- **Axios** — HTTP 客户端
- **ECharts** 6.x — 数据可视化
- **Sass** — CSS 预处理器

### 环境变量

| 变量 | 开发环境 | 说明 |
|------|---------|------|
| `VITE_APP_API_BASE_URL` | `http://localhost:5197` | 后端 API 地址 |
| `VITE_SIGNALR_URL` | `http://localhost:5197/hubs/stock` | SignalR Hub 地址 |
| `VITE_OPEN_URL` | `http://localhost:5122` | 前端访问地址 |
| `VUE_APP_TITLE` | `开发环境` | 环境名称 |

另有 `.env.production` 和 `.env.test` 对应生产和测试环境。`VITE_MAP_TILE_MAP_URL` 仅通过 Docker 环境变量或 `public/config.js` 配置。

### 运行时配置

应用从 `window.__APP_CONFIG__` 读取配置，该对象由 `public/config.js` 设置。Docker 部署时 `docker/entrypoint/40-env-config.sh` 在容器启动时根据环境变量生成此文件。本地开发使用 `.env.development` 中的 Vite 环境变量。**不要假设 .env 文件一定存在**——生产环境通过容器或构建脚本注入。

关键配置项：`VITE_APP_API_BASE_URL`、`VITE_SIGNALR_URL`、`VITE_OPEN_URL`、`VITE_MAP_TILE_MAP_URL`、`VUE_APP_TITLE`。

### 路由与布局

- `src/router/index.js` — 18 个路由定义，大多懒加载，`meta: { requiresAuth: true, menuKey: '...' }`
- 路由守卫 `beforeEach`：先检查认证状态（`localStorage.isAuthenticated`），再检查菜单权限（`canAccessMenu(menuKey)`）
- `src/views/index.vue` — 登录后的主应用外壳。`<el-container>` 布局，顶部 `<el-header>` 含 `AppSidebar` 和用户信息，主内容区用 `<component :is="currentView">` 动态切换视图——**不是**嵌套 `<router-view>`
- `src/views/login.vue` — 独立登录页
- `src/App.vue` — 根组件，仅渲染 `<router-view>`

**关键路由：** `/main`（主页）、`/realtime_map`（实时地图）、`/autoMap`（自动地图）、`/livePreview`（实时预览）、`/historical_map`（历史轨迹）、`/play`（轨迹回放）、`/radar3d`（3D 雷达）、`/screen`（数据大屏）、`/calibration`（标定）。

### 状态管理

- Pinia + 持久化插件，入口 `src/stores/index.js`
- `useAuthStore`（`src/stores/auth.js`）— 登录/登出、Token 管理、`isAuthenticated` 状态
- `useSocketStore`（`src/stores/socket.js`）— SignalR 相关状态（在线用户、聊天、通知、全局错误等），持久化聊天和通知数据

### API 层

- `src/utils/request.js` — 中央 Axios 实例。`baseURL` 来自 `window.__APP_CONFIG__.VITE_APP_API_BASE_URL`。请求拦截器自动从 localStorage 附加 `Bearer` Token。响应拦截器透传错误日志。
- API 模块按域划分：
  - `system/` — 登录、用户、系统配置、字典
  - `device/` — 雷达、摄像头、标定、防区
  - `alarm/` — 告警查询、轨迹信息
  - `map/` — 地图数据、保存
  - `config/` — 应用配置项
- 调用风格：`import { listRadar } from '@/api/device/radar'`，返回 `response.data.data`

### SignalR（实时通信）

两套实现并存：

1. **旧版** — `src/signalr/signalr.js` 导出单例，`init(url)` + `start()`，消息通过 `src/signalr/analysis.js` 路由到 Pinia socket store
2. **新版（推荐）** — `src/utils/signalRUtils.js` 发布/订阅模式：`ensureSignalRConnection()` 获取共享连接，`subscribeSignalR()` 订阅，`setSignalRReceiveEnabled()` 控制消费

主外壳 `index.vue` 挂载时调用 `ensureSignalRConnection`，根据当前视图切换数据接收状态。

**客户端接收的 SignalR 消息名：** `ReceiveTargetData`（雷达目标）、`TrackTargetData`（跟踪通知）、`AlarmPopup`（报警弹窗）、`ReceiveCommand`（远程指令）、`PlayStream` / `ShowWindow` / `HideWindow`（视频窗口控制）。

### 认证与权限

- `src/utils/permission.js` — 本地基于角色：`admin`（全部权限）、`operator`（指定菜单）、`guest`（仅地图查看）
- 登录时 `buildAuthProfile()` 将服务端数据与本地预设合并，存入 localStorage `authProfile`
- `canAccessMenu(menuKey)` 控制菜单显隐和视图切换
- Token 存储在 localStorage `token` 键（`src/utils/auth.js`），登录接口响应中取 `token` / `accessToken` / `Authorization` 字段

### GIS / 地图

- `src/utils/TrackManager.js` — 核心类，管理 Leaflet 地图上的雷达目标轨迹：目标创建、折线轨迹、标记图标（人/车）、弹窗、超时清理、轨迹高亮。提供钩子 `onTargetAdded` / `onTargetUpdated` / `onTargetRemoved`
- 地图视图：`src/views/map/realtime_map.vue`（实时）、`historical map.vue`（历史）、`AutoMap.vue`（自动）、`play.vue`（回放）、`radar3d.vue`（Three.js 3D 视图）
- 地图中心/缩放通过后端配置 key `mapCenter` / `mapZoom` 管理，用 `proxy.getConfigKey` 读取、`updateConfig` 保存
- 组件销毁时必须移除 map 和停止定时器（`onBeforeUnmount`），新增图层需确保被记录并在清理时移除

### 国际化

- `src/i18n/index.js` — `vue-i18n` 实例，默认中文，支持英文。`changeLanguage()` 持久化到 localStorage 并同步更新 Element Plus 组件语言
- `src/main.js` 中通过 `app.config.globalProperties` 暴露 `$t` 和 `$locale`

### 全局插件

`src/plugins/index.js` 安装：`auth.js`（权限指令）、`cache.js`（session/local/cookie）、`modal.js`（消息/通知/加载）、`download.js`（文件下载）、`tab.js`（标签页管理）。组件内通过 `const { proxy } = getCurrentInstance()` 调用 `proxy.$modal`、`proxy.getConfigKey` 等。

### 关键公共组件

| 组件 | 文件 | 说明 |
|------|------|------|
| `AppSidebar` | `src/components/AppSidebar.vue` | 水平导航菜单，权限控制菜单项显隐 |
| `Pagination` | `src/components/Pagination/index.vue` | 分页组件（全局注册） |
| `DictTag` | `src/components/DictTag/index.vue` | 字典标签渲染（全局注册） |
| `SvgIcon` | `src/components/SvgIcon/index.vue` | SVG 图标（全局注册，支持 Element Plus 图标前缀 `ele-`） |
| `LocalPlayerWindow` | `src/components/LocalPlayerWindow.vue` | 可拖拽缩放的浮动视频预览窗口，通过 WebSocket 与本地播放器通信 |
| `AlarmAutoPlayerWindow` | `src/components/AlarmAutoPlayerWindow.vue` | 报警自动弹窗视频播放器（右下角 420×235） |

### Vue 代码注意事项

- `unplugin-auto-import` 已全局导入 `vue`/`vue-router`/`pinia` API，**不要手动添加** `import { ref, computed, watch, onMounted } from 'vue'` 等语句
- `unplugin-vue-components` 自动按需导入 Element Plus 组件，**不要手动 import Element Plus 组件**
- `@` 别名映射到 `src/`
- 全局属性通过 `proxy` 访问：`const { proxy } = getCurrentInstance()` → `proxy.$modal`、`proxy.getConfigKey`、`proxy.$t` 等

---

## Android App 架构

### 技术栈

- **Kotlin** + **Jetpack Compose**（Material3）
- **Gradle 8.9** + AGP 8.7.2 + Kotlin 2.1.0
- **minSdk 26** / targetSdk 35 / compileSdk 35
- **OSMDroid 6.1.20** — 离线地图（高德卫星图瓦片）
- **Retrofit 2.11** + Gson — HTTP 请求
- **SignalR 8.0** (`com.microsoft.signalr:signalr`) — 实时雷达目标数据
- **Navigation Compose 2.8** — 底部 Tab 导航
- **Coroutines** — 异步操作

仓库使用阿里云 Maven 镜像加速依赖下载。

### 构建与运行

用 Android Studio 打开 `Lan.Android/` 目录，Gradle 同步后直接运行。

```sh
# 命令行构建
cd Lan.Android
./gradlew assembleDebug    # 编译 Debug APK
./gradlew assembleRelease  # 编译 Release APK
```

### 项目结构

| 包路径 | 职责 |
|--------|------|
| `data.api.ApiClient` | OkHttp + Retrofit 单例，支持运行时 `configure(url, token)` |
| `data.api.ApiService` | Retrofit 接口：`listRadar`、`allDefenceArea`、`listAlarm`、`login` |
| `data.model` | 数据类：`ApiResponse<T>`、`RadarModel`、`CameraModel`、`DefenceAreaModel`、`AlarmModel`、`TargetData` |
| `data.signalr.SignalRClient` | SignalR 客户端，连接 `/hubs/stock`，订阅 `ReceiveTargetData`，断线自动重连（5s） |
| `ui.navigation.AppNavigation` | 底部 4 Tab 导航：地图、报警、设备、设置 |
| `ui.map.MapScreen` | 全屏 OSMDroid 地图 + 浮动防区/雷达 FilterChip + 目标信息卡片 |
| `ui.map.MapViewModel` | 地图状态管理：加载防区→雷达→连接 SignalR→按 `radarIp` 过滤目标（保留最近 50 条） |
| `ui.alarm.AlarmListScreen` | 报警列表页（骨架，待实现 API 加载） |
| `ui.device.DeviceScreen` | 设备状态页（骨架，待实现） |
| `ui.settings.SettingsScreen` | 设置页：配置后端 API 地址 |
| `ui.theme.Theme` | Material3 浅色主题，配色与 Web 端 Element Plus 对齐（Blue `#409EFF`、Red、Yellow、Green） |

### 核心流程

1. **启动** → `LanApplication` 初始化 OSMDroid（离线瓦片缓存目录）
2. **进入地图** → `MapViewModel.loadData()` 调用 `allDefenceArea` API 获取防区列表
3. **选择防区** → 调用 `listRadar(areaId)` 获取雷达列表 → 自动连接 SignalR
4. **选择雷达** → 按 `radarIp` 过滤 `ReceiveTargetData` 推送的目标数据
5. **目标渲染** → `MapScreen` 通过 `AndroidView` 嵌入 OSMDroid `MapView`，`update` 回调中清除旧覆盖物并重绘雷达扇形 + 目标标记
6. **生命周期** → `MapViewModel.onCleared()` 断开 SignalR 连接

### 与 Vue 前端的对应关系

| Android | Vue | 说明 |
|---------|-----|------|
| `MapScreen` + `MapViewModel` | `calibration` 视图 | 地图 + 防区/雷达选择 + 实时目标 |
| `SignalRClient` | `signalRUtils.js` | SignalR 连接管理，订阅 `ReceiveTargetData` |
| `ApiClient` | `request.js` | HTTP 客户端（Bearer Token 拦截器） |
| `ApiService` | `api/device/radar.js` 等 | API 接口定义 |
| 高德卫星图 | `VITE_MAP_TILE_MAP_URL` | 地图瓦片源 |
| `TargetData` | `SendMS` (SignalR 消息) | 实时目标数据结构 |
| `targetTypeLabel()` | `mapTargetType()` | 目标类型 0/1=人, 2/3=车 |
| `FilterChip` 过滤 | `queryParams.radarIp` | 按雷达 IP 过滤目标 |

### Android 特有注意事项

- OSMDroid 通过 `AndroidView` 嵌入 Compose，覆盖物操作在 `update` lambda 中执行（每次 state 变化都会清空并重绘）
- `ApiClient` 默认地址 `http://192.168.1.100:5000/`，用户可在设置页修改
- SignalR 只订阅 `ReceiveTargetData` 消息（未订阅 `AlarmPopup`、`TrackTargetData` 等）
- `AlarmListScreen` 和 `DeviceScreen` 目前为占位骨架，功能待实现
- `usesCleartextTraffic=true` 允许 HTTP 明文通信（开发/内网环境）

---

## 数据流（雷达 → 前端）

```
雷达设备 → RadarClientManager (TCP + 0x0A状态轮询)
  → RadarTargetAdapter → WRadar.RadarTargets
  → RadarManager.OnTargetDetect()
  → TargetCollection.AddTarget() → 坐标转换、多边形过滤
  → AlarmBackgroundService (BlockingCollection 报警队列)
    + RadarDataChannelService (Channel 跟踪数据)
  → Worker (每10ms ConcurrentQueue 去重推送 SignalR)
  → SignalR Hub → Vue 前端 / Android App (SignalR 连接)
  → TrackManager (Leaflet) / OSMDroid MapView → 地图轨迹渲染
```

---

## 平台限制

- 后端：.NET 10.0，x64 only。强依赖 Windows 原生 DLL，Linux 需要对应 `.so`
- Vue 前端：Node 20+（`package.json` engines: `^20.19.0 || >=22.12.0`），浏览器环境（Leaflet 依赖 DOM）
- Android App：minSdk 26 (Android 8.0)，Kotlin 2.1 + Compose，需要 Android Studio 开发
- 无自动化测试（前后端均无测试项目）
