# 6.0 后端开发参考

真源顺序：代码与架构测试 → `nextdocs/下一代总体方案.md`（第 5 节与各修订）→ `docs-next/backend/*.md`。三者不一致时以代码为准，并把文档改正。

## 包的组成

```text
src/XiHan.BasicApp.<包>/
├─ <包>Feature.cs          IFeature：清单（Id = xihan.basicapp.<包>、Provides basicapp.<包>@1、Requires 能力而不是包名）+ Configure 显式登记
├─ <包>Permissions.cs      权限码常量与 All（PermissionDescriptor：码、显示名、作用侧、audited、说明）
├─ <包>Pages.cs            页面登记（页面码、路由、标题键、图标、排序、读权限）
├─ <包>Messages.cs         消息键常量与 Catalog
├─ <包>Errors.cs           错误码常量 <包大写>_<KIND>_<NNN>
├─ <包>JsonContext.cs      本包全部请求 / 响应 / 推送载荷的源生成上下文（枚举按字符串）
├─ Localization/<区域>.json × 10
├─ Domain/  Operations/<资源 Pascal>/  Queries/  Handlers/  Middleware/  Jobs/  Infrastructure/  Seeds/  Schema/
```

- 目录只许上面这些（`PackageStructureTests`，`Contracts`、`Runtime`、`Host` 例外）。新包要进 `XiHan.BasicApp.slnx`、`Architecture.Tests` 的项目引用与 `Model/Packages.cs` 的 Tier 表，并在宿主 `UseFeature`。
- Tier 3 可选模块照 Printing 的接法（方案修订（八十五））：`HostModules` 加开关、宿主 `if (modules.<名>) UseFeature`、OpenAPI 加分组、一条开关用例（关掉后页面与权限码不下发、操作 404、`/openapi.json` 照常、写错值启动即失败）。
- 审批接入（方案修订（九十七））：业务包实现 Contracts `Workflow/IWorkflowBusinessHandler`（描述、办理前校验、改动前校验、结论回写、中止回写、单据数据；改动前校验在转办、加签、撤回与管理员实例控制之前，批量里不过的那一项不交给引擎，方案修订（一百零三）），`Configure` 里 `services.AddWorkflowBusiness<THandler>()`，业务码即流程定义名、重复即启动失败；发起、撤回与进度经 `IEnumerable<IWorkflowBusinessService>` 注入（Workflow 关掉时没有实现）。流程定义与实例在框架的引擎表里，列表交给框架查询（过滤、排序、分页都在存储里）：排序照 4.x 多列，请求的排序项按顺序把响应字段名映射到框架的排序白名单（第一项为主），别的列 400 `WORKFLOW_VALIDATION_104`，同一列两次或超过可排序列数由框架报 `WORKFLOW_VALIDATION_002`，不在 BasicApp 内存排序；我发起的与待办顺序固定。结论在结束节点经结果处理器同事务回写；只要「至少一次、按消息幂等」做不到状态核对的处理器（如通知），照 Workflow 的 `WorkflowNotifier` 在同一事务里先写回执表（租户 + 消息 ID 唯一，方案修订（九十九））。
- 操作命名空间按资源名逐段转 Pascal：`oauth-apps` → `Operations/OauthApps`；共用 DTO 可放 `Operations/<主题>/XxxContracts.cs`。
- 跨包协作只经 `Contracts` 的接口（`IConfigValueReader`、`ITenantQuota`、`IRealtimePush`、`IMessageSender`、`IWorkbenchWidgetProvider`、`IEditionGate` 等），实现方在自己的 Feature 里登记，消费方 `IEnumerable<>` 注入时要处理没有实现的情形。
- 产出给人下载的文件（导出结果、代码生成的产物包）经 Contracts `Files/ITemporaryFiles` 存成临时文件（存在调用方这一侧、带到期时间、读取不核对通用的文件权限）：业务先按自己的权限核对，再由宿主的普通端点（`Host/Endpoints`，可选模块按 `HostModules` 开关才映射，挂 `RequireXiHanPermission`、带元数据进 `openapi.json`）以附件下载，不把文件以 Base64 放进操作的响应。
- 异步解耦用领域事件（方案修订（八十九），Organization → Chat 的部门群同步）：发出方的实体是聚合根、在方法里记事件，`Configure` 里 `AddDomainEvent(Topics.X, type, jsonTypeInfo, subject)`；主题常量在 `Contracts/Topics.cs`，事件记录放 `Contracts/<发出方>/`；订阅方 `AddMessageHandler<THandler>(topic)`。投递至少一次、可能乱序，处理器按处理时的当前状态核对，重复处理无副作用。

