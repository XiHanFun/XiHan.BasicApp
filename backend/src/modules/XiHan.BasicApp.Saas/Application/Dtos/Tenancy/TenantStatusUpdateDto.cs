#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantStatusUpdateDto
// Guid:3110c417-f50b-4ebf-83a5-682f2721270a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户状态更新 DTO
/// </summary>
public sealed class TenantStatusUpdateDto
{
    /// <summary>
    /// 租户主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// 变更原因
    /// </summary>
    public string? Reason { get; set; }
}
