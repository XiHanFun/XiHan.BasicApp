# 部署

本篇介绍如何把 XiHan.BasicApp 发布到服务器。开发环境的本地启动见 [快速开始](./getting-started)。

## 环境要求

| 依赖 | 版本 |
| --- | --- |
| .NET SDK / Runtime | 10.0+ |
| PostgreSQL | 14+（或 MySQL / MariaDB） |
| Redis | 6.0+ |
| Node.js（构建前端） | 24.0+ |
| pnpm（构建前端） | 11.0+（`packageManager` 已锁定 `pnpm@11.7.0`） |

::: tip 部署前置
- 准备好可连接的 **PostgreSQL** 与 **Redis**。
- 全新数据库首次启动会自动建库、建表并执行数据种子（对应 `EnableDbInitialization` / `EnableTableInitialization` / `EnableDataSeeding`，默认均为 `true`）。存量数据库的变更应写入 `UpdateScripts/{version}.sql`，但当前 BasicApp 尚未接入升级执行入口，发布流程必须显式安排迁移步骤，不能假定应用启动会自动执行。
- 系统基线种子始终执行；内置的演示数据（示例组织、演示账号等）由 `Saas:Seed:EnableDemoData` 控制，默认 `true`，生产环境如不需要可显式设为 `false` 跳过。
- 生产环境 CORS 仅放行配置中的域名（`XiHan:Web:Api:Cors:AllowedOrigins` 与网关 `XiHan:Web:Gateway:AllowedOrigins`），部署到自己的域名时务必同步修改，否则前端会被跨域拦截。
- 若用到 AI / 知识库能力，还需准备对应的向量库（如 Qdrant）与嵌入模型配置。
:::

## 后端：发布

```bash
dotnet publish backend/src/main/XiHan.BasicApp.WebHost -c Release -o /opt/xihan-basicapp
```

发布前在目标环境的 `appsettings.Production.json`（或环境变量）中配置好数据库连接串、Redis、JWT 签名密钥、以及初始超管密码等敏感项。

升级脚本当前使用 PostgreSQL 方言。引擎被调用时可先处理平台库、再处理配置为独立数据库的租户，并用数据库租约锁协调多副本；但这些能力当前不会由 BasicApp 启动流程自动触发。上线前应备份数据库、在同版本副本验证脚本，并按[升级与迁移](./backend/upgrade)补齐或外置唯一执行入口。

应用监听地址与端口由配置项 `Hosting:Urls` 决定（`Program.cs` 启动时读取该值并调用 `UseUrls`）；仓库自带的 `appsettings.Production.json` 默认配置为 `http://127.0.0.1:9708`，可按需调整。对外暴露时建议在前面加一层反向代理（Nginx / Caddy 等）做 TLS 终止与静态资源分流。

## 后端：Linux（Supervisor）

仓库自带的进程守护配置是 **Supervisor**（`backend/src/main/XiHan.BasicApp.WebHost/ServiceScripts/XiHan.BasicApp.ini`），并非 systemd unit。下面以 **CentOS / RHEL 系**（CentOS Stream 9、Rocky、Alma 等）为例。

### 1. 安装并启用 Supervisor

CentOS Stream 9 默认仓库即含 supervisor 包，直接用 dnf 安装：

```bash
sudo dnf install supervisor -y

# 开机自启并启动（⚠ 服务名是 supervisord，不是 supervisor）
sudo systemctl enable supervisord
sudo systemctl start supervisord
sudo systemctl status supervisord
```

### 2. 发布后端

```bash
# 发布到 .ini 中约定的目录（换用其他路径需同步改 command / directory）
dotnet publish backend/src/main/XiHan.BasicApp.WebHost -c Release -o /home/basicappapi
sudo mkdir -p /home/basicappapi/logs
```

仓库自带的 `XiHan.BasicApp.ini` 内容如下（程序名取自 `[program:XiHanBasicApp]`）：

```ini
[program:XiHanBasicApp]
command=/usr/bin/dotnet /home/basicappapi/XiHan.BasicApp.WebHost.dll
directory=/home/basicappapi
autorestart=true
startsecs=5
startretries=5
stdout_logfile=/home/basicappapi/logs/run.log
stderr_logfile=/home/basicappapi/logs/run.log
stdout_logfile_maxbytes=5MB
stderr_logfile_maxbytes=5MB
stdout_logfile_backups=5
stderr_logfile_backups=5
user=root
priority=999
numprocs=1
environment=ASPNETCORE_ENVIRONMENT=Production,DOTNET_CLI_HOME=/tmp
```

### 3. 添加服务监听

