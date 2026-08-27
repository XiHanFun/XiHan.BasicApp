// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 框架运行时模型与 SqlSugar 实体之间双向映射的往返锁定测试。
/// </summary>
/// <remarks>
/// 实体的 JSON 列是真源、其余列只是检索投影，这条约定意味着两类静默 bug：
/// 一是「投影列漏写」——列表页字段查不到但详情页正常；
/// 二是「JSON 丢字段」——引擎恢复实例时变量/汇聚状态/活动私有状态凭空消失，流程走错分支。
/// 本文件对四个模型逐字段断言往返结果，任何一侧漏字段都会在这里变红。
/// </remarks>
public sealed class WorkflowStoreMapperRoundTripTests
{
    /// <summary>
    /// 定义模型转实体必须写满全部检索投影列，租户为空时按平台租户 0 落库。
    /// </summary>
    [Fact]
    public void DefinitionToEntity_ShouldFillEveryProjectionColumn()
    {
        var definition = WorkflowTestHelper.CreateDefinition();

        var entity = WorkflowStoreMapper.ToEntity(definition);

        Assert.Equal(100200300400500L, entity.BasicId);
        Assert.Equal("leave", entity.Code, StringComparer.Ordinal);
        Assert.Equal("请假流程", entity.Name, StringComparer.Ordinal);
        Assert.Equal(3, entity.Version);
        Assert.Equal("员工请假审批", entity.Description, StringComparer.Ordinal);
        Assert.Equal("hr", entity.Category, StringComparer.Ordinal);
        Assert.Equal(WorkflowDefinitionStatus.Published, entity.Status);
        Assert.True(entity.EnableCompensation);
        Assert.Equal(WorkflowTestHelper.PublishTime, entity.PublishTime);
        Assert.Equal(7L, entity.TenantId);
        Assert.False(string.IsNullOrWhiteSpace(entity.DefinitionJson));
    }

    /// <summary>
    /// 定义往返必须还原节点、连线、变量声明与扩展属性——这些字段只活在 JSON 真源里。
    /// </summary>
    [Fact]
    public void DefinitionRoundTrip_ShouldPreserveGraphAndVariables()
    {
        var definition = WorkflowTestHelper.CreateDefinition();

        var restored = WorkflowStoreMapper.ToModel(WorkflowStoreMapper.ToEntity(definition));

        Assert.Equal(definition.Id, restored.Id, StringComparer.Ordinal);
        Assert.Equal(definition.Code, restored.Code, StringComparer.Ordinal);
        Assert.Equal(definition.Name, restored.Name, StringComparer.Ordinal);
        Assert.Equal(definition.Version, restored.Version);
        Assert.Equal(definition.Description, restored.Description, StringComparer.Ordinal);
        Assert.Equal(definition.Category, restored.Category, StringComparer.Ordinal);
        Assert.Equal(definition.Status, restored.Status);
        Assert.Equal(definition.EnableCompensation, restored.EnableCompensation);
        Assert.Equal(definition.TenantId, restored.TenantId);
        Assert.Equal(definition.CreationTime, restored.CreationTime);
        Assert.Equal(definition.PublishTime, restored.PublishTime);
        Assert.Equal("{\"x\":1}", restored.ExtraProperties["layout"], StringComparer.Ordinal);

        Assert.Equal(2, restored.Nodes.Count);
        var start = restored.Nodes[0];
        Assert.Equal("start", start.Id, StringComparer.Ordinal);
        Assert.Equal("开始", start.Name, StringComparer.Ordinal);
        Assert.Equal("Start", start.ActivityType, StringComparer.Ordinal);
        Assert.Equal(120, start.TimeoutSeconds);
        Assert.True(start.ContinueOnError);
        Assert.Equal("approver", Assert.IsType<JsonElement>(start.Properties["assignee"]).GetString(), StringComparer.Ordinal);
        Assert.NotNull(start.RetryPolicy);
        Assert.Equal(3, start.RetryPolicy!.MaxAttempts);
        Assert.Equal(5, start.RetryPolicy.FirstDelaySeconds);
        Assert.Equal(1.5, start.RetryPolicy.BackoffFactor);
        Assert.Null(restored.Nodes[1].RetryPolicy);

        var transition = Assert.Single(restored.Transitions);
        Assert.Equal("t1", transition.Id, StringComparer.Ordinal);
        Assert.Equal("直连", transition.Name, StringComparer.Ordinal);
        Assert.Equal("start", transition.SourceNodeId, StringComparer.Ordinal);
        Assert.Equal("end", transition.TargetNodeId, StringComparer.Ordinal);
        Assert.Equal("days > 1", transition.Condition, StringComparer.Ordinal);
        Assert.Equal(5, transition.Priority);
        Assert.True(transition.IsDefault);

        var variable = Assert.Single(restored.Variables);
        Assert.Equal("days", variable.Name, StringComparer.Ordinal);
        Assert.Equal("int", variable.Type, StringComparer.Ordinal);
        Assert.True(variable.Required);
        Assert.Equal("请假天数", variable.Description, StringComparer.Ordinal);
        Assert.Equal(1, Assert.IsType<JsonElement>(variable.DefaultValue).GetInt32());
    }

