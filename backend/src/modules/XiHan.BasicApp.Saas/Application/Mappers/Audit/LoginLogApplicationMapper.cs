#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogApplicationMapper
// Guid:b430be46-9ed1-4eb4-aee1-21606b119ce3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 登录日志应用层映射器
/// </summary>
public static class LoginLogApplicationMapper
{
    /// <summary>
    /// 映射登录日志列表项
    /// </summary>
    /// <param name="loginLog">登录日志实体</param>
    /// <returns>登录日志列表项 DTO</returns>
    public static LoginLogListItemDto ToListItemDto(SysLoginLog loginLog)
    {
        ArgumentNullException.ThrowIfNull(loginLog);

        return new LoginLogListItemDto
        {
            BasicId = loginLog.BasicId,
            UserId = loginLog.UserId,
            UserName = loginLog.UserName,
            SessionId = loginLog.SessionId,
            TraceId = loginLog.TraceId,
            LoginResult = loginLog.LoginResult,
            IsRiskLogin = loginLog.IsRiskLogin,
            LoginTime = loginLog.LoginTime,
            HasClientContext = HasClientContext(loginLog),
            HasDeviceContext = HasDeviceContext(loginLog),
            HasResultNote = !string.IsNullOrWhiteSpace(loginLog.Message),
            CreatedTime = loginLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射登录日志详情
    /// </summary>
    /// <param name="loginLog">登录日志实体</param>
    /// <returns>登录日志详情 DTO</returns>
    public static LoginLogDetailDto ToDetailDto(SysLoginLog loginLog)
    {
        ArgumentNullException.ThrowIfNull(loginLog);

        var item = ToListItemDto(loginLog);
        return new LoginLogDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            SessionId = item.SessionId,
            TraceId = item.TraceId,
            LoginResult = item.LoginResult,
            IsRiskLogin = item.IsRiskLogin,
            LoginTime = item.LoginTime,
            HasClientContext = item.HasClientContext,
            HasDeviceContext = item.HasDeviceContext,
            HasResultNote = item.HasResultNote,
            CreatedTime = item.CreatedTime,
            CreatedId = loginLog.CreatedId,
            CreatedBy = loginLog.CreatedBy
        };
    }

    /// <summary>
    /// 判断是否存在客户端上下文
    /// </summary>
    private static bool HasClientContext(SysLoginLog loginLog)
    {
        return !string.IsNullOrWhiteSpace(loginLog.LoginIp) ||
               !string.IsNullOrWhiteSpace(loginLog.LoginLocation) ||
               !string.IsNullOrWhiteSpace(loginLog.UserAgent) ||
               !string.IsNullOrWhiteSpace(loginLog.Browser) ||
               !string.IsNullOrWhiteSpace(loginLog.Os);
    }

    /// <summary>
    /// 判断是否存在设备上下文
    /// </summary>
    private static bool HasDeviceContext(SysLoginLog loginLog)
    {
        return !string.IsNullOrWhiteSpace(loginLog.Device) ||
               !string.IsNullOrWhiteSpace(loginLog.DeviceId);
    }
}
