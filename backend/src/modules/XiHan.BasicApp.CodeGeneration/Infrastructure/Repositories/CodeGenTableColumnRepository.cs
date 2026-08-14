// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Repositories;

/// <summary>
/// 代码生成列配置仓储实现
/// </summary>
public sealed class CodeGenTableColumnRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysCodeGenTableColumn>(clientResolver), ICodeGenTableColumnRepository
{
    /// <summary>
    /// 获取指定表的全部列配置（按 Sort 升序）
    /// </summary>
    public async Task<IReadOnlyList<SysCodeGenTableColumn>> GetByTableIdAsync(long tableId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(column => column.TableId == tableId)
            .OrderBy(column => column.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 删除指定表的全部列配置（软删）
    /// </summary>
    public async Task DeleteByTableIdAsync(long tableId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 软删由实体 ISoftDelete + 仓储 DeleteAsync 自动处理
        await DeleteAsync(column => column.TableId == tableId, cancellationToken);
    }
}
