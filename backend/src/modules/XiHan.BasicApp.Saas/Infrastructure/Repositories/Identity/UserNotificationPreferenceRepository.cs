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
    /// <summary>
    /// 根据用户ID获取通知偏好信息（跨租户）
    /// </summary>
    /// <remarks>
    /// 唯一索引 UX_UsId 不含租户：每个用户全局仅一行，行带的是首次保存时所在租户的戳。
    /// 带租户过滤会在别的租户里查不到而再插一行，撞唯一索引。
    /// </remarks>
    public async Task<SysUserNotificationPreference?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(preference => preference.UserId == userId)
            .FirstAsync(cancellationToken);
    }
}
