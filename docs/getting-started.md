# 快速开始

本页说明如何在本机运行：后端连接本地 PostgreSQL，前端开发服务器代理到后端，再以初始超级管理员登录。适用于首次运行项目的开发者。

## 环境要求

.NET 10 SDK、Node.js 24+、pnpm 11+、PostgreSQL 14+。后端以 NuGet 包引用 XiHan.Framework，不需要检出框架源码。逐项说明见[开发环境](./dev-environment)。

## 一、克隆代码

```bash
git clone -b dev https://github.com/XiHanFun/XiHan.BasicApp.git
```

要连框架源码一起调试，见[框架引用](./dev-environment#框架引用)。

## 二、准备数据库

只要本机 PostgreSQL 在跑。`backend/src/XiHan.BasicApp.Host/appsettings.Development.json` 已把 `XiHan:Bindings:db` 指向 `xihan_basicapp` 库（账号为开发用占位值），并打开了 `BasicApp:Schema:CreateDatabase`：库不存在时启动先建库，连接账号要有建库权限。账号没有建库权限时先手工建：

```bash
createdb -h 127.0.0.1 -U postgres xihan_basicapp
```

本机账号不同时，用环境变量 `XiHan__Bindings__db__value` 或用户机密覆盖连接串，不要把真实密码写进仓库。

## 三、启动后端

在仓库根：

```bash
dotnet restore backend/XiHan.BasicApp.slnx
dotnet run --project backend/src/XiHan.BasicApp.Host --launch-profile Development
```

启动时依次发生：

1. 装配全部能力包；数据库绑定缺失即以 `RUNTIME_VALIDATION_001` 中断启动。
2. Development 打开了 `InitTables`，按已登记的实体建缺表、加缺列。
3. 表结构升级器执行 `Schema/6.0.0/*.sql` 并写版本台账，失败即中断启动（`BasicApp:Schema:AutoUpgrade`，缺省开）；库不存在时先建库（`BasicApp:Schema:CreateDatabase`，Development 打开）。
4. 执行一遍种子，失败即中断启动（`BasicApp:Seed:RunOnStartup`，Development 打开），见下一节。
5. 监听 `http://localhost:9710`（HTTP）与 `http://127.0.0.1:9711`（gRPC），浏览器打开 `/scalar` 接口文档。

## 四、写入种子

Development 每次启动都执行一遍种子（`BasicApp:Seed:RunOnStartup`），首次运行后即可登录。别的环境缺省不随启动执行，用 `seed` 命令（命令模式用同一套装配，但不监听端口，可以与正在运行的宿主并存）：

```bash
dotnet run --project backend/src/XiHan.BasicApp.Host --launch-profile Development -- seed
```

它按号段顺序执行各包登记的种子，可重复执行：

| 种子 | 写入 |
| --- | --- |
| `identity.platform` | 平台账号 `superadmin` |
| `access.platform` | 平台内置角色 `admin`（通配授予 `*`），并把 `superadmin` 指派进去 |
| `access.menus` | 从各包页面登记对齐菜单 |
| `tenancy.editions` | 套餐免费版（缺省）/ 基础版 / 专业版 / 企业版 |
| `files.storage-configs` | 平台本地存储配置 |
| `settings.configs` | 各包登记的内建参数（如登录方式），元数据每次对齐，值归运营 |
| `notification.templates` | 平台消息模板 |
| `access.policies` | 访问策略的平台缺省 |
| `codegeneration.templates` | 代码生成的内置模板（每次对齐） |
| `settings.demo` · `tenancy.demo` · `organization.demo` · `access.demo` · `access.demo-policies` · `workflow.demo` · `notification.demo` · `printing.demo` | 平台演示字典，演示租户 `demo` 及其管理员与成员、部门、岗位、角色与访问策略，`demo` 里的四个演示流程，平台与 `demo` 的草稿通知，平台的演示打印模板。只在 `BasicApp:Seed:EnableDemoData` 为真时写（Development 缺省开） |

`seed --list` 只列出不执行，`seed --only identity.platform` 只跑指定的种子。可选模块关掉时，它的种子不登记。

## 五、启动前端

```bash
cd frontend
pnpm install --no-frozen-lockfile
pnpm dev
```

浏览器打开 `http://localhost:9100`。开发服务器把 `/api`、`/hubs` 等路径代理到 `http://localhost:9710`，前后端同源，没有跨域问题。

## 六、登录

| 项 | 值 |
| --- | --- |
| 租户 | 留空（平台账号） |
| 用户名 | `superadmin` |
| 密码 | `SuperAdmin@123` |

::: warning 首次登录后修改密码
`SuperAdmin@123` 只写在 `appsettings.Development.json` 的 `BasicApp:Seed:SuperAdminPassword` 里。别的环境必须配这一项，否则首次 `seed` 失败。
:::

登录后落到工作台（`VITE_HOME_PATH=/workbench/dashboard`）。要体验租户侧：

- 从控制中心切换到 `demo`：演示种子已把 `superadmin` 加为 `demo` 的成员（`auth.switch-tenant` 只能进注册地租户、生效中的成员租户或平台）；
- 用租户 `demo` 的管理员 `admin` 登录，初始密码 `Demo@123456`，首次登录须改密；
- 用模仿登录以租户用户的身份进入（`impersonation.start`）。

## 七、调用第一个接口

图形验证码缺省开启（`BasicApp:Auth:CaptchaEnabled`）。用命令行调试时，可以在本机配置里临时关掉，或先调 `auth.captcha` 取验证码：

```bash
# 1. 登录方式与是否要求验证码
curl http://localhost:9710/api/auth:login-config

# 2. 登录（验证码关闭时不需要 captchaId / captchaCode）
curl -X POST http://localhost:9710/api/auth:login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"superadmin","password":"SuperAdmin@123"}'
```

成功的响应正文直接是 `LoginResponse`，没有信封：

```json
{
  "token": {
    "accessToken": "<JWT>",
    "accessExpiresAt": "2026-10-31T12:00:00Z",
    "refreshToken": "<刷新令牌>",
    "refreshExpiresAt": "2026-11-07T10:00:00Z",
    "sessionId": "0193…",
    "tenantId": null,
    "mustChangePassword": false,
    "user": { "id": "0193…", "userName": "superadmin", "…": "…" }
  },
  "twoFactor": null
}
```

带上访问令牌调一个分页操作：

```bash
curl -X POST http://localhost:9710/api/users:list \
  -H 'Authorization: Bearer <JWT>' \
  -H 'Content-Type: application/json' \
  -d '{"page":{"index":1,"size":20}}'
```

失败时返回 `application/problem+json`，按 `code` 判断原因。令牌、刷新、切换租户的完整流程见[接口对接指南](./api-guide)。

## 常见问题

- 启动即退出，日志里有 `RUNTIME_VALIDATION_001`：没有数据库连接串，检查 `XiHan:Bindings:db:value`。
- 用 `XiHanFun.Local.slnx` 构建时 `FrameworkPinTests` 失败或报框架工程找不到：同级 XiHan.Framework 没有检出在锁定提交上，见[框架引用](./dev-environment#框架引用)。
- 登录提示账号或密码错误：种子还没执行（非 Development 要先跑 `seed`）。
- 前端页面空白、控制台 404：后端没起来，或 `VITE_DEV_PROXY_TARGET` 与后端地址不一致。

更多见[常见问题](./faq)。

## 下一步

- [目录结构](./project-structure) · [后端开发流程](./backend/development) · [前端开发流程](./frontend/development)
