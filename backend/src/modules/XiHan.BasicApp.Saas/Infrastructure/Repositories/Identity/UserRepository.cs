// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public sealed class UserRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager,
    ICurrentTenant currentTenant)
    : SaasAggregateRepository<SysUser>(clientResolver, unitOfWorkManager), IUserRepository
{
    /// <summary>
    /// 平台租户标识（账号注册表里平台账号的归属值）
    /// </summary>
    private const long PlatformTenantId = 0;

    /// <summary>
    /// 根据当前租户和用户名获取用户
    /// </summary>
    /// <remarks>
    /// 经 CreateQueryable 的全局租户过滤（AOP）按当前租户上下文隔离，与唯一索引 UX_TeId_UsNa 语义一致。
    /// 注：登录路径的用户定位实际走框架 IUserStore（SaasUserStore，显式 WHERE TenantId + UserName）；本方法当前无调用方，仅为仓储能力预留。
    /// </remarks>
    public async Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(user => user.UserName == userName)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据当前租户和邮箱获取用户
    /// </summary>
    /// <remarks>
    /// 经 CreateQueryable 的全局租户过滤（AOP）按当前租户上下文隔离。邮箱列为非唯一索引（IX_Em），存在重复时取首条匹配。
    /// </remarks>
    public async Task<SysUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(user => user.Email == email)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查当前租户下用户名是否存在
    /// </summary>
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

    /// <summary>
    /// 检查邮箱是否已被占用（全平台范围，邮箱为登录身份标识须全局唯一）
    /// </summary>
    /// <remarks>
    /// 平台态执行：账号注册表落在平台库，租户上下文下连接会被解析到该租户独立库（库隔离部署）。
    /// </remarks>
    /// <param name="email">邮箱（调用方已 Trim）</param>
    /// <param name="excludeUserId">排除的用户主键（更新自身时传入）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> ExistsEmailGloballyAsync(string email, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        cancellationToken.ThrowIfCancellationRequested();

        using var platformScope = currentTenant.Change(null);

        var query = CreateNoTenantQueryable().Where(user => user.Email == email);
        if (excludeUserId.HasValue)
        {
            query = query.Where(user => user.BasicId != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 检查指定租户下用户名是否已被占用（连带平台账号一起比对，避免与平台账号重名）
    /// </summary>
    /// <remarks>
    /// 租户范围来自入参而非当前上下文：平台态执行，租户范围显式落进 WHERE。
    /// 与 <see cref="ExistsUserNameAsync"/> 的区别是后者按当前租户上下文经全局过滤器隔离。
    /// </remarks>
    /// <param name="tenantId">目标租户主键</param>
    /// <param name="userName">用户名（调用方已 Trim）</param>
    /// <param name="excludeUserId">排除的用户主键（更新自身时传入）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> ExistsUserNameInTenantAsync(long tenantId, string userName, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        cancellationToken.ThrowIfCancellationRequested();

        using var platformScope = currentTenant.Change(null);

        var query = CreateNoTenantQueryable()
            .Where(user => user.TenantId == tenantId || user.TenantId == PlatformTenantId)
            .Where(user => user.UserName == userName);
        if (excludeUserId.HasValue)
        {
            query = query.Where(user => user.BasicId != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 忽略租户过滤，按主键获取用户（平台运维 / 跨租户切换场景使用，需上层做权限校验）
    /// </summary>
    /// <remarks>
    /// 多租户成员切换时，用户当前 token 的活动租户可能与 SysUser.TenantId（归属租户）不一致，
    /// 经全局租户过滤会查不到用户，故此处显式忽略租户过滤按主键定位。
    /// </remarks>
    public async Task<SysUser?> GetByIdIgnoreTenantAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(user => user.BasicId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 按主键批量获取用户（忽略租户过滤）
    /// </summary>
    /// <remarks>
    /// 用于跨租户场景批量解析用户身份：跨租户成员（外部协作者/顾问）的 <see cref="SysUser"/> 属于来源租户，
    /// 而成员关系行属于目标租户，带租户过滤会解析不出他们的名字。
    /// </remarks>
    /// <param name="userIds">用户主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表（集合为空时返回空列表）</returns>
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
