// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    public async Task<SysUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(user => user.Email == email)
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

    /// <inheritdoc />
    public async Task<bool> ExistsEmailGloballyAsync(string email, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateNoTenantQueryable().Where(user => user.Email == email);
        if (excludeUserId.HasValue)
        {
            query = query.Where(user => user.BasicId != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysUser?> GetByIdIgnoreTenantAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(user => user.BasicId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SysUser>> GetListByIdsIgnoreTenantAsync(IReadOnlyCollection<long> userIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userIds is null || userIds.Count == 0)
        {
            return [];
        }

        // 必须忽略租户过滤：跨租户成员（外部协作者/顾问）的 SysUser 属于**来源租户**，
        // 而成员关系行属于**目标租户**，带租户过滤会解析不出他们的名字。
        var ids = userIds.Distinct().ToList();
        return await CreateNoTenantQueryable()
            .Where(user => ids.Contains(user.BasicId))
            .ToListAsync(cancellationToken);
    }
}
