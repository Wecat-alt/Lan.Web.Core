# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在此仓库中工作时提供指导。

## 项目概述

Lan.Vue 是 **RVS-M Web** 的 Vue 3 前端，一个雷达监控与 GIS 追踪系统。通过 REST API 与 .NET 后端（参见同级目录 `Lan.NetCore`）通信，并通过 SignalR 接收实时雷达目标数据。

## 常用命令

```sh
npm run dev       # 启动 Vite 开发服务器（热更新）
npm run build     # 生产构建，输出到 dist/
npm run preview   # 本地预览生产构建
npm run lint      # ESLint 检查并自动修复（带缓存）
npm run format    # Prettier 格式化 src/
```

## 技术栈

- **Vue 3**（Composition API，`<script setup>` 与 Options API 混用）
- **Vite 7** + `@vitejs/plugin-vue`
- **Element Plus** 2.x — UI 组件库，通过 `unplugin-vue-components` 自动按需导入
- **Pinia** 3.x + `pinia-plugin-persistedstate` — 状态管理及持久化
- **Vue Router** 4.x — 路由懒加载
- **vue-i18n** 11.x — 中/英文国际化
- **Leaflet** 1.x + `@geoman-io/leaflet-geoman-free` + `leaflet-trackplayer` + `leaflet.motion` — GIS 地图与轨迹可视化
- **OpenLayers** (`ol`) 10.x — 备用地图引擎
- **SignalR** (`@microsoft/signalr`) — 实时雷达目标数据推送
- **Axios** — HTTP 客户端，请求拦截器自动注入 Bearer Token
- **ECharts** 6.x — 数据可视化图表
- **Sass** — CSS 预处理器

## 运行时配置

应用从 `window.__APP_CONFIG__` 读取后端地址，该对象由 `public/config.js` 设置。Docker 部署时，`docker/entrypoint/40-env-config.sh` 在容器启动时根据环境变量生成此文件。本地开发时使用 `.env.development` 中的 Vite 环境变量。

关键配置项：`VITE_APP_API_BASE_URL`、`VITE_SIGNALR_URL`、`VITE_OPEN_URL`、`VITE_MAP_TILE_MAP_URL`、`VUE_APP_TITLE`。

## 架构

### 路由与布局

- [src/router/index.js](src/router/index.js) — 定义所有路由，大多为懒加载，带有 `meta: { requiresAuth: true, menuKey: '...' }`。
- [src/views/index.vue](src/views/index.vue) — 登录后的**主应用外壳**。使用 `<el-container>` 布局，顶部 `<el-header>` 包含 [AppSidebar.vue](src/components/AppSidebar.vue) 和用户信息。主内容区使用 `<component :is="currentView">` 根据侧边栏菜单动态切换视图——**并非**嵌套 `<router-view>`。
- [src/views/login.vue](src/views/login.vue) — 独立登录页。
- [src/App.vue](src/App.vue) — 根组件，仅渲染 `<router-view>`（区分为登录页或主外壳）。

### 状态管理

- **Pinia** + 持久化插件。Store 入口：[src/stores/index.js](src/stores/index.js)。
- `useSocketStore`（[src/stores/socket.js](src/stores/socket.js)）— 存放 SignalR 相关状态（在线用户、聊天、通知、全局错误等）。

### API 层

- [src/utils/request.js](src/utils/request.js) — 中央 Axios 实例。`baseURL` 来自 `window.__APP_CONFIG__.VITE_APP_API_BASE_URL`。请求拦截器自动从 localStorage 附加 `Bearer` Token。响应拦截器仅做透传错误日志。
- API 模块按业务域划分在 `src/api/` 下：
  - `system/` — 登录、用户、系统配置、字典
  - `device/` — 雷达、摄像头、标定、防区
  - `alarm/` — 告警查询、轨迹信息
  - `map/` — 地图数据、保存
  - `config/` — 应用配置项

### SignalR（实时通信）

项目中并存两套实现：

