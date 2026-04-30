#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeRepository
// Guid:9f9e3fef-8652-4331-a440-345a1e6e84fb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
