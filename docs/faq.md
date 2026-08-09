# 常见问题

按「**现象 → 原因 → 解决**」组织的高频故障速查表。每条都对照仓库源码核实过，看到相同现象直接照做。

[[toc]]

---

## 启动与环境

### 后端和前端各自跑在哪个端口？

| 进程 | 端口 | 备注 |
| --- | --- | --- |
| 后端 Development | `9708` | API 文档 `http://127.0.0.1:9708/scalar` |
| 后端 Production | `9708`（仓库默认） | 可由 `Hosting:Urls` / 容器端口映射覆盖 |
| 前端 dev server | `9800` | `VITE_PORT` |

开发态前端**不直连后端**：`VITE_API_BASE_URL` 留空，请求打到自己的 dev server，再由 Vite 代理转发到 `VITE_DEV_PROXY_TARGET`（默认 `http://localhost:9708`）——同源转发，天然没有 CORS 问题。

::: tip 改后端端口后前端连不上
改的是 `Hosting:Urls` 或 launch profile，别忘了同步改 `frontend/.env.development` 的 `VITE_DEV_PROXY_TARGET`。
:::

### 前端报跨域（CORS）

生产部署前后端**不同源**时才会遇到。三条路，选一条：

1. 前端与 API 同域，由 Nginx 反向代理 `/api`（推荐，仓库自带 `frontend/nginx.conf`）。
2. 配后端网关的 `Gateway.AllowedOrigins` 放行前端源。
3. 开发期保持走 Vite 代理，别把 `VITE_API_BASE_URL` 填成后端绝对地址。

### 启动报 Redis `WRONGPASS`

