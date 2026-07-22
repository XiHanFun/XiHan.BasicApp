// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 标记配置（库隔离数据库初始化）状态
    /// </summary>
    /// <param name="configStatus">新的配置状态</param>
    public void MarkConfigStatus(TenantConfigStatus configStatus)
    {
        ConfigStatus = configStatus;
    }
}
