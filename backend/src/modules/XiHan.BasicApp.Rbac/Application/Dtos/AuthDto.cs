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

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 登录页配置
/// </summary>
public class LoginConfigDto
{
    /// <summary>
    /// 是否启用验证码
    /// </summary>
    public bool CaptchaEnabled { get; set; } = true;

    /// <summary>
    /// 登录方式
    /// </summary>
    public string[] LoginMethods { get; set; } = ["password"];

    /// <summary>
    /// 是否启用租户
    /// </summary>
    public bool TenantEnabled { get; set; } = true;

    /// <summary>
    /// 三方登录提供商
    /// </summary>
    public string[] OauthProviders { get; set; } = [];
}

/// <summary>
/// 验证码信息
/// </summary>
public class CaptchaDto
{
    /// <summary>
    /// 验证码ID
    /// </summary>
    public string CaptchaId { get; set; } = string.Empty;

    /// <summary>
    /// Base64 图片
    /// </summary>
    public string ImageBase64 { get; set; } = string.Empty;
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
}

/// <summary>
/// 当前用户信息
/// </summary>
public class CurrentUserDto
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
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
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
}
