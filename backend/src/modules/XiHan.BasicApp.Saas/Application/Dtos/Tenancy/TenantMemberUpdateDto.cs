#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberUpdateDto
// Guid:ef31c66c-b4c2-4e4c-b2af-5fdc0c18279c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户成员更新 DTO
/// </summary>
public sealed class TenantMemberUpdateDto
{
    /// <summary>
    /// 租户成员主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    public TenantMemberType MemberType { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
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
