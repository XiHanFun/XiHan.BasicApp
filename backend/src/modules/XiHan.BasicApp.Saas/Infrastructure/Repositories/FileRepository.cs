#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileRepository
// Guid:75a7ae9f-1511-4f31-b95c-93cfeca79892
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:16:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 文件仓储实现
/// </summary>
public class FileRepository : SqlSugarAggregateRepository<SysFile, long>, IFileRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FileRepository(
        ISqlSugarClientResolver clientResolver,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientResolver, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    public async Task<SysFile?> GetByFileHashAsync(string fileHash, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        var resolvedTenantId = tenantId;

        var query = CreateQueryable()
            .Where(file => file.FileHash == fileHash);

        query = resolvedTenantId.HasValue
            ? query.Where(file => file.TenantId == resolvedTenantId.Value)
            : query.Where(file => file.TenantId == 0);

        return await query.FirstAsync(cancellationToken);
    }
}
