# AGENTS.md

本文件约束在 XiHan.BasicApp 仓库内工作的 AI Agent。回答、文档与提交说明使用中文；代码标识、公开 API 和标准技术术语保持英文。

## 仓库概览

XiHan.BasicApp 是基于 XiHan.Framework 的 .NET 10 后端与基于 XiHan.UI 的 Vue 3 前端。后端由能力包组成，业务接口是操作（一份契约同时服务 HTTP、gRPC 与 MCP），权限、多租户与数据范围在后端执行；前端调用由后端导出的契约生成，菜单与路由由后端页面登记下发。

### 技术栈

| 区域 | 技术 | 要求 |
| --- | --- | --- |
| 后端 | .NET 10、XiHan.Framework、SqlSugar | 以 `backend/` 当前工程与 `props/framework.props` 锁定的框架提交为准 |
| 接口 | 操作（`[Operation]`）、OpenAPI、Scalar | 业务接口一律是操作，按 `Expose` 发布到 HTTP / gRPC / MCP |
| 前端 | Vue 3、Vite、TypeScript、Pinia、Vue Router、vue-i18n | Node 24+、pnpm 11+，没有锁文件 |
| UI | XiHan.UI、Tailwind CSS | XiHan.UI 是组件与样式契约真源 |
| 测试 | xUnit（Microsoft.Testing.Platform）、Vitest | 后端架构测试 + 集成测试，前端门禁 + 单测 |

## 开始前必须做

1. 按任务读取对应技能：后端读 `.agents/skills/backend-development/SKILL.md`；前端读 `.agents/skills/frontend-development/SKILL.md`；跨端契约、配置与运维读 `.agents/skills/fullstack-contracts/SKILL.md`。提交信息、PR、Issue、测试审查、更新日志与版本发布另有对应技能。
2. 同时涉及多个职责时加载对应多个技能，不要一次读取无关资料。
3. 检查当前分支、`git status` 和最近提交；保留用户已有改动。在 worktree 中还要按「分支与 worktree」核对分支分组和基线。
4. 阅读目标包的 `README.md`、`<包>Feature.cs`、相邻操作、`Queries/`、测试和配置，确认当前契约；设计依据在 `plans/下一代总体方案.md`，与代码不一致时以代码为准。
5. 跨前后端改动先固定操作 ID、请求 / 响应、权限码、页面登记（路由与组件路径）和错误码。
6. 前端视觉或交互改动必须读取 `.agents/skills/frontend-development/references/frontend.md` 的「视觉」一节。

## 目录结构

```text
/
├─ AGENTS.md
├─ CLAUDE.md -> AGENTS.md
├─ .agents/skills/                      # 仓库技能（.claude/skills 链接到这里）
├─ backend/
│  ├─ XiHan.BasicApp.slnx
│  ├─ props/framework.props             # 框架引用方式与锁定提交 XiHanFrameworkCommit
│  ├─ src/XiHan.BasicApp.<包>/          # 能力包；Host 是宿主（配置、表结构脚本、普通端点）
│  └─ test/                             # Architecture.Tests、Integration.Tests
├─ frontend/
│  ├─ contracts/                        # 后端导出的 openapi.json、realtime.json（入库）
│  ├─ packages/                         # 别名 ~：生成的契约层、请求、壳层、公共组件、文案
│  ├─ src/modules/<包>/                 # 别名 @：各能力包的页面、文案与 setup.ts
│  └─ scripts/                          # gen-contracts 与各门禁
├─ docs/                                # 文档站
├─ plans/                               # 总体方案、对齐矩阵与方案脚本
└─ .github/workflows/ci.yml             # 后端构建测试、契约漂移、前端门禁
```

## 常用命令

### 后端（仓库根）

