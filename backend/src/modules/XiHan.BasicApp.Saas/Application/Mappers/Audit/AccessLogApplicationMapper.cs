#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AccessLogApplicationMapper
// Guid:2f1031d7-0c66-4e6d-9b5c-c81b4e5b641e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 访问日志应用层映射器
/// </summary>
public static class AccessLogApplicationMapper
{
    /// <summary>
    /// 映射访问日志列表项
    /// </summary>
    /// <param name="accessLog">访问日志实体</param>
    /// <returns>访问日志列表项 DTO</returns>
    public static AccessLogListItemDto ToListItemDto(SysAccessLog accessLog)
    {
        ArgumentNullException.ThrowIfNull(accessLog);

        return new AccessLogListItemDto
        {
            BasicId = accessLog.BasicId,
            UserId = accessLog.UserId,
            UserName = accessLog.UserName,
            SessionId = accessLog.SessionId,
            TraceId = accessLog.TraceId,
            ResourcePath = accessLog.ResourcePath,
            ResourceName = accessLog.ResourceName,
            ResourceType = accessLog.ResourceType,
            Method = accessLog.Method,
            AccessResult = accessLog.AccessResult,
            StatusCode = accessLog.StatusCode,
            ExecutionTime = accessLog.ExecutionTime,
            AccessTime = accessLog.AccessTime,
            HasClientContext = HasClientContext(accessLog),
            HasError = !string.IsNullOrWhiteSpace(accessLog.ErrorMessage),
            HasExtension = !string.IsNullOrWhiteSpace(accessLog.ExtendData),
            CreatedTime = accessLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射访问日志详情
    /// </summary>
    /// <param name="accessLog">访问日志实体</param>
    /// <returns>访问日志详情 DTO</returns>
    public static AccessLogDetailDto ToDetailDto(SysAccessLog accessLog)
    {
        ArgumentNullException.ThrowIfNull(accessLog);

        var item = ToListItemDto(accessLog);
        return new AccessLogDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            SessionId = item.SessionId,
            TraceId = item.TraceId,
            ResourcePath = item.ResourcePath,
            ResourceName = item.ResourceName,
            ResourceType = item.ResourceType,
            Method = item.Method,
            AccessResult = item.AccessResult,
            StatusCode = item.StatusCode,
            ExecutionTime = item.ExecutionTime,
            AccessTime = item.AccessTime,
            HasClientContext = item.HasClientContext,
            HasError = item.HasError,
            HasExtension = item.HasExtension,
            CreatedTime = item.CreatedTime,
            CreatedId = accessLog.CreatedId,
            CreatedBy = accessLog.CreatedBy
        };
    }

    /// <summary>
    /// 判断是否存在客户端上下文
    /// </summary>
    /// <param name="accessLog">访问日志实体</param>
    /// <returns>是否存在客户端上下文</returns>
    private static bool HasClientContext(SysAccessLog accessLog)
    {
        return !string.IsNullOrWhiteSpace(accessLog.AccessIp) ||
               !string.IsNullOrWhiteSpace(accessLog.AccessLocation) ||
               !string.IsNullOrWhiteSpace(accessLog.UserAgent) ||
               !string.IsNullOrWhiteSpace(accessLog.Browser) ||
               !string.IsNullOrWhiteSpace(accessLog.Os) ||
               !string.IsNullOrWhiteSpace(accessLog.Device) ||
               !string.IsNullOrWhiteSpace(accessLog.Referer);
    }
}
