#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthContextQueryService
// Guid:d9b0155f-fc10-4c5c-9be3-5484c4d28682
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 认证上下文查询服务
/// </summary>
public interface IAuthContextQueryService
{
    /// <summary>
    /// 获取登录租户上下文
    /// </summary>
    Task<LoginTenantContext?> GetLoginTenantOrThrowAsync(long? tenantId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserInfoDto> GetCurrentUserInfoAsync(
        long userId,
        long? tenantId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}
