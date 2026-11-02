# 部署

本页说明 6.0 后端与前端的发布、首次初始化、反向代理配置与部署限制，适用于搭建评估或预发环境。

::: warning 预览版
6.0 尚未发布，下面是 `next` 分支当前的部署形态，适合评估与预发环境。生产系统请继续使用 5.x。
:::

## 环境要求

| 项 | 要求 |
| --- | --- |
| 运行时 | ASP.NET Core 10 运行时（或自包含发布） |
| 数据库 | PostgreSQL 14+；独立库租户另需建库权限或预建库 |
| 静态托管 | 任意能托管单页应用并做反向代理的 Web 服务器（Nginx、Caddy、IIS…） |
| 可选 | Redis 与 RabbitMQ（配了连接才用，见[配置参考](./configuration#分布式缓存与消息)）；邮件 / 短信服务、S3 兼容对象存储、Telegram（都在后台页面里配置） |

单实例零外部依赖：缓存、消息走框架内存实现，文件落本地目录。多实例部署见文末。

## 后端：发布

框架以源码引用，发布机上同样要有锁定提交的 XiHan.Framework 检出（与 BasicApp 并排）：

```bash
dotnet publish backend-next/src/XiHan.BasicApp.Host -c Release -o ./publish/basicapp
```

发布目录里带着 `Schema/<版本>/*.sql`、`IpDatabases/*.xdb`（IP 归属地离线库）与 `appsettings.json`。

### 生产配置清单

用环境变量或密钥存储提供，不要写进镜像或仓库：

| 键 | 说明 |
| --- | --- |
| `XiHan__Bindings__db__value` | 平台库连接串（必需） |
| `XiHan__Security__Jwt__Secret` | HS256 密钥，至少 32 字节（必需） |
| `XiHan__ObjectStorage__SignedUrls__Secret` | 本地存储签名地址密钥（用本地存储时必需） |
| `BasicApp__Seed__SuperAdminPassword` | 超级管理员初始密码，替换缺省的 `SuperAdmin@123` |
| `BasicApp__DataProtection__KeysDirectory` | 数据保护密钥目录，持久化存储；多实例共享 |
| `BasicApp__Files__LocalRootPath` | 本地存储根目录，挂持久卷 |
| `BasicApp__OpenPlatform__Issuer` | 对外的 OIDC 签发者（反向代理后必须显式配置） |
| `BasicApp__Auth__OAuth__CallbackBaseUrl` | 第三方登录回调的后端对外地址 |
| `XiHan__Bindings__cache__value` | Redis 连接（多实例时配；不配即进程内缓存） |
| `XiHan__Bindings__queue__value` | RabbitMQ 连接（AMQP URI；不配即进程内消息） |

Kubernetes 也可以经 Service Binding 提供三个连接：`SERVICE_BINDING_ROOT` 下的 `db`、`cache`、`queue` 目录，各放 `type` 与 `uri` 文件（`db` 另可放 `provider`）。`appsettings.json` 不写这几个绑定，挂进来的 Secret 不会被配置文件盖掉。

`ASPNETCORE_ENVIRONMENT=Production` 时 `InitTables` 关闭、`/openapi.json` 与 `/scalar` 不映射（可用 `BasicApp:ApiDocs:Enabled` 显式打开）、演示数据不写。

### 首次初始化

1. 建空库（6.0 不接受 5.x 的库，见[表结构升级](./backend/upgrade)）。
2. 启动宿主：表结构升级器执行 `Schema/6.0.0/` 基线并写版本台账；失败即中断启动。也可以先单独执行 `XiHan.BasicApp.Host schema upgrade`。
3. 执行一次种子：`XiHan.BasicApp.Host seed`（命令模式不监听端口，可在宿主运行时执行；可重复执行）。
4. 用 `superadmin` 登录，立即修改密码，再建租户与租户管理员。

```bash
# 发布目录中执行
dotnet XiHan.BasicApp.Host.dll schema upgrade
dotnet XiHan.BasicApp.Host.dll seed
dotnet XiHan.BasicApp.Host.dll
```

### 健康检查

| 端点 | 用途 |
| --- | --- |
| `/health/startup` | 启动探针 |
| `/health/ready` | 就绪探针：含各 Feature 与 Provider 健康项 |
| `/health/live` | 存活探针 |

配了 Redis / RabbitMQ 时就绪探针多出 `caching.redis.connection` / `messaging.rabbitmq.connection`：启动时连不上即启动失败，运行中断开只记 Degraded，应用照常服务。

宿主目录里的 `xihan.app.json` 是框架打包规范的应用清单：镜像 `ghcr.io/xihanfun/xihan-basicapp:6.0.0-preview.1`、端口 `http 9710` / `grpc 9711`、绑定 `db`、启动探针 60 秒、停机宽限 30 秒与资源请求。清单只列了必需的 `db`；要让打包工具一并生成 Redis 与 RabbitMQ，在 `bindings` 里加上 `{ "name": "cache", "type": "cache:distributed" }` 与 `{ "name": "queue", "type": "queue" }`。

### 维护模式

升级共享库期间，其他节点经 `MaintenanceModeMiddleware` 对除健康检查与 `/.well-known/` 之外的请求返回 503（`RUNTIME_UNAVAILABLE_003`，带 `Retry-After`）。

## 前端：构建与部署

```bash
cd frontend-next
pnpm install --no-frozen-lockfile
pnpm build          # 产物在 dist/
```

构建期变量见[配置参考](./configuration#前端-env)：`VITE_API_BASE_URL` 留空即同源；`VITE_ROUTER_HISTORY=history` 时静态托管要把未知路径回退到 `index.html`。

推荐与后端同源部署：由同一个站点托管 `dist/` 并把后端路径反向代理过去，不必开跨域。前端放在别的源时，在 `BasicApp:Cors:AllowedOrigins` 列出前端的来源（见[配置参考](./configuration#basicapp-cors)）。

```nginx
server {
    listen 80;
    server_name admin.example.com;
    root /var/www/basicapp;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # 操作、文件、OAuth、Telegram Webhook
    location /api/ {
        proxy_pass http://127.0.0.1:9710;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        client_max_body_size 100m;
    }

    # 本地存储的签名地址
    location /blobs/ {
        proxy_pass http://127.0.0.1:9710;
        proxy_set_header Host $host;
    }

    # SignalR（WebSocket）
    location /hubs/ {
        proxy_pass http://127.0.0.1:9710;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_read_timeout 300s;
    }

    location /health/ {
        proxy_pass http://127.0.0.1:9710;
    }
}
```

`client_max_body_size` 与 `BasicApp:Files:MaxUploadBytes`（缺省 100 MB）保持一致；大文件走分片上传，分片经签名地址直传存储。

::: tip 真实客户端 IP
宿主还原 `X-Forwarded-For` / `-Proto` / `-Host`，但只信本机回环。反向代理不在本机时，把它的地址或网段写进 `BasicApp:ForwardedHeaders:KnownProxies` / `KnownNetworks`（见[配置参考](./configuration#basicapp-forwardedheaders)），否则访问日志、登录地点、会话 IP 与访问策略的 IP / 地区条件看到的都是代理地址。
:::

## gRPC

gRPC 端点（`Kestrel:Endpoints:Grpc`，HTTP/2）只供内部服务调用，只发布显式 `Expose.Grpc` 的操作。保持只监听内网地址，不要经反向代理暴露到公网；`.proto` 从 `GET /operations.proto` 取。

CloudEvents 入站端点 `POST /events`（`BasicApp:CloudEvents:Enabled`，缺省关）同理：只收持有 `runtime.cloud-events.deliver` 的调用方（平台的服务账号或服务角色，见[配置参考](./configuration#basicapp-cloudevents)），只开放给内网，上面的 Nginx 示例不代理它。

## 多实例部署

| 方面 | 单实例（不配连接） | 多实例 |
| --- | --- | --- |
| 缓存 | 进程内实现 | 配 Redis（`XiHan__Bindings__cache__value`）：授权规则、会话门禁、验证码、登录节流、开放接口防重放等共享缓存跨实例一致，写入方的失效对所有实例生效 |
| 消息 | 进程内代理 | 配 RabbitMQ（`XiHan__Bindings__queue__value`）：发件箱派发与 CloudEvents 发布模式经它，各实例按主题竞争消费；队列由订阅方声明，没人订阅的主题（如框架的 `scheduling.*` 事件）发布即丢弃 |
| 实时推送 | 进程内的连接登记与组 | 配 Redis 即跨实例：SignalR 接 Redis 背板，在线状态汇总各实例写进共享缓存的连接；强制下线时别的实例上的连接经背板移出全部组，再配 RabbitMQ 时还由所在实例断开（见[实时推送](./backend/realtime#跨实例)） |
| 数据库译文 | 平台库 `message_translation` | 各实例按 `XiHan:Localization:Database:RefreshIntervalSeconds` 各自重载 |
| 作业 | `Scheduling.Database` | 同左，按框架的数据库调度协调 |
| 数据保护 | 本机密钥目录 | 必须共享 `BasicApp:DataProtection:KeysDirectory` |
| 表结构升级 | 升级器取数据库租约锁 | 同左，同一时刻只有一个节点执行 |

因此多实例部署配 Redis 即可让缓存与实时推送跨实例；强制下线要立刻断开别的实例上的连接，还要配 RabbitMQ。

## 下一步

- [配置参考](./configuration) · [健康与可观测性](./backend/health-observability) · [表结构升级](./backend/upgrade)
