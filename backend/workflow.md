来源：https://basicapp.docs.xihanfun.com/backend/workflow

# 工作流

`XiHan.BasicApp.Workflow` 是四个一等业务模块之一，坐落在框架 [工作流引擎](https://framework.docs.xihanfun.com/packages/workflow) 之上，把「图执行引擎」变成一套可运营的审批系统：定义在管理端画、审批人按组织架构规则解析、业务单据挂到流程上走审批、审批人在审批中心办理，状态落库不怕重启。

> 引擎本身的机制（书签、活动、表达式、定时器、受理人规则、结果结束节点）见 [框架 · Workflow](https://framework.docs.xihanfun.com/packages/workflow)。本页只讲 BasicApp 这一层加了什么。

## 模块做了什么

模块类 `[DependsOn]` 同时挂 `XiHanBasicAppSaasModule` 与 `XiHanWorkflowModule`，`ConfigureServices` 按职责分成几段：

```csharp
services.AddWorkflowStores();               // ① Replace 掉框架的内存存储
services.AddWorkflowAssigneeProviders();    // ② 组织架构受理人规则（角色 / 部门 / 岗位 / 主管）
services.AddWorkflowBusinessIntegration();  // ③ 审批业务接入（发起、回写、撤回、进度）
services.AddWorkflowFieldSecurityEntities();
services.AddWorkflowDataSeeders();          // ④ 权限与菜单种子
services.AddWorkflowEventHandlers();        // ⑤ 待办通知与抄送记录
```

仓储与应用服务全部交给约定注册。

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

存储是单例、每次操作开独立作用域；但 SqlSugar 客户端解析会加入当前环境里的事务工作单元，所以业务在 `[UnitOfWork(true)]` 里发起流程时，引擎写入与业务写入同一个事务提交或回滚。

另有一张 `Sys_Workflow_Task_Record` 记下每次办理、转办、加签与抄送，供「我已审批」「抄送我的」与审批轨迹使用——引擎的书签在办理后即删除，不能用来回看。

### ② 组织架构受理人规则

人工任务的受理人按规则 `{ type, values }` 解析（规则机制与内置的 `User` / `Starter` / `Expression` 见框架文档）。BasicApp 基于 Saas 组织数据注册了六种：

| 规则 | 参数 | 解析为 |
| --- | --- | --- |
| `Role` | 角色编码列表 | 启用角色里有效期内的成员 |
| `Department` | 部门编码列表 | 启用部门的有效成员（不含下级部门） |
| `DepartmentLeader` | 部门编码列表 | 部门负责人 |
| `Position` | 岗位编码列表 | 部门任职上挂这些岗位的用户 |
| `StarterLeader` | 层级 N（缺省 1） | 发起人的第 N 级主管 |
| `StarterLeaderChain` | 最多层级 N（缺省 1） | 发起人往上逐级的主管，配合「依次审批」做连续多级 |

角色、部门、岗位按**编码**引用（编码创建后不可改），同一份流程定义可以在租户间复用，业务的默认流程模板也能直接写系统角色编码。主管从发起人的主部门起沿上级部门逐级找负责人；发起人自己是某部门负责人时跳过该部门。

节点上与规则配套的设置：

| 设置 | 作用 |
| --- | --- |
| 跳过发起人本人 `ExcludeStarter` | 解析结果里去掉发起人，避免「自己批自己」 |
| 找不到受理人时 `EmptyAssigneeStrategy` | `Fault` 流程报错（缺省）/ `AutoApprove` / `AutoReject` / `Fallback` 转交兜底受理人 |
| 兜底受理人 `FallbackAssigneeRules` | 策略为 `Fallback` 时使用 |
| 抄送人 `CcRules` | 任务创建时抄送，进「抄送我的」，不参与办理 |
| 完成策略 `CompletionPolicy` | 或签 / 会签 / 依次审批 |

设计器的属性面板按这些规则给出选择器：角色、部门、岗位从组织目录选，用户远程检索，主管填层级，表达式提示可用的流程变量。旧版定义里的 `Assignees` / `AssigneesExpression` / `CcUserIds` 打开时会并入规则。

### ③ 审批业务接入

这是把「报销」「权限申请」这类业务单据挂到流程上的机制，详见下文 [审批业务接入](#审批业务接入)。

### ④ 权限与菜单

权限码是独立命名空间 `workflow:*`（不是 `saas:*`）：

| 权限码 | 用途 |
| --- | --- |
| `workflow:read` | 查看定义 / 实例；也可以查看任意审批的详情 |
| `workflow:create` | 新建定义 |
| `workflow:update` | 编辑草稿、发布、新版本、停用、归档；挂起 / 恢复实例 |
| `workflow:delete` | 删除定义 |
| `workflow:execute` | 发起实例、干预实例（取消/终止/重试） |

种子链 `Order` 300–304，顺序恒为「操作 300 → 资源 301 → 权限 302 → 菜单 303 → 角色授权 304」，默认**仅授超管**。

菜单由模块自己的 `PageRegistry` 驱动：

| 页面 | 路由 | 权限 |
| --- | --- | --- |
| 审批中心 | `/approval/center`（挂在「审批」目录下） | 无（主体服务端锁定为当前用户） |
| 流程定义 | `/workflow/definition` | `workflow:read` |
| 流程实例 | `/workflow/instance` | `workflow:read` |

### ⑤ 待办通知与抄送记录

四个本地事件处理器，经 `AddWorkflowLocalEventHandler<T>` 登记进 `XiHanLocalEventBusOptions.Handlers`：

- 待办**创建** → 站内通知受理人
- 待办**转办** → 通知新受理人
- 实例**故障** → 通知相关人
- 任务**抄送** → 写抄送记录

::: warning 裸 `AddTransient` 不会被订阅
本地事件总线只自动发现「以接口为服务类型」的注册。加新处理器必须走 `AddWorkflowLocalEventHandler<T>()`，否则静默不触发。
:::

## 三个页面能做什么

### 审批中心（`/approval/center`）

四个页签，主体都由服务端锁定为当前用户，所以页面不需要权限码：

| 页签 | 内容 |
| --- | --- |
| 待我审批 | 当前用户的待办；「办理」打开详情抽屉 |
| 我已审批 | 当前用户办理、转办、加签过的记录，以及流程现在的状态与结果 |
| 我发起的 | 当前用户发起的流程，停在哪个节点、等谁审批；可按状态筛选 |
| 抄送我的 | 抄送给当前用户的流程 |

详情抽屉自上而下是：概要（状态、结果、业务、发起人）、**业务单据**（由审批业务登记的视图组件渲染）、办理区（仅待办）、撤回（仅发起人且流程进行中）、审批轨迹（发起 → 每次办理 / 转办 / 加签 / 抄送 / 自动处理 → 当前等待谁 → 流程结束）。

| 动作 | 说明 |
| --- | --- |
| 办理 | 同意 / 驳回，或节点白名单里的自定义结果；可带意见。单据组件暴露 `getVariables()` 时，返回值作为附加变量一起提交 |
| 转办 | 把任务移交给另一位用户（转办后原受理人的办理会被拒绝） |
| 加签 | 追加受理人 |
| 撤回 | 发起人撤回进行中（含故障）的流程，业务单据回到撤回状态 |

办理后若后续节点或业务回写出错，审批已经记录，页面提示流程进入故障态，由管理员在流程实例页重试。

查看详情的人须是发起人、受理人、办理或抄送过的人，或拥有 `workflow:read`；业务单据数据随之下发，业务侧不必再给审批人开单据的查看权限。

地址栏带 `?instanceId=` 会直接打开该流程的详情（业务页面的「查看审批」就是这样跳过来的），`?tab=done` 等预选页签（待办通知里的链接用它）。这两个参数只用一次：页面读完就把地址换回 `/approval/center`，带参数的标签并入审批中心的标签，切回来或刷新不会再弹出详情。

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
| 发布 | `PublishAsync` | **只有已发布的定义才能启动实例**；编码等于审批业务编码时，每个结束节点都要声明该业务允许的结果，且不能有终止节点 |
| 新版本 | `NewVersionAsync` | 基于现有定义开一个新版本草稿 |
| 停用 | `DisableAsync` | 不可启动新实例，**存量实例继续运行** |
| 归档 | `ArchiveAsync` | 彻底下架 |

列表的「关联业务」列标出编码与哪个审批业务对应。设计器的流程设置里可以选「审批业务」：选中后流程编码随之改成业务编码，并补上业务写入的流程变量声明；校验面板提前提示缺结果的结束节点与终止节点（发布时后端同样拒绝）。

### 流程实例（`/workflow/instance`）

分页查询 + 详情（**含变量、执行历史与待恢复等待点**），以及干预动作：

| 动作 | 语义 |
| --- | --- |
| 发起 `StartAsync` | 当前用户为发起人；同步链路能走多远走多远，可能一次调用就直接完成 |
| 取消 `CancelAsync` | 删书签、取消挂起节点；**定义启用补偿时按执行逆序补偿** |
| 终止 `TerminateAsync` | 强制结束，**不执行补偿**、不可恢复 |
| 重试 `RetryAsync` | 从故障节点重新执行 |

取消与终止在实例已处于终态时**幂等返回**；故障实例也可以取消或终止（不必先重试成功）。审批业务的实例被取消或终止后，会回调业务把单据置为中止。

实例状态：`Running`（含等待书签的空闲态）/ `Suspended` / `Completed` / `Canceled` / `Faulted`（可重试）/ `Terminated`。经结果结束节点完成的实例另有「结果」（如 `approved` / `rejected`），列表与详情都会显示。

## 审批业务接入

业务单据（报销、合同、权限申请……）接审批流，只需要写一个处理器，不用碰引擎。约定只有两条：

- **业务编码 = 流程定义编码**。发起时按编码取当前租户最新已发布的版本；管理员在设计器里改流程、发新版本，业务代码不用动。
- **单据主键 = 实例的 `CorrelationId`**。按单据反查流程、批量查进度都靠它。

```text
业务提交（[UnitOfWork(true)]）
   │ 落业务表（状态 = 待审批）
   │ IWorkflowBusinessService.StartAsync(业务编码, 单据主键, 标题, 变量)
   ▼                                   ← 引擎写入与业务写入同一事务
UserTask：按受理人规则解析审批人 → 待办 + 站内通知
   │ 审批人在审批中心办理
   │   └─ OnTaskCompletingAsync：办理前校验（如「不能审批自己的申请」），抛异常即拒绝办理
   ▼
走到结果结束节点（Outcome = approved / rejected）
   │   └─ OnOutcomeAsync：回写业务表、授予权限……失败则回滚，实例进入故障态待重试
   ▼
实例 Completed（结果 = approved / rejected）
```

### 1. 实现处理器

处理器接口 `IWorkflowBusinessHandler` 在 Saas 里（业务模块不依赖工作流模块）：

```csharp
public sealed class ExpenseWorkflowHandler : IWorkflowBusinessHandler
{
    public WorkflowBusinessDescriptor Descriptor { get; } = new()
    {
        Code = "expense",                                  // 同时是流程编码
        Name = "费用报销",
        FormComponent = "finance/expense/expense-approval", // 审批抽屉里渲染单据的前端组件
        Variables = [new("amount", "报销金额", "number")],  // 设计器据此提示分支条件
        DefaultDefinition = BuildDefaultDefinition,         // 可空：租户首次发起时自动建流程
    };

    // 办理前校验：只读，抛异常即拒绝本次办理
    public Task OnTaskCompletingAsync(WorkflowBusinessTaskContext context, CancellationToken ct) => Task.CompletedTask;

    // 流程得出结论：回写单据（必须幂等）
    public async Task OnOutcomeAsync(WorkflowBusinessOutcomeContext context, CancellationToken ct)
    {
        // context.Outcome 为结束节点声明的 approved / rejected（或 Descriptor.Outcomes 里的自定义结果）
    }

    // 流程被审批中心撤回、或被管理员取消 / 终止：把单据置为中止（必须幂等）
    public async Task OnAbortedAsync(WorkflowBusinessAbortedContext context, CancellationToken ct) { }

    // 审批抽屉读取单据数据；单据不存在返回 null
    public async Task<object?> GetFormDataAsync(string businessId, CancellationToken ct) => ...;
}
```

在业务模块里注册：

```csharp
services.TryAddEnumerable(ServiceDescriptor.Transient<IWorkflowBusinessHandler, ExpenseWorkflowHandler>());
```

### 2. 在业务写侧发起与撤回

```csharp
[UnitOfWork(true)]
public async Task<ExpenseDto> SubmitAsync(ExpenseSubmitDto input, CancellationToken ct)
{
    var expense = await _expenseRepository.AddAsync(..., ct);       // 状态 = 待审批
    var result = await _workflowBusinessService.StartAsync(new WorkflowBusinessStartRequest
    {
        BusinessCode = "expense",
        BusinessId = expense.BasicId.ToString(CultureInfo.InvariantCulture),
        Title = $"费用报销：{expense.Title}",
        Variables = { ["amount"] = expense.Amount },
    }, ct);
    expense.WorkflowInstanceId = result.InstanceId;                 // 单据记住实例，便于跳转审批中心
    ...
}
```

| 方法 | 说明 |
| --- | --- |
| `StartAsync` | 当前用户为发起人。业务未登记、同一单据已有进行中的审批、流程未发布、发起即故障（如首个节点解析不到审批人）都抛业务异常，事务随之回滚 |
| `WithdrawAsync` | 业务自己撤回（删除单据、修改后重新提交前）。业务已自行更新单据状态，**不再回调** `OnAbortedAsync` |
| `GetProgressAsync` | 批量查单据的审批进度：状态、结果、停在哪个节点、等谁审批。列表页一次查一页 |

工作流模块没有加载时，`IWorkflowBusinessService` 的占位实现对每个调用都抛业务异常——业务不会在不知情的情况下跳过审批。

### 3. 回调的事务与上下文

| 回调 | 何时、在哪个上下文 | 失败时 |
| --- | --- | --- |
| `OnTaskCompletingAsync` | 审批人办理之前，审批人的请求上下文 | 拒绝本次办理，待办不变 |
| `OnOutcomeAsync` | 结果结束节点执行时；有环境事务就加入，否则开独立工作单元 | 回滚回写，实例进入故障态；管理员重试会再次调用 |
| `OnAbortedAsync` | 审批中心撤回、实例被取消 / 终止之后 | 记录日志，不影响已完成的中止 |
| `GetFormDataAsync` | 查看审批详情时，按流程参与关系鉴权 | — |

`OnOutcomeAsync` 与 `OnAbortedAsync` 都可能被重复调用，必须幂等（例如按单据当前状态判断是否已处理）。

### 4. 默认流程模板

`DefaultDefinition` 让业务开箱即用：某租户第一次发起该业务、且租户里还没有任何同编码的流程定义时，按模板建一个并发布；之后它和手工设计的流程一样可改、可发新版本。租户里已有同编码定义（哪怕是草稿或已停用）就不再自动建——没发布则发起失败，尊重管理员的选择。

模板用框架的构建器写，结束节点用 `AddOutcomeEnd` 声明结果：

```csharp
private static WorkflowDefinition BuildDefaultDefinition() =>
    WorkflowDefinitionBuilder.Create("expense", "费用报销")
        .AddStart()
        .AddUserTask("leader-approve", "主管审批", node => node
            .WithAssigneeRule(OrganizationAssigneeRuleTypes.StarterLeader, "1")
            .WithProperty("ExcludeStarter", true))
        .AddOutcomeEnd("end-approved", WorkflowUserTaskOutcomes.Approved, "通过")
        .AddOutcomeEnd("end-rejected", WorkflowUserTaskOutcomes.Rejected, "驳回")
        .AddTransition("start", "leader-approve")
        .AddTransition("leader-approve", "end-approved", "outcome == 'approved'")
        .AddTransition("leader-approve", "end-rejected", "outcome == 'rejected'")
        .Build();
```

### 5. 前端单据组件

`FormComponent` 是相对 `src/views` 的组件路径（与菜单 `Component` 同口径）。审批抽屉按路径加载它，传入：

| 属性 | 说明 |
| --- | --- |
| `businessId` | 单据主键 |
| `data` | `GetFormDataAsync` 的返回值 |
| `mode` | `approve`（办理中）/ `view`（只读查看） |
| `nodeId` | 当前待办节点（只读查看时为空） |

需要审批人补填字段时，组件 `defineExpose({ getVariables })`，办理时返回值作为流程变量一起提交。业务列表页可以用 `GetProgressAsync` 的结果显示审批进度，并以 `/approval/center?instanceId=<实例主键>` 跳到审批详情。

### 参照实现：权限申请

`PermissionRequestWorkflowHandler`（编码 `permission-request`）是第一个接入的业务：

- 提交申请即发起流程；删除、修改内容或改到终态时撤回进行中的审批，修改后重新发起；
- 默认模板是一个「管理员审批」节点，受理人为 `tenant_owner` 与 `super_admin` 角色、跳过申请人本人；
- 办理前拒绝审批自己的申请，并检查审批人能否授出该角色 / 权限；
- 通过时授予角色或权限并刷新授权缓存，驳回时只改申请状态；撤回或终止时申请置为已撤回。

## 运维注意

| 项 | 说明 |
| --- | --- |
| **定时器开关** | `XiHan:Workflow:Worker:IsTimerEnabled` 关掉后，延时 / 重试 / 节点超时书签**永不自动恢复**，只有人工任务与信号还能推进流程 |
| **集群单活** | 定时器 Worker 靠分布式锁保证集群内单活；分布式锁来自 Caching，**未接 Redis 时会退化为进程内锁**，多实例会各跑各的 |
| **环路防护** | 单批次最大节点执行数（默认 1000）、子流程最大嵌套深度（默认 16）都有硬上限，坏定义不会把进程转死 |
| **并发** | 引擎对同一实例的所有操作以分布式锁保证单写者；抢锁超时会抛 `WorkflowLockTimeoutException` |
| **业务回写失败** | 实例停在 `Faulted`，审批记录已保留；修好原因后在流程实例页重试，`OnOutcomeAsync` 会再次执行 |

## 相关页面

- [框架 · Workflow](https://framework.docs.xihanfun.com/packages/workflow)：引擎机制、内置活动、受理人规则、结果结束节点、代码方式定义流程
- [框架 · Workflow.Abstractions](https://framework.docs.xihanfun.com/packages/workflow-abstractions)：定义模型与存储端口
- [审批与约束](./approval)：审批中心与 RBAC 约束规则引擎的分工
- [权限模型](./permission)：权限申请与委托
- [数据模型](./data-model#工作流)：工作流的表
- [消息中心](./messaging)：待办通知的投递通道
