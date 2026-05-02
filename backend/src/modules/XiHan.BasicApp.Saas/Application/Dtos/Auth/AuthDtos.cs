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
    /// 是否启用租户选择
    /// </summary>
    public bool TenantEnabled { get; set; } = true;

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
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 初始登录租户标识，仅用于登录前建立租户上下文
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 当前租户
    /// </summary>
    public long? TenantId { get; set; }

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
