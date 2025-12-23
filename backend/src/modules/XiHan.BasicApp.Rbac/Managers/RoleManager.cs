#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleManager
// Guid:8b2b3c4d-5e6f-7890-abcd-ef12345678ad
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 角色领域管理器
/// </summary>
public class RoleManager : DomainService
{
    private readonly ISysRoleRepository _roleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleRepository">角色仓储</param>
    public RoleManager(ISysRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    /// <summary>
    /// 验证角色编码是否唯一
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeId">排除的角色ID</param>
    /// <returns></returns>
    public async Task<bool> IsRoleCodeUniqueAsync(string roleCode, XiHanBasicAppIdType? excludeId = null)
    {
        return !await _roleRepository.ExistsByRoleCodeAsync(roleCode, excludeId);
    }

    /// <summary>
    /// 检查角色是否可以删除
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public async Task<bool> CanDeleteAsync(XiHanBasicAppIdType roleId)
    {
        var userCount = await _roleRepository.GetRoleUserCountAsync(roleId);
        return userCount == 0;
    }
}
