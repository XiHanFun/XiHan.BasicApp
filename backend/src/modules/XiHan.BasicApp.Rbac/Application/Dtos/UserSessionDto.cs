#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionDto
// Guid:a2b498fe-a4ec-4524-bb14-6405b0dd104f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:33:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 用户会话 DTO
/// </summary>
public class UserSessionDto : BasicAppDto
{
    public long UserId { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    public string? DeviceName { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset LoginTime { get; set; }

    public DateTimeOffset LastActivityTime { get; set; }

    public bool IsOnline { get; set; } = true;

    public bool IsRevoked { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset? LogoutTime { get; set; }
}

/// <summary>
/// 创建用户会话 DTO
/// </summary>
public class UserSessionCreateDto : BasicAppCDto
{
    [Range(1, long.MaxValue, ErrorMessage = "用户ID无效")]
    public long UserId { get; set; }

    [StringLength(200, ErrorMessage = "AccessToken JTI 长度不能超过 200")]
    public string? CurrentAccessTokenJti { get; set; }

    [Required(ErrorMessage = "会话ID不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "会话ID长度必须在 1～100 之间")]
    public string SessionId { get; set; } = string.Empty;

    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    [StringLength(200, ErrorMessage = "设备名称长度不能超过 200")]
    public string? DeviceName { get; set; }

    [StringLength(200, ErrorMessage = "设备ID长度不能超过 200")]
    public string? DeviceId { get; set; }

    [StringLength(100, ErrorMessage = "操作系统长度不能超过 100")]
    public string? OperatingSystem { get; set; }

    [StringLength(100, ErrorMessage = "浏览器长度不能超过 100")]
    public string? Browser { get; set; }

    [StringLength(50, ErrorMessage = "IP 地址长度不能超过 50")]
    public string? IpAddress { get; set; }

    [StringLength(200, ErrorMessage = "登录位置长度不能超过 200")]
    public string? Location { get; set; }

    public DateTimeOffset LoginTime { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastActivityTime { get; set; } = DateTimeOffset.UtcNow;

    public bool IsOnline { get; set; } = true;

    public long? TenantId { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新用户会话 DTO
/// </summary>
public class UserSessionUpdateDto : BasicAppUDto
{
    [StringLength(200, ErrorMessage = "AccessToken JTI 长度不能超过 200")]
    public string? CurrentAccessTokenJti { get; set; }

    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    [StringLength(200, ErrorMessage = "设备名称长度不能超过 200")]
    public string? DeviceName { get; set; }

    [StringLength(200, ErrorMessage = "设备ID长度不能超过 200")]
    public string? DeviceId { get; set; }

    [StringLength(100, ErrorMessage = "操作系统长度不能超过 100")]
    public string? OperatingSystem { get; set; }

    [StringLength(100, ErrorMessage = "浏览器长度不能超过 100")]
    public string? Browser { get; set; }

    [StringLength(50, ErrorMessage = "IP 地址长度不能超过 50")]
    public string? IpAddress { get; set; }

    [StringLength(200, ErrorMessage = "登录位置长度不能超过 200")]
    public string? Location { get; set; }

    public DateTimeOffset LastActivityTime { get; set; } = DateTimeOffset.UtcNow;

    public bool IsOnline { get; set; } = true;

    public bool IsRevoked { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    [StringLength(200, ErrorMessage = "撤销原因长度不能超过 200")]
    public string? RevokedReason { get; set; }

    public DateTimeOffset? LogoutTime { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
