#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenant.Aggregate
// Guid:a7fd32d8-ccd1-440e-96f9-04cb0f1d274b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 07:04:40
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 租户聚合领域行为
/// </summary>
public partial class SysTenant
{
    /// <summary>
    /// 切换租户状态并发布事件
    /// </summary>
    public void ChangeTenantStatus(TenantStatus status)
    {
        TenantStatus = status;
        AddLocalEvent(new TenantStatusChangedDomainEvent(BasicId, status));
    }

    /// <summary>
    /// 启用租户
    /// </summary>
    public void Enable()
    {
        ChangeTenantStatus(TenantStatus.Normal);
    }

    /// <summary>
    /// 禁用租户
    /// </summary>
    public void Disable()
    {
        ChangeTenantStatus(TenantStatus.Disabled);
    }
}
