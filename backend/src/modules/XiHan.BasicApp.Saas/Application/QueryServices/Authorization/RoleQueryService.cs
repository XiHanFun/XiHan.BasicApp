#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleQueryService
// Guid:33fb14dc-3b6d-4390-97fd-f047f99c3482
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
/// 角色查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色")]
public sealed class RoleQueryService(IRoleRepository roleRepository)
    : SaasApplicationService, IRoleQueryService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 获取角色分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Role.Read)]
    public async Task<PageResultDtoBase<RoleListItemDto>> GetRolePageAsync(RolePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildRolePageRequest(input);
        var roles = await _roleRepository.GetPagedAsync(request, cancellationToken);

        return roles.Map(RoleApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    /// <param name="id">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Role.Read)]
    public async Task<RoleDetailDto?> GetRoleDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        return role is null ? null : RoleApplicationMapper.ToDetailDto(role);
    }

    /// <summary>
    /// 获取已启用角色选择项
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已启用角色选择项</returns>
    [PermissionAuthorize(SaasPermissionCodes.Role.Read)]
    public async Task<IReadOnlyList<RoleSelectItemDto>> GetEnabledRolesAsync(RoleSelectQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildRoleSelectRequest(input);
        var roles = await _roleRepository.GetPagedAsync(request, cancellationToken);

        return [.. roles.Items.Select(RoleApplicationMapper.ToSelectItemDto)];
    }

    /// <summary>
    /// 构建角色分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>角色分页请求</returns>
    private static BasicAppPRDto BuildRolePageRequest(RolePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        ApplyCommonRoleFilters(
            request,
            input.Keyword,
            input.RoleType,
            input.DataScope,
            input.IsGlobal,
            input.Status);

        request.Conditions.AddSort(nameof(SysRole.RoleType), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysRole.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysRole.RoleCode), SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 构建角色选择请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>角色选择请求</returns>
    private static BasicAppPRDto BuildRoleSelectRequest(RoleSelectQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 500);

        ApplyCommonRoleFilters(
            request,
            input.Keyword,
            input.RoleType,
            dataScope: null,
            input.IsGlobal,
            EnableStatus.Enabled);

        request.Conditions.AddSort(nameof(SysRole.RoleType), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysRole.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysRole.RoleCode), SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 应用角色通用筛选条件
    /// </summary>
    private static void ApplyCommonRoleFilters(
        BasicAppPRDto request,
        string? keyword,
        RoleType? roleType,
        DataPermissionScope? dataScope,
        bool? isGlobal,
        EnableStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword(
                keyword.Trim(),
                nameof(SysRole.RoleCode),
                nameof(SysRole.RoleName),
                nameof(SysRole.RoleDescription),
                nameof(SysRole.Remark));
        }

        if (roleType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysRole.RoleType), roleType.Value);
        }

        if (dataScope.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysRole.DataScope), dataScope.Value);
        }

        if (isGlobal.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysRole.IsGlobal), isGlobal.Value);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysRole.Status), status.Value);
        }
    }
}
