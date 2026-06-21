#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenHistoryContracts
// Guid:c0de9e00-0605-4a00-9000-000000000605
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.CodeGeneration.Application.Contracts;

/// <summary>
/// 代码生成历史查询应用服务接口（历史为系统写入，只读对外）
/// </summary>
public interface ICodeGenHistoryQueryService : IApplicationService
{
    Task<PageResultDtoBase<CodeGenHistoryListItemDto>> GetPageAsync(CodeGenHistoryPageQueryDto input, CancellationToken cancellationToken = default);

    Task<CodeGenHistoryDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CodeGenHistoryListItemDto>> GetByTableAsync(long tableId, CancellationToken cancellationToken = default);
}
