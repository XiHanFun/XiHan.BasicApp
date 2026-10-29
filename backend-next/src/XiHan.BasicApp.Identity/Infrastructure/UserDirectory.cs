// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// <see cref="IUserDirectory"/> 的实现：在调用的工作单元里直接查用户表（含已删除的）。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
public sealed class UserDirectory(SqlSugarUnitOfWork unitOfWork) : IUserDirectory
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, UserSummary>> FindAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, UserSummary>();
        }

        var rows = await unitOfWork.ConnectionFor(typeof(SysUser)).Queryable<SysUser>().Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.ToDictionary(static u => u.Id, static u => new UserSummary(u.Id, u.UserName, u.DisplayName));
    }
}