## 操作

- 操作 ID `<资源>.<动词>`（资源复数 kebab）；标准动词 `create / get / list / update / delete`，写动作用动词原形，`get` / `list` 以外的只读查询一律 `ReadOnly = true`。
- 对已有对象按标识的写入（删除、启停、授权、撤销）只给批量形态，返回 `BatchResult`，逐项 `Result`。
- 授权：`Permission = <包>Permissions.X`；自助操作 `Policy = "authenticated"`；匿名操作两者都不写，并登记进 `OperationContractTests` 的匿名白名单。
- 请求 / 响应是 `record`：有缺省值的属性用 `set` 不用 `init`（`ContractDtoTests`）；时间 `DateTimeOffset`（UTC）；主键 `Guid`；租户 ID `string`；`long` 保持 `long`。单字段约束用 DataAnnotations，跨字段规则实现 `IValidatableObject`。
- 文案：`<操作 ID>.title` / `.description`（资源段的 `-` 换 `_`），`openapi.tags.<资源>.name`，`permissions.<码>.name` / `.description`（`permissions.list` 按调用方区域取，界面不显示描述表里的英文）；10 个区域键集一致且与常量一一对应。
- 在同一个调用上下文里不能再调另一个操作（派生上下文共享 Items）；要改操作响应用框架的 `IOperationResponseFilter`。
- `Result` / `Result<T>` 是 struct：找第一个失败要写循环；`Result<T>` 不许空成功值，可空用 `Result<Maybe<T>>`。
- 形状不定的 JSON（如零代码运行时的数据行）用 `JsonElement` / `IReadOnlyDictionary<string, JsonElement>`，框架的 Schema 生成器按「任意 JSON」写进契约；`JsonObject` 的值类型 `JsonNode?` 现在会被当成普通类型展开（已交框架会话）。

## 数据

| 场景 | 用什么 |
| --- | --- |
| 按 ID、按规约、计数、增删改、批量 | `IRepository<TEntity, Guid>`（`AddGeneratedRepositories()` 登记），自动租户、软删除、并发戳、时间 |
| 分页、投影、联表、聚合 | `Queries/` 注入 `ISqlSugarClient`，以 `TenantScopedQuery.For<T>(client, context)` 起手，数据范围用 `ApplyDataScope(...)` |
| 独立于调用事务的读写 | `Runtime.Persistence.StoreScope.ReadAsync / WriteAsync` |
| 后台对租户库的工作 | `TenantShardScope.BindTenantAsync`（框架 ADR-0057）或 `ITenantScopeRunner` |

- 库决定协议：平台库实体（租户、用户与账号、角色与授权、菜单、参数、字典、模板、存储配置……）不实现 `IMultiTenant`，用 `OwnerTenantId`（平台 = `TenantIds.Platform`）分侧；租户库实体（部门、文件、日志、通知投递……）用 `TenantScoped*` 基类，没有租户上下文时什么都查不到。
- 基类：`AppEntity` / `AppRecord` / `AppAuditedEntity` / `AppAggregateRoot` 及 `TenantScoped*` 变体。软删除实体的唯一约束写 `[ActiveUniqueIndex]`（部分唯一索引），不软删除的用唯一 `[SugarIndex]`。
- 规约里可空值类型判空写 `!x.HasValue`；查询 `Take` 只收 1–1000，读全量按主键分页读完。
- 没有实体的原始表（代码生成导入的表、外部数据源）用 `client.Queryable<object>().AS(表名)` 读：表名只取配置里的、不接受请求给的；列名逐个经 `QueryBuilder.Builder.GetTranslationColumnName` 加引号（大小写混合的照原样），不写 `*`（Oracle 的分页不接受 `*` 与行号并列）；主库的表不经实体过滤，租户与软删的条件显式补；库报错（`SqlSugarException` / `DbException`）转成本包的 `Unavailable` 错误、带原因，不原样抛出。
- 只读操作（动词 `get` / `list` 或声明 `ReadOnly`）跑在只读工作单元里，写入即 500 `PERSISTENCE_UNEXPECTED_001`（按缺陷处理，计入熔断）：要补建的行放到写操作里建，读路径缺行按缺省值读（参照 `UserAccountService.SecurityOfAsync`）。
- 授予存储、权限检查器与收窄过滤器返回 `Result`：判定本身失败要原样向上返回（框架给 503），不能当作没有权限或悄悄隐藏东西。
- 字符串列没写长度缺省 512，更长的显式 `[SugarColumn]` / `[LongText]`；给已有行的表加列，非可空的字符串与日期列声明成可空。
- 高频写入的实体标 `[DiffLogIgnored]`；读操作别标 `audited`。