仓库默认连接串用的是 **ACL 用户** `user=redis,password=redis`，而官方 `redis` 镜像默认既无密码也没有名为 `redis` 的用户。按 [开发环境](./dev-environment#redis-6-生产多实例必需) 里的 `docker run` 命令建好同名 ACL 用户即可；本地图省事也可以把 `XiHan:Caching:Redis:IsEnabled` 设为 `false` 退化成进程内内存缓存（会失去分布式缓存/锁/队列）。

### 表没建好 / 首次启动就登录不了

全新数据库首次启动会**自动建表 + 播种子数据**。初始化中途失败时，可在确认没有业务数据后删库重试；存量数据库不要直接删除，应先核对 `UpdateScripts` 与实际迁移入口，见[下文](#部署后接口报-42703-column-does-not-exist)。

### 初始超管账号是什么？

| 字段 | 值 |
| --- | --- |
| 账号 | `superadmin` |
| 密码 | `SuperAdmin@123` |

初始密码可用配置 `Saas:Seed:SuperAdminPassword`（环境变量写法 `Saas__Seed__SuperAdminPassword`）覆盖。**生产务必覆盖，并在首次登录后立即修改。**

### 不想要演示数据

种子分两类：**系统基线**（身份/权限/菜单/字典等，始终播种，是应用能跑的最小骨架）与**演示数据**（示例组织、演示账号、演示业务租户）。把 `Saas:Seed:EnableDemoData` 显式设为 `false` 即整体跳过演示种子；**缺省或非法值都视为启用**。

---

## 认证与令牌

### 怎么获取 token？

`POST /api/Auth/Login`，body 传 `username`（**邮箱**）+ `password`，成功后从 `data.token.accessToken` 取。完整流程（含 2FA 分支、验证码登录、刷新、切租户）与可直接照抄的 curl 见 [接口对接指南 · 怎么获取 token](./api-guide#怎么获取-token-完整实操)。

### 登录一直提示账号或密码错误

`username` 字段要传**邮箱**——邮箱是全平台唯一登录标识；只有平台账号（`TenantId=0`，如超管）才可以用用户名登录。

### 接口一直 401，甚至陷入「刷新 → 401 → 刷新」

按这个顺序查：

1. 请求头写全了吗：`Authorization: Bearer <accessToken>`，`Bearer` 后有一个空格。
2. Access Token 默认 60 分钟就过期了，要用 `POST /api/Auth/RefreshToken` 换新，**必须同时提交旧 `accessToken` 与 `refreshToken`**，缺一即失败。
3. Refresh Token 默认 7 天，过期只能重新登录。
4. 会话被撤销（管理员踢下线、删除用户、超出最大设备数被顶）后，令牌即便没过期也不再有效。

前端 `RequestClient` 已内建自动刷新与并发排队，刷新请求自身带 `_isRefresh` 标记不会再次触发刷新，不存在死循环；如果你在裸调接口，自己实现时也要加这个标记。

### 接口返回 423，前端弹回了登录页

**423 不是 401**。它表示会话被锁屏——用户身份**仍然有效**，只是当前会话锁住了。正确处理是弹出解锁遮罩、调 `POST /api/Auth/UnlockSession`，**不要**清 token 跳登录页。

### 给用户加了权限，需要重新登录才生效吗？

**不需要**。权限码不冻结进 JWT（超管例外，只放一个通配 `*` 作快路径），鉴权走服务端**授权快照**实时判定，授予/回收立刻生效。

### 切换租户会不会把我原来的登录顶掉？

不会。切租户是**复用当前会话轮换令牌**：不发登录事件、不新增设备记录。调 `POST /api/Auth/SwitchTenant`，`tenantId` 传 `null` 即回到平台运维态。

---

## 权限与菜单

### 新加的菜单在前端不显示

菜单种子是 **fail-closed** 的：`SaasMenuSeeder` 先按 `PermissionCode` 去查 `SysPermission`，**查不到就跳过该菜单并打 Warning**（日志里搜「依赖权限 ... 不存在，跳过初始化」）。所以：

1. 权限码得先在 `SaasPermissionCodes` 里定义，并追加进 `All`；
2. 权限**定义**要加进 `SaasPermissionDefinitions.Groups`（这才是落库的那份），权限种子 `Order=20` 必须排在菜单种子 `Order=25` 之前；
3. `PageRegistry` 里父目录要排在子项之前（种子按顺序解析 `ParentId`）；
4. 新增独立模块时，种子链必须保持「**操作 → 资源 → 权限 → 菜单 → 角色授权**」完整顺序，缺了 `SysOperationSeeder` 会让整条链静默失效（权限由「资源 × 操作」派生）。

改完重建库或重跑种子。详见 [二次开发 · 接线点检查清单](./backend/development#接线点检查清单)。

### 菜单出来了，但点进去 404 / 白屏

后端 `PageDescriptor.Component` 决定前端视图落点：约定 `Component` = `Path` 去掉前导斜杠 + `/index`，对应 `frontend/src/views/**/index.vue`，由 `import.meta.glob` 匹配。

例外是 `_core` 页面（个人中心、仪表盘、关于页等，落在 `packages/views/_core`）：`Component` 写 `_core/xxx/index`，**必须同时在 `packages/router/dynamic.ts` 的 `coreComponentMap` 里登记**，否则匹配不到组件、回退 not-found。

### 我有这个权限码，接口还是 403

依次排查：

1. **租户版本（Edition）门控**：租户所属版本的权限白名单没放行该权限码，运行时会被拦掉。
2. **数据范围**：权限码通过了，但数据范围（本人 / 部门 / 部门及下级 / 租户）把这条记录挡在外面——表现常是「有权限但查不到数据」而非 403。
3. **约束规则引擎**：SSD/DSD、互斥、时间窗、IP 等约束命中，被拒绝。
4. **权限委托已到期**或被撤销。

完整判定链见 [权限模型](./backend/permission)。

### 列表少了几列 / 排序点了没反应

**字段级安全（FLS）在服务端门控**：读侧经 `IFieldSecurityService.GuardFiltersAsync` / `GuardSortsAsync` 过滤，**只有「可读且未脱敏」的字段**才允许参与过滤与排序，其余被静默剔除；剔完没有有效排序时回退默认排序。前端 Schema 页也会按字段 `permission` 直接不渲染该列。所以是权限问题，不是 bug。

---

## 接口与联调

### 新加的分页方法收不到请求体

分页方法**必须显式标 `[HttpPost]`**。方法名以 `Get` 开头会被动态 API 约定推导成 GET，body 自然绑不上。

```csharp
[PermissionAuthorize(SaasPermissionCodes.Position.Read)]
[HttpPost]  // ← 别漏
public async Task<PageResultDtoBase<PositionListItemDto>> GetPositionPageAsync(
    PositionPageQueryDto input, CancellationToken cancellationToken = default) { … }
```

### 生成的路由和我预期的不一样

动态 API 按方法名推导路由，三条容易忘的规则：

1. **动词前缀被剥离**：`GetUserPageAsync` → 动作名 `UserPage`（不是 `GetUserPage`）。
2. **无匹配动词前缀时默认 POST**：`SwitchTenantAsync` → `POST /api/Auth/SwitchTenant`。
3. **路由段只由显式 `[FromRoute]` 参数产生**。普通参数一律走查询串或 body，**不会**变成 `/{id}` 路径段。想要 `/api/User/User/{id}` 必须给参数标 `[FromRoute]`。

前缀匹配要求词边界（前缀后必须是大写字母或下划线），所以 `AddressBook` 不会被 `Add` 命中。完整推导表见 [接口对接指南](./api-guide#动态-api-路由推导规则)。

### 返回的 ID 变成了字符串

框架把 **`long` 统一序列化为 JSON 字符串**，避免 JavaScript Number 精度溢出（雪花 ID 会溢出 53 位安全整数）。前端类型标成 `string`，需要运算时显式 `Number()`。反序列化时数字与字符串都接受。

### 响应里某个字段整个不见了

服务端 `DefaultIgnoreCondition = WhenWritingNull`：**值为 null 的字段直接不输出**。客户端把字段按可选处理，别用「字段存在与否」判断成功——判定成功请读 `isSuccess`。

### `oAuthProviders` 还是 `oauthProviders`？

`oAuthProviders`。camelCase 命名策略只把**首字母**变小写，`OAuthProviders` → `oAuthProviders`。同类还有 `oAuthApps` 等，抄字段名时留意。

### 时间显示差了几个小时

后端**存储恒为 UTC**，输出时按请求头 `X-Timezone`（IANA 标识，如 `Asia/Shanghai`）换算。不发这个头就按服务端默认时区输出。前端已在请求拦截器里自动带上（用户已选时区优先，否则跟随浏览器 `Intl`）。

### 枚举值到底发数字还是发字符串？

- **响应**：枚举输出为**成员名字符串**（如 `"Enabled"`）。唯一例外是 `ApiResponse.code`，恒为 int。
- **请求**：数字和成员名都接受。前端契约层统一发数字（`QueryOperator.Between` = `4000` 等）。

---

## 后端开发

### 运行期 DI 解析不到我的领域服务

**领域服务必须手写注册**。领域服务接口不带 `IScopedDependency` / `IDomainService` 之类的 DI 标记接口，框架的约定注册扫不到它，得在 `Extensions/ServiceCollectionExtensions.cs` 的 `AddSaasDomainServices`（或模块自己的对应方法）里显式登记：

```csharp
services.AddScoped<IPositionDomainService, PositionDomainService>();
```

对比之下：**仓储**（继承 `SaasRepository` → `IScopedDependency`）和**应用服务 / 查询服务**（实现 `IApplicationService`）都由约定自动注册，不用手写。这是纵切片最常见的漏接线点。

### 我覆盖了框架的默认实现，但跑起来还是默认的

框架模块用 **`TryAdd`** 先注册默认实现（`IPermissionChecker`、`IUserStore`、`IJobStore`、`IAiProviderConfigStore`、各 Bot `*ConfigStore` 等），你**再 `TryAdd` 会被静默忽略**。覆盖必须用 `Replace`：

```csharp
services.Replace(ServiceDescriptor.Singleton<IAiProviderConfigStore, SaasAiProviderConfigStore>());
```

### 事件处理器不触发

本地事件总线只自动发现「**以接口为服务类型**」的注册，裸 `services.AddTransient<具体处理器>()` **不会被订阅、静默失败**。处理器必须显式加入 `XiHanLocalEventBusOptions.Handlers`——Saas 封装了 `AddSaasLocalEventHandler<T>()`（内部 `AddTransient` + `Configure<XiHanLocalEventBusOptions>(o => o.Handlers.AddIfNotContains(typeof(T)))`），在 `AddSaasEventHandlers` 里登记即可。

### 匿名 Minimal API 端点调应用服务时永久挂起

匿名端点没有工作单元中间件，而应用服务被 Castle 代理包着，拦截器会急切开事务从而死锁。解法是**绕开代理**：注入真正的依赖直连，或用 `ProxyHelper.UnProxy` 取出真实目标实例再调。仓库里的 OAuth 回调端点就是这么处理的（`ExternalLoginAsync` 标 `[DynamicApi(IsEnabled = false)]` 不对外暴露，只由回调端点经未代理实例调用）。

### 数据变更日志（Diff）一条都没有

需要两个条件同时成立：

1. 配置 **`XiHan:Data:SqlSugarCore:EnableDiffLog` 设为 `true`**（**默认是 `false`**，不开则 AOP 不挂载、差异被直接丢弃）；
2. 写操作走仓储并显式调用了 `.EnableDiffLogEvent(businessData)`。

生产环境的 `appsettings` 常被 gitignore，容易漏配，记得在服务器上单独补。另外只有走仓储的写才会被覆盖到。

### 部署后接口报 `42703 column does not exist`

`DbInitializer` **表存在就跳过创建**（日志里是「表已存在，跳过创建」），它**从不为已有表补列**。所以给既有实体加字段后部署必炸。

不要依赖 CodeFirst 给已有表补列。正式版本应在 `WebHost/UpdateScripts/{version}.sql` 中加入前向迁移，升级引擎会按版本执行并记录到 `SysMigrationHistory`；临时环境也可重建库或手工 `ALTER TABLE`。

### 本地 `dotnet build` 报文件被占用

运行中的应用会锁住 DLL。停掉正在运行的实例再构建。后端由部署流程在服务器上构建/发布，本地改动需部署后才生效，线上诊断以加日志为主。

---

## 前端

### 页面整片白屏，控制台报 `Invalid linked format`

语言包文案里出现了**裸 `@`**（如 `联系 @admin`）。vue-i18n 会把 `@` 当作 linked message 语法解析并抛错。必须转义成 <code v-pre>{'@'}</code>：

```ts
// ❌ 白屏
contact: '联系 @admin'
// ✅
contact: "联系 {'@'}admin"
```

新增文案前先扫一遍裸 `@`。

### 图标显示成空白

图标走 Iconify **离线模式**，运行期只保证 **`lucide` / `tabler` / `mdi` / `simple-icons`** 这四个已预加载的图标集能直接渲染。用了未预加载的集（carbon / ep / heroicons 等）会渲染为空——离线 `Icon` 对已挂载组件不会因后加载而重渲染。品牌图标优先用 `simple-icons:*` 或 `tabler:brand-*`。

### 上传的文件在线上打不开（404）

本地存储返回的是**根相对路径** `/uploads/...`。前后端同源没问题，**线上前后端不同源时必须拼上 `VITE_API_BASE_URL` 的 origin**。仓库里的 `toAbsoluteFileUrl` / `useAvatarUrl` 已经处理了这件事，自己拼 URL 时别忘了。

### 跑 lint 把不相干的文件改坏了

`pnpm run lint:fix` 展开是 `oxlint --fix && eslint . --fix`，会扫全仓并改动与你无关的文件。**只对本次改动的文件跑**：

```bash
npx eslint src/views/identity/position/index.vue --fix
```

误跑了就 `git checkout HEAD -- <非本次任务的文件>` 还原。

---

## 部署与运维

### 升级版本要写数据迁移吗？

**要写前向 SQL 迁移。** 首次部署由 CodeFirst 建库建表；已有数据库的变更放入 `WebHost/UpdateScripts/{version}.sql`。升级引擎具备逐库执行、台账与失败状态能力，但当前 BasicApp 没有调用执行入口，启动不会自动跑脚本；发布流程必须显式补齐或外置唯一迁移步骤。当前脚本为 PostgreSQL 方言，切换数据库时需提供对应实现。详见[升级与迁移](./backend/upgrade)。

### 启用 AI 知识库（RAG）前要准备什么？

需要先部署 **Qdrant 向量库**并配好 `XiHan:AI:Rag` 连接参数。全新数据库会自动创建 RAG 表并执行种子；存量库应随版本升级脚本迁移。见 [AI 能力](./backend/ai) 与 [开发环境 · Qdrant](./dev-environment#向量数据库-qdrant-可选-ai-知识库用)。

### 怎么定位一次线上请求？

每个响应都带 `traceId`（信封字段）与 `X-Trace-Id` 响应头。拿它去日志、审计日志、链路时间线里检索，就能串起这一次请求的访问 / API / 操作 / 异常 / 实体变更全部记录。启用 OpenTelemetry（`XiHan:Observability`，**默认关闭**）后该值就是 W3C 的 32-hex TraceId，可直接在 Jaeger / Tempo 里查。

---

## 还没解决？

- [接口对接指南](./api-guide)：响应信封、业务码、请求头、分页协议的完整规范
- [二次开发](./backend/development)：新增功能的完整接线清单（漏哪一步会怎样）
- [权限模型](./backend/permission)：403 / 查不到数据的判定链
- [框架常见问题](https://framework.docs.xihanfun.com/guide/faq)：模块装配、DI、事务、拦截器层面的坑
- 仍未解决可到 [GitHub Issues](https://github.com/XiHanFun/XiHan.BasicApp/issues) 提问，附上 `traceId` 与复现步骤
