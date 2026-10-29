// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Domain.Services;

/// <summary>
/// 模板内容（新建与修改共用；编码与渠道只在新建时给）。
/// </summary>
/// <param name="Name">名称。</param>
/// <param name="Subject">主题。</param>
/// <param name="Content">正文。</param>
/// <param name="IsHtml">正文是 HTML。</param>
/// <param name="Description">说明。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record TemplateDraft(string Name, string? Subject, string Content, bool IsHtml, string? Description, bool IsEnabled, int Order, string? Remark);

/// <summary>
/// 消息模板的规则：编码在同一侧、同一渠道内唯一，编码与渠道建好不改；主题与正文保存前做语法检查；取模板时当前一侧启用的优先，没有取平台启用的。
/// </summary>
/// <param name="templates">模板仓储。</param>
/// <param name="ids">标识生成器。</param>
public sealed class MessageTemplateService(IRepository<SysMessageTemplate, Guid> templates, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 取当前一侧的模板。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="id">模板 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模板；不存在即 <c>NOTIFICATION_NOTFOUND_003</c>。</returns>
    public async Task<Result<SysMessageTemplate>> FindAsync(string owner, Guid id, CancellationToken cancellationToken)
    {
        var found = await templates.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var template) && string.Equals(template.OwnerTenantId, owner, StringComparison.Ordinal)
            ? template
            : Error.NotFound(NotificationErrors.TemplateNotFound, NotificationMessages.TemplateNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 发送时取模板：当前一侧启用的同编码模板，没有取平台启用的。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="channel">渠道。</param>
    /// <param name="code">编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模板；都没有为 <see langword="null"/>。</returns>
    public async Task<SysMessageTemplate?> ResolveAsync(string owner, TemplateChannel channel, string code, CancellationToken cancellationToken)
    {
        var platform = TenantIds.Platform;
        var rows = await templates.ListAsync(EntityQuery.All<SysMessageTemplate>().Where(new Specification<SysMessageTemplate>(t => (t.OwnerTenantId == owner || t.OwnerTenantId == platform) && t.Channel == channel && t.Code == code && t.IsEnabled)), cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault(t => string.Equals(t.OwnerTenantId, owner, StringComparison.Ordinal)) ?? (rows.Count > 0 ? rows[0] : null);
    }

    /// <summary>
    /// 这一侧、这个渠道是否已有这个编码的模板（不论启用与否）。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="channel">渠道。</param>
    /// <param name="code">编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否已有。</returns>
    public async Task<bool> ExistsAsync(string owner, TemplateChannel channel, string code, CancellationToken cancellationToken)
    {
        var taken = await templates.ListAsync(EntityQuery.All<SysMessageTemplate>().Where(new Specification<SysMessageTemplate>(t => t.OwnerTenantId == owner && t.Channel == channel && t.Code == code)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return taken.Count > 0;
    }

    /// <summary>
    /// 新建。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="code">编码。</param>
    /// <param name="channel">渠道。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新模板。</returns>
    public async Task<Result<SysMessageTemplate>> CreateAsync(string owner, string code, TemplateChannel channel, TemplateDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var valid = Validate(draft);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        if (await ExistsAsync(owner, channel, code, cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(NotificationErrors.TemplateCodeTaken, NotificationMessages.TemplateCodeTaken, MessageArguments.Of(("code", code)));
        }

        var template = new SysMessageTemplate { Id = ids.NewId(), OwnerTenantId = owner, Code = code, Channel = channel };
        Apply(template, draft);
        var added = await templates.AddAsync(template, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : template;
    }

    /// <summary>
    /// 修改（编码与渠道不改）。
    /// </summary>
    /// <param name="template">模板。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> UpdateAsync(SysMessageTemplate template, TemplateDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(draft);
        var valid = Validate(draft);
        if (valid.IsFailure)
        {
            return valid;
        }

        Apply(template, draft);
        return await templates.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 批量删除当前一侧的模板。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="templateIds">模板 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteAsync(string owner, IReadOnlyList<Guid> templateIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(templateIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in templateIds.Distinct())
        {
            var found = await FindAsync(owner, id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var template))
            {
                failed.Add(new BatchFailure(id, NotificationErrors.TemplateNotFound));
                continue;
            }

            var removed = await templates.RemoveAsync(template, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
                continue;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }

    private static Result Validate(TemplateDraft draft)
    {
        foreach (var source in new[] { draft.Subject, draft.Content })
        {
            if (TemplateRenderer.SyntaxError(source) is { } error)
            {
                return Error.Validation(NotificationErrors.TemplateInvalid, NotificationMessages.TemplateInvalid, MessageArguments.Of(("detail", error)));
            }
        }

        return Result.Success();
    }

    private static void Apply(SysMessageTemplate template, TemplateDraft draft)
    {
        template.Name = draft.Name.Trim();
        template.Subject = string.IsNullOrWhiteSpace(draft.Subject) ? null : draft.Subject;
        template.Content = draft.Content;
        template.IsHtml = draft.IsHtml;
        template.Description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim();
        template.IsEnabled = draft.IsEnabled;
        template.Order = draft.Order;
        template.Remark = string.IsNullOrWhiteSpace(draft.Remark) ? null : draft.Remark.Trim();
    }
}
