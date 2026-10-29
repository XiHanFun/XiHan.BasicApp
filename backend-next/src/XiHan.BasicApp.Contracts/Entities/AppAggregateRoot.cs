// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities;
using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 有行为的聚合根基类：<see cref="AppAuditedEntity"/> + 领域事件缓冲。
/// </summary>
/// <remarks>
/// 聚合在方法里 <see cref="Raise"/> 事件，工作单元提交前由框架经已登记的映射派发成消息。
/// </remarks>
public abstract class AppAggregateRoot : AppAuditedEntity, IAggregateRoot<Guid>
{
    private readonly DomainEventBuffer _events = new();

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> DomainEvents => _events.Items;

    /// <inheritdoc />
    public void ClearDomainEvents() => _events.Clear();

    /// <summary>
    /// 记录一条待派发的领域事件。
    /// </summary>
    protected void Raise(IDomainEvent domainEvent) => _events.Add(domainEvent);
}
