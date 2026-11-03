// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Values;

namespace XiHan.BasicApp.Workflow.Domain.Services;

/// <summary>
/// 发出去的通知与写进库里的文案（作用域）
/// </summary>
/// <remarks>
/// 按区域取消息目录里的文案（4.x 写死中文）：写进库里的按这次调用的区域，发给人的通知按收件人的界面语言（见 <see cref="Handlers.WorkflowNotifier"/>）；
/// 键都在 10 种区域里，取不到是缺陷，直接抛出
/// </remarks>
/// <param name="localizer">消息本地化</param>
public sealed class WorkflowTexts(IMessageLocalizer localizer)
{
    /// <summary>
    /// 按这次调用的区域取一条文案
    /// </summary>
    public string Render(InvocationContext context, string key, params (string Name, object? Value)[] arguments)
        => Render(LocaleOf(context), key, arguments);

    /// <summary>
    /// 这次调用的区域
    /// </summary>
    /// <remarks>
    /// 调用没带区域（如后台恢复流程）时为缺省区域
    /// </remarks>
    public string LocaleOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return localizer.Options.Resolve(context);
    }

    /// <summary>
    /// 把存下的语言与支持的区域协商
    /// </summary>
    public string Negotiate(string locale) => localizer.Options.Negotiate([locale]);

    /// <summary>
    /// 按指定区域取一条文案
    /// </summary>
    public string Render(string locale, string key, params (string Name, object? Value)[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        var values = arguments.ToDictionary(static a => a.Name, static a => a.Value, StringComparer.Ordinal);
        return localizer.Resolve(key, locale, values).TryGetValue(out var text)
            ? text.Text
            : throw new InvalidOperationException($"Workflow text '{key}' is missing for locale '{locale}'.");
    }
}

/// <summary>
/// 一次调用里查人名与定义（作用域）
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>WorkflowUserNameResolver</c> 与 <c>WorkflowNodeNameCache</c>：人名取用户目录的显示名（已删除的照常有名字），
/// 定义按 ID 在本次调用里只取一次；节点名取节点的显示名，没有时是节点 ID
/// </remarks>
/// <param name="users">用户目录</param>
/// <param name="catalog">定义目录</param>
public sealed class WorkflowLookup(IUserDirectory users, IWorkflowDefinitionCatalog catalog)
{
    private readonly Dictionary<string, Maybe<WorkflowDefinition>> _definitions = new(StringComparer.Ordinal);

    /// <summary>
    /// 一批引擎用户 ID 的显示名（不是用户 ID 的、查不到的不在结果里）
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>> NamesAsync(IEnumerable<string?> engineUsers, CancellationToken cancellationToken)
    {
        var ids = engineUsers.Select(WorkflowIds.UserOf).OfType<Guid>().Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var found = await users.FindAsync(ids, cancellationToken).ConfigureAwait(false);
        return found.Values.ToDictionary(static u => WorkflowIds.ToEngineUser(u.Id), static u => u.DisplayName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 实例所用的定义（不存在时为空）
    /// </summary>
    public async Task<Result<Maybe<WorkflowDefinition>>> DefinitionAsync(string definitionId, CancellationToken cancellationToken)
    {
        if (_definitions.TryGetValue(definitionId, out var cached))
        {
            return cached;
        }

        var found = await catalog.FindAsync(definitionId, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var maybe))
        {
            return found.Error!;
        }

        _definitions[definitionId] = maybe;
        return maybe;
    }

    /// <summary>
    /// 节点名（定义或节点不存在时是节点 ID）
    /// </summary>
    public async Task<string> NodeNameAsync(string definitionId, string nodeId, CancellationToken cancellationToken)
    {
        var definition = await DefinitionAsync(definitionId, cancellationToken).ConfigureAwait(false);
        return definition.TryGetValue(out var maybe) && maybe.TryGetValue(out var value) && value.FindNode(nodeId).TryGetValue(out var node) ? node.Title : nodeId;
    }

    /// <summary>
    /// 用户任务节点允许的结论（节点属性 <c>AllowedOutcomes</c>，没写时为空）
    /// </summary>
    public static IReadOnlyList<string> AllowedOutcomes(WorkflowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Properties.TryGetPropertyValue("AllowedOutcomes", out var raw) ? WorkflowValues.ToStringList(raw) : [];
    }
}

/// <summary>
/// 写审批记录时的任务快照
/// </summary>
/// <param name="TaskId">任务</param>
/// <param name="InstanceId">实例</param>
/// <param name="DefinitionId">定义（引擎 ID）</param>
/// <param name="DefinitionName">定义名</param>
/// <param name="InstanceTitle">实例标题</param>
/// <param name="NodeId">节点</param>
/// <param name="Title">任务标题</param>
/// <param name="CorrelationId">关联 ID</param>
public sealed record WorkflowTaskSnapshot(Guid TaskId, Guid InstanceId, string DefinitionId, string DefinitionName, string InstanceTitle, string NodeId, string Title, string? CorrelationId);

/// <summary>
/// 审批记录的写入（作用域）
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>WorkflowTaskRecordWriter</c>：每个归属用户一行，名字按写入时取快照，过长的截断。写在调用方的事务里，与办理同进退
/// </remarks>
/// <param name="rows">按侧读写</param>
/// <param name="ids">ID 生成</param>
/// <param name="lookup">人名与定义</param>
public sealed class WorkflowTaskRecords(SideRows rows, IIdGenerator<Guid> ids, WorkflowLookup lookup)
{
    /// <summary>
    /// 写记录
    /// </summary>
    /// <param name="side">侧</param>
    /// <param name="task">任务快照</param>
    /// <param name="owners">归属用户（办理人或被抄送人）</param>
    /// <param name="action">动作</param>
    /// <param name="outcome">办理的结论</param>
    /// <param name="comment">意见</param>
    /// <param name="targets">转办与加签的目标</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AppendAsync(string side, WorkflowTaskSnapshot task, IEnumerable<Guid> owners, WorkflowTaskRecordAction action, string? outcome, string? comment, IEnumerable<Guid>? targets, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        var users = owners.Distinct().ToList();
        if (users.Count == 0)
        {
            return;
        }

