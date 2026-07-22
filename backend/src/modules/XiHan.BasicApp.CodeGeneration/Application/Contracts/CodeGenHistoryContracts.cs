// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.CodeGeneration.Application.Contracts;

/// <summary>
/// 代码生成历史查询应用服务接口（历史为系统写入，只读对外）
/// </summary>
public interface ICodeGenHistoryQueryService : IApplicationService
{
    /// <summary>
    /// 获取代码生成历史分页列表
    /// </summary>
    Task<PageResultDtoBase<CodeGenHistoryListItemDto>> GetPageAsync(CodeGenHistoryPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取代码生成历史详情
    /// </summary>
    Task<CodeGenHistoryDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按表获取代码生成历史列表
    /// </summary>
    Task<IReadOnlyList<CodeGenHistoryListItemDto>> GetByTableAsync(long tableId, CancellationToken cancellationToken = default);
}
