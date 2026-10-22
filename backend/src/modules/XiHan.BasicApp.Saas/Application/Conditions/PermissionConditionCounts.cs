// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 授权行上启用的条件条数（授权列表标「有条件」用）
/// </summary>
public static class PermissionConditionCounts
{
    /// <summary>
    /// 角色权限主键 → 启用的条件条数
    /// </summary>
    public static async Task<IReadOnlyDictionary<long, int>> CountForRolePermissionsAsync(
        this IPermissionConditionRepository repository,
        IEnumerable<long> rolePermissionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return (await repository.GetValidByAuthorizationIdsAsync(rolePermissionIds, [], cancellationToken))
            .GroupBy(condition => condition.RolePermissionId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    /// <summary>
    /// 用户直授主键 → 启用的条件条数
    /// </summary>
    public static async Task<IReadOnlyDictionary<long, int>> CountForUserPermissionsAsync(
        this IPermissionConditionRepository repository,
        IEnumerable<long> userPermissionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return (await repository.GetValidByAuthorizationIdsAsync([], userPermissionIds, cancellationToken))
            .GroupBy(condition => condition.UserPermissionId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());
    }
}
