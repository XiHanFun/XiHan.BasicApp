// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.Repositories;

/// <summary>
/// 代码生成历史仓储接口
/// </summary>
public interface ICodeGenHistoryRepository : ISaasRepository<SysCodeGenHistory>
{
    /// <summary>
    /// 按表配置获取生成历史（按生成时间倒序）
    /// </summary>
    Task<IReadOnlyList<SysCodeGenHistory>> GetByTableIdAsync(long tableId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按批次号获取
    /// </summary>
    Task<SysCodeGenHistory?> GetByBatchNumberAsync(string batchNumber, CancellationToken cancellationToken = default);
}
