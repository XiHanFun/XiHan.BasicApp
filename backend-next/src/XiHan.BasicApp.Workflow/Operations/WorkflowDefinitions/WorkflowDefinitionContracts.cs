// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Workflow.Abstractions.Activities;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Serialization;

namespace XiHan.BasicApp.Workflow.Operations.WorkflowDefinitions;

/// <summary>
/// 流程定义（列表的一行）。
/// </summary>
/// <param name="Id">定义 ID。</param>
/// <param name="Name">名字（同名的各版本共用；关联审批业务时即业务码）。</param>
/// <param name="Version">版本。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Description">说明。</param>
/// <param name="Category">分类。</param>
/// <param name="Status">状态。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="PublishedAt">发布时间。</param>
/// <param name="BusinessNameKey">关联的审批业务的显示名文案键，没有关联时为空。</param>
public sealed record WorkflowDefinitionResponse(
    Guid Id,
    string Name,
    int Version,
    string? DisplayName,
    string? Description,
    string? Category,
    WorkflowDefinitionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    string? BusinessNameKey)
{
    /// <summary>
    /// 从框架的定义摘要投影。
    /// </summary>
    /// <param name="summary">定义摘要。</param>
    /// <param name="business">登记表。</param>
    /// <returns>响应。</returns>
    public static WorkflowDefinitionResponse From(WorkflowDefinitionSummary summary, WorkflowBusinessRegistry business)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(business);
        return new(WorkflowIds.ToContract(summary.Id), summary.Name.Value, summary.Version, summary.DisplayName, summary.Description, summary.Category, summary.Status, summary.CreatedAt, summary.PublishedAt, business.Find(summary.Name.Value)?.Descriptor.NameKey);
    }
}

/// <summary>
/// 流程定义详情。
/// </summary>
/// <param name="Id">定义 ID。</param>
/// <param name="Name">名字。</param>
/// <param name="Version">版本。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Description">说明。</param>
/// <param name="Category">分类。</param>
/// <param name="Status">状态。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="PublishedAt">发布时间。</param>
/// <param name="BusinessNameKey">关联的审批业务的显示名文案键。</param>
/// <param name="Document">整份定义（WorkflowJson）。</param>
public sealed record WorkflowDefinitionDetailResponse(
    Guid Id,
    string Name,
    int Version,
    string? DisplayName,
    string? Description,
    string? Category,
    WorkflowDefinitionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    string? BusinessNameKey,
    string Document)
{
    /// <summary>
    /// 从框架的定义投影。
    /// </summary>
    /// <param name="definition">定义。</param>
    /// <param name="business">登记表。</param>
    /// <returns>响应。</returns>
    public static WorkflowDefinitionDetailResponse From(WorkflowDefinition definition, WorkflowBusinessRegistry business)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(business);
        return new(WorkflowIds.ToContract(definition.Id), definition.Name.Value, definition.Version, definition.DisplayName, definition.Description, definition.Category, definition.Status, definition.CreatedAt, definition.PublishedAt, business.Find(definition.Name.Value)?.Descriptor.NameKey, WorkflowJson.Serialize(definition));
    }
}

/// <summary>
/// 取一个流程定义。
/// </summary>
/// <param name="Id">定义 ID。</param>
public sealed record GetWorkflowDefinitionRequest(Guid Id);

/// <summary>
/// 新建流程定义草稿。
/// </summary>
/// <remarks>
/// 名字、显示名、分类与说明都在定义文档里（WorkflowJson）；版本由框架按同名的最大版本加一，状态为草稿。
/// </remarks>
/// <param name="Document">整份定义（WorkflowJson）。</param>
public sealed record CreateWorkflowDefinitionRequest([property: Required, StringLength(WorkflowDefinitionLimits.MaxDocumentLength, MinimumLength = 2)] string Document);

/// <summary>
/// 改流程定义草稿。
/// </summary>
/// <remarks>
/// 只有草稿能改；名字与版本沿用已存的（文档里的不生效）。
/// </remarks>
/// <param name="Id">定义 ID。</param>
/// <param name="Document">整份定义（WorkflowJson）。</param>
public sealed record UpdateWorkflowDefinitionRequest(Guid Id, [property: Required, StringLength(WorkflowDefinitionLimits.MaxDocumentLength, MinimumLength = 2)] string Document);

/// <summary>
/// 发布流程定义。
/// </summary>
/// <param name="Id">定义 ID。</param>
public sealed record PublishWorkflowDefinitionRequest(Guid Id);

