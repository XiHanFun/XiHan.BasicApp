// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成列配置领域服务接口
/// </summary>
public interface ICodeGenTableColumnDomainService
{
    /// <summary>
    /// 按表批量保存列配置（覆盖各列的可变字段）
    /// </summary>
    Task<CodeGenTableColumnBatchSaveResult> BatchSaveColumnsAsync(CodeGenTableColumnBatchSaveCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 单列更新列配置
    /// </summary>
    Task<CodeGenTableColumnCommandResult> UpdateColumnAsync(CodeGenTableColumnUpdateCommand command, CancellationToken cancellationToken = default);
}
