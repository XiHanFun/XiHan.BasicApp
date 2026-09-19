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

    /// <summary>
    /// 跨租户获取用户全部统计快照（快照按「租户 × 用户 × 日期 × 周期」落行，个人中心自助场景按用户取全）
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计快照列表</returns>
    Task<IReadOnlyList<SysUserStatistics>> GetListByUserIdIgnoreTenantAsync(long userId, CancellationToken cancellationToken = default);
}
