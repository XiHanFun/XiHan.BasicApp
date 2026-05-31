#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileDtos
// Guid:7cdd6a96-25c1-46cf-b635-cc0106552538
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 当前用户个人资料 DTO
/// </summary>
public sealed class UserProfileDto
{
    /// <summary>
    /// 用户主键
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
    /// 性别
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
    /// 国家或地区
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 当前租户
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录 IP
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 是否系统账号
    /// </summary>
    public bool IsSystemAccount { get; set; }

    /// <summary>
    /// 是否启用双因素认证
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// 双因素方式位标识
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
    /// 是否可修改用户名
    /// </summary>
    public bool CanChangeUserName { get; set; }

    /// <summary>
    /// 是否已锁定
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// 锁定结束时间（为空表示未设置自动解锁）
    /// </summary>
    public DateTimeOffset? LockoutEndTime { get; set; }

    /// <summary>
    /// 连续失败登录次数
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// 最后失败登录时间
    /// </summary>
    public DateTimeOffset? LastFailedLoginTime { get; set; }
}

/// <summary>
/// 更新个人资料 DTO
/// </summary>
public sealed class ProfileUpdateDto
{
    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public int? Gender { get; set; }

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
    /// 国家或地区
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 修改密码 DTO
/// </summary>
public sealed class ProfileChangePasswordDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 旧密码
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 修改用户名 DTO
/// </summary>
public sealed class ProfileChangeUserNameDto
{
    /// <summary>
    /// 新用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 当前密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 修改邮箱 DTO
/// </summary>
public sealed class ProfileChangeEmailDto
{
    /// <summary>
    /// 新邮箱
    /// </summary>
    public string NewEmail { get; set; } = string.Empty;

    /// <summary>
    /// 当前密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 修改手机 DTO
/// </summary>
public sealed class ProfileChangePhoneDto
{
    /// <summary>
    /// 新手机号
    /// </summary>
    public string NewPhone { get; set; } = string.Empty;

    /// <summary>
    /// 当前密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 验证码 DTO
/// </summary>
public sealed class ProfileVerificationCodeDto
{
    /// <summary>
    /// 验证码
    /// </summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 双因素方式 DTO
/// </summary>
public sealed class ProfileTwoFactorMethodDto
{
    /// <summary>
    /// 双因素方式位标识
    /// </summary>
    public int Method { get; set; }
}

/// <summary>
/// 双因素校验 DTO
/// </summary>
public sealed class ProfileTwoFactorVerifyDto
{
    /// <summary>
    /// 双因素方式位标识
    /// </summary>
    public int Method { get; set; }

