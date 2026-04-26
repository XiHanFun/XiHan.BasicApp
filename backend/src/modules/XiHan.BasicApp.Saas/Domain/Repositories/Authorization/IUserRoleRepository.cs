#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRoleRepository
// Guid:9290fc24-0555-4c99-a754-903f4c7641dd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户角色仓储接口
/// </summary>
public interface IUserRoleRepository : ISaasRepository<SysUserRole>
{
    /// <summary>
    /// 获取用户有效角色授权
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetValidByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
