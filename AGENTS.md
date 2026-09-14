# AGENTS.md

本文件约束在 XiHan.BasicApp 仓库内工作的 AI Agent。回答、文档与提交说明使用中文；代码标识、公开 API 和标准技术术语保持英文。

## 仓库概览

XiHan.BasicApp 是基于 XiHan.Framework 的 .NET 10 模块化后端与基于 XiHan.UI 的 Vue 3 管理端。后端以 Dynamic API、DDD/CQRS、SqlSugar、权限与多租户为核心；前端以类型化 API、Pinia、动态路由和统一设计令牌为核心。

### 技术栈

| 区域 | 技术 | 要求 |
| --- | --- | --- |
| 后端 | .NET、XiHan.Framework、SqlSugar | 以 `backend/` 当前项目与 props 为准 |
| API | Dynamic API、Scalar | 普通业务接口由 Application Service 暴露 |
| 前端 | Vue、Vite、TypeScript、Pinia、Vue Router | Node 24+、pnpm 11+ |
| UI | XiHan.UI、Tailwind CSS | XiHan.UI 是组件与样式契约真源 |
| 测试 | xUnit、Vitest | 后端与前端分别验证 |

## 开始前必须做

1. 读取 `.agents/skills/xihan-basicapp/SKILL.md`。
2. 按任务类型读取该技能指向的 references，不要一次加载无关资料。
3. 检查当前分支、`git status` 和最近提交；保留用户已有改动。
4. 阅读目标模块 README、相邻实现、测试和配置，确认当前契约。
5. 跨前后端改动先固定 DTO、Dynamic API 路由、权限码、菜单组件路径和错误语义。
6. 前端视觉或交互改动必须读取 `.agents/skills/xihan-basicapp/references/frontend.md`。

## 目录结构

```text
/
├─ AGENTS.md
├─ .agents/skills/xihan-basicapp/       # Codex 仓库技能与工程规范
├─ backend/
│  ├─ XiHan.BasicApp.slnx
│  ├─ props/                            # 公共构建与 Framework 引用策略
│  ├─ src/
│  │  ├─ framework/                     # BasicApp 公共基础层
│  │  ├─ modules/                       # Saas、AI、Chat、Workflow 等模块
│  │  ├─ business/                      # 业务示例/扩展
│  │  └─ main/XiHan.BasicApp.WebHost/   # 应用宿主
│  └─ test/
├─ frontend/
│  ├─ src/                              # 应用 API、页面、路由与插件模块
│  ├─ packages/                         # 跨页面共享前端能力
│  └─ scripts/                          # i18n、模块、px、token 门禁
└─ docs/
```

## 常用命令

### 后端（仓库根）

| 任务 | 命令 |
| --- | --- |
| 还原 | `dotnet restore backend/XiHan.BasicApp.slnx` |
| 构建 | `dotnet build backend/XiHan.BasicApp.slnx -c Release --no-restore` |
| 测试 | `dotnet test backend/XiHan.BasicApp.slnx -c Release --no-build` |
| 启动 | `dotnet run --project backend/src/main/XiHan.BasicApp.WebHost/XiHan.BasicApp.WebHost.csproj --launch-profile Development` |

`XiHan.BasicApp.slnx` 默认使用 XiHan.Framework NuGet 包。联调同级 Framework 源码时，通过工作区解决方案或显式 `-p:UseXiHanFrameworkSource=true`；不要根据磁盘目录自行猜测引用模式。

### 前端（`frontend/`）

| 任务 | 命令 |
| --- | --- |
| 安装 | `pnpm install --no-frozen-lockfile` |
| 开发 | `pnpm dev` |
| 类型检查 | `pnpm type-check` |
| Lint | `pnpm lint` |
| 单测 | `pnpm test` |
| 综合检查 | `pnpm check` |
| 生产构建 | `pnpm build` |

前端没有锁文件；不要添加 `--frozen-lockfile` 或虚构锁文件流程。CI 当前分别执行类型、lint、裸 px 和文案/模块门禁，完整功能交付还应按改动补跑测试和生产构建。

## 后端架构

### 模块归属

- `Saas` 是租户、身份、RBAC/ABAC、配置、日志和平台基础能力的基座。
- `AI`、`Chat`、`CodeGeneration`、`Printing`、`Workflow` 是可选业务模块；依赖必须显式。
- 公共实体、DTO 和查询基类放在 `src/framework/XiHan.BasicApp.Core`；Web 公共接线放在 `XiHan.BasicApp.Web.Core`。
- 业务规则归属 Domain，命令与查询编排归属 Application，SqlSugar 实现归属 Infrastructure。

### API 与权限

- 普通业务 API 使用 `[DynamicApi]` 的 Application Service/Query Service，不创建 Controller 或 Minimal API 平行入口。
- 写操作沿用 `[UnitOfWork(true)]`；受保护操作使用 `[PermissionAuthorize(...)]`。
- 权限码由所属模块集中定义并进入种子数据；前端隐藏按钮不能替代后端授权。
- 方法签名、DTO、Dynamic API 服务名与前端类型化客户端必须同步变更。
- 异步方法接受并传递 `CancellationToken`，不吞异常或返回伪成功。

