#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserLoginResponseDto
// Guid:709aa133-23e2-4db2-ab73-4a210bf95328
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>


#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserLoginResponseDto
// Guid:709aa133-23e2-4db2-ab73-4a210bf95328
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.ApplicationServices.Users.Dtos;

/// <summary>
/// 用户登录响应DTO
/// </summary>
public class UserLoginResponseDto
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
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 发行时间
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public SysUserDto User { get; set; } = null!;

    /// <summary>
    /// 用户角色列表
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 用户权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
