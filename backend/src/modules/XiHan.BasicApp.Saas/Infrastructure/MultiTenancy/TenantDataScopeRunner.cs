// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.MultiTenancy;

/// <summary>
/// 逐数据作用域执行：平台自身，以及每个数据可达的租户
/// </summary>
/// <remarks>
/// 跨租户的后台作业（日志保留清理、统计汇总等）不靠「无租户上下文看全部」取巧，而是逐个切入作用域执行：
/// 读过滤、写入落戳、连接解析都按该作用域走——严格隔离的数据只在自己的作用域里可见，库隔离租户的数据只在它自己的库里。
/// </remarks>
public interface ITenantDataScopeRunner
{
    /// <summary>
    /// 依次在平台与每个数据可达的租户内执行
    /// </summary>
    /// <param name="action">作用域内的操作，参数为作用域的租户标识（平台为 null）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RunAsync(Func<long?, Task> action, CancellationToken cancellationToken = default);
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
