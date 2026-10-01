// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
using XiHan.BasicApp.Saas.Domain.Repositories;
using MenuType = XiHan.BasicApp.Saas.Domain.Entities.MenuType;
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

    private readonly IMenuRepository _menuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTableQueryService(
        ICodeGenTableRepository tableRepository,
        ICodeGenTableColumnRepository columnRepository,
        IFieldSecurityService fieldSecurityService,
        IMenuRepository menuRepository)
    {
        _tableRepository = tableRepository;
        _columnRepository = columnRepository;
        _fieldSecurity = fieldSecurityService;
        _menuRepository = menuRepository;
    }

    /// <summary>
    /// 获取表配置全量列表
    /// </summary>
    /// <remarks>供主子表选择等下拉一次取全，不走分页。</remarks>
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<IReadOnlyList<CodeGenTableListItemDto>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tables = await _tableRepository.GetListAsync(table => true, cancellationToken);

        return [.. tables
            .OrderBy(table => table.ModuleName, StringComparer.Ordinal)
            .ThenBy(table => table.TableName, StringComparer.Ordinal)
            .Select(CodeGenTableApplicationMapper.ToListItemDto)];
    }

    /// <summary>
    /// 获取表配置分页列表
    /// </summary>
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<CodeGenTableListItemDto>> GetPageAsync(CodeGenTablePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        // 字段安全：剔除读受保护字段上的排序、过滤与关键字搜索（防按结果反推原值）
        await _fieldSecurity.GuardQueryAsync(request.Conditions, typeof(SysCodeGenTable), cancellationToken);

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

    /// <summary>
    /// 获取表配置详情
    /// </summary>
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
    /// 获取父菜单候选
    /// </summary>
    /// <remarks>
    /// 返回平台菜单树（目录与菜单，不含按钮），和菜单管理页的上级菜单同一棵树，带上级主键供前端组树。
    /// 只有目录可选：页面挂在菜单下会被当成父路由；目录还须有菜单码，生成的菜单登记按菜单码找父级。
    /// 与代码生成同一个查看权限，不要求菜单管理权限。
    /// </remarks>
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<IReadOnlyList<CodeGenParentMenuOptionDto>> GetParentMenuOptionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var menus = await _menuRepository.GetListAsync(
            menu => menu.TenantId == 0 && menu.MenuType != MenuType.Button,
            cancellationToken);

        return [.. menus
            .OrderBy(menu => menu.Sort)
            .ThenBy(menu => menu.MenuName, StringComparer.Ordinal)
            .Select(menu => new CodeGenParentMenuOptionDto
            {
                Value = menu.BasicId,
                Label = menu.MenuName,
                ParentValue = menu.ParentId,
                Selectable = menu.MenuType == MenuType.Directory && !string.IsNullOrWhiteSpace(menu.MenuCode)
            })];
    }

    /// <summary>
    /// 构建表配置分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(CodeGenTablePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
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
