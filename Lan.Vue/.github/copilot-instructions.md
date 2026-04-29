## 目的

提供给 AI 编码代理（Copilot / 代码助理）的一份精简、可执行的项目上下文说明，帮助快速理解本仓库的架构、常用工作流、约定和关键集成点。

## 快速上下文（几句话）

- 这是一个基于 Vue 3 + Vite 的前端应用（单页面），在 `src/` 下按功能组织。主要功能是地图/雷达可视化与实时目标跟踪，使用 Leaflet、SignalR 进行实时通信与可视化。

## 关键组件与边界

- 前端入口与路由： [src/main.js](src/main.js) 、 [src/router/index.js](src/router/index.js)
- 状态管理：Pinia，stores 在 [src/stores/](src/stores)
- API 层：后端接口封装在 [src/api/](src/api)（按域划分，如 device、map、system 等），调用样式为 `import { listRadar } from '@/api/device/radar'`。
- 地图与实时：地图页面在 [src/views/map/realtime_map.vue](src/views/map/realtime_map.vue)（Leaflet + Geoman + leaflet.motion），轨迹逻辑封装在 [src/utils/TrackManager.js](src/utils/TrackManager.js)
- 配置与运行时替换：运行时配置通过 `public/config.js` 暴露 `window.__APP_CONFIG__`（例如 `VITE_SIGNALR_URL`, `VITE_MAP_TILE_MAP_URL`）。参考 [public/config.js](public/config.js)

## 典型数据流与集成点

- 后端/API 调用：axios 在各 `src/api/*` 文件内使用，返回数据通常为 `response.data.data`。修改接口时请检查对应调用点。
- 实时数据流：使用 SignalR 客户端连接（@microsoft/signalr），在 [src/views/map/realtime_map.vue](src/views/map/realtime_map.vue) 的 `init()` 中创建 connection 并绑定消息处理函数（示例：`connection.on('ReceiveTargetData', handler)`）。
- 地图瓦片：地图瓦片 URL 从 `window.__APP_CONFIG__.VITE_MAP_TILE_MAP_URL` 读取，生产可替换此值而无需重构代码。
- 本地播放器：组件 `LocalPlayerWindow` 用于 RTSP/摄像头预览，相关唯一键生成方式见 `openLocalPlayerPreview()`（base64 编码拼接）。

## 项目约定与模式（可被 AI 利用）

- Vue 风格：大量使用 `<script setup>` 单文件组件与组合式 API。请用相同风格添加新组件。
- 全局代理：组件内常用 `const { proxy } = getCurrentInstance()` 调用全局方法（`proxy.$modal`, `proxy.getConfigKey` 等），修改这类调用需保留 `proxy` 语义。
- 异步模式：接口调用通常返回 Promise，调用方直接 `.then()` 处理，不总是使用 async/await；保持现有风格以减少不必要改动。
- 地图图层清理：组件销毁时必须移除 map 和停止定时器（见 onBeforeUnmount）。新增图层请确保被记录并在 clear/销毁时移除。

## 开发 / 测试 / 调试 工作流

- Node 要求：见 package.json engines，推荐 Node 20.x 或 >=22.12
- 常用命令（package.json）:
  - `npm run dev`：本地开发（vite）
  - `npm run build`：打包
  - `npm run preview`：本地预览构建产物
  - `npm run lint`：eslint 修复
  - `npm run format`：prettier 格式化
- 环境与运行时：大多数可替换配置通过 `public/config.js` 在运行时注入，修改无需重启构建（只需刷新页面并保证 config.js 可访问）。

## 变更建议与安全边界（AI 行为守则）

- 不要随意更改公共 API 封装（`src/api/*`）的返回契约。若确实需要修改，更新所有调用点并运行本地应用验证。
- 对实时逻辑（SignalR、TrackManager、leaflet layer）进行改动时，务必处理好资源释放（定时器、map.remove、connection.stop()）。
- 新增依赖前请确认与现有 Leaflet / Vite 版本兼容（Leaflet 需在浏览器环境运行）。

## 代码修改与提交建议（给自动化编码代理）

- 使用仓库内既有样式和工具：ESLint/Prettier 已配置。修改后运行 `npm run lint` 和 `npm run format`。
- 小改动直接通过 patch 提交，复杂改动先在分支上实现并运行 `npm run dev` 手动验证地图页面交互和 SignalR 连接。

## 首次阅读优先文件（按顺序）

- [src/views/map/realtime_map.vue](src/views/map/realtime_map.vue) — 地图与实时逻辑集中处，理解此文件可快速掌握实时流向与 UI 交互。
- [src/utils/TrackManager.js](src/utils/TrackManager.js) — 轨迹管理的封装，新增目标跟踪/清理逻辑请在此扩展。
- [src/api/](src/api) — 后端接口聚合位置，按目录查看 device/map/system 等子模块。
- [public/config.js](public/config.js) — 运行时配置源。
- [package.json](package.json) 和 [vite.config.js](vite.config.js) — 构建与依赖信息。

---

如果这份说明有遗漏或你希望我补充某些细节（比如某个 API 的返回示例、TrackManager 的方法契约、或 CI/CD 流程），请指出要补充的部分，我会迭代更新。

# Copilot 使用说明（为 AI 编码代理定制）

