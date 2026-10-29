// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 存储用量：各租户未删除文件的大小之和（共用同一对象的文件各自计入，归档与损坏的也占空间）。按调用方的连接读，平台的调用读默认库里字段隔离的租户。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
public sealed class FileStorageUsage(SqlSugarUnitOfWork unitOfWork) : IStorageUsage
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, long>> UsedBytesAsync(IReadOnlyCollection<string> tenantIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenantIds);
        if (tenantIds.Count == 0)
        {
            return new Dictionary<string, long>();
        }

        var ids = tenantIds.ToList();
        var rows = await unitOfWork.ConnectionFor(typeof(SysFile)).Queryable<SysFile>()
            .Where(f => ids.Contains(f.TenantId) && !f.IsDeleted)
            .GroupBy(f => f.TenantId)
            .Select(f => new { Tenant = f.TenantId, Bytes = SqlFunc.AggregateSum(f.Size) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.ToDictionary(static r => r.Tenant, static r => r.Bytes, StringComparer.Ordinal);
    }
}
