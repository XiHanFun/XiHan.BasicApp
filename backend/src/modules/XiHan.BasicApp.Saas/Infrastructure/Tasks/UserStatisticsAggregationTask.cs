// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Tasks;

/// <summary>
/// 用户统计聚合任务：把登录/访问/操作/接口调用日志与会话时长按周期聚合为 SysUserStatistics 快照
/// </summary>
/// <remarks>
/// <para>由动态任务调度（SysTask：TaskClass=本类全名，TaskMethod=ExecuteAsync，建议 Cron 每 10 分钟）触发。</para>
/// <para>口径：</para>
/// <list type="bullet">
///   <item>周期：今日 / 本周（周一起，月初几天会跨入上月） / 本月，均为 UTC 自然区间，StatisticsDate=今日；</item>
///   <item>登录数=区间内成功登录；访问/操作数=区间内对应日志行数；错误操作数=Result 非 Success；</item>
///   <item>接口调用数=区间内开放接口日志行数（按请求时间）；</item>
///   <item>在线时长=会话活跃区间（登录时间 → 登出/撤销/最后活动时间）与统计区间交集秒数合计，
///   最后活动时间由 SignalR 连接/断开心跳刷新；</item>
///   <item>按 (TenantId, UserId, StatisticsDate, Period) upsert，每租户另写 UserId=0 的全体汇总行。</item>
/// </list>
/// <para>日志与统计按上下文严格隔离，逐个作用域（平台与每个数据可达的租户）切入聚合，日志按查询窗口跨月分表扫描。</para>
/// </remarks>
public sealed class UserStatisticsAggregationTask
{
    private readonly ISqlSugarClientResolver _clientResolver;

    private readonly ITenantDataScopeRunner _scopeRunner;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<UserStatisticsAggregationTask> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserStatisticsAggregationTask(
        ISqlSugarClientResolver clientResolver,
        ITenantDataScopeRunner scopeRunner,
        TimeProvider timeProvider,
        ILogger<UserStatisticsAggregationTask> logger)
    {
        _clientResolver = clientResolver;
        _scopeRunner = scopeRunner;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// 执行聚合（动态任务反射入口）
    /// </summary>
    /// <returns>聚合结果摘要</returns>
    public async Task<string> ExecuteAsync()
    {
        var now = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime.Date);
        var todayStart = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var weekStart = todayStart.AddDays(-(((int)todayStart.DayOfWeek + 6) % 7));
        var monthStart = new DateTimeOffset(new DateTime(today.Year, today.Month, 1, 0, 0, 0), TimeSpan.Zero);
        var periods = new (StatisticsPeriod Period, DateTimeOffset Start)[]
        {
            (StatisticsPeriod.Today, todayStart),
            (StatisticsPeriod.ThisWeek, weekStart),
            (StatisticsPeriod.ThisMonth, monthStart)
        };

        // 查询窗口取各周期起点中最早者：月初几天本周从上月末的周一算起，早于本月起点
        var windowStart = periods.Min(item => item.Start);

        var inserted = 0;
        var updated = 0;
        var failures = new List<string>();

        // 日志与统计按上下文严格隔离，逐个作用域（平台与每个数据可达的租户）切入汇总
        await _scopeRunner.RunAsync(async tenantId =>
        {
            try
            {
                var (scopeInserted, scopeUpdated) = await AggregateScopeAsync(tenantId ?? 0, today, windowStart, periods, now);
                inserted += scopeInserted;
                updated += scopeUpdated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "汇总作用域 {TenantId} 的用户统计失败", tenantId?.ToString() ?? "平台");
                failures.Add(tenantId?.ToString() ?? "平台");
            }
        });

