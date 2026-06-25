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
    /// <summary>创建模板</summary>
    Task<CodeGenTemplateDetailDto> CreateAsync(CodeGenTemplateCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>更新模板</summary>
    Task<CodeGenTemplateDetailDto> UpdateAsync(CodeGenTemplateUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>更新模板状态</summary>
    Task<CodeGenTemplateDetailDto> UpdateStatusAsync(CodeGenTemplateStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>删除模板</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>校验模板语法（保存前预检）</summary>
    Task<CodeGenTemplateValidateResultDto> ValidateAsync(CodeGenTemplateValidateDto input, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代码生成模板查询应用服务接口
/// </summary>
public interface ICodeGenTemplateQueryService : IApplicationService
{
    /// <summary>获取模板分页列表</summary>
    Task<PageResultDtoBase<CodeGenTemplateListItemDto>> GetPageAsync(CodeGenTemplatePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>获取模板详情</summary>
    Task<CodeGenTemplateDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}