| 任务 | 命令 |
| --- | --- |
| 还原 | `dotnet restore backend/XiHan.BasicApp.slnx` |
| 构建 | `dotnet build backend/XiHan.BasicApp.slnx -c Release --no-restore` |
| 测试 | `dotnet test --solution backend/XiHan.BasicApp.slnx -c Release --no-build` |
| 启动 | `dotnet run --project backend/src/XiHan.BasicApp.Host --launch-profile Development` |
| 种子 | `dotnet run --project backend/src/XiHan.BasicApp.Host --launch-profile Development -- seed` |
| 导出契约 | `dotnet run --project backend/src/XiHan.BasicApp.Host -- contracts export --out "$PWD/frontend/contracts"` |
| 列出操作 | `dotnet run --project backend/src/XiHan.BasicApp.Host -- operations` |

测试走 Microsoft.Testing.Platform（仓库根 `global.json`）：解决方案必须由 `--solution` 传入；要报告时用 `-- --report-xunit-trx`。连真实 PostgreSQL、Redis、RabbitMQ 的用例读 `XIHAN_TEST_*` 环境变量，没设即跳过。

后端以源码引用 XiHan.Framework 的锁定提交（`backend/props/framework.props` 的 `XiHanFrameworkCommit`，`FrameworkPinTests` 核对同级检出的 HEAD）。同级检出不在锁定提交上时，按后端技能 `references/backend.md` 的快照做法构建（`-p:XiHanFrameworkSrc=<快照> --artifacts-path artifacts/pin`），不要根据磁盘目录自行猜测引用模式。

### 前端（`frontend/`）

| 任务 | 命令 |
| --- | --- |
| 安装 | `pnpm install --no-frozen-lockfile` |
| 开发 | `pnpm dev`（9100，`/api`、`/blobs`、`/hubs` 代理到后端 9710） |
| 生成契约层 | `pnpm gen:contracts` |
| 综合门禁 | `pnpm check`（type-check → lint → px → token → i18n → modules → tests → headers → contracts → permissions） |
| 单测 | `pnpm test` |
| 生产构建 | `pnpm build` |

前端没有锁文件；不要添加 `--frozen-lockfile` 或虚构锁文件流程。

## 后端架构

### 能力包

- 能力包按 Tier 分层，只能向下依赖：Tier 0 `Contracts → Runtime`；Tier 1 `Identity → Tenancy → Organization → Access`；Tier 2 平台能力 `Settings`、`Notification`、`Files`、`Auditing`、`Maintenance`、`OpenPlatform`、`Realtime`；Tier 3 可选模块 `Workbench`、`Workflow`、`Printing`、`CodeGeneration`、`Chat`、`AI`；`Host` 装配。同层之间经 `Contracts` 的接口协作，`TierDependencyTests` 守护。
- 每个包一个 `<包>Feature : IFeature`，在 `Configure` 里显式登记仓储、服务、权限目录、页面、消息目录、中间件、作业与生成的操作；没有约定扫描、`IScopedDependency` 或拦截器。
- 可选模块由 `BasicApp:Modules:<名>:Enabled` 开关：后端装配、页面与权限码下发、前端模块随菜单出现，同一开关语义。

### 操作与权限

- 业务接口是操作：`Operations/<资源 Pascal>/<动词>.cs` 里一个 `[Operation(Id, Expose = …, Permission = …)]` 类实现 `IOperation<TRequest, TResponse>`。不写 Controller 或 Minimal API 业务入口；普通端点只留给文件上传下载、OAuth 重定向、Hub 与 Webhook。
- 操作 ID `<资源>.<动词>`；每个操作显式写 `Expose`（`XHG004` 编译期强制），超出 HTTP 的必须只读并登记在 `OperationExposureTests`；匿名操作登记匿名白名单。
- 权限码 `<包>.<资源>.<动作>`，资源段等于操作 ID 的资源段，在 `<包>Permissions.All` 登记（作用侧、`audited`）；权限目录、菜单门控与种子都由它派生。前端隐藏按钮不能替代后端授权。
- 失败是值（`Error.Validation / NotFound / Conflict / Denied / Unavailable`），错误码 `<包大写>_<KIND>_<NNN>`，消息键同时补 10 个区域文件；不吞异常、不返回伪成功。
- 事务由管道开启；`context.CancellationToken` 一路传下去；独立于调用事务的读写走 `StoreScope`。