## 缓存、消息与宿主 Provider

- 共享缓存用 `ISharedCache`，键前缀在包里 `AddCacheNamespace` 登记。宿主配了 Redis（绑定 `cache`）就是 Redis，值要能按 JSON 往返；Access 等包把缓存值存成字符串，新写的缓存照此做。
- 读穿缓存的失效写 `CacheInvalidation.RemoveAsync(cache, key, ct)`（批量 `RemoveManyAsync`，Runtime）：调用里只记下，`CacheInvalidationMiddleware` 在工作单元提交或回滚之后才删键。直接 `cache.RemoveAsync` / `RemoveManyAsync` 会在提交前删，同一请求或并发请求未命中时经 `StoreScope` 读不到未提交的行、把旧数据写回缓存，变更要等缓存过期才生效；进程内缓存的测试若靠 `ManualClock` 拨钟让条目过期会掩盖它，要写不拨钟、改完立刻请求的用例。只有并发窗口、同一请求里没有再读的，用 `server.InvokeAsync` 在一次调用里执行真实操作、提交前插一次读（`AuthFlowTests.Sessions_Revoke_IsNotUndoneByACheckBeforeCommit`）。
- 宿主的分布式 Provider 由 `HostProviders` 按配置装配（Redis + Hybrid、RabbitMQ），`DatabaseLocalizationFeature` 常装；Realtime 由宿主按是否配了 Redis 以 `scaleOut` 装配（SignalR Redis 背板、在线状态存共享缓存、断开连接广播）。各包的 Hub 继承 `RealtimeHub` 并 `AddRealtimeHub<THub>(路径)` 登记，推送 `IRealtimePush.SendAsync(hub, …)` 随提交发出，瞬时事件用 `SendNowAsync`（事务里调用即抛出）。
- 同阶段中间件按清单 ID 字典序排，跨包依赖要 `context.Pipeline.After<Mine, Theirs>()` 并在清单 `PipelineContribution.After` 同步声明。
- 后台循环开平台调用前等 `app.State.IsServing()`。

## 种子与表结构

- 种子实现 `ISeeder`、`AddSeeder<T>()`，号段取 `Contracts.Seeding.SeedOrders`（身份 100、操作字典 200、权限目录 300、菜单 400、套餐 500、平台数据 600、演示 900，模块偏移 +10）；目录类每次对齐，运营数据只首写，演示数据看 `BasicApp:Seed:EnableDemoData`。
- 开发库靠 Development 的 `InitTables` 建表；正式库只认 `Host/Schema/<版本>/*.sql` 与版本台账（`SchemaUpgrader` 启动时执行、失败即中断）。Agent 不对任何库手工执行脚本。
- 6.0 发布前改表就重生成 6.0.0 基线：`XIHAN_TEST_POSTGRES=<服务器> XIHAN_UPDATE_SCHEMA_BASELINE=1` 跑 `SchemaBaselineTests.Baseline_BuildsTheSameTablesAsInitTables_AndRunsOnce`，测试自建临时库、按 InitTables 写回 `0001-baseline.sql` 并校验基线建的库与之一致；只提交基线里属于本次改动的变化。

## XML 文档

`<summary>` 三行写法、正文一句、显示宽度不超过 40、不出现「；」与「。」，不写 4.x 对照；细节进 `<remarks>` / `<param>` / `<returns>`。DTO 的注释会成为 OpenAPI 字段说明。

所有注释（`///`、`//`、`/* */`）句末不加「。」，结束标签之前的也不加（`…</param>`）；一行里有多句时句间照常用「。」。`XmlDocumentationTests.Comments_DoNotEndWithAFullStop` 守护 src 与 test。生成的代码同样：代码生成的模板（`Seeds/Templates/**/*.sbn`，`CodeGenerationPreviewTests` 核对产物）、渲染器拼进注释的文案（用 `CodeGenItems.PhraseOf` 去掉消息文案的句号）与前端 `gen-contracts.mjs` 写出的注释都不带。消息目录、本地化文案与异常信息不是注释，照常带标点。提交前：

