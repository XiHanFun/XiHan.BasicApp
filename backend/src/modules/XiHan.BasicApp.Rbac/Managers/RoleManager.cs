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

using XiHan.BasicApp.Rbac.Repositories.Roles;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 系统角色领域管理器
/// </summary>
/// <remarks>
/// 职责：角色相关的领域业务规则和验证逻辑
/// </remarks>
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
    public async Task<bool> IsRoleCodeUniqueAsync(string roleCode, long? excludeId = null)
    {
        return !await _roleRepository.ExistsByRoleCodeAsync(roleCode, excludeId);
    }

    /// <summary>
    /// 检查角色是否可以删除
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public async Task<bool> CanDeleteAsync(long roleId)
    {
        var userCount = await _roleRepository.GetRoleUserCountAsync(roleId);
        return userCount == 0;
    }

    /// <summary>
    /// 验证角色继承关系是否合法
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="parentRoleId">父角色ID</param>
    /// <returns></returns>
    public async Task<bool> CanSetParentRoleAsync(long roleId, long parentRoleId)
    {
        // 检查是否会形成循环继承
        return !await _roleRepository.WouldCreateCycleAsync(roleId, parentRoleId);
    }

    /// <summary>
    /// 检查角色名称是否合法
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <returns></returns>
    public bool IsValidRoleCode(string roleCode)
    {
        // 业务规则：角色编码不能为空，长度在2-50之间，只能包含字母、数字、下划线
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return false;
        }

        if (roleCode.Length is < 2 or > 50)
        {
            return false;
        }

        return System.Text.RegularExpressions.Regex.IsMatch(roleCode, @"^[a-zA-Z0-9_]+$");
    }
}