        var summary = failures.Count == 0
            ? $"用户统计聚合完成：新增 {inserted} 行，更新 {updated} 行（{today} Today/ThisWeek/ThisMonth）"
            : $"用户统计聚合部分失败：新增 {inserted} 行，更新 {updated} 行，失败作用域 {string.Join("，", failures)}";
        _logger.LogInformation("{Summary}", summary);
        return summary;
    }

    /// <summary>
    /// 汇总当前作用域（已切入）的用户统计
    /// </summary>
    private async Task<(int Inserted, int Updated)> AggregateScopeAsync(
        long scopeTenantId,
        DateOnly today,
        DateTimeOffset windowStart,
        (StatisticsPeriod Period, DateTimeOffset Start)[] periods,
        DateTimeOffset now)
    {
        var client = _clientResolver.GetCurrentClient();

        // 一次拉取查询窗口内的最小列原始数据（窗口可能跨月，按分表扫描），三个周期在内存里按各自起点切分
        var logins = await client.Queryable<SysLoginLog>()
            .Where(log => log.LoginTime >= windowStart && log.UserId != null && log.UserId > 0)
            .SplitTable()
            .Select(log => new SysLoginLog { TenantId = log.TenantId, UserId = log.UserId, LoginResult = log.LoginResult, LoginTime = log.LoginTime })
            .ToListAsync();

        var accesses = await client.Queryable<SysAccessLog>()
            .Where(log => log.AccessTime >= windowStart && log.UserId != null && log.UserId > 0)
            .SplitTable()
            .Select(log => new SysAccessLog { TenantId = log.TenantId, UserId = log.UserId, AccessTime = log.AccessTime })
            .ToListAsync();

        var operations = await client.Queryable<SysOperationLog>()
            .Where(log => log.OperationTime >= windowStart && log.UserId != null && log.UserId > 0)
            .SplitTable()
            .Select(log => new SysOperationLog { TenantId = log.TenantId, UserId = log.UserId, Result = log.Result, OperationTime = log.OperationTime })
            .ToListAsync();

        var apiCalls = await client.Queryable<SysOpenApiLog>()
            .Where(log => log.RequestTime >= windowStart && log.UserId != null && log.UserId > 0)
            .SplitTable()
            .Select(log => new SysOpenApiLog { TenantId = log.TenantId, UserId = log.UserId, RequestTime = log.RequestTime })
            .ToListAsync();

        // 会话：活跃区间与查询窗口有交集的（含跨窗口起点在线的活跃会话）。会话在平台库（严格隔离），按实体取连接，只取本作用域的行
        var sessions = await _clientResolver.GetClientForEntity<SysUserSession>().Queryable<SysUserSession>()
            .Where(session => session.TenantId == scopeTenantId && session.LastActivityTime >= windowStart && session.UserId > 0)
            .ToListAsync();

        // 读取今日全部快照一次，upsert 时按键匹配
        var existing = await client.Queryable<SysUserStatistics>()
            .Where(item => item.StatisticsDate == today)
            .ToListAsync();
        var existingMap = existing.ToDictionary(item => (item.TenantId, item.UserId, item.Period));

        var inserts = new List<SysUserStatistics>();
        var updates = new List<SysUserStatistics>();

        foreach (var (period, start) in periods)
        {
            var snapshots = BuildSnapshots(logins, accesses, operations, apiCalls, sessions, start, now);
            foreach (var snapshot in snapshots)
            {
                if (existingMap.TryGetValue((snapshot.TenantId, snapshot.UserId, period), out var row))
                {
                    ApplySnapshot(row, snapshot);
                    updates.Add(row);
                }
                else
                {
                    var entity = new SysUserStatistics
                    {
                        TenantId = snapshot.TenantId,
                        UserId = snapshot.UserId,
                        StatisticsDate = today,
                        Period = period,
                        Remark = "定时任务聚合"
                    };
                    ApplySnapshot(entity, snapshot);
                    inserts.Add(entity);
                }
            }
        }

        if (inserts.Count > 0)
        {
            _ = await client.Insertable(inserts).ExecuteCommandAsync();
        }

        if (updates.Count > 0)
        {
            _ = await client.Updateable(updates).ExecuteCommandAsync();
        }

        return (inserts.Count, updates.Count);
    }

    /// <summary>
    /// 周期内按 (TenantId, UserId) 聚合（含每租户 UserId=0 的全体汇总行）
    /// </summary>
    private static List<SnapshotData> BuildSnapshots(
        List<SysLoginLog> logins,
        List<SysAccessLog> accesses,
        List<SysOperationLog> operations,
        List<SysOpenApiLog> apiCalls,
        List<SysUserSession> sessions,
        DateTimeOffset start,
        DateTimeOffset now)
    {
        var map = new Dictionary<(long TenantId, long UserId), SnapshotData>();

        SnapshotData Get(long tenantId, long userId)
        {
            if (!map.TryGetValue((tenantId, userId), out var data))
            {
                data = new SnapshotData { TenantId = tenantId, UserId = userId };
                map[(tenantId, userId)] = data;
            }

            return data;
        }

        foreach (var login in logins.Where(item => item.LoginTime >= start))
        {
            var data = Get(login.TenantId, login.UserId!.Value);
            if (login.LoginResult == LoginResult.Success)
            {
                data.LoginCount++;
            }

            data.LastLoginTime = Max(data.LastLoginTime, login.LoginTime);
        }

        foreach (var access in accesses.Where(item => item.AccessTime >= start))
        {
            var data = Get(access.TenantId, access.UserId!.Value);
            data.AccessCount++;
            data.LastAccessTime = Max(data.LastAccessTime, access.AccessTime);
        }

        foreach (var operation in operations.Where(item => item.OperationTime >= start))
        {
            var data = Get(operation.TenantId, operation.UserId!.Value);
            data.OperationCount++;
            if (operation.Result != OperationExecuteResult.Success)
            {
                data.ErrorOperationCount++;
            }

            data.LastOperationTime = Max(data.LastOperationTime, operation.OperationTime);
        }

        foreach (var apiCall in apiCalls.Where(item => item.RequestTime >= start))
        {
            Get(apiCall.TenantId, apiCall.UserId!.Value).ApiCallCount++;
        }

        foreach (var session in sessions)
        {
            // 会话活跃区间：登录 → 登出/撤销/最后活动（最后活动由 SignalR 心跳刷新）
            var sessionEnd = session.LogoutTime ?? session.RevokedTime ?? session.LastActivityTime;
            var overlapStart = session.LoginTime > start ? session.LoginTime : start;
            var overlapEnd = sessionEnd < now ? sessionEnd : now;
            var seconds = (long)(overlapEnd - overlapStart).TotalSeconds;
            if (seconds > 0)
            {
                Get(session.TenantId, session.UserId).OnlineSeconds += seconds;
            }
        }

        // 每租户全体汇总行（UserId=0）
        foreach (var group in map.Values.GroupBy(item => item.TenantId).ToList())
        {
            var total = new SnapshotData { TenantId = group.Key, UserId = 0 };
            foreach (var item in group)
            {
                total.LoginCount += item.LoginCount;
                total.AccessCount += item.AccessCount;
                total.OperationCount += item.OperationCount;
                total.ErrorOperationCount += item.ErrorOperationCount;
                total.ApiCallCount += item.ApiCallCount;
                total.OnlineSeconds += item.OnlineSeconds;
                total.LastLoginTime = Max(total.LastLoginTime, item.LastLoginTime);
                total.LastAccessTime = Max(total.LastAccessTime, item.LastAccessTime);
                total.LastOperationTime = Max(total.LastOperationTime, item.LastOperationTime);
            }

            map[(group.Key, 0L)] = total;
        }

        return [.. map.Values];
    }

    private static void ApplySnapshot(SysUserStatistics entity, SnapshotData data)
    {
        entity.LoginCount = data.LoginCount;
        entity.AccessCount = data.AccessCount;
        entity.OperationCount = data.OperationCount;
        entity.ErrorOperationCount = data.ErrorOperationCount;
        entity.ApiCallCount = data.ApiCallCount;
        entity.OnlineTime = data.OnlineSeconds;
        entity.LastLoginTime = data.LastLoginTime;
        entity.LastAccessTime = data.LastAccessTime;
        entity.LastOperationTime = data.LastOperationTime;
    }

    private static DateTimeOffset? Max(DateTimeOffset? current, DateTimeOffset? candidate)
    {
        if (current is null) return candidate;
        if (candidate is null) return current;
        return candidate > current ? candidate : current;
    }

    private sealed class SnapshotData
    {
        public long TenantId { get; set; }

        public long UserId { get; set; }

        public int LoginCount { get; set; }

        public int AccessCount { get; set; }

        public int OperationCount { get; set; }

        public int ErrorOperationCount { get; set; }

        public int ApiCallCount { get; set; }

        public long OnlineSeconds { get; set; }

        public DateTimeOffset? LastLoginTime { get; set; }

        public DateTimeOffset? LastAccessTime { get; set; }

        public DateTimeOffset? LastOperationTime { get; set; }
    }
}
