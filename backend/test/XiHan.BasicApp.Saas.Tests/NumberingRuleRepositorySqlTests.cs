// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 业务编号流水推进语句的真实数据库行为测试。
/// </summary>
/// <remarks>
/// <para>
/// 这些用例跑在真实的 SQLite 引擎上，执行的是生产仓储代码本身，因此覆盖了纯内存替身覆盖不到的三件事：
/// ORM 是否把自引用累加翻译成数据库端算术、守卫是否真的落进 WHERE、以及连接级事务断言是否生效。
/// </para>
/// <para>
/// 不覆盖的部分要说清楚：SQLite 是库级写锁且没有 MVCC，无法复现 InnoDB / PostgreSQL 的行级锁与快照语义，
/// 所以「推进后读回值等于本次推进结果」这条跨会话不变量在这里测不到，
/// 它由 <see cref="NumberGeneratorTests"/> 的模拟行锁用例和生产库的行锁共同保证。
/// </para>
/// </remarks>
public sealed class NumberingRuleRepositorySqlTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-numbering-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;
    private readonly NumberingRuleRepository _repository;

    /// <summary>
    /// 建库建表并把生产仓储接到该连接上。
    /// </summary>
    public NumberingRuleRepositorySqlTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            // 关闭连接池，否则驱动会缓存连接、在用例结束后仍持有临时库文件句柄导致无法清理。
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = false
        });
        _client.CodeFirst.InitTables<SysNumberingRule>();
        _repository = new NumberingRuleRepository(new SingleClientResolver(_client));
    }

    /// <summary>
    /// 没有活动事务时推进语句必须直接失败，而不是在无锁保护下写库。
    /// </summary>
    [Fact]
    public async Task TryAdvanceSequenceAsync_WithoutTransactionShouldThrow()
    {
        InsertRule(ordinal: 0, period: "never", currentValue: 0);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.TryAdvanceSequenceAsync(0, 1, "never", 1, 9999));

        Assert.Contains("没有活动事务", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 推进语句必须在数据库端完成累加，并把行版本恰好推进 1。
    /// </summary>
    /// <remarks>
    /// 行版本自增是阻止管理端整实体回写把流水打回旧值的唯一手段，
    /// 而它依赖 SqlSugar 把 <c>RowVersion = RowVersion + 1</c> 翻译成数据库端算术这一未公开行为。
    /// 该断言就是升级 SqlSugarCore 时的第一道闸。
    /// </remarks>
    [Fact]
    public async Task TryAdvanceSequenceAsync_ShouldAddInDatabaseAndBumpRowVersion()
    {
        InsertRule(ordinal: 0, period: "never", currentValue: 40, rowVersion: 7);

        _client.Ado.BeginTran();
        var advanced = await _repository.TryAdvanceSequenceAsync(0, 1, "never", 5, 9999);
        _client.Ado.CommitTran();

        Assert.True(advanced);
        var stored = ReadRule();
        Assert.Equal(45, stored.CurrentValue);
        Assert.Equal(8, stored.RowVersion);
        Assert.True(stored.HasAllocated);
    }

    /// <summary>
    /// 容量守卫必须落在 WHERE 里：越界时命中 0 行且流水保持不变。
    /// </summary>
    [Fact]
    public async Task TryAdvanceSequenceAsync_ShouldRejectWhenCapacityExceeded()
    {
        InsertRule(ordinal: 0, period: "never", currentValue: 9);

        _client.Ado.BeginTran();
        var advanced = await _repository.TryAdvanceSequenceAsync(0, 1, "never", 1, 9);
        _client.Ado.CommitTran();

        Assert.False(advanced);
        Assert.Equal(9, ReadRule().CurrentValue);
    }

    /// <summary>
    /// 周期键不匹配时不得推进，这是时钟落后的节点无法借用当前周期流水的直接原因。
    /// </summary>
    [Fact]
    public async Task TryAdvanceSequenceAsync_ShouldRejectWhenPeriodDiffers()
    {
        InsertRule(ordinal: 20260728, period: "20260728", currentValue: 42);

        _client.Ado.BeginTran();
        var advanced = await _repository.TryAdvanceSequenceAsync(0, 1, "20260727", 1, 9999);
        _client.Ado.CommitTran();

        Assert.False(advanced);
        Assert.Equal(42, ReadRule().CurrentValue);
    }

    /// <summary>
    /// 周期翻转只接受更大的周期序号，并在翻转时把流水归零。
    /// </summary>
    [Fact]
    public async Task TryRollOverPeriodAsync_ShouldOnlyMoveForward()
    {
        InsertRule(ordinal: 20260728, period: "20260728", currentValue: 42);

        _client.Ado.BeginTran();
        var backward = await _repository.TryRollOverPeriodAsync(0, 1, "20260727", 20260727);
        var forward = await _repository.TryRollOverPeriodAsync(0, 1, "20260729", 20260729);
        _client.Ado.CommitTran();

        Assert.False(backward);
        Assert.True(forward);
        var stored = ReadRule();
        Assert.Equal("20260729", stored.CurrentPeriod);
        Assert.Equal(20260729, stored.CurrentPeriodOrdinal);
        Assert.Equal(0, stored.CurrentValue);
    }

    /// <summary>
    /// 未建立周期基线的规则必须能完成首次翻转，包括周期序号恒为 0 的「永不重置」规则。
    /// </summary>
    [Fact]
    public async Task TryRollOverPeriodAsync_ShouldBootstrapNeverResetRule()
    {
        InsertRule(ordinal: SysNumberingRule.NoPeriodBaseline, period: string.Empty, currentValue: 0);

        _client.Ado.BeginTran();
        var rolledOver = await _repository.TryRollOverPeriodAsync(0, 1, "never", 0);
        var advanced = await _repository.TryAdvanceSequenceAsync(0, 1, "never", 1, 9999);
        _client.Ado.CommitTran();

        Assert.True(rolledOver);
        Assert.True(advanced);
        Assert.Equal(1, ReadRule().CurrentValue);
    }

    /// <summary>
    /// 停用规则不得推进流水。
    /// </summary>
    [Fact]
    public async Task TryAdvanceSequenceAsync_ShouldRejectDisabledRule()
    {
        InsertRule(ordinal: 0, period: "never", currentValue: 0, status: EnableStatus.Disabled);

        _client.Ado.BeginTran();
        var advanced = await _repository.TryAdvanceSequenceAsync(0, 1, "never", 1, 9999);
        _client.Ado.CommitTran();

        Assert.False(advanced);
        Assert.Equal(0, ReadRule().CurrentValue);
    }

    /// <summary>
    /// 释放占用的资源
    /// </summary>
    public void Dispose()
    {
        _client.Ado.Connection.Close();
        _client.Dispose();
        SaasTestHelper.DeleteTemporaryDatabase(_databasePath);
    }

    /// <summary>
    /// 直接写入一条测试规则，绕开仓储写路径以免掩盖被测语句的行为。
    /// </summary>
    private void InsertRule(
        long ordinal,
        string period,
        long currentValue,
        long rowVersion = 0,
        EnableStatus status = EnableStatus.Enabled)
    {
        var rule = new SysNumberingRule
        {
            TenantId = 0,
            RowVersion = rowVersion,
            RuleCode = "ORDER",
            RuleName = "订单编号",
            Separator = "-",
            DateFormat = NumberingDateFormat.None,
            SerialLength = 4,
            ResetCycle = NumberingResetCycle.Never,
            TimeZoneId = "UTC",
            CurrentValue = currentValue,
            CurrentPeriod = period,
            CurrentPeriodOrdinal = ordinal,
            Status = status,
            CreatedTime = DateTimeOffset.UnixEpoch
        };
        // 主键 setter 对外不可见，测试沿用与生成器用例一致的反射回填方式模拟 SqlSugar 的主键赋值。
        typeof(SysNumberingRule)
            .GetProperty(nameof(SysNumberingRule.BasicId), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(rule, 1L);
        _ = _client.Insertable(rule).ExecuteCommand();
    }

    /// <summary>
    /// 从库中重新读取规则，确保断言看到的是落库后的值而不是内存对象。
    /// </summary>
    private SysNumberingRule ReadRule()
    {
        return _client.Queryable<SysNumberingRule>().Single(rule => rule.BasicId == 1);
    }

    /// <summary>
    /// 把仓储固定接到单一测试连接上的解析器替身。
    /// </summary>
    private sealed class SingleClientResolver(ISqlSugarClient client) : ISqlSugarClientResolver
    {
        /// <summary>
        /// 获取当前租户对应的客户端
        /// </summary>
        /// <returns>当前 Scope 级客户端</returns>
        public ISqlSugarClient GetCurrentClient() => client;

        /// <summary>
        /// 获取实体对应的客户端
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <returns>Scope 级客户端</returns>
        public ISqlSugarClient GetClientForEntity(Type entityType) => client;

        /// <summary>
        /// 按 ConfigId 获取指定客户端
        /// </summary>
        /// <param name="configId">连接配置标识</param>
        /// <returns>Scope 级客户端</returns>
        public ISqlSugarClient GetClient(string configId) => client;

        /// <summary>
        /// 获取全部连接配置标识
        /// </summary>
        public IReadOnlyCollection<string> GetAllConfigIds() => [];

        /// <summary>
        /// 按顺序获取所有库的客户端（初始化/种子数据等场景使用）
        /// </summary>
        public IEnumerable<ISqlSugarClient> GetAllClients() => [client];

        /// <summary>
        /// 底层 SqlSugarScope（仅在需要多库切换/租户管理等高级场景使用）
        /// </summary>
        public ITenant AsTenant() => throw new NotSupportedException();
    }
}