        var nodeName = await lookup.NodeNameAsync(task.DefinitionId, task.NodeId, cancellationToken).ConfigureAwait(false);
        var targetText = targets is null ? null : string.Join(',', targets.Distinct());
        foreach (var user in users)
        {
            await rows.AppendAsync(
                new SysWorkflowTaskRecord
                {
                    Id = ids.NewId(),
                    TenantId = side,
                    InstanceId = task.InstanceId,
                    TaskId = task.TaskId,
                    NodeId = task.NodeId,
                    NodeName = Clip(nodeName, 200),
                    DefinitionName = task.DefinitionName,
                    InstanceTitle = Clip(task.InstanceTitle, 200),
                    Title = Clip(task.Title, 500),
                    CorrelationId = task.CorrelationId,
                    UserId = user,
                    Action = action,
                    Outcome = outcome,
                    Comment = string.IsNullOrWhiteSpace(comment) ? null : Clip(comment.Trim(), 2000),
                    TargetUserIds = string.IsNullOrEmpty(targetText) ? null : Clip(targetText, 1000),
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 一个实例在一个节点上已经抄送过的人
    /// </summary>
    public async Task<IReadOnlySet<Guid>> CopiedAsync(string side, Guid instanceId, string nodeId, CancellationToken cancellationToken)
    {
        var copied = await rows.Records<SysWorkflowTaskRecord>(side)
            .Where(r => r.InstanceId == instanceId && r.NodeId == nodeId && r.Action == WorkflowTaskRecordAction.CarbonCopied)
            .Select(static r => r.UserId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return copied.ToHashSet();
    }

    /// <summary>
    /// 一个实例的全部记录（按时间）
    /// </summary>
    public async Task<IReadOnlyList<SysWorkflowTaskRecord>> OfInstanceAsync(string side, Guid instanceId, CancellationToken cancellationToken)
        => await rows.Records<SysWorkflowTaskRecord>(side)
            .Where(r => r.InstanceId == instanceId)
            .OrderBy(static r => r.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 一串目标用户
    /// </summary>
    public static IReadOnlyList<Guid> TargetsOf(SysWorkflowTaskRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return string.IsNullOrWhiteSpace(record.TargetUserIds)
            ? []
            : record.TargetUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(static t => Guid.TryParse(t, out var id) ? id : (Guid?)null).OfType<Guid>().ToList();
    }

    private static string Clip(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}

/// <summary>
/// 事件载荷里的文本
/// </summary>
internal static class WorkflowPayload
{
    public static string? Text(JsonObject? payload, string key)
        => payload is not null && payload.TryGetPropertyValue(key, out var value) && value is JsonValue scalar ? WorkflowValues.AsString(scalar) : null;
}
