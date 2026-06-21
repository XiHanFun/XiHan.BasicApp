#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenDataSourceContracts
// Guid:c0de9e00-0601-4a00-9000-000000000601
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
/// 代码生成数据源命令应用服务接口
/// </summary>
public interface ICodeGenDataSourceAppService : IApplicationService
{
    Task<CodeGenDataSourceDetailDto> CreateAsync(CodeGenDataSourceCreateDto input, CancellationToken cancellationToken = default);

    Task<CodeGenDataSourceDetailDto> UpdateAsync(CodeGenDataSourceUpdateDto input, CancellationToken cancellationToken = default);

    Task<CodeGenDataSourceDetailDto> UpdateStatusAsync(CodeGenDataSourceStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>测试数据源连接（测试通过方可启用/保存）</summary>
    Task<CodeGenConnectionTestResultDto> TestConnectionAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代码生成数据源查询应用服务接口
/// </summary>
public interface ICodeGenDataSourceQueryService : IApplicationService
{
    Task<PageResultDtoBase<CodeGenDataSourceListItemDto>> GetPageAsync(CodeGenDataSourcePageQueryDto input, CancellationToken cancellationToken = default);

    Task<CodeGenDataSourceDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}
