# 开发环境

本页列出在本机开发 6.0 所需的工具、仓库布局、数据库准备与常用命令，适用于参与后端或前端开发的开发者。

## 前置条件

| 工具 | 版本 | 用途 |
| --- | --- | --- |
| .NET SDK | 10.x | 后端构建、测试与运行（`backend-next` 目标框架 `net10.0`） |
| Node.js | 24+ | 前端与文档站 |
| pnpm | 11+ | 前端包管理（`frontend-next` 没有锁文件，安装用 `--no-frozen-lockfile`） |
| Git | 任意近期版本 | 检出 BasicApp 与 XiHan.Framework 两个仓库 |
| PostgreSQL | 14+（CI 用 17） | 平台库；独立库租户另建库 |

6.0 开发态只要求 PostgreSQL。Redis、RabbitMQ、对象存储都不是必需：宿主装的是框架 Full Profile，缓存、消息、对象存储走框架的内存或文件系统缺省实现（文件缺省落本地目录 `App_Data/files`）。

## 两个仓库并排检出

`backend-next` 以 ProjectReference 引用框架源码，缺省路径是与 BasicApp 同级的 `XiHan.Framework/next/src`：

```text
<工作目录>/
├─ XiHan.BasicApp/          # 切到 next 分支
│  ├─ backend-next/
│  └─ frontend-next/
└─ XiHan.Framework/         # 检出 backend-next/props/framework.props 锁定的提交
   └─ next/src/
```

```bash
git clone -b next https://github.com/XiHanFun/XiHan.BasicApp.git
git clone https://github.com/XiHanFun/XiHan.Framework.git
```

锁定提交写在 `backend-next/props/framework.props` 的 `XiHanFrameworkCommit`（撰写时为 `26bdff62`）。把框架检出到这个提交：

```bash
git -C XiHan.Framework checkout 26bdff62
```

架构测试 `FrameworkPinTests` 会核对框架检出的 HEAD 是否以锁定提交开头；检出不在锁定上时这条测试失败，其余测试照常。框架不在同级目录时，用 `-p:XiHanFrameworkSrc=<检出>/next/src/` 显式指路。

::: tip NuGet 包引用
框架 5.0 还没有发布到 nuget.org。`-p:UseXiHanFrameworkSource=false` 可以切到 `5.0.0-preview.*` 包引用（本地源 `next/artifacts/packages`），但日常开发与 CI 都走源码引用。
:::

## PostgreSQL

`appsettings.Development.json` 指向本机 `127.0.0.1:5432` 的 `xihan_basicapp_next` 库，账号为开发用占位值。先建库：

```bash
createdb -h 127.0.0.1 -U postgres xihan_basicapp_next
```

连接串写在 `XiHan:Bindings:db:value`，库类型写在同一节的 `provider`（缺省 `postgresql`）。本机账号不同就在 `appsettings.Development.json` 之外用环境变量或用户机密覆盖，不要提交真实密码：

```bash
# 环境变量覆盖（节名以双下划线分隔）
export XiHan__Bindings__db__value="Host=127.0.0.1;Port=5432;Database=xihan_basicapp_next;Username=<用户>;Password=<密码>"
```

Development 打开了 `XiHan:Persistence:SqlSugar:InitTables`：启动时按已登记的实体建缺表、加缺列。正式环境由 `Schema/<版本>/*.sql` 管理表结构，见[表结构升级](./backend/upgrade)。

## 可选：Telegram、邮件、短信、对象存储

这些都在运行时由后台页面配置（邮件配置、短信配置、存储配置、Telegram 机器人），不需要在开发环境预装服务。第三方登录的客户端 ID 与密钥写在 `BasicApp:Auth:OAuth:Providers`，见[配置参考](./configuration)。

## 端口一览

| 进程 | 地址 | 来源 |
| --- | --- | --- |
| 后端 HTTP | `http://localhost:9710` | `appsettings.json` 的 `Kestrel:Endpoints:Http` |
| 后端 gRPC（HTTP/2） | `http://127.0.0.1:9711` | `Kestrel:Endpoints:Grpc`，只监听本机 |
| 前端开发服务器 | `http://localhost:9100` | `frontend-next/.env` 的 `VITE_PORT` |
| 文档站 | `http://localhost:5173` | VitePress 缺省 |

前端开发服务器把 `/api`、`/openapi.json`、`/mcp`、`/health`、`/blobs` 与 `/hubs`（WebSocket）代理到 `VITE_DEV_PROXY_TARGET`（缺省 `http://localhost:9710`）。

## 常用命令

```bash
# 后端（仓库根）
dotnet restore backend-next/XiHan.BasicApp.slnx
dotnet build backend-next/XiHan.BasicApp.slnx -c Release --no-restore
dotnet test --solution backend-next/XiHan.BasicApp.slnx -c Release --no-build
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development
```

```bash
# 前端（frontend-next/）
pnpm install --no-frozen-lockfile
pnpm dev
pnpm check
pnpm test
pnpm build
```

测试走 Microsoft.Testing.Platform：解决方案必须用 `--solution` 传入。需要对真实 PostgreSQL 跑的用例读环境变量 `XIHAN_TEST_POSTGRES`（只写到服务器级，不含库名），没设置时这些用例跳过：

```bash
XIHAN_TEST_POSTGRES='Host=127.0.0.1;Port=5432;Username=postgres;Password=<密码>' \
  dotnet test --solution backend-next/XiHan.BasicApp.slnx -c Release --no-build
```

## 工作区解决方案

同时改 BasicApp 与框架时，可以在两个仓库的上级目录建一个工作区解决方案（如 `XiHanFun.Next.Local.slnx`），登记 `backend-next` 全部工程与框架 `next/src`。它不入任何仓库，新增工程时自己补进去。

## 下一步

- [快速开始](./getting-started) · [目录结构](./project-structure) · [配置参考](./configuration)
