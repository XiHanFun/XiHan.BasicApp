# 快速开始

本页把 6.0 在本机跑起来：后端连本地 PostgreSQL，前端开发服务器代理到后端，用初始超级管理员登录。

## 环境要求

.NET 10 SDK、Node 24+、pnpm 11+、PostgreSQL 14+，以及与 BasicApp 同级检出的 XiHan.Framework。逐项说明见[开发环境](./dev-environment)。

## 一、克隆代码

```bash
git clone -b next https://github.com/XiHanFun/XiHan.BasicApp.git
git clone https://github.com/XiHanFun/XiHan.Framework.git
# 框架检出到 backend-next/props/framework.props 里锁定的提交
git -C XiHan.Framework checkout 26bdff62
```

两个目录必须并排放在同一个上级目录下（`backend-next` 按相对路径 `../../XiHan.Framework/next/src` 找框架源码）。

## 二、准备数据库

```bash
createdb -h 127.0.0.1 -U postgres xihan_basicapp_next
```

`backend-next/src/XiHan.BasicApp.Host/appsettings.Development.json` 已把 `XiHan:Bindings:db` 指向这个库。本机账号不同时，用环境变量 `XiHan__Bindings__db__value` 覆盖连接串，不要把真实密码写进仓库。

## 三、启动后端

在仓库根：

```bash
dotnet restore backend-next/XiHan.BasicApp.slnx
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development
```

启动时依次发生：

1. 装配全部能力包；数据库绑定缺失会以 `RUNTIME_VALIDATION_001` 中断启动。
2. Development 打开 `InitTables`，按已登记的实体建缺表、加缺列。
3. 表结构升级器执行 `Schema/6.0.0/*.sql` 并写版本台账，失败即中断启动（`BasicApp:Schema:AutoUpgrade`，缺省开）。
4. 监听 `http://localhost:9710`（HTTP）与 `http://127.0.0.1:9711`（gRPC），浏览器打开 `/scalar` 接口文档。

## 四、写入种子

种子**不会**随启动自动执行，另开一个终端运行 `seed` 命令（命令模式用同一套装配，但不监听端口，可以与正在运行的宿主并存）：

```bash
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development -- seed
```

它按号段顺序执行各包登记的种子，可重复执行：

| 种子 | 写入 |
| --- | --- |
| `identity.platform` | 平台账号 `superadmin` |
| `access.platform` | 平台内置角色 `admin` 与超管的全局授予 |
| `access.menus` | 从各包页面登记对齐菜单 |
| `tenancy.editions` | 套餐 Free（缺省）/ Basic / Professional / Enterprise |
| `files.storage-configs` | 平台本地存储配置 |
| `notification.templates` | 平台消息模板 |
| `access.policies` | 访问策略的平台缺省 |
| `tenancy.demo` · `organization.demo` | 演示租户 `demo` 与其部门、岗位（只在 `BasicApp:Seed:EnableDemoData` 为真时，Development 缺省开） |

`seed --list` 只列出不执行，`seed --only identity.platform` 只跑指定的种子。

## 五、启动前端

```bash
cd frontend-next
pnpm install --no-frozen-lockfile
pnpm dev
```

浏览器打开 `http://localhost:9100`。开发服务器把 `/api`、`/hubs` 等路径代理到 `http://localhost:9710`，所以前后端同源、没有跨域问题。

## 六、登录

| 项 | 值 |
| --- | --- |
| 租户 | 留空（平台账号） |
| 用户名 | `superadmin` |
| 密码 | `SuperAdmin@123` |

::: warning 首次登录后修改密码
`SuperAdmin@123` 是方案公开的缺省值，来自 `BasicApp:Seed:SuperAdminPassword`（留空即用缺省）。部署前在配置里换掉，或登录后在个人中心修改。
:::

登录成功后落到工作台（`VITE_HOME_PATH=/workbench/dashboard`）。要体验租户侧，有三种办法：

- 用租户 `demo` 的管理员 `admin` 登录（初始密码见 `DemoTenantSeeder`，首次登录须改密）；
- 在租户管理里把自己加为 `demo` 的运维支持成员，再从控制中心切换过去（`auth.switch-tenant` 只能进注册地租户、生效中的成员租户或平台）；
- 用模仿登录以租户用户的身份进入（`impersonation.start`）。

## 七、调通第一个接口

图形验证码缺省开启（`BasicApp:Auth:CaptchaEnabled`）。用命令行调试时，可以在本机配置里临时关掉，或先取验证码：

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
    "sessionId": "0193…",
    "tenantId": null,
    "mustChangePassword": false,
    "user": { "id": "0193…", "userName": "superadmin" }
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

失败时返回 `application/problem+json`，按 `code` 判断原因。完整的令牌、刷新、切换租户流程见[接口对接指南](./api-guide)。

## 常见问题

- **启动即退出，日志里有 `RUNTIME_VALIDATION_001`**：没有数据库连接串，检查 `XiHan:Bindings:db:value`。
- **构建报找不到 `XiHan.Framework.*` 工程**：框架没有检出在同级目录，或用 `-p:XiHanFrameworkSrc=…` 指路。
- **登录提示账号或密码错误**：还没执行 `seed`。
- **前端页面空白、控制台 404**：后端没起来，或 `VITE_DEV_PROXY_TARGET` 与后端地址不一致。

更多见[常见问题](./faq)。

## 下一步

- [目录结构](./project-structure) · [后端开发流程](./backend/development) · [前端开发流程](./frontend/development)
