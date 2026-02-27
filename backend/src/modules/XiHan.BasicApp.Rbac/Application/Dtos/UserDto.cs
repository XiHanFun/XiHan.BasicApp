#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDto
// Guid:c143c7ce-6962-477a-bfba-b0d802c1ed12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:42:22
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 用户 DTO
/// </summary>
public class UserDto : BasicAppDto
{
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
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 创建用户 DTO
/// </summary>
public class UserCreateDto : BasicAppCDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "用户名长度必须在 1～50 之间")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 更新用户 DTO
/// </summary>
public class UserUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 登录结果 DTO
/// </summary>
public class UserLoginResultDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 登录结果
    /// </summary>
    public LoginResult LoginResult { get; set; } = LoginResult.Failed;

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 权限代码
    /// </summary>
    public IReadOnlyCollection<string> PermissionCodes { get; set; } = [];
}
