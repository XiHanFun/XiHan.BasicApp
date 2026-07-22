// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户通知偏好仓储实现
/// </summary>
public sealed class UserNotificationPreferenceRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserNotificationPreference>(clientResolver), IUserNotificationPreferenceRepository
{
    /// <inheritdoc />
    public async Task<SysUserNotificationPreference?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(preference => preference.UserId == userId)
            .FirstAsync(cancellationToken);
    }
}
