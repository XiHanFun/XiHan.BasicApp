#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IProfileQueryService
// Guid:1d8f2f38-87dc-4d49-879c-f7792ce8cf4e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
}
