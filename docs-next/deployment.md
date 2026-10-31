# 部署

::: warning 预览版
6.0 尚未发布，下面是 `next` 分支当前的部署形态，适合评估与预发环境。生产系统请继续使用 5.x。
:::

## 环境要求

| 项 | 要求 |
| --- | --- |
| 运行时 | ASP.NET Core 10 运行时（或自包含发布） |
| 数据库 | PostgreSQL 14+；独立库租户另需建库权限或预建库 |
| 静态托管 | 任意能托管单页应用并做反向代理的 Web 服务器（Nginx、Caddy、IIS…） |
| 可选 | 邮件 / 短信服务、S3 兼容对象存储、Telegram（都在后台页面里配置） |

单实例开发态零外部依赖：缓存、消息走框架内存实现，文件落本地目录。多实例部署见文末的限制。

## 后端：发布

框架以源码引用，发布机上同样要有锁定提交的 XiHan.Framework 检出（与 BasicApp 并排）：

```bash
dotnet publish backend-next/src/XiHan.BasicApp.Host -c Release -o ./publish/basicapp
```

发布目录里带着 `Schema/<版本>/*.sql`、`IpDatabases/*.xdb`（IP 归属地离线库）与 `appsettings.json`。

### 生产配置清单

用环境变量或密钥存储提供，**不要写进镜像或仓库**：

| 键 | 说明 |
| --- | --- |
| `XiHan__Bindings__db__value` | 平台库连接串（必需） |
| `XiHan__Security__Jwt__Secret` | HS256 密钥，至少 32 字节（必需） |
| `XiHan__ObjectStorage__SignedUrls__Secret` | 本地存储签名地址密钥（用本地存储时必需） |
| `BasicApp__Seed__SuperAdminPassword` | 超管初始密码，替换缺省的 `SuperAdmin@123` |
| `BasicApp__DataProtection__KeysDirectory` | 数据保护密钥目录，持久化存储；多实例共享 |
| `BasicApp__Files__LocalRootPath` | 本地存储根目录，挂持久卷 |
| `BasicApp__OpenPlatform__Issuer` | 对外的 OIDC 签发者（反向代理后必须显式配置） |
| `BasicApp__Auth__OAuth__CallbackBaseUrl` | 第三方登录回调的后端对外地址 |

`ASPNETCORE_ENVIRONMENT=Production` 时 `InitTables` 关闭、`/openapi.json` 与 `/scalar` 不映射（可用 `BasicApp:ApiDocs:Enabled` 显式打开）、演示数据不写。

### 首次初始化

1. 建空库（6.0 不接受 5.x 的库，见[表结构升级](./backend/upgrade)）。
2. 启动宿主：表结构升级器执行 `Schema/6.0.0/` 基线并写版本台账；失败即中断启动。也可以先单独执行 `XiHan.BasicApp.Host schema upgrade`。
3. 执行一次种子：`XiHan.BasicApp.Host seed`（命令模式不监听端口，可在宿主运行时执行；可重复执行）。
4. 用 `superadmin` 登录，**立即修改密码**，再建租户与租户管理员。

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

宿主目录里的 `xihan.app.json` 是框架打包规范的应用清单：镜像 `ghcr.io/xihanfun/xihan-basicapp:6.0.0-preview.1`、端口 `http 9710` / `grpc 9711`、绑定 `db`、启动探针 60 秒、优雅停机 30 秒与资源请求。

### 维护模式

升级共享库期间，其他节点经 `MaintenanceModeMiddleware` 对除健康检查与 `/.well-known/` 之外的请求返回 503（`RUNTIME_UNAVAILABLE_003`，带 `Retry-After`）。

## 前端：构建与部署

```bash
cd frontend-next
pnpm install --no-frozen-lockfile
pnpm build          # 产物在 dist/
```

构建期变量见[配置参考](./configuration#前端-env)：`VITE_API_BASE_URL` 留空即同源；`VITE_ROUTER_HISTORY=history` 时静态托管要把未知路径回退到 `index.html`。

后端没有配置 CORS，前端**应与后端同源**部署：由同一个站点托管 `dist/` 并把后端路径反向代理过去。

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

::: warning 真实客户端 IP
宿主目前没有启用转发头处理（`UseForwardedHeaders`）。放在反向代理后面时，访问日志、登录地点、会话 IP 与访问策略的 IP / 地区条件看到的都是代理地址。在这项补上之前，访问策略里不要依赖 IP 条件做放行判断。
:::

## gRPC

gRPC 端点（`Kestrel:Endpoints:Grpc`，HTTP/2）只供内部服务调用，只发布显式 `Expose.Grpc` 的操作。保持只监听内网地址，不要经反向代理暴露到公网；`.proto` 从 `GET /operations.proto` 取。

## 多实例的限制

| 方面 | 现状 |
| --- | --- |
| 实时推送 | Realtime 用进程内的连接登记与组，**单实例成立**；多实例时在线状态只看本实例，推送到不了别的实例上的连接 |
| 缓存 | 宿主未装 Redis Provider，`ISharedCache` 是进程内实现，各实例不共享 |
| 作业 | 作业定义与运行记录在 `Scheduling.Database`，多实例按框架的数据库调度协调 |
| 数据保护 | 必须共享 `BasicApp:DataProtection:KeysDirectory` |
| 表结构升级 | 升级器取数据库租约锁，同一时刻只有一个节点执行 |

因此 6.0 预览版建议**单实例**部署。

## 下一步

- [配置参考](./configuration) · [健康与可观测性](./backend/health-observability) · [表结构升级](./backend/upgrade)
