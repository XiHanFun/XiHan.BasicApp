// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Abstracts;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 业务编号生成器作用域、幂等、并发、周期与失败边界测试。
/// </summary>
public sealed class NumberGeneratorTests
{
    /// <summary>
    /// Auto 在租户上下文中应优先使用同编码租户私有规则。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_AutoShouldPreferTenantRule()
    {
        var fixture = new GeneratorFixture();
        var tenantRule = fixture.AddRule(701, 7, "ORDER", "TEN", allowTenantUse: false);
        var globalRule = fixture.AddRule(1, 0, "ORDER", "GLB", allowTenantUse: true);

        var result = await fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Auto, "tenant-first")));

        Assert.Equal(NumberingScope.Tenant, result.ResolvedScope);
        Assert.Equal(tenantRule.BasicId, result.RuleId);
        Assert.Equal("TEN-0001", Assert.Single(result.Numbers));
        Assert.Equal(1, tenantRule.CurrentValue);
        Assert.Equal(0, globalRule.CurrentValue);
    }

    /// <summary>
    /// 字段隔离与独立数据库租户都必须切换平台上下文后回退全局规则。
    /// </summary>
    /// <param name="isolatedDatabase">是否模拟租户独立数据库的连接可见性。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GenerateAsync_AutoShouldFallbackToPlatformGlobalRule(bool isolatedDatabase)
    {
        var fixture = new GeneratorFixture { SimulateIsolatedTenantDatabase = isolatedDatabase };
        fixture.AddRule(1, 0, "ORDER", "GLB", allowTenantUse: true);

        var result = await fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Auto, $"fallback-{isolatedDatabase}")));

        Assert.Equal(NumberingScope.Global, result.ResolvedScope);
        Assert.Equal(7, result.RequestTenantId);
        Assert.Equal("GLB-0001", Assert.Single(result.Numbers));
        Assert.Contains(fixture.RuleReadContexts, item => item.OwnerTenantId == 0 && item.CurrentTenantId is null);
        Assert.All(fixture.AllocationWriteContexts, tenantId => Assert.Null(tenantId));
    }

    /// <summary>
    /// Tenant 强制作用域找不到私有规则时不能偷偷回退全局规则。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_TenantScopeShouldNotFallbackToGlobalRule()
    {
        var fixture = new GeneratorFixture();
        fixture.AddRule(1, 0, "ORDER", "GLB", allowTenantUse: true);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Tenant, "tenant-only"))));

        Assert.Contains("租户私有", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Allocations);
    }

    /// <summary>
    /// 平台或单体上下文的 Auto 请求应直接使用全局规则。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_PlatformAutoShouldUseGlobalRule()
    {
        var fixture = new GeneratorFixture();
        fixture.AddRule(1, 0, "ORDER", "APP", allowTenantUse: false);

        var result = await fixture.RunAsTenantAsync(
            null,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Auto, "single-app")));

        Assert.Equal(NumberingScope.Global, result.ResolvedScope);
        Assert.Equal(0, result.RequestTenantId);
        Assert.Equal("APP-0001", Assert.Single(result.Numbers));
    }

    /// <summary>
    /// 同一租户规则的并行单号与批量请求不得产生重复，并应形成连续数据库分配区间。
    /// </summary>
    [Fact]
    public async Task GenerateBatchAsync_ConcurrentTenantRequestsShouldNotDuplicate()
    {
        var fixture = new GeneratorFixture();
        var rule = fixture.AddRule(701, 7, "ORDER", "TEN", allowTenantUse: false, serialLength: 6);
        var requests = Enumerable.Range(1, 80)
            .Select(index => fixture.RunAsTenantAsync(
                7,
                () => fixture.Generator.GenerateBatchAsync(
                    new NumberBatchGenerateRequest("ORDER", NumberingScope.Tenant, $"tenant-batch-{index}", (index % 5) + 1))))
            .ToArray();

        var results = await Task.WhenAll(requests);
        var numbers = results.SelectMany(result => result.Numbers).ToArray();
        var serials = numbers.Select(ParseSerial).Order().ToArray();

        Assert.Equal(numbers.Length, numbers.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Enumerable.Range(1, numbers.Length).Select(value => (long)value), serials);
        Assert.Equal(numbers.Length, rule.CurrentValue);
        Assert.Equal(80, fixture.Allocations.Count);
    }

    /// <summary>
    /// 两个租户共同调用一个全局规则时必须共享同一序列，同时保留各自请求租户审计。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_TwoTenantsShouldShareOneGlobalSequence()
    {
        var fixture = new GeneratorFixture();
        var rule = fixture.AddRule(1, 0, "GLOBAL_ORDER", "G", allowTenantUse: true, serialLength: 5);
        var requests = Enumerable.Range(1, 100)
            .Select(index => fixture.RunAsTenantAsync(
                index % 2 == 0 ? 11 : 22,
                () => fixture.Generator.GenerateAsync(
                    new NumberGenerateRequest("GLOBAL_ORDER", NumberingScope.Global, $"global-{index}"))))
            .ToArray();

        var results = await Task.WhenAll(requests);
        var serials = results.SelectMany(result => result.Numbers).Select(ParseSerial).Order().ToArray();

        Assert.Equal(Enumerable.Range(1, 100).Select(value => (long)value), serials);
        Assert.Equal(100, rule.CurrentValue);
        Assert.Contains(fixture.Allocations, allocation => allocation.RequestTenantId == 11);
        Assert.Contains(fixture.Allocations, allocation => allocation.RequestTenantId == 22);
    }

    /// <summary>
    /// 相同租户、规则和幂等键的并发调用只能写入一条分配记录，其余调用重建相同结果。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ConcurrentSameIdempotencyKeyShouldAllocateOnce()
    {
        var fixture = new GeneratorFixture();
        fixture.AddRule(701, 7, "ORDER", "TEN", allowTenantUse: false);
        var requests = Enumerable.Range(0, 40)
            .Select(_ => fixture.RunAsTenantAsync(
                7,
                () => fixture.Generator.GenerateAsync(
                    new NumberGenerateRequest("ORDER", NumberingScope.Tenant, "same-key", "Order", "1001"))))
            .ToArray();

        var results = await Task.WhenAll(requests);

        Assert.Single(fixture.Allocations);
        Assert.Single(results.SelectMany(result => result.Numbers).Distinct(StringComparer.Ordinal));
        _ = Assert.Single(results, result => !result.IsIdempotentReplay);
        Assert.Equal(39, results.Count(result => result.IsIdempotentReplay));
    }

    /// <summary>
    /// 相同幂等键携带不同参数时必须返回冲突，且不能推进第二段流水。
    /// </summary>
    [Fact]
    public async Task GenerateBatchAsync_SameKeyWithDifferentParametersShouldConflict()
    {
        var fixture = new GeneratorFixture();
        var rule = fixture.AddRule(701, 7, "ORDER", "TEN", allowTenantUse: false);

        _ = await fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(
                new NumberGenerateRequest("ORDER", NumberingScope.Tenant, "conflict-key", "Order", "1001")));
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateBatchAsync(
                new NumberBatchGenerateRequest("ORDER", NumberingScope.Tenant, "conflict-key", 2, "Order", "1001"))));

        Assert.Contains("不同参数", exception.Message, StringComparison.Ordinal);
        Assert.Single(fixture.Allocations);
        Assert.Equal(1, rule.CurrentValue);
    }

    /// <summary>
    /// 新周期第一次发号应从 1 开始，而不是延续旧周期当前值。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_NewPeriodShouldResetAutomatically()
    {
        var fixture = new GeneratorFixture();
        var rule = fixture.AddRule(
            701,
            7,
            "DAILY",
            "D",
            allowTenantUse: false,
            dateFormat: NumberingDateFormat.YyyyMMdd,
            resetCycle: NumberingResetCycle.Daily);
        GeneratorFixture.SeedBaseline(rule, "20260726", 20260726, 998);

        var result = await fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("DAILY", NumberingScope.Tenant, "new-day")));

        Assert.Equal("D-20260727-0001", Assert.Single(result.Numbers));
        Assert.Equal("20260727", rule.CurrentPeriod);
        Assert.Equal(1, rule.CurrentValue);
    }

    /// <summary>
    /// 固定位数容量耗尽时应返回友好错误，并且不写分配记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ExhaustedSerialShouldNotAdvance()
    {
        var fixture = new GeneratorFixture();
        var rule = fixture.AddRule(701, 7, "SHORT", null, allowTenantUse: false, serialLength: 1);
        GeneratorFixture.SeedBaseline(rule, "never", 0, 9);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("SHORT", NumberingScope.Tenant, "overflow"))));

        Assert.Contains("流水已耗尽", exception.Message, StringComparison.Ordinal);
        Assert.Equal(9, rule.CurrentValue);
        Assert.Empty(fixture.Allocations);
    }

    /// <summary>
    /// 发号目标库与调用方事务同库时必须加入调用方事务，使编号与业务数据同时可见或同时消失。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_SameDatabaseShouldJoinCallerTransaction()
    {
        var fixture = new GeneratorFixture { SimulateIsolatedTenantDatabase = false };
        fixture.AddRule(1, 0, "ORDER", "GLB", allowTenantUse: true);

        _ = await fixture.RunAsTenantAsync(
            7,
            () => fixture.RunInCallerTransactionAsync(
                () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Global, "same-db"))));

        // 只有调用方那一个事务，发号没有另开事务。
        Assert.Equal(1, fixture.OwnTransactionCount);
    }

    /// <summary>
    /// 发号目标库与调用方事务不同库时必须自行提交，不能依赖工作单元对两条连接的顺序提交。
    /// </summary>
    /// <remarks>
    /// 跨库时并进调用方事务只是幻觉：没有两阶段提交，调用方先提交而平台库提交失败会让调用方
    /// 拿到一个既未记录也未推进流水的编号，下一次发号必然重复它。自行提交把最坏结果降级为编号空洞。
    /// </remarks>
    [Fact]
    public async Task GenerateAsync_CrossDatabaseShouldCommitOwnTransaction()
    {
        var fixture = new GeneratorFixture { SimulateIsolatedTenantDatabase = true };
        fixture.AddRule(1, 0, "ORDER", "GLB", allowTenantUse: true);

        var result = await fixture.RunAsTenantAsync(
            7,
            () => fixture.RunInCallerTransactionAsync(
                () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Global, "cross-db"))));

        Assert.Equal("GLB-0001", Assert.Single(result.Numbers));
        // 调用方事务 + 发号自有事务。
        Assert.Equal(2, fixture.OwnTransactionCount);
    }

    /// <summary>
    /// 没有可用事务时必须立即失败，而不是在无锁保护下发号。
    /// </summary>
    /// <remarks>
    /// 这是整套设计的安全底座：推进语句的行锁必须持有到提交，读回值才等于本次推进结果。
    /// 一旦允许在非事务型工作单元内发号，两个并发调用会读回同一个流水值并发出重复编号。
    /// </remarks>
    [Fact]
    public async Task GenerateAsync_WithoutTransactionShouldFailClosed()
    {
        var fixture = new GeneratorFixture();
        var rule = fixture.AddRule(701, 7, "ORDER", "TEN", allowTenantUse: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RunAsTenantAsync(
            7,
            () => fixture.RunWithoutTransactionAsync(
                () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Tenant, "no-tran")))));

        Assert.Contains("没有活动事务", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, rule.CurrentValue);
        Assert.Empty(fixture.Allocations);
    }

    /// <summary>
    /// 节点时钟落后时必须拒绝发号，而不是把规则拉回上一周期重发已发出的编号。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ClockBehindStoredPeriodShouldBeRejected()
    {
        // 库中已经推进到 7 月 28 日，本节点时钟仍停在 7 月 27 日。
        var fixture = new GeneratorFixture { CurrentUtcNow = new DateTimeOffset(2026, 7, 27, 8, 30, 0, TimeSpan.Zero) };
        var rule = fixture.AddRule(
            701,
            7,
            "DAILY",
            "D",
            allowTenantUse: false,
            dateFormat: NumberingDateFormat.YyyyMMdd,
            resetCycle: NumberingResetCycle.Daily);
        GeneratorFixture.SeedBaseline(rule, "20260728", 20260728, 42);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("DAILY", NumberingScope.Tenant, "clock-skew"))));

        Assert.Contains("时间同步", exception.Message, StringComparison.Ordinal);
        // 关键断言：周期基线和流水都没有被拉回，否则 20260728 的 1..42 会被重新发一遍。
        Assert.Equal("20260728", rule.CurrentPeriod);
        Assert.Equal(20260728, rule.CurrentPeriodOrdinal);
        Assert.Equal(42, rule.CurrentValue);
        Assert.Empty(fixture.Allocations);
    }

    /// <summary>
    /// 正常并发路径不应产生任何重试：推进由数据库行锁串行化，不存在需要退避的冲突。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ConcurrentRequestsShouldNotRetry()
    {
        var fixture = new GeneratorFixture();
        fixture.AddRule(701, 7, "ORDER", "TEN", allowTenantUse: false, serialLength: 6);

        var tasks = Enumerable.Range(0, 32).Select(index => Task.Run(() => fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Tenant, $"no-retry-{index}")))));
        var results = await Task.WhenAll(tasks);

        // 每次发号恰好执行一条推进语句；出现多于请求数的尝试说明重试逻辑又被引入。
        Assert.Equal(32, fixture.AdvanceAttempts);
        Assert.Equal(32, results.Select(result => ParseSerial(Assert.Single(result.Numbers))).Distinct().Count());
    }

    /// <summary>
    /// 从完整编号末段解析流水，用于验证并发结果集合。
    /// </summary>
    private static long ParseSerial(string number)
    {
        var separatorIndex = number.LastIndexOf('-');
        var serialText = separatorIndex < 0 ? number : number[(separatorIndex + 1)..];
        return long.Parse(serialText, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 使用线程安全内存状态模拟仓储、租户切换和工作单元，测试生产发号器本身的编排逻辑。
    /// </summary>
    private sealed class GeneratorFixture
    {
        private readonly AsyncLocal<long?> _currentTenantId = new();
        private readonly ConcurrentDictionary<(long OwnerTenantId, string RuleCode), SysNumberingRule> _rules = new();
        private readonly ConcurrentDictionary<AllocationKey, SysNumberingAllocation> _allocations = new();

        /// <summary>
        /// 每条规则一把锁，模拟数据库对被推进行加的排他锁。
        /// </summary>
        private readonly ConcurrentDictionary<(long OwnerTenantId, long RuleId), SemaphoreSlim> _ruleLocks = new();

        /// <summary>
        /// 当前执行流所处的模拟事务；为 null 表示连接上没有活动事务。
        /// </summary>
        private readonly AsyncLocal<FakeTransaction?> _ambientTransaction = new();

        private long _nextAllocationId;
        private int _advanceAttempts;
        private int _ownTransactionCount;

        /// <summary>
        /// 初始化测试夹具并把仓储回调连接到内存状态。
        /// </summary>
        public GeneratorFixture()
        {
            RuleRepository = new Mock<INumberingRuleRepository>();
            AllocationRepository = new Mock<INumberingAllocationRepository>();
            RuleReadContexts = new ConcurrentBag<RuleReadContext>();
            AllocationWriteContexts = new ConcurrentBag<long?>();

            RuleRepository
                .Setup(repository => repository.FindByCodeInScopeAsync(
                    It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long ownerTenantId, string ruleCode, bool enabledOnly, CancellationToken _) =>
                {
                    RuleReadContexts.Add(new RuleReadContext(ownerTenantId, _currentTenantId.Value));
                    if (SimulateIsolatedTenantDatabase && !IsVisibleInCurrentDatabase(ownerTenantId))
                    {
                        return null;
                    }

                    if (!_rules.TryGetValue((ownerTenantId, ruleCode), out var rule))
                    {
                        return null;
                    }

                    return !enabledOnly || rule.Status == EnableStatus.Enabled ? rule : null;
                });
            RuleRepository
                .Setup(repository => repository.FindByIdInScopeAsync(
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long ownerTenantId, long id, CancellationToken _) =>
                    _rules.Values.SingleOrDefault(rule => rule.TenantId == ownerTenantId && rule.BasicId == id));
            // 以下三个方法替身实现的是生产语句的真实语义，而不是「返回预设值」：
            // 守卫全部真的求值，命中才改状态，并且模拟数据库把行锁持有到事务提交，
            // 因此并发用例是在验证真实不变量，而不是验证 Mock 的配置。
            RuleRepository
                .Setup(repository => repository.TryRollOverPeriodAsync(
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns((long ownerTenantId, long ruleId, string periodKey, long periodOrdinal, CancellationToken _) =>
                {
                    EnsureInTransaction();
                    AcquireRuleLock(ownerTenantId, ruleId);
                    var rule = FindRule(ownerTenantId, ruleId);
                    // 单调守卫：只接受更大的周期序号，时钟落后的节点无法把规则拉回旧周期。
                    if (rule is null || rule.IsDeleted || rule.Status != EnableStatus.Enabled || rule.CurrentPeriodOrdinal >= periodOrdinal)
                    {
                        return Task.FromResult(false);
                    }

                    rule.CurrentValue = 0;
                    rule.CurrentPeriod = periodKey;
                    rule.CurrentPeriodOrdinal = periodOrdinal;
                    rule.RowVersion++;
                    return Task.FromResult(true);
                });
            RuleRepository
                .Setup(repository => repository.TryAdvanceSequenceAsync(
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns((long ownerTenantId, long ruleId, string periodKey, int count, long maximum, CancellationToken _) =>
                {
                    Interlocked.Increment(ref _advanceAttempts);
                    EnsureInTransaction();
                    AcquireRuleLock(ownerTenantId, ruleId);
                    var rule = FindRule(ownerTenantId, ruleId);
                    if (rule is null
                        || rule.IsDeleted
                        || rule.Status != EnableStatus.Enabled
                        || !string.Equals(rule.CurrentPeriod, periodKey, StringComparison.Ordinal)
                        || rule.CurrentValue > maximum - count)
                    {
                        return Task.FromResult(false);
                    }

                    rule.CurrentValue += count;
                    rule.HasAllocated = true;
                    rule.RowVersion++;
                    return Task.FromResult(true);
                });
            // 库隔离部署下平台库与租户库是两条连接；共享库部署下两者同为默认连接。
            RuleRepository
                .Setup(repository => repository.GetCurrentDatabaseId())
                .Returns(() => SimulateIsolatedTenantDatabase && _currentTenantId.Value is { } tenantId
                    ? $"Tenant_{tenantId}"
                    : "Default");
            RuleRepository
                .Setup(repository => repository.ReadCurrentValueAsync(
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns((long ownerTenantId, long ruleId, CancellationToken _) =>
                {
                    EnsureInTransaction();
                    AcquireRuleLock(ownerTenantId, ruleId);
                    var rule = FindRule(ownerTenantId, ruleId);
                    return Task.FromResult(rule is null
                        ? (NumberingSequenceState?)null
                        : new NumberingSequenceState(
                            rule.CurrentValue,
                            rule.CurrentPeriod,
                            rule.CurrentPeriodOrdinal,
                            rule.Status,
                            rule.SerialLength));
                });

            AllocationRepository
                .Setup(repository => repository.FindByIdempotencyKeyAsync(
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long ownerTenantId, long ruleId, long requestTenantId, string idempotencyKey, CancellationToken _) =>
                {
                    _allocations.TryGetValue(new AllocationKey(ownerTenantId, ruleId, requestTenantId, idempotencyKey), out var allocation);
                    return allocation;
                });
            AllocationRepository
                .Setup(repository => repository.AddAsync(It.IsAny<SysNumberingAllocation>(), It.IsAny<CancellationToken>()))
                .Returns((SysNumberingAllocation allocation, CancellationToken _) =>
                {
                    AllocationWriteContexts.Add(_currentTenantId.Value);
                    allocation.TenantId = _currentTenantId.Value ?? 0;
                    SetEntityId(allocation, Interlocked.Increment(ref _nextAllocationId));
                    var key = new AllocationKey(
                        allocation.TenantId,
                        allocation.RuleId,
                        allocation.RequestTenantId,
                        allocation.IdempotencyKey);
                    if (!_allocations.TryAdd(key, allocation))
                    {
                        throw new InvalidOperationException("测试仓储检测到重复幂等记录。");
                    }

                    return Task.FromResult(allocation);
                });

            var currentTenant = new Mock<ICurrentTenant>();
            currentTenant.SetupGet(tenant => tenant.Id).Returns(() => _currentTenantId.Value);
            currentTenant.SetupGet(tenant => tenant.IsAvailable).Returns(() => _currentTenantId.Value.HasValue);
            currentTenant
                .Setup(tenant => tenant.Change(It.IsAny<long?>(), It.IsAny<string?>()))
                .Returns((long? tenantId, string? _) => ChangeTenant(tenantId));

            var currentUser = new Mock<ICurrentUser>();
            currentUser.SetupGet(user => user.UserId).Returns(9001);

            // 工作单元替身复现框架的两条关键语义：
            // 已有环境工作单元时 Begin 返回子工作单元（CompleteAsync 为空实现，由外层提交）；
            // 没有时创建真正的事务并在 CompleteAsync 时释放持有的行锁。
            var unitOfWorkManager = new Mock<IUnitOfWorkManager>();
            unitOfWorkManager
                .Setup(manager => manager.Begin(It.IsAny<XiHanUnitOfWorkOptions>(), It.IsAny<bool>()))
                .Returns((XiHanUnitOfWorkOptions options, bool requiresNew) => BeginTransaction(options, requiresNew));

            Generator = new NumberGenerator(
                RuleRepository.Object,
                AllocationRepository.Object,
                new NumberingFormatter(),
                currentTenant.Object,
                currentUser.Object,
                unitOfWorkManager.Object,
                // 必须延迟读取：CurrentUtcNow 是 init 属性，对象初始化器在构造函数体之后才赋值，
                // 构造期直接取值只会拿到默认时刻，用例设定的时钟将静默失效。
                new FixedTimeProvider(() => CurrentUtcNow),
                NullLogger<NumberGenerator>.Instance);
        }

        /// <summary>被测生产发号器。</summary>
        public NumberGenerator Generator { get; }

        /// <summary>规则仓储模拟对象。</summary>
        public Mock<INumberingRuleRepository> RuleRepository { get; }

        /// <summary>分配仓储模拟对象。</summary>
        public Mock<INumberingAllocationRepository> AllocationRepository { get; }

        /// <summary>规则查询发生时的租户上下文快照。</summary>
        public ConcurrentBag<RuleReadContext> RuleReadContexts { get; }

        /// <summary>分配写入发生时的租户上下文快照。</summary>
        public ConcurrentBag<long?> AllocationWriteContexts { get; }

        /// <summary>是否模拟只有切换连接后才能看到平台库的独立数据库租户。</summary>
        public bool SimulateIsolatedTenantDatabase { get; init; }

        /// <summary>被测发号器使用的固定 UTC 时刻，可按用例改写以跨越周期边界。</summary>
        public DateTimeOffset CurrentUtcNow { get; init; } = new(2026, 7, 27, 8, 30, 0, TimeSpan.Zero);

        /// <summary>流水推进语句的执行次数，用于断言不再存在冲突重试。</summary>
        public int AdvanceAttempts => Volatile.Read(ref _advanceAttempts);

        /// <summary>自行开启并提交的事务数量；调用方事务与发号自有事务各计一次。</summary>
        public int OwnTransactionCount => Volatile.Read(ref _ownTransactionCount);

        /// <summary>当前永久分配记录快照。</summary>
        public IReadOnlyCollection<SysNumberingAllocation> Allocations => _allocations.Values.ToArray();

        /// <summary>
        /// 新增一条内存规则。
        /// </summary>
        public SysNumberingRule AddRule(
            long id,
            long tenantId,
            string ruleCode,
            string? prefix,
            bool allowTenantUse,
            int serialLength = 4,
            NumberingDateFormat dateFormat = NumberingDateFormat.None,
            NumberingResetCycle resetCycle = NumberingResetCycle.Never)
        {
            var rule = new SysNumberingRule
            {
                TenantId = tenantId,
                RuleCode = ruleCode,
                RuleName = ruleCode,
                Prefix = prefix,
                Separator = "-",
                DateFormat = dateFormat,
                SerialLength = serialLength,
                ResetCycle = resetCycle,
                TimeZoneId = "UTC",
                AllowTenantUse = allowTenantUse,
                Status = EnableStatus.Enabled
            };
            SetEntityId(rule, id);
            Assert.True(_rules.TryAdd((tenantId, ruleCode), rule));
            return rule;
        }

        /// <summary>
        /// 为已发号规则设置一致的周期基线。
        /// </summary>
        /// <param name="rule">目标规则。</param>
        /// <param name="periodKey">周期键。</param>
        /// <param name="periodOrdinal">与周期键同源的周期序号。</param>
        /// <param name="currentValue">该周期内已分配到的流水值。</param>
        /// <remarks>
        /// 周期键、周期序号与已发号标记必须成组设置：三者不同步的规则在生产中不可能出现，
        /// 用这种数据构造出的用例只会测出假象。
        /// </remarks>
        public static void SeedBaseline(SysNumberingRule rule, string periodKey, long periodOrdinal, long currentValue)
        {
            rule.CurrentPeriod = periodKey;
            rule.CurrentPeriodOrdinal = periodOrdinal;
            rule.CurrentValue = currentValue;
            rule.HasAllocated = true;
        }

        /// <summary>
        /// 在指定租户执行异步动作并在结束后恢复调用上下文。
        /// </summary>
        public async Task<TResult> RunAsTenantAsync<TResult>(long? tenantId, Func<Task<TResult>> action)
        {
            using var scope = ChangeTenant(tenantId);
            return await action();
        }

        /// <summary>
        /// 在模拟事务外执行动作，用于验证发号器的 fail-closed 断言。
        /// </summary>
        /// <remarks>把当前执行流标记为「已存在非事务型工作单元」，使发号器无法自行开启事务。</remarks>
        public async Task<TResult> RunWithoutTransactionAsync<TResult>(Func<Task<TResult>> action)
        {
            var previous = _ambientTransaction.Value;
            _ambientTransaction.Value = FakeTransaction.NonTransactional;
            try
            {
                return await action();
            }
            finally
            {
                _ambientTransaction.Value = previous;
            }
        }

        /// <summary>
        /// 开启或加入模拟事务。
        /// </summary>
        /// <param name="options">工作单元选项；非事务型选项不会建立事务。</param>
        /// <param name="requiresNew">框架语义下是否要求新的工作单元。</param>
        /// <returns>可释放的工作单元替身。</returns>
        private IUnitOfWork BeginTransaction(XiHanUnitOfWorkOptions options, bool requiresNew)
        {
            // 发号器只在「目标库与调用方事务不在同一条连接上」时传 requiresNew，
            // 那条连接上必然没有调用方的事务，因此这里按「必定拿到真实事务」建模。
            if (_ambientTransaction.Value is not null && !requiresNew)
            {
                // 子工作单元：不接管提交，也不释放外层持有的行锁。
                return new FakeUnitOfWork(null);
            }

            var previous = _ambientTransaction.Value;
            var transaction = options.IsTransactional
                ? new FakeTransaction(ReleaseLocks)
                : FakeTransaction.NonTransactional;
            if (transaction.IsTransactional)
            {
                Interlocked.Increment(ref _ownTransactionCount);
            }

            _ambientTransaction.Value = transaction;
            return new FakeUnitOfWork(() =>
            {
                transaction.Release();
                _ambientTransaction.Value = previous;
            });
        }

        /// <summary>
        /// 以调用方身份开启一个环境事务，用于验证发号是加入它还是自行提交。
        /// </summary>
        public async Task<TResult> RunInCallerTransactionAsync<TResult>(Func<Task<TResult>> action)
        {
            using var unitOfWork = BeginTransaction(new XiHanUnitOfWorkOptions(isTransactional: true), false);
            var result = await action();
            await unitOfWork.CompleteAsync();
            return result;
        }

        /// <summary>
        /// 断言当前执行流处于模拟事务内，对应仓储实现的连接级事务断言。
        /// </summary>
        /// <exception cref="InvalidOperationException">当前没有活动事务。</exception>
        private void EnsureInTransaction()
        {
            if (_ambientTransaction.Value is not { IsTransactional: true })
            {
                throw new InvalidOperationException("业务编号流水推进必须在数据库事务内执行，当前连接上没有活动事务。");
            }
        }

        /// <summary>
        /// 取得规则行锁并登记到当前事务，锁一直持有到事务提交。
        /// </summary>
        /// <remarks>这正是「推进后读回值等于本次推进结果」的依据；同一事务重复取同一把锁不会自锁。</remarks>
        private void AcquireRuleLock(long ownerTenantId, long ruleId)
        {
            var transaction = _ambientTransaction.Value!;
            var key = (ownerTenantId, ruleId);
            if (!transaction.TryTrackLock(key))
            {
                return;
            }

            _ruleLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1)).Wait();
        }

        /// <summary>
        /// 释放一个事务持有的全部规则行锁。
        /// </summary>
        private void ReleaseLocks(IEnumerable<(long OwnerTenantId, long RuleId)> keys)
        {
            foreach (var key in keys)
            {
                _ruleLocks[key].Release();
            }
        }

        /// <summary>
        /// 按所属租户和主键定位内存规则。
        /// </summary>
        private SysNumberingRule? FindRule(long ownerTenantId, long ruleId)
        {
            return _rules.Values.SingleOrDefault(rule => rule.TenantId == ownerTenantId && rule.BasicId == ruleId);
        }

        /// <summary>
        /// 模拟独立数据库路由时检查规则所属库是否与当前连接一致。
        /// </summary>
        private bool IsVisibleInCurrentDatabase(long ownerTenantId)
        {
            return ownerTenantId == 0
                ? _currentTenantId.Value is null
                : _currentTenantId.Value == ownerTenantId;
        }

        /// <summary>
        /// 切换 AsyncLocal 租户并返回可恢复作用域。
        /// </summary>
        private IDisposable ChangeTenant(long? tenantId)
        {
            var previous = _currentTenantId.Value;
            _currentTenantId.Value = tenantId;
            return new DelegateDisposable(() => _currentTenantId.Value = previous);
        }

        /// <summary>
        /// 模拟 SqlSugar 插入后对受保护实体主键的回填。
        /// </summary>
        private static void SetEntityId(object entity, long id)
        {
            var property = entity.GetType().GetProperty(
                "BasicId",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("未找到实体主键属性。");
            property.SetValue(entity, id);
        }
    }

    /// <summary>
    /// 模拟数据库事务：登记本事务持有的行锁，提交或释放时一次性归还。
    /// </summary>
    /// <remarks>
    /// 只模拟「锁持有到提交」这一条与发号正确性直接相关的语义，不模拟回滚撤销数据变更，
    /// 因此不要用它断言回滚行为。
    /// </remarks>
    private sealed class FakeTransaction
    {
        /// <summary>表示存在工作单元但不是事务型，对应发号器必须 fail-closed 的场景。</summary>
        public static readonly FakeTransaction NonTransactional = new();

        private readonly Action<IEnumerable<(long OwnerTenantId, long RuleId)>>? _release;
        private readonly HashSet<(long OwnerTenantId, long RuleId)> _heldLocks = [];

        private FakeTransaction()
        {
        }

        public FakeTransaction(Action<IEnumerable<(long OwnerTenantId, long RuleId)>> release)
        {
            _release = release;
        }

        /// <summary>是否为事务型；非事务型工作单元下所有写语句都必须被拒绝。</summary>
        public bool IsTransactional => _release is not null;

        /// <summary>
        /// 登记一把行锁；返回 false 表示本事务已持有，调用方不应重复等待。
        /// </summary>
        public bool TryTrackLock((long OwnerTenantId, long RuleId) key)
        {
            lock (_heldLocks)
            {
                return _heldLocks.Add(key);
            }
        }

        /// <summary>归还本事务持有的全部行锁。</summary>
        public void Release()
        {
            lock (_heldLocks)
            {
                _release?.Invoke(_heldLocks.ToArray());
                _heldLocks.Clear();
            }
        }
    }

    /// <summary>
    /// 工作单元替身；子工作单元的完成与释放均为空实现，与框架 <c>ChildUnitOfWork</c> 一致。
    /// </summary>
    private sealed class FakeUnitOfWork(Action? commit) : IUnitOfWork
    {
        private int _finished;

        public event EventHandler<UnitOfWorkFailedEventArgs> Failed = default!;

        public event EventHandler<UnitOfWorkEventArgs> Disposed = default!;

        public Guid Id { get; } = Guid.NewGuid();

        public IXiHanUnitOfWorkOptions Options { get; } = new XiHanUnitOfWorkOptions(isTransactional: true);

        public IUnitOfWork? Outer => null;

        public bool IsReserved => false;

        public bool IsDisposed => Volatile.Read(ref _finished) == 1;

        public bool IsCompleted => Volatile.Read(ref _finished) == 1;

        public string? ReservationName => null;

        public IServiceProvider ServiceProvider => throw new NotSupportedException();

        public Dictionary<string, object> Items { get; } = [];

        public void SetOuter(IUnitOfWork? outer)
        {
        }

        public void Initialize(XiHanUnitOfWorkOptions options)
        {
        }

        public void Reserve(string reservationName)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            Finish();
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Finish();
            return Task.CompletedTask;
        }

        public void OnCompleted(Func<Task> handler)
        {
        }

        public void AddOrReplaceLocalEvent(UnitOfWorkEventRecord eventRecord, Predicate<UnitOfWorkEventRecord>? replacementSelector = null)
        {
        }

        public void AddOrReplaceDistributedEvent(UnitOfWorkEventRecord eventRecord, Predicate<UnitOfWorkEventRecord>? replacementSelector = null)
        {
        }

        public IDatabaseApi? FindDatabaseApi(string key) => null;

        public void AddDatabaseApi(string key, IDatabaseApi api)
        {
        }

        public IDatabaseApi GetOrAddDatabaseApi(string key, Func<IDatabaseApi> factory) => factory();

        public ITransactionApi? FindTransactionApi(string key) => null;

        public void AddTransactionApi(string key, ITransactionApi api)
        {
        }

        public ITransactionApi GetOrAddTransactionApi(string key, Func<ITransactionApi> factory) => factory();

        public void Dispose()
        {
            Finish();
            Disposed?.Invoke(this, new UnitOfWorkEventArgs(this));
            Failed?.Invoke(this, new UnitOfWorkFailedEventArgs(this, null, false));
        }

        /// <summary>提交或释放只生效一次，避免重复归还行锁。</summary>
        private void Finish()
        {
            if (Interlocked.Exchange(ref _finished, 1) == 0)
            {
                commit?.Invoke();
            }
        }
    }

    /// <summary>
    /// 固定 UTC 时间提供器，使周期测试不依赖机器时钟。
    /// </summary>
    private sealed class FixedTimeProvider(Func<DateTimeOffset> utcNow) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => utcNow();
    }

    /// <summary>
    /// 通过委托恢复一次上下文切换。
    /// </summary>
    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        private int _disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                dispose();
            }
        }
    }

    /// <summary>幂等记录内存键。</summary>
    private sealed record AllocationKey(long OwnerTenantId, long RuleId, long RequestTenantId, string IdempotencyKey);

    /// <summary>规则读取及其租户上下文快照。</summary>
    public sealed record RuleReadContext(long OwnerTenantId, long? CurrentTenantId);
}
