// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
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
    /// 按邮箱定位账号（全平台范围）
    /// </summary>
    /// <remarks>
    /// 邮箱是登录身份标识、全平台唯一（UX_Em），账号可能归属任意租户，显式跨租户查找。
    /// 平台态执行：账号注册表落在平台库，租户上下文下连接会被解析到该租户独立库（库隔离部署）。
    /// </remarks>
    /// <param name="email">邮箱（调用方已 Trim）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<SysUser?> GetByEmailGloballyAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        cancellationToken.ThrowIfCancellationRequested();

        using var platformScope = currentTenant.Change(null);

        return await CreateNoTenantQueryable()
            .Where(user => user.Email == email)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查当前上下文注册的账号里用户名是否已被占用（租户里连带平台账号一起比对）
    /// </summary>
    public async Task<bool> ExistsUserNameAsync(string userName, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        cancellationToken.ThrowIfCancellationRequested();

        // 账号严格隔离，与平台账号比对要显式跨租户
        var scopeTenantId = currentTenant.Id ?? PlatformTenantId;
        var query = CreateNoTenantQueryable()
            .Where(user => user.TenantId == scopeTenantId || user.TenantId == PlatformTenantId)
            .Where(user => user.UserName == userName);
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

    /// <summary>
    /// 租户成员的账号分页：本租户已接受的成员（含注册在别处的外部成员），账号跨租户读取
    /// </summary>
    public async Task<PageResultDtoBase<SysUser>> GetMemberAccountsPagedAsync(long tenantId, PageRequestDtoBase request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return await QueryMemberAccounts(tenantId)
            .ApplyPageRequest(request)
            .ToPageResultAsync(request, cancellationToken);
    }

    /// <summary>
    /// 租户成员的账号：不是该租户已接受的成员时返回 null
    /// </summary>
    public async Task<SysUser?> GetMemberAccountAsync(long tenantId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await QueryMemberAccounts(tenantId)
            .Where(user => user.BasicId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 租户成员中启用账号的主键
    /// </summary>
    public async Task<IReadOnlyList<long>> GetEnabledMemberAccountIdsAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await QueryMemberAccounts(tenantId)
            .Where(user => user.Status == EnableStatus.Enabled)
            .Select(user => user.BasicId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 某个租户已接受成员的账号：账号可能注册在别的租户，按成员关系跨租户取
    /// </summary>
    private ISugarQueryable<SysUser> QueryMemberAccounts(long tenantId)
    {
        return CreateNoTenantQueryable()
            .Where(user => SqlFunc.Subqueryable<SysTenantUser>()
                .Where(member => member.UserId == user.BasicId
                                 && member.TenantId == tenantId
                                 && member.InviteStatus == TenantMemberInviteStatus.Accepted
                                 && member.IsDeleted == false)
                .Any());
    }

    /// <summary>
    /// 跨租户获取全部启用账号的主键（平台公告「全员」投递专用）
    /// </summary>
    public async Task<IReadOnlyList<long>> GetEnabledIdsIgnoreTenantAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(user => user.Status == EnableStatus.Enabled)
            .Select(user => user.BasicId)
            .ToListAsync(cancellationToken);
    }
}
