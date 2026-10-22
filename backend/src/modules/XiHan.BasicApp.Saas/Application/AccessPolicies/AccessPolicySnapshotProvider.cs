// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 访问策略快照提供器：进程内一份快照，按分布式缓存里的版本号失效
/// </summary>
/// <remarks>
/// 每个节点最多每 <see cref="AccessPolicyOptions.VersionCheckSeconds"/> 秒读一次版本号；
/// 读版本号失败或重建失败时沿用旧快照并记日志，还没有快照时把异常抛给调用方。
/// </remarks>
public sealed class AccessPolicySnapshotProvider : IAccessPolicySnapshotProvider, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IOptionsMonitor<AccessPolicyOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccessPolicySnapshotProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AccessPolicySnapshot? _snapshot;
    private long _nextCheckTicks;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicySnapshotProvider(
        IServiceScopeFactory scopeFactory,
        IDistributedCache cache,
        IUnitOfWorkManager unitOfWorkManager,
        IOptionsMonitor<AccessPolicyOptions> options,
        TimeProvider timeProvider,
        ILogger<AccessPolicySnapshotProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _unitOfWorkManager = unitOfWorkManager;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<AccessPolicySnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = Volatile.Read(ref _snapshot);
        if (snapshot is not null && _timeProvider.GetUtcNow() < ReadNextCheck())
        {
            return snapshot;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            snapshot = _snapshot;
            var now = _timeProvider.GetUtcNow();
            if (snapshot is not null && now < ReadNextCheck())
            {
                return snapshot;
            }

            string? version;
            try
            {
                version = await _cache.GetStringAsync(SaasCacheNames.AccessPolicyVersion, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException && snapshot is not null)
            {
                _logger.LogWarning(exception, "读取访问策略版本号失败，沿用当前快照");
                SetNextCheck(now);
                return snapshot;
            }

            if (snapshot is null || !string.Equals(snapshot.Version, version, StringComparison.Ordinal))
            {
                try
                {
                    snapshot = await BuildAsync(version, cancellationToken);
                    Volatile.Write(ref _snapshot, snapshot);
                }
                catch (Exception exception) when (exception is not OperationCanceledException && snapshot is not null)
                {
                    _logger.LogError(exception, "重建访问策略快照失败，沿用旧快照");
                }
            }

            SetNextCheck(now);
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        var unitOfWork = _unitOfWorkManager.Current;
        if (unitOfWork is null)
        {
            return PublishVersionAsync();
        }

        unitOfWork.OnCompleted(PublishVersionAsync);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _gate.Dispose();
    }

    private async Task PublishVersionAsync()
    {
        await _cache.SetStringAsync(SaasCacheNames.AccessPolicyVersion, Guid.NewGuid().ToString("N"));
        Interlocked.Exchange(ref _nextCheckTicks, 0);
    }

    private DateTimeOffset ReadNextCheck()
    {
        return new DateTimeOffset(Interlocked.Read(ref _nextCheckTicks), TimeSpan.Zero);
    }

    private void SetNextCheck(DateTimeOffset now)
    {
        var seconds = Math.Max(1, _options.CurrentValue.VersionCheckSeconds);
        Interlocked.Exchange(ref _nextCheckTicks, now.AddSeconds(seconds).UtcTicks);
    }

    private async Task<AccessPolicySnapshot> BuildAsync(string? version, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        using var platformScope = services.GetRequiredService<ICurrentTenant>().Change(null);
        var policies = await services.GetRequiredService<IAccessPolicyRepository>().GetEnabledInAllTenantsAsync(cancellationToken);
        var conditions = await services.GetRequiredService<IAccessPolicyConditionRepository>()
            .GetByPolicyIdsInAllTenantsAsync([.. policies.Select(policy => policy.BasicId)], cancellationToken);
        var compiler = services.GetRequiredService<AccessPolicyConditionCompiler>();
        var conditionsByPolicy = conditions.ToLookup(condition => condition.PolicyId);
        var compiled = new List<CompiledAccessPolicy>(policies.Count);
        foreach (var policy in policies)
        {
            var set = compiler.Compile(conditionsByPolicy[policy.BasicId]);
            if (set.IsEmpty)
            {
                _logger.LogWarning("访问策略 {PolicyCode}（租户 {TenantId}）没有条件，不参与求值", policy.PolicyCode, policy.TenantId);
                continue;
            }

            if (set.Errors.Count > 0)
            {
                _logger.LogWarning(
                    "访问策略 {PolicyCode}（租户 {TenantId}）有 {Count} 条条件无法求值，按不命中处理：{Error}",
                    policy.PolicyCode,
                    policy.TenantId,
                    set.Errors.Count,
                    set.Errors[0].Message);
            }

            compiled.Add(new CompiledAccessPolicy(
                policy.BasicId,
                policy.TenantId,
                policy.PolicyCode,
                policy.PolicyName,
                policy.Action,
                policy.Priority,
                policy.EffectiveTime,
                policy.ExpirationTime,
                set));
        }

        return new AccessPolicySnapshot(compiled, version);
    }
}
