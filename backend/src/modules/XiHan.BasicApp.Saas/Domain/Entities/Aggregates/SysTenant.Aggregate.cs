#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenant.Aggregate
// Guid:b8f7d262-ef0a-43c0-8ecd-c2531723728a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 租户聚合行为
/// </summary>
public partial class SysTenant
{
    /// <summary>
    /// 变更租户状态
    /// </summary>
    /// <param name="newStatus">新状态</param>
    /// <param name="operatorUserId">操作人主键</param>
    /// <param name="reason">变更原因</param>
    public void ChangeStatus(TenantStatus newStatus, long? operatorUserId = null, string? reason = null)
    {
        if (TenantStatus == newStatus)
        {
            return;
        }

        var oldStatus = TenantStatus;
        TenantStatus = newStatus;

        AddLocalEvent(new TenantStatusChangedDomainEvent(TenantId, BasicId, oldStatus, newStatus, operatorUserId, reason));
    }
}
