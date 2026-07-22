// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Repositories;

/// <summary>
/// 代码生成数据源仓储实现
/// </summary>
public sealed class CodeGenDataSourceRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysCodeGenDataSource>(clientResolver), ICodeGenDataSourceRepository
{
    /// <inheritdoc />
    public async Task<SysCodeGenDataSource?> GetByNameAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(source => source.SourceName == sourceName.Trim())
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsNameAsync(string sourceName, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(source => source.SourceName == sourceName.Trim());
        if (excludeId.HasValue)
        {
            query = query.Where(source => source.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysCodeGenDataSource?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(source => source.IsDefault)
            .OrderBy(source => source.Sort)
            .FirstAsync(cancellationToken);
    }
}
