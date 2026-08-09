# 请求生命周期

一个请求从进入进程到返回 JSON，中间经过哪些环节、每个环节能改变什么、出问题该往哪一段看——这页把整条链路摊开。**排查 401 / 403 / 423 / 租户串数据 / 时间不对 / 事务没提交，都从这张图开始定位。**

## 全景

```text
浏览器 / 客户端
   │  Authorization: Bearer …   X-Language: zh-CN   X-Timezone: Asia/Shanghai
   ▼
┌─ ASP.NET Core 中间件管道（由框架 XiHanWebApiModule 编排，顺序经过设计） ──────────┐
│                                                                                  │
│  ① UseForwardedHeaders          还原反向代理转发头（scheme/host/客户端 IP）        │
│  ② XiHanTraceIdMiddleware       分配/透传 TraceId，写入响应头 X-Trace-Id          │
│  ③ UseXiHanRequestCulture       按 X-Language 设置请求文化                        │
│  ④ XiHanRequestContextMiddleware 建立请求上下文                                   │
│  ⑤ XiHanExceptionLoggingMiddleware 异常日志                                      │
│  ⑥ XiHanRequestLoggingMiddleware   请求日志                                      │
│  ⑦ UseRouting                   路由匹配                                          │
│  ⑧ UseRateLimiter               限流（配置开关，默认关）                           │
│  ⑨ XiHanCircuitBreakingMiddleware 熔断（配置开关，默认关）                         │
│  ⑩ UseCors                      跨域                                              │
│  ⑪ 本地对象存储静态文件           /uploads/** 匿名直链（在鉴权之前）                │
│  ⑫ XiHanApiLoggingMiddleware    API 日志                                          │
│  ⑬ XiHanOpenApiSecurityMiddleware 开放接口签名/防重放/加解密                       │
│  ⑭ UseAuthentication            JWT 认证 → 填充 ClaimsPrincipal                   │
│  ⑮ XiHanTenantResolveMiddleware 租户解析 → 建立 ICurrentTenant                    │
│  ⑯ XiHanSessionStateMiddleware  会话闸门：失效→401，锁屏→423                       │
│  ⑰ UseAuthorization             授权（权限码判定）                                 │
│  ⑱ UseEndpoints                 命中动态 API 生成的 Controller Action              │
│                                                                                  │
│  ★ BasicApp 追加：Telegram Webhook 中间件（鉴权之前，强校验 secret_token）          │
└──────────────────────────────────────────────────────────────────────────────────┘
   ▼
┌─ 应用层 ─────────────────────────────────────────────────────────────────────────┐
│  写侧 *AppService（[UnitOfWork] + [PermissionAuthorize]）                          │
│      └→ 领域服务（业务规则） → 仓储（SqlSugar，自动挂租户/软删过滤） → 数据库        │
│  读侧 *QueryService                                                               │
│      └→ 分布式缓存命中？ → 命中直接返回 / 未命中查库回填                            │
└──────────────────────────────────────────────────────────────────────────────────┘
   ▼
┌─ 收尾 ───────────────────────────────────────────────────────────────────────────┐
│  UoW 提交 → 本地事件（提交前）/ 分布式事件（提交后）→ 精准失效缓存 → 队列入队       │
│  响应过滤器包 ApiResponse 信封；本地化覆盖 Data；时间按 X-Timezone 换算            │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## 逐段说明

### ① 转发头还原必须在最前

`UseForwardedHeaders` 先于一切读取 scheme / host / 客户端 IP 的中间件（路由、鉴权、CORS、重定向生成）。放错位置的表现是：反向代理后面拿到的客户端 IP 全是网关 IP、生成的重定向 URL 是 `http://` 而不是 `https://`。

### ② TraceId

优先取 W3C `Activity.TraceId`（启用 OpenTelemetry 后是 32-hex），否则回退入站 `X-Trace-Id` 头，再回退 Kestrel 的 `TraceIdentifier`。结果同时写进 `HttpContext.Items` 和**响应头 `X-Trace-Id`**，并进入 `ApiResponse.traceId`。

