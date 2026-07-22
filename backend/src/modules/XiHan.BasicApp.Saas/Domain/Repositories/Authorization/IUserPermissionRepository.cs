// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
