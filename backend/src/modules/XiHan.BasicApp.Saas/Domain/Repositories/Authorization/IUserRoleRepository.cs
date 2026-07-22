// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
