// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 认证上下文查询服务
/// </summary>
public interface IAuthContextQueryService
{
    /// <summary>
    /// 获取登录租户上下文（租户不可用时抛出带原因的异常，用于显式切换租户等需要明确报错的场景）
    /// </summary>
    Task<LoginTenantContext?> GetLoginTenantOrThrowAsync(long? tenantId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找可登录的租户上下文（租户不存在或不可用时返回 null，不抛异常，用于登录落点判定）
    /// </summary>
    Task<LoginTenantContext?> FindAvailableLoginTenantAsync(long tenantId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserInfoDto> GetCurrentUserInfoAsync(
        long userId,
        long? tenantId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}
