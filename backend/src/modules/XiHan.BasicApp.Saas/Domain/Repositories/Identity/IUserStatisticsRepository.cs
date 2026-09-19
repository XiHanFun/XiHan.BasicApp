// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户统计仓储接口
/// </summary>
public interface IUserStatisticsRepository : ISaasRepository<SysUserStatistics>
{
    /// <summary>
    /// 根据用户ID获取统计信息
    /// </summary>
    Task<SysUserStatistics?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
