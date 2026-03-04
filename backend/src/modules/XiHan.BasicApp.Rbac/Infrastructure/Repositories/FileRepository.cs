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

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 文件仓储实现
/// </summary>
public class FileRepository : SqlSugarAggregateRepository<SysFile, long>, IFileRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FileRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    public async Task<SysFile?> GetByFileHashAsync(string fileHash, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        var resolvedTenantId = tenantId;

        var query = CreateTenantQueryable()
            .Where(file => file.FileHash == fileHash);

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(file => file.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(file => file.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }
}
