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

using System.Text.RegularExpressions;
using XiHan.BasicApp.Rbac.Repositories.Permissions;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 系统权限领域管理器
/// </summary>
/// <remarks>
/// 职责：权限相关的领域业务规则和验证逻辑
/// </remarks>
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
    public async Task<bool> IsPermissionCodeUniqueAsync(string permissionCode, long? excludeId = null)
    {
        return !await _permissionRepository.ExistsByPermissionCodeAsync(permissionCode, excludeId);
    }

    /// <summary>
    /// 检查权限编码格式是否合法
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <returns></returns>
    public bool IsValidPermissionCode(string permissionCode)
    {
        // 业务规则：权限编码格式为 Module.Action，如 User.Create
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        var parts = permissionCode.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        return parts.All(p => !string.IsNullOrWhiteSpace(p) && Regex.IsMatch(p, @"^[a-zA-Z0-9_]+$"));
    }

    /// <summary>
    /// 检查权限是否可以删除
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <returns></returns>
    public async Task<bool> CanDeleteAsync(long permissionId)
    {
        // 检查权限是否被引用
        // 这里可以添加更多业务规则
        var permission = await _permissionRepository.GetByIdAsync(permissionId);
        return permission != null && !permission.IsDeleted;
    }
}
