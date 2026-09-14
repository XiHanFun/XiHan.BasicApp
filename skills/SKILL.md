---
name: xihan-basicapp
description: 开发、重构、审查、测试或记录 XiHan.BasicApp 时使用。覆盖 .NET 10 + XiHan.Framework 后端、Dynamic API、DDD/CQRS、SqlSugar、多租户与权限，以及 Vue 3 + XiHan.UI 前端、类型化 API、Pinia、动态路由、统一交互样式和前后端契约；普通 XiHan.Framework 或 XiHan.UI 库开发不要加载。
---

# XiHan.BasicApp

先识别任务类型，只读取对应资料。

## 路由

| 任务 | 必须读取 |
| --- | --- |
| 判断模块、层次、前后端职责或新增完整功能 | `references/architecture.md` |
| 后端实体、服务、仓储、Dynamic API、权限或测试 | `references/backend.md` |
| Vue 页面、API、状态、路由、XiHan.UI、样式或交互 | `references/frontend.md` |
| DTO、权限、租户、配置、健康检查、SQL 或部署 | `references/contracts-security-and-operations.md` |
| 同时涉及前后端 | 先读架构，再读 backend、frontend 和契约规范 |

不要一次加载与任务无关的 reference。

## 不可变约束

- 普通业务接口由 `[DynamicApi]` Application Service/Query Service 暴露，不建立 Controller 或 Minimal API 平行层。
- 业务规则在 Domain，应用编排在 Application，SqlSugar 实现在 Infrastructure。
- 后端权限是安全边界；受保护操作必须显式标注并与权限码、种子和前端一致。
- 平台/全局记录使用 `TenantId = 0`，不使用 nullable TenantId 表示全局。
- 前端调用通过类型化 API 模块，跨页面状态使用 Pinia，动态路由保持后端菜单兼容。
- 前端组件使用 XiHan.UI，不使用 Naive UI；样式消费语义令牌，不创建第二套组件皮肤。
- 普通 control 4px、surface 8px、overlay 12px；统一 0.97 按压反馈；禁止 glass，只允许 frosted 柔和模糊。
- 不添加静默兜底、伪成功、双轨旧协议或推测性兼容。
- 一项完整功能一个提交，不混入无关清理。

## 先查事实

在仓库根开始：

```bash
git status --short --branch
git log -5 --oneline
dotnet sln backend/XiHan.BasicApp.slnx list
```

涉及前端时，在 `frontend/` 继续检查：

```bash
pnpm run type-check
pnpm run validate
```

实现前至少确认：

1. 能力所属后端模块和前端页面/插件模块。
2. 现有实体、DTO、服务名、API facade、权限码和菜单路径。
3. 租户、软删除、事务、取消和异常语义。
4. 是否已有可复用的 XiHan.UI 组件、共享前端包或后端领域能力。
5. Framework 使用 NuGet 还是工作区源码模式。

## 修改边界

- 用户要求解释、审查或诊断时只做只读检查，不擅自实现。
- 用户要求实现时，完成后端、前端、测试、种子、迁移和文档中实际受影响的同一功能闭环。
- Framework 通用能力应在 Framework 仓库实现；BasicApp 只保留应用与 Provider 接线。
- XiHan.UI 组件缺陷应在 XiHan.UI 修复；BasicApp 主题桥只处理应用级组合和品牌语义。
- 保留工作区中用户已有改动，不使用破坏性 Git 命令。

## 验证

按风险从小到大验证：

1. 受影响的后端测试项目或前端 Vitest 用例。
2. 后端 Release 构建与测试。
3. 前端 `pnpm check` 和 `pnpm test`。
4. 前端 `pnpm build`。
5. 跨端功能进行实际 API、权限、租户、菜单和浏览器验证。

单独通过编译、类型检查或快照都不等于功能完成。
