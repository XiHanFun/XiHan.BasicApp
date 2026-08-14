// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 认证上下文查询服务实现
/// </summary>
public sealed class AuthContextQueryService
    : IAuthContextQueryService
{
    /// <summary>
    /// 超级管理员角色编码（与种子/授权快照约定一致）
    /// </summary>
    private const string SuperAdminRoleCode = "super_admin";

    private readonly IUserRepository _userRepository;

    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthContextQueryService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
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
        if (tenant is null
            || tenant.TenantStatus != TenantStatus.Normal
            || tenant.ConfigStatus is not TenantConfigStatus.Configured
            || (tenant.ExpirationTime.HasValue && tenant.ExpirationTime.Value <= now))
        {
            return null;
        }

        return new LoginTenantContext(tenant.BasicId, tenant.TenantName);
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

        return new UserInfoDto
        {
            BasicId = user.BasicId,
            UserName = user.UserName,
            NickName = user.NickName ?? user.RealName,
            Avatar = user.Avatar,
            Email = user.Email,
            Phone = user.Phone,
            TenantId = tenantId,
            IsPlatform = !tenantId.HasValue,
            CanAccessPlatform = roleList.Contains(SuperAdminRoleCode, StringComparer.OrdinalIgnoreCase),
            Roles = roleList
        };
    }
}