### 数据与租户

- 使用 `BasicAppEntity`、`BasicAppFullAuditedEntity`、`BasicAppAggregateRoot` 等现有基类。
- 使用 SqlSugar 特性和现有仓储契约；不引入 EF Core 作为平行业务数据层。
- 平台/全局记录固定 `TenantId = 0`，租户记录使用当前租户；不以 nullable TenantId 表示全局。
- 软删除实体的唯一索引必须包含 `IsDeleted`，保持删除后可重新创建的语义。
- 数据库升级脚本放在 WebHost 的 `UpdateScripts/<version>/<version>.sql`，面向 PostgreSQL、可重复检查且不自动执行。
- Framework 的默认内存实现保持零外部依赖；BasicApp 负责 Redis/数据库等分布式实现。

## 前端架构与设计

- 页面位于 `frontend/src/views`，类型化 API 位于 `frontend/src/api/modules`，跨页面通用能力位于 `frontend/packages`。
- `@` 指向 `frontend/src`，`~` 指向 `frontend/packages`；不要建立第三套别名与目录模型。
- Dynamic API 使用 `createDynamicApiClient`、`createCommandApi`、`createReadApi` 和 `formatDynamicApiRouteValue` 等现有工具。
- 后端菜单驱动动态路由；插件模块只在 `frontend/src/modules/<module>` 放置其页面、API、语言和 `setup.ts`。
- 跨页面状态使用 Pinia；筛选、弹窗、选择和分页等页面局部状态留在页面或局部 composable。
- 组件统一使用 XiHan.UI；不得继续使用或重新引入 Naive UI，也不复制第三方组件运行时。
- 普通 control 4px、surface 8px、overlay 12px；pill 仅用于明确的胶囊语义。
- 离散按钮按下 120ms 缩放至 0.97，释放 200ms；透明浮层只用 frosted 柔和模糊，禁止 glass 材质。
- 颜色、间距、圆角、阴影与动效使用 XiHan.UI 语义令牌；新增裸 px 或缺失令牌必须由门禁发现并解决。
- 管理端保持紧凑、清晰、任务导向，不在控制台页面使用营销式大标题或装饰性 Hero 区域。

## 前后端契约

- 后端 DTO 是网络契约真源，前端 `*.types.ts` 与其字段、nullability、分页和日期格式一致。
- Dynamic API 服务/方法改名必须同步 API facade、调用点、权限、测试和文档。
- 后端菜单 `Component` 路径必须能被前端动态映射；可选模块的后端注册、前端 `setup.ts` 和语言资源保持同一开关语义。
- 权限校验以后端为准；前端只改善可用性，不实现安全边界。
- 不增加静默 fallback、伪造空结果或双轨旧协议；破坏性变更应明确迁移并一次收口。

## 配置与运维

- 数据库配置是启动硬要求；Redis 和 Qdrant 等能力按当前配置与健康检查语义处理，不改变失败等级来掩盖不可用。
- 诊断服务前从 launchSettings、appsettings、环境变量或启动日志读取实际地址，不硬编码端口。
- 不提交密码、Token、连接串或真实生产数据；示例配置使用明确占位符。
- 不执行数据库升级脚本、部署、生产迁移、外部消息或发布，除非用户明确要求。

## 测试

- 后端修改先跑所属模块测试，再跑解决方案构建与测试。
- Dynamic API 必须验证权限标注、DTO 映射、租户隔离、事务、取消和异常语义。
- 前端逻辑使用 Vitest；视觉与交互改动还要在真实浏览器检查亮/暗色、窄视口、键盘、焦点和 reduced motion。
- 修改共享组件、路由、请求、权限或主题时，运行 `pnpm check`、`pnpm test` 和 `pnpm build`。
- 不删除断言、忽略失败、扩大 allowlist 或降低门禁来制造绿色结果。

## Git 与提交

- 保留用户已有和无关改动；不要 reset、checkout 或清理它们。
- 使用 Conventional Commits：`<type>(<scope>): <中文说明>`。
- 每项完整功能独立提交；同一契约所需的后端、前端、测试、种子和文档可以同一提交。
- 提交前运行 `git diff --check`、相关测试和必要构建。
- 不推送、不发布、不创建远程 PR，除非用户明确要求。

## 禁止事项

- 不把普通业务接口改写成 Controller 或 Minimal API 双轨实现。
- 不以 nullable TenantId 表示全局数据，不绕过权限或租户过滤。
- 不引入 EF Core、Naive UI 或另一套通用脚手架替代当前技术栈。
- 不在 Framework 放置 BasicApp 专用 Provider，也不在前端复制后端业务规则。
- 不添加静默兜底、推测性兼容或无限重试。
- 不手改构建产物，不通过更新基线或 allowlist 掩盖生产问题。
