#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDataScopeRepository
// Guid:bc9ac6b4-e042-4d30-9eea-01eac8a47e24
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 角色数据范围仓储实现
/// </summary>
public sealed class RoleDataScopeRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysRoleDataScope>(clientResolver), IRoleDataScopeRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysRoleDataScope>> GetValidByRoleIdsAsync(IEnumerable<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var roleIdArray = roleIds.Distinct().ToArray();
        if (roleIdArray.Length == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(scope => roleIdArray.Contains(scope.RoleId))
            .Where(scope => scope.Status == ValidityStatus.Valid)
            .Where(scope => scope.EffectiveTime == null || scope.EffectiveTime <= now)
            .Where(scope => scope.ExpirationTime == null || scope.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }
}