日志、审计、事件总线共用这一个值——排查线上问题时拿它去检索即可串起全链路。

### ③ 请求文化

紧跟 TraceId、**先于路由与 MVC**，这样后续管线（请求上下文、控制器、响应过滤器）全都在正确的文化下执行。文化来源是请求头 `X-Language`。

### ⑧⑨ 限流与熔断

两者都在**路由之后、鉴权之前**——尽早拒绝超额请求、过载时快速失败，不浪费鉴权与数据库资源。默认都关闭，由 `XiHan:Web:RateLimiting:IsEnabled` / `XiHan:Web:CircuitBreaking:IsEnabled` 打开。

### ⑪ 本地存储静态文件在鉴权之前

本地对象存储的公开资源（头像等）要能匿名直链访问，所以静态文件中间件挂在 `UseAuthentication` 之前。路径与 `XiHan:ObjectStorage:Local` 约定一致（默认 `wwwroot/uploads` ↔ `/uploads`）。

::: warning 线上跨源要拼 origin
本地存储返回的是**根相对路径** `/uploads/...`。前后端不同源时前端必须拼上 `VITE_API_BASE_URL` 的 origin，仓库里的 `toAbsoluteFileUrl` / `useAvatarUrl` 已处理。
:::

### ⑬ 开放接口签名

