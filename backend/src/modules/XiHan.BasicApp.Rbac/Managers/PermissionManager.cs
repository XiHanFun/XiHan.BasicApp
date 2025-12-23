#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionManager
// Guid:9b2b3c4d-5e6f-7890-abcd-ef12345678ae
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 权限领域管理器
/// </summary>
public class PermissionManager : DomainService
{
    private readonly ISysPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionRepository">权限仓储</param>
    public PermissionManager(ISysPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// 验证权限编码是否唯一
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="excludeId">排除的权限ID</param>
    /// <returns></returns>
    public async Task<bool> IsPermissionCodeUniqueAsync(string permissionCode, XiHanBasicAppIdType? excludeId = null)
    {
        return !await _permissionRepository.ExistsByPermissionCodeAsync(permissionCode, excludeId);
    }

    /// <summary>
    /// 验证用户是否有权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限编码</param>
    /// <returns></returns>
    public async Task<bool> HasPermissionAsync(XiHanBasicAppIdType userId, string permissionCode)
    {
        var permissions = await _permissionRepository.GetByUserIdAsync(userId);
        return permissions.Any(p => p.PermissionCode == permissionCode);
    }
}
