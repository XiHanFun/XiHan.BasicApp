#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberListItemDto
// Guid:7627e1ba-8923-447f-93f0-b31b047645c8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户成员列表项 DTO
/// </summary>
public sealed class TenantMemberListItemDto : BasicAppDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 成员类型
    /// </summary>
    public TenantMemberType MemberType { get; set; }

    /// <summary>
    /// 邀请状态
    /// </summary>
    public TenantMemberInviteStatus InviteStatus { get; set; }

    /// <summary>
    /// 邀请人主键
    /// </summary>
    public long? InvitedBy { get; set; }

    /// <summary>
    /// 邀请时间
    /// </summary>
    public DateTimeOffset? InvitedTime { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTimeOffset? RespondedTime { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 最近活跃时间
    /// </summary>
    public DateTimeOffset? LastActiveTime { get; set; }

    /// <summary>
    /// 租户内显示名
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 用户账号（只读，来自 SysUser）
    /// </summary>
    /// <remarks>
    /// <see cref="DisplayName"/> 是<b>租户内覆盖名</b>，绝大多数成员没设，因而为空。
    /// 这三个只读字段用于让前端回退展示"这个人是谁"（显示名 → 姓名 → 昵称 → 账号），
    /// 而不是把回退值写回 <see cref="DisplayName"/>——那会被「编辑资料」当成覆盖名存回库里。
    /// </remarks>
    public string? UserName { get; set; }

    /// <summary>
    /// 用户真实姓名（只读，来自 SysUser）
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 用户昵称（只读，来自 SysUser）
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 成员状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
