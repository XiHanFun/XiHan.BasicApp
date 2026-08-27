// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.UserTasks;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 工作流测试夹具共享工具。
/// </summary>
/// <remarks>
/// 所有时间常量均以显式 <see cref="DateTimeKind.Utc"/> 构造，测试因此不依赖机器时区与当前时间；
/// 模型工厂刻意把「只活在 JSON 真源里」的字段（变量/汇聚状态/输入输出/载荷）全部填满，
/// 使映射往返一旦丢字段就会在断言处失败。
/// </remarks>
public static class WorkflowTestHelper
{
    /// <summary>
    /// 固定创建时间（不依赖当前时间与机器时区）。
    /// </summary>
    public static readonly DateTime CreationTime = new(2024, 3, 1, 8, 30, 0, DateTimeKind.Utc);

    /// <summary>
    /// 固定开始时间。
    /// </summary>
    public static readonly DateTime StartTime = new(2024, 3, 1, 8, 31, 0, DateTimeKind.Utc);

    /// <summary>
    /// 固定结束时间。
    /// </summary>
    public static readonly DateTime EndTime = new(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 固定发布时间。
    /// </summary>
    public static readonly DateTime PublishTime = new(2024, 2, 20, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 固定到期时间（定时类书签）。
    /// </summary>
    public static readonly DateTime DueTime = new(2024, 3, 2, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 测试中模拟持久化层回填受保护的实体主键。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="entity">待回填主键的实体。</param>
    /// <param name="id">主键值。</param>
    public static void SetBasicId<TEntity>(TEntity entity, long id)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var property = typeof(TEntity).GetProperty(
            "BasicId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"未找到 {typeof(TEntity).Name} 的主键属性。");
        property.SetValue(entity, id);
    }

    /// <summary>
    /// 以指定服务注册构建带作用域计数的服务作用域工厂。
    /// </summary>
    /// <param name="configure">服务注册回调（把 Moq 仓储登记为 Scoped）。</param>
    /// <returns>带计数的作用域工厂。</returns>
    public static CountingScopeFactory CreateScopeFactory(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var services = new ServiceCollection();
        configure(services);
        var provider = services.BuildServiceProvider();
        return new CountingScopeFactory(provider.GetRequiredService<IServiceScopeFactory>());
    }

    /// <summary>
    /// 构造完整的定义模型（含节点/连线/变量声明/扩展属性）。
    /// </summary>
    /// <param name="id">定义标识（雪花数值字符串）。</param>
    /// <param name="status">定义状态。</param>
    /// <param name="tenantId">租户标识（null 表示平台级）。</param>
    /// <returns>定义模型。</returns>
    public static WorkflowDefinition CreateDefinition(
        string id = "100200300400500",
        WorkflowDefinitionStatus status = WorkflowDefinitionStatus.Published,
        long? tenantId = 7)
    {
        return new WorkflowDefinition
        {
            Id = id,
            Code = "leave",
            Name = "请假流程",
            Version = 3,
            Description = "员工请假审批",
            Category = "hr",
            Status = status,
            EnableCompensation = true,
            Nodes =
            [
                new WorkflowNode
                {
                    Id = "start",
                    Name = "开始",
                    ActivityType = "Start",
                    Properties = new Dictionary<string, object?> { ["assignee"] = "approver" },
                    TimeoutSeconds = 120,
                    ContinueOnError = true,
                    RetryPolicy = new WorkflowRetryPolicy { MaxAttempts = 3, FirstDelaySeconds = 5, BackoffFactor = 1.5 }
                },
                new WorkflowNode { Id = "end", Name = "结束", ActivityType = "End" }
            ],
            Transitions =
            [
                new WorkflowTransition
                {
                    Id = "t1",
                    Name = "直连",
                    SourceNodeId = "start",
                    TargetNodeId = "end",
                    Condition = "days > 1",
                    Priority = 5,
                    IsDefault = true
                }
            ],
            Variables =
            [
                new WorkflowVariableDefinition
                {
                    Name = "days",
                    Type = "int",
                    Required = true,
                    DefaultValue = 1,
                    Description = "请假天数"
                }
            ],
            TenantId = tenantId,
            CreationTime = CreationTime,
            PublishTime = PublishTime,
            ExtraProperties = new Dictionary<string, string> { ["layout"] = "{\"x\":1}" }
        };
    }

    /// <summary>
    /// 构造完整的实例模型（含变量/汇聚状态/父子链接/故障信息）。
    /// </summary>
    /// <param name="id">实例标识。</param>
    /// <param name="status">实例状态。</param>
    /// <param name="parentInstanceId">父实例标识（null 表示顶层实例）。</param>
    /// <param name="tenantId">租户标识。</param>
    /// <param name="faultMessage">故障信息。</param>
    /// <returns>实例模型。</returns>
    public static WorkflowInstance CreateInstance(
        string id = "900800700600500",
        WorkflowInstanceStatus status = WorkflowInstanceStatus.Faulted,
        string? parentInstanceId = "111222333444555",
        long? tenantId = 7,
        string? faultMessage = "节点执行超时")
    {
        return new WorkflowInstance
        {
            Id = id,
            DefinitionId = "100200300400500",
            DefinitionCode = "leave",
            DefinitionVersion = 3,
            Name = "张三的请假",
            Status = status,
            Variables = new Dictionary<string, object?> { ["days"] = "3", ["reason"] = "年假" },
            JoinStates = new Dictionary<string, WorkflowJoinState>
            {
                ["join1"] = new WorkflowJoinState { ArrivedTransitionIds = ["t1", "t2"], Fired = true }
            },
            CorrelationId = "ORDER-2024-0001",
            StarterId = "1001",
            ParentInstanceId = parentInstanceId,
            ParentNodeInstanceId = "555444333222111",
            Depth = 1,
            TenantId = tenantId,
            CreationTime = CreationTime,
            StartTime = StartTime,
            EndTime = EndTime,
            FaultMessage = faultMessage,
            FaultNodeId = "approve",
            FaultNodeInstanceId = "777666555444333",
            CancellationReason = "申请人撤回"
        };
    }

    /// <summary>
    /// 构造完整的节点实例模型（含输入/输出/活动私有状态）。
    /// </summary>
    /// <param name="id">节点实例标识。</param>
    /// <param name="status">节点实例状态。</param>
    /// <param name="tenantId">租户标识。</param>
    /// <returns>节点实例模型。</returns>
    public static WorkflowNodeInstance CreateNodeInstance(
        string id = "300300300300300",
        WorkflowNodeInstanceStatus status = WorkflowNodeInstanceStatus.Compensated,
        long? tenantId = 7)
    {
        return new WorkflowNodeInstance
        {
            Id = id,
            InstanceId = "900800700600500",
            NodeId = "approve",
            Name = "部门审批",
            ActivityType = "UserTask",
            Status = status,
            TryCount = 2,
            StartTime = StartTime,
            EndTime = EndTime,
            Inputs = new Dictionary<string, object?> { ["assignee"] = "1001" },
            Outputs = new Dictionary<string, object?> { ["outcome"] = "approved" },
            State = new Dictionary<string, object?> { ["cursor"] = "2" },
            FaultMessage = "审批人不存在",
            CompensatedTime = EndTime,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// 构造完整的书签模型（含节点标识与附加载荷）。
    /// </summary>
    /// <param name="id">书签标识。</param>
    /// <param name="kind">书签种类。</param>
    /// <param name="key">索引键（null 表示不按键检索的种类）。</param>
    /// <param name="dueTime">到期时间（null 表示非定时类）。</param>
    /// <param name="tenantId">租户标识。</param>
    /// <returns>书签模型。</returns>
    public static WorkflowBookmark CreateBookmark(
        string id = "400400400400400",
        string kind = WorkflowBookmarkKinds.UserTask,
        string? key = "1001",
        DateTime? dueTime = null,
        long? tenantId = 7)
    {
        return new WorkflowBookmark
        {
            Id = id,
            InstanceId = "900800700600500",
            NodeId = "approve",
            NodeInstanceId = "300300300300300",
            Kind = kind,
            Key = key,
            Payload = new Dictionary<string, object?> { ["title"] = "部门审批", ["form"] = "leave-form" },
            DueTime = dueTime,
            CorrelationId = "ORDER-2024-0001",
            CreationTime = CreationTime,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// 构造待办任务模型。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="title">任务标题。</param>
    /// <param name="instanceName">实例名称。</param>
    /// <param name="definitionCode">定义编码。</param>
    /// <param name="correlationId">业务相关性标识。</param>
    /// <param name="creationTime">创建时间（null 取固定基准时间）。</param>
    /// <returns>待办任务模型。</returns>
    public static WorkflowUserTask CreateUserTask(
        string taskId = "400400400400400",
        string title = "部门审批",
        string instanceName = "张三的请假",
        string definitionCode = "leave",
        string? correlationId = "ORDER-2024-0001",
        DateTime? creationTime = null)
    {
        return new WorkflowUserTask
        {
            TaskId = taskId,
            InstanceId = "900800700600500",
            InstanceName = instanceName,
            DefinitionCode = definitionCode,
            NodeId = "approve",
            NodeInstanceId = "300300300300300",
            Title = title,
            AssigneeId = "1001",
            CorrelationId = correlationId,
            FormData = new Dictionary<string, object?> { ["amount"] = 3 },
            CreationTime = creationTime ?? CreationTime,
            TenantId = 7
        };
    }

    /// <summary>
    /// 长整型转框架标识（不变文化十进制，与生产代码同口径）。
    /// </summary>
    /// <param name="id">主键。</param>
    /// <returns>框架标识字符串。</returns>
    public static string ToWorkflowId(long id)
    {
        return id.ToString(CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// 记录作用域创建次数的服务作用域工厂装饰器。
/// </summary>
/// <remarks>
/// 三个 SqlSugar 存储都是单例，必须「每个操作新建一个作用域」解析 Scoped 仓储；
/// 直接持有仓储会跨请求泄漏数据库连接，本装饰器把这一契约变成可断言的计数。
/// </remarks>
public sealed class CountingScopeFactory : IServiceScopeFactory
{
    private readonly IServiceScopeFactory _inner;
    private int _scopeCount;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="inner">被装饰的真实作用域工厂。</param>
    public CountingScopeFactory(IServiceScopeFactory inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// 已创建的作用域数量。
    /// </summary>
    public int ScopeCount => _scopeCount;

    /// <summary>
    /// 创建作用域并计数。
    /// </summary>
    /// <returns>服务作用域。</returns>
    public IServiceScope CreateScope()
    {
        _scopeCount++;
        return _inner.CreateScope();
    }
}

/// <summary>
/// 记录日志条目的测试日志器（用于断言"跳过投递并记 Warning"这类旁路行为）。
/// </summary>
/// <typeparam name="TCategory">日志类别。</typeparam>
public sealed class RecordingLogger<TCategory> : ILogger<TCategory>
{
    /// <summary>
    /// 已记录的日志条目（级别 + 渲染后的消息）。
    /// </summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <summary>
    /// 开始逻辑操作范围（测试中不使用）。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <param name="state">状态。</param>
    /// <returns>始终返回 null。</returns>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <summary>
    /// 是否启用指定级别（测试中全部启用）。
    /// </summary>
    /// <param name="logLevel">日志级别。</param>
    /// <returns>始终返回 true。</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <summary>
    /// 记录日志。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <param name="logLevel">日志级别。</param>
    /// <param name="eventId">事件标识。</param>
    /// <param name="state">状态。</param>
    /// <param name="exception">异常。</param>
    /// <param name="formatter">格式化器。</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        Entries.Add((logLevel, formatter(state, exception)));
    }
}
