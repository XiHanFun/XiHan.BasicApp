// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Tasks.BackgroundServices;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// 命中记录落库后台服务：每 5 秒取出已经结束的分钟里合并好的命中，按所属层分别写入按月分表；停机时连当前分钟一起写
/// </summary>
/// <remarks>
/// 继承 <see cref="XiHanBackgroundServiceBase{T}"/>、类名以 <c>HostedService</c> 结尾，由约定注册自动托管，切勿再手动 <c>AddHostedService</c>。
/// 某一层写入失败只记错误日志，这批命中不再重试。
/// </remarks>
public sealed class AccessPolicyHitHostedService : XiHanBackgroundServiceBase<AccessPolicyHitHostedService>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAccessPolicyHitRecorder _recorder;
    private readonly IIpRegionResolver _regions;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyHitHostedService(
        IServiceScopeFactory scopeFactory,
        IAccessPolicyHitRecorder recorder,
        IIpRegionResolver regions,
        TimeProvider timeProvider,
        IOptions<XiHanBackgroundServiceOptions> options,
        ILogger<AccessPolicyHitHostedService> logger)
        : base(logger, options, BuildConfig(options))
    {
        _scopeFactory = scopeFactory;
        _recorder = recorder;
        _regions = regions;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 停机前把内存里剩下的命中写掉
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await WriteAsync(_recorder.Drain(_timeProvider.GetUtcNow(), includeCurrentMinute: true), cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// 取出已经结束的分钟里合并好的命中
    /// </summary>
    protected override Task<IEnumerable<IBackgroundTaskItem>> FetchWorkItemsAsync(int maxCount, CancellationToken cancellationToken)
    {
        var items = _recorder.Drain(_timeProvider.GetUtcNow());
        IEnumerable<IBackgroundTaskItem> work = items.Count == 0 ? [] : [new HitBatch(items, _timeProvider.GetUtcNow())];
        return Task.FromResult(work);
    }

    /// <summary>
    /// 写入一批命中
    /// </summary>
    protected override Task ProcessItemAsync(IBackgroundTaskItem item, CancellationToken cancellationToken)
    {
        return WriteAsync(((HitBatch)item).Items, cancellationToken);
    }

    /// <summary>
    /// 单实例写入（限并发 1），每 5 秒检查一次
    /// </summary>
    private static IDynamicServiceConfig BuildConfig(IOptions<XiHanBackgroundServiceOptions> options)
    {
        var config = new DynamicServiceConfig(options);
        config.UpdateMaxConcurrentTasks(1);
        config.UpdateIdleDelay(5_000);
        return config;
    }

    private async Task WriteAsync(IReadOnlyList<AccessPolicyHitBatchItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        foreach (var group in items.GroupBy(item => item.First.TenantId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 切入命中所属的层再取连接，库隔离租户的命中进它自己的库
                using var tenantScope = currentTenant.Change(group.Key == 0 ? null : group.Key);
                var entities = group.Select(ToEntity).ToList();
                _ = await resolver.GetCurrentClient().Insertable(entities).SplitTable().ExecuteCommandAsync();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.LogError(exception, "写入层 {TenantId} 的 {Count} 条命中记录失败", group.Key, group.Count());
            }
        }
    }

    private SysAccessPolicyHit ToEntity(AccessPolicyHitBatchItem item)
    {
        var entry = item.First;
        var request = entry.Request;
        return new SysAccessPolicyHit
        {
            TenantId = entry.TenantId,
            Source = entry.Source,
            PolicyId = entry.PolicyId,
            PolicyCode = Trim(entry.PolicyCode, 200) ?? string.Empty,
            PolicyName = Trim(entry.PolicyName, 200),
            Action = entry.Action,
            Phase = entry.Phase,
            IsBlocked = entry.IsBlocked,
            TraceId = Trim(request.TraceId, 64),
            Ip = Trim(request.Ip, 50),
            Location = Trim(ResolveLocation(request.Ip), 200),
            UserId = request.UserId,
            UserName = Trim(request.UserName, 50),
            Method = Trim(request.Method, 10),
            Path = Trim(request.Path, 500),
            PermissionCodes = Trim(request.PermissionCodes, 500),
            UserAgent = Trim(request.UserAgent, 500),
            ClientKind = Trim(request.ClientKind, 20),
            HitTime = entry.Time,
            HitCount = item.Count
        };
    }

    private string? ResolveLocation(string? ip)
    {
        return ip is not null && IPAddress.TryParse(ip, out var address) ? _regions.Resolve(address)?.Location : null;
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    /// <summary>
    /// 一批待写入的命中
    /// </summary>
    private sealed class HitBatch(IReadOnlyList<AccessPolicyHitBatchItem> items, DateTimeOffset createdAt) : IBackgroundTaskItem
    {
        /// <summary>
        /// 任务唯一标识
        /// </summary>
        public string TaskId { get; } = $"access-policy-hits-{createdAt.ToUnixTimeMilliseconds()}";

        /// <summary>
        /// 任务数据
        /// </summary>
        [JsonIgnore]
        public object? Data => null;

        /// <summary>
        /// 任务创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; } = createdAt;

        /// <summary>
        /// 已重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 合并好的命中
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<AccessPolicyHitBatchItem> Items { get; } = items;
    }
}
