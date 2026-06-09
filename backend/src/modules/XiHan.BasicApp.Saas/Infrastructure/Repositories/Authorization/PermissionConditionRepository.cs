#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionRepository
// Guid:8e879385-7f0c-453b-9e06-543c63798b9e
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
/// 权限条件仓储实现
/// </summary>
public sealed class PermissionConditionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPermissionCondition>(clientResolver), IPermissionConditionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysPermissionCondition>> GetValidByAuthorizationIdsAsync(
        IEnumerable<long> rolePermissionIds,
        IEnumerable<long> userPermissionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rolePermissionIds);
        ArgumentNullException.ThrowIfNull(userPermissionIds);

        var rolePermissionIdArray = rolePermissionIds.Distinct().ToArray();
        var userPermissionIdArray = userPermissionIds.Distinct().ToArray();
        if (rolePermissionIdArray.Length == 0 && userPermissionIdArray.Length == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(condition =>
                (condition.RolePermissionId != null && rolePermissionIdArray.Contains(condition.RolePermissionId.Value)) ||
                (condition.UserPermissionId != null && userPermissionIdArray.Contains(condition.UserPermissionId.Value)))
            .Where(condition => condition.Status == ValidityStatus.Valid)
            .ToListAsync(cancellationToken);
    }
}
