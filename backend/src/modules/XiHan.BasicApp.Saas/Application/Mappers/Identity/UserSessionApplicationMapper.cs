#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionApplicationMapper
// Guid:3572b674-5574-4b25-9c78-3f987eebf420
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户会话应用层映射器
/// </summary>
public static class UserSessionApplicationMapper
{
    /// <summary>
    /// 映射用户会话列表项
    /// </summary>
    /// <param name="session">用户会话实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>用户会话列表项 DTO</returns>
    public static UserSessionListItemDto ToListItemDto(SysUserSession session, SysUser? user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new UserSessionListItemDto
        {
            BasicId = session.BasicId,
            UserId = session.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            UserSessionId = session.UserSessionId,
            DeviceType = session.DeviceType,
            DeviceName = session.DeviceName,
            DeviceIdMasked = MaskIdentifier(session.DeviceId),
            OperatingSystem = session.OperatingSystem,
            Browser = session.Browser,
            IpAddressMasked = MaskIpAddress(session.IpAddress),
            LoginTime = session.LoginTime,
            LastActivityTime = session.LastActivityTime,
            IsOnline = session.IsOnline,
            IsRevoked = session.IsRevoked,
            RevokedAt = session.RevokedAt,
            LogoutTime = session.LogoutTime,
            ExpiresAt = session.ExpiresAt,
            IsExpired = IsExpired(session, now),
            CreatedTime = session.CreatedTime,
            ModifiedTime = session.ModifiedTime
        };
    }

    /// <summary>
    /// 映射用户会话详情
    /// </summary>
    /// <param name="session">用户会话实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>用户会话详情 DTO</returns>
    public static UserSessionDetailDto ToDetailDto(SysUserSession session, SysUser? user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(session);

        var item = ToListItemDto(session, user, now);
        return new UserSessionDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            RealName = item.RealName,
            NickName = item.NickName,
            UserSessionId = item.UserSessionId,
            DeviceType = item.DeviceType,
            DeviceName = item.DeviceName,
            DeviceIdMasked = item.DeviceIdMasked,
            OperatingSystem = item.OperatingSystem,
            Browser = item.Browser,
            IpAddressMasked = item.IpAddressMasked,
            LoginTime = item.LoginTime,
            LastActivityTime = item.LastActivityTime,
            IsOnline = item.IsOnline,
            IsRevoked = item.IsRevoked,
            RevokedAt = item.RevokedAt,
            RevokedReason = session.RevokedReason,
            LogoutTime = item.LogoutTime,
            ExpiresAt = item.ExpiresAt,
            IsExpired = item.IsExpired,
            Remark = session.Remark,
            CreatedTime = item.CreatedTime,
            CreatedId = session.CreatedId,
            CreatedBy = session.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = session.ModifiedId,
            ModifiedBy = session.ModifiedBy
        };
    }

    /// <summary>
    /// 判断会话是否过期
    /// </summary>
    private static bool IsExpired(SysUserSession session, DateTimeOffset now)
    {
        return session.ExpiresAt.HasValue && session.ExpiresAt.Value <= now;
    }

    /// <summary>
    /// 脱敏 IP 地址
    /// </summary>
    private static string? MaskIpAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var ipv4Parts = trimmed.Split('.');
        if (ipv4Parts.Length == 4 && ipv4Parts.All(part => int.TryParse(part, out _)))
        {
            return $"{ipv4Parts[0]}.{ipv4Parts[1]}.{ipv4Parts[2]}.*";
        }

        var ipv6Parts = trimmed.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (ipv6Parts.Length > 2)
        {
            return $"{ipv6Parts[0]}:{ipv6Parts[1]}:****";
        }

        return MaskIdentifier(trimmed);
    }

    /// <summary>
    /// 脱敏标识符
    /// </summary>
    private static string? MaskIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= 8)
        {
            return "****";
        }

        return $"{trimmed[..4]}****{trimmed[^4..]}";
    }
}
