// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 密码历史仓储接口
/// </summary>
public interface IPasswordHistoryRepository : ISaasRepository<SysPasswordHistory>
{
    /// <summary>
    /// 获取用户最近的密码历史记录
    /// </summary>
    Task<IReadOnlyList<SysPasswordHistory>> GetRecentByUserIdAsync(long userId, int count, CancellationToken cancellationToken = default);
}
