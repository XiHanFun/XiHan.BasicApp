#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictQueryService
// Guid:51df8c8f-e272-4c0a-a547-8bd869bc04b8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统字典查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统字典")]
public sealed class DictQueryService(
    IDictRepository dictRepository,
    IDictItemRepository dictItemRepository)
    : SaasApplicationService, IDictQueryService
{
    /// <summary>
    /// 系统字典仓储
    /// </summary>
    private readonly IDictRepository _dictRepository = dictRepository;

    /// <summary>
    /// 系统字典项仓储
    /// </summary>
    private readonly IDictItemRepository _dictItemRepository = dictItemRepository;

    /// <summary>
    /// 获取系统字典分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Dict.Read)]
    public async Task<PageResultDtoBase<DictListItemDto>> GetDictPageAsync(DictPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildDictPageRequest(input);
        var dicts = await _dictRepository.GetPagedAsync(request, cancellationToken);
        return dicts.Map(DictApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取系统字典详情
    /// </summary>
    /// <param name="id">系统字典主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Dict.Read)]
    public async Task<DictDetailDto?> GetDictDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统字典主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var dict = await _dictRepository.GetByIdAsync(id, cancellationToken);
        return dict is null ? null : DictApplicationMapper.ToDetailDto(dict);
    }

    /// <summary>
    /// 获取系统字典项分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典项分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Dict.Read)]
    public async Task<PageResultDtoBase<DictItemListItemDto>> GetDictItemPageAsync(DictItemPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildDictItemPageRequest(input);
        var dictItems = await _dictItemRepository.GetPagedAsync(request, cancellationToken);
        return dictItems.Map(DictApplicationMapper.ToItemListItemDto);
    }

    /// <summary>
    /// 获取系统字典项详情
    /// </summary>
    /// <param name="id">系统字典项主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典项详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Dict.Read)]
    public async Task<DictItemDetailDto?> GetDictItemDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统字典项主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var dictItem = await _dictItemRepository.GetByIdAsync(id, cancellationToken);
        return dictItem is null ? null : DictApplicationMapper.ToItemDetailDto(dictItem);
    }

    /// <summary>
    /// 获取系统字典项树
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典项树</returns>
    [PermissionAuthorize(SaasPermissionCodes.Dict.Read)]
    public async Task<IReadOnlyList<DictItemTreeNodeDto>> GetDictItemTreeAsync(DictItemTreeQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.DictId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "字典主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildDictItemTreeRequest(input);
        var dictItems = await _dictItemRepository.GetPagedAsync(request, cancellationToken);
        if (dictItems.Items.Count == 0)
        {
            return [];
        }

        var nodes = dictItems.Items
            .Select(DictApplicationMapper.ToItemTreeNodeDto)
            .ToList();
        return BuildItemTree(nodes);
    }

    /// <summary>
    /// 构建系统字典分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统字典分页请求</returns>
    private static BasicAppPRDto BuildDictPageRequest(DictPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysDict.DictCode),
                nameof(SysDict.DictName),
                nameof(SysDict.DictType),
                nameof(SysDict.DictDescription));
        }

        if (!string.IsNullOrWhiteSpace(input.DictCode))
        {
            request.Conditions.AddFilter(nameof(SysDict.DictCode), input.DictCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.DictType))
        {
            request.Conditions.AddFilter(nameof(SysDict.DictType), input.DictType.Trim());
        }

        if (input.IsBuiltIn.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysDict.IsBuiltIn), input.IsBuiltIn.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysDict.Status), input.Status.Value);
        }

        ApplyDictSorts(request);
        return request;
    }

    /// <summary>
    /// 构建系统字典项分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统字典项分页请求</returns>
    private static BasicAppPRDto BuildDictItemPageRequest(DictItemPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        ApplyCommonDictItemFilters(
            request,
            input.Keyword,
            input.DictId,
            input.ParentId,
            input.ItemCode,
            input.IsDefault,
            input.Status);
        ApplyDictItemSorts(request);
        return request;
    }

    /// <summary>
    /// 构建系统字典项树请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统字典项树请求</returns>
    private static BasicAppPRDto BuildDictItemTreeRequest(DictItemTreeQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 5000);
        ApplyCommonDictItemFilters(
            request,
            keyword: null,
            dictId: input.DictId,
            parentId: null,
            itemCode: null,
            isDefault: null,
            status: input.OnlyEnabled ? EnableStatus.Enabled : null);
        ApplyDictItemSorts(request);
        return request;
    }

    /// <summary>
    /// 应用系统字典项通用筛选条件
    /// </summary>
    private static void ApplyCommonDictItemFilters(
        BasicAppPRDto request,
        string? keyword,
        long? dictId,
        long? parentId,
        string? itemCode,
        bool? isDefault,
        EnableStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword(
                keyword.Trim(),
                nameof(SysDictItem.ItemCode),
                nameof(SysDictItem.ItemName),
                nameof(SysDictItem.ItemValue),
                nameof(SysDictItem.ItemDescription));
        }

        if (dictId.HasValue && dictId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysDictItem.DictId), dictId.Value);
        }

        if (parentId.HasValue && parentId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysDictItem.ParentId), parentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(itemCode))
        {
            request.Conditions.AddFilter(nameof(SysDictItem.ItemCode), itemCode.Trim());
        }

        if (isDefault.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysDictItem.IsDefault), isDefault.Value);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysDictItem.Status), status.Value);
        }
    }

    /// <summary>
    /// 应用系统字典排序
    /// </summary>
    private static void ApplyDictSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort(nameof(SysDict.Sort), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysDict.DictType), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysDict.DictCode), SortDirection.Ascending, 2);
    }

    /// <summary>
    /// 应用系统字典项排序
    /// </summary>
    private static void ApplyDictItemSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort(nameof(SysDictItem.DictId), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysDictItem.ParentId), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysDictItem.Sort), SortDirection.Ascending, 2);
        request.Conditions.AddSort(nameof(SysDictItem.ItemCode), SortDirection.Ascending, 3);
    }

    /// <summary>
    /// 构建系统字典项树
    /// </summary>
    /// <param name="nodes">系统字典项节点</param>
    /// <returns>系统字典项树</returns>
    private static IReadOnlyList<DictItemTreeNodeDto> BuildItemTree(IReadOnlyList<DictItemTreeNodeDto> nodes)
    {
        var nodeMap = nodes.ToDictionary(node => node.BasicId);
        var roots = new List<DictItemTreeNodeDto>();

        foreach (var node in nodes.OrderBy(node => node.Sort).ThenBy(node => node.ItemCode, StringComparer.Ordinal))
        {
            if (node.ParentId.HasValue
                && node.ParentId.Value != node.BasicId
                && nodeMap.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
                continue;
            }

            roots.Add(node);
        }

        return roots;
    }
}
