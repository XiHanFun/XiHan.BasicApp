#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserUpdateDto
// Guid:743aa764-d36e-434e-b861-91b9637c96e7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Application.Contracts.Dtos;

namespace XiHan.BasicApp.Rbac.Application.Services.Users.Dtos;

/// <summary>
/// 用户更新DTO
/// </summary>
public class SysUserUpdateDto : UpdateDtoBase<long>
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
    public UserGender? Gender { get; set; }

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
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
