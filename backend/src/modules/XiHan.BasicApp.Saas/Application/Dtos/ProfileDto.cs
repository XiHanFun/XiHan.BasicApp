#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileDto
// Guid:e3f4a5b6-7c8d-9e0f-1a2b-3c4d5e6f7a8b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户个人档案（完整信息，用于个人中心）
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 性别（0未知 1男 2女）
    /// </summary>
    public int Gender { get; set; }

    /// <summary>
    /// 生日
    /// </summary>
    public DateTimeOffset? Birthday { get; set; }

    /// <summary>
    /// 时区
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// 国家/地区
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 个人简介
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录IP
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 是否为系统内置账号
    /// </summary>
    public bool IsSystemAccount { get; set; }

    /// <summary>
    /// 是否启用双因素认证
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// 双因素认证方式（Flags 位掩码：0=无 1=TOTP 2=邮箱 4=手机）
    /// </summary>
    public int TwoFactorMethod { get; set; }

    /// <summary>
    /// 邮箱是否验证
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// 手机号是否验证
    /// </summary>
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// 最后修改密码时间
    /// </summary>
    public DateTimeOffset? LastPasswordChangeTime { get; set; }

    /// <summary>
    /// 最后修改用户名时间
    /// </summary>
    public DateTimeOffset? LastUserNameChangeTime { get; set; }

    /// <summary>
    /// 是否可以修改用户名（非系统账号且满足 90 天冷却期）
    /// </summary>
    public bool CanChangeUserName { get; set; }
}

/// <summary>
/// 用户登录会话信息
/// </summary>
public class UserSessionItemDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 设备类型（0未知 1Web 2Mobile 3Desktop ...）
    /// </summary>
    public int DeviceType { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 登录位置
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTimeOffset LastActivityTime { get; set; }

    /// <summary>
    /// 是否是当前会话
    /// </summary>
    public bool IsCurrent { get; set; }
}

/// <summary>
/// 双因素认证设置结果
/// </summary>
public class TwoFactorSetupResultDto
{
    /// <summary>
    /// 共享密钥（Base32，供手动输入）
    /// </summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>
    /// 认证器 URI（otpauth:// 格式，供二维码扫描）
    /// </summary>
    public string AuthenticatorUri { get; set; } = string.Empty;
}

/// <summary>
/// 第三方登录绑定信息
/// </summary>
public class ExternalLoginItemDto
{
    /// <summary>
    /// 提供商标识（google、github 等）
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 提供商显示名称
    /// </summary>
    public string? ProviderDisplayName { get; set; }

    /// <summary>
    /// 三方邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 三方头像
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }
}
