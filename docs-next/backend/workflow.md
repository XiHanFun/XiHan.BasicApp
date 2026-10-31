# 工作流（规划中 · N3）

本页记录工作流模块在总体方案中的设计，供规划与评估参考。

::: warning 尚未实现
6.0 还没有 Workflow 包，菜单里没有流程定义、流程实例与审批中心，也没有相关操作与权限码。本页记录总体方案里的设计，落地时以代码为准。5.x 的工作流文档见 [5.x 文档](https://basicapp.docs.xihanfun.com/backend/workflow)。
:::

## 定位

Workflow 是 Tier 3 可选模块，计划在 N3 与 Printing、CodeGeneration、Chat 一起落地。N3 的退出条件是：四个演示流程跑通，审批中心可以办理。

## 设计要点

### 引擎：框架的显式 Provider

工作流执行能力由框架 5 的 `Workflow.Abstractions` 定义（能力 `workflow.execution@1`），BasicApp 以显式 Provider 接入，而不是自建引擎：

| 部分 | 方案 |
| --- | --- |
| 存储 | SqlSugar 实现的三个存储（定义、实例、书签），或框架 `Workflow.Database`（四张表，基线已规划） |
| 并发 | `ICacheStore` 租约锁 |
| 多租户 | 逐库轮询书签：平台扫租户库按租户绑定分片，不在一个上下文里切换租户 |
| 定义格式 | 直接是 5.0 的 `WorkflowJson`（节点 `properties`、`transitions`、`variables`、`outputMappings`、超时与补偿） |

### 业务接入

- 业务契约 `IWorkflowBusinessHandler` 放在 `Contracts`，业务包实现它即可让自己的单据走审批；Workflow 不引用业务包。
- Access 的授权申请是参照实现：提交即发起审批流，通过后执行授予（见[约束规则与审批](./approval)）。
- 受理人规则沿用 5.x 的 6 种。

### 计划的资源与权限

| 资源 | 用途 |
| --- | --- |
| `workflow-definitions` | 流程定义：设计、发布、版本 |
| `workflow-instances` | 流程实例：查看、挂起、恢复、终止 |
| `tasks` | 我的待办 |
| `approvals` | 审批中心：同意、驳回、转办、加签、撤回 |

权限码按 `workflow.<资源>.<动作>` 文法登记；「我的待办」这类只读目录操作计划同时暴露为 MCP / AI 工具。

### 前端

- 流程设计器用 `@xihan-flow/designer`，它产出的 JSON 需要映射到 5.0 `WorkflowJson`，映射层计划放在 `packages/designers/flow`；缺省项必须写全，不依赖框架对 null 的兜底。
- 页面进 `src/modules/workflow`。

## 当前替代方案

- 需要「职责分离」类的控制：用已落地的[约束规则](./approval)。
- 需要审批：在 6.0 预览期没有内置方案；约束规则的 `RequireApproval` 目前按拒绝处理。

## 下一步

- [约束规则与审批](./approval) · [架构总览](./introduction)