    /// <summary>
    /// 验证码
    /// </summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// TOTP 设置结果 DTO
/// </summary>
public sealed class ProfileTwoFactorSetupDto
{
    /// <summary>
    /// 共享密钥
    /// </summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>
    /// Authenticator URI
    /// </summary>
    public string AuthenticatorUri { get; set; } = string.Empty;
}

/// <summary>
/// 验证码发送结果 DTO
/// </summary>
public sealed class ProfileVerificationCodeResultDto
{
    /// <summary>
    /// 过期秒数
    /// </summary>
    public int ExpiresInSeconds { get; set; }
}

/// <summary>
/// 当前用户会话 DTO
/// </summary>
public sealed class ProfileSessionDto
{
    /// <summary>
    /// 业务会话标识
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 设备类型
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
    /// IP 地址
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
    /// 是否当前会话
    /// </summary>
    public bool IsCurrent { get; set; }
}

/// <summary>
/// 会话撤销 DTO
/// </summary>
public sealed class ProfileSessionRevokeDto
{
    /// <summary>
    /// 业务会话标识
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// 登录日志项 DTO
/// </summary>
public sealed class ProfileLoginLogItemDto
{
    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 登录 IP
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// 登录位置
    /// </summary>
    public string? LoginLocation { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 登录结果
    /// </summary>
    public int LoginResult { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// 登录日志分页 DTO
/// </summary>
public sealed class ProfileLoginLogPageDto
{
    /// <summary>
    /// 登录日志列表
    /// </summary>
    public List<ProfileLoginLogItemDto> Items { get; set; } = [];

    /// <summary>
    /// 总数
    /// </summary>
    public int Total { get; set; }
}

/// <summary>
/// 第三方账号绑定 DTO
/// </summary>
public sealed class ProfileExternalLoginDto
{
    /// <summary>
    /// 提供商
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 提供商显示名称
    /// </summary>
    public string? ProviderDisplayName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }
}

/// <summary>
/// 第三方账号解绑 DTO
/// </summary>
public sealed class ProfileUnlinkAccountDto
{
    /// <summary>
    /// 提供商
    /// </summary>
    public string Provider { get; set; } = string.Empty;
}

/// <summary>
/// 账号密码确认 DTO
/// </summary>
public sealed class ProfilePasswordConfirmDto
{
    /// <summary>
    /// 当前密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 当前用户活跃度周期统计 DTO
/// </summary>
public sealed class ProfileActivityPeriodDto
{
    /// <summary>
    /// 登录次数
    /// </summary>
    public int LoginCount { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// 操作次数
    /// </summary>
    public int OperationCount { get; set; }

    /// <summary>
    /// 在线时长（秒）
    /// </summary>
    public long OnlineTime { get; set; }
}

/// <summary>
/// 当前用户活跃度趋势点 DTO
/// </summary>
public sealed class ProfileActivityTrendPointDto
{
    /// <summary>
    /// 统计日期
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// 操作次数
    /// </summary>
    public int OperationCount { get; set; }

    /// <summary>
    /// 在线时长（分钟）
    /// </summary>
    public long OnlineMinutes { get; set; }
}

/// <summary>
/// 当前用户活跃度统计 DTO
/// </summary>
public sealed class ProfileActivityDto
{
    /// <summary>
    /// 今日活跃度
    /// </summary>
    public ProfileActivityPeriodDto Today { get; set; } = new();

    /// <summary>
    /// 本周活跃度
    /// </summary>
    public ProfileActivityPeriodDto ThisWeek { get; set; } = new();

    /// <summary>
    /// 本月活跃度
    /// </summary>
    public ProfileActivityPeriodDto ThisMonth { get; set; } = new();

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; set; }

    /// <summary>
    /// 最后操作时间
    /// </summary>
    public DateTimeOffset? LastOperationTime { get; set; }

    /// <summary>
    /// 最近若干天的每日趋势（按日期升序）
    /// </summary>
    public List<ProfileActivityTrendPointDto> Trend { get; set; } = [];
}

/// <summary>
/// 用户通知偏好 DTO（读 + 写共用）
/// </summary>
public sealed class ProfileNotificationPreferenceDto
{
    /// <summary>
    /// 站内信通知
    /// </summary>
    public bool ChannelInApp { get; set; } = true;

    /// <summary>
    /// 邮箱通知
    /// </summary>
    public bool ChannelEmail { get; set; } = true;

    /// <summary>
    /// 短信通知
    /// </summary>
    public bool ChannelSms { get; set; }

    /// <summary>
    /// 推送通知
    /// </summary>
    public bool ChannelPush { get; set; } = true;

    /// <summary>
    /// 系统公告
    /// </summary>
    public bool TypeAnnouncement { get; set; } = true;

    /// <summary>
    /// 任务提醒
    /// </summary>
    public bool TypeTask { get; set; } = true;

    /// <summary>
    /// 审批通知
    /// </summary>
    public bool TypeApproval { get; set; } = true;

    /// <summary>
    /// 安全告警
    /// </summary>
    public bool TypeSecurity { get; set; } = true;

    /// <summary>
    /// 营销消息
    /// </summary>
    public bool TypeMarketing { get; set; }
}
