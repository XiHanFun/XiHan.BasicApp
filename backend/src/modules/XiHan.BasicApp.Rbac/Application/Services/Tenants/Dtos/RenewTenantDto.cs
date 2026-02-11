#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RenewTenantDto
// Guid:7761c7af-040e-4d0d-971f-af720f4b20a6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Services.Tenants.Dtos;

/// <summary>
/// 租户续期DTO
/// </summary>
public class RenewTenantDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 续期天数
    /// </summary>
    public int Days { get; set; }
}
