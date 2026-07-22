// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户安全仓储接口
/// </summary>
public interface IUserSecurityRepository : ISaasRepository<SysUserSecurity>
{
    /// <summary>
    /// 根据用户ID获取安全信息
    /// </summary>
    Task<SysUserSecurity?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
