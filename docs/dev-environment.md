# 开发环境

本页说明本机开发所需的工具、框架检出、数据库准备、端口与常用命令，适用于参与后端或前端开发的开发者。

## 前置条件

| 工具 | 版本 | 用途 |
| --- | --- | --- |
| .NET SDK | 10.x | 后端构建、测试与运行（目标框架 `net10.0`） |
| Node.js | 24+ | 前端与文档站 |
| pnpm | 11+ | 前端包管理（`frontend` 没有锁文件，安装用 `--no-frozen-lockfile`） |
| Git | 任意近期版本 | 检出 BasicApp 与 XiHan.Framework 两个仓库 |
| PostgreSQL | 14+（CI 用 `postgres:17-alpine`） | 平台库；独立库租户另建库 |

开发只要求 PostgreSQL。Redis、RabbitMQ、对象存储都不是必需：不配连接时缓存与消息走框架的进程内实现，文件落本地目录 `App_Data/files`。要在本机联调 Redis / RabbitMQ，在 `appsettings.Development.json` 打开绑定 `cache` / `queue` 的注释即可（见[配置参考](./configuration#分布式缓存与消息)）。

## 框架检出

后端缺省以 ProjectReference 引用框架源码，路径是与 BasicApp 同级的 `XiHan.Framework/framework/src`：

```text
<工作目录>/
├─ XiHan.BasicApp/          # dev 分支
│  ├─ backend/
│  └─ frontend/
└─ XiHan.Framework/         # 检出 backend/props/framework.props 锁定的提交
   └─ framework/src/
```

```bash
git clone -b dev https://github.com/XiHanFun/XiHan.BasicApp.git
git clone https://github.com/XiHanFun/XiHan.Framework.git
commit=$(grep -oP '(?<=<XiHanFrameworkCommit>)[0-9a-f]+' XiHan.BasicApp/backend/props/framework.props)
git -C XiHan.Framework checkout "$commit"
```

`backend/props/framework.props` 决定引用方式：

| 属性 | 作用 |
| --- | --- |
| `XiHanFrameworkCommit` | 锁定的框架提交；架构测试 `FrameworkPinTests` 要求同级检出存在 `framework/src` 且 HEAD 以它开头 |
| `XiHanFrameworkSrc` | 框架源码目录，缺省同级的 `XiHan.Framework/framework/src/`；框架在别处时用 `-p:XiHanFrameworkSrc=<检出>/framework/src/` 指路 |
| `UseXiHanFrameworkSource` | 缺省按 `XiHanFrameworkSrc` 下有没有框架源码自动判定；`-p:UseXiHanFrameworkSource=false` 改用 NuGet 包（版本取 `XiHanFrameworkPackageVersion`） |

找不到源码时构建会自动改用 NuGet 包，公开面可能与锁定提交不一致，`FrameworkPinTests` 也会失败；日常开发与 CI 都走锁定提交的源码。同级检出停在别的提交、又不想切换时，可以用 `git archive` 把锁定提交导出成快照，再以 `-p:XiHanFrameworkSrc=<快照>/framework/src/` 构建（做法见后端技能 `.agents/skills/backend-development/references/backend.md`），快照下只有 `FrameworkPinTests` 失败属预期。

## PostgreSQL

`appsettings.Development.json` 指向本机 `127.0.0.1:5432` 的 `xihan_basicapp_next` 库，账号为开发用占位值。先建库：

```bash
createdb -h 127.0.0.1 -U postgres xihan_basicapp_next
```

连接串写在 `XiHan:Bindings:db:value`，库类型写在同一节的 `provider`（缺省 `postgresql`）。本机账号不同时用环境变量或用户机密覆盖，不要改 `appsettings.Development.json`，也不要提交真实密码：

```bash
# 环境变量覆盖（节名以双下划线分隔）
export XiHan__Bindings__db__value="Host=127.0.0.1;Port=5432;Database=xihan_basicapp_next;Username=<用户>;Password=<密码>"
```

Development 打开了 `XiHan:Persistence:SqlSugar:InitTables`：启动时按已登记的实体建缺表、加缺列，改了实体重启即生效。正式环境由 `Schema/<版本>/*.sql` 管理表结构，见[表结构升级](./backend/upgrade)。

## 可选服务

邮件、短信、对象存储、群机器人与 Telegram 都在后台页面里配置（邮件配置、短信配置、存储配置、机器人配置、Telegram 机器人），开发环境不需要预装服务。知识库要 Qdrant，在 `appsettings.Development.json` 打开 `BasicApp:AI:Qdrant` 的注释。见[配置参考](./configuration)。

开发配置打开了自助注册，登录页有「立即注册」入口，注册的是不带角色的平台账号。第三方登录的回调经前端开发服务器（`BasicApp:Auth:OAuth:CallbackBaseUrl` 是 `http://localhost:9100`），在第三方后台登记的回调地址是 `http://localhost:9100/api/oauth/<Name>/callback`。客户端 ID 与密钥用用户机密写进 `BasicApp:Auth:OAuth:Providers`，不要写进配置文件；配好重启后端，登录页就出现对应按钮：

```bash
cd backend/src/XiHan.BasicApp.Host
dotnet user-secrets set "BasicApp:Auth:OAuth:Providers:0:Name" "github"
dotnet user-secrets set "BasicApp:Auth:OAuth:Providers:0:ClientId" "<客户端 ID>"
dotnet user-secrets set "BasicApp:Auth:OAuth:Providers:0:ClientSecret" "<客户端密钥>"
```

开发配置放行了回环地址（`XiHan:Security:Outbound:AllowLoopback`），本机的模型端点、MinIO 与 Webhook 调试服务可以直接连。

## 端口一览

| 进程 | 地址 | 来源 |
| --- | --- | --- |
| 后端 HTTP | `http://localhost:9710` | `appsettings.json` 的 `Kestrel:Endpoints:Http` |
| 后端 gRPC（HTTP/2） | `http://127.0.0.1:9711` | `Kestrel:Endpoints:Grpc`，只监听本机 |
| 前端开发服务器 | `http://localhost:9100` | `frontend/.env` 的 `VITE_PORT` |
| 文档站 | `http://localhost:5173` | VitePress 缺省 |

前端开发服务器把 `/api`、`/openapi.json`、`/mcp`、`/health`、`/blobs` 与 `/hubs`（WebSocket）代理到 `VITE_DEV_PROXY_TARGET`（缺省 `http://localhost:9710`）。

## 常用命令

```bash
# 后端（仓库根）
dotnet restore backend/XiHan.BasicApp.slnx
dotnet build backend/XiHan.BasicApp.slnx -c Release --no-restore
dotnet test --solution backend/XiHan.BasicApp.slnx -c Release --no-build
dotnet run --project backend/src/XiHan.BasicApp.Host --launch-profile Development
dotnet run --project backend/src/XiHan.BasicApp.Host --launch-profile Development -- seed
dotnet run --project backend/src/XiHan.BasicApp.Host -- operations          # 列出操作 ID、暴露面与权限
dotnet run --project backend/src/XiHan.BasicApp.Host -- contracts export --out "$PWD/frontend/contracts"
```

```bash
# 前端（frontend/）
pnpm install --no-frozen-lockfile
pnpm dev
pnpm gen:contracts   # 由 contracts/openapi.json 生成 packages/contracts/*
pnpm check           # type-check → lint → px → token → i18n → modules → tests → headers → contracts → permissions
pnpm test
pnpm build
```

`dotnet run` 以工程目录为工作目录，路径参数（如 `--out`）一律给绝对路径。

测试走 Microsoft.Testing.Platform（仓库根 `global.json`）：解决方案必须用 `--solution` 传入，要报告时加 `-- --report-xunit-trx`。连外部服务的用例读环境变量，没设即跳过：

| 变量 | 用途 |
| --- | --- |
| `XIHAN_TEST_POSTGRES` | 对真实 PostgreSQL 跑的用例，只写到服务器级、不含库名（如 `Host=127.0.0.1;Port=5432;Username=postgres;Password=<密码>`） |
| `XIHAN_TEST_REDIS` / `XIHAN_TEST_RABBITMQ` | 分布式缓存与消息的用例 |
| `XIHAN_TEST_S3` | S3 兼容存储的用例 |
| `XIHAN_TEST_MYSQL` / `XIHAN_TEST_SQLSERVER` / `XIHAN_TEST_ORACLE` | 代码生成读外部库的用例 |

```bash
XIHAN_TEST_POSTGRES='Host=127.0.0.1;Port=5432;Username=postgres;Password=<密码>' \
  dotnet test --solution backend/XiHan.BasicApp.slnx -c Release --no-build
```

## 工作区解决方案

同时改 BasicApp 与框架时，可以在两个仓库的上级目录建一个工作区解决方案（文件名自定），登记 `backend` 全部工程与框架 `framework/src` 的工程。它不入任何仓库，新增工程时自己补进去，否则 IDE 沿用旧的 `obj`。

## 下一步

- [快速开始](./getting-started) · [目录结构](./project-structure) · [配置参考](./configuration)
