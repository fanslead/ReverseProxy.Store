# ReverseProxy.Store

[English](README.md) | 简体中文

[![NuGet 下载量](https://img.shields.io/nuget/dt/ReverseProxy.Store)](https://www.nuget.org/packages/ReverseProxy.Store)
[![许可证](https://img.shields.io/github/license/fanslead/ReverseProxy.Store)](LICENSE)

ReverseProxy.Store 3.0 是面向 YARP 的版本化控制面。它使用 PostgreSQL 存储规范化代理配置，通过安全的 HTTP API 发布不可变版本，并借助 Redis 向运行节点分发版本通知。

> 3.0 当前为 `preview.1`，将有意替代 2.x 的可变实体与 CRUD 模型。

## 项目组成

- `ReverseProxy.Store`：规范化配置文档、验证与差异计算，以及支持版本感知的 YARP 配置提供器。
- `ReverseProxy.Store.EFCore`：兼容 PostgreSQL 的版本与控制面持久化模型。
- `ReverseProxy.Store.Distributed`：强类型 Redis 版本发布器与订阅器。
- `ReverseProxy.WebApi`：Identity Cookie 认证、CSRF 防护、配置变更 API、审计记录、健康检查和 YARP 运行时。
- `ReverseProxy.Dashboard`：生产级 SPA 宿主与同源 API 代理。
- `ReverseProxy.Dashboard/ClientApp`：基于 Vue 3、TypeScript、Pinia、Vite 和 Arco Design 的控制台。

## 管理控制台

Signal Plane 按运维任务组织控制面，而不是向用户暴露底层存储概念：

- **工作台 / 概览**：展示运行健康状态、生效版本、请求链路和进行中的配置变更。
- **配置管理 / 路由与集群**：展示当前生效的资源目录及其关联关系。生效资源为只读；新建或修改资源时会进入安全的配置变更流程。
- **变更与发布 / 配置变更**：提供表单与 JSON 编辑、服务端验证、差异审查、乐观并发控制和发布功能。
- **变更与发布 / 版本历史**：提供不可变发布快照审查，以及带生效版本并发保护的前向恢复。

响应式界面默认使用简体中文，并可随时切换为英文。

### 运行概览

![Signal Plane 运行概览](docs/images/dashboard-overview.png)

### 路由管理

![Signal Plane 路由管理](docs/images/dashboard-routes.png)

### 配置变更工作台

![Signal Plane 配置变更工作台](docs/images/dashboard-change-studio.png)

## 配置发布流程

1. 基于当前生效版本创建配置变更。
2. 使用表单编辑器或规范化 JSON 文档新增、修改路由和集群。
3. 运行服务端验证，并查看相对基线版本的差异。
4. 使用当前 `ETag` 发布；过期编辑会被拒绝，不会覆盖更新的配置。
5. 本地发布信号或 Redis 通知到达后，运行节点从 PostgreSQL 重新加载新的不可变版本。

主要 HTTP 接口如下：

| 功能 | 接口 |
| --- | --- |
| 身份认证 | `/api/v1/auth/antiforgery`、`/api/v1/auth/login`、`/api/v1/auth/me`、`/api/v1/auth/logout` |
| 生效运行配置 | `/api/v1/runtime/status`、`/api/v1/runtime/configuration` |
| 配置变更 | `/api/v1/drafts`、`/api/v1/drafts/{id}/validate`、`/api/v1/drafts/{id}/diff`、`/api/v1/drafts/{id}/publish` |
| 版本历史 | `/api/v1/revisions`、`/api/v1/revisions/{id}`、`/api/v1/revisions/{id}/rollback` |
| 配置操作 | `/api/v1/operations/{id}` |

## 发布模型

发布操作在 PostgreSQL 可序列化事务中执行。事务会写入不可变版本、移动生效版本指针、记录操作与审计条目，并追加一条强类型 outbox 事件。

回滚采用前向恢复：恢复历史文档时会创建编号更高的新版本，而不是倒退生效指针。调用方必须提供预期的当前生效版本 ID，因此并发发布会被拒绝而不会被覆盖。恢复 worker 会继续处理因进程中断而停留在 `Applying` 状态的操作。

Outbox worker 使用 `FOR UPDATE SKIP LOCKED` 领取记录，发布 `proxy-config.published` 事件，并在成功投递后确认。Redis 仅作为低延迟提示通道：每个订阅节点都会从 PostgreSQL 重新加载并验证权威生效版本。重复或乱序事件均可安全处理，定期对账机制会补偿 Redis 故障期间遗漏的通知。

## 环境要求

- .NET SDK 10.0
- Node.js 22 和 npm
- Docker Desktop，或自行管理的 PostgreSQL 17 与 Redis 7

## 本地开发

启动 PostgreSQL 和 Redis：

```bash
docker compose up -d
```

首次使用时信任 ASP.NET Core 开发证书，然后启动 API。仅当用户存储为空时才会使用引导管理员凭据。

```bash
dotnet dev-certs https --trust
ASPNETCORE_ENVIRONMENT=Development \
BootstrapAdmin__UserName=admin \
BootstrapAdmin__Password='replace-with-a-development-password' \
dotnet run --project ReverseProxy.WebApi
```

在另一个终端中启动 Vite 开发服务器：

```bash
npm --prefix ReverseProxy.Dashboard/ClientApp ci
npm --prefix ReverseProxy.Dashboard/ClientApp run dev
```

打开 `http://localhost:5173`。Vite 会将 `/api` 和 `/health` 代理到 `https://localhost:5201`。

如需验证生产模式的 SPA 宿主：

```bash
npm --prefix ReverseProxy.Dashboard/ClientApp run build
dotnet run --project ReverseProxy.Dashboard
```

打开 `http://localhost:5232`。

## 配置

在开发环境之外，请使用环境变量或密钥提供器。重要配置项如下：

```text
ConnectionStrings__Default
ConnectionStrings__Redis
Database__ApplyMigrationsOnStartup
BootstrapAdmin__UserName
BootstrapAdmin__Password
Redis__ChannelName
Redis__SubscriptionRetryDelay
Redis__ReconciliationInterval
Outbox__BatchSize
Outbox__PollInterval
Outbox__FailureDelay
Operations__BatchSize
Operations__PollInterval
AllowedHosts
```

单节点运行时可以不配置 `ConnectionStrings__Redis`。省略后，本地发布与重新加载仍可正常工作，outbox 记录也会保持持久化；后续启用 Redis 时会继续投递待处理记录。多节点部署时，应在每个节点上配置 Redis。

生产环境应将迁移作为部署步骤执行，并保持 `Database__ApplyMigrationsOnStartup=false`：

```bash
dotnet tool restore
ConnectionStrings__Default='Host=db;Database=reverseproxy_store;Username=reverseproxy;Password=replace-me' \
  dotnet ef database update \
  --project ReverseProxy.WebApi \
  --startup-project ReverseProxy.WebApi
```

Dashboard 会将 `/api` 和 `/health` 转发到控制面。可使用以下配置覆盖生产环境目标地址：

```text
ReverseProxy__Clusters__control-plane__Destinations__api__Address
```

## 验证

```bash
dotnet restore ReverseProxy.Store.sln
dotnet build ReverseProxy.Store.sln -c Release --no-restore
dotnet test tests/ReverseProxy.Store.Tests/ReverseProxy.Store.Tests.csproj -c Release --no-restore
npm --prefix ReverseProxy.Dashboard/ClientApp ci
npm --prefix ReverseProxy.Dashboard/ClientApp run typecheck
npm --prefix ReverseProxy.Dashboard/ClientApp run build
```

集成测试使用 Testcontainers，因此需要 Docker。

## AI 服务

[![Powered by OrcaRouter](https://img.shields.io/badge/Powered_by-OrcaRouter-2563eb)](https://www.orcarouter.ai/ref/ref_293cafb35044abc53d15)
