#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserManager
// Guid:e6c148bd-eeb1-4f79-aea5-8ff00a7ed2a8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:39:28
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices;

/// <summary>
/// 用户领域管理器
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// 创建用户
    /// </summary>
    Task<SysUser> CreateAsync(SysUser user, string plainPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改用户密码
    /// </summary>
    Task ChangePasswordAsync(SysUser user, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验用户名是否可用
    /// </summary>
    Task EnsureUserNameUniqueAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
