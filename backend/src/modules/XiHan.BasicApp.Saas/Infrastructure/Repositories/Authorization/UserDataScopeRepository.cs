// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户数据范围仓储实现
/// </summary>
public sealed class UserDataScopeRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserDataScope>(clientResolver), IUserDataScopeRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysUserDataScope>> GetValidByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(scope => scope.UserId == userId)
            .Where(scope => scope.Status == ValidityStatus.Valid)
            .ToListAsync(cancellationToken);
    }
}
