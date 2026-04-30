#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentQueryService
// Guid:89edfdc1-d4d5-4b47-8339-12614634b2c5
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
/// 部门查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "部门")]
public sealed class DepartmentQueryService(IDepartmentRepository departmentRepository)
    : SaasApplicationService, IDepartmentQueryService
{
    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 获取部门分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Department.Read)]
    public async Task<PageResultDtoBase<DepartmentListItemDto>> GetDepartmentPageAsync(DepartmentPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildDepartmentPageRequest(input);
        var departments = await _departmentRepository.GetPagedAsync(request, cancellationToken);

        return departments.Map(DepartmentApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取部门详情
    /// </summary>
    /// <param name="id">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Department.Read)]
    public async Task<DepartmentDetailDto?> GetDepartmentDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "部门主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        return department is null ? null : DepartmentApplicationMapper.ToDetailDto(department);
    }

    /// <summary>
    /// 获取部门树
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门树</returns>
    [PermissionAuthorize(SaasPermissionCodes.Department.Read)]
    public async Task<IReadOnlyList<DepartmentTreeNodeDto>> GetDepartmentTreeAsync(DepartmentTreeQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildDepartmentTreeRequest(input);
        var departments = await _departmentRepository.GetPagedAsync(request, cancellationToken);
        if (departments.Items.Count == 0)
        {
            return [];
        }

        var nodes = departments.Items
            .Select(DepartmentApplicationMapper.ToTreeNodeDto)
            .ToList();
        return BuildTree(nodes);
    }

    /// <summary>
    /// 构建部门分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>部门分页请求</returns>
    private static BasicAppPRDto BuildDepartmentPageRequest(DepartmentPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        ApplyCommonDepartmentFilters(
            request,
            input.Keyword,
            input.ParentId,
            input.DepartmentType,
            input.LeaderId,
            input.Status);
        ApplyDepartmentSorts(request);
        return request;
    }

    /// <summary>
    /// 构建部门树请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>部门树请求</returns>
    private static BasicAppPRDto BuildDepartmentTreeRequest(DepartmentTreeQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 5000);
        ApplyCommonDepartmentFilters(
            request,
            input.Keyword,
            parentId: null,
            departmentType: null,
            leaderId: null,
            status: input.OnlyEnabled ? EnableStatus.Enabled : null);
        ApplyDepartmentSorts(request);
        return request;
    }

    /// <summary>
    /// 应用部门通用筛选条件
    /// </summary>
    private static void ApplyCommonDepartmentFilters(
        BasicAppPRDto request,
        string? keyword,
        long? parentId,
        DepartmentType? departmentType,
        long? leaderId,
        EnableStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword(
                keyword.Trim(),
                nameof(SysDepartment.DepartmentCode),
                nameof(SysDepartment.DepartmentName),
                nameof(SysDepartment.Phone),
                nameof(SysDepartment.Email),
                nameof(SysDepartment.Address),
                nameof(SysDepartment.Remark));
        }

        if (parentId.HasValue && parentId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysDepartment.ParentId), parentId.Value);
        }

        if (departmentType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysDepartment.DepartmentType), departmentType.Value);
        }

        if (leaderId.HasValue && leaderId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysDepartment.LeaderId), leaderId.Value);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysDepartment.Status), status.Value);
        }
    }

    /// <summary>
    /// 应用部门排序
    /// </summary>
    private static void ApplyDepartmentSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort(nameof(SysDepartment.ParentId), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysDepartment.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysDepartment.DepartmentCode), SortDirection.Ascending, 2);
    }

    /// <summary>
    /// 构建部门树
    /// </summary>
    private static IReadOnlyList<DepartmentTreeNodeDto> BuildTree(IReadOnlyList<DepartmentTreeNodeDto> nodes)
    {
        var nodeMap = nodes.ToDictionary(node => node.BasicId);
        var roots = new List<DepartmentTreeNodeDto>();

        foreach (var node in nodes.OrderBy(node => node.Sort).ThenBy(node => node.DepartmentCode, StringComparer.Ordinal))
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
