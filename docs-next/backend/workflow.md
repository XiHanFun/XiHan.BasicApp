# 工作流

本页说明工作流的引擎与存储、流程定义与实例的操作和权限、组织类受理人、审批业务的接入、演示流程与模块开关，适用于设计流程、接入审批的后端与前端开发者。

Workflow 是 Tier 3 的可选模块（`BasicApp:Modules:Workflow:Enabled`，缺省开）。引擎、定义目录与存储都是框架 5 的工作流，本包只做管理操作、组织类受理人、审批业务接入与页面；模型与规则沿用 5.x，写法换成 6.0 的。审批中心与授权申请随 N3 第三批后续几笔进来。

## 引擎与存储

| 部分 | 做法 |
| --- | --- |
| 存储 | 宿主恒装框架的 `Workflow.Database`（模块关掉也装）：定义、实例、节点实例与书签按分区落表（租户四张随租户分库、平台四张在平台库），在调用方的工作单元里随业务提交或回滚；九张表在 6.0.0 基线里，配置见[配置参考](../configuration#xihan-workflow) |
| 并发 | 租约表 `workflow_lock_record` 上的实例锁；到期书签的轮询由持有 `workflow.poller` 领导权的节点做 |
| 分区 | 取自当前调用：平台调用只看平台的定义与实例，租户调用只看本租户的；没有全局定义，平台建的定义租户看不到 |
| 定义格式 | 框架 5 的 WorkflowJson（节点 `properties`、`transitions`、`variables`、超时与补偿）；设计器的布局放在定义的 `properties` 里 |
| 事件 | `workflow.*` 事件写进调用方事务里的发件箱，提交才发出 |

## 流程定义

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `workflow-definitions.list` / `get` | `workflow.workflow-definitions.read` | 列表在库里按关键字（名字、显示名、说明）、名字、分类与状态过滤分页，名字升序、版本降序；详情带整份 WorkflowJson |
| `workflow-definitions.create` / `update` | `.create` / `.update` | 以 WorkflowJson 建草稿（版本按同名最大版本加一）、改草稿（名字与版本沿用已存的） |
| `workflow-definitions.publish` | `.update` | 单条：关联了审批业务的先过业务校验，再由框架做结构校验，问题全部列在 `details.errors`（`WORKFLOW_VALIDATION_001`） |
| `workflow-definitions.new-version` | `.create` | 以某个名字的最新版本为底复制出新草稿 |
| `workflow-definitions.disable` / `archive` / `delete` | `.update` / `.update` / `.delete` | 批量；停用只对已发布的，删除只删草稿 |
| `workflow-definitions.business-types` / `activity-types` | `.read` | 已登记的审批业务；设计器可放的活动（不含 Script 与框架 5 新增的事件网关、补偿、外部任务） |

关联审批业务的定义（名字等于业务码）发布前要满足：每个结束节点都写结论、结论在业务允许的范围里（缺省 `approved` / `rejected`，否则 `WORKFLOW_VALIDATION_101`），不能有终止节点（`WORKFLOW_VALIDATION_102`）。

## 流程实例

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `workflow-instances.list` / `get` | `workflow.workflow-instances.read` | 列表按标题关键字、状态、定义名、关联 ID、发起人与创建区间过滤，按创建时间降序；详情带变量、节点执行历史、待恢复的等待与所用的定义 |
| `workflow-instances.start` | `.execute` | 发起人是调用方；按名字取最新已发布的版本 |
| `workflow-instances.cancel` / `terminate` / `retry` / `signal` | `.execute` | 取消（启用补偿时逆序补偿）、终止（不补偿）、故障重试、发信号 |
| `workflow-instances.suspend` / `resume` | `.update` | 挂起与恢复 |

控制类都是批量，逐项返回 `BatchResult`。BasicApp 自己的错误码在 `WORKFLOW_` 前缀下从 101 起编（`WORKFLOW_NOTFOUND_101` 定义不存在、`102` 实例不存在），引擎、目录与任务服务的失败（001 段）原样返回。

## 受理人

框架内置 `User`、`Starter`、`Expression` 三种规则，本包登记六种组织类规则（按编码引用，结果与实例所在一侧可见的在用账号求交，运维支持成员、停用账号收不到待办）：

| 规则 | 参数 | 解析 |
| --- | --- | --- |
| `Role` | 角色编码 | 启用角色在有效期内的成员 |
| `Department` | 部门编码 | 启用部门的成员（不含下级） |
| `DepartmentLeader` | 部门编码 | 部门负责人 |
| `Position` | 岗位编码 | 启用部门里任这些岗位的人 |
| `StarterLeader` | 层级 N（1～10，缺省 1） | 发起人第 N 级主管：从主部门起逐级上溯，跳过停用部门、发起人自己负责的部门与重复的负责人 |
| `StarterLeaderChain` | 最多 N 级 | 第 1 到 N 级主管链，配 `Sequential` 做逐级审批 |

数据来自 Contracts 的组织目录（Organization 实现）与角色成员目录（Access 实现）。设计器与转办、加签选人用 `workflow-assignees.users`（只要登录；关键字只匹配登录名与显示名，缺省 50、至多 200 个；按 ID 回显含停用的账号）与 `roles` / `positions` / `departments`（`workflow.workflow-assignees.read`）。

## 审批业务接入

业务包在 `Configure` 里以 `services.AddWorkflowBusiness<THandler>()` 登记一个 `IWorkflowBusinessHandler`（Contracts `Workflow/`）：描述（业务码即流程定义名、单据组件、发起变量、允许的结论、缺省定义的 WorkflowJson）、办理前校验、结论回写、中止回写与单据数据。同一业务码登记两次，宿主启动即失败。发起、撤回与查进度经 `IWorkflowBusinessService`（随审批中心进来）。

## 演示流程

`BasicApp:Seed:EnableDemoData` 打开时，种子 `workflow.demo` 在演示租户 `demo` 里建并发布四个流程（按名字只首写）：

| 流程 | 走向 |
| --- | --- |
| `demo-expense` 费用报销 | 发起人的直属主管或签；金额超过 10000 再由总经理审批 |
| `demo-leave` 请假 | 两级主管依次审批（找不到人转管理员角色），抄送人事专员；超过 3 天再由人事部审批 |
| `demo-procurement` 采购 | 指定用户会签与总部负责人审核并行，汇聚后总经理审批；120 秒没办以超时关闭 |
| `demo-onboarding` 入职 | 设置变量、人事准备（抄送发起人）、导师确认、等 60 秒、逐件起采购子流程、差旅报销子流程 |

要用的人与组织见[组织架构](./organization#演示数据)。

## 模块开关

`BasicApp:Modules:Workflow:Enabled=false` 时 Workflow 包不装：权限码与页面不下发、操作 404，引擎与九张表照旧（宿主恒装数据库工作流）。

## 前端

`src/modules/workflow`：流程定义页（元数据弹窗、全屏设计器、发起弹窗）与流程实例页（详情抽屉、按节点状态着色的流程图）。设计器是 `@xihan-flow/*`，`packages/designers` 把它的 4.x 定义格式与 WorkflowJson 互相映射，面板去掉 Script。

## 下一步

- [约束规则与审批](./approval) · [组织架构](./organization) · [配置参考](../configuration#xihan-workflow)
