# 常见问题

本页汇总 6.0 在运行、认证、权限与接口联调中的常见问题，适用于开发与运维排查。

## 版本与范围

### 6.0 能否用于生产环境？

不能。6.0 是预览版（`6.0.0-preview.1`），在 `next` 分支开发、尚未发布；N2 平台能力正在补收尾差距，N3（工作流、打印、代码生成、聊天）与 N4（AI）还没开始。生产请继续用 5.x。

### 5.x 的数据库能否升级到 6.0？

不能，这是有意的决定（方案 D12）。6.0 的主键（UUID v7）、租户 ID（字符串）、并发戳、权限码与表名全部变了，不提供原地升级，也不提供 5.x → 6.0 的数据迁移工具。要换到 6.0 的部署从空库初始化，在 6.0 上重录租户与账号。见[表结构升级](./backend/upgrade)。

### 5.x 的接口调用代码能否直接用于 6.0？

不能。6.0 没有统一响应信封，路由由操作 ID 推导，错误是 Problem Details。见[接口对接指南](./api-guide)。

## 启动与环境

### 后端与前端使用哪些端口？

后端 HTTP `9710`、gRPC `9711`（只监听本机），前端开发服务器 `9100`。以 `appsettings.json` 的 `Kestrel:Endpoints` 与 `frontend-next/.env` 的 `VITE_PORT` 为准。

### 构建时找不到 `XiHan.Framework.*` 工程

`backend-next` 以源码引用框架，要求 XiHan.Framework 与 BasicApp 并排检出（`../../XiHan.Framework/next/src`）。框架在别处时加 `-p:XiHanFrameworkSrc=<检出>/next/src/`。

### `FrameworkPinTests` 失败

框架检出不在 `backend-next/props/framework.props` 锁定的提交上。把框架检出到 `XiHanFrameworkCommit` 指定的提交；其他测试不受影响。

### 宿主启动后立即退出

- 日志有 `RUNTIME_VALIDATION_001`：没有配置 `XiHan:Bindings:db`。
- 日志有 `Startup aborted: schema upgrade failed`：表结构升级脚本执行失败，看后面的错误码与信息；修好后重启即可，台账里记着执行到哪。

### 表已创建，但无法以 `superadmin` 登录

种子不随启动执行，要单独运行一次：

```bash
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development -- seed
```

### 前端提示跨域错误

开发态前端经 Vite 代理访问后端，与后端同源，不应出现跨域。出现了说明 `VITE_API_BASE_URL` 被设成了别的源；留空即可。部署时也要同源（后端没有配置 CORS），见[部署](./deployment)。

### 是否需要 Redis？

不需要。宿主装的是框架 Full Profile，缓存与消息走进程内实现，开发与单实例部署零外部依赖。代价是多实例时缓存与实时推送不共享，见[部署](./deployment#多实例的限制)。

## 认证与令牌

### 初始超级管理员账号是什么？

`superadmin` / `SuperAdmin@123`，由 `seed` 写入；密码可用 `BasicApp:Seed:SuperAdminPassword` 改掉。首次登录后请修改密码。

### 命令行调用登录接口时如何处理验证码？

图形验证码缺省开启（`BasicApp:Auth:CaptchaEnabled`）。先调 `POST /api/auth:captcha` 取 `captchaId` 与图片，或在本机配置里临时关掉。

### 登录提示尝试过于频繁

登录节流：同一账号同一地址每分钟 5 次失败、同一地址跨账号 30 次（`LoginThrottle*`），超限返回 `IDENTITY_UNAVAILABLE_001` 并带 `retryAfter`。连续失败达到 `MaxFailedAccessAttempts` 还会锁定账号。

### 接口返回 403，`code` 是 `IDENTITY_DENIED_002`

会话处于锁屏状态。前端进锁屏界面，调 `auth.unlock` 解锁；5.x 的 423 在 6.0 统一成这个错误码。

### 授予权限后是否需要重新登录？

- 授予、Deny、委托：判定时实时查（规则缓存 60 秒、写入方即时失效），不用重新登录。
- 角色指派：判定用的角色取「令牌里的角色」与「有效指派」的交集，解除立即生效；新增的角色要等下一次刷新令牌才进令牌。
- 前端菜单与按钮：刷新令牌后会重新取 `auth.permissions`。

### 切换租户是否会结束原来的会话？

不会。`auth.switch-tenant` 续用同一个会话，只换令牌；前端换完令牌整页重载。

## 权限与菜单

### 新增页面未出现在菜单中

1. 页面要登记在包的 `<包>Pages.cs` 里，并重新执行 `seed`（`access.menus` 种子从页面登记对齐菜单）。
2. 当前主体要有页面登记里的权限码；租户主体还要被套餐白名单放行。
3. 页面的作用侧要和当前主体一致（平台 / 租户）。

### 菜单可见但页面显示 404

前端按组件路径精确解析：页面路由 `/identity/users` 的组件是 `identity/users/index`，对应 `src/modules/identity/views/users/index.vue`。文件不在这个位置就落 404 兜底，控制台报错。`pnpm modules:check` 会检查撞键。

### 已授予权限码但接口仍返回 403

按顺序查：

1. 有没有 Deny（角色或用户直授上的拒绝压过授予）；
2. 授予或指派是不是过期了、还没生效；
3. 租户套餐白名单有没有这个码（套餐门控）；
4. 权限码的作用侧与当前主体是否一致；
5. 有没有挂权限条件（ABAC）且这次请求不满足：查「条件命中」日志；
6. 访问策略是否拦截：查「条件命中」与登录日志。

### 列表字段为空或显示为星号

字段安全规则命中了：读取方式为隐藏的字段不返回值，部分脱敏的字段打码；受保护的字段也不能筛选和排序。见[数据权限与字段安全](./backend/data-permission)。

## 接口与联调

### 如何确定操作的 HTTP 路由

路由完全由操作 ID 与请求形状推导：请求带 `id` 即实例级（`/{id}`），带分页对象这类嵌套对象的 `list` / `get` 走 POST，声明了 `ReadOnly` 且没有嵌套对象的查询动词走 GET。以 `/openapi.json` 为准。

### 批量删除如何传递 ID

批量删除是 `DELETE /api/<资源>?ids=<id1>&ids=<id2>`，响应是逐项结果 `{ succeeded, failed }`。

### 时间显示相差若干小时

6.0 的接口一律 UTC（带 `Z`），没有服务端时区换算，换算是客户端的事。管理端目前用 `~/utils` 的 `formatDate`（dayjs）按浏览器时区显示；方案里「按用户偏好时区显示」还没有实现。自己对接时不要把带 `Z` 的时间当本地时间解析。

### 修改后端 DTO 后前端类型未更新

前端契约是生成物：后端导出 `openapi.json`，前端 `pnpm gen:contracts` 重新生成，两者同一提交。CI 会重新生成并比对，不一致即失败。见[契约与请求](./frontend/request)。

## 下一步

- [快速开始](./getting-started) · [接口对接指南](./api-guide) · [配置参考](./configuration)
