// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.CodeGeneration.Application.Contracts;

/// <summary>
/// 代码生成列配置命令应用服务接口
/// </summary>
public interface ICodeGenTableColumnAppService : IApplicationService
{
    /// <summary>
    /// 按表整体保存列配置（前端表格批量提交）
    /// </summary>
    Task BatchSaveAsync(CodeGenTableColumnBatchSaveDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新单列配置
    /// </summary>
    Task<CodeGenTableColumnListItemDto> UpdateAsync(CodeGenTableColumnUpdateDto input, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代码生成列配置查询应用服务接口
/// </summary>
public interface ICodeGenTableColumnQueryService : IApplicationService
{
    /// <summary>
    /// 获取指定表的全部列配置
    /// </summary>
    Task<IReadOnlyList<CodeGenTableColumnListItemDto>> GetByTableAsync(long tableId, CancellationToken cancellationToken = default);
}
