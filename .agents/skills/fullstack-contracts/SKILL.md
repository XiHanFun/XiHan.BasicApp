---
name: xihan-basicapp-fullstack-contracts
description: 设计、变更、审查或诊断 XiHan.BasicApp 前后端契约时使用。覆盖 Dynamic API 服务与 DTO、权限码、租户隔离、菜单路由、可选模块、Framework 引用模式、配置、健康检查、数据库升级和部署边界。
---

# XiHan.BasicApp 全栈契约

功能归属与跨端实现先读 `references/architecture.md`；涉及安全、配置、健康检查、SQL、运行或发布时再读 `references/contracts-security-and-operations.md`。

## 契约清单

改变 Dynamic API 前确认并同步：

1. 服务名、方法名、HTTP 语义和 route value。
2. 请求/响应 DTO 字段、nullable、枚举、ID、日期和分页。
3. 后端 Mapper 与前端 `*.types.ts`、API facade 和调用点。
4. 权限码、权限种子、角色授权和 UI 可见性。
5. 后端菜单 Component 路径、动态路由映射和可选模块注册。
6. 后端与前端测试、升级脚本和文档。

## 不可变边界

- 认证、授权、租户和数据范围在后端执行；客户端标识不是可信事实。
- 不保留双字段、双 API、伪空结果或静默 fallback；破坏性变化明确迁移并一次收口。
- 单独构建 `backend/XiHan.BasicApp.slnx` 默认使用 Framework NuGet 包；源码联调显式选择工作区或 `UseXiHanFrameworkSource`。
- 数据库是启动硬依赖；Redis、Qdrant 与健康检查语义以当前代码和配置为准，不篡改状态掩盖不可用。
- SQL 升级、生产配置、部署和外部发布只有用户明确要求时执行。

跨端功能验证实际 API、权限、租户、菜单、异常和浏览器行为，不以两端分别编译成功代替契约验证。