```bash
grep -rn --include=*.cs '/// <summary>.*</summary>' backend-next | grep -v /obj/   # 应为空
```

## 构建与测试

同级 `XiHan.Framework` 停在锁定提交时，直接：

```bash
dotnet restore backend-next/XiHan.BasicApp.slnx
dotnet build backend-next/XiHan.BasicApp.slnx -c Release --no-restore
dotnet test --solution backend-next/XiHan.BasicApp.slnx -c Release --no-build
```

同级检出前进了（别的会话在框架 `next` 上提交）就按锁定提交做快照，产物放在 `backend-next/artifacts/pin`（已 gitignore；放别处时基线测试与架构测试找不到 `XiHan.BasicApp.slnx`）：

```bash
git -C <XiHan.Framework> archive <锁定提交> global.json next/src next/Directory.Build.props next/Directory.Packages.props next/.editorconfig next/spec | tar -x -C <scratchpad>/fw-<短号>
cd backend-next
dotnet build XiHan.BasicApp.slnx -c Release -p:XiHanFrameworkSrc=<快照>\next\src\ --artifacts-path artifacts/pin
artifacts/pin/bin/XiHan.BasicApp.Architecture.Tests/release/XiHan.BasicApp.Architecture.Tests.exe
artifacts/pin/bin/XiHan.BasicApp.Integration.Tests/release/XiHan.BasicApp.Integration.Tests.exe -maxThreads 4
```

- 新 worktree 里同级没有框架检出，`restore` 也要带 `-p:XiHanFrameworkSrc=…`。
- 测试 exe 是 xUnit v3 进程内运行器：`-class <全名>`、`-method <全名>`；`dotnet test` 走 MTP，单类过滤是 `--filter-class`（一次一个类）。
- `FrameworkPinTests` 读同级检出的 HEAD，快照构建下失败属预期；其余必须全过。
- 外部服务用例读环境变量，没设即跳过：`XIHAN_TEST_POSTGRES`（如 `Host=127.0.0.1;Port=5432;Username=postgres;Password=postgres`）、`XIHAN_TEST_REDIS`、`XIHAN_TEST_RABBITMQ`、`XIHAN_TEST_S3`，代码生成读外部库另有 `XIHAN_TEST_MYSQL`、`XIHAN_TEST_SQLSERVER`、`XIHAN_TEST_ORACLE`。
- 集成测试经 `BasicAppTestServer.StartAsync(connectionString, settings: [...], configure: ..., configureApp: ..., args: ...)` 起宿主（SQLite 临时库）；要开发环境的行为传 `args: ["--environment", Environments.Development]`（建好宿主再改环境名不生效），开发配置把框架日志开到 Debug，用例里配 `Logging:LogLevel:XiHan=Warning` 压回去；演示数据要 `BasicApp:Seed:EnableDemoData=true`；可选模块用 `BasicApp:Modules:<名>:Enabled=false` 关；拨钟用 `ManualClock`，只对进程内调用有效（JWT 用真实时间）。
- 后端全量测试别与前端门禁并行跑，负载高时宿主启动会超时。

## 代码生成的模板

CodeGeneration 的内置模板（`Seeds/Templates/**/*.sbn`，每次启动对齐到模板表）按当前包的写法生成后端包与前端模块：Feature、Permissions、Pages、Errors、Messages、JsonContext、操作、Queries、SchemaPage 页面与 10 种区域文案。改了这些写法（新的架构规则、基类、登记方式、页面组件）要同时改模板，再跑退出验证：

```bash
XIHAN_TEST_CODEGEN_COMPILE=<框架 src 目录> artifacts/pin/bin/XiHan.BasicApp.Integration.Tests/release/XiHan.BasicApp.Integration.Tests.exe -class XiHan.BasicApp.Integration.Tests.CodeGenerationCompileTests
```

它把当前提交导出到临时目录，四个演示新包按「生成到项目」写进去、照生成的登记片段登记，宿主 Release 构建不许有警告，导出契约之后前端 `pnpm check` 八项通过；前端要先在本检出装好依赖。不设变量即跳过，不进缺省 CI。

## 命令

```bash
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- operations          # ID、稳定性、暴露、权限
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- pipeline            # 各阶段中间件次序
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development -- seed --list
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development -- schema upgrade
```

`dotnet run` 以工程目录为工作目录，路径参数一律给绝对路径。用户在 IDE 里调试宿主时 Debug bin 被锁，构建与测试走 `-c Release`。
