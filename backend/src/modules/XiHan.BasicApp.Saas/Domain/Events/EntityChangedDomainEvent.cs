#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EntityChangedDomainEvent
// Guid:e1f2a3b4-c5d6-7890-4567-89abcdef0123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 05:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 实体变更领域事件基类，用于驱动缓存失效
/// </summary>
/// <typeparam name="TKey">实体主键类型</typeparam>
public abstract class EntityChangedDomainEvent<TKey> : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    /// <param name="tenantId">租户 ID</param>
    protected EntityChangedDomainEvent(TKey entityId, long? tenantId = null)
    {
        EntityId = entityId;
        TenantId = tenantId;
    }

    /// <summary>
    /// 变更的实体 ID
    /// </summary>
    public TKey EntityId { get; }

    /// <summary>
    /// 租户 ID
    /// </summary>
    public long? TenantId { get; }
}
