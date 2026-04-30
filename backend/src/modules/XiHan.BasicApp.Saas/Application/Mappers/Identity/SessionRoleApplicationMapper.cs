#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SessionRoleApplicationMapper
// Guid:22726f94-c3df-4066-9e5e-2f4dbdcef6e6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 会话角色应用层映射器
/// </summary>
public static class SessionRoleApplicationMapper
{
    /// <summary>
    /// 映射会话角色列表项
    /// </summary>
    /// <param name="sessionRole">会话角色实体</param>
    /// <param name="session">用户会话实体</param>
    /// <param name="role">角色实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>会话角色列表项 DTO</returns>
    public static SessionRoleListItemDto ToListItemDto(
        SysSessionRole sessionRole,
        SysUserSession? session,
        SysRole? role,
        SysUser? user,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sessionRole);

        return new SessionRoleListItemDto
        {
            BasicId = sessionRole.BasicId,
            SessionId = sessionRole.SessionId,
            UserSessionId = session?.UserSessionId,
            UserId = session?.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            RoleId = sessionRole.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            ActivatedAt = sessionRole.ActivatedAt,
            DeactivatedAt = sessionRole.DeactivatedAt,
            ExpiresAt = sessionRole.ExpiresAt,
            IsExpired = IsExpired(sessionRole, now),
            Status = sessionRole.Status,
            CreatedTime = sessionRole.CreatedTime
        };
    }

    /// <summary>
    /// 映射会话角色详情
    /// </summary>
    /// <param name="sessionRole">会话角色实体</param>
    /// <param name="session">用户会话实体</param>
    /// <param name="role">角色实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>会话角色详情 DTO</returns>
    public static SessionRoleDetailDto ToDetailDto(
        SysSessionRole sessionRole,
        SysUserSession? session,
        SysRole? role,
        SysUser? user,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sessionRole);

        var item = ToListItemDto(sessionRole, session, role, user, now);
        return new SessionRoleDetailDto
        {
            BasicId = item.BasicId,
            SessionId = item.SessionId,
            UserSessionId = item.UserSessionId,
            UserId = item.UserId,
            UserName = item.UserName,
            RealName = item.RealName,
            NickName = item.NickName,
            RoleId = item.RoleId,
            RoleCode = item.RoleCode,
            RoleName = item.RoleName,
            ActivatedAt = item.ActivatedAt,
            DeactivatedAt = item.DeactivatedAt,
            ExpiresAt = item.ExpiresAt,
            IsExpired = item.IsExpired,
            Status = item.Status,
            Reason = sessionRole.Reason,
            CreatedTime = item.CreatedTime,
            CreatedId = sessionRole.CreatedId,
            CreatedBy = sessionRole.CreatedBy
        };
    }

    /// <summary>
    /// 判断会话角色是否已过期
    /// </summary>
    private static bool IsExpired(SysSessionRole sessionRole, DateTimeOffset now)
    {
        return sessionRole.ExpiresAt.HasValue && sessionRole.ExpiresAt.Value <= now;
    }
}
