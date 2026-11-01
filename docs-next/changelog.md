# 更新日志

本页按总体方案的分期记录 `next` 分支上已经落地的内容，适用于跟踪 6.0 进度的读者。6.0 尚未发布，正式发布时整理为 `6.0.0` 的发布说明。5.x 的更新日志见 [5.x 文档](https://basicapp.docs.xihanfun.com/changelog)。

## 6.0.0-preview.1（未发布）

### 框架升锁 42d4c570（2026-11-01）

- 只读接口里写库改报 500 `PERSISTENCE_UNEXPECTED_001`（原为 403 `PERSISTENCE_DENIED_001`），按缺陷处理；现有接口都不会触发。
- RabbitMQ：发布方只声明交换机 `XiHan:Messaging:RabbitMQ:Exchange`（缺省 `xihan`），队列由订阅方声明；没人订阅的框架事件（如 `scheduling.job.*`）不再堆积。接过 RabbitMQ 的开发环境里旧的 `xihan.scheduling.*` 队列不会再有新消息，手工删掉（`rabbitmqctl list_queues name messages consumers` 里 `consumers` 为 0 的）。见[配置参考](./configuration#xihan-messaging)。
- Redis 与 RabbitMQ 的绑定可以另带 `username` / `password`，覆盖连接串里的凭据。
- 工作流的租户隔离与随业务事务、库结构读取、运行时数据源与广播订阅已进框架：N3 的工作流、代码生成与实时推送的跨实例断开不再等框架。

### 框架升锁 25614c9a（2026-11-01）

- 授予判定读不到存储时返回 503 `SECURITY_UNAVAILABLE_001`，不再当作没有权限；文件访问、通知立即发布与工作台小组件同样如此。
- 只读接口不再写库：缺安全行的账号读个人信息时按缺省值显示，安全行在写操作里补建。
- 框架表新增 13 个索引（3 个唯一），6.0.0 基线重生成；开发库有重复数据时重建或去重，见[表结构升级](./backend/upgrade)。
- 状态码：同一作用域的嵌套调用、一次查询超过 1000 条改为 400，撞唯一索引改为 409。
- `appsettings.json` 不再写空的 `db`、`cache`、`queue` 绑定：原先会把 Kubernetes 经 Service Binding 挂进来的同名 Secret 整个盖掉。见[配置参考](./configuration)。
- 页码上限 100000，越界 400（原先只限到 `int.MaxValue`，换算跳过的行数时溢出，SQLite 返回第一页、PostgreSQL 报 500）。见[接口指南](./api-guide)。

### 授权与会话缓存的失效时机（2026-11-01）

- 授权类缓存（权限规则、角色指派、继承图、委托、条件、访问策略、字段安全、约束规则）的失效改到事务提交之后：新建角色委托不再要等一分钟缓存过期才生效，写入期间的并发请求也不会把旧数据写回缓存。见[缓存与异步](./backend/caching#缓存时长与失效)。
- 会话门禁与套餐门控同样改到提交之后失效：强制下线、改密、禁用、注销、锁屏与停止模仿时，恰在提交之前到达的同一会话请求不再把「有效」写回缓存，被吊销的会话不会在缓存过期前（缺省 30 秒）继续可用；换套餐与改白名单同理，租户的权限立即按新白名单计算。

### 宿主按配置接上分布式能力（2026-11-01）

- 配了 Redis 连接（绑定 `cache`）即装 `Caching.Redis` + `Caching.Hybrid`，配了 RabbitMQ 连接（绑定 `queue`）即装 `Messaging.RabbitMQ`；不配仍是单机零外部依赖。实时推送要到 N3 接上 Redis 背板才跨实例。见[配置参考](./configuration#分布式缓存与消息)与[部署](./deployment#多实例部署)。
- 装上 `Localization.Database`：平台库 `message_translation` 里的译文按区域覆盖嵌入的文案，6.0.0 基线随之重生成。
- CloudEvents 入站端点 `/events`（`BasicApp:CloudEvents:Enabled`，缺省关，只收登录过的调用方）。
- 缓存管理在 Redis 下列键攒满一页再返回（原先 SCAN 一段没命中时第一页是空的）。见[缓存与异步](./backend/caching)。

### 文档站发现的代码问题（2026-11-01）

写这套文档时对照代码发现的出入，按 5.x 与方案补齐：

- 索引与唯一约束：软删除实体的唯一约束改为只约束未删除的行（部分唯一索引），外键查找与列表筛选补上普通索引，6.0.0 基线随之重生成。见[数据模型](./backend/data-model)。
- 宿主补上转发头（`BasicApp:ForwardedHeaders`，缺省只信本机回环）与跨域（`BasicApp:Cors`，不配即不启用）。见[配置](./configuration)。
- 前端维护提示认 `RUNTIME_UNAVAILABLE_003` 与 `KERNEL_UNAVAILABLE_001`，并真正弹出提示。
- 时间按偏好设置里的应用时区显示（缺省 `Asia/Shanghai`），个人统计与工作台按它切天。见[国际化与时区](./frontend/i18n#时区)。
- 租户到期生效：到期时间一过登录即被拒（`IDENTITY_DENIED_019`），作业 `tenancy.tenants.expire` 把租户标为到期；续期后恢复。见[多租户](./backend/multi-tenancy)。
- 文件库与通知管理的列表按创建人套数据范围。见[数据权限](./backend/data-permission)。
- 自助注册（缺省关，`BasicApp:Auth:SelfRegistrationEnabled`）与找回密码（发验证码）。见[认证](./backend/authentication)。

### N2 收尾（2026-10-31 起，进行中）

按 5.x 功能清单逐条核对后补齐的差距：

- 用户管理：`users.disable` / `users.enable`（权限 `identity.users.disable`）：停用即吊销全部会话，之后各种登录方式都给 `IDENTITY_DENIED_018`；`users.list` 筛选补手机号、性别、状态多选、创建时间区间、角色与部门；用户页加按人强制下线与批量启停。
- 任务调度：`jobs.create` / `jobs.delete`（运行时作业，经 `IJobScheduler`）；前端搬入 Cron 构建器。重试策略与单次执行超时等框架支持。
- 消息模板：渠道补成邮件、短信、站内通知、机器人四种；机器人消息按平台模板 `notification-bot` 渲染；租户能看到并覆盖平台模板。
- 操作日志：描述取操作的本地化标题，补客户端 IP、浏览器、系统、设备与归属地；宿主装上 `Ip2RegionSecurityFeature`。
- 数据变更日志：区分「恢复」（软删除标记由真变假）。

- 次要四项：通知正文的 Markdown 编辑器（`XMdEditor`，异步懒加载）；部门成员表补职级与入职日期；个人中心统计补在线时长（`my-activity.summary`）；版本管理列出独立库租户。

仍差：任务调度的重试与单次执行时限、缓存管理的进程内对象缓存（均待框架推送、升级锁定提交），字段安全其余实体的登记范围待定。

### N2 平台能力（2026-10-28 – 10-31）

| 批次 | 内容 |
| --- | --- |
| 第一批 · 操作表定稿 | 每个操作显式 `Expose`（编译期 `XHG004`），超出 HTTP 的逐个登记；只读查询声明 `ReadOnly` 走 GET |
| 第二批 · Settings | 参数（平台 + 租户覆盖、加密）、字典（树形项）、业务编号；用户设置 |
| 第三批 · Auditing | 八类日志的查询面与链路追踪；分表日志有界队列写入；保留清理作业；`Auditing.Conformance` 通过 |
| 第四批 · Files | 存储配置、文件与副本、秒传、签名地址与 Range 读、分片上传、导出中心与导入历史；套餐价格与配额、我的订阅 |
| 第五批 · Realtime → Notification | 通知 Hub、在线用户与强制下线、锁屏；通知公告与收件箱、模板、邮件短信、群机器人、Telegram 机器人与交互 |
| 第六批 · OpenPlatform | 签名开放接口与凭证、OAuth 应用与我的应用、OAuth 2.0 / OIDC 端点与同意页、第三方登录；个人中心（资料、头像、联系方式验证与换绑、登录设备、登录日志、我的租户、通知偏好与统计） |
| 第七批 · Maintenance | 作业与运行历史、表结构升级执行器与 6.0.0 基线、版本与升级记录、缓存管理、服务监控；租户所有权转移、运维支持成员、独立库分步开通 |
| 第八批 · Workbench | 图标选择器、看板布局跨端同步、各包 15 个工作台小组件 |

穿插在各批之间的「N1 四包深化」：

- Identity：图形验证码、登录节流、认证器双因素；邮箱 / 手机验证码登录；邮件 / 短信双因素。
- Access：Deny、授予与指派有效期、用户直授、用户级数据范围、权限目录页；字段安全；角色继承与限时委托；权限条件（ABAC）及其试算与盘点；访问策略、登录时机与登录失败自动封禁；约束规则（五类）与会话角色激活。
- 接口文档：操作标题、标签、分组与字段说明本地化；XML 文档注释统一为一句话摘要。

### N1 身份与租户基座（2026-10-28）

九批：身份链路（登录、刷新、注销、会话门禁）→ Access 授权（角色、授予、指派、菜单）→ Tenancy（租户与框架租户目录同步）→ Organization（部门闭包表、岗位、成员）→ Access 补遗（数据范围）→ 前端壳层（从 5.x 整体搬入）→ 租户成员、切换租户与模仿登录 → 套餐门控与一致性套件 → 菜单管理与工作台。退出条件：超级管理员登录到工作台、开通租户与管理员、权限与套餐门控、切换租户与模仿。

### N0 骨架与验证（2026-10-28）

- `backend-next` 解决方案、`Contracts` / `Runtime` 最小集、Host（Full Profile + SqlSugar + JWT + 三个 Database Provider + 发件箱）、架构测试与 CI。
- `frontend-next` 脚手架与 `gen-contracts`。
- 冒烟：操作人字段经 `AsyncLocal` 填充、Hub 与 `UseXiHan` 共存、一次调用同时写平台库与租户库并一起回滚（PostgreSQL 两库）、gRPC 与 HTTP 同形。

## 规划

| 阶段 | 内容 |
| --- | --- |
| N3 可选模块一 | Workflow（含授权申请发起审批与审批中心）、Printing、CodeGeneration、Chat |
| N4 AI | AI 包与助手接入 Chat |
| N5 对齐与收口 | 对齐矩阵逐项核销、文档切到 6.0、性能与安全审查 |
| N6 切换 | 目录 `git mv`、CI 收口、5.x 进维护线、发布 6.0.0 |
