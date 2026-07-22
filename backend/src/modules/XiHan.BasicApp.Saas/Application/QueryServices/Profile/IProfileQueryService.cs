// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 当前用户个人中心查询服务
/// </summary>
public interface IProfileQueryService
{
    /// <summary>
    /// 获取当前用户安全上下文
    /// </summary>
    Task<ProfileUserSecurityContext> GetSecurityContextAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户个人资料
    /// </summary>
    Task<UserProfileDto> GetProfileAsync(long userId, long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户会话列表
    /// </summary>
    Task<List<ProfileSessionDto>> GetSessionsAsync(long userId, string? currentSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户登录日志
    /// </summary>
    Task<ProfileLoginLogPageDto> GetLoginLogsAsync(long userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户第三方账号绑定
    /// </summary>
    Task<List<ProfileExternalLoginDto>> GetLinkedAccountsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户活跃度统计
    /// </summary>
    Task<ProfileActivityDto> GetActivityAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户通知偏好（无记录时返回默认值）
    /// </summary>
    Task<ProfileNotificationPreferenceDto> GetNotificationPreferenceAsync(long userId, CancellationToken cancellationToken = default);
}
