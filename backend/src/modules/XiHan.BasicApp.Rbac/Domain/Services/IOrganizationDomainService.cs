#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOrganizationDomainService
// Guid:03ad22ea-ea8f-444e-8655-efc6e71260d9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Domain.Services;

/// <summary>
/// 组织架构领域服务
/// </summary>
public interface IOrganizationDomainService
{
    /// <summary>
    /// 获取部门及全部下级部门ID
    /// </summary>
    Task<IReadOnlyCollection<long>> GetDepartmentAndChildrenIdsAsync(long departmentId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户所属部门ID
    /// </summary>
    Task<IReadOnlyCollection<long>> GetUserDepartmentIdsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    Task<IReadOnlyCollection<long>> GetUserDepartmentScopeIdsAsync(
        long userId,
        bool includeChildren,
        long? tenantId = null,
        CancellationToken cancellationToken = default);
}
