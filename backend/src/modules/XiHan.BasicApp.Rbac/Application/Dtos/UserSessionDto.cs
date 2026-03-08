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
    /// <summary>
    /// 用户标识
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 会话标识
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTimeOffset LastActivityTime { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 登出时间
    /// </summary>
    public DateTimeOffset? LogoutTime { get; set; }
}

/// <summary>
/// 创建用户会话 DTO
/// </summary>
public class UserSessionCreateDto : BasicAppCDto
{
    /// <summary>
    /// 用户标识
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "用户ID无效")]
    public long UserId { get; set; }

    /// <summary>
    /// 当前 AccessToken JTI
    /// </summary>
    [StringLength(200, ErrorMessage = "AccessToken JTI 长度不能超过 200")]
    public string? CurrentAccessTokenJti { get; set; }

    /// <summary>
    /// 会话标识
    /// </summary>
    [Required(ErrorMessage = "会话ID不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "会话ID长度必须在 1～100 之间")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备名称
    /// </summary>
    [StringLength(200, ErrorMessage = "设备名称长度不能超过 200")]
    public string? DeviceName { get; set; }

    /// <summary>
    /// 设备标识
    /// </summary>
    [StringLength(200, ErrorMessage = "设备ID长度不能超过 200")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [StringLength(100, ErrorMessage = "操作系统长度不能超过 100")]
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    [StringLength(100, ErrorMessage = "浏览器长度不能超过 100")]
    public string? Browser { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    [StringLength(50, ErrorMessage = "IP 地址长度不能超过 50")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 登录位置
    /// </summary>
    [StringLength(200, ErrorMessage = "登录位置长度不能超过 200")]
    public string? Location { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTimeOffset LastActivityTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>
    /// 租户标识
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新用户会话 DTO
/// </summary>
public class UserSessionUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 当前 AccessToken JTI
    /// </summary>
    [StringLength(200, ErrorMessage = "AccessToken JTI 长度不能超过 200")]
    public string? CurrentAccessTokenJti { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备名称
    /// </summary>
    [StringLength(200, ErrorMessage = "设备名称长度不能超过 200")]
    public string? DeviceName { get; set; }

    /// <summary>
    /// 设备标识
    /// </summary>
    [StringLength(200, ErrorMessage = "设备ID长度不能超过 200")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [StringLength(100, ErrorMessage = "操作系统长度不能超过 100")]
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    [StringLength(100, ErrorMessage = "浏览器长度不能超过 100")]
    public string? Browser { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    [StringLength(50, ErrorMessage = "IP 地址长度不能超过 50")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 登录位置
    /// </summary>
    [StringLength(200, ErrorMessage = "登录位置长度不能超过 200")]
    public string? Location { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTimeOffset LastActivityTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    [StringLength(200, ErrorMessage = "撤销原因长度不能超过 200")]
    public string? RevokedReason { get; set; }

    /// <summary>
    /// 登出时间
    /// </summary>
    public DateTimeOffset? LogoutTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