    /// <summary>
    /// 定义 JSON 必须保持「camelCase 属性 + 枚举字符串」格式，前端设计器与导入导出依赖该文本约定。
    /// </summary>
    [Fact]
    public void DefinitionJson_ShouldUseCamelCasePropertiesAndStringEnum()
    {
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition());

        Assert.Contains("\"enableCompensation\"", entity.DefinitionJson, StringComparison.Ordinal);
        Assert.Contains("\"status\": \"published\"", entity.DefinitionJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"EnableCompensation\"", entity.DefinitionJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// 平台级定义（TenantId 为空）投影列必须落 0，而 JSON 真源保留 null——两侧口径不同且都要稳定。
    /// </summary>
    [Fact]
    public void DefinitionRoundTrip_NullTenant_ShouldProjectZeroAndKeepNullInJson()
    {
        var definition = WorkflowTestHelper.CreateDefinition(tenantId: null);

        var entity = WorkflowStoreMapper.ToEntity(definition);
        var restored = WorkflowStoreMapper.ToModel(entity);

        Assert.Equal(0L, entity.TenantId);
        Assert.Null(restored.TenantId);
    }

    /// <summary>
    /// 更新走「加载现有实体、只覆盖业务列」，主键与审计列必须原样保全，否则乐观锁与创建痕迹被覆盖。
    /// </summary>
    [Fact]
    public void DefinitionToEntity_WithExistingEntity_ShouldKeepKeyAndAuditColumns()
    {
        var existing = new SysWorkflowDefinition();
        WorkflowTestHelper.SetBasicId(existing, 999L);
        existing.CreatedTime = DateTimeOffset.UnixEpoch;
        existing.CreatedId = 42L;
        existing.RowVersion = 8L;
        existing.Code = "old";

        var updated = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition(), existing);

        Assert.Same(existing, updated);
        Assert.Equal(999L, updated.BasicId);
        Assert.Equal(DateTimeOffset.UnixEpoch, updated.CreatedTime);
        Assert.Equal(42L, updated.CreatedId);
        Assert.Equal(8L, updated.RowVersion);
        Assert.Equal("leave", updated.Code, StringComparer.Ordinal);
    }

