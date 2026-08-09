# 工作流

`XiHan.BasicApp.Workflow` 是四个一等业务模块之一，坐落在框架 [工作流引擎](https://framework.docs.xihanfun.com/packages/workflow) 之上，把「图执行引擎」变成一套可运营的审批/流程系统：定义能在管理端建、实例能在页面上看和干预、待办能办理和转办、状态落库不怕重启。

> 引擎本身的机制（书签、活动、表达式、定时器）见 [框架 · Workflow](https://framework.docs.xihanfun.com/packages/workflow)。本页只讲 BasicApp 这一层加了什么。

## 模块做了什么

模块类 `[DependsOn]` 同时挂 `XiHanBasicAppSaasModule` 与 `XiHanWorkflowModule`，`ConfigureServices` 只有三行：

```csharp
services.AddWorkflowStores();          // ① Replace 掉框架的内存存储
services.AddWorkflowDataSeeders();     // ② 权限与菜单种子（Order 300–304）
services.AddWorkflowEventHandlers();   // ③ 待办通知
```

仓储与应用服务全部交给约定注册。**这是仓库里最干净的独立模块样板**——要照着做一个新模块，读它比读 AI 模块省力。

### ① 存储持久化（最关键的一层）

框架默认的三个存储是**进程内内存实现，进程一停全丢**。BasicApp 用 `Replace` 换成 SqlSugar 落库：

| 框架接口 | BasicApp 实现 | 表 |
| --- | --- | --- |
| `IWorkflowDefinitionStore` | `SqlSugarWorkflowDefinitionStore` | `Sys_Workflow_Definition` |
| `IWorkflowInstanceStore` | `SqlSugarWorkflowInstanceStore` | `Sys_Workflow_Instance` + `Sys_Workflow_Node_Instance` |
| `IWorkflowBookmarkStore` | `SqlSugarWorkflowBookmarkStore` | `Sys_Workflow_Bookmark` |

::: danger 必须用 `Replace`
框架 `AddXiHanWorkflow` 已经 `TryAddSingleton` 了内存实现，你再 `TryAdd` 会被静默忽略——表现是「重启后流程全没了」而没有任何报错。
:::

落库之后才谈得上**崩溃恢复**：进程重启后定时器 Worker 能重新捞到到期书签，挂起中的实例继续往下走。

### ② 权限与菜单

权限码是独立命名空间 `workflow:*`（不是 `saas:*`）：

| 权限码 | 用途 |
| --- | --- |
| `workflow:read` | 查看定义 / 实例 / 待办 |
| `workflow:create` | 新建定义 |
| `workflow:update` | 编辑草稿、发布、新版本、停用、归档 |
| `workflow:delete` | 删除定义 |
| `workflow:execute` | 发起实例、干预实例（取消/终止/重试） |

种子链 `Order` 300–304，顺序恒为「操作 300 → 资源 301 → 权限 302 → 菜单 303 → 角色授权 304」，默认**仅授超管**。

菜单由模块自己的 `PageRegistry` 驱动：

| 页面 | 路由 | 权限 |
| --- | --- | --- |
| 我的待办 | `/workflow/todo` | 无（受理人服务端锁定为当前用户） |
| 流程定义 | `/workflow/definition` | `workflow:read` |
| 流程实例 | `/workflow/instance` | `workflow:read` |

### ③ 待办通知

三个本地事件处理器，经 `AddWorkflowLocalEventHandler<T>` 登记进 `XiHanLocalEventBusOptions.Handlers`：

- 待办**创建** → 站内通知受理人
- 待办**转办** → 通知新受理人
- 实例**故障** → 通知相关人

::: warning 裸 `AddTransient` 不会被订阅
本地事件总线只自动发现「以接口为服务类型」的注册。加新处理器必须走 `AddWorkflowLocalEventHandler<T>()`，否则静默不触发。
:::

## 三个页面能做什么

### 流程定义（`/workflow/definition`）

定义的生命周期是一条状态链：

```text
Draft 草稿 ──发布──► Published 已发布 ──停用──► Disabled 已停用 ──► Archived 已归档
   ▲                      │
   └──── 新版本 ◄─────────┘
```

| 操作 | 应用服务方法 | 约束 |
| --- | --- | --- |
| 新建 | `CreateAsync` | 建出来是 `Draft` |
| 编辑 | `UpdateDraftAsync` | **只能改草稿**，已发布的不能直接改 |
| 发布 | `PublishAsync` | **只有已发布的定义才能启动实例** |
| 新版本 | `NewVersionAsync` | 基于现有定义开一个新版本草稿 |
| 停用 | `DisableAsync` | 不可启动新实例，**存量实例继续运行** |
| 归档 | `ArchiveAsync` | 彻底下架 |

定义本身是**可序列化的纯数据**（节点 + 连线 + 变量），所以能落库、能在管理端编辑，改流程不需要改代码重新部署。

### 流程实例（`/workflow/instance`）

分页查询 + 详情（**含变量、执行历史与待恢复等待点**），以及四个干预动作：

| 动作 | 语义 |
| --- | --- |
| 发起 `StartAsync` | 当前用户为发起人；同步链路能走多远走多远，可能一次调用就直接完成 |
| 取消 `CancelAsync` | 删书签、取消挂起节点；**定义启用补偿时按执行逆序补偿** |
| 终止 `TerminateAsync` | 强制结束，**不执行补偿**、不可恢复 |
| 重试 `RetryAsync` | 从故障节点重新执行 |

取消与终止在实例已处于终态时**幂等返回**，不会报错。

实例状态：`Running`（含等待书签的空闲态）/ `Suspended` / `Completed` / `Canceled` / `Faulted`（可重试）/ `Terminated`。

### 我的待办（`/workflow/todo`）

**受理人与办理人都由服务端锁定为当前用户**，所以这个页面不需要额外权限码——你只能看到和办理属于自己的待办。

| 动作 | 说明 |
| --- | --- |
| 办理 `CompleteAsync` | 同意 / 拒绝 / 自定义结果，可带意见与附加变量 |
| 转办 | 把任务移交给新受理人（转办后原受理人的办理动作会被拒绝） |
| 加签 | 追加受理人 |

办理结果除内置的 `approved` / `rejected` / `timeout` 外**允许自定义**——配合连线条件即可做多路分支（如「退回补充材料」走一条单独的线）。

完成策略由节点配置决定：**或签**（任一同意即通过、任一拒绝即拒绝）、**会签**（全部同意才通过、任一拒绝一票否决）、**依次审批**（按顺序逐一）。

## 典型接入姿势

业务系统接工作流通常是这样：

```text
业务写侧（如报销申请提交）
   │ 落业务表（状态=审批中）
   │ 调 IWorkflowEngine.StartAsync(定义编码, 变量, CorrelationId=业务单号)
   ▼
流程跑到 UserTask 节点 → 写 UserTask 书签 → 实例挂起（不占线程）
   │ 事件处理器发站内通知给受理人
   ▼
受理人在「我的待办」办理 → CompleteAsync → 引擎恢复书签继续推进
   ▼
流程走到 End → 实例 Completed
   │ 业务侧订阅实例完成事件（或流程里用 PublishEvent 活动）
   ▼
回写业务表（状态=已通过/已驳回）
```

两个建议：

- **用 `CorrelationId` 挂业务单号**，这样按业务单反查流程实例、或用 `PublishSignalAsync` 定向唤醒都很自然。
- **业务状态以业务表为准**，流程实例只驱动流转。不要把业务状态只存在流程变量里——查询、报表、权限都会很难做。

## 运维注意

| 项 | 说明 |
| --- | --- |
| **定时器开关** | `XiHan:Workflow:Worker:IsTimerEnabled` 关掉后，延时 / 重试 / 节点超时书签**永不自动恢复**，只有人工任务与信号还能推进流程 |
| **集群单活** | 定时器 Worker 靠分布式锁保证集群内单活；分布式锁来自 Caching，**未接 Redis 时会退化为进程内锁**，多实例会各跑各的 |
| **环路防护** | 单批次最大节点执行数（默认 1000）、子流程最大嵌套深度（默认 16）都有硬上限，坏定义不会把进程转死 |
| **并发** | 引擎对同一实例的所有操作以分布式锁保证单写者；抢锁超时会抛 `WorkflowLockTimeoutException` |

## 相关页面

- [框架 · Workflow](https://framework.docs.xihanfun.com/packages/workflow)：引擎机制、内置活动、配置项、代码方式定义流程
- [框架 · Workflow.Abstractions](https://framework.docs.xihanfun.com/packages/workflow-abstractions)：定义模型与存储端口
- [审批与约束](./approval)：另一套更轻的审批（`SysReview`）与 RBAC 约束规则引擎
- [数据模型](./data-model#工作流)：四张表
- [消息中心](./messaging)：待办通知的投递通道
