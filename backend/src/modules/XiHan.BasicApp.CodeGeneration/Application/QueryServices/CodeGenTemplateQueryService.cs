#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTemplateQueryService
// Guid:c0de9e00-0804-4a00-9000-000000000804
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Application.Mappers;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.CodeGeneration.Application.QueryServices;

/// <summary>
/// 代码生成模板查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "模板")]
public sealed class CodeGenTemplateQueryService : CodeGenerationApplicationService, ICodeGenTemplateQueryService
{
    private readonly ICodeGenTemplateRepository _templateRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTemplateQueryService(ICodeGenTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<PageResultDtoBase<CodeGenTemplateListItemDto>> GetPageAsync(CodeGenTemplatePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);
        var templatePage = await _templateRepository.GetPagedAsync(request, cancellationToken);
        if (templatePage.Items.Count == 0)
        {
            return new PageResultDtoBase<CodeGenTemplateListItemDto>([], templatePage.Page)
            {
                ExtendDatas = templatePage.ExtendDatas
            };
        }

        var items = templatePage.Items
            .Select(CodeGenTemplateApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<CodeGenTemplateListItemDto>(items, templatePage.Page)
        {
            ExtendDatas = templatePage.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<CodeGenTemplateDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "模板主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
        return template is null ? null : CodeGenTemplateApplicationMapper.ToDetailDto(template);
    }

    /// <summary>
    /// 构建模板分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(CodeGenTemplatePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysCodeGenTemplate>(
                input.Keyword.Trim(),
                template => template.TemplateCode,
                template => template.TemplateName);
        }

        if (!string.IsNullOrWhiteSpace(input.TemplateGroup))
        {
            request.Conditions.AddFilter((SysCodeGenTemplate template) => template.TemplateGroup, input.TemplateGroup.Trim());
        }

        if (input.TemplateType.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenTemplate template) => template.TemplateType, input.TemplateType.Value);
        }

        if (input.TemplateEngine.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenTemplate template) => template.TemplateEngine, input.TemplateEngine.Value);
        }

        if (input.IsBuiltIn.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenTemplate template) => template.IsBuiltIn, input.IsBuiltIn.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenTemplate template) => template.IsEnabled, input.IsEnabled.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenTemplate template) => template.Status, input.Status.Value);
        }

        request.Conditions.AddSort((SysCodeGenTemplate template) => template.TemplateGroup, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysCodeGenTemplate template) => template.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysCodeGenTemplate template) => template.TemplateCode, SortDirection.Ascending, 2);
        return request;
    }
}