只对配置里 `ProtectedPathPrefixes` 命中的路径生效（BasicApp 配的是 `/api/openapi`）。它在**认证之前**——开放接口的身份由 HMAC 签名确立，没有 JWT。详见 [接口对接指南](../api-guide#开放接口-签名调用-无-jwt)。

### ⑮ 租户解析

在认证之后（要读令牌里的 `TenantId` claim），建立 `ICurrentTenant`。**这一步之后**，仓储上的全局查询过滤器才知道当前租户是谁。

租户上下文决定了两件事：读什么（过滤器放行 `TenantId IN (0, 当前租户)`）、写哪里（写路径禁止改非本租户行）。见 [多租户与版本](./multi-tenancy)。

### ⑯ 会话闸门（401 与 423 的分水岭）

`XiHanSessionStateMiddleware` 的位置是精心安排的：

- **在认证之后**——要读 `session_id` claim；
- **在租户解析之后**——会话表是多租户实体，租户上下文没解析会被全局过滤器挡掉；
- **在授权之前**——`423` / `401` 要先于权限评估短路，不能和 `403` 混淆。

判定逻辑委托给应用侧的 `ISessionStateGate`（框架默认实现一律放行）。BasicApp 用 `services.Replace(...)` 换成 `SaasSessionStateGate`，据此产出两种结果：

| 结果 | 语义 | 客户端该怎么做 |
| --- | --- | --- |
| `401` | 会话已失效（登出 / 被踢 / 撤销 / 过期） | 尝试刷新令牌，失败则跳登录页 |
| **`423`** | **会话被锁屏，身份仍然有效** | **弹解锁遮罩，绝不能跳登录页** |

### ⑰ 授权：读授权快照，不读令牌

框架的 `UseAuthorization` 会调 `IPermissionChecker`。BasicApp 用 `services.Replace(IPermissionChecker → SaasPermissionChecker)` 把判定改为读 **Redis 里的授权快照**（`SaasAuthorizationSnapshotCacheItem`）。

由此得到一个重要性质：**授权变更即时生效，无需重新登录**。权限码不冻结进 JWT（超管例外，只放一个字面通配 `*` 作快路径）。

判定链依次是：权限码 → 租户版本（Edition）白名单 → 数据范围 → ABAC 属性条件 → 约束规则。任何一环不过都会被拒。见 [权限模型](./permission)。

### ⑱ 端点：动态 API

没有手写 Controller——应用服务打 `[DynamicApi]` 由框架在启动时生成 Controller 并注册路由。路由推导规则（动词前缀剥离、默认 POST、路由段只由 `[FromRoute]` 产生）见 [接口对接指南](../api-guide#动态-api-路由推导规则)。

## 应用层内部

### 写侧

```csharp
[UnitOfWork(true)]                                          // ← 开事务
[PermissionAuthorize(SaasPermissionCodes.Position.Create)]  // ← 细粒度权限
public async Task<PositionDetailDto> CreatePositionAsync(PositionCreateDto input, CancellationToken cancellationToken = default)
{
    var command = PositionApplicationMapper.ToCreateCommand(input);
    var result = await _positionDomainService.CreatePositionAsync(command, cancellationToken);  // 业务规则在领域服务
    return PositionApplicationMapper.ToDetailDto(result.Position);
}
```

影响缓存的写方法在领域服务返回后调 `ISaasCacheInvalidator` 的对应方法（如 `OperationAppService` 调 `InvalidateOperationDefinitionAsync`），失效以 `considerUow: true` 排队，UoW 提交后才真正执行。

::: danger 没标 `[UnitOfWork]` 就没有事务
框架判定「要不要被工作单元拦截」看的是类/方法上有没有 `[UnitOfWork]`（或实现 `IUnitOfWorkEnabled`，而它在框架里无人实现）。**没有中间件会替你开环境工作单元。** 多步写操作漏标注 = 没有原子性。
:::

### 读侧

`*QueryService` 直接投影，多数带分布式缓存。读路径还会经 `IFieldSecurityService.GuardFiltersAsync` / `GuardSortsAsync` 做字段级门控——只有「可读且未脱敏」的字段允许参与过滤与排序。

## 收尾顺序（最容易踩的时序）

工作单元完成时的动作是**有先后的**：

```text
1. 本地事件发布        ← 提交之前（处理器可能继续写库，必须落在同一事务里）
2. 提交事务
3. 分布式事件发布      ← 提交成功之后（避免「事务回滚了事件照发」的幽灵事件）
4. 缓存失效执行        ← RemoveByPatternAsync(..., considerUow: true) 排队到提交后
5. 队列入队 / 后台作业
```

几条推论：

- **缓存失效必须 `considerUow: true`**，否则会在事务提交前就清缓存，并发读会把未提交的旧值重新灌回去。
- **分布式事件在提交后才发**，因此提交成功但投递失败会丢事件。要强投递保证得自己接持久化发件箱。
- **内层 `RollbackAsync` 之后外层再提交会抛 `XiHanException`**——父子共用同一物理事务，内层回滚即整体终止。

## 响应

统一 `ApiResponse` / `ApiResponse<T>` 信封：

- 国际化时响应过滤器会**覆盖 `Data`**，所以前端优先读 `data`。
- 时间列按 `X-Timezone` 换算输出（存储恒 UTC）。
- `long` 序列化为字符串；枚举序列化为成员名；`null` 字段整个省略。

字段表、业务码全表与客户端解包顺序见 [接口对接指南](../api-guide#统一响应信封)。

## 按现象定位

| 现象 | 先看哪一段 |
| --- | --- |
| 401 且刷新也救不回来 | ⑯ 会话闸门：会话是否被撤销/过期 |
| 423 | ⑯ 锁屏，不是认证问题 |
| 403 但权限码明明有 | ⑰ 版本门控 / 数据范围 / ABAC / 约束规则 |
| 查得到别的租户数据 | ⑮ 租户解析是否成功；实体是否漏了多租户基类 |
| 时间差几小时 | 请求有没有带 `X-Timezone`；存储侧是否为 UTC |
| 客户端 IP 全是网关 IP | ① 转发头配置 |
| 接口返回 200 但库里没数据 | 写方法漏了 `[UnitOfWork]`，或内层回滚过 |
| 改了数据但页面还是旧值 | 收尾第 4 步：失效方法没调，或没走 `considerUow` |

## 相关页面

- [后端架构](./introduction)：模块装配与分层
- [缓存与异步](./caching)：缓存条目、失效器、队列
- [接口对接指南](../api-guide)：信封、业务码、请求头
- [权限模型](./permission)：授权判定链
- [常见问题](../faq)：具体故障的处置步骤
