#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleChangedDomainEvent
// Guid:d0a6b4c3-5e8f-6a7b-1c3d-4e5f6a7b8c93
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 角色变更领域事件
/// </summary>
public sealed class RoleChangedDomainEvent : EntityChangedDomainEvent<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleChangedDomainEvent(long entityId, long? tenantId = null)
        : base(entityId, tenantId)
    {
    }
}