### 数据与租户

- 平台库实体不实现 `IMultiTenant`，用 `OwnerTenantId`（平台 = `TenantIds.Platform`）分侧；租户库实体用 `TenantScoped*` 基类，没有租户上下文时什么都查不到。租户只从令牌声明解析。
- 用 `AppEntity`、`AppAuditedEntity`、`AppAggregateRoot` 等现有基类与 SqlSugar 仓储；分页、投影、联表在 `Queries/` 以 `TenantScopedQuery.For<T>` 起手。不引入 EF Core 作为平行业务数据层。
- 软删除实体的唯一约束写 `[ActiveUniqueIndex]`（部分唯一索引），删除后可重新创建。
- 开发库由 Development 的 `InitTables` 建表补列；正式库只认 `backend/src/XiHan.BasicApp.Host/Schema/<版本>/*.sql`（PostgreSQL、可重复执行）与版本台账，`BasicApp:Schema:AutoUpgrade` 开启（缺省）时启动自动执行、失败即中断启动。Agent 不手工对任何库执行脚本。
- 框架的默认内存实现保持零外部依赖；配了 Redis / RabbitMQ 绑定，宿主才装对应 Provider。

## 前端架构与设计

- 页面在 `frontend/src/modules/<包>/views/**/index.vue`，组件路径由后端页面登记决定；每个模块带 `setup.ts` 与 10 个 `locales/<区域>.ts`。跨页面通用能力在 `frontend/packages`。
- `@` 指向 `frontend/src`，`~` 指向 `frontend/packages`（`packages/` 不反向引用 `@/`）；不要建立第三套别名与目录模型。
- 前端不手写接口：操作客户端、类型、权限码与推送方法名都从生成的 `~/contracts` 取。
- 列表页用 `SchemaPage`，新增 / 编辑用 `XEditModal` + `XhFormRoot`，每个字段都有可见标签；按钮 `permission` 填后端权限码。
- 跨页面状态使用 Pinia；筛选、弹窗、选择和分页等页面局部状态留在页面或局部 composable。
- 组件统一使用 XiHan.UI，不引入其他组件库，也不复制第三方组件运行时；组件库缺陷回 XiHan.UI 修，不在 BasicApp 写兼容。
- 圆角按形状角色取：偏好设置的圆角接到组件库的圆角种子 `--xh-radius-seed`，控件、承载面与浮层同一档，内嵌件（菜单与下拉的行、方框、已选标签）取一半，对话框与抽屉朝页面的两角、通知卡片取 1.25 倍；自写样式只写 `--xh-shape-*`，不写 `--xh-radius-*` 档位；pill 仅用于明确的胶囊语义，正方盒取 circle。
- 按钮按下只换面、不缩放（随组件库缺省，不改写 `--xh-motion-scale-press`）；自写的可聚焦元素用与组件库同一道聚焦环（2px 画在盒内：`--xh-ring-width` + `--xh-ring-offset`；面是实心或任意颜色时内侧衬 `--xh-ring-gap` 底色间隔线）；锚定浮层走 frosted（实体底 + 描边 + 柔和投影）；玻璃拟态只用在命令面板、登录与注册页的卡片和右上角工具条、灵动岛、标签总览遮罩、控制中心面板、页面切换的加载遮罩，统一取 `packages/design/ui.css` 的 `.app-glass` / `--app-glass-*`，组件里不另写半透明底与模糊。
- 颜色、间距、圆角、阴影与动效使用 XiHan.UI 语义令牌；新增裸 px 或缺失令牌必须由门禁发现并解决。`.css` 文件和 `.vue` 的 `<style>` 块用与 XiHan.UI 皮肤同一套 stylelint 规则（逻辑属性、颜色、长度与字号只取令牌、过渡不写 all）。
- 弹出层（对话框、抽屉）里针对单个字段的说明收进字段名后面的问号提示（`XFieldLabel` 的 `help`，与偏好设置各项同款），不放在字段下方；只有针对整张表单的说明才留在外面。
- 前端保持紧凑、清晰、任务导向，不在控制台页面使用营销式大标题或装饰性 Hero 区域。

