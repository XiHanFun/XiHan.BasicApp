#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserCreateDto
// Guid:95f5a5dc-0d2e-42db-b260-3c1280f7e626
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户创建 DTO
/// </summary>
public sealed class UserCreateDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 初始密码
    /// </summary>
    public string InitialPassword { get; set; } = string.Empty;

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
    public UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 生日
    /// </summary>
    public DateTimeOffset? Birthday { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 时区
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    public string? Language { get; set; } = "zh-CN";

    /// <summary>
    /// 国家/地区
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 租户成员类型
    /// </summary>
    public TenantMemberType MemberType { get; set; } = TenantMemberType.Member;

    /// <summary>
    /// 成员生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 成员失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 租户内显示名
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 邀请备注
    /// </summary>
    public string? InviteRemark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
