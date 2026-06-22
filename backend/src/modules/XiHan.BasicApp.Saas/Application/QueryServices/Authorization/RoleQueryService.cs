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
using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
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
/// 角色查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色")]
public sealed class RoleQueryService
    : SaasApplicationService, IRoleQueryService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 已启用角色选择项缓存
    /// </summary>
    private readonly IDistributedCache<SaasRoleSelectCacheItem, string> _roleSelectCache;

    /// <summary>
    /// 超级管理员保护守卫
    /// </summary>
    private readonly ISuperAdminProtector _superAdminProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleQueryService(
        IRoleRepository roleRepository,
        IDistributedCache<SaasRoleSelectCacheItem, string> roleSelectCache,
        ISuperAdminProtector superAdminProtector)
    {
        _roleRepository = roleRepository;
        _roleSelectCache = roleSelectCache;
        _superAdminProtector = superAdminProtector;
    }

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

        // 超管隐藏：非超管用户在列表中排除 super_admin 角色（超管自身不受限）
        if (!_superAdminProtector.IsCurrentUserSuperAdmin())
        {
            request.Conditions.AddFilter((SysRole role) => role.RoleCode, "super_admin", QueryOperator.NotEqual);
        }

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

        // 超管隐藏：非超管不得按 id 读取 super_admin 角色详情（列表/选择项已隐藏，详情按 not-found 处理保持一致）
        if (!_superAdminProtector.IsCurrentUserSuperAdmin()
            && await _superAdminProtector.IsProtectedRoleAsync(id, cancellationToken))
        {
            return null;
        }

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

        // 带关键字的搜索命中率低，直接查库；仅缓存无关键字的（类型/是否全局/上限）筛选组合。
        // 失效由角色定义写路径触发——RoleAppService 增删改启停调 InvalidateRoleDefinitionAsync。
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            return await QueryEnabledRolesAsync(input, cancellationToken);
        }

        var cacheKey = SaasCacheKeys.RoleSelect((int?)input.RoleType, input.IsGlobal, input.Limit);
        var item = await _roleSelectCache.GetOrAddAsync(
            cacheKey,
            async () => new SaasRoleSelectCacheItem
            {
                Items = [.. await QueryEnabledRolesAsync(input, cancellationToken)],
                CachedAt = DateTimeOffset.UtcNow
            },
            CreateCacheOptions,
            hideErrors: true,
            token: cancellationToken);

        return item is null
            ? await QueryEnabledRolesAsync(input, cancellationToken)
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

        request.Conditions.AddSort((SysRole role) => role.RoleType, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysRole role) => role.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysRole role) => role.RoleCode, SortDirection.Ascending, 2);
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

        // 超管隐藏：选择项始终排除 super_admin 角色（内置单例、不可经下拉授予；
        // 结果按 type/global/limit 缓存，按用户过滤会污染缓存，故无条件排除最安全）
        request.Conditions.AddFilter((SysRole role) => role.RoleCode, "super_admin", QueryOperator.NotEqual);

        request.Conditions.AddSort((SysRole role) => role.RoleType, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysRole role) => role.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysRole role) => role.RoleCode, SortDirection.Ascending, 2);
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
            request.Conditions.SetKeyword<SysRole>(
                keyword.Trim(),
                role => role.RoleCode,
                role => role.RoleName,
                role => role.RoleDescription,
                role => role.Remark);
        }

        if (roleType.HasValue)
        {
            request.Conditions.AddFilter((SysRole role) => role.RoleType, roleType.Value);
        }

        if (dataScope.HasValue)
        {
            request.Conditions.AddFilter((SysRole role) => role.DataScope, dataScope.Value);
        }

        if (isGlobal.HasValue)
        {
            // IsGlobal 为派生属性（TenantId == 0），不落库，故按租户列过滤：全局=租户0，非全局=非租户0
            request.Conditions.AddFilter(
                (SysRole role) => role.TenantId,
                0L,
                isGlobal.Value ? QueryOperator.Equal : QueryOperator.NotEqual);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter((SysRole role) => role.Status, status.Value);
        }
    }

    /// <summary>
    /// 实时查询已启用角色选择项（缓存未命中或带关键字时执行）。
    /// </summary>
    private async Task<IReadOnlyList<RoleSelectItemDto>> QueryEnabledRolesAsync(RoleSelectQueryDto input, CancellationToken cancellationToken)
    {
        var request = BuildRoleSelectRequest(input);
        var roles = await _roleRepository.GetPagedAsync(request, cancellationToken);

        return [.. roles.Items.Select(RoleApplicationMapper.ToSelectItemDto)];
    }
}
