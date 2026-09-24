// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 逐数据作用域执行：平台自身，以及每个数据可达的租户
/// </summary>
/// <remarks>
/// 跨租户的后台作业（日志保留清理、统计汇总等）不靠「无租户上下文看全部」取巧，而是逐个切入作用域执行：
/// 读过滤、写入落戳、连接解析都按该作用域走——严格隔离的数据只在自己的作用域里可见，库隔离租户的数据只在它自己的库里。
/// <para>
/// 平台要跨租户扫描租户库里的数据（到期书签、存储配置的引用等）时按库走：字段隔离租户的数据与平台同在平台库，
/// 在平台库里清掉租户过滤查一次就覆盖了它们；库隔离租户的数据各在自己的库里，逐个切入查。
/// </para>
/// </remarks>
public interface ITenantDataScopeRunner
{
    /// <summary>
    /// 依次在平台与每个数据可达的租户内执行
    /// </summary>
    /// <param name="action">作用域内的操作，参数为作用域的租户标识（平台为 null）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RunAsync(Func<long?, Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// 依次在每个库上执行：先平台库（平台作用域），再每个已建库的库隔离租户（切入该租户，连接解析到它自己的库）
    /// </summary>
    /// <remarks>
    /// 操作里清掉租户过滤查一次，就覆盖了这个库里的全部租户。
    /// </remarks>
    /// <param name="action">库上的操作，参数为切入的租户标识（平台库为 null）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RunPerDatabaseAsync(Func<long?, Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// 逐库探查（顺序同 <see cref="RunPerDatabaseAsync"/>），任一库命中即停
    /// </summary>
    /// <param name="probe">库上的探查，清掉租户过滤查这个库里的全部租户</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任一库命中返回 true</returns>
    Task<bool> AnyPerDatabaseAsync(Func<Task<bool>> probe, CancellationToken cancellationToken = default);
}

/// <summary>
/// 逐数据作用域执行器
/// </summary>
/// <remarks>
/// 数据可达的租户：未删除，且字段隔离，或独立库已配置完成（未建库的独立库租户没有可连的库）。
/// </remarks>
public sealed class TenantDataScopeRunner : ITenantDataScopeRunner
{
    private readonly ITenantRepository _tenantRepository;

    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantDataScopeRunner(ITenantRepository tenantRepository, ICurrentTenant currentTenant)
    {
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task RunAsync(Func<long?, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        IReadOnlyList<long> tenantIds;
        using (_currentTenant.Change(null))
        {
            // 租户目录是平台数据，在平台作用域读
            tenantIds = [.. (await _tenantRepository.GetListAsync(
                    tenant => tenant.IsolationMode == TenantIsolationMode.Field || tenant.ConfigStatus == TenantConfigStatus.Configured,
                    cancellationToken))
                .Select(tenant => tenant.BasicId)];

            await action(null);
        }

        await RunInTenantsAsync(tenantIds, action, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RunPerDatabaseAsync(Func<long?, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        IReadOnlyList<long> tenantIds;
        using (_currentTenant.Change(null))
        {
            tenantIds = await GetDatabaseTenantIdsAsync(cancellationToken);
            await action(null);
        }

        await RunInTenantsAsync(tenantIds, action, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyPerDatabaseAsync(Func<Task<bool>> probe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(probe);

        IReadOnlyList<long> tenantIds;
        using (_currentTenant.Change(null))
        {
            if (await probe())
            {
                return true;
            }

            tenantIds = await GetDatabaseTenantIdsAsync(cancellationToken);
        }

        foreach (var tenantId in tenantIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (_currentTenant.Change(tenantId))
            {
                if (await probe())
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 已建库的库隔离租户（平台作用域内调用）
    /// </summary>
    private async Task<IReadOnlyList<long>> GetDatabaseTenantIdsAsync(CancellationToken cancellationToken)
    {
        return [.. (await _tenantRepository.GetListAsync(
                tenant => tenant.IsolationMode == TenantIsolationMode.Database && tenant.ConfigStatus == TenantConfigStatus.Configured,
                cancellationToken))
            .Select(tenant => tenant.BasicId)];
    }

    private async Task RunInTenantsAsync(IReadOnlyList<long> tenantIds, Func<long?, Task> action, CancellationToken cancellationToken)
    {
        foreach (var tenantId in tenantIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (_currentTenant.Change(tenantId))
            {
                await action(tenantId);
            }
        }
    }
}
