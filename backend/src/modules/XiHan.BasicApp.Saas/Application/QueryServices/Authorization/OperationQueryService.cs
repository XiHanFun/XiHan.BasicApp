#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationQueryService
// Guid:9d4e752f-b7dc-4742-b3f7-c5c6e3a7be4c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
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
/// 操作查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "操作")]
public sealed class OperationQueryService(IOperationRepository operationRepository)
    : SaasApplicationService, IOperationQueryService
{
    /// <summary>
    /// 操作仓储
    /// </summary>
    private readonly IOperationRepository _operationRepository = operationRepository;

    /// <summary>
    /// 获取操作分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Operation.Read)]
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

        var request = BuildOperationSelectRequest(input);
        var operations = await _operationRepository.GetPagedAsync(request, cancellationToken);

        return [.. operations.Items.Select(OperationApplicationMapper.ToSelectItemDto)];
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
            Behavior = input.Behavior,
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

        request.Conditions.AddSort(nameof(SysOperation.Category), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysOperation.OperationTypeCode), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysOperation.Sort), SortDirection.Ascending, 2);
        request.Conditions.AddSort(nameof(SysOperation.OperationCode), SortDirection.Ascending, 3);
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

        request.Conditions.AddSort(nameof(SysOperation.Category), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysOperation.OperationTypeCode), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysOperation.Sort), SortDirection.Ascending, 2);
        request.Conditions.AddSort(nameof(SysOperation.OperationCode), SortDirection.Ascending, 3);
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
            request.Conditions.SetKeyword(
                keyword.Trim(),
                nameof(SysOperation.OperationCode),
                nameof(SysOperation.OperationName),
                nameof(SysOperation.Description));
        }

        if (operationTypeCode.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOperation.OperationTypeCode), operationTypeCode.Value);
        }

        if (category.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOperation.Category), category.Value);
        }

        if (httpMethod.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOperation.HttpMethod), httpMethod.Value);
        }

        if (isDangerous.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOperation.IsDangerous), isDangerous.Value);
        }

        if (isRequireAudit.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOperation.IsRequireAudit), isRequireAudit.Value);
        }

        if (isGlobal.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOperation.IsGlobal), isGlobal.Value);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOperation.Status), status.Value);
        }
    }
}