## 前后端契约

- 后端是契约真源：`contracts export` 导出 `frontend/contracts/openapi.json` 与 `realtime.json`，`pnpm gen:contracts` 生成 `packages/contracts/*`；生成物与后端改动同一提交，CI 重新导出比对漂移。
- 操作、请求 / 响应、权限码、页面登记、错误码与推送方法改动时，同步后端测试、契约生成物、前端调用、种子与文档。
- 后端页面登记的组件路径必须能被前端动态映射；可选模块的后端开关、前端 `setup.ts` 和语言资源保持同一开关语义。
- 权限校验以后端为准；前端只改善可用性，不实现安全边界。
- 不增加静默 fallback、伪造空结果或双轨协议；破坏性变更应明确迁移并一次收口。

## 配置与运维

- 配置分框架节 `XiHan:*` 与应用节 `BasicApp:*`；宿主 `appsettings.json` 写出了全部配置项与注释，开发专用值在 `appsettings.Development.json`。框架节的列表与字典没有内容时整项不写（空的 `[]` / `{}` 会让启动失败）。
- 数据库绑定是启动硬要求；Redis、RabbitMQ、Qdrant 等按配置装，配了连不上即启动失败或健康项降级，不改变失败等级来掩盖不可用。
- 诊断服务前从 launchSettings、appsettings、环境变量或启动日志读取实际地址（后端 HTTP 9710、gRPC 9711，前端 9100），不硬编码端口。
- 不提交密码、Token、连接串或真实生产数据；示例配置使用明确占位符。
- 不执行数据库升级脚本、部署、生产迁移、外部消息或发布，除非用户明确要求。

## 测试

- 后端修改先跑相关的集成测试类，再跑解决方案构建与全量测试；Release 构建 0 警告。
- 架构测试守护层级、`Expose`、权限码文法、匿名白名单、目录结构、文案键集、DTO 写法与 XML 文档格式；集成测试经 `BasicAppTestServer` 走真实 HTTP，验证权限、租户隔离、事务、取消与错误码。
- 前端逻辑使用 Vitest，测试放所属包或模块的 `tests/` 目录、按被测文件的子路径摆放、命名 `*.spec.ts`；视觉与交互改动还要在真实浏览器检查亮 / 暗色、窄视口、键盘、焦点和 reduced motion。
- 修改共享组件、路由、请求、权限或主题时，运行 `pnpm check`、`pnpm test` 和 `pnpm build`。
- 不删除断言、忽略失败、扩大 allowlist 或降低门禁来制造绿色结果。

## 分支与 worktree

在主检出上的日常改动直接提交到当前开发分支（`next`，并入 `dev` 之后以 `dev` 为准），不另开分支。新功能或并行开发开启 worktree 时，分支按用途分组命名，不保留工具生成的 `claude/<名>`、`codex/<名>` 等前缀：

| 分组 | 用途 | 分支名 |
| --- | --- | --- |
| `feat/` | 新功能、增强，以及重构、性能、文档、测试等非缺陷改动 | `feat/<worktree 名>`，如 `feat/confident-morse-925efe` |
| `fix/` | 缺陷修复 | `fix/<worktree 名>`，如 `fix/confident-morse-925efe` |
| `issue/` | 处理指定的 GitHub Issue | `issue/<编号>`，如 `issue/46` |
| `pr/` | 审阅或接手已有 Pull Request | 拉取他人 PR 审阅用 `pr/<编号>-review`；没有编号时用 `pr/<worktree 名>` |

