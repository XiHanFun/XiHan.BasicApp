#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkbenchQueryService
// Guid:e5c67d37-8a57-4f21-9147-9af8a4ef459f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 工作台查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "工作台")]
public sealed class WorkbenchQueryService(
    IUserStatisticsRepository userStatisticsRepository,
    IUserInboxAppService userInboxAppService,
    ICurrentUser currentUser)
    : SaasApplicationService, IWorkbenchQueryService
{
    private const int LatestInboxCount = 5;

    private readonly IUserStatisticsRepository _userStatisticsRepository = userStatisticsRepository;
    private readonly IUserInboxAppService _userInboxAppService = userInboxAppService;
    private readonly ICurrentUser _currentUser = currentUser;

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.UserStatistics.Read)]
    public async Task<WorkbenchDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var statistics = await GetTodayStatisticsAsync(userId, today, cancellationToken);
        var inboxItems = await _userInboxAppService.GetListAsync(unreadOnly: true, cancellationToken);

        return new WorkbenchDashboardSummaryDto
        {
            GeneratedTime = DateTimeOffset.UtcNow,
            Inbox = BuildInboxSummary(inboxItems),
            Statistics = BuildStatisticsSummary(statistics, today)
        };
    }

    private async Task<SysUserStatistics?> GetTodayStatisticsAsync(long userId, DateOnly today, CancellationToken cancellationToken)
    {
        var statistics = await _userStatisticsRepository.GetListAsync(
            item => item.UserId == userId &&
                    item.Period == StatisticsPeriod.Today &&
                    item.StatisticsDate == today,
            cancellationToken);

        return statistics
            .OrderByDescending(item => item.ModifiedTime ?? item.CreatedTime)
            .ThenByDescending(item => item.BasicId)
            .FirstOrDefault();
    }

    private static WorkbenchDashboardStatisticsDto BuildStatisticsSummary(SysUserStatistics? statistics, DateOnly today)
    {
        if (statistics is null)
        {
            return new WorkbenchDashboardStatisticsDto
            {
                Period = StatisticsPeriod.Today,
                StatisticsDate = today
            };
        }

        return new WorkbenchDashboardStatisticsDto
        {
            AccessCount = statistics.AccessCount,
            ApiCallCount = statistics.ApiCallCount,
            ErrorOperationCount = statistics.ErrorOperationCount,
            LastAccessTime = statistics.LastAccessTime,
            LastLoginTime = statistics.LastLoginTime,
            LastOperationTime = statistics.LastOperationTime,
            LoginCount = statistics.LoginCount,
            OnlineTime = statistics.OnlineTime,
            OperationCount = statistics.OperationCount,
            Period = statistics.Period,
            StatisticsDate = statistics.StatisticsDate
        };
    }

    private static WorkbenchInboxSummaryDto BuildInboxSummary(IReadOnlyCollection<UserInboxItemDto> inboxItems)
    {
        return new WorkbenchInboxSummaryDto
        {
            LatestItems = [.. inboxItems
                .OrderByDescending(item => item.SendTime)
                .ThenByDescending(item => item.BasicId)
                .Take(LatestInboxCount)],
            PendingConfirmCount = inboxItems.Count(item => item.NeedConfirm && !item.ConfirmTime.HasValue),
            UnreadCount = inboxItems.Count(item => item.NotificationStatus == (int)NotificationStatus.Unread)
        };
    }
}
