#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantChangedDomainEvent
// Guid:2a3b4c5d-6e7f-4901-bcde-230000000003
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 租户变更领域事件
/// </summary>
public sealed class TenantChangedDomainEvent : EntityChangedDomainEvent<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantChangedDomainEvent(long entityId, long? tenantId = null)
        : base(entityId, tenantId)
    {
    }
}
