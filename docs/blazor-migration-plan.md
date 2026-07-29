# Vue 3 → Blazor Server 全量迁移方案

## Context

将 `Lan.Vue`（Vue 3 + Element Plus + Leaflet + Three.js）前端完整迁移到 Blazor Server（.NET 10），后端 API + SignalR Hub 保持不变。核心约束：**业务零变化、技术细节尽量不动**。

## 现状盘点

### 前端文件总览

| 类别 | 文件数 | 总代码量 | Leaflet 强依赖 |
|------|--------|---------|---------------|
| 地图页面 | 5 | ~2,600 行 | ✅ 全部 |
| Three.js 3D | 10 | ~1,100 行 | ❌ |
| CRUD 页面 | 7 | ~4,500 行 | ❌ |
| 仪表盘 | 2 | ~1,077 行 | ❌ |
| 登录/布局/组件 | 7 | ~700 行 | ❌ |
| 工具类（纯 JS） | 4 | ~1,140 行 | TrackManager+mapUtils 依赖 Leaflet |
| API 模块 | 17 | 93 个函数 | ❌ |
| Store | 3 | ~200 行 | ❌ |
| 插件 | 5 | ~400 行 | ❌ |
| SignalR | 2 | ~400 行 | ❌ |
| **合计** | **~55** | **~13,000 行** | |

### 核心技术栈映射

| Vue | → | Blazor |
|-----|---|--------|
| Vue 3 Composition API | → | Razor Components + `@code` |
| Element Plus | → | MudBlazor |
| Pinia + persistedstate | → | ProtectedLocalStorage / Scoped Service |
| Vue Router (lazy routes) | → | Blazor Routing (`@page`) |
| Axios + interceptors | → | HttpClient + DelegatingHandler |
| Leaflet + Geoman | → | **保留 JS，通过 IJSRuntime 互操作** |
| TrackManager.js (396行) | → | **零改动保留** |
| mapUtils.js (351行) | → | **零改动保留** |
| Three.js (radar3d) | → | **零改动保留** |
| Canvas (calibration) | → | **零改动保留** |
| SignalR @microsoft/signalr | → | **双 Hub：Blazor circuit + 自定义 MapData Hub** |

---

## 核心架构决策

### 前端 JS 层：TypeScript

`wwwroot/js/` 下所有互操作代码用 TypeScript 编写，`tsconfig.json` 编译输出 `.js`。TrackManager.js、mapUtils.js 等现有文件可直接迁移为 `.ts`（加类型标注），新写的 `mapInterop.ts` 也用 TS。

```
wwwroot/js/
├── tsconfig.json           # target: ES2020, module: ESM, outDir: ./
├── mapInterop.ts           # 统一的 Blazor ↔ Leaflet 互操作
├── TrackManager.ts         # 从 .js 迁移，加类型
├── mapUtils.ts             # 从 .js 迁移，加类型
├── signalrClient.ts        # JS 端独立 SignalR 连接
└── radar3d/                # Three.js 模块
```

编译：`tsc --project wwwroot/js/tsconfig.json`，可加入 `.csproj` 的 `BeforeBuild` 目标自动执行。

TrackManager.js 和 mapUtils.js 经确认是**纯 JS、零 Vue 依赖、仅依赖 Leaflet**。直接保留 .js 文件，放在 `wwwroot/js/` 下，Blazor 通过 `IJSRuntime` 调用。

```
Blazor Component (.razor)          JS (wwwroot/js/)
┌──────────────────────┐     ┌─────────────────────────┐
│  <div id="map-xxx">  │     │  TrackManager.js        │
│  MudDrawer (配置面板) │     │  mapUtils.js            │
│  MudButton (工具栏)   │     │  AutoMap.js             │
│                      │     │  radar3d/*.js (Three.js)│
│  IJSRuntime ─────────┼────→│  calibration canvas     │
│  DotNetObjectRef ←───┼─────│  (回调 C# 方法)          │
└──────────────────────┘     └─────────────────────────┘
```

