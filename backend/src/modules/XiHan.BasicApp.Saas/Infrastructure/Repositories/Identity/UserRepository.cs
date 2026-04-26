#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRepository
// Guid:2dcd5307-53b9-485e-b771-58df21690f95
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public sealed class UserRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysUser>(clientResolver, unitOfWorkManager), IUserRepository
{
    /// <inheritdoc />
    public async Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(user => user.UserName == userName)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsUserNameAsync(string userName, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(user => user.UserName == userName);
        if (excludeUserId.HasValue)
        {
            query = query.Where(user => user.BasicId != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
