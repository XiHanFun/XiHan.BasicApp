// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 文件仓储实现
/// </summary>
public sealed class FileRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysFile>(clientResolver), IFileRepository
{
    /// <summary>
    /// 根据文件哈希获取
    /// </summary>
    public async Task<SysFile?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(file => file.FileHash == fileHash)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 统计当前作用域已占用的存储字节数
    /// </summary>
    public async Task<long> SumUsedStorageAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(file => file.Status == FileStatus.Normal || file.Status == FileStatus.Uploading)
            .SumAsync(file => file.FileSize);
    }

    /// <summary>
    /// 按租户分组统计当前库里这些租户已占用的存储字节数
    /// </summary>
    public async Task<IReadOnlyDictionary<long, long>> SumUsedStorageByTenantIdsAsync(IReadOnlyCollection<long> tenantIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantIds);
        cancellationToken.ThrowIfCancellationRequested();

        if (tenantIds.Count == 0)
        {
            return new Dictionary<long, long>();
        }

        var ids = tenantIds.Distinct().ToList();

        // 跨租户分组：清租户过滤后按 TenantId 精确匹配。软删过滤器保持生效，回收站文件不占配额。
        var rows = await CreateNoTenantQueryable()
            .Where(file => ids.Contains(file.TenantId))
            .Where(file => file.Status == FileStatus.Normal || file.Status == FileStatus.Uploading)
            .GroupBy(file => file.TenantId)
            .Select(file => new TenantUsageRow { TenantId = file.TenantId, Value = SqlFunc.AggregateSum(file.FileSize) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.TenantId, row => row.Value);
    }
}
