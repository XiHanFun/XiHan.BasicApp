// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 操作查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "操作")]
public sealed class OperationQueryService
    : SaasApplicationService, IOperationQueryService
{
    /// <summary>
    /// 操作仓储
    /// </summary>
    private readonly IOperationRepository _operationRepository;

    /// <summary>
    /// 可选操作选择项缓存
    /// </summary>
    private readonly IDistributedCache<SaasOperationSelectCacheItem, string> _operationSelectCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OperationQueryService(
        IOperationRepository operationRepository,
        IDistributedCache<SaasOperationSelectCacheItem, string> operationSelectCache)
    {
        _operationRepository = operationRepository;
        _operationSelectCache = operationSelectCache;
    }

    /// <summary>
    /// 获取操作分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Operation.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<OperationListItemDto>> GetOperationPageAsync(OperationPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildOperationPageRequest(input);
        var operations = await _operationRepository.GetPagedAsync(request, cancellationToken);

        return operations.Map(OperationApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取操作详情
    /// </summary>
    /// <param name="id">操作主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Operation.Read)]
    public async Task<OperationDetailDto?> GetOperationDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "操作主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var operation = await _operationRepository.GetByIdAsync(id, cancellationToken);
        return operation is null ? null : OperationApplicationMapper.ToDetailDto(operation);
    }

    /// <summary>
    /// 获取可选全局操作列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可选全局操作列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Operation.Read)]
    public async Task<IReadOnlyList<OperationSelectItemDto>> GetAvailableGlobalOperationsAsync(OperationSelectQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 带关键字的搜索命中率低，直接查库；仅缓存无关键字的（类型/分类/上限）筛选组合。
        // 失效由操作定义写路径触发——OperationAppService 增删改启停调 InvalidateOperationDefinitionAsync。
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            return await QueryAvailableGlobalOperationsAsync(input, cancellationToken);
        }

        var cacheKey = SaasCacheKeys.OperationSelect((int?)input.OperationTypeCode, (int?)input.Category, input.Limit);
        var item = await _operationSelectCache.GetOrAddAsync(
            cacheKey,
            async () => new SaasOperationSelectCacheItem
            {
                Items = [.. await QueryAvailableGlobalOperationsAsync(input, cancellationToken)],
                CachedAt = DateTimeOffset.UtcNow
            },
            CreateCacheOptions,
            hideErrors: true,
            token: cancellationToken);

        return item is null
            ? await QueryAvailableGlobalOperationsAsync(input, cancellationToken)
            : item.Items;
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
    }

    /// <summary>
    /// 构建操作分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>操作分页请求</returns>
    private static BasicAppPRDto BuildOperationPageRequest(OperationPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        ApplyCommonOperationFilters(
            request,
            input.Keyword,
            input.OperationTypeCode,
            input.Category,
            input.HttpMethod,
            input.IsDangerous,
            input.IsRequireAudit,
            input.IsGlobal,
            input.Status);

        request.Conditions.AddSort((SysOperation operation) => operation.Category, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysOperation operation) => operation.OperationTypeCode, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysOperation operation) => operation.Sort, SortDirection.Ascending, 2);
        request.Conditions.AddSort((SysOperation operation) => operation.OperationCode, SortDirection.Ascending, 3);
        return request;
    }

    /// <summary>
    /// 构建操作选择请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>操作选择请求</returns>
    private static BasicAppPRDto BuildOperationSelectRequest(OperationSelectQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 500);

        ApplyCommonOperationFilters(
            request,
            input.Keyword,
            input.OperationTypeCode,
            input.Category,
            httpMethod: null,
            isDangerous: null,
            isRequireAudit: null,
            isGlobal: true,
            status: EnableStatus.Enabled);

        request.Conditions.AddSort((SysOperation operation) => operation.Category, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysOperation operation) => operation.OperationTypeCode, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysOperation operation) => operation.Sort, SortDirection.Ascending, 2);
        request.Conditions.AddSort((SysOperation operation) => operation.OperationCode, SortDirection.Ascending, 3);
        return request;
    }

    /// <summary>
    /// 应用操作通用筛选条件
    /// </summary>
    private static void ApplyCommonOperationFilters(
        BasicAppPRDto request,
        string? keyword,
        OperationTypeCode? operationTypeCode,
        OperationCategory? category,
        HttpMethodType? httpMethod,
        bool? isDangerous,
        bool? isRequireAudit,
        bool? isGlobal,
        EnableStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword<SysOperation>(
                keyword.Trim(),
                operation => operation.OperationCode,
                operation => operation.OperationName,
                operation => operation.Description);
        }

        if (operationTypeCode.HasValue)
        {
            request.Conditions.AddFilter((SysOperation operation) => operation.OperationTypeCode, operationTypeCode.Value);
        }

        if (category.HasValue)
        {
            request.Conditions.AddFilter((SysOperation operation) => operation.Category, category.Value);
        }

        if (httpMethod.HasValue)
        {
            request.Conditions.AddFilter((SysOperation operation) => operation.HttpMethod, httpMethod.Value);
        }

        if (isDangerous.HasValue)
        {
            request.Conditions.AddFilter((SysOperation operation) => operation.IsDangerous, isDangerous.Value);
        }

        if (isRequireAudit.HasValue)
        {
            request.Conditions.AddFilter((SysOperation operation) => operation.IsRequireAudit, isRequireAudit.Value);
        }

        if (isGlobal.HasValue)
        {
            // IsGlobal 为派生属性（TenantId == 0），不落库，故按租户列过滤：全局=租户0，非全局=非租户0
            request.Conditions.AddFilter(
                (SysOperation operation) => operation.TenantId,
                0L,
                isGlobal.Value ? QueryOperator.Equal : QueryOperator.NotEqual);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter((SysOperation operation) => operation.Status, status.Value);
        }
    }

    /// <summary>
    /// 实时查询可选全局操作列表（缓存未命中或带关键字时执行）。
    /// </summary>
    private async Task<IReadOnlyList<OperationSelectItemDto>> QueryAvailableGlobalOperationsAsync(OperationSelectQueryDto input, CancellationToken cancellationToken)
    {
        var request = BuildOperationSelectRequest(input);
        var operations = await _operationRepository.GetPagedAsync(request, cancellationToken);

        return [.. operations.Items.Select(OperationApplicationMapper.ToSelectItemDto)];
    }
}
