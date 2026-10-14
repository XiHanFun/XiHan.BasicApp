# XiHan.BasicApp.Workflow

曦寒基础应用工作流模块。

## 概述

在框架工作流引擎（`XiHan.Framework.Workflow`）之上提供业务落地：SqlSugar 持久化存储（替换内存默认，获得崩溃恢复）、
基于 Saas 组织数据的受理人规则、审批业务接入（业务单据挂流程、结论回写）、审批中心与流程定义/实例的动态 API、
权限与菜单种子、待办站内通知（含实时推送）。

## 核心能力

| 分层 | 内容 |
| --- | --- |
| Domain | 五张表：`Sys_Workflow_Definition`（软删+审计）、`Sys_Workflow_Instance` / `Sys_Workflow_Node_Instance` / `Sys_Workflow_Bookmark`（引擎运行时行，硬删）、`Sys_Workflow_Task_Record`（审批记录：办理/转办/加签/抄送）；JSON 列为真源、投影列供检索；权限码 `workflow:*` |
| Infrastructure | 三个框架存储契约的 SqlSugar 实现（单例 + 每操作独立作用域，Replace 注册，加入环境事务）；组织架构受理人规则（`Role` / `Department` / `DepartmentLeader` / `Position` / `StarterLeader` / `StarterLeaderChain`）；五个种子（Order 300-304：操作 → 资源 → 权限 → 菜单 → 角色授权） |
| Application | `WorkflowDefinition`（创建/草稿更新/发布/新版本/停用/归档/删除 + 分页详情 + 审批业务目录）、`WorkflowInstance`（发起/取消/终止/重试/挂起/恢复/信号 + 分页详情含执行历史）、`WorkflowTodo`（待我审批/办理/转办/加签）、`WorkflowApproval`（我已审批/我发起的/抄送我的/审批详情/撤回）、`WorkflowDirectory`（设计器与选人用的组织目录）；`Business/` 下是审批业务接入（登记表、发起/撤回/进度服务、结果回写、中止通知、发布校验、审批记录）；事件处理器把待办创建/转办/实例故障投递为站内通知，并写抄送记录 |

## 依赖关系

`XiHan.BasicApp.Saas`（仓储基类/权限/通知/组织数据，以及审批业务契约 `IWorkflowBusinessHandler` / `IWorkflowBusinessService`）、
`XiHan.Framework.Workflow` + `Workflow.Abstractions`（模块级直接引用，Local 变体对应框架源码工程）。

审批业务契约放在 Saas，业务模块不必引用本模块；Saas 先以「工作流未启用」实现占位 `IWorkflowBusinessService`（每个调用都抛业务异常），本模块 Replace 为真实实现。

## 配置与约定

- 本模块无独立配置节；引擎调优走框架 `XiHan:Workflow`（步数上限/锁参数/定时器 Worker）；
- 审批中心与待办接口不设权限码（登录即可），主体服务端锁定为当前用户；办理人归属由框架任务服务在实例锁内校验；审批详情按参与关系鉴权（发起人、受理人、办理或抄送过的人，或持有 `workflow:read`）；
- 受理人标识 = 用户主键（长整型字符串）；角色、部门、岗位规则按编码引用，定义可跨租户复用；
- 审批业务：业务编码 = 流程定义编码，单据主键 = 实例 `CorrelationId`；登记为审批业务的流程发布时，结束节点必须声明业务允许的结果、不能用终止节点；
- 表结构变更随 WebHost `UpdateScripts/<版本>/` 升级脚本落地（5.8.0：实例结果列、发起人索引、撤掉系统审查）。

## 使用方式

WebHost 已挂载本模块。业务单据接审批流：

```csharp
// 1. 实现 IWorkflowBusinessHandler（描述：编码/名称/单据视图组件/变量/默认流程模板；回调：办理前校验/结论回写/中止/读单据）
// 2. services.TryAddEnumerable(ServiceDescriptor.Transient<IWorkflowBusinessHandler, XxxWorkflowHandler>());
// 3. 业务提交的 [UnitOfWork(true)] 里调 IWorkflowBusinessService.StartAsync(编码, 单据主键, 标题, 变量)
// 4. 审批人在「审批 → 审批中心」办理；流程走到结果结束节点时回调 OnOutcomeAsync 回写单据
```

参照实现：Saas 的 `PermissionRequestWorkflowHandler`（权限申请）。完整说明见 `docs/backend/workflow.md`。

## 扩展点

- 审批业务：实现 `IWorkflowBusinessHandler`（见上）；
- 受理人规则：实现框架 `IWorkflowAssigneeProvider` 并 `services.AddXiHanWorkflowAssigneeProvider<T>()`，前端设计器的规则目录同步补一类；
- 自定义活动：任意模块实现 `IWorkflowActivity` 并 `services.AddXiHanWorkflowActivity<T>()`；
- 通知通道：替换/追加本模块事件处理器（邮件/短信走 `IMessageDeliveryService`）。

## 目录结构

- `Domain/`：实体、仓储接口、权限码
- `Application/`：应用服务（AppServices/QueryServices）、审批业务接入（Business）、契约、DTO、映射器、事件处理器
- `Infrastructure/`：SqlSugar 仓储与存储实现、组织架构受理人规则、系统种子
- `Extensions/`：服务注册扩展
