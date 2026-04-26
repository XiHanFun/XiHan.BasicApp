#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberInviteStatus
// Guid:e1f2a3b4-5678-9012-bcde-f56789012345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Entities.Enums;

/// <summary>
/// 租户成员邀请状态枚举
/// 描述用户获得租户成员身份的生命周期
/// </summary>
public enum TenantMemberInviteStatus
{
    /// <summary>
    /// 待接受：邀请已发出但用户尚未响应（未生效，不能访问租户数据）
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已接受：成员身份已生效，鉴权通过后可访问租户数据
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// 已拒绝：用户拒绝邀请，关系不生效
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// 已撤销：管理员吊销成员身份，或用户主动退出租户
    /// </summary>
    Revoked = 3,

    /// <summary>
    /// 已过期：邀请在 EffectiveTo 前未被接受，或成员身份有效期已过
    /// </summary>
    Expired = 4
}
