#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberInviteStatusUpdateDto
// Guid:2fdc6ccf-07c9-4f60-887e-f9899ccdf2ec
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户成员邀请状态更新 DTO
/// </summary>
public sealed class TenantMemberInviteStatusUpdateDto
{
    /// <summary>
    /// 租户成员主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 邀请状态
    /// </summary>
    public TenantMemberInviteStatus InviteStatus { get; set; }

    /// <summary>
    /// 邀请备注
    /// </summary>
    public string? InviteRemark { get; set; }
}
