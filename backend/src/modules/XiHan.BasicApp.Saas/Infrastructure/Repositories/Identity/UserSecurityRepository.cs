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
    /// <inheritdoc />
    public async Task<SysUserSecurity?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(security => security.UserId == userId)
            .FirstAsync(cancellationToken);
    }
}
