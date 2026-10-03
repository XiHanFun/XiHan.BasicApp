// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 用户统计聚合任务（真实 SQLite 按月分表 + 与生产同口径的严格隔离过滤器）。
/// </summary>
/// <remarks>
/// 回归锚点：聚合任务只汇总登录、访问、操作与会话，从不统计开放接口日志，用户管理与个人中心展示的接口调用数恒为 0。
/// 接口调用数是请求时间落在统计周期内的开放接口日志行数，按作用域（平台与每个租户）分别聚合，每个作用域另有 UserId=0 的全员汇总行。
/// 回归锚点：聚合只查本月起点之后的日志与会话，月初几天本周从上月末的周一算起，上月末那几天的活动从本周的每项计数里漏掉。
/// </remarks>
public sealed class UserStatisticsAggregationTaskSqlTests : IDisposable
{
    private const long TenantId = 11;
    private const long PlatformUserId = 101;
    private const long OtherPlatformUserId = 102;
    private const long AccessOnlyUserId = 103;
    private const long TenantUserId = 201;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-user-statistics-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;
    private readonly TestCurrentTenant _currentTenant = new();
    private readonly FixedTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);
    private readonly UserStatisticsAggregationTask _task;
    private long _nextId;

    /// <summary>
    /// 建库与日志分表，挂与生产同口径的过滤器，装配真实的逐作用域执行器（平台 + 一个字段隔离租户）
    /// </summary>
    public UserStatisticsAggregationTaskSqlTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
        _client.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
        _client.QueryFilter.AddTableFilter<IStrictMultiTenantEntity>(entity => entity.TenantId == (_currentTenant.Id ?? 0));
        // 生产由雪花 ID 填主键，这里插入时按序给 IEntityBase<long> 主键赋值
        _client.Aop.DataExecuting = (_, entityInfo) =>
        {
            if (entityInfo.OperationType == DataFilterType.InsertByObject
                && entityInfo.EntityColumnInfo.IsPrimarykey
                && entityInfo.EntityValue is IEntityBase<long> { BasicId: 0 })
            {
                entityInfo.SetValue(Interlocked.Increment(ref _nextId));
            }
        };
        // SQLite 只存墙钟时间、丢掉偏移，读回时套上本机偏移；用例全按 UTC 写入，读回后按墙钟还原为 UTC
        _client.Aop.DataExecuted = (_, entityInfo) =>
        {
            foreach (var property in entityInfo.EntityColumnInfos.Select(column => column.PropertyInfo))
            {
                if ((property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?))
                    && property.GetValue(entityInfo.EntityValue) is DateTimeOffset value)
                {
                    property.SetValue(entityInfo.EntityValue, new DateTimeOffset(value.DateTime, TimeSpan.Zero));
                }
            }
        };
        _client.CodeFirst.SplitTables().InitTables<SysLoginLog>();
        _client.CodeFirst.SplitTables().InitTables<SysAccessLog>();
        _client.CodeFirst.SplitTables().InitTables<SysOperationLog>();
        _client.CodeFirst.SplitTables().InitTables<SysOpenApiLog>();
        _client.CodeFirst.InitTables<SysUserSession, SysUserStatistics>();

        var tenant = new SysTenant
        {
            TenantCode = $"t{TenantId}",
            TenantName = $"租户{TenantId}",
            IsolationMode = TenantIsolationMode.Field,
            ConfigStatus = TenantConfigStatus.Configured
        };
        SaasTestHelper.SetBasicId(tenant, TenantId);
        var tenantRepository = new Mock<ITenantRepository>();
        _ = tenantRepository
            .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysTenant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([tenant]);

        _task = new UserStatisticsAggregationTask(
            new FixedClientResolver(_client),
            new TenantDataScopeRunner(tenantRepository.Object, _currentTenant),
            _timeProvider,
            NullLogger<UserStatisticsAggregationTask>.Instance);
    }

    /// <summary>
    /// 接口调用按用户与周期累加，平台与租户各自聚合并各有全员汇总行；匿名调用与上月初的调用不计入，只有访问没有调用的用户记 0
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CountsApiCallsPerUserAndPeriod_WithScopeTotals()
    {
        var window = PeriodWindow.At(_timeProvider.GetUtcNow());
        var todayTime = window.TodayStart.AddMinutes(1);
        var earlierThisMonthTime = window.MonthStart.AddMinutes(1);
        var lastMonthTime = window.MonthStart.AddMonths(-1);

        // 访问日志让快照行无论有无接口调用都存在，接口调用数只来自开放接口日志
        InsertAccess(0, PlatformUserId, todayTime);
        InsertAccess(0, AccessOnlyUserId, todayTime);
        InsertAccess(TenantId, TenantUserId, todayTime);
        InsertApiCall(0, PlatformUserId, todayTime);
        InsertApiCall(0, PlatformUserId, todayTime);
        InsertApiCall(0, PlatformUserId, earlierThisMonthTime);
        InsertApiCall(0, PlatformUserId, lastMonthTime);
        InsertApiCall(0, OtherPlatformUserId, todayTime);
        InsertApiCall(0, null, todayTime);
        InsertApiCall(TenantId, TenantUserId, todayTime);
        InsertApiCall(TenantId, TenantUserId, todayTime);
        InsertApiCall(TenantId, TenantUserId, todayTime);
        InsertApiCall(TenantId, TenantUserId, earlierThisMonthTime);

        var summary = await _task.ExecuteAsync();

        Assert.StartsWith("用户统计聚合完成", summary);
        var expected = window.Periods
            .SelectMany(period =>
            {
                var platformUser = CountSince(period.Start, todayTime, todayTime, earlierThisMonthTime);
                var otherPlatformUser = CountSince(period.Start, todayTime);
                var tenantUser = CountSince(period.Start, todayTime, todayTime, todayTime, earlierThisMonthTime);
                return new[]
                {
                    (0L, PlatformUserId, period.Period, platformUser),
                    (0L, OtherPlatformUserId, period.Period, otherPlatformUser),
                    (0L, AccessOnlyUserId, period.Period, 0),
                    (0L, 0L, period.Period, platformUser + otherPlatformUser),
                    (TenantId, TenantUserId, period.Period, tenantUser),
                    (TenantId, 0L, period.Period, tenantUser)
                };
            })
            .Order()
            .ToArray();
        Assert.Equal(expected, ReadApiCallCounts(window.Today));
    }

    /// <summary>
    /// 再次聚合按最新日志改写已有快照的接口调用数（更新路径），不重复插入
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Rerun_RefreshesApiCallCountOfExistingSnapshots()
    {
        var window = PeriodWindow.At(_timeProvider.GetUtcNow());
        var todayTime = window.TodayStart.AddMinutes(1);
        InsertAccess(0, PlatformUserId, todayTime);
        InsertAccess(TenantId, TenantUserId, todayTime);
        InsertApiCall(0, PlatformUserId, todayTime);
        InsertApiCall(TenantId, TenantUserId, todayTime);
        _ = await _task.ExecuteAsync();

        InsertApiCall(0, PlatformUserId, todayTime);
        InsertApiCall(0, PlatformUserId, todayTime);
        InsertApiCall(TenantId, TenantUserId, todayTime);
        var summary = await _task.ExecuteAsync();

        Assert.StartsWith("用户统计聚合完成", summary);
        var expected = window.Periods
            .SelectMany(period => new[]
            {
                (0L, PlatformUserId, period.Period, 3),
                (0L, 0L, period.Period, 3),
                (TenantId, TenantUserId, period.Period, 2),
                (TenantId, 0L, period.Period, 2)
            })
            .Order()
            .ToArray();
        Assert.Equal(expected, ReadApiCallCounts(window.Today));
    }

    /// <summary>
    /// 月初几天本周从上月末的周一算起：上月末本周内的日志与会话计入本周、不计入本月，本周之前的都不计入；
    /// 只在上月末活动的用户也有本周快照。跨年时上月末的日志落在上一年的月表
    /// </summary>
    [Theory]
    [InlineData("2026-10-03T08:00:00Z", "2026-09-28T00:00:00Z", "2026-10-01T00:00:00Z")]
    [InlineData("2027-01-02T08:00:00Z", "2026-12-28T00:00:00Z", "2027-01-01T00:00:00Z")]
    public async Task ExecuteAsync_WeekStartsLastMonth_CountsLastMonthTailInThisWeekOnly(string now, string weekStartText, string monthStartText)
    {
        _timeProvider.UtcNow = ParseUtc(now);
        var weekStart = ParseUtc(weekStartText);
        var monthStart = ParseUtc(monthStartText);
        var today = DateOnly.FromDateTime(_timeProvider.UtcNow.UtcDateTime);
        var beforeWeekTime = weekStart.AddMinutes(-1);
        var weekHeadTime = weekStart.AddHours(1);
        var lastMonthEndTime = monthStart.AddMinutes(-1);
        var monthHeadTime = monthStart.AddHours(1);
        var todayTime = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddHours(1);

        // 平台用户在本周前、上月末本周内两处、本月、今日各有一条登录、访问、操作与接口调用，上月最后一分钟的操作失败
        foreach (var time in new[] { beforeWeekTime, weekHeadTime, lastMonthEndTime, monthHeadTime, todayTime })
        {
            InsertLogin(0, PlatformUserId, time);
            InsertAccess(0, PlatformUserId, time);
            InsertOperation(0, PlatformUserId, time, time == lastMonthEndTime ? OperationExecuteResult.Failed : OperationExecuteResult.Success);
            InsertApiCall(0, PlatformUserId, time);
        }

        // 会话：上月末本周内 30 分钟；跨月在线 2 小时（月前月后各 1 小时）；本周之前结束的不计
        InsertSession(0, PlatformUserId, weekHeadTime, weekHeadTime.AddMinutes(30));
        InsertSession(0, PlatformUserId, monthStart.AddHours(-1), monthStart.AddHours(1));
        InsertSession(0, PlatformUserId, beforeWeekTime.AddHours(-1), beforeWeekTime);

        // 租户用户只在上月末本周内活动
        InsertLogin(TenantId, TenantUserId, weekHeadTime);
        InsertAccess(TenantId, TenantUserId, weekHeadTime);
        InsertOperation(TenantId, TenantUserId, lastMonthEndTime, OperationExecuteResult.Failed);
        InsertApiCall(TenantId, TenantUserId, lastMonthEndTime);
        InsertSession(TenantId, TenantUserId, weekHeadTime, weekHeadTime.AddMinutes(10));

        var summary = await _task.ExecuteAsync();

        Assert.StartsWith("用户统计聚合完成", summary);
        var platformToday = new StatisticsSnapshot(1, 1, 1, 0, 1, 0, todayTime, todayTime, todayTime);
        var platformWeek = new StatisticsSnapshot(4, 4, 4, 1, 4, 30 * 60 + 2 * 3600, todayTime, todayTime, todayTime);
        var platformMonth = new StatisticsSnapshot(2, 2, 2, 0, 2, 3600, todayTime, todayTime, todayTime);
        var tenantWeek = new StatisticsSnapshot(1, 1, 1, 1, 1, 10 * 60, weekHeadTime, weekHeadTime, lastMonthEndTime);
        var expected = new Dictionary<(long TenantId, long UserId, StatisticsPeriod Period), StatisticsSnapshot>
        {
            [(0, PlatformUserId, StatisticsPeriod.Today)] = platformToday,
            [(0, PlatformUserId, StatisticsPeriod.ThisWeek)] = platformWeek,
            [(0, PlatformUserId, StatisticsPeriod.ThisMonth)] = platformMonth,
            [(0, 0, StatisticsPeriod.Today)] = platformToday,
            [(0, 0, StatisticsPeriod.ThisWeek)] = platformWeek,
            [(0, 0, StatisticsPeriod.ThisMonth)] = platformMonth,
            [(TenantId, TenantUserId, StatisticsPeriod.ThisWeek)] = tenantWeek,
            [(TenantId, 0, StatisticsPeriod.ThisWeek)] = tenantWeek
        };
        Assert.Equal(expected, ReadSnapshots(today));
    }

    /// <summary>
    /// 释放连接并清理临时库文件。
    /// </summary>
    public void Dispose()
    {
        _client.Ado.Connection.Close();
        _client.Dispose();
        SaasTestHelper.DeleteTemporaryDatabase(_databasePath);
    }

    private static int CountSince(DateTimeOffset periodStart, params DateTimeOffset[] requestTimes)
    {
        return requestTimes.Count(time => time >= periodStart);
    }

    private static DateTimeOffset ParseUtc(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
    }

    private void InsertLogin(long tenantId, long userId, DateTimeOffset loginTime)
    {
        _ = _client.Insertable(new SysLoginLog
        {
            TenantId = tenantId,
            UserId = userId,
            LoginResult = LoginResult.Success,
            LoginTime = loginTime,
            CreatedTime = loginTime
        }).SplitTable().ExecuteCommand();
    }

    private void InsertOperation(long tenantId, long userId, DateTimeOffset operationTime, OperationExecuteResult result)
    {
        _ = _client.Insertable(new SysOperationLog
        {
            TenantId = tenantId,
            UserId = userId,
            Result = result,
            OperationTime = operationTime,
            CreatedTime = operationTime
        }).SplitTable().ExecuteCommand();
    }

    private void InsertSession(long tenantId, long userId, DateTimeOffset loginTime, DateTimeOffset lastActivityTime)
    {
        // 未登出、未撤销：活跃区间止于最后活动时间
        _ = _client.Insertable(new SysUserSession
        {
            TenantId = tenantId,
            UserId = userId,
            UserSessionId = Guid.NewGuid().ToString("N"),
            LoginTime = loginTime,
            LastActivityTime = lastActivityTime,
            CreatedTime = loginTime
        }).ExecuteCommand();
    }

    private void InsertAccess(long tenantId, long userId, DateTimeOffset accessTime)
    {
        _ = _client.Insertable(new SysAccessLog
        {
            TenantId = tenantId,
            UserId = userId,
            ResourcePath = "/system/user",
            AccessTime = accessTime,
            CreatedTime = accessTime
        }).SplitTable().ExecuteCommand();
    }

    private void InsertApiCall(long tenantId, long? userId, DateTimeOffset requestTime)
    {
        // 分表字段是 CreatedTime：与请求时间同月，落到对应月表
        _ = _client.Insertable(new SysOpenApiLog
        {
            TenantId = tenantId,
            UserId = userId,
            ApiPath = "/api/open/ping",
            Method = "GET",
            RequestTime = requestTime,
            CreatedTime = requestTime
        }).SplitTable().ExecuteCommand();
    }

    private (long TenantId, long UserId, StatisticsPeriod Period, int ApiCallCount)[] ReadApiCallCounts(DateOnly statisticsDate)
    {
        return [.. _client.Queryable<SysUserStatistics>()
            .ClearFilter<IStrictMultiTenantEntity>()
            .Where(item => item.StatisticsDate == statisticsDate)
            .ToList()
            .Select(item => (item.TenantId, item.UserId, item.Period, item.ApiCallCount))
            .Order()];
    }

    private Dictionary<(long TenantId, long UserId, StatisticsPeriod Period), StatisticsSnapshot> ReadSnapshots(DateOnly statisticsDate)
    {
        return _client.Queryable<SysUserStatistics>()
            .ClearFilter<IStrictMultiTenantEntity>()
            .Where(item => item.StatisticsDate == statisticsDate)
            .ToList()
            .ToDictionary(
                item => (item.TenantId, item.UserId, item.Period),
                item => new StatisticsSnapshot(
                    item.LoginCount,
                    item.AccessCount,
                    item.OperationCount,
                    item.ErrorOperationCount,
                    item.ApiCallCount,
                    item.OnlineTime,
                    item.LastLoginTime,
                    item.LastAccessTime,
                    item.LastOperationTime));
    }

    /// <summary>
    /// 一行统计快照中由聚合任务写入的计数与最后时间
    /// </summary>
    private sealed record StatisticsSnapshot(
        int LoginCount,
        int AccessCount,
        int OperationCount,
        int ErrorOperationCount,
        int ApiCallCount,
        long OnlineTime,
        DateTimeOffset? LastLoginTime,
        DateTimeOffset? LastAccessTime,
        DateTimeOffset? LastOperationTime);

    /// <summary>
    /// 与任务同口径的 UTC 统计周期：今日 / 本周（周一起） / 本月
    /// </summary>
    private sealed record PeriodWindow(DateOnly Today, DateTimeOffset TodayStart, DateTimeOffset MonthStart, (StatisticsPeriod Period, DateTimeOffset Start)[] Periods)
    {
        public static PeriodWindow At(DateTimeOffset now)
        {
            var today = DateOnly.FromDateTime(now.UtcDateTime.Date);
            var todayStart = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var weekStart = todayStart.AddDays(-(((int)todayStart.DayOfWeek + 6) % 7));
            var monthStart = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);
            return new PeriodWindow(today, todayStart, monthStart,
            [
                (StatisticsPeriod.Today, todayStart),
                (StatisticsPeriod.ThisWeek, weekStart),
                (StatisticsPeriod.ThisMonth, monthStart)
            ]);
        }
    }

    /// <summary>
    /// 可设定的 UTC 时间提供器，使周期边界不依赖机器时钟。
    /// </summary>
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        /// <summary>
        /// 当前 UTC 时间
        /// </summary>
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        /// <summary>
        /// 获取当前 UTC 时间
        /// </summary>
        /// <returns>当前 UTC 时间</returns>
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    /// <summary>
    /// 固定返回同一客户端的解析器替身。
    /// </summary>
    private sealed class FixedClientResolver(ISqlSugarClient client) : ISqlSugarClientResolver
    {
        /// <summary>
        /// 获取当前客户端。
        /// </summary>
        public ISqlSugarClient GetCurrentClient() => client;

        /// <summary>
        /// 获取实体对应的客户端。
        /// </summary>
        public ISqlSugarClient GetClientForEntity(Type entityType) => client;

        /// <summary>
        /// 按 ConfigId 获取指定客户端。
        /// </summary>
        public ISqlSugarClient GetClient(string configId) => client;

        /// <summary>
        /// 获取全部连接配置标识。
        /// </summary>
        public IReadOnlyCollection<string> GetAllConfigIds() => [];

        /// <summary>
        /// 获取当前布局的全部连接配置标识。
        /// </summary>
        public IReadOnlyList<string> GetCurrentLayoutConfigIds() => [];

        /// <summary>
        /// 获取所有客户端。
        /// </summary>
        public IEnumerable<ISqlSugarClient> GetAllClients() => [client];

        /// <summary>
        /// 底层 SqlSugarScope。
        /// </summary>
        public ITenant AsTenant() => throw new NotSupportedException();
    }
}