1. **旧版** — [src/signalr/signalr.js](src/signalr/signalr.js) 导出单例对象，提供 `init(url)` 和 `start()` 方法。消息通过 [src/signalr/analysis.js](src/signalr/analysis.js) 路由，将 `ReceiveTargetData` 和 `TrackTargetData` 分发到 Pinia socket store。

2. **新版（推荐）** — [src/utils/signalRUtils.js](src/utils/signalRUtils.js) 管理一个共享 `HubConnection`，采用发布/订阅模式。使用 `ensureSignalRConnection()` 获取或创建共享连接，`subscribeSignalR()` 订阅消息，`setSignalRReceiveEnabled()` 控制是否消费数据。主外壳 `index.vue` 在挂载时调用 `ensureSignalRConnection`，并根据当前激活视图切换数据接收状态。

### 认证与权限

- [src/utils/permission.js](src/utils/permission.js) — 本地基于角色的权限控制。预设三种角色：`admin`（全部权限）、`operator`（指定菜单）、`guest`（仅可查看地图）。
- 登录时 `buildAuthProfile()` 将服务端返回数据与本地预设合并，存入 localStorage 的 `authProfile` 键。
- `canAccessMenu(menuKey)` 控制侧边栏菜单显隐和视图切换。
- Token 存储在 localStorage 的 `token` 键（见 [src/utils/auth.js](src/utils/auth.js)）。

### GIS / 地图

- [src/utils/TrackManager.js](src/utils/TrackManager.js) — 核心类，管理 Leaflet 地图上的雷达目标轨迹。处理目标创建、折线轨迹、标记图标（人/车）、弹窗信息、超时目标自动清理、轨迹高亮。
- 地图视图位于 `src/views/map/`：`realtime_map.vue`、`historical map.vue`、`AutoMap.vue`/`AutoMap.js`、`play.vue`。
- 地图瓦片 URL 来自运行时配置 `VITE_MAP_TILE_MAP_URL`。

### 国际化

- [src/i18n/index.js](src/i18n/index.js) — 创建 `vue-i18n` 实例，默认中文（`zh`），支持英文（`en`）。`changeLanguage()` 将选择持久化到 localStorage 并同步更新 Element Plus 组件语言。
- [src/main.js](src/main.js) 中通过 `app.config.globalProperties` 暴露 `$t` 和 `$locale` 到全局。

### 全局插件

[src/plugins/index.js](src/plugins/index.js) 安装全局属性和方法：
- `auth.js` — 权限指令和辅助函数
- `cache.js` — session/local/cookie 缓存封装
- `modal.js` — Element Plus 消息/通知/加载封装
- `download.js` — 文件下载
- `tab.js` — 标签页管理

### 关键目录结构

```
src/
  api/          # 按业务域划分的 API 模块
  assets/       # 静态资源、图标字体、样式
  components/   # 公共组件（AppSidebar、Pagination、DictTag、SvgIcon 等）
  composables/  # Vue 组合式函数（usePreviewVisibility）
  i18n/         # 国际化配置与语言文件
  layout/       # （空，布局实际在 views/index.vue 中）
  plugins/      # 全局插件，通过 app.use() 安装
  router/       # Vue Router 路由配置
  signalr/      # 旧版 SignalR 实现
  stores/       # Pinia 状态管理
  utils/        # 工具函数：request、auth、permission、TrackManager、signalRUtils 等
  views/        # 页面组件
    components/   # 视图级公共子组件（ptz、starBackground、video_play）
    dataScreen/   # 大屏展示
    device/       # 设备管理（雷达、摄像头、防区、标定）
    livePreview/  # 实时视频预览
    map/          # GIS 地图视图（实时、历史、自动）
    query/        # 告警查询
    system/       # 系统配置、用户管理
```

## Docker 部署

多阶段构建：`node:20-alpine` 编译前端应用，`nginx:1.27-alpine` 提供静态文件服务。运行时配置通过 `docker/entrypoint/40-env-config.sh` 在 nginx 启动前将环境变量写入 `config.js`（即 `window.__APP_CONFIG__`）。
