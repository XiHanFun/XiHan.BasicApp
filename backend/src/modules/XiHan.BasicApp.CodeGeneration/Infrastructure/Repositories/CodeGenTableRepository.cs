#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableRepository
// Guid:c0de9e00-0202-4a00-9000-000000000202
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Repositories;

/// <summary>
/// 代码生成表配置仓储实现
/// </summary>
public sealed class CodeGenTableRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysCodeGenTable>(clientResolver), ICodeGenTableRepository
{
    /// <inheritdoc />
    public async Task<SysCodeGenTable?> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(table => table.TableName == tableName.Trim())
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsTableNameAsync(string tableName, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(table => table.TableName == tableName.Trim());
        if (excludeId.HasValue)
        {
            query = query.Where(table => table.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysCodeGenTable>> GetByModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(table => table.ModuleName == moduleName.Trim())
            .OrderBy(table => table.TableName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysCodeGenTable>> GetByMasterTableIdAsync(long masterTableId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(table => table.MasterTableId == masterTableId)
            .OrderBy(table => table.TableName)
            .ToListAsync(cancellationToken);
    }
}