    /// <summary>
    /// 定义 JSON 真源损坏时必须抛工作流异常，而不是让 JsonException 穿透成 500。
    /// </summary>
    /// <param name="brokenJson">损坏的 JSON 真源。</param>
    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("not-json")]
    public void DefinitionToModel_BrokenJson_ShouldThrowWorkflowException(string brokenJson)
    {
        var entity = new SysWorkflowDefinition { DefinitionJson = brokenJson };

        var exception = Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ToModel(entity));

        Assert.Contains("流程定义 JSON 非法", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 实例模型转实体必须写满全部检索投影列，含父实例主键的字符串转 long。
    /// </summary>
    [Fact]
    public void InstanceToEntity_ShouldFillEveryProjectionColumn()
    {
        var instance = WorkflowTestHelper.CreateInstance();

        var entity = WorkflowStoreMapper.ToEntity(instance);

        Assert.Equal(900800700600500L, entity.BasicId);
        Assert.Equal(100200300400500L, entity.DefinitionId);
        Assert.Equal("leave", entity.DefinitionCode, StringComparer.Ordinal);
        Assert.Equal(3, entity.DefinitionVersion);
        Assert.Equal("张三的请假", entity.Name, StringComparer.Ordinal);
        Assert.Equal(WorkflowInstanceStatus.Faulted, entity.Status);
        Assert.Equal("ORDER-2024-0001", entity.CorrelationId, StringComparer.Ordinal);
        Assert.Equal("1001", entity.StarterId, StringComparer.Ordinal);
        Assert.Equal(111222333444555L, entity.ParentInstanceId);
        Assert.Equal(1, entity.Depth);
        Assert.Equal(7L, entity.TenantId);
        Assert.Equal(WorkflowTestHelper.CreationTime, entity.CreationTime);
        Assert.Equal(WorkflowTestHelper.StartTime, entity.StartTime);
        Assert.Equal(WorkflowTestHelper.EndTime, entity.EndTime);
        Assert.Equal("approve", entity.FaultNodeId, StringComparer.Ordinal);
        Assert.Equal("节点执行超时", entity.FaultMessage, StringComparer.Ordinal);
    }

    /// <summary>
    /// 实例往返必须还原变量、汇聚状态、父子链接与撤销原因——引擎崩溃恢复完全依赖这些字段。
    /// </summary>
    [Fact]
    public void InstanceRoundTrip_ShouldPreserveVariablesAndJoinStates()
    {
        var instance = WorkflowTestHelper.CreateInstance();

        var restored = WorkflowStoreMapper.ToModel(WorkflowStoreMapper.ToEntity(instance));

        Assert.Equal(instance.Id, restored.Id, StringComparer.Ordinal);
        Assert.Equal(instance.DefinitionId, restored.DefinitionId, StringComparer.Ordinal);
        Assert.Equal(instance.DefinitionCode, restored.DefinitionCode, StringComparer.Ordinal);
        Assert.Equal(instance.DefinitionVersion, restored.DefinitionVersion);
        Assert.Equal(instance.Name, restored.Name, StringComparer.Ordinal);
        Assert.Equal(instance.Status, restored.Status);
        Assert.Equal(instance.CorrelationId, restored.CorrelationId, StringComparer.Ordinal);
        Assert.Equal(instance.StarterId, restored.StarterId, StringComparer.Ordinal);
        Assert.Equal(instance.ParentInstanceId, restored.ParentInstanceId, StringComparer.Ordinal);
        Assert.Equal(instance.ParentNodeInstanceId, restored.ParentNodeInstanceId, StringComparer.Ordinal);
        Assert.Equal(instance.Depth, restored.Depth);
        Assert.Equal(instance.TenantId, restored.TenantId);
        Assert.Equal(instance.CreationTime, restored.CreationTime);
        Assert.Equal(instance.StartTime, restored.StartTime);
        Assert.Equal(instance.EndTime, restored.EndTime);
        Assert.Equal(instance.FaultMessage, restored.FaultMessage, StringComparer.Ordinal);
        Assert.Equal(instance.FaultNodeId, restored.FaultNodeId, StringComparer.Ordinal);
        Assert.Equal(instance.FaultNodeInstanceId, restored.FaultNodeInstanceId, StringComparer.Ordinal);
        Assert.Equal(instance.CancellationReason, restored.CancellationReason, StringComparer.Ordinal);

        Assert.Equal(2, restored.Variables.Count);
        Assert.Equal("3", Assert.IsType<JsonElement>(restored.Variables["days"]).GetString(), StringComparer.Ordinal);
        Assert.Equal("年假", Assert.IsType<JsonElement>(restored.Variables["reason"]).GetString(), StringComparer.Ordinal);

        var joinState = Assert.Single(restored.JoinStates);
        Assert.Equal("join1", joinState.Key, StringComparer.Ordinal);
        Assert.True(joinState.Value.Fired);
        Assert.Equal(
            new[] { "t1", "t2" },
            joinState.Value.ArrivedTransitionIds.OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// 顶层实例（无父实例）的父主键投影列必须是 NULL，不能被 ParseId 变成 0。
    /// </summary>
    [Fact]
    public void InstanceToEntity_WithoutParent_ShouldLeaveParentColumnNull()
    {
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance(parentInstanceId: null));

        Assert.Null(entity.ParentInstanceId);
    }

    /// <summary>
    /// 故障信息超过 2000 字符时投影列必须截断以匹配列长，JSON 真源仍须保留全文。
    /// </summary>
    [Fact]
    public void InstanceToEntity_OverlongFaultMessage_ShouldTruncateColumnButKeepJson()
    {
        var faultMessage = new string('x', 2500);

        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance(faultMessage: faultMessage));
        var restored = WorkflowStoreMapper.ToModel(entity);

        Assert.Equal(2000, entity.FaultMessage!.Length);
        Assert.Equal(2500, restored.FaultMessage!.Length);
    }

    /// <summary>
    /// 恰好 2000 字符的故障信息处于边界内，必须原样保留不被截断。
    /// </summary>
    [Fact]
    public void InstanceToEntity_FaultMessageAtBoundary_ShouldNotTruncate()
    {
        var faultMessage = new string('y', 2000);

        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance(faultMessage: faultMessage));

        Assert.Equal(2000, entity.FaultMessage!.Length);
    }

    /// <summary>
    /// 无故障信息时截断逻辑必须放行 null，不得变成空串。
    /// </summary>
    [Fact]
    public void InstanceToEntity_NullFaultMessage_ShouldStayNull()
    {
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance(faultMessage: null));

        Assert.Null(entity.FaultMessage);
    }

    /// <summary>
    /// 实例 JSON 真源用数值枚举（Web 默认选项，未挂字符串枚举转换器），改动会让历史行读不回来。
    /// </summary>
    [Fact]
    public void InstanceJson_ShouldSerializeStatusAsNumber()
    {
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance());

        Assert.Contains("\"status\":5", entity.InstanceJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// 节点实例模型转实体必须写满执行历史检索列。
    /// </summary>
    [Fact]
    public void NodeInstanceToEntity_ShouldFillEveryProjectionColumn()
    {
        var nodeInstance = WorkflowTestHelper.CreateNodeInstance();

        var entity = WorkflowStoreMapper.ToEntity(nodeInstance);

        Assert.Equal(300300300300300L, entity.BasicId);
        Assert.Equal(900800700600500L, entity.InstanceId);
        Assert.Equal("approve", entity.NodeId, StringComparer.Ordinal);
        Assert.Equal("UserTask", entity.ActivityType, StringComparer.Ordinal);
        Assert.Equal(WorkflowNodeInstanceStatus.Compensated, entity.Status);
        Assert.Equal(WorkflowTestHelper.StartTime, entity.StartTime);
        Assert.Equal(WorkflowTestHelper.EndTime, entity.EndTime);
        Assert.Equal(7L, entity.TenantId);
    }

    /// <summary>
    /// 节点实例往返必须还原名称、重试次数、输入输出与活动私有状态——它们全部只存在于 JSON 真源。
    /// </summary>
    [Fact]
    public void NodeInstanceRoundTrip_ShouldPreserveInputsOutputsAndState()
    {
        var nodeInstance = WorkflowTestHelper.CreateNodeInstance();

        var restored = WorkflowStoreMapper.ToModel(WorkflowStoreMapper.ToEntity(nodeInstance));

        Assert.Equal(nodeInstance.Id, restored.Id, StringComparer.Ordinal);
        Assert.Equal(nodeInstance.InstanceId, restored.InstanceId, StringComparer.Ordinal);
        Assert.Equal(nodeInstance.NodeId, restored.NodeId, StringComparer.Ordinal);
        Assert.Equal("部门审批", restored.Name, StringComparer.Ordinal);
        Assert.Equal(nodeInstance.ActivityType, restored.ActivityType, StringComparer.Ordinal);
        Assert.Equal(nodeInstance.Status, restored.Status);
        Assert.Equal(2, restored.TryCount);
        Assert.Equal(nodeInstance.StartTime, restored.StartTime);
        Assert.Equal(nodeInstance.EndTime, restored.EndTime);
        Assert.Equal("1001", Assert.IsType<JsonElement>(restored.Inputs["assignee"]).GetString(), StringComparer.Ordinal);
        Assert.Equal("approved", Assert.IsType<JsonElement>(restored.Outputs["outcome"]).GetString(), StringComparer.Ordinal);
        Assert.Equal("2", Assert.IsType<JsonElement>(restored.State["cursor"]).GetString(), StringComparer.Ordinal);
        Assert.Equal("审批人不存在", restored.FaultMessage, StringComparer.Ordinal);
        Assert.Equal(WorkflowTestHelper.EndTime, restored.CompensatedTime);
        Assert.Equal(7L, restored.TenantId);
    }

    /// <summary>
    /// 书签模型转实体必须写满待办/信号/到期轮询三条检索路径依赖的列。
    /// </summary>
    [Fact]
    public void BookmarkToEntity_ShouldFillEveryProjectionColumn()
    {
        var bookmark = WorkflowTestHelper.CreateBookmark(dueTime: WorkflowTestHelper.DueTime);

        var entity = WorkflowStoreMapper.ToEntity(bookmark);

        Assert.Equal(400400400400400L, entity.BasicId);
        Assert.Equal(900800700600500L, entity.InstanceId);
        Assert.Equal(300300300300300L, entity.NodeInstanceId);
        Assert.Equal(WorkflowBookmarkKinds.UserTask, entity.Kind, StringComparer.Ordinal);
        Assert.Equal("1001", entity.Key, StringComparer.Ordinal);
        Assert.Equal("ORDER-2024-0001", entity.CorrelationId, StringComparer.Ordinal);
        Assert.Equal(WorkflowTestHelper.DueTime, entity.DueTime);
        Assert.Equal(WorkflowTestHelper.CreationTime, entity.CreationTime);
        Assert.Equal(7L, entity.TenantId);
    }

    /// <summary>
    /// 书签往返必须还原节点标识与附加载荷——节点标识没有投影列，丢了待办详情就打不开。
    /// </summary>
    [Fact]
    public void BookmarkRoundTrip_ShouldPreserveNodeIdAndPayload()
    {
        var bookmark = WorkflowTestHelper.CreateBookmark();

        var restored = WorkflowStoreMapper.ToModel(WorkflowStoreMapper.ToEntity(bookmark));

        Assert.Equal(bookmark.Id, restored.Id, StringComparer.Ordinal);
        Assert.Equal(bookmark.InstanceId, restored.InstanceId, StringComparer.Ordinal);
        Assert.Equal("approve", restored.NodeId, StringComparer.Ordinal);
        Assert.Equal(bookmark.NodeInstanceId, restored.NodeInstanceId, StringComparer.Ordinal);
        Assert.Equal(bookmark.Kind, restored.Kind, StringComparer.Ordinal);
        Assert.Equal(bookmark.Key, restored.Key, StringComparer.Ordinal);
        Assert.Equal(bookmark.CorrelationId, restored.CorrelationId, StringComparer.Ordinal);
        Assert.Equal(bookmark.CreationTime, restored.CreationTime);
        Assert.Equal(bookmark.TenantId, restored.TenantId);
        Assert.Equal("部门审批", Assert.IsType<JsonElement>(restored.Payload["title"]).GetString(), StringComparer.Ordinal);
        Assert.Equal("leave-form", Assert.IsType<JsonElement>(restored.Payload["form"]).GetString(), StringComparer.Ordinal);
    }

    /// <summary>
    /// 非按键检索的书签（Key 为空）必须允许索引键列为 NULL，且到期时间保持为空。
    /// </summary>
    [Fact]
    public void BookmarkToEntity_WithoutKey_ShouldAllowNullKeyAndDueTime()
    {
        var entity = WorkflowStoreMapper.ToEntity(
            WorkflowTestHelper.CreateBookmark(kind: WorkflowBookmarkKinds.SubWorkflow, key: null));

        Assert.Null(entity.Key);
        Assert.Null(entity.DueTime);
        Assert.Equal(WorkflowBookmarkKinds.SubWorkflow, entity.Kind, StringComparer.Ordinal);
    }

    /// <summary>
    /// 运行时三类实体的 JSON 真源为 null 字面量时必须抛出带主键的工作流异常，而不是返回 null 引用。
    /// </summary>
    [Fact]
    public void RuntimeToModel_NullJsonLiteral_ShouldThrowWorkflowExceptionWithKey()
    {
        var instance = new SysWorkflowInstance(11L) { InstanceJson = "null" };
        var nodeInstance = new SysWorkflowNodeInstance(22L) { NodeInstanceJson = "null" };
        var bookmark = new SysWorkflowBookmark(33L) { BookmarkJson = "null" };

        var instanceException = Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ToModel(instance));
        var nodeException = Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ToModel(nodeInstance));
        var bookmarkException = Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ToModel(bookmark));

        Assert.Contains("实例 11 的 JSON 真源为空", instanceException.Message, StringComparison.Ordinal);
        Assert.Contains("节点实例 22 的 JSON 真源为空", nodeException.Message, StringComparison.Ordinal);
        Assert.Contains("书签 33 的 JSON 真源为空", bookmarkException.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 模型上任一标识字段非雪花数值时必须整体拒绝映射，避免脏标识被静默写成 0 号主键。
    /// </summary>
    [Fact]
    public void ToEntity_WithDirtyRelatedId_ShouldThrowWorkflowException()
    {
        var instance = WorkflowTestHelper.CreateInstance();
        instance.DefinitionId = "not-a-number";
        var nodeInstance = WorkflowTestHelper.CreateNodeInstance();
        nodeInstance.InstanceId = string.Empty;
        var bookmark = WorkflowTestHelper.CreateBookmark();
        bookmark.NodeInstanceId = "abc";

        Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ToEntity(instance));
        Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ToEntity(nodeInstance));
        Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ToEntity(bookmark));
    }

    /// <summary>
    /// 三个状态枚举的数值取值必须稳定：它们直接以 int 落库，改数值等于把历史行的状态整体改写。
    /// </summary>
    [Fact]
    public void StatusEnums_ShouldKeepPersistedNumericValues()
    {
        Assert.Equal(0, (int)WorkflowDefinitionStatus.Draft);
        Assert.Equal(1, (int)WorkflowDefinitionStatus.Published);
        Assert.Equal(2, (int)WorkflowDefinitionStatus.Disabled);
        Assert.Equal(3, (int)WorkflowDefinitionStatus.Archived);

        Assert.Equal(1, (int)WorkflowInstanceStatus.Running);
        Assert.Equal(2, (int)WorkflowInstanceStatus.Suspended);
        Assert.Equal(3, (int)WorkflowInstanceStatus.Completed);
        Assert.Equal(4, (int)WorkflowInstanceStatus.Canceled);
        Assert.Equal(5, (int)WorkflowInstanceStatus.Faulted);
        Assert.Equal(6, (int)WorkflowInstanceStatus.Terminated);

        Assert.Equal(1, (int)WorkflowNodeInstanceStatus.Running);
        Assert.Equal(2, (int)WorkflowNodeInstanceStatus.Suspended);
        Assert.Equal(3, (int)WorkflowNodeInstanceStatus.Completed);
        Assert.Equal(4, (int)WorkflowNodeInstanceStatus.Canceled);
        Assert.Equal(5, (int)WorkflowNodeInstanceStatus.Faulted);
        Assert.Equal(6, (int)WorkflowNodeInstanceStatus.Compensated);
    }

    /// <summary>
    /// 全部实例状态与节点实例状态都必须能原样往返，避免新增枚举值时映射静默降级。
    /// </summary>
    /// <param name="status">被往返的实例状态。</param>
    [Theory]
    [InlineData(WorkflowInstanceStatus.Running)]
    [InlineData(WorkflowInstanceStatus.Suspended)]
    [InlineData(WorkflowInstanceStatus.Completed)]
    [InlineData(WorkflowInstanceStatus.Canceled)]
    [InlineData(WorkflowInstanceStatus.Faulted)]
    [InlineData(WorkflowInstanceStatus.Terminated)]
    public void InstanceRoundTrip_EveryStatus_ShouldSurvive(WorkflowInstanceStatus status)
    {
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance(status: status));

        Assert.Equal(status, entity.Status);
        Assert.Equal(status, WorkflowStoreMapper.ToModel(entity).Status);
    }

    /// <summary>
    /// 全部定义状态都必须能原样往返（JSON 侧是 camelCase 字符串枚举，投影列是数值）。
    /// </summary>
    /// <param name="status">被往返的定义状态。</param>
    [Theory]
    [InlineData(WorkflowDefinitionStatus.Draft)]
    [InlineData(WorkflowDefinitionStatus.Published)]
    [InlineData(WorkflowDefinitionStatus.Disabled)]
    [InlineData(WorkflowDefinitionStatus.Archived)]
    public void DefinitionRoundTrip_EveryStatus_ShouldSurvive(WorkflowDefinitionStatus status)
    {
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition(status: status));

        Assert.Equal(status, entity.Status);
        Assert.Equal(status, WorkflowStoreMapper.ToModel(entity).Status);
    }
}
