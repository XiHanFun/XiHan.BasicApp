#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OnlineUserQueryService
// Guid:f2a7c4e9-8b13-4d65-a921-6e0f5b8d3c72
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Utils.Linq.Expressions;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 在线用户查询应用服务
/// </summary>
/// <remarks>
/// 在线判定双数据源：
/// - 活跃会话（SysUserSession.Status==Active 且未过期）：登录态在线，跨进程重启稳定；
/// - 实时连接（<see cref="IConnectionManager"/>）：当前持有 SignalR 连接，逐行标注 IsRealtimeOnline。
/// 权限复用用户会话码（在线用户本质是会话的实时视图）；强制下线走 UserSession/RevokeUserSession[s]。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "在线用户")]
public sealed class OnlineUserQueryService
    : SaasApplicationService, IOnlineUserQueryService
{
    private readonly IUserSessionRepository _userSessionRepository;

    private readonly IUserRepository _userRepository;

    private readonly IConnectionManager _connectionManager;

    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OnlineUserQueryService(
        IUserSessionRepository userSessionRepository,
        IUserRepository userRepository,
        IConnectionManager connectionManager,
        IFieldSecurityService fieldSecurityService)
    {
        _userSessionRepository = userSessionRepository;
        _userRepository = userRepository;
        _connectionManager = connectionManager;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Read)]
    public async Task<PageResultDtoBase<OnlineUserListItemDto>> GetOnlineUserPageAsync(OnlineUserPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.UserId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        var now = DateTimeOffset.UtcNow;
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        // 排序：前端选择原样带入，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认（最后活动时间倒序）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysUserSession", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            request.Conditions.AddSort((SysUserSession session) => session.LastActivityTime, SortDirection.Descending);
        }

        var sessionPage = await _userSessionRepository.GetPagedAsync(
            request,
            BuildPredicate(input, now),
            cancellationToken);
        if (sessionPage.Items.Count == 0)
        {
            return new PageResultDtoBase<OnlineUserListItemDto>([], sessionPage.Page);
        }

        var userIds = sessionPage.Items.Select(session => session.UserId).Where(id => id > 0).Distinct().ToArray();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userMap = users.ToDictionary(user => user.BasicId);

        // 实时连接标注：按去重用户查询一次连接管理器（进程内/缓存查询，开销可忽略）
        var realtimeMap = new Dictionary<long, bool>(userIds.Length);
        foreach (var userId in userIds)
        {
            realtimeMap[userId] = await _connectionManager.IsUserOnlineAsync(userId.ToString());
        }

        var items = sessionPage.Items
            .Select(session => UserSessionApplicationMapper.ToOnlineUserListItemDto(
                session,
                userMap.GetValueOrDefault(session.UserId),
                realtimeMap.GetValueOrDefault(session.UserId),
                now))
            .ToList();

        return new PageResultDtoBase<OnlineUserListItemDto>(items, sessionPage.Page);
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Read)]
    public async Task<OnlineUserSummaryDto> GetOnlineUserSummaryAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var activeSessions = await _userSessionRepository.GetListAsync(
            session => session.Status == SessionStatus.Active && (session.ExpirationTime == null || session.ExpirationTime > now),
            cancellationToken);

        return new OnlineUserSummaryDto
        {
            RealtimeOnlineUsers = await _connectionManager.GetOnlineUserCountAsync(),
            ActiveSessions = activeSessions.Count,
            ActiveUsers = activeSessions.Select(session => session.UserId).Distinct().Count()
        };
    }

    /// <summary>
    /// 构建在线会话谓词：活跃且未过期（ExpirationTime 为空视为不过期，与自助会话列表口径一致）
    /// </summary>
    private static Expression<Func<SysUserSession, bool>> BuildPredicate(OnlineUserPageQueryDto input, DateTimeOffset now)
    {
        var predicate = PredicateComposer.True<SysUserSession>()
            .And(session => session.Status == SessionStatus.Active)
            .And(session => session.ExpirationTime == null || session.ExpirationTime > now)
            .AndIf(input.UserId.HasValue, session => session.UserId == input.UserId!.Value)
            .AndIf(input.DeviceType.HasValue, session => session.DeviceType == input.DeviceType!.Value);

        var keyword = input.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            predicate = predicate.And(session =>
                session.UserSessionId.Contains(keyword)
                || (session.DeviceName != null && session.DeviceName.Contains(keyword))
                || (session.OperatingSystem != null && session.OperatingSystem.Contains(keyword))
                || (session.Browser != null && session.Browser.Contains(keyword)));
        }

        return predicate;
    }
}
