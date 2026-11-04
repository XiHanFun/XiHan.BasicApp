// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// <see cref="ISessionHistory"/> 的实现
/// </summary>
/// <remarks>
/// 在调用的工作单元里直接查会话表；模仿会话（带发起人）不列
/// </remarks>
/// <param name="unitOfWork">工作单元</param>
public sealed class SessionHistory(SqlSugarUnitOfWork unitOfWork) : ISessionHistory
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<SessionSpan>> ListAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken)
        => Spans(await unitOfWork.ConnectionFor(typeof(SysUserSession)).Queryable<SysUserSession>()
            .Where(s => s.UserId == userId && s.RefreshExpiresAt >= since)
            .ToListAsync(cancellationToken).ConfigureAwait(false), since);

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<SessionSpan>> ListInSideAsync(Guid userId, string side, DateTimeOffset since, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        var query = unitOfWork.ConnectionFor(typeof(SysUserSession)).Queryable<SysUserSession>().Where(s => s.UserId == userId && s.RefreshExpiresAt >= since);
        query = string.Equals(side, TenantIds.Platform, StringComparison.Ordinal) ? query.Where(s => s.ActiveTenantId == null) : query.Where(s => s.ActiveTenantId == side);
        return Spans(await query.ToListAsync(cancellationToken).ConfigureAwait(false), since);
    }

    // 模仿会话与那之前就结束的在内存里筛（一个人的会话不多）
    private static List<SessionSpan> Spans(List<SysUserSession> rows, DateTimeOffset since)
        => rows
            .Where(s => s.ImpersonatorUserId is null && (s.RevokedAt is null || s.RevokedAt >= since))
            .Select(static s => new SessionSpan(s.Id, s.IssuedAt, s.LastSeenAt ?? s.IssuedAt, s.RevokedAt))
            .ToList();
}
