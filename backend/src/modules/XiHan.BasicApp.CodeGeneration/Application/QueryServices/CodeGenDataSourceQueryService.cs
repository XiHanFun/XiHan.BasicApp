#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenDataSourceQueryService
// Guid:c0de9e00-0801-4a00-9000-000000000801
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Application.Mappers;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.CodeGeneration.Application.QueryServices;

/// <summary>
/// 代码生成数据源查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "数据源")]
public sealed class CodeGenDataSourceQueryService : CodeGenerationApplicationService, ICodeGenDataSourceQueryService
{
    private readonly ICodeGenDataSourceRepository _dataSourceRepository;
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenDataSourceQueryService(
        ICodeGenDataSourceRepository dataSourceRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _dataSourceRepository = dataSourceRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<CodeGenDataSourceListItemDto>> GetPageAsync(CodeGenDataSourcePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        // 过滤：前端区间(Between)/多选(In)等条件经 conditions.filters 下发，FLS 门控剔除不可读/已脱敏字段后由框架统一应用
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysCodeGenDataSource", cancellationToken);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysCodeGenDataSource", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyDefaultSorts(request);
        }

        var page = await _dataSourceRepository.GetPagedAsync(request, cancellationToken);
        if (page.Items.Count == 0)
        {
            return new PageResultDtoBase<CodeGenDataSourceListItemDto>([], page.Page)
            {
                ExtendDatas = page.ExtendDatas
            };
        }

        var items = page.Items
            .Select(CodeGenDataSourceApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<CodeGenDataSourceListItemDto>(items, page.Page)
        {
            ExtendDatas = page.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<CodeGenDataSourceDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "数据源主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var dataSource = await _dataSourceRepository.GetByIdAsync(id, cancellationToken);
        return dataSource is null ? null : CodeGenDataSourceApplicationMapper.ToDetailDto(dataSource);
    }

    /// <summary>
    /// 构建数据源分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(CodeGenDataSourcePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysCodeGenDataSource>(
                input.Keyword.Trim(),
                source => source.SourceName,
                source => source.SourceDescription);
        }

        if (input.DatabaseType.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenDataSource source) => source.DatabaseType, input.DatabaseType.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenDataSource source) => source.Status, input.Status.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在 GetPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
        // 前端区间/多选等过滤条件原样带入（FLS 门控在调用方处理，框架统一应用）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }

    /// <summary>
    /// 应用数据源默认排序
    /// </summary>
    private static void ApplyDefaultSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysCodeGenDataSource source) => source.Sort, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysCodeGenDataSource source) => source.SourceName, SortDirection.Ascending, 1);
    }
}