- 任务对应 Issue 或 PR 编号时归入 `issue/`、`pr/` 并以编号命名；其余按改动性质取 `feat/` 或 `fix/`，后缀沿用 worktree 目录名。
- 进入 worktree 后先看 `git branch --show-current`。分支是工具生成的 `claude/<名>` 等形式时，在首次提交前就地改名：`git branch -m <分组>/<名>`；worktree 目录名不变。改名后上游若指向 `main` 等别的分支，执行 `git branch --unset-upstream`。分支已推送到远端时不改名，先向用户说明。
- 自行创建时直接带分组，以当前开发分支为基线：`git worktree add -b feat/<名> .claude/worktrees/<名> next`。审阅他人 PR 时先 `git fetch origin pull/<编号>/head:pr/<编号>-review`，再 `git worktree add .claude/worktrees/pr-<编号> pr/<编号>-review`。
- 发布分支：预览版（带预发布后缀的版本）从 `preview` 分支发，由 `next` 快进（`git fetch . next:preview`，不是快进就停下查）；正式版从 `main` 发，由 `dev` 快进。这两个分支只接收发版，日常改动不提交到这里，worktree 也不以它们为基线；修复先进开发分支，再快进过来。`ci.yml` 在开发分支与发布分支上都跑；仓库没有发包工作流，打标签与建 GitHub Release 按 `version-release` 技能手工做。
- 基线默认是当前开发分支；热修复以对应 `hotfix/v<主版本>` 为基线，用户指定 `release/*` 时以其为准。工具从 `main` 或 `origin/main` 切出且还没有提交时，用 `git merge --ff-only <开发分支>` 对齐；不能快进就停下说明，不 reset。
- 一个 worktree 只承载一项任务、一条分支。并行任务会改到同一操作或 DTO、权限码、页面登记种子或同一版本的 `Schema/<版本>` 脚本时，先说明冲突风险再动手。
- 新 worktree 没有还原结果、`node_modules` 和被 gitignore 的本地配置（如 `frontend/.env.local`）：后端先执行 `dotnet restore backend/XiHan.BasicApp.slnx`（同级没有框架检出时带 `-p:XiHanFrameworkSrc=<快照>`），前端在 `frontend/` 执行 `pnpm install --no-frozen-lockfile`。
- worktree 分支的合并、推送、创建 PR 以及 worktree 和分支的删除，只在用户明确要求时执行。

## 注释与文档

- 注释简洁明了：只写代码表达不了的意图、约束与理由，不写铺垫、复述代码或多余的话。
- 按全新仓库写：代码、注释、文档里不写旧版本对照与来源（旧版、照搬、搬入、重写前后），只写现在的事实；真实版本值（表结构目录 `6.0.0`、版本号）保留。
- 文档正文不写产品与自有依赖的版本（版本在 `backend/Directory.Build.props`、`frontend/package.json`、`backend/Directory.Packages.props` 里）；运行环境要求与第三方镜像 tag 保留，更新日志例外。
- 源码文件以版权头开头：后端 C# 两行 `//`，前端 `.ts` / `.css` 用 `/* */`、`.vue` 用 `<!-- -->`（与 XiHan.UI 同款，测试不加）。
- C# XML 文档与前端注释的格式见后端、前端技能（`<summary>` 三行一句，注释句末不加「。」）。

## Git 与提交

- 保留用户已有和无关改动；不要 reset、checkout 或清理它们。
- 使用 Conventional Commits：一行 `<type>(<scope>): <中文说明>`，64 字以内，不写正文。
- 每项完整功能独立提交；同一契约所需的后端、前端、测试、种子和文档可以同一提交。
- 提交前运行 `git diff --check`、相关测试和必要构建。
- 不推送、不发布、不创建远程 PR，除非用户明确要求。

## 禁止事项

- 不把业务接口写成 Controller 或 Minimal API，不绕过操作管道。
- 不绕过权限、租户过滤或数据范围。
- 不引入 EF Core、其他组件库或另一套通用脚手架替代当前技术栈。
- 不在 Framework 放置 BasicApp 专用 Provider，也不在前端复制后端业务规则。
- 不添加静默兜底、推测性兼容或无限重试。
- 不手改构建产物与生成物，不通过更新基线或 allowlist 掩盖生产问题。
- 代码、文档与提交信息里不写参考项目名。
