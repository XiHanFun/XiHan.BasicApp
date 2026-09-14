# 后端开发规范

## Dynamic API

- 普通业务能力通过实现 `IApplicationService` 的 `[DynamicApi]` AppService/QueryService 暴露。
- 不创建等价 Controller、Minimal API 或第二套路由。
- 服务名、方法名、参数顺序、route value 和 DTO 会影响前端客户端；改变前先全仓搜索调用点。
- 命令与查询分开，复杂业务规则委托给 Domain Service。

## 权限与事务

- 受保护操作使用模块权限常量和 `[PermissionAuthorize(...)]`。
- 新权限同步更新权限定义、种子、角色授权、菜单/操作资源和前端判断。
- 所有动态 API 方法都必须显式授权、匿名或进入仓库认可的例外清单。
- 写操作沿用相邻服务的 `[UnitOfWork(true)]` 和事务边界，不在多个层重复开启事务。
- 前端隐藏操作从不替代后端权限验证。

## 实体、租户与仓储

- 使用 BasicApp 实体基类、审计字段和软删除语义。
- SqlSugar 映射使用 `[SugarTable]`、`[SugarColumn]`、`[SugarIndex]` 等当前特性。
- 全局记录使用 `TenantId = 0`；租户记录使用当前租户上下文。
- 唯一索引对软删除实体包含 `IsDeleted`，并包含实现隔离所需的 TenantId。
- 仓储契约位于 Domain，实现位于 Infrastructure；复用已有 `SaasRepository<T>` 和 `ISqlSugarClientResolver` 模式。
- 不让 Application Service 直接拼接复杂 SqlSugar 查询替代仓储/QueryService 边界。

## DTO 与映射

- 请求、响应、分页、日期和 nullable 字段必须与前端契约一致。
- 使用显式 Mapper，避免反射映射悄然改变公开字段。
- DTO 不暴露实体导航、密码、密钥、内部状态或跨租户标识。
- 校验错误、业务冲突和未授权使用明确异常语义，不返回 `null` 或伪成功。

## 异步与资源

- 异步方法接受 `CancellationToken cancellationToken = default` 并传递到所有异步调用。
- 不使用 `.Result`、`.Wait()` 或无界并发。
- 缓存、会话、在线用户和后台任务必须有容量、超时、清理和释放语义。
- Framework 默认存储保持内存且零外部依赖；BasicApp 在应用层注册分布式 Provider。

## 测试

至少覆盖：

- 权限存在、拒绝和动态 API 权限元数据。
- TenantId=0 与普通租户的隔离。
- 软删除与唯一索引相关行为。
- 事务提交/回滚、取消和业务异常。
- DTO 映射、分页和空结果。
- 种子重复执行的幂等性。

标准命令在仓库根执行：

```bash
dotnet restore backend/XiHan.BasicApp.slnx
dotnet build backend/XiHan.BasicApp.slnx -c Release --no-restore
dotnet test backend/XiHan.BasicApp.slnx -c Release --no-build
```

启动必须指向具体 WebHost 项目文件，不能只传包含多个项目文件的目录。
