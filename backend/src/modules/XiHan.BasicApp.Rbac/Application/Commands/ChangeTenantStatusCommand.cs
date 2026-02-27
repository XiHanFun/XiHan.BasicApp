#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChangeTenantStatusCommand
// Guid:f1854143-08b2-4b8f-b97c-b48d2fa5888f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:45:07
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 修改租户状态命令
/// </summary>
public class ChangeTenantStatusCommand
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;
}