/// <summary>
/// 按名字开新版本。
/// </summary>
/// <param name="Name">定义名。</param>
public sealed record NewWorkflowDefinitionVersionRequest([property: Required, StringLength(128, MinimumLength = 1), RegularExpression("^[a-z0-9][a-z0-9._-]{0,127}$")] string Name);

/// <summary>
/// 一批流程定义。
/// </summary>
/// <param name="Ids">定义 ID。</param>
public sealed record WorkflowDefinitionIdsRequest([property: Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// 列出审批业务。
/// </summary>
public sealed record ListWorkflowBusinessTypesRequest;

/// <summary>
/// 审批业务目录。
/// </summary>
/// <param name="Items">已登记的审批业务。</param>
public sealed record WorkflowBusinessTypesResponse(IReadOnlyList<WorkflowBusinessTypeResponse> Items);

/// <summary>
/// 一个审批业务。
/// </summary>
/// <param name="Code">业务码（流程定义名）。</param>
/// <param name="NameKey">显示名的文案键。</param>
/// <param name="FormComponent">单据组件路径。</param>
/// <param name="Variables">发起时带的变量。</param>
/// <param name="Outcomes">允许的结论。</param>
public sealed record WorkflowBusinessTypeResponse(string Code, string NameKey, string FormComponent, IReadOnlyList<WorkflowBusinessVariableResponse> Variables, IReadOnlyList<string> Outcomes)
{
    /// <summary>
    /// 从业务描述投影。
    /// </summary>
    /// <param name="descriptor">业务描述。</param>
    /// <returns>响应。</returns>
    public static WorkflowBusinessTypeResponse From(WorkflowBusinessDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return new(descriptor.Code, descriptor.NameKey, descriptor.FormComponent, descriptor.Variables.Select(static v => new WorkflowBusinessVariableResponse(v.Name, v.LabelKey, v.ValueType)).ToList(), WorkflowBusinessRegistry.OutcomesOf(descriptor));
    }
}

/// <summary>
/// 审批业务的一个变量。
/// </summary>
/// <param name="Name">变量名。</param>
/// <param name="LabelKey">显示名的文案键。</param>
/// <param name="ValueType">值类型。</param>
public sealed record WorkflowBusinessVariableResponse(string Name, string LabelKey, string ValueType);

/// <summary>
/// 列出设计器可用的活动类型。
/// </summary>
public sealed record ListWorkflowActivityTypesRequest;

/// <summary>
/// 活动类型目录。
/// </summary>
/// <param name="Items">活动类型。</param>
public sealed record WorkflowActivityTypesResponse(IReadOnlyList<WorkflowActivityTypeResponse> Items);

/// <summary>
/// 一个活动类型。
/// </summary>
/// <param name="Type">类型名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Category">分类。</param>
/// <param name="Inputs">输入属性。</param>
public sealed record WorkflowActivityTypeResponse(string Type, string DisplayName, string? Category, IReadOnlyList<WorkflowActivityInputResponse> Inputs)
{
    /// <summary>
    /// 从框架的活动描述投影。
    /// </summary>
    /// <param name="descriptor">活动描述。</param>
    /// <returns>响应。</returns>
    public static WorkflowActivityTypeResponse From(ActivityDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return new(descriptor.Type, descriptor.DisplayName, descriptor.Category, descriptor.Inputs.Select(static i => new WorkflowActivityInputResponse(i.Name, i.Kind, i.Required)).ToList());
    }
}

/// <summary>
/// 活动的一个输入属性。
/// </summary>
/// <param name="Name">属性名。</param>
/// <param name="Kind">取值种类。</param>
/// <param name="Required">是否必填。</param>
public sealed record WorkflowActivityInputResponse(string Name, string Kind, bool Required);

/// <summary>
/// 流程定义的限制。
/// </summary>
public static class WorkflowDefinitionLimits
{
    /// <summary>
    /// 定义文档的最大长度（字符）。
    /// </summary>
    public const int MaxDocumentLength = 1_000_000;

    /// <summary>
    /// 设计器能放的活动类型。
    /// </summary>
    /// <remarks>
    /// 照 4.x（W4、W5）：4.x 的 17 种去掉 Script（框架 5 没有、不回灌）；框架 5 新增的事件网关、补偿与外部任务不放。
    /// </remarks>
    public static IReadOnlySet<string> ActivityTypes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "Start", "End", "Terminate", "SetVariable", "Decision", "Parallel", "Join", "Delay", "UserTask", "Http", "PublishEvent", "WaitSignal", "SubWorkflow", "ForEach", "Log", "Fault",
    };
}
