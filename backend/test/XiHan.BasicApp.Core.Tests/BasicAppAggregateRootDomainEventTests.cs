// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Aggregates.Abstracts;

namespace XiHan.BasicApp.Core.Tests;

/// <summary>
/// BasicApp 聚合根的领域事件容器语义测试。
/// </summary>
/// <remarks>
/// 聚合根比普通实体多的那部分能力就是领域事件容器。这里守两件事：
/// 容器必须是**实例级**的（共享会把 A 聚合的事件串到 B 聚合上，在并行场景里表现为随机串数据），
/// 以及新建聚合根的事件集合必须是空集合而非 null（工作单元会无条件枚举它）。
/// </remarks>
public sealed class BasicAppAggregateRootDomainEventTests
{
    /// <summary>
    /// 新建聚合根的本地与分布式事件集合必须非 null 且为空。
    /// </summary>
    /// <remarks>
    /// 工作单元在提交前会无条件遍历这两个集合；返回 null 会让整条提交路径 NullReferenceException。
    /// </remarks>
    [Fact]
    public void NewAggregateRoot_ShouldExposeEmptyEventCollections()
    {
        var aggregate = new CoreAggregateRootProbe();

        Assert.NotNull(aggregate.GetLocalEvents());
        Assert.NotNull(aggregate.GetDistributedEvents());
        Assert.Empty(aggregate.GetLocalEvents());
        Assert.Empty(aggregate.GetDistributedEvents());
    }

    /// <summary>
    /// 登记本地事件后必须能读回，且不会串进分布式事件队列。
    /// </summary>
    [Fact]
    public void RaiseLocalEvent_ShouldOnlyAffectLocalQueue()
    {
        var aggregate = new CoreAggregateRootProbe();
        var domainEvent = new CoreProbeDomainEvent();

        aggregate.RaiseLocalEvent(domainEvent);

        var record = Assert.Single(aggregate.GetLocalEvents());
        Assert.Same(domainEvent, record.EventData);
        Assert.Empty(aggregate.GetDistributedEvents());
    }

    /// <summary>
    /// 登记分布式事件后必须能读回，且不会串进本地事件队列。
    /// </summary>
    [Fact]
    public void RaiseDistributedEvent_ShouldOnlyAffectDistributedQueue()
    {
        var aggregate = new CoreAggregateRootProbe();
        var domainEvent = new CoreProbeDomainEvent();

        aggregate.RaiseDistributedEvent(domainEvent);

        var record = Assert.Single(aggregate.GetDistributedEvents());
        Assert.Same(domainEvent, record.EventData);
        Assert.Empty(aggregate.GetLocalEvents());
    }

    /// <summary>
    /// 两个聚合根实例的事件容器必须彼此独立。
    /// </summary>
    /// <remarks>
    /// 若容器被写成静态字段共享，往 A 上登记的事件会出现在 B 的队列里，
    /// 提交 B 时会把 A 的事件一并发出去——这是并行执行下最难复现的一类事故。
    /// </remarks>
    [Fact]
    public void EventContainers_ShouldBeIsolatedPerInstance()
    {
        var first = new CoreAggregateRootProbe(1L);
        var second = new CoreAggregateRootProbe(2L);

        first.RaiseLocalEvent(new CoreProbeDomainEvent());
        first.RaiseDistributedEvent(new CoreProbeDomainEvent());

        Assert.Single(first.GetLocalEvents());
        Assert.Single(first.GetDistributedEvents());
        Assert.Empty(second.GetLocalEvents());
        Assert.Empty(second.GetDistributedEvents());
    }

    /// <summary>
    /// 清空本地事件不会影响分布式事件队列，反之亦然。
    /// </summary>
    [Fact]
    public void ClearEvents_ShouldNotCrossQueues()
    {
        var aggregate = new CoreAggregateRootProbe();
        aggregate.RaiseLocalEvent(new CoreProbeDomainEvent());
        aggregate.RaiseDistributedEvent(new CoreProbeDomainEvent());

        aggregate.ClearLocalEvents();

        Assert.Empty(aggregate.GetLocalEvents());
        Assert.Single(aggregate.GetDistributedEvents());

        aggregate.ClearDistributedEvents();

        Assert.Empty(aggregate.GetDistributedEvents());
    }

    /// <summary>
    /// 在空队列上清空事件必须可安全调用且不抛异常。
    /// </summary>
    /// <remarks>
    /// 工作单元提交后会无条件清一次，绝大多数聚合根此时根本没有事件。
    /// </remarks>
    [Fact]
    public void ClearEvents_ShouldBeSafeOnEmptyQueues()
    {
        var aggregate = new CoreAggregateRootProbe();

        aggregate.ClearLocalEvents();
        aggregate.ClearDistributedEvents();
        aggregate.ClearLocalEvents();

        Assert.Empty(aggregate.GetLocalEvents());
        Assert.Empty(aggregate.GetDistributedEvents());
    }

    /// <summary>
    /// 登记事件必须拒绝 null，避免空事件进入队列后在发布阶段才炸。
    /// </summary>
    [Fact]
    public void RaiseEvent_NullEventShouldThrow()
    {
        var aggregate = new CoreAggregateRootProbe();

        _ = Assert.Throws<ArgumentNullException>(() => aggregate.RaiseLocalEvent(null!));
        _ = Assert.Throws<ArgumentNullException>(() => aggregate.RaiseDistributedEvent(null!));
    }

    /// <summary>
    /// 读取事件返回的是快照，后续登记不会回写到已取出的集合。
    /// </summary>
    /// <remarks>
    /// 工作单元先取事件再逐条发布；若返回的是活引用，发布过程中新登记的事件会造成边遍历边修改。
    /// </remarks>
    [Fact]
    public void GetEvents_ShouldReturnSnapshotNotLiveView()
    {
        var aggregate = new CoreAggregateRootProbe();
        aggregate.RaiseLocalEvent(new CoreProbeDomainEvent());

        var snapshot = aggregate.GetLocalEvents().ToList();
        aggregate.RaiseLocalEvent(new CoreProbeDomainEvent());

        Assert.Single(snapshot);
        Assert.Equal(2, aggregate.GetLocalEvents().Count());
    }

    /// <summary>
    /// 派生聚合根实例必须可赋值给 <see cref="IAggregateRoot{TKey}"/>，仓储才会收集它的事件。
    /// </summary>
    [Fact]
    public void AggregateRootInstance_ShouldBeUsableThroughAggregateRootInterface()
    {
        IAggregateRoot<long> aggregate = new CoreAggregateRootProbe(5L);

        Assert.Equal(5L, aggregate.BasicId);
        Assert.Empty(aggregate.GetLocalEvents());
    }

    /// <summary>
    /// 聚合根基类本身不得暴露公开的事件登记入口。
    /// </summary>
    /// <remarks>
    /// 事件必须由聚合根内部的业务方法登记，外部随意 AddLocalEvent 会绕过聚合的不变量校验。
    /// 样例聚合根 <see cref="CoreAggregateRootProbe"/> 是测试专用包装，不代表基类形状。
    /// </remarks>
    [Fact]
    public void BasicAppAggregateRoot_ShouldNotExposePublicEventAppenders()
    {
        Assert.Null(typeof(BasicAppAggregateRoot).GetMethod("AddLocalEvent"));
        Assert.Null(typeof(BasicAppAggregateRoot).GetMethod("AddDistributedEvent"));
    }
}
