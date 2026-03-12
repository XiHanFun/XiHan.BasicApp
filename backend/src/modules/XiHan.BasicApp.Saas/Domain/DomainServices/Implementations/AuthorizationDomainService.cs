#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationDomainService
// Guid:fa6a130f-a199-492d-b1d7-e34f0e1e1c7b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:59:06
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 授权领域服务实现
/// </summary>
public class AuthorizationDomainService : IAuthorizationDomainService
{
    private readonly IPermissionDomainService _permissionDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionDomainService">权限规则领域服务</param>
    public AuthorizationDomainService(IPermissionDomainService permissionDomainService)
    {
        _permissionDomainService = permissionDomainService;
    }

    /// <summary>
    /// 获取用户权限代码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户权限代码集合</returns>
    public async Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        return await _permissionDomainService.GetUserPermissionCodesAsync(userId, tenantId, cancellationToken);
    }

    /// <summary>
    /// 判断用户是否有权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限代码</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有权限</returns>
    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        return await _permissionDomainService.HasPermissionAsync(userId, permissionCode, tenantId, cancellationToken);
    }

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门ID集合，空集合表示不限部门</returns>
    public async Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        return await _permissionDomainService.GetUserDataScopeDepartmentIdsAsync(userId, tenantId, cancellationToken);
    }
}
