// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户数据范围仓储接口
/// </summary>
public interface IUserDataScopeRepository : ISaasRepository<SysUserDataScope>
{
    /// <summary>
    /// 获取用户有效数据范围覆盖
    /// </summary>
    Task<IReadOnlyList<SysUserDataScope>> GetValidByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