### SignalR 策略：双 Hub 并存

| Hub | 用途 | 连接方 |
|-----|------|--------|
| Blazor Circuit（内置） | UI 渲染、组件事件 | 自动 |
| `/hubs/stock`（现有 MessageHub） | 雷达目标数据、TrackTargetData | **JS 直接连接**（绕过 Blazor 互操作层） |

JS 直接连 SignalR Hub，收到数据后直接调 TrackManager 更新 Leaflet 图层。不走 `Server → C# → IJSRuntime → JS` 的绕路，延迟零增加。

### UI 组件库：MudBlazor

直接对标 Element Plus：

| Element Plus | MudBlazor | 用途 |
|-------------|-----------|------|
| `el-table` | `MudDataGrid` / `MudTable` | 设备列表、告警表格 |
| `el-form` + `el-input` | `MudForm` + `MudTextField` | 参数配置表单 |
| `el-dialog` / `el-drawer` | `MudDialog` / `MudDrawer` | 编辑弹窗、地图控制面板 |
| `el-select` | `MudSelect` | 下拉选择 |
| `el-slider` | `MudSlider` | 雷达角度/半径调节 |
| `el-radio-group` | `MudRadioGroup` | 启停开关 |
| `el-tag` | `MudChip` | 状态标签 |
| `el-tree` | `MudTreeView` | 防区树形过滤 |
| `el-date-picker` | `MudDatePicker` | 日期范围选择 |
| `el-message` / `el-notification` | `MudSnackbar` / `MudAlert` | 操作提示 |
| `el-pagination` | `MudTablePager`（内置） | 分页 |
| `el-transfer` | 手写或 MudList 自定义 | 设备绑定 |

### 权限策略：完全复用

`permission.js` 的角色权限逻辑直接翻译为 C# `PermissionService`，注入组件。`localStorage` 改为 Blazor 的 `ProtectedLocalStorage`。

---

## 迁移阶段

### 第 0 阶段：项目骨架（1 天）

1. 在解决方案中新建 `Lan.Blazor` 项目（Blazor Server, .NET 10）
2. 添加 NuGet 包：`MudBlazor`、`Microsoft.AspNetCore.SignalR.Client`
3. 配置 `Program.cs`：`AddMudServices()`、`AddServerSideBlazor()`、`MapBlazorHub()`、`MapHub<MessageHub>("/hubs/stock")`
4. 迁移现有 JS 文件到 `wwwroot/js/`，转为 `.ts` 并加类型标注：
   - `TrackManager.ts`、`mapUtils.ts`、`AutoMap.ts`
   - `signalrClient.ts`（独立 SignalR 客户端）
   - `radar3d/` 目录（8 个文件）、`radar3dConverter.ts`
5. 配置 `tsconfig.json`，`tsc` 编译输出 `.js`
6. 复制 Leaflet CSS、图标等静态资源
7. `_Host.cshtml` 中引用所有编译后的 JS/CSS

### 第 1 阶段：登录 + 布局 + 路由（1 天）

**新建文件：**
- `Pages/_Host.cshtml` — Blazor Server 入口
- `Components/Layout/MainLayout.razor` — `MudAppBar` + `MudDrawer` + 动态内容区
- `Components/Layout/NavMenu.razor` — 权限控制的侧边导航
- `Pages/Auth/Login.razor` — 登录页，调用现有 `api/login`
- `Services/AuthService.cs` — Token 管理（ProtectedLocalStorage）
- `Services/PermissionService.cs` — 角色权限检查（翻译 permission.js）
- `Services/CustomAuthStateProvider.cs` — Blazor 认证状态
- `Services/ApiHttpClient.cs` — HttpClient + Bearer Token DelegatingHandler

