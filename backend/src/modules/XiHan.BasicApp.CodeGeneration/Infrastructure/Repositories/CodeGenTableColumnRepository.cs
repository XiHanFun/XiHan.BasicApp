#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableColumnRepository
// Guid:c0de9e00-0203-4a00-9000-000000000203
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
/// 代码生成列配置仓储实现
/// </summary>
public sealed class CodeGenTableColumnRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysCodeGenTableColumn>(clientResolver), ICodeGenTableColumnRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysCodeGenTableColumn>> GetByTableIdAsync(long tableId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(column => column.TableId == tableId)
            .OrderBy(column => column.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteByTableIdAsync(long tableId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 软删由实体 ISoftDelete + 仓储 DeleteAsync 自动处理
        await DeleteAsync(column => column.TableId == tableId, cancellationToken);
    }
}
