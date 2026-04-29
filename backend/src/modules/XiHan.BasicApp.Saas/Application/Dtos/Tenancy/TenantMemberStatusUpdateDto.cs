#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberStatusUpdateDto
// Guid:615b4170-aeea-4021-8f18-3883d6f35b9b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户成员状态更新 DTO
/// </summary>
public sealed class TenantMemberStatusUpdateDto
{
    /// <summary>
    /// 租户成员主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 成员状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
