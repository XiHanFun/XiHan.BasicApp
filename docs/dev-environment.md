# 开发环境

承接 [系统概述](/overview) 对整体架构的了解，本篇用 **Docker 命令行** 准备本地开发可用的 **Redis** 与关系型数据库，并给出与仓库 `appsettings.Development.json` 对齐的默认 PostgreSQL 组合。其他数据库示例只用于开发验证；当前发布升级脚本以 PostgreSQL 为准。

> 数据库表结构无需手动创建——后端首次启动会自动建表并初始化种子数据。你只需准备好一个「可连接、账号密码正确」的数据库与 Redis 实例。

## 前置条件

- 已安装 **Docker Desktop**（或 Docker Engine），且引擎处于运行状态（`docker version` 能正常输出）。
- 下面的命令 **PowerShell 与 bash 通用**：ACL 等特殊字符统一用 **单引号** 包裹（单引号在两种 shell 里都是字面量，避免 `>`、`&`、`~` 被 shell 解释）。
- 数据以 **命名卷**（如 `-v pg-data:/var/lib/postgresql/data`）持久化，容器删了数据还在。
- 端口只映射到本机，密码仅为本地开发示例；**切勿用于生产**。

::: warning 命名冲突
若同名容器已存在，`docker run` 会报错。重建前先删除：`docker rm -f <名称>`（下面命令已在示例中带上）。
:::

## 与 appsettings 的对应关系

需要改动的文件：`backend/src/main/XiHan.BasicApp.WebHost/appsettings.Development.json`，两处：

| 依赖 | 配置节 | 键 |
| --- | --- | --- |
| 关系型数据库 | `XiHan:Data:SqlSugarCore:ConnectionConfigs[0]` | `DbType` + `ConnectionString` |
| Redis | `XiHan:Caching:Redis` | `IsEnabled` + `Configuration` |

数据库按需 **选一种** 即可（把对应的 `DbType` 与连接串填进去）。Redis 可关闭并回退进程内实现，但多实例与分布式锁/队列场景必须启用。

---

## Redis 6+（生产多实例必需）

用于分布式缓存、分布式锁与延迟/流式队列。仓库默认连接串用的是 **ACL 用户** `user=redis,password=redis`，所以要在容器里建好同名 ACL 用户（官方 `redis` 镜像默认既无密码、也没有名为 `redis` 的用户，直接连会报 `WRONGPASS`）。

```bash
docker rm -f redis
docker run -d --name redis -p 6379:6379 -v redis-data:/data redis:8-alpine redis-server --appendonly yes --user default off --user redis on '>redis' '~*' '&*' '+@all'
```

- `--user redis on '>redis' '~*' '&*' '+@all'`：建用户 `redis`、密码 `redis`，授予全部键（`~*`）、全部频道（`&*`，本项目用到发布订阅/Streams，必须给）、全部命令（`+@all`）。
- `--user default off`：关闭默认匿名用户（加固）。本地嫌麻烦可去掉这一行，但端口就是裸奔的。
- `--appendonly yes`：开启 AOF 持久化。

对应 `appsettings.Development.json`：

```json
{
  "XiHan": {
    "Caching": {
      "Redis": {
        "IsEnabled": true,
        "Configuration": "127.0.0.1:6379,user=redis,password=redis,defaultDatabase=0"
      }
    }
  }
}
```

验证（返回 `PONG` 即通）：

```bash
docker exec -it redis redis-cli -u redis://redis:redis@127.0.0.1:6379 ping
```

::: tip 不想用密码？
把 `IsEnabled` 设为 `false` 会退化为进程内内存缓存（单实例够用，失去分布式缓存/锁/队列）；此时无需启动 Redis 容器。
:::

---

## 关系型数据库（按需选一种）

### PostgreSQL 14+（默认，推荐）

```bash
docker rm -f postgres
docker run -d --name postgres -p 5432:5432 -v pg-data:/var/lib/postgresql/data -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=XiHanBasicApp postgres:17-alpine
```

对应连接串：

```json
{
  "DbType": "PostgreSQL",
  "ConnectionString": "Server=127.0.0.1;Port=5432;Database=XiHanBasicApp;Username=postgres;Password=postgres;TrustServerCertificate=true;"
}
```

验证：

```bash
docker exec -it postgres psql -U postgres -d XiHanBasicApp -c "select version();"
```

### MySQL 8+

```bash
docker rm -f mysql
docker run -d --name mysql -p 3306:3306 -v mysql-data:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=mysql -e MYSQL_DATABASE=XiHanBasicApp mysql:8.4
```

对应连接串（SqlSugar 的 MySql 类型）：

```json
{
  "DbType": "MySql",
  "ConnectionString": "Server=127.0.0.1;Port=3306;Database=XiHanBasicApp;Uid=root;Pwd=mysql;"
}
```

验证：

```bash
docker exec -it mysql mysql -uroot -pmysql -e "select version();"
```

### MariaDB 11.8+

MariaDB 与 MySQL 协议兼容，SqlSugar 仍用 `MySql` 类型。

