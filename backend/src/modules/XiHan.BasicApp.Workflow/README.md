# XiHan.BasicApp.Workflow

曦寒基础应用工作流模块。

## 概述

在框架工作流引擎（`XiHan.Framework.Workflow`）之上提供业务落地：SqlSugar 持久化存储（替换内存默认，获得崩溃恢复）、
流程定义/实例/待办的动态 API、权限与菜单种子、待办站内通知（含实时推送）。

## 核心能力

| 分层 | 内容 |
| --- | --- |
| Domain | 四张表：`Sys_Workflow_Definition`（软删+审计）、`Sys_Workflow_Instance` / `Sys_Workflow_Node_Instance` / `Sys_Workflow_Bookmark`（引擎运行时行，硬删）；JSON 列为真源、投影列供检索；权限码 `workflow:*` |
| Infrastructure | 三个框架存储契约的 SqlSugar 实现（单例 + 每操作独立作用域，Replace 注册）；五个种子（Order 300-304：操作 → 资源 → 权限 → 菜单 → 角色授权） |
| Application | `WorkflowDefinition`（创建/草稿更新/发布/新版本/停用/归档/删除 + 分页详情）、`WorkflowInstance`（发起/取消/终止/重试/挂起/恢复/信号 + 分页详情含执行历史）、`WorkflowTodo`（我的待办/办理/转办/加签，办理人服务端锁定为当前用户）；事件处理器把待办创建/转办/实例故障投递为站内通知 |

## 依赖关系

`XiHan.BasicApp.Saas`（仓储基类/权限/通知）、`XiHan.Framework.Workflow` + `Workflow.Abstractions`（模块级直接引用，Local 变体对应框架源码工程）。

## 配置与约定

- 本模块无独立配置节；引擎调优走框架 `XiHan:Workflow`（步数上限/锁参数/定时器 Worker）；
- 待办办理接口不设权限码（登录即可），受理人归属由框架任务服务在实例锁内校验；
- 受理人标识 = 用户主键（长整型字符串），非数值受理人跳过站内通知投递；
- 新表与种子在干净库启动时自动落地（CodeFirst + IDataSeeder），线上升级须重建数据库（项目无迁移策略）。

## 使用方式

WebHost 已挂载本模块。发起一个流程：

```csharp
// 1. 创建并发布定义（POST /api/WorkflowDefinition/Create → Publish）
// 2. 发起实例（POST /api/WorkflowInstance/StartInstance，DefinitionCode + VariablesJson）
// 3. 受理人在「工作流 → 我的待办」办理（同意/拒绝/转办/加签）
```

## 扩展点

- 自定义活动：任意模块实现 `IWorkflowActivity` 并 `services.AddXiHanWorkflowActivity<T>()`；
- 通知通道：替换/追加本模块事件处理器（邮件/短信走 `IMessageDeliveryService`）；
- 前端设计器：定义以 JSON 编辑（`WorkflowDefinitionJsonSerializer` 稳定格式），后续可接 LogicFlow/VueFlow 画布。

## 目录结构

- `Domain/`：实体、仓储接口、权限码
- `Application/`：应用服务（AppServices/QueryServices）、契约、DTO、映射器、事件处理器
- `Infrastructure/`：SqlSugar 仓储与存储实现、系统种子
- `Extensions/`：服务注册扩展
