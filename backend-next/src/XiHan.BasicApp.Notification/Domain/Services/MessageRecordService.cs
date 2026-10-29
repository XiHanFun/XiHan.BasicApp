// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Notification.Domain.Services;

/// <summary>
/// 发送记录的人工处理（邮件与短信同一套）：重发即改回排队中、清零尝试次数（排队中与发送中的逐项拒绝）；取消只对排队中的；删除是软删除（发送中的逐项拒绝）。
/// </summary>
/// <param name="signal">唤醒信号（重发后立即发）。</param>
public sealed class MessageRecordService(MessageDispatchSignal signal)
{
    /// <summary>
    /// 批量重发。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="records">记录仓储。</param>
    /// <param name="owner">侧。</param>
    /// <param name="ids">记录 ID。</param>
    /// <param name="notFound">不存在时的错误码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> ResendAsync<T>(IRepository<T, Guid> records, string owner, IReadOnlyList<Guid> ids, string notFound, CancellationToken cancellationToken)
        where T : AppAuditedEntity, IMessageRecord
    {
        var result = await ApplyAsync(records, owner, ids, notFound, static r => r.Status is MessageSendStatus.Pending or MessageSendStatus.Sending ? NotificationErrors.RecordBusy : null, static r =>
        {
            r.Status = MessageSendStatus.Pending;
            r.Attempts = 0;
            r.NextAttemptAt = null;
            r.ErrorMessage = null;
        }, cancellationToken).ConfigureAwait(false);
        if (result.Succeeded.Count > 0)
        {
            signal.Pulse();
        }

        return result;
    }

    /// <summary>
    /// 批量取消（只对排队中的）。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="records">记录仓储。</param>
    /// <param name="owner">侧。</param>
    /// <param name="ids">记录 ID。</param>
    /// <param name="notFound">不存在时的错误码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public static Task<BatchResult> CancelAsync<T>(IRepository<T, Guid> records, string owner, IReadOnlyList<Guid> ids, string notFound, CancellationToken cancellationToken)
        where T : AppAuditedEntity, IMessageRecord
        => ApplyAsync(records, owner, ids, notFound, static r => r.Status == MessageSendStatus.Pending ? null : NotificationErrors.RecordNotPending, static r => r.Status = MessageSendStatus.Cancelled, cancellationToken);

    /// <summary>
    /// 批量删除（发送中的逐项拒绝）。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="records">记录仓储。</param>
    /// <param name="owner">侧。</param>
    /// <param name="ids">记录 ID。</param>
    /// <param name="notFound">不存在时的错误码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public static async Task<BatchResult> DeleteAsync<T>(IRepository<T, Guid> records, string owner, IReadOnlyList<Guid> ids, string notFound, CancellationToken cancellationToken)
        where T : AppAuditedEntity, IMessageRecord
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(ids);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in ids.Distinct())
        {
            var found = await records.FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var record) || !string.Equals(record.OwnerTenantId, owner, StringComparison.Ordinal))
            {
                failed.Add(new BatchFailure(id, notFound));
            }
            else if (record.Status == MessageSendStatus.Sending)
            {
                failed.Add(new BatchFailure(id, NotificationErrors.RecordBusy));
            }
            else if ((await records.RemoveAsync(record, cancellationToken).ConfigureAwait(false)) is { IsFailure: true } removed)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
            }
            else
            {
                succeeded.Add(id);
            }
        }

        return new BatchResult(succeeded, failed);
    }

    private static async Task<BatchResult> ApplyAsync<T>(IRepository<T, Guid> records, string owner, IReadOnlyList<Guid> ids, string notFound, Func<T, string?> reject, Action<T> change, CancellationToken cancellationToken)
        where T : AppAuditedEntity, IMessageRecord
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(ids);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in ids.Distinct())
        {
            var found = await records.FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var record) || !string.Equals(record.OwnerTenantId, owner, StringComparison.Ordinal))
            {
                failed.Add(new BatchFailure(id, notFound));
                continue;
            }

            if (reject(record) is { } code)
            {
                failed.Add(new BatchFailure(id, code));
                continue;
            }

            change(record);
            var updated = await records.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                failed.Add(new BatchFailure(id, updated.Error!.Code));
                continue;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
