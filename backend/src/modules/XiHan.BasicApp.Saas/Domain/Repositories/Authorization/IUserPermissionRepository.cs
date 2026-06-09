#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserPermissionRepository
// Guid:a1553526-0f21-4557-b14d-31f35e8b106d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户直授权限仓储接口
/// </summary>
public interface IUserPermissionRepository : ISaasRepository<SysUserPermission>
{
    /// <summary>
    /// 获取用户有效直授权限
    /// </summary>
    Task<IReadOnlyList<SysUserPermission>> GetValidByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
