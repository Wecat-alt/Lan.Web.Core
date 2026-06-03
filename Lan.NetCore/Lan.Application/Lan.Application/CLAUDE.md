# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 构建与运行

- 解决方案: `Lan.Application/Lan.Application.sln`
- 目标框架: .NET 10.0, x64 only
- 构建: `dotnet build Lan.Application/Lan.Application.sln`
- 运行: `dotnet run --project Lan.Application/Lan.Application/Lan.Application.csproj`
- 发布 (Windows x64): `dotnet publish -c Release -r win-x64`
- 发布 (Linux x64): `dotnet publish -c Release -r linux-x64`

## 项目架构

这是一个**雷达安防监控系统**，通过雷达设备进行周界监控，集成 ONVIF 摄像头进行录像，并通过 SignalR 实时推送目标数据到前端。

### 分层结构（自底向上）

```
Lan.Model          — 领域模型、DTO、VO、分页模型
Lan.NsrRadarSdk    — NsrRadar 雷达 SDK（TCP/UDP Socket 通信协议）
Lan.Infrastructure — 基础设施（BaseController、配置、ONVIF/RBTrack 原生 SDK 封装、地理计算、异常处理）
Lan.Repository     — 数据访问层（SqlSugar ORM + MySQL），Repository<T> 泛型基类
Lan.ServiceCore    — 业务逻辑层（Service、BackgroundService、SignalR Hub、设备管理单例）
Lan.Application    — Web API 层（Controllers、Program.cs、Swagger）
```

### 依赖链路

`Application` → `ServiceCore` → `Repository` → `Infrastructure` + `Model`
`NsrRadarSdk` 被 `ServiceCore` 通过 DLL 引用调用

### 核心设计模式

- **`[AppService]` 特性自动注册 DI**: `ServiceExtensions.AddService()` 扫描 `Lan.Repository` 和 `Lan.ServiceCore` 程序集，自动将带有 `[AppService]` 特性的类注册到 DI 容器（支持 Transient/Scoped/Singleton）。
- **Repository 模式**: 所有 Service 继承 `Repository<T>`（泛型），后者继承 `DbContext<T>`（封装 SqlSugarClient）。Service 同时承担业务逻辑和数据访问。
- **Manager 单例模式**: `CameraManager`、`DefenceAreaManager`、`RadarManager` 采用 `GetInstance()` 静态方法管理设备生命周期。
- **Producer-Consumer**: `AlarmBackgroundService` 使用 `BlockingCollection<AlarmEvent>` 收集报警事件；`RadarDataChannelService` 使用 Channel 处理跟踪数据；`Worker` 使用 `ConcurrentQueue<SendMS>` 批量发送 SignalR 消息。
- **按月分表**: `trackinfo` 表按 `yyyyMM` 后缀自动分表（`trackinfo202501`），在 `Repository<T>.BatchInsertAsync()` 中实现。

### 关键业务流程

1. **雷达数据流**: 雷达设备 → `RadarManager` → `TargetCollection.AddTarget()` → 解析目标、坐标转换、多边形过滤 → 写入报警队列和跟踪数据 Channel
2. **报警流程**: `AlarmBackgroundService` 消费报警队列 → 开始录像 → 生成 `AlarmModel` 记录 → 通过 SignalR `MessageHub` 推送弹窗消息
3. **实时推送**: `Worker` 后台服务每 10ms 从 `ConcurrentQueue` 去重并推送 `SendMS` 到 SignalR Hub (`ReceiveTargetData`)

### 原生 SDK 集成

项目严重依赖 Windows x64 原生 DLL：
- **ONVIF SDK** (`onvifSdk/x64/OnvifClient.dll`) — 摄像头 PTZ 控制
- **NovaPlayer SDK** (`NovaPlayer/x64/`) — 视频解码和录像
- **RBTrack SDK** (`RBTrackSdk/x64/RBTrack.dll`) — 目标跟踪算法
- **RadarToLonLat.dll** — 雷达坐标转经纬度
- 这些 DLL 在 `Program.cs` 中通过 `NativeLibrary.SetDllImportResolver` 动态加载，Linux 下使用 `.so` 文件

### 数据库

- MySQL 数据库，连接字符串在 `appsettings.json` → `ConnectionStrings:conn`
- 使用 SqlSugarCore ORM，`InitKeyType = InitKeyType.Attribute`（通过特性标记主键）
- 日志使用 NLog + Serilog