下面的说明帮助 AI 代理快速在本仓库中开展工作：包含整体架构要点、关键运行命令、项目约定、以及常见集成点和示例引用。

1. 项目概览

- 框架：Vue 3 + Vite（参见 `package.json` 脚本）。前端状态管理使用 `pinia`，UI 使用 `element-plus`。
- 地图与实时：主要基于 `leaflet` 及 `@geoman-io/leaflet-geoman-free`，并通过 `@microsoft/signalr` 接收实时目标数据。
- 目录要点：
  - 地图与实时逻辑：[src/views/map/realtime_map.vue](src/views/map/realtime_map.vue)（雷达扇形绘制、SignalR 处理、轨迹管理示例）。
  - 公共 API 分层：[src/api/\*\*](src/api)（分模块的后端接口封装，如 device/radar、map/map 等）。
  - 工具与管理器：[src/utils/TrackManager.js](src/utils/TrackManager.js)（轨迹管理抽象；代理应调用其方法而不是直接操作图层）。
  - i18n：国际化入口在 [src/i18n](src/i18n)。

2. 启动 / 构建 / 代码质量（显式命令）

- 安装依赖：`npm install`
- 本地开发：`npm run dev`（Vite dev server）
- 生产构建：`npm run build`
- 预览构建产物：`npm run preview`
- 静态检查：`npm run lint`
- 代码格式化：`npm run format`
- Node 要求：参见 `package.json` engines（推荐 Node 20+）。

3. 运行时与配置约定（项目特有）

- 运行时配置通过覆盖 `public/config.js` 注入前端全局变量（示例在代码中通过 `window.__APP_CONFIG__` 读取）。地图瓦片和 SignalR URL 从该对象读取：
  - `VITE_MAP_TILE_MAP_URL` 用作地图瓦片模板（参见 realtime_map.vue 中的 mapUrl）。
  - `VITE_SIGNALR_URL` 用作 SignalR 连接地址。
- 不要假设存在 .env 文件——运行时常通过容器或构建时脚本（docker/entrypoint）生成 `public/config.js`。

4. 实时与跨组件通信模式

- SignalR：使用 `connection.on('<消息名>', handler)` 接收消息，调用方通常在 [src/views/map/realtime_map.vue](src/views/map/realtime_map.vue) 的 `init()` 中建立连接。
- 轨迹管理：`TrackManager` 提供 `processRadarData`, `setTrackTarget`, `getAllTargets`, `clearAll` 等方法。所有实时数据的转换/聚合优先封装到 `TrackManager`，避免重复图层操作。

5. 常见代码模式与约定（可直接举例给 AI）

- 后端接口：遵循模块化导出（例如 `src/api/device/radar` 有 `listRadar`, `updateLatLng`, `updateRadar`），优先调用这些方法而非直接构造 URL。
- 地图图层操作：尽量通过 `trackManager` 或在 `map.value` 上封装的 helper 操作图层；示例：在 [src/views/map/realtime_map.vue](src/views/map/realtime_map.vue) 中，`ints()` 函数负责绘制雷达扇形与中心标记。
- 弹窗/提示：项目中使用 `proxy.$modal` 与 `proxy.$t`（国际化），在自动化修改 UI 文本或测试时应保留这些调用以保持一致性。

6. 修改/添加功能时的注意事项

- 若修改实时处理路径，先查看并复用 `TrackManager` 的钩子（onTargetAdded / onTargetUpdated / onTargetRemoved）以保持列表和图层同步。
- 地图中心与缩放设置通过后端配置 key `mapCenter` 和 `mapZoom` 管理，realtime_map.vue 使用 `proxy.getConfigKey` 读取并用 `updateConfig` 保存；修改这些逻辑请同时更新 config API 使用方式。
- 对绘制（leaflet-geoman）交互的改动会影响后端存储（addDrawPolygon / delDrawPolygon / listDrawPolygon），变更时请同时检查这些 API。

7. 调试与常见问题线索

- 若地图瓦片不显示，先检查 [public/config.js](public/config.js) 的 `VITE_MAP_TILE_MAP_URL` 是否正确，或在浏览器网络面板查看瓦片请求 URL。
- SignalR 连接问题：查看浏览器控制台是否有 CORS 或 404，SignalR 地址为 `VITE_SIGNALR_URL`，可在 `realtime_map.vue` 的 init() 中添加日志。
- 性能：大量目标/轨迹使用 `TrackManager` 的历史长度与清理超时（cleanupTimeout）控制，调整这些参数比直接删除图层更高效。

8. 例子片段（参考）

- 绘制雷达扇形的关键函数：`ints(lat, lon, radius, startAngle, endAngle, color, fillLength, cameraIp, username, password, cameraURL, radarIp)`，见 [src/views/map/realtime_map.vue](src/views/map/realtime_map.vue)。
- SignalR 初始化：查看 `init(api, acceptMsg, sendMsg)` 在同一文件。

9. 提交/PR 建议（给 AI 的行为准则）

- 变更前运行 `npm run lint` 并确保没有格式化冲突。
- 对涉及地图与实时逻辑的变更，附带运行说明和复现步骤（如何复现 SignalR 数据、如何配置本地 `public/config.js`）。

如果以上信息有遗漏或你希望我把某些代码片段加入说明（例如 `TrackManager` 的完整 API 列表或关键行数引用），请告诉我，我会迭代更新此文件。
