// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.Repositories;

/// <summary>
/// 代码生成列配置仓储接口
/// </summary>
public interface ICodeGenTableColumnRepository : ISaasRepository<SysCodeGenTableColumn>
{
    /// <summary>
    /// 获取指定表的全部列配置（按 Sort 升序）
    /// </summary>
    Task<IReadOnlyList<SysCodeGenTableColumn>> GetByTableIdAsync(long tableId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定表的全部列配置（软删）
    /// </summary>
    Task DeleteByTableIdAsync(long tableId, CancellationToken cancellationToken = default);
}
