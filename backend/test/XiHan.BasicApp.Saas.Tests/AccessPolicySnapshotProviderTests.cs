// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;
using static XiHan.BasicApp.Saas.Tests.AccessPolicyTestKit;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 访问策略快照：按分布式缓存里的版本号失效，多节点在检查间隔内一致，写入在事务提交后才换版本
/// </summary>
public sealed class AccessPolicySnapshotProviderTests
{
    /// <summary>
    /// 启用的策略编译进快照；没有条件的不参与
    /// </summary>
    [Fact]
    public async Task Build_CompilesEnabledPolicies()
    {
        var store = new Store();
        store.Add(1, 0, AccessPolicyAction.Deny, "203.0.113.0/24");
        store.Add(2, 7, AccessPolicyAction.LogOnly, "198.51.100.0/24");
        store.AddWithoutConditions(3);
        var node = store.Node();

        var snapshot = await node.Provider.GetAsync();

        Assert.Equal(1L, Assert.Single(snapshot.Platform).PolicyId);
        Assert.Equal(2L, Assert.Single(snapshot.GetTenant(7)).PolicyId);
    }

    /// <summary>
    /// 一个节点写入后换版本号：本节点下次取用就重建，另一个节点在检查间隔到了才重建
    /// </summary>
    [Fact]
    public async Task Invalidate_PropagatesToOtherNodesWithinCheckInterval()
    {
        var store = new Store();
        store.Add(1, 0, AccessPolicyAction.Deny, "203.0.113.0/24");
        var writer = store.Node();
        var reader = store.Node();
        Assert.Single((await writer.Provider.GetAsync()).Platform);
        Assert.Single((await reader.Provider.GetAsync()).Platform);

        store.Add(2, 0, AccessPolicyAction.Deny, "198.51.100.0/24");
        await writer.Provider.InvalidateAsync();

        Assert.Equal(2, (await writer.Provider.GetAsync()).Platform.Count);
        Assert.Single((await reader.Provider.GetAsync()).Platform);
        store.Time.Now = Now.AddSeconds(5);
        Assert.Equal(2, (await reader.Provider.GetAsync()).Platform.Count);
    }

    /// <summary>
    /// 在工作单元里写入时，版本号到提交后才换，提交前别的请求重建出的仍是旧版本
    /// </summary>
    [Fact]
    public async Task Invalidate_InsideUnitOfWork_PublishesAfterCompletion()
    {
        var store = new Store();
        store.Add(1, 0, AccessPolicyAction.Deny, "203.0.113.0/24");
        var node = store.Node();
        Assert.Single((await node.Provider.GetAsync()).Platform);
        Func<Task>? completed = null;
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(uow => uow.OnCompleted(It.IsAny<Func<Task>>())).Callback<Func<Task>>(handler => completed = handler);
        node.UnitOfWork.Setup(manager => manager.Current).Returns(unitOfWork.Object);

        store.Add(2, 0, AccessPolicyAction.Deny, "198.51.100.0/24");
        await node.Provider.InvalidateAsync();
        store.Time.Now = Now.AddSeconds(5);
        Assert.Single((await node.Provider.GetAsync()).Platform);

        await completed!();
        Assert.Equal(2, (await node.Provider.GetAsync()).Platform.Count);
    }

    /// <summary>
    /// 重建失败时沿用旧快照；还没有快照时把异常抛给调用方
    /// </summary>
    [Fact]
    public async Task RebuildFailure_KeepsPreviousSnapshot()
    {
        var store = new Store();
        store.Add(1, 0, AccessPolicyAction.Deny, "203.0.113.0/24");
        var node = store.Node();
        Assert.Single((await node.Provider.GetAsync()).Platform);

        store.Failure = new InvalidOperationException("数据库不可用");
        await node.Provider.InvalidateAsync();
        Assert.Single((await node.Provider.GetAsync()).Platform);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await store.Node().Provider.GetAsync());
    }

    private sealed class Store
    {
        private readonly List<SysAccessPolicy> _policies = [];
        private readonly List<SysAccessPolicyCondition> _conditions = [];
        private readonly MemoryDistributedCache _cache = new(Options.Create(new MemoryDistributedCacheOptions()));

        public ManualTimeProvider Time { get; } = new(Now);

        public Exception? Failure { get; set; }

        public void Add(long id, long tenantId, AccessPolicyAction action, string network)
        {
            AddWithoutConditions(id, tenantId, action);
            var condition = new SysAccessPolicyCondition
            {
                PolicyId = id,
                TenantId = tenantId,
                AttributeName = SaasConditionAttributes.EnvironmentIp,
                Operator = ConditionOperator.In,
                ConditionValue = network
            };
            SaasTestHelper.SetBasicId(condition, id * 10);
            _conditions.Add(condition);
        }

        public void AddWithoutConditions(long id, long tenantId = 0, AccessPolicyAction action = AccessPolicyAction.Deny)
        {
            var policy = new SysAccessPolicy { TenantId = tenantId, PolicyCode = $"policy-{id}", PolicyName = $"策略 {id}", Action = action, Status = EnableStatus.Enabled };
            SaasTestHelper.SetBasicId(policy, id);
            _policies.Add(policy);
        }

        public (AccessPolicySnapshotProvider Provider, Mock<IUnitOfWorkManager> UnitOfWork) Node()
        {
            var policies = new Mock<IAccessPolicyRepository>();
            policies
                .Setup(repository => repository.GetEnabledInAllTenantsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Failure is null ? _policies.ToList() : throw Failure);
            var conditions = new Mock<IAccessPolicyConditionRepository>();
            conditions
                .Setup(repository => repository.GetByPolicyIdsInAllTenantsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<long> ids, CancellationToken _) => _conditions.Where(condition => ids.Contains(condition.PolicyId)).ToList());
            var services = new ServiceCollection()
                .AddSingleton<ICurrentTenant>(new TestCurrentTenant())
                .AddSingleton(policies.Object)
                .AddSingleton(conditions.Object)
                .AddSingleton(Compiler())
                .BuildServiceProvider();
            var unitOfWork = new Mock<IUnitOfWorkManager>();
            var provider = new AccessPolicySnapshotProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                _cache,
                unitOfWork.Object,
                PolicyOptions(),
                Time,
                NullLogger<AccessPolicySnapshotProvider>.Instance);
            return (provider, unitOfWork);
        }
    }
}
