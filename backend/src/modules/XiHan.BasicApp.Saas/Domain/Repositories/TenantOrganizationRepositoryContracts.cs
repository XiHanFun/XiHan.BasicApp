#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantOrganizationRepositoryContracts
// Guid:ff53247a-45cd-4782-a82c-ce7eae9f1248
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 租户仓储接口
/// </summary>
public interface ITenantRepository : ISaasAggregateRepository<SysTenant>
{
    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    Task<SysTenant?> GetByCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    Task<bool> ExistsTenantCodeAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 部门仓储接口
/// </summary>
public interface IDepartmentRepository : ISaasAggregateRepository<SysDepartment>
{
    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    Task<SysDepartment?> GetByCodeAsync(long tenantId, string departmentCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取子部门
    /// </summary>
    Task<IReadOnlyList<SysDepartment>> GetChildrenAsync(long tenantId, long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否存在子部门
    /// </summary>
    Task<bool> HasChildrenAsync(long tenantId, long departmentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 部门层级仓储接口
/// </summary>
public interface IDepartmentHierarchyRepository : ISaasRepository<SysDepartmentHierarchy>
{
    /// <summary>
    /// 获取后代部门ID
    /// </summary>
    Task<IReadOnlyList<long>> GetDescendantIdsAsync(long tenantId, long departmentId, bool includeSelf, CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户部门仓储接口
/// </summary>
public interface IUserDepartmentRepository : ISaasRepository<SysUserDepartment>
{
    /// <summary>
    /// 获取用户有效部门归属
    /// </summary>
    Task<IReadOnlyList<SysUserDepartment>> GetValidByUserIdAsync(long tenantId, long userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 角色层级仓储接口
/// </summary>
public interface IRoleHierarchyRepository : ISaasRepository<SysRoleHierarchy>
{
    /// <summary>
    /// 获取角色继承链中的祖先角色ID
    /// </summary>
    Task<IReadOnlyList<long>> GetAncestorIdsAsync(long tenantId, IEnumerable<long> roleIds, bool includeSelf, CancellationToken cancellationToken = default);
}

/// <summary>
/// 角色数据范围仓储接口
/// </summary>
public interface IRoleDataScopeRepository : ISaasRepository<SysRoleDataScope>
{
    /// <summary>
    /// 获取角色有效自定义数据范围
    /// </summary>
    Task<IReadOnlyList<SysRoleDataScope>> GetValidByRoleIdsAsync(long tenantId, IEnumerable<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}
