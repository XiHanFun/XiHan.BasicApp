#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthDtos
// Guid:61a50ab3-7a4b-48ab-a4b9-955943795a36
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 登录配置 DTO
/// </summary>
public sealed class LoginConfigDto
{
    /// <summary>
    /// 支持的登录方式
    /// </summary>
    public List<string> LoginMethods { get; set; } = ["password"];

    /// <summary>
    /// OAuth 提供商
    /// </summary>
    public List<OAuthProviderItemDto> OAuthProviders { get; set; } = [];
}

/// <summary>
/// OAuth 提供商 DTO
/// </summary>
public sealed class OAuthProviderItemDto
{
    /// <summary>
    /// 提供商名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// 登录请求 DTO
/// </summary>
public sealed class LoginRequestDto
{
    /// <summary>
    /// 登录账号（邮箱，全平台唯一；平台账号也可使用用户名）
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 双因素验证码
    /// </summary>
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// 双因素方式
    /// </summary>
    public string? TwoFactorMethod { get; set; }

    /// <summary>
    /// 设备标识
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// 登录响应 DTO
/// </summary>
public sealed class LoginResponseDto
{
    /// <summary>
    /// 是否需要双因素认证
    /// </summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// 可用双因素方式
    /// </summary>
    public List<string>? AvailableTwoFactorMethods { get; set; }

    /// <summary>
    /// 当前双因素方式
    /// </summary>
    public string? TwoFactorMethod { get; set; }

    /// <summary>
    /// 验证码是否已发送
    /// </summary>
    public bool? CodeSent { get; set; }

    /// <summary>
    /// 登录令牌
    /// </summary>
    public LoginTokenDto? Token { get; set; }
}

/// <summary>
/// 登录令牌 DTO
/// </summary>
public sealed class LoginTokenDto
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 过期秒数
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 签发时间
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 邮箱登录验证码请求 DTO
/// </summary>
public sealed class EmailLoginCodeRequestDto
{
    /// <summary>
    /// 邮箱地址（全平台唯一的登录身份标识）
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// 邮箱验证码登录请求 DTO
/// </summary>
public sealed class EmailLoginRequestDto
{
    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱验证码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 设备标识
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// 验证码下发结果 DTO
/// </summary>
public sealed class VerificationCodeResultDto
{
    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    public int ExpiresInSeconds { get; set; }

    /// <summary>
    /// 调试验证码，仅在未接入真实邮件/短信通道时回显，便于联调；生产接入真实通道后应置空
    /// </summary>
    public string? DebugCode { get; set; }
}

/// <summary>
/// 切换租户请求 DTO
/// </summary>
public sealed class SwitchTenantRequestDto
{
    /// <summary>
    /// 目标租户标识；为空或 0 表示切换到平台运维态（无租户上下文）
    /// </summary>
    /// <remarks>切换复用当前登录会话（设备信息随会话保留），不需要设备标识</remarks>
    public long? TenantId { get; set; }
}

/// <summary>
/// 刷新令牌请求 DTO
/// </summary>
public sealed class RefreshTokenRequestDto
{
    /// <summary>
    /// 原访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 注册请求 DTO
/// </summary>
public sealed class RegisterRequestDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 昵称（缺省时取用户名）
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 邮箱（必填，全平台唯一的登录身份标识）
    /// </summary>
    public string? Email { get; set; }
}

/// <summary>
/// 注册结果 DTO
/// </summary>
public sealed class RegisterResultDto
{
    /// <summary>
    /// 新建用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 新建用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// 密码重置请求 DTO
/// </summary>
public sealed class PasswordResetRequestDto
{
    /// <summary>
    /// 注册邮箱（全平台唯一，按邮箱全局定位账号）
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// 密码重置申请结果 DTO
/// </summary>
public sealed class PasswordResetResultDto
{
    /// <summary>
    /// 请求是否已受理（为防用户枚举，邮箱不存在也返回受理）
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// 一次性重置链接，仅开发环境回显便于本地联调；生产绝不回显
    /// </summary>
    public string? DebugResetUrl { get; set; }
}

/// <summary>
/// 密码重置确认（消费一次性链接并设置新密码）请求 DTO
/// </summary>
public sealed class PasswordResetConfirmDto
{
    /// <summary>
    /// 一次性重置令牌（来自找回密码邮件链接）
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 密码重置确认结果 DTO
/// </summary>
public sealed class PasswordResetConfirmResultDto
{
    /// <summary>
    /// 是否重置成功
    /// </summary>
    public bool Success { get; set; }
}

/// <summary>
/// 当前用户信息 DTO
/// </summary>
public sealed class UserInfoDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 应用标题
    /// </summary>
    public string? AppTitle { get; set; } = "XiHan BasicApp";

    /// <summary>
    /// 应用 Logo
    /// </summary>
    public string? AppLogo { get; set; }

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
    /// 当前租户（为空表示处于平台运维态）
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 是否处于平台运维态（无租户上下文）
    /// </summary>
    public bool IsPlatform { get; set; }

    /// <summary>
    /// 是否可进入平台运维态（超管 / 平台管理员）
    /// </summary>
    public bool CanAccessPlatform { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// 权限编码
    /// </summary>
    public List<string> Permissions { get; set; } = [];
}

/// <summary>
/// 权限信息 DTO
/// </summary>
public sealed class PermissionInfoDto
{
    /// <summary>
    /// 角色编码
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// 权限编码
    /// </summary>
    public List<string> Permissions { get; set; } = [];

    /// <summary>
    /// 菜单路由
    /// </summary>
    public List<MenuRouteDto> Menus { get; set; } = [];
}

/// <summary>
/// 菜单路由 DTO
/// </summary>
public sealed class MenuRouteDto
{
    /// <summary>
    /// 主键
    /// </summary>
    public string? BasicId { get; set; }

    /// <summary>
    /// 路由路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 路由名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 组件路径
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 重定向路径
    /// </summary>
    public string? Redirect { get; set; }

    /// <summary>
    /// 路由元数据
    /// </summary>
    public MenuMetaDto Meta { get; set; } = new();

    /// <summary>
    /// 子路由
    /// </summary>
    public List<MenuRouteDto>? Children { get; set; }
}

/// <summary>
/// 菜单元数据 DTO
/// </summary>
public sealed class MenuMetaDto
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    public bool? Hidden { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool? KeepAlive { get; set; }

    /// <summary>
    /// 是否固定标签
    /// </summary>
    public bool? AffixTab { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    public List<string>? Roles { get; set; }

    /// <summary>
    /// 权限
    /// </summary>
    public List<string>? Permissions { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// 徽标
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>
    /// 徽标类型
    /// </summary>
    public string? BadgeType { get; set; }

    /// <summary>
    /// 是否圆点徽标
    /// </summary>
    public bool? Dot { get; set; }

    /// <summary>
    /// 外链
    /// </summary>
    public string? Link { get; set; }
}
