#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableQueryService
// Guid:c0de9e00-0802-4a00-9000-000000000802
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
/// 代码生成表配置查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "表配置")]
public sealed class CodeGenTableQueryService : CodeGenerationApplicationService, ICodeGenTableQueryService
{
    private readonly ICodeGenTableRepository _tableRepository;

    private readonly ICodeGenTableColumnRepository _columnRepository;

    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTableQueryService(
        ICodeGenTableRepository tableRepository,
        ICodeGenTableColumnRepository columnRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _tableRepository = tableRepository;
        _columnRepository = columnRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<CodeGenTableListItemDto>> GetPageAsync(CodeGenTablePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        // 过滤：前端区间(Between)/多选(In)等条件经 conditions.filters 下发，FLS 门控剔除不可读/已脱敏字段后由框架统一应用
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysCodeGenTable", cancellationToken);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysCodeGenTable", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyTableSorts(request);
        }

        var tablePage = await _tableRepository.GetPagedAsync(request, cancellationToken);
        if (tablePage.Items.Count == 0)
        {
            return new PageResultDtoBase<CodeGenTableListItemDto>([], tablePage.Page)
            {
                ExtendDatas = tablePage.ExtendDatas
            };
        }

        var items = tablePage.Items
            .Select(CodeGenTableApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<CodeGenTableListItemDto>(items, tablePage.Page)
        {
            ExtendDatas = tablePage.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<CodeGenTableDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "表配置主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var table = await _tableRepository.GetByIdAsync(id, cancellationToken);
        if (table is null)
        {
            return null;
        }

        var columns = await _columnRepository.GetByTableIdAsync(table.BasicId, cancellationToken);
        return CodeGenTableApplicationMapper.ToDetailDto(table, columns);
    }

    /// <summary>
    /// 构建表配置分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(CodeGenTablePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysCodeGenTable>(
                input.Keyword.Trim(),
                table => table.TableName,
                table => table.ClassName,
                table => table.TableComment);
        }

        if (!string.IsNullOrWhiteSpace(input.ModuleName))
        {
            request.Conditions.AddFilter((SysCodeGenTable table) => table.ModuleName, input.ModuleName.Trim());
        }

        if (input.TemplateType.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenTable table) => table.TemplateType, input.TemplateType.Value);
        }

        if (input.GenStatus.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenTable table) => table.GenStatus, input.GenStatus.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenTable table) => table.Status, input.Status.Value);
        }

        // 前端区间/多选等过滤条件原样带入（FLS 门控在调用方处理，框架统一应用）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
        return request;
    }

    /// <summary>
    /// 应用表配置默认排序（无前端排序时的兜底）
    /// </summary>
    private static void ApplyTableSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysCodeGenTable table) => table.ModuleName, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysCodeGenTable table) => table.TableName, SortDirection.Ascending, 1);
    }
}
