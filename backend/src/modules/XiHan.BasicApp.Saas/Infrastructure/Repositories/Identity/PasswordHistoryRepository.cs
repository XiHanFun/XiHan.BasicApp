// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 密码历史仓储实现
/// </summary>
public sealed class PasswordHistoryRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPasswordHistory>(clientResolver), IPasswordHistoryRepository
{
    /// <summary>
    /// 获取用户最近的密码历史记录
    /// </summary>
    public async Task<IReadOnlyList<SysPasswordHistory>> GetRecentByUserIdAsync(long userId, int count, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 账号域数据落在注册地，按用户跨租户读取：不论当前在哪个租户，比对的都是这个账号的全部历史
        return await CreateNoTenantQueryable()
            .Where(history => history.UserId == userId)
            .OrderByDescending(history => history.ChangedTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