**路由表：**
```
/login          → Login.razor              (public)
/               → MainLayout.razor          (requires auth)
/radar          → RadarList.razor
/camera         → CameraList.razor
/defencearea    → DefenceAreaList.razor
/calibration    → Calibration.razor
/realtime_map   → RealtimeMap.razor
/livePreview    → LivePreview.razor
/historical_map → HistoricalMap.razor
/radar3d        → Radar3D.razor
/autoMap        → AutoMap.razor
/alarm          → AlarmQuery.razor
/config         → ConfigList.razor
/user           → UserList.razor
/screen         → DataScreen.razor
/play           → Play.razor
```

### 第 2 阶段：CRUD 页面（2-3 天）

逐个翻译 Vue CRUD 页面为 Blazor + MudBlazor：

| Vue 页面 | Blazor 页面 | 复杂度 | 备注 |
|----------|------------|--------|------|
| `system/user/index.vue` (213行) | `Pages/System/UserList.razor` | 简单 | 3 字段表单，无 wizard |
| `system/config/index.vue` (229行) | `Pages/System/ConfigList.razor` | 简单 | 3 字段表单 |
| `device/radar/index.vue` (642行) | `Pages/Device/RadarList.razor` | 中等 | wizard 模式 |
| `device/camera/index.vue` (1110行) | `Pages/Device/CameraList.razor` | 中等 | 30 字段表单，wizard |
| `device/defencearea/index.vue` (641行) | `Pages/Device/DefenceAreaList.razor` | 中等 | 双 el-transfer |
| `query/alarm_index.vue` (408行) | `Pages/Query/AlarmQuery.razor` | 中等 | 树形过滤 |

**通用模式：**
```razor
@* 每个 CRUD 页面结构 *@
<MudTable Items="@items" ServerData="@LoadData" SortLabel="@_sortLabel">
    <ToolBarContent>...</ToolBarContent>
    <HeaderContent>...</HeaderContent>
    <RowTemplate>...</RowTemplate>
</MudTable>

<MudDialog>
    <DialogContent>
        <MudForm @ref="_form">
            <MudTextField ... />
        </MudForm>
    </DialogContent>
</MudDialog>
```

**Wizard 模式**：通过 `NavigationManager` + query string 传递步骤状态，不再使用 `localStorage + CustomEvent`。

**Device binding (el-transfer)**：用 `MudList` + checkbox 自定义双栏穿梭。

### 第 3 阶段：地图页面（核心，3-4 天）

这是整个迁移最关键的部分。策略：**Blazor 提供 HTML 外壳 + UI 控件，Leaflet 完全由 JS 驱动**。

#### 3.1 创建 mapInterop.ts（`wwwroot/js/mapInterop.ts`）

统一的 JS 互操作入口：

```javascript
window.mapInterop = {
  maps: {},            // 所有 Leaflet 地图实例
  trackManagers: {},   // 所有 TrackManager 实例

  initRealtimeMap: function(id, options) { ... },  // 初始化实时地图
  initLivePreviewMap: function(id, options) { ... }, // 初始化预览地图
  initHistoricalMap: function(id, options) { ... },  // 初始化历史地图
  initAutoMap: function(id, options) { ... },        // 初始化瓦片下载地图

  // 扇区操作
  addSectors: function(id, sectors) { ... },      // ints() 创建扇区
  updateSector: function(id, sector) { ... },     // 更新单个扇区
  removeSectors: function(id) { ... },            // 清除所有扇区

  // 多边形绘制
  enableDrawing: function(id, mode) { ... },      // 启用 Geoman 绘制
  disableDrawing: function(id) { ... },

  // SignalR 数据连接（JS 直接连）
  connectSignalR: function(id, hubUrl, dotNetRef) { ... },

  destroy: function(id) { ... }                   // 清理
};
```

#### 3.2 RealtimeMap.razor — 最复杂的地图页面

