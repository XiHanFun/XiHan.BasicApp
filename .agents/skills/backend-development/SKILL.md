---
name: xihan-basicapp-backend-development
description: 实现、重构、审查或测试 XiHan.BasicApp 后端模块时使用。覆盖 Dynamic API、DDD/CQRS、SqlSugar、实体、仓储、权限、租户、事务、种子和后台资源；跨前后端协议变化使用全栈契约技能。
---

# XiHan.BasicApp 后端开发

开始前读 `references/backend.md`，并检查所属模块 README、相邻实体、服务、仓储、权限、种子和测试。

## 架构边界

- `Saas` 承载租户、身份、RBAC/ABAC 与平台基础能力；AI、Chat、CodeGeneration、Printing、Workflow 是显式依赖的可选模块。
- 普通业务 API 使用 `[DynamicApi]` Application Service/Query Service，不建立 Controller 或 Minimal API 平行入口。
- 业务规则在 Domain，命令与查询编排在 Application，SqlSugar 实现在 Infrastructure。
- 写操作沿用 `[UnitOfWork(true)]`；受保护操作使用模块权限常量和 `[PermissionAuthorize(...)]`。
- 全局记录使用 `TenantId = 0`；软删除实体的唯一索引包含 `IsDeleted` 和隔离所需 TenantId。
- 异步方法接受并传递 `CancellationToken`，不吞异常、不返回伪成功。
- Framework 默认内存实现保持零外部依赖；BasicApp 在应用层注册分布式 Provider。

## 契约与验证

改变 DTO、服务名、方法、权限或菜单路径时，同时加载 `../fullstack-contracts/SKILL.md`。先跑所属模块测试，再运行：

```bash
dotnet build backend/XiHan.BasicApp.slnx -c Release --no-restore
dotnet test backend/XiHan.BasicApp.slnx -c Release --no-build
```

实体、应用服务、仓储、种子、测试和必要升级脚本应形成同一功能闭环。
