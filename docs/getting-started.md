# 快速开始

本篇带你在**本地**把 XiHan.BasicApp 的后端与前端跑起来。跟着做，约 10 分钟即可登录系统。

## 环境要求

| 依赖 | 版本 | 说明 |
| --- | --- | --- |
| .NET SDK | **10.0+** | 后端运行时 |
| Node.js | **24.0+** | 前端构建 |
| pnpm | **11.0+** | 前端包管理器 |
| PostgreSQL | **14+** | 数据库（也支持 MySQL / MariaDB） |
| Redis | **6.0+** | 分布式缓存与分布式锁 |

> 需要先准备好一个可连接的 PostgreSQL 和 Redis 实例——还没准备的话，先看上一篇 [开发环境](./dev-environment)，用 Docker 命令行几分钟即可备齐。数据库表结构无需手动创建，首次启动会自动建表并初始化种子数据。

## 一、克隆代码

```bash
git clone https://github.com/XiHanFun/XiHan.BasicApp.git
cd XiHan.BasicApp
```

## 二、配置数据库连接

编辑 `backend/src/main/XiHan.BasicApp.WebHost/appsettings.Development.json`，填入你的连接串：

```json
{
  "XiHan": {
    "Data": {
      "SqlSugarCore": {
        "ConnectionConfigs": [
          {
            "DbType": "PostgreSQL",
            "ConnectionString": "Host=localhost;Port=5432;Database=xihan_basic_app;Username=postgres;Password=your_password;"
          }
        ]
      }
    }
  }
}
```

> 同时确认 Redis 连接配置正确（用于缓存与分布式锁）。具体配置节以仓库内 `appsettings.Development.json` 为准。

## 三、启动后端

```bash
cd backend
dotnet run --project src/main/XiHan.BasicApp.WebHost --launch-profile Development
```

启动后：

- API 文档（Scalar）：<http://127.0.0.1:9708/scalar>
- 仓库当前 Development 与 Production 都监听 `9708`；部署时可通过 `Hosting:Urls` 或容器端口映射覆盖

**首次启动会自动建表并执行数据种子初始化**，稍等片刻即可。

## 四、启动前端

另开一个终端：

```bash
cd frontend
pnpm install
pnpm dev
```

按终端提示的地址打开浏览器即可访问前端。

## 五、登录

初始超级管理员账号：

| 字段 | 值 |
| --- | --- |
| 账号 | `superadmin` |
| 密码 | `SuperAdmin@123` |

可通过配置 `Saas:Seed:SuperAdminPassword`（环境变量 `Saas__Seed__SuperAdminPassword`）覆盖初始密码。

::: warning 安全提醒
生产环境**务必**覆盖初始密码，并在首次登录后立即修改。
:::

## 六、调通第一个接口

登录页背后就是 `POST /api/Auth/Login`。想脱离前端直接对接（Postman / curl / 自己的客户端），从这里开始：

```bash
curl -X POST http://127.0.0.1:9708/api/Auth/Login \
  -H "Content-Type: application/json" \
  -d '{ "username": "superadmin", "password": "SuperAdmin@123" }'
```

`data.token.accessToken` 就是后续所有请求要带的 `Authorization: Bearer <token>`。完整的响应信封、业务码、请求头与分页协议见 [接口对接指南](./api-guide)。

## 常见问题

- **启动报连不上数据库/Redis**：先确认两者已启动、连接串正确、防火墙放通端口。Redis 报 `WRONGPASS` 见 [开发环境](./dev-environment#redis-6-生产多实例必需)。
- **表没建 / 登录不了**：首次启动会自动建表与种子，若中途失败，排查数据库权限后重启；已有数据库的结构变化由 `UpdateScripts` 前向升级脚本处理。
- **端口被占用**：后端 Development 默认 `9708`，前端 dev server 默认 `9800`；改后端端口要同步改 `frontend/.env.development` 的 `VITE_DEV_PROXY_TARGET`。

> 更多故障速查见 [常见问题](./faq)。

## 下一步

- [接口对接指南](./api-guide)：怎么获取 token、统一响应信封、分页协议
- [系统架构](./backend/introduction)：理解后端模块与前后端如何协作
- [权限模型](./backend/permission)：RBAC + ABAC、数据范围、多租户
- [常见问题](./faq)：高频故障速查
- [功能清单](./features)：系统都有哪些能力
- [部署](./deployment)：发布到 Linux / Windows
