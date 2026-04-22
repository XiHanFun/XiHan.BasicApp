#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthDto
// Guid:9d3dd138-6d6e-4a79-ad30-2c06dd6e8e87
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/07 10:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 登录页配置
/// </summary>
public class LoginConfigDto
{
    /// <summary>
    /// 登录方式
    /// </summary>
    public string[] LoginMethods { get; set; } = ["password"];

    /// <summary>
    /// 是否启用租户
    /// </summary>
    public bool TenantEnabled { get; set; } = true;

    /// <summary>
    /// 三方登录提供商详情
    /// </summary>
    public List<OAuthProviderItemDto> OauthProviders { get; set; } = [];
}

/// <summary>
/// OAuth 提供商信息（前端展示用）
/// </summary>
public class OAuthProviderItemDto
{
    /// <summary>
    /// 提供商标识
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应（统一封装，区分正常登录与双因素验证挑战）
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// 是否需要双因素验证（true 时前端应展示 OTP 输入）
    /// </summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// 可用的双因素认证方式列表（totp/email/phone），仅 RequiresTwoFactor 时有值
    /// </summary>
    public List<string>? AvailableTwoFactorMethods { get; set; }

    /// <summary>
    /// 当前选中的双因素方式（用户已选择后返回）
    /// </summary>
    public string? TwoFactorMethod { get; set; }

    /// <summary>
    /// 验证码是否已发送（邮箱/手机方式选中后为 true）
    /// </summary>
    public bool CodeSent { get; set; }

    /// <summary>
    /// 令牌信息（仅在登录完成时返回，需要双因素验证时为 null）
    /// </summary>
    public AuthTokenDto? Token { get; set; }
}

/// <summary>
/// 鉴权令牌结果
/// </summary>
public class AuthTokenDto
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
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 签发时间
    /// </summary>
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 当前会话租户ID。
    /// </summary>
    public long? CurrentTenantId { get; set; }

    /// <summary>
    /// 主归属租户ID。
    /// </summary>
    public long? HomeTenantId { get; set; }

    /// <summary>
    /// 会话ID。
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// 发送验证码结果
/// </summary>
public class AuthVerificationCodeDto
{
    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    public int ExpiresInSeconds { get; set; } = 300;

    /// <summary>
    /// 调试验证码（仅在开发模式或显式开启时返回）
    /// </summary>
    public string? DebugCode { get; set; }
}

/// <summary>
/// 找回密码结果
/// </summary>
public class PasswordResetResultDto
{
    /// <summary>
    /// 是否已受理
    /// </summary>
    public bool Accepted { get; set; } = true;

    /// <summary>
    /// 临时密码（仅在开发模式或显式开启时返回）
    /// </summary>
    public string? TemporaryPassword { get; set; }
}

/// <summary>
/// 当前用户信息
/// </summary>
public class CurrentUserDto
{
    /// <summary>
    /// 用户基础ID
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
    /// 当前会话租户ID
    /// </summary>
    public long? CurrentTenantId { get; set; }

    /// <summary>
    /// 主归属租户ID
    /// </summary>
    public long? HomeTenantId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// 权限上下文信息
/// </summary>
public class AuthPermissionDto
{
    /// <summary>
    /// 角色编码
    /// </summary>
    public string[] Roles { get; set; } = [];

    /// <summary>
    /// 权限编码
    /// </summary>
    public string[] Permissions { get; set; } = [];

    /// <summary>
    /// 菜单路由
    /// </summary>
    public IReadOnlyList<AuthMenuRouteDto> Menus { get; set; } = [];
}

public class FieldSecurityDecisionDto
{
    public string ResourceCode { get; set; } = string.Empty;

    public long ResourceId { get; set; }

    public IReadOnlyList<FieldSecurityFieldDecisionDto> Fields { get; set; } = [];
}

public class FieldSecurityFieldDecisionDto
{
    public string FieldName { get; set; } = string.Empty;

    public bool IsReadable { get; set; } = true;

    public bool IsEditable { get; set; } = true;

    public XiHan.BasicApp.Saas.Domain.Enums.FieldMaskStrategy MaskStrategy { get; set; }

    public string? MaskPattern { get; set; }

    public int Priority { get; set; }
}

/// <summary>
/// 鉴权菜单路由
/// </summary>
public class AuthMenuRouteDto
{
    /// <summary>
    /// 路由名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 路由路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 组件路径
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 重定向路径
    /// </summary>
    public string? Redirect { get; set; }

    /// <summary>
    /// 菜单权限标识
    /// </summary>
    public string? Permission { get; set; }

    /// <summary>
    /// 路由元数据
    /// </summary>
    public AuthMenuMetaDto Meta { get; set; } = new();

    /// <summary>
    /// 子菜单
    /// </summary>
    public List<AuthMenuRouteDto> Children { get; set; } = [];
}

/// <summary>
/// 鉴权菜单元数据
/// </summary>
public class AuthMenuMetaDto
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
    public bool Hidden { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool KeepAlive { get; set; }

    /// <summary>
    /// 是否固定标签
    /// </summary>
    public bool AffixTab { get; set; }

    /// <summary>
    /// 权限集合
    /// </summary>
    public string[] Permissions { get; set; } = [];

    /// <summary>
    /// 菜单排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 外链
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 标签内容
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>
    /// 标签类型
    /// </summary>
    public string? BadgeType { get; set; }

    /// <summary>
    /// 是否仅显示标签圆点
    /// </summary>
    public bool Dot { get; set; }
}
