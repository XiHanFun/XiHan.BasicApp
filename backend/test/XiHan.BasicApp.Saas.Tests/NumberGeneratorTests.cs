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
using XiHan.Framework.Domain.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
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
        rule.CurrentPeriod = "20260726";
        rule.CurrentValue = 998;
        rule.HasAllocated = true;

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
        rule.CurrentPeriod = "never";
        rule.CurrentValue = 9;
        rule.HasAllocated = true;

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("SHORT", NumberingScope.Tenant, "overflow"))));

        Assert.Contains("流水已耗尽", exception.Message, StringComparison.Ordinal);
        Assert.Equal(9, rule.CurrentValue);
        Assert.Empty(fixture.Allocations);
    }

    /// <summary>
    /// 乐观锁连续冲突五次后必须停止重试并返回可重放的友好错误。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ConcurrencyRetryExhaustedShouldReturnFriendlyError()
    {
        var fixture = new GeneratorFixture { AlwaysThrowConcurrencyConflict = true };
        fixture.AddRule(701, 7, "ORDER", "TEN", allowTenantUse: false);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.RunAsTenantAsync(
            7,
            () => fixture.Generator.GenerateAsync(new NumberGenerateRequest("ORDER", NumberingScope.Tenant, "retry-key"))));

        Assert.Contains("并发繁忙", exception.Message, StringComparison.Ordinal);
        Assert.Equal(5, fixture.UpdateAttempts);
        Assert.Empty(fixture.Allocations);
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
        private long _nextAllocationId;
        private int _updateAttempts;

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
            RuleRepository
                .Setup(repository => repository.UpdateAsync(It.IsAny<SysNumberingRule>(), It.IsAny<CancellationToken>()))
                .Returns((SysNumberingRule rule, CancellationToken _) =>
                {
                    Interlocked.Increment(ref _updateAttempts);
                    if (AlwaysThrowConcurrencyConflict)
                    {
                        throw new ConcurrencyConflictException("模拟 RowVersion 冲突");
                    }

                    return Task.FromResult(rule);
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

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork
                .Setup(work => work.CompleteAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var unitOfWorkManager = new Mock<IUnitOfWorkManager>();
            unitOfWorkManager
                .Setup(manager => manager.Begin(It.IsAny<XiHanUnitOfWorkOptions>(), true))
                .Returns(unitOfWork.Object);

            Generator = new NumberGenerator(
                RuleRepository.Object,
                AllocationRepository.Object,
                new NumberingFormatter(),
                new NumberingLockProvider(),
                currentTenant.Object,
                currentUser.Object,
                unitOfWorkManager.Object,
                new FixedTimeProvider(new DateTimeOffset(2026, 7, 27, 8, 30, 0, TimeSpan.Zero)),
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

        /// <summary>是否让每次规则更新都抛出乐观锁冲突。</summary>
        public bool AlwaysThrowConcurrencyConflict { get; init; }

        /// <summary>规则更新尝试次数。</summary>
        public int UpdateAttempts => Volatile.Read(ref _updateAttempts);

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
        /// 在指定租户执行异步动作并在结束后恢复调用上下文。
        /// </summary>
        public async Task<TResult> RunAsTenantAsync<TResult>(long? tenantId, Func<Task<TResult>> action)
        {
            using var scope = ChangeTenant(tenantId);
            return await action();
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
    /// 固定 UTC 时间提供器，使周期测试不依赖机器时钟。
    /// </summary>
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => utcNow;
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
