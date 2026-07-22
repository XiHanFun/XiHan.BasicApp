// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户统计应用层映射器
/// </summary>
public static class UserStatisticsApplicationMapper
{
    /// <summary>
    /// 映射用户统计列表项
    /// </summary>
    /// <param name="statistics">用户统计实体</param>
    /// <param name="user">用户实体</param>
    /// <returns>用户统计列表项 DTO</returns>
    public static UserStatisticsListItemDto ToListItemDto(SysUserStatistics statistics, SysUser? user)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        return new UserStatisticsListItemDto
        {
            BasicId = statistics.BasicId,
            UserId = statistics.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            StatisticsDate = statistics.StatisticsDate,
            Period = statistics.Period,
            LoginCount = statistics.LoginCount,
            AccessCount = statistics.AccessCount,
            OnlineTime = statistics.OnlineTime,
            OperationCount = statistics.OperationCount,
            ApiCallCount = statistics.ApiCallCount,
            ErrorOperationCount = statistics.ErrorOperationCount,
            LastLoginTime = statistics.LastLoginTime,
            LastAccessTime = statistics.LastAccessTime,
            LastOperationTime = statistics.LastOperationTime,
            CreatedTime = statistics.CreatedTime,
            ModifiedTime = statistics.ModifiedTime
        };
    }

    /// <summary>
    /// 映射用户统计详情
    /// </summary>
    /// <param name="statistics">用户统计实体</param>
    /// <param name="user">用户实体</param>
    /// <returns>用户统计详情 DTO</returns>
    public static UserStatisticsDetailDto ToDetailDto(SysUserStatistics statistics, SysUser? user)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        var item = ToListItemDto(statistics, user);
        return new UserStatisticsDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            RealName = item.RealName,
            NickName = item.NickName,
            StatisticsDate = item.StatisticsDate,
            Period = item.Period,
            LoginCount = item.LoginCount,
            AccessCount = item.AccessCount,
            OnlineTime = item.OnlineTime,
            OperationCount = item.OperationCount,
            ApiCallCount = item.ApiCallCount,
            ErrorOperationCount = item.ErrorOperationCount,
            LastLoginTime = item.LastLoginTime,
            LastAccessTime = item.LastAccessTime,
            LastOperationTime = item.LastOperationTime,
            FileUploadCount = statistics.FileUploadCount,
            FileDownloadCount = statistics.FileDownloadCount,
            EmailSentCount = statistics.EmailSentCount,
            SmsSentCount = statistics.SmsSentCount,
            NotificationSentCount = statistics.NotificationSentCount,
            NotificationReceivedCount = statistics.NotificationReceivedCount,
            Remark = statistics.Remark,
            CreatedTime = item.CreatedTime,
            CreatedId = statistics.CreatedId,
            CreatedBy = statistics.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = statistics.ModifiedId,
            ModifiedBy = statistics.ModifiedBy
        };
    }
}
