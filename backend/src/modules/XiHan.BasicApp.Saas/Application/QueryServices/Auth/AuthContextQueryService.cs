// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 认证上下文查询服务实现
/// </summary>
public sealed class AuthContextQueryService
    : IAuthContextQueryService
{
    private readonly IUserRepository _userRepository;

    private readonly ITenantRepository _tenantRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthContextQueryService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITenantUserRepository tenantUserRepository)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _tenantUserRepository = tenantUserRepository;
    }

    /// <summary>
    /// 获取登录租户上下文（租户不可用时抛出带原因的异常，用于显式切换租户等需要明确报错的场景）
    /// </summary>
    public async Task<LoginTenantContext?> GetLoginTenantOrThrowAsync(long? tenantId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (!tenantId.HasValue || tenantId.Value <= 0)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken)
            ?? throw new InvalidOperationException("租户不存在。");
        if (tenant.TenantStatus != TenantStatus.Normal)
        {
            throw new InvalidOperationException("租户当前不可登录。");
        }

        if (tenant.ConfigStatus is not TenantConfigStatus.Configured)
        {
            throw new InvalidOperationException("租户尚未完成初始化配置。");
        }

        if (tenant.ExpirationTime.HasValue && tenant.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("租户已过期。");
        }

        return new LoginTenantContext(tenant.BasicId, tenant.TenantName);
    }

    /// <summary>
    /// 查找可登录的租户上下文（租户不存在或不可用时返回 null，不抛异常，用于登录落点判定）
    /// </summary>
    public async Task<LoginTenantContext?> FindAvailableLoginTenantAsync(long tenantId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null || !new AvailableTenantSpecification(now).IsSatisfiedBy(tenant))
        {
            return null;
        }

        return new LoginTenantContext(tenant.BasicId, tenant.TenantName);
    }

    /// <summary>
    /// 用户可进入的租户：有效成员关系 ∩ 可进入的租户，按最近进入时间倒序（没进入过的排后），再按租户排序
    /// </summary>
    public async Task<IReadOnlyList<AccessibleTenant>> GetAccessibleTenantsAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);
        cancellationToken.ThrowIfCancellationRequested();

        var memberships = await _tenantUserRepository.GetActiveByUserIdAsync(userId, now, cancellationToken);
        if (memberships.Count == 0)
        {
            return [];
        }

        var isAvailable = new AvailableTenantSpecification(now);
        var tenants = (await _tenantRepository.GetByIdsAsync(memberships.Select(membership => membership.TenantId).Distinct(), cancellationToken))
            .Where(isAvailable.IsSatisfiedBy)
            .ToDictionary(tenant => tenant.BasicId);

        return [.. memberships
            .Where(membership => tenants.ContainsKey(membership.TenantId))
            .Select(membership => new AccessibleTenant(membership, tenants[membership.TenantId]))
            .OrderByDescending(item => item.Membership.LastActiveTime.HasValue)
            .ThenByDescending(item => item.Membership.LastActiveTime)
            .ThenBy(item => item.Tenant.Sort)
            .ThenBy(item => item.Tenant.TenantName, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public async Task<UserInfoDto> GetCurrentUserInfoAsync(
        long userId,
        long? tenantId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户标识必须大于 0。");
        }

        ArgumentNullException.ThrowIfNull(roles);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdIgnoreTenantAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前用户不存在。");

        var roleList = roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var tenantName = tenantId is > 0
            ? (await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken))?.TenantName
            : null;

        return new UserInfoDto
        {
            BasicId = user.BasicId,
            UserName = user.UserName,
            NickName = user.NickName ?? user.RealName,
            Avatar = user.Avatar,
            Email = user.Email,
            Phone = user.Phone,
            TenantId = tenantId,
            TenantName = tenantName,
            IsPlatform = !tenantId.HasValue,
            // 平台是 0 号租户，只对平台账号开放
            CanAccessPlatform = user.TenantId == 0,
            Roles = roleList
        };
    }
}