CentOS 上 Supervisor 的包含目录是 **`/etc/supervisord.d/*.ini`**（`/etc/supervisord.conf` 里 `files = /etc/supervisord.d/*.ini`），与 Debian 的 `/etc/supervisor/conf.d/*.conf` **不同**。因此配置文件必须放到 `/etc/supervisord.d/` 且**保留 `.ini` 后缀**，否则不会被加载（这也是「服务监听不生效」最常见的坑）。

```bash
sudo mkdir -p /etc/supervisord.d
```

**方法一：软链接（推荐，`.ini` 随发布产物更新，改动免拷贝）**

`dotnet publish` 会把 `ServiceScripts/` 一并输出到发布目录（WebHost 工程 csproj 中已设 `CopyToOutputDirectory=Always`），因此 `.ini` 已随产物落到 `/home/basicappapi/ServiceScripts/`，直接软链接即可：

```bash
sudo ln -s /home/basicappapi/ServiceScripts/XiHan.BasicApp.ini /etc/supervisord.d/XiHan.BasicApp.ini
```

**方法二：直接从仓库复制**

```bash
sudo cp backend/src/main/XiHan.BasicApp.WebHost/ServiceScripts/XiHan.BasicApp.ini /etc/supervisord.d/XiHan.BasicApp.ini
```

### 4. 重载配置并启动

```bash
sudo supervisorctl reread
sudo supervisorctl update
sudo supervisorctl avail            # 验证配置已被识别
sudo supervisorctl start XiHanBasicApp
```

### 5. 管理与日志

```bash
# 状态（单个 / 全部）
sudo supervisorctl status XiHanBasicApp
sudo supervisorctl status
# 停止 / 重启
sudo supervisorctl stop XiHanBasicApp
sudo supervisorctl restart XiHanBasicApp
# 日志
sudo tail -f /home/basicappapi/logs/run.log
```

> **Debian / Ubuntu** 布局不同：包名与服务名均为 `supervisor`，包含目录是 `/etc/supervisor/conf.d/*.conf`（放 `.conf`）。此时把上面的目标路径换成 `/etc/supervisor/conf.d/XiHan.BasicApp.conf`、服务名用 `supervisor` 即可，其余命令一致。
>
> 若偏好用 systemd 管理进程，可参照上面的发布命令自行编写 unit 文件；仓库当前未随附对应的 `.service` 文件。

## 后端：Windows

仓库内提供的是一个**注册 Windows 服务**的批处理脚本：

```text
backend/src/main/XiHan.BasicApp.WebHost/ServiceScripts/XiHan.BasicApp.bat
```

该脚本以管理员权限运行，内部调用 `sc create` 把可执行文件注册为自动启动的 Windows 服务（服务名 `XiHan.BasicApp`）并立即 `Net Start`。`binPath` 是按脚本自身所在目录拼接 `XiHan.BasicApp.exe` 得到的：`dotnet publish` 已把该 `.bat` 一并输出到发布目录的 `ServiceScripts\` 子目录，但**运行前需把它移到与 `.exe` 同级的发布根目录**（否则拼出的 `binPath` 指向 `ServiceScripts\` 里并不存在的 exe）。此外发布产物的程序集名实际是 `XiHan.BasicApp.WebHost`，如生成的可执行文件名与脚本引用的 `XiHan.BasicApp.exe` 不一致，需重命名或调整脚本后再运行。后续可用 `sc stop XiHan.BasicApp` / `sc delete XiHan.BasicApp` 或系统「服务」管理器管理该服务。

## 前端：构建与部署

```bash
cd frontend
pnpm install
pnpm build
```

构建产物为静态文件，部署到任意静态服务器（Nginx / IIS / 对象存储 + CDN 均可）。注意配置反向代理，将 API 请求转发到后端端口。

> 若前后端**不同源**部署，前端需正确设置 API 基址（`VITE_API_BASE_URL`），本地存储返回的 `/uploads` 根相对路径会据此拼成绝对 URL。

## 初始账号与安全

- 初始超级管理员：账号 `superadmin`，密码 `SuperAdmin@123`
- 通过 `Saas:Seed:SuperAdminPassword`（环境变量 `Saas__Seed__SuperAdminPassword`）覆盖初始密码

::: warning
生产环境务必覆盖初始密码，并在首次登录后立即修改。数据库、Redis 等连接凭据请通过环境变量或密钥管理注入，不要写死在提交的配置文件里。
:::

## 下一步

- [快速开始](./getting-started)：本地开发环境
- [系统架构](./backend/introduction)：模块划分
- [功能清单](./features)：系统能力