```
┌─────────────────────────────────────────────────────────┐
│ MudAppBar (顶部工具栏)                                   │
│  [雷达选择] [坐标] [防区半径滑块] [角度滑块] [保存/刷新]    │
├────────────────────────────────────────────┬────────────┤
│                                            │ MudDrawer  │
│   <div id="map-realtime">                  │ (右侧抽屉)  │
│     Leaflet 地图                            │ 多边形类型  │
│     - 扇区多边形 + 可拖拽标记                │ 选择对话框  │
│     - Geoman 绘制控件                       │ 视频弹窗    │
│     - TrackManager 轨迹                     │             │
│   </div>                                   │             │
│                                            │             │
└────────────────────────────────────────────┴────────────┘
```

**数据流（关键！）：**
```
雷达设备 → TCP → RadarClientManager → TargetCollection.AddTarget
  → SignalRSender.SendFrameIfNeeded → MessageHub.SendAsync("ReceiveTargetData", json)
    → JS SignalR 客户端 (mapInterop.connectSignalR) → TrackManager.processRadarData()
      → Leaflet 图层更新（零 Blazor 参与）
```

**C# 只负责：**
- 加载雷达配置、多边形数据（API 调用）
- 用户修改扇区参数 → 调用 `updateRadar` API → `JSRuntime.InvokeVoidAsync("mapInterop.addSectors", ...)`
- Geoman 绘制完成回调（JS → DotNetObjectReference → C# 调 API 保存多边形）
- 扇区标记拖动完成回调（JS → DotNetObjectReference → C# 调 `updateLatLng` API）

#### 3.3 其他地图页面

| 页面 | 策略 |
|------|------|
| LivePreview.razor | `MudGrid` 布局（4/6/9 格），第一格嵌 Leaflet 地图，其余格用 `LocalPlayerWindow`（通过 JS interop 发送窗口消息） |
| HistoricalMap.razor | 读 URL 参数 → API 加载轨迹 → JS 调 `leaflet.motion` 动画 |
| AutoMap.razor | 复用 AutoMap.js，Blazor 只做瓦片下载 UI（文件夹选择、进度条） |

### 第 4 阶段：3D 雷达 + 标定（2-3 天）

**Radar3D.razor：** 完全保留 Three.js 代码，Blazor 提供工具栏 UI（雷达选择、暂停/播放、视角切换、参数滑块）。

```
<MudToolBar> 雷达选择 | 目标计数 | 暂停/播放 | 视角预设 | 参数滑块 </MudToolBar>
<div id="radar3d-container"> ← Three.js 渲染在这里，JS 完全控制
```

**Calibration.razor：** 保留 3 层 Canvas 渲染 + PTZ 控制组件。SignalR 数据连接由 JS 处理。

### 第 5 阶段：仪表盘 + 收尾（1 天）

- `DataScreen.razor` — 保留 ECharts（JS），Blazor 提供布局
- `Home.razor` — 保留 ECharts
- `Play.razor` — WebSocket + MSE，纯 JS
- 清理旧 `Lan.Vue` 项目

---

## 风险点与缓解

| 风险 | 缓解措施 |
|------|---------|
| Leaflet 互操作延迟 | JS 直接连 SignalR Hub，数据流不经过 IJSRuntime |
| Circuit 断开导致地图丢失 | JS 维护独立 SignalR 连接 + 自动重连 |
| Three.js 无法 Blazor 化 | 完全不碰，保留纯 JS |
| MudBlazor API 差异 | CRUD 模式确定化，每个页面结构一致 |
| 并发用户多时 Circuit 内存 | Blazor Server 每个用户 ~250KB，评估服务器容量 |

## 验证方式

1. 每个阶段完成后 `dotnet build` 零错误
2. 第 2 阶段：对比 Vue 和 Blazor 的 CRUD 操作截图
3. 第 3 阶段：雷达数据上线 → 对比 Vue 和 Blazor 地图的目标轨迹是否一致
4. 全量回归：登录 → 设备管理 → 防区配置 → 实时地图 → 告警查询 → 3D 视图
