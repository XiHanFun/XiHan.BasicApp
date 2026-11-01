# 6.0 框架引用、配置与运维

## 框架引用与锁定

- `backend-next/props/framework.props` 锁 `XiHanFrameworkCommit`；缺省以 ProjectReference 引用同级 `XiHan.Framework/next/src`，`-p:XiHanFrameworkSrc=<检出>/next/src/` 改指路径，`-p:UseXiHanFrameworkSource=false` 走 NuGet 包 `5.0.0-preview.*`。工程文件只写 `<XiHanFrameworkReference Include="…" />` / `<XiHanFrameworkAnalyzer … />`，由 `Directory.Build.targets` 翻译，不写路径与版本。
- `FrameworkPinTests` 核对同级检出的 HEAD；别的会话在框架 `next` 上提交时，按锁定提交 `git archive` 出快照再构建（命令见后端技能的 `references/backend.md`），快照下这一条失败属预期。
- 升锁定提交：改 `XiHanFrameworkCommit`，集中跟进公开面与行为变化（签名、校验、表结构），基线随之重生成，方案写一条修订；CI 按锁定提交检出框架（经 GitHub API 把短 SHA 换成完整 SHA），框架没推送前 CI 在检出步失败，推送要用户同意。
- 框架缺陷在框架仓库修、BasicApp 只升锁定；需要回灌的事项登记在方案 9.4（N3、N4 的在 9.5）。
- 本机 IDE 联调用工作区根的 `XiHanFun.Next.Local.slnx`（不在任何 git 里）；新建包要补进去，否则 IDE 沿用旧的 `obj`。

## 配置

- 框架节 `XiHan:*`（各 Feature 经清单的配置贡献读取，非法值启动即失败）+ 应用节 `BasicApp:*`（各包 Options 直接绑定）。环境变量用双下划线：`XiHan__Bindings__db__value`。
- 绑定：`db`（平台库，启动硬要求）、`cache`（Redis，可选）、`queue`（RabbitMQ，可选），来自 `XiHan:Bindings:<名>` 或 `SERVICE_BINDING_ROOT` 目录。宿主 `HostProviders` 看到 `cache` / `queue` 不是空白才装对应 Provider；没配即单机零外部依赖。
- 可选模块 `BasicApp:Modules:<名>:Enabled`：没写即开，写了必须是 `true` / `false`；后端注册、前端模块与菜单下发同一开关语义。
- 入口开关：`BasicApp:ApiDocs:Enabled`（缺省只在 Development）、`BasicApp:CloudEvents:Enabled`（缺省关）、`BasicApp:Cors`（不配来源即不启用）、`BasicApp:ForwardedHeaders`（缺省只信本机回环）。
- 不提交密码、令牌、连接串；`appsettings.json` 留空，示例只用占位符；`appsettings.Development.json` 的值只用于本机。
- 诊断服务前从 `launchSettings.json`、`appsettings*.json`、环境变量或启动日志读实际地址（HTTP 9710、gRPC 9711、前端 9100），不硬编码。

## Provider 与健康

- 健康项由各 Provider 自带（框架 ADR-0030），BasicApp 不另写：数据库、租户目录、授予、审计、调度、对象存储命名实例、IP 归属地、数据库译文，以及配了才有的 `caching.redis.connection` / `messaging.rabbitmq.connection`。端点 `/health/startup`、`/health/ready`、`/health/live`，不经维护模式拦截。
- 配了而连不上的 Provider 启动即失败；运行中断开记 Degraded、应用照常服务。不改失败等级、不加内存兜底来掩盖不可用。
- 实时推送与在线状态目前只在本实例（配了 Redis 即跨实例排在 N3 第二批）；多实例部署时缓存与消息靠 Redis / RabbitMQ，数据保护密钥目录必须共享。

## 表结构与种子

- 开发库由 Development 的 `InitTables` 建表补列；正式库只认 `Host/Schema/<版本>/*.sql`（PostgreSQL、`CREATE TABLE IF NOT EXISTS`、可重复执行）与版本台账，`SchemaUpgrader` 在启动时按库取租约锁执行、失败即中断，别的节点升级期间维护模式返回 503。
- 6.0 发布前改表就重生成 `6.0.0/0001-baseline.sql`（`SchemaBaselineTests` + `XIHAN_UPDATE_SCHEMA_BASELINE=1` + `XIHAN_TEST_POSTGRES`），发布后每个版本一个新目录。
- 6.0 不做 4.x 数据迁移（D12）。Agent 不对任何库手工执行脚本，也不删改用户的开发库；要验证建表，用测试自建的临时库。
- 种子：`dotnet XiHan.BasicApp.Host.dll seed [--only <名>] [--list]`，号段见 `SeedOrders`；初始账号 `superadmin`，密码来自 `BasicApp:Seed:SuperAdminPassword`。

## 门禁与 CI（`.github/workflows/next-ci.yml`）

| 作业 | 内容 |
| --- | --- |
| 后端 | 按锁定提交检出框架；Release 构建（警告即错误）；`dotnet test --solution`，带 PostgreSQL、Redis、RabbitMQ 服务容器与 `XIHAN_TEST_*` 环境变量 |
| 契约漂移 | 重新 `contracts export` 比对 `contracts/*`，再 `pnpm contracts:check` 比对生成物 |
| 前端 | `pnpm check`（八项）、`pnpm test`、`pnpm build` |

文档：行为或配置变了同步 `docs-next/` 对应页与更新日志、`nextdocs/下一代总体方案.md` 写修订（编号接在最后一条之后，正文与第 8 节状态行一致）。代码、文档与提交信息里不写参考项目名。
