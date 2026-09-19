// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户安全状态仓储实现
/// </summary>
public sealed class UserSecurityRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserSecurity>(clientResolver), IUserSecurityRepository
{
    /// <summary>
    /// 根据用户ID获取安全信息（跨租户）
    /// </summary>
    /// <remarks>
    /// 唯一索引 UX_UsId 不含租户：每个用户全局仅一行，行带的是归属租户戳。
    /// 跨租户成员在别的租户里定位自己的安全记录，带租户过滤会查不到。
    /// </remarks>
    public async Task<SysUserSecurity?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(security => security.UserId == userId)
            .FirstAsync(cancellationToken);
    }
}
