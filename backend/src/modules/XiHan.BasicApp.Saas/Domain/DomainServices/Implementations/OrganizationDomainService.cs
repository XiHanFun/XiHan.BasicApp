#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OrganizationDomainService
// Guid:7bafaf57-ceaf-49ec-ac85-920301df5aa9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 组织架构领域服务实现
/// </summary>
public class OrganizationDomainService : IOrganizationDomainService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="departmentRepository">部门仓储</param>
    /// <param name="userRepository">用户仓储</param>
    public OrganizationDomainService(
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 获取部门及其全部下级部门ID
    /// </summary>
    /// <param name="departmentId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetDepartmentAndChildrenIdsAsync(long departmentId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (departmentId <= 0)
        {
            return [];
        }

        var descendantIds = await _departmentRepository.GetDescendantIdsAsync(
            departmentId,
            includeSelf: true,
            tenantId,
            cancellationToken);

        return descendantIds.Count > 0
            ? descendantIds
            : includeSelf
                ? [departmentId]
                : [];
    }

    /// <summary>
    /// 获取用户所属部门ID
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetUserDepartmentIdsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return [];
        }

        var relations = await _userRepository.GetUserDepartmentsAsync(userId, tenantId, cancellationToken);
        return relations
            .Where(relation => relation.Status == YesOrNo.Yes)
            .Select(relation => relation.DepartmentId)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="includeChildren"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetUserDepartmentScopeIdsAsync(
        long userId,
        bool includeChildren,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var ownDepartmentIds = await GetUserDepartmentIdsAsync(userId, tenantId, cancellationToken);
        if (ownDepartmentIds.Count == 0 || !includeChildren)
        {
            return ownDepartmentIds;
        }

        var scope = new HashSet<long>();
        foreach (var departmentId in ownDepartmentIds)
        {
            var children = await GetDepartmentAndChildrenIdsAsync(departmentId, tenantId, cancellationToken);
            scope.UnionWith(children);
        }

        return scope.ToArray();
    }
}
