// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.Mappers;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 应用层映射器的字段完整性与「不该外泄的字段确实没外泄」测试。
/// </summary>
/// <remarks>
/// 映射器手写逐字段赋值，漏字段不会有任何编译提示，只会让列表页某一列永远空着。
/// 反向也一样重要：书签载荷、节点输入、待办受理人与表单数据都是刻意不进列表 DTO 的——
/// 前者是列表体积问题，后两者是"列表不暴露他人受理信息"的边界问题。
/// </remarks>
public sealed class WorkflowApplicationMapperTests
{
    /// <summary>
    /// 实例实体转列表项必须逐字段带出全部投影列，父实例主键保持 long? 不做二次解析。
    /// </summary>
    [Fact]
    public void ToListItemDto_FromInstanceEntity_ShouldMapEveryProjectionColumn()
    {
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance());

        var dto = WorkflowApplicationMapper.ToListItemDto(entity);

        Assert.Equal(900800700600500L, dto.BasicId);
        Assert.Equal("leave", dto.DefinitionCode, StringComparer.Ordinal);
        Assert.Equal(3, dto.DefinitionVersion);
        Assert.Equal("张三的请假", dto.Name, StringComparer.Ordinal);
        Assert.Equal(WorkflowInstanceStatus.Faulted, dto.Status);
        Assert.Equal("ORDER-2024-0001", dto.CorrelationId, StringComparer.Ordinal);
        Assert.Equal("1001", dto.StarterId, StringComparer.Ordinal);
        Assert.Equal(111222333444555L, dto.ParentInstanceId);
        Assert.Equal(1, dto.Depth);
        Assert.Equal(WorkflowTestHelper.CreationTime, dto.CreationTime);
        Assert.Equal(WorkflowTestHelper.StartTime, dto.StartTime);
        Assert.Equal(WorkflowTestHelper.EndTime, dto.EndTime);
        Assert.Equal("approve", dto.FaultNodeId, StringComparer.Ordinal);
        Assert.Equal("节点执行超时", dto.FaultMessage, StringComparer.Ordinal);
    }

    /// <summary>
    /// 定义实体的列表项与详情必须字段一致，唯一差别是详情多带 JSON 真源（列表不驮设计器全量图）。
    /// </summary>
    [Fact]
    public void DefinitionDtos_ListItemAndDetail_ShouldDifferOnlyByDefinitionJson()
    {
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition());
        entity.CreatedTime = DateTimeOffset.UnixEpoch;

        var listItem = WorkflowApplicationMapper.ToListItemDto(entity);
        var detail = WorkflowApplicationMapper.ToDetailDto(entity);

        Assert.Equal(listItem.BasicId, detail.BasicId);
        Assert.Equal(listItem.Code, detail.Code, StringComparer.Ordinal);
        Assert.Equal(listItem.Name, detail.Name, StringComparer.Ordinal);
        Assert.Equal(listItem.Version, detail.Version);
        Assert.Equal(listItem.Description, detail.Description, StringComparer.Ordinal);
        Assert.Equal(listItem.Category, detail.Category, StringComparer.Ordinal);
        Assert.Equal(listItem.Status, detail.Status);
        Assert.Equal(listItem.EnableCompensation, detail.EnableCompensation);
        Assert.Equal(listItem.PublishTime, detail.PublishTime);
        Assert.Equal(listItem.CreatedTime, detail.CreatedTime);
        Assert.Equal(entity.DefinitionJson, detail.DefinitionJson, StringComparer.Ordinal);
        Assert.False(listItem is WorkflowDefinitionDetailDto, "列表项不得是详情 DTO，否则会随手带上整份定义 JSON。");
    }

    /// <summary>
    /// 定义模型转详情必须用框架序列化器重新生成 JSON（生命周期操作的返回没有实体可取真源）。
    /// </summary>
    [Fact]
    public void ToDetailDto_FromDefinitionModel_ShouldRegenerateJsonAndUseCreationTime()
    {
        var definition = WorkflowTestHelper.CreateDefinition(status: WorkflowDefinitionStatus.Disabled);

        var detail = WorkflowApplicationMapper.ToDetailDto(definition);

        Assert.Equal(100200300400500L, detail.BasicId);
        Assert.Equal(WorkflowDefinitionStatus.Disabled, detail.Status);
        Assert.Equal(WorkflowTestHelper.CreationTime, detail.CreatedTime);
        Assert.Contains("\"status\": \"disabled\"", detail.DefinitionJson, StringComparison.Ordinal);
        Assert.Contains("\"nodes\"", detail.DefinitionJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// 实例详情在没有执行历史与等待点时必须给出空集合而不是 null，前端可直接遍历。
    /// </summary>
    [Fact]
    public void ToDetailDto_FromInstanceModel_WithoutHistory_ShouldReturnEmptyCollections()
    {
        var detail = WorkflowApplicationMapper.ToDetailDto(WorkflowTestHelper.CreateInstance(), [], []);

        Assert.Empty(detail.NodeInstances);
        Assert.Empty(detail.PendingBookmarks);
        Assert.Contains("\"days\": \"3\"", detail.VariablesJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// 变量 JSON 走 Web 默认编码器，非 ASCII 字符会被转义成 \uXXXX。
    /// </summary>
    /// <remarks>
    /// 这是刻意锁定的当前行为：前端 JSON.parse 后可正常还原中文，
    /// 但若有人直接把该文本当展示串塞进页面（而不是先解析），就会看到一串转义码。
    /// 换成 UnsafeRelaxedJsonEscaping 会改变全部历史响应的文本形态，因此在这里钉住。
    /// </remarks>
    [Fact]
    public void ToDetailDto_VariablesJson_ShouldEscapeNonAsciiCharacters()
    {
        var detail = WorkflowApplicationMapper.ToDetailDto(WorkflowTestHelper.CreateInstance(), [], []);

        Assert.DoesNotContain("年假", detail.VariablesJson, StringComparison.Ordinal);
        Assert.Contains("\\u5E74\\u5047", detail.VariablesJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// 节点实例 DTO 只暴露输出，不得暴露输入与活动私有状态（前者含受理人指派，后者是引擎内部游标）。
    /// </summary>
    [Fact]
    public void WorkflowNodeInstanceDto_ShouldNotExposeInputsOrPrivateState()
    {
        Assert.Null(typeof(WorkflowNodeInstanceDto).GetProperty("Inputs"));
        Assert.Null(typeof(WorkflowNodeInstanceDto).GetProperty("State"));
        Assert.NotNull(typeof(WorkflowNodeInstanceDto).GetProperty(nameof(WorkflowNodeInstanceDto.OutputsJson)));
    }

    /// <summary>
    /// 书签 DTO 只暴露等待点的定位信息，不得外泄附加载荷与业务相关性。
    /// </summary>
    [Fact]
    public void WorkflowBookmarkDto_ShouldNotExposePayloadOrCorrelation()
    {
        var dto = WorkflowApplicationMapper.ToBookmarkDto(
            WorkflowTestHelper.CreateBookmark(dueTime: WorkflowTestHelper.DueTime));

        Assert.Null(typeof(WorkflowBookmarkDto).GetProperty("Payload"));
        Assert.Null(typeof(WorkflowBookmarkDto).GetProperty("CorrelationId"));
        Assert.Equal("400400400400400", dto.Id, StringComparer.Ordinal);
        Assert.Equal("approve", dto.NodeId, StringComparer.Ordinal);
        Assert.Equal("1001", dto.Key, StringComparer.Ordinal);
        Assert.Equal(WorkflowTestHelper.DueTime, dto.DueTime);
        Assert.Equal(WorkflowTestHelper.CreationTime, dto.CreationTime);
    }

    /// <summary>
    /// 待办列表项不得暴露受理人标识、节点实例标识与表单数据——列表只用于点开详情。
    /// </summary>
    [Fact]
    public void WorkflowTodoListItemDto_ShouldNotExposeAssigneeOrFormData()
    {
        var dto = WorkflowApplicationMapper.ToTodoListItemDto(WorkflowTestHelper.CreateUserTask());

        Assert.Null(typeof(WorkflowTodoListItemDto).GetProperty("AssigneeId"));
        Assert.Null(typeof(WorkflowTodoListItemDto).GetProperty("FormData"));
        Assert.Null(typeof(WorkflowTodoListItemDto).GetProperty("NodeInstanceId"));
        Assert.Null(typeof(WorkflowTodoListItemDto).GetProperty("TenantId"));
        Assert.Equal("400400400400400", dto.TaskId, StringComparer.Ordinal);
        Assert.Equal("部门审批", dto.Title, StringComparer.Ordinal);
    }

    /// <summary>
    /// 定义与实例的列表项 DTO 都不得暴露租户标识，租户隔离在服务端完成、无需回传给前端。
    /// </summary>
    [Fact]
    public void ListItemDtos_ShouldNotExposeTenantId()
    {
        Assert.Null(typeof(WorkflowDefinitionListItemDto).GetProperty("TenantId"));
        Assert.Null(typeof(WorkflowInstanceListItemDto).GetProperty("TenantId"));
        Assert.Null(typeof(WorkflowDefinitionDetailDto).GetProperty("TenantId"));
        Assert.Null(typeof(WorkflowInstanceDetailDto).GetProperty("TenantId"));
    }

    /// <summary>
    /// 定义详情继承自列表项：前端一套列渲染两处数据，继承关系断掉会让详情页字段整体错位。
    /// </summary>
    [Fact]
    public void DetailDtos_ShouldExtendListItemDtos()
    {
        Assert.True(typeof(WorkflowDefinitionDetailDto).IsAssignableTo(typeof(WorkflowDefinitionListItemDto)));
        Assert.True(typeof(WorkflowInstanceDetailDto).IsAssignableTo(typeof(WorkflowInstanceListItemDto)));
    }
}
