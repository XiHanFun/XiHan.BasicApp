# 部署

本篇介绍如何把 XiHan.BasicApp 发布到服务器。开发环境的本地启动见 [快速开始](./getting-started)。

## 环境要求

| 依赖 | 版本 |
| --- | --- |
| .NET SDK / Runtime | 10.0+ |
| PostgreSQL | 14+（或 MySQL / MariaDB） |
| Redis | 6.0+ |
| Node.js（构建前端） | 24.0+ |
| pnpm（构建前端） | 11.0+（具体版本以 `packageManager` 为准） |

::: tip 部署前置
- 准备好可连接的 **PostgreSQL** 与 **Redis**。
- 全新数据库首次启动会自动建库、建表并执行数据种子（对应 `EnableDbInitialization` / `EnableTableInitialization` / `EnableDataSeeding`：框架缺省均为 `false`，仓库的 `appsettings.Development.json` 全部打开，生产配置需自行打开）。存量数据库的变更写在 `UpdateScripts/<版本>/<版本>.sql`，`XiHan:Upgrade:EnableAutoCheckOnStartup` 为 `true`（缺省值）时应用启动会自动执行，失败即中断启动；不希望应用启动时改库，就关掉它并在发布流程里先完成迁移。
- 基础数据种子始终执行；演示数据（演示租户、组织、账号等）只在 `Saas:Seed:EnableDemoData` 为 `true` 时写入，缺省即不写。生产环境在 `appsettings.Production.json` 里写明 `false`。
- 生产环境 CORS 仅放行配置中的域名（`XiHan:Web:Api:Cors:AllowedOrigins` 与网关 `XiHan:Web:Gateway:AllowedOrigins`），部署到自己的域名时务必同步修改，否则前端会被跨域拦截。
- 若用到 AI / 知识库能力，还需准备对应的向量库（如 Qdrant）与嵌入模型配置。
:::

## 后端：发布

```bash
dotnet publish backend/src/main/XiHan.BasicApp.WebHost -c Release -o /opt/xihan-basicapp
```

发布前在目标环境的 `appsettings.Production.json`（或环境变量）中配置好数据库连接串、Redis、JWT 签名密钥、以及初始超管密码等敏感项。

升级脚本当前使用 PostgreSQL 方言。启动时引擎先处理平台库、再处理配置为独立数据库的租户，并用数据库租约锁协调多副本；没抢到租约的副本不会等待，照常启动。上线前应备份数据库、在同版本副本验证脚本，多副本发布时由编排层控制放流，细节见[升级与迁移](./backend/upgrade)。

应用监听地址与端口由配置项 `Hosting:Urls` 决定（`Program.cs` 启动时读取该值并调用 `UseUrls`）；`appsettings.Production.json` 被 `.gitignore` 忽略、仓库里没有，需在部署处自行写入该键，如 `http://127.0.0.1:9708`；未配置时 `--launch-profile Production` 取 launch profile 的 `http://127.0.0.1:9709`，Docker 镜像则由环境变量 `Hosting__Urls` 设为容器内 `9708`。对外暴露时建议在前面加一层反向代理（Nginx / Caddy 等）做 TLS 终止与静态资源分流。

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

### 隐私政策与服务条款

注册页的《隐私政策》《服务条款》是随前端打包的 Markdown，每种界面语言一份，放在 `frontend/packages/views/_core/authentication/legal/<语言>/` 下（`privacy-policy.md`、`terms-of-service.md`）。仓库自带的是通用范本，以「本平台的运营方」指代运营主体，没有写具体的公司名称、联系方式、管辖法院和数据存储地。

上线前请按自己的主体与业务改写，并请法务审阅，再重新构建前端。只支持部分语言时，其余语言也要保留一份对应文件：单测会核对每种界面语言的两份文书是否齐全、章节数是否与简体中文一致。

## 初始账号与安全

- 初始超级管理员：账号 `superadmin`，密码 `SuperAdmin@123`
- 初始密码写在种子里，不走配置；账号标记为需要本人改密，参数「密码设置」（`saas.auth.password`）的 `forceChange` 开启后首次登录即要求修改。

::: warning
生产环境首次登录后立即修改初始密码，并建议开启强制改密。数据库、Redis 等连接凭据请通过环境变量或密钥管理注入，不要写死在提交的配置文件里。
:::

### 访问策略与真实 IP

访问策略按客户端 IP、地区、时段、用户、角色、接口等条件放行、拒绝或只记录请求，登录失败过多的 IP 会被自动封禁（参数 `saas.security.login-auto-ban`，缺省关）。策略在「系统管理 → 访问策略」里维护，命中写进条件命中记录。

- **真实客户端 IP**：日志、会话、访问策略与自动封禁读的都是框架转发头中间件按受信代理还原后的地址，客户端自己伪造的 `X-Forwarded-For`、`X-Real-IP` 不算。缺省只信任本机回环地址上的代理（同机 nginx）；代理不在本机时（另一台主机上的 nginx、Docker Compose 里经桥接网络转发的前端 nginx 容器、云负载均衡），要在配置节 `XiHan:Web:Api:ForwardedHeaders` 里加上代理的地址（`KnownProxies`）或网段（`KnownNetworks`），多级代理同时调大 `ForwardLimit`，否则看到的客户端 IP 都是代理的地址。见[配置 · 受信代理](./configuration#api-forwardedheaders)。
- **出错时放行**：策略求值本身出错时记错误日志、本次请求放行，不因访问策略自身的故障拒绝全部请求。
- **多实例部署**：策略改动最多 5 秒后在所有节点生效；自动封禁的计数与名单在启用 Redis 时存在 Redis，没有 Redis 时退回进程内实现，多实例部署必须启用 Redis。

#### 误封了自己

1. 换一个不受这条策略影响的网络或账号登录，在访问策略页把它停用或改回「只记录」；被自动封禁时，换个地址登录后在「自动封禁」里解封，或等封禁到期。
2. 所有入口都被拦住时，在配置里关掉总开关 `Saas:AccessPolicy:Enabled`（改 `appsettings.Production.json` 热生效；用环境变量 `Saas__AccessPolicy__Enabled=false` 设置的要重启），改完策略后再打开。总开关关着时访问策略与自动封禁都不拦截，登录失败照常计数，日志里每 10 分钟提醒一次。

## 下一步

- [快速开始](./getting-started)：本地开发环境
- [系统架构](./backend/introduction)：模块划分
- [功能清单](./features)：系统能力
