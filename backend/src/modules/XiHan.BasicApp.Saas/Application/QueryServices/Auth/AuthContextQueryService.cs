#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthContextQueryService
// Guid:0a9c37bd-3f44-4af1-99b5-af91f9e4f990
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
