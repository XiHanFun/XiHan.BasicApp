#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTemplateContracts
// Guid:c0de9e00-0604-4a00-9000-000000000604
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
/// 代码生成模板命令应用服务接口
/// </summary>
public interface ICodeGenTemplateAppService : IApplicationService
{
    Task<CodeGenTemplateDetailDto> CreateAsync(CodeGenTemplateCreateDto input, CancellationToken cancellationToken = default);

    Task<CodeGenTemplateDetailDto> UpdateAsync(CodeGenTemplateUpdateDto input, CancellationToken cancellationToken = default);

    Task<CodeGenTemplateDetailDto> UpdateStatusAsync(CodeGenTemplateStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>校验模板语法（保存前预检）</summary>
    Task<CodeGenTemplateValidateResultDto> ValidateAsync(CodeGenTemplateValidateDto input, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代码生成模板查询应用服务接口
/// </summary>
public interface ICodeGenTemplateQueryService : IApplicationService
{
    Task<PageResultDtoBase<CodeGenTemplateListItemDto>> GetPageAsync(CodeGenTemplatePageQueryDto input, CancellationToken cancellationToken = default);

    Task<CodeGenTemplateDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}