```bash
docker rm -f mariadb
docker run -d --name mariadb -p 3306:3306 -v mariadb-data:/var/lib/mysql -e MARIADB_ROOT_PASSWORD=mariadb -e MARIADB_DATABASE=XiHanBasicApp mariadb:11.8
```

```json
{
  "DbType": "MySql",
  "ConnectionString": "Server=127.0.0.1;Port=3306;Database=XiHanBasicApp;Uid=root;Pwd=mariadb;"
}
```

### SQL Server 2025+

SQL Server 对 SA 密码有强度要求（≥8 位，含大小写、数字、符号）。

```bash
docker rm -f mssql
docker run -d --name mssql -p 1433:1433 -v mssql-data:/var/opt/mssql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=XiHan@2026 mcr.microsoft.com/mssql/server:2025-latest
```

对应连接串（数据库 `XiHanBasicApp` 会由后端首启时自动创建）：

```json
{
  "DbType": "SqlServer",
  "ConnectionString": "Server=127.0.0.1,1433;Database=XiHanBasicApp;User Id=sa;Password=XiHan@2026;TrustServerCertificate=true;"
}
```

验证：

```bash
docker exec -it mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'XiHan@2026' -C -Q "select @@version"
```

### Oracle（框架支持但不建议）

用社区维护的 `gvenzl/oracle-free` 镜像（Oracle Database 23ai Free）。Oracle 没有「新建数据库」概念，改为用 `APP_USER` 建一个业务 schema。首次启动较慢（需初始化实例）。

```bash
docker rm -f oracle
docker run -d --name oracle -p 1521:1521 -v oracle-data:/opt/oracle/oradata -e ORACLE_PASSWORD=oracle -e APP_USER=xihan -e APP_USER_PASSWORD=xihan gvenzl/oracle-free:latest
```

```json
{
  "DbType": "Oracle",
  "ConnectionString": "User Id=xihan;Password=xihan;Data Source=127.0.0.1:1521/FREEPDB1;"
}
```

### 国产数据库（达梦 DM / 人大金仓 Kingbase / 华为 GaussDB）

SqlSugar 同样支持这些国产库（`DbType` 取 `Dm` / `Kdbndp` / `PostgreSQL` 等对应值）。它们的官方镜像通常需从厂商渠道获取（并非都在 Docker Hub 公开），拿到镜像后按厂商说明 `docker run`，再把 `DbType` 与连接串填入 `ConnectionConfigs[0]` 即可，用法与上面一致。

---

## 向量数据库 Qdrant（可选，AI 知识库用）

仅当你要启用 **AI 知识库 / RAG** 能力时才需要。

```bash
docker rm -f qdrant
docker run -d --name qdrant -p 6333:6333 -p 6334:6334 -v qdrant-data:/qdrant/storage qdrant/qdrant:v1.15.1
```

- `6333` 为 HTTP/REST 与管理面板（浏览器访问 `http://127.0.0.1:6333/dashboard`），`6334` 为 gRPC。
- 具体接入配置见 [AI 能力](/backend/ai)。

---

## 一键起（docker compose 备选）

把「PostgreSQL + Redis」这套默认组合一次拉起。在任意目录新建 `docker-compose.yml`：

```yaml
services:
  postgres:
    image: postgres:17-alpine
    container_name: postgres
    restart: unless-stopped
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: XiHanBasicApp
    volumes:
      - pg-data:/var/lib/postgresql/data

  redis:
    image: redis:8-alpine
    container_name: redis
    restart: unless-stopped
    ports:
      - "6379:6379"
    command: >
      redis-server --appendonly yes
      --user default off
      --user redis on >redis ~* &* +@all
    volumes:
      - redis-data:/data

volumes:
  pg-data:
  redis-data:
```

启动 / 停止：

```bash
docker compose up -d      # 后台启动
docker compose ps         # 查看状态
docker compose down       # 停止并删除容器（命名卷保留，数据不丢）
```

> compose 的 `command:` 用 YAML 折叠块（`>`）书写，其中的 `>redis`、`~*`、`&*` 无需再加引号（YAML 会整体作为一行字符串传给容器）。

---

## 常用运维命令

```bash
docker ps                 # 查看运行中的容器
docker logs -f redis      # 跟踪日志（换成 postgres/mysql 等）
docker stop redis         # 停止
docker start redis        # 启动
docker rm -f redis        # 强制删除容器（命名卷不受影响）
docker exec -it redis sh  # 进入容器
docker volume ls          # 查看数据卷
```

---

## 下一步

依赖就绪后：

1. 把上面选定数据库的 `DbType` + `ConnectionString`、以及 Redis 的 `Configuration` 填入 `appsettings.Development.json`。
2. 进入下一篇 [快速开始](/getting-started)：克隆代码、启动前后端并登录系统（首次运行会自动建表并写入种子数据）。
3. 生产环境请改用独立的强密码、密钥库/环境变量注入，部署细节见 [部署](/deployment)。

> 上一篇：[系统概述](/overview) · 下一篇：[快速开始](/getting-started)
