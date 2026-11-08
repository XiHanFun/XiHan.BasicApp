# 常见问题

本页说明运行、认证、权限与接口联调中的常见问题，适用于开发与运维排查。

## 启动与环境

### 后端与前端使用哪些端口？

后端 HTTP `9710`、gRPC `9711`（只监听本机），前端开发服务器 `9100`。以 `appsettings.json` 的 `Kestrel:Endpoints` 与 `frontend/.env` 的 `VITE_PORT` 为准。

### 构建或测试报框架相关的错

后端以源码引用同级检出的 XiHan.Framework（`../../XiHan.Framework/framework/src`，相对 `backend/`），并锁定在 `backend/props/framework.props` 的 `XiHanFrameworkCommit`。同级找不到 `framework/src` 时构建会自动改用 NuGet 包，公开面可能与锁定提交不一致。把框架检出到锁定提交；框架在别处时加 `-p:XiHanFrameworkSrc=<检出>/framework/src/`。见[开发环境](./dev-environment#框架检出)。

### `FrameworkPinTests` 失败

同级框架检出没有 `framework/src`，或 HEAD 不在锁定提交上。把框架检出到 `XiHanFrameworkCommit` 指定的提交；其他测试不受影响。

### 宿主启动后立即退出

- 日志有 `RUNTIME_VALIDATION_001`：没有配置 `XiHan:Bindings:db`。
- 日志有 `Startup aborted: schema upgrade failed`：表结构升级脚本执行失败，看后面的错误码与信息；修好后重启即可，台账里记着执行到哪。
- JWT 密钥或签名地址密钥缺失、不足 32 字节：非 Development 环境必须配 `XiHan:Security:Jwt:Secret` 与 `XiHan:ObjectStorage:SignedUrls:Secret`。
- 框架节写了空的 `[]` / `{}`：整项删掉，见[配置参考](./configuration#文件与优先级)。

### 表已创建，但无法以 `superadmin` 登录

种子不随启动执行，要单独运行一次：

```bash
dotnet run --project backend/src/XiHan.BasicApp.Host --launch-profile Development -- seed
```

### 前端提示跨域错误

开发时前端经 Vite 代理访问后端，与后端同源，不应出现跨域。出现了说明 `VITE_API_BASE_URL` 被设成了别的源，留空即可。部署推荐同源；前端放在别的源时，在 `BasicApp:Cors:AllowedOrigins` 列出它，见[部署](./deployment#前端-构建与部署)。

### 是否需要 Redis？

不需要。不配连接时缓存与消息走框架的进程内实现，开发与单实例部署零外部依赖。多实例部署时配上 Redis（与 RabbitMQ），宿主自动装上对应 Provider，缓存与消息跨实例共享，实时推送也跨实例，见[部署](./deployment#多实例部署)。

## 认证与令牌

### 初始超级管理员账号是什么？

开发环境是 `superadmin` / `SuperAdmin@123`，由 `seed` 写入，密码写在 `appsettings.Development.json` 的 `BasicApp:Seed:SuperAdminPassword`。别的环境必须配这一项，否则 `seed` 失败（`IDENTITY_VALIDATION_026`）。首次登录后请修改密码。

### 命令行调用登录接口时如何处理验证码？

图形验证码缺省开启（`BasicApp:Auth:CaptchaEnabled`）。先调 `POST /api/auth:captcha` 取 `captchaId` 与图片，或在本机配置里临时关掉。

### 登录提示尝试过于频繁

登录节流：同一账号同一地址每分钟 5 次失败、同一地址跨账号 30 次（`BasicApp:Auth:LoginThrottle*`），超限返回 `IDENTITY_UNAVAILABLE_001` 并带 `retryAfter`。连续失败达到 `MaxFailedAccessAttempts` 还会锁定账号。

### 接口返回 403，`code` 是 `IDENTITY_DENIED_002`

会话被锁定，`details.reason` 说明原因：`ScreenLock` 是锁屏，前端进锁屏界面，调 `auth.unlock` 解锁；`PasswordChangeRequired` 是强制改密（系统参数 `identity.password`），调 `profile.change-password` 改完即解开。

### 授予权限后是否需要重新登录？

- 授予、Deny、委托：判定时实时查（规则缓存 60 秒、写入方即时失效），不用重新登录。
- 角色指派：判定用的角色取「令牌里的角色」与「有效指派」的交集，解除立即生效；新增的角色要等下一次刷新令牌才进令牌。
- 前端菜单与按钮：刷新令牌后会重新取 `auth.permissions`。

### 切换租户是否会结束原来的会话？

不会。`auth.switch-tenant` 续用同一个会话，只换令牌；前端换完令牌整页重建。

## 权限与菜单

### 新增页面未出现在菜单中

1. 页面要登记在包的 `<包>Pages.cs` 里，并重新执行 `seed`（`access.menus` 种子从页面登记对齐菜单）。
2. 当前主体要有页面登记里的权限码；租户主体还要被套餐白名单放行。
3. 页面的作用侧要和当前主体一致（平台 / 租户）。
4. 页面属于可选模块时，模块要开着（`BasicApp:Modules:<名>:Enabled`）。

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

路由完全由操作 ID 与请求形状推导：请求带 `id` 即实例级（`/{id}`），带分页对象这类嵌套对象的 `list` 走 POST，`get` 与声明了 `ReadOnly` 且没有嵌套对象的查询动词走 GET。以 `/openapi.json` 为准，见[接口对接指南](./api-guide#路由推导)。

### 批量删除如何传递 ID

批量删除是 `DELETE /api/<资源>?ids=<id1>&ids=<id2>`，响应是逐项结果 `{ succeeded, failed }`。

### 时间显示相差若干小时

接口一律 UTC（带 `Z`），服务端不做时区换算，换算是客户端的事。前端按偏好设置里的应用时区显示（缺省 `Asia/Shanghai`，顶栏的时区切换器可改），不看浏览器所在的时区，见[国际化与时区](./frontend/i18n#时区)。自己对接时不要把带 `Z` 的时间当本地时间解析。

### 修改后端 DTO 后前端类型未更新

前端契约是生成物：后端导出 `openapi.json`，前端 `pnpm gen:contracts` 重新生成，两者同一提交。CI 会重新生成并比对，不一致即失败。见[契约与请求](./frontend/request)。

## 下一步

- [快速开始](./getting-started) · [接口对接指南](./api-guide) · [配置参考](./configuration)
