#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserRepository
// Guid:8a2b3c4d-5e6f-7890-abcd-ef1234567897
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统用户仓储接口
/// </summary>
public interface ISysUserRepository : IRepositoryBase<SysUser, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns></returns>
    Task<SysUser?> GetByUserNameAsync(string userName);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns></returns>
    Task<SysUser?> GetByEmailAsync(string email);

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <returns></returns>
    Task<SysUser?> GetByPhoneAsync(string phone);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    Task<bool> ExistsByUserNameAsync(string userName, XiHanBasicAppIdType? excludeId = null);

    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    Task<bool> ExistsByEmailAsync(string email, XiHanBasicAppIdType? excludeId = null);

    /// <summary>
    /// 检查手机号是否存在
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    Task<bool> ExistsByPhoneAsync(string phone, XiHanBasicAppIdType? excludeId = null);

    /// <summary>
    /// 获取用户的角色ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<XiHanBasicAppIdType>> GetUserRoleIdsAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 获取用户的部门ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<XiHanBasicAppIdType>> GetUserDepartmentIdsAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 获取用户的权限编码列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<string>> GetUserPermissionsAsync(XiHanBasicAppIdType userId);
}
