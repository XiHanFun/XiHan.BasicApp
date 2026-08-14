// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色继承领域服务实现
/// </summary>
public sealed class RoleHierarchyDomainService
    : IRoleHierarchyDomainService
{
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleHierarchyDomainService(IRoleHierarchyRepository roleHierarchyRepository)
    {
        _roleHierarchyRepository = roleHierarchyRepository;
    }

    /// <summary>
    /// 检测角色继承是否会形成环路
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="childRoleId">子角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在环路</returns>
    public async Task<bool> WouldCreateCycleAsync(long parentRoleId, long childRoleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (parentRoleId == childRoleId)
        {
            return true;
        }

        // 查 childRoleId 的所有祖先，若包含 parentRoleId 则形成环路
        var ancestorIds = await _roleHierarchyRepository.GetAncestorIdsAsync([childRoleId], includeSelf: false, cancellationToken);
        return ancestorIds.Contains(parentRoleId);
    }

    /// <summary>
    /// 获取角色完整继承链（含自身）的所有角色ID
    /// </summary>
    /// <param name="roleIds">起始角色ID集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>展开后的全部角色ID（含继承链）</returns>
    public async Task<IReadOnlyList<long>> ExpandRoleHierarchyAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ids = roleIds.ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _roleHierarchyRepository.GetAncestorIdsAsync(ids, includeSelf: true, cancellationToken);
    }
}
