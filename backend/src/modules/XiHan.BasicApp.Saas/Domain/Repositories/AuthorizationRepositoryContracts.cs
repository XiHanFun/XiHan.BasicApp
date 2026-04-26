#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationRepositoryContracts
// Guid:7517b3a0-7519-4202-ae05-a578bd7f92b1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IRoleRepository : ISaasAggregateRepository<SysRole>
{
    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    Task<SysRole?> GetByCodeAsync(long tenantId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取有效角色集合
    /// </summary>
    Task<IReadOnlyList<SysRole>> GetEnabledByIdsAsync(long tenantId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// 权限仓储接口
/// </summary>
public interface IPermissionRepository : ISaasAggregateRepository<SysPermission>
{
    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    Task<SysPermission?> GetByCodeAsync(long tenantId, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据权限编码集合获取权限
    /// </summary>
    Task<IReadOnlyList<SysPermission>> GetByCodesAsync(long tenantId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据资源和操作获取权限
    /// </summary>
    Task<SysPermission?> GetByResourceOperationAsync(long tenantId, long resourceId, long operationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 资源仓储接口
/// </summary>
public interface IResourceRepository : ISaasAggregateRepository<SysResource>
{
    /// <summary>
    /// 根据资源编码获取资源
    /// </summary>
    Task<SysResource?> GetByCodeAsync(long tenantId, string resourceCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// 操作仓储接口
/// </summary>
public interface IOperationRepository : ISaasAggregateRepository<SysOperation>
{
    /// <summary>
    /// 根据操作编码获取操作
    /// </summary>
    Task<SysOperation?> GetByCodeAsync(long tenantId, string operationCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// 角色权限仓储接口
/// </summary>
public interface IRolePermissionRepository : ISaasRepository<SysRolePermission>
{
    /// <summary>
    /// 获取角色有效权限授权
    /// </summary>
    Task<IReadOnlyList<SysRolePermission>> GetValidByRoleIdsAsync(long tenantId, IEnumerable<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}

/// <summary>
/// 权限条件仓储接口
/// </summary>
public interface IPermissionConditionRepository : ISaasRepository<SysPermissionCondition>
{
    /// <summary>
    /// 获取角色或用户授权关联的有效 ABAC 条件
    /// </summary>
    Task<IReadOnlyList<SysPermissionCondition>> GetValidByAuthorizationIdsAsync(long tenantId, IEnumerable<long> rolePermissionIds, IEnumerable<long> userPermissionIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// 约束规则仓储接口
/// </summary>
public interface IConstraintRuleRepository : ISaasRepository<SysConstraintRule>
{
    /// <summary>
    /// 获取当前生效的约束规则
    /// </summary>
    Task<IReadOnlyList<SysConstraintRule>> GetActiveRulesAsync(long tenantId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
