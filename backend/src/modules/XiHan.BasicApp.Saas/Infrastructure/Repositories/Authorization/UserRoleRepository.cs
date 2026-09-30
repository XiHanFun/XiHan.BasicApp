// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户角色仓储实现
/// </summary>
public sealed class UserRoleRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserRole>(clientResolver), IUserRoleRepository
{
    /// <summary>
    /// 获取用户有效角色授权
    /// </summary>
    public async Task<IReadOnlyList<SysUserRole>> GetValidByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(role => role.UserId == userId)
            .Where(role => role.Status == ValidityStatus.Valid)
            .Where(role => role.EffectiveTime == null || role.EffectiveTime <= now)
            .Where(role => role.ExpirationTime == null || role.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 角色在当前上下文此刻生效的授权
    /// </summary>
    public async Task<IReadOnlyList<SysUserRole>> GetValidByRoleIdAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(userRole => userRole.RoleId == roleId)
            .Where(userRole => userRole.Status == ValidityStatus.Valid)
            .Where(userRole => userRole.EffectiveTime == null || userRole.EffectiveTime <= now)
            .Where(userRole => userRole.ExpirationTime == null || userRole.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 角色在当前上下文占用的成员名额
    /// </summary>
    public async Task<int> CountOccupiedByRoleIdAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(userRole => userRole.RoleId == roleId && userRole.Status == ValidityStatus.Valid)
            .Where(userRole => userRole.ExpirationTime == null || userRole.ExpirationTime > now)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// 跨租户获取持有指定角色有效授权的用户主键
    /// </summary>
    public async Task<IReadOnlyList<long>> GetValidUserIdsByRoleIdsIgnoreTenantAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        cancellationToken.ThrowIfCancellationRequested();

        if (roleIds.Count == 0)
        {
            return [];
        }

        var ids = roleIds.Distinct().ToList();
        return await CreateNoTenantQueryable()
            .Where(userRole => ids.Contains(userRole.RoleId) && userRole.Status == ValidityStatus.Valid)
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
    /// <summary>
    /// 一组角色在当前上下文此刻生效的授权
    /// </summary>
    public async Task<IReadOnlyList<SysUserRole>> GetValidByRoleIdsAsync(IReadOnlyCollection<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        cancellationToken.ThrowIfCancellationRequested();

        var ids = roleIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await CreateQueryable()
            .Where(userRole => ids.Contains(userRole.RoleId))
            .Where(userRole => userRole.Status == ValidityStatus.Valid)
            .Where(userRole => userRole.EffectiveTime == null || userRole.EffectiveTime <= now)
            .Where(userRole => userRole.ExpirationTime == null || userRole.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 一组用户在当前上下文此刻生效的授权
    /// </summary>
    public async Task<IReadOnlyList<SysUserRole>> GetValidByUserIdsAsync(IReadOnlyCollection<long> userIds, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        cancellationToken.ThrowIfCancellationRequested();

        var ids = userIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await CreateQueryable()
            .Where(userRole => ids.Contains(userRole.UserId))
            .Where(userRole => userRole.Status == ValidityStatus.Valid)
            .Where(userRole => userRole.EffectiveTime == null || userRole.EffectiveTime <= now)
            .Where(userRole => userRole.ExpirationTime == null || userRole.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 跨租户获取此刻有成员持有指定角色的租户
    /// </summary>
    public async Task<IReadOnlyList<long>> GetValidTenantIdsByRoleIdsIgnoreTenantAsync(IReadOnlyCollection<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        cancellationToken.ThrowIfCancellationRequested();

        var ids = roleIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await CreateNoTenantQueryable()
            .Where(userRole => userRole.TenantId != 0 && ids.Contains(userRole.RoleId))
            .Where(userRole => userRole.Status == ValidityStatus.Valid)
            .Where(userRole => userRole.EffectiveTime == null || userRole.EffectiveTime <= now)
            .Where(userRole => userRole.ExpirationTime == null || userRole.ExpirationTime > now)
            .Select(userRole => userRole.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
