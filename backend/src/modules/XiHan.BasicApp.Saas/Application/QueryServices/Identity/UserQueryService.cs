#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserQueryService
// Guid:7ff333fb-54a4-4dc5-ad98-b3a893fcaecd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
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
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户")]
public sealed class UserQueryService
    : SaasApplicationService, IUserQueryService
{
    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 用户角色关联仓储
    /// </summary>
    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 用户部门关联仓储
    /// </summary>
    private readonly IUserDepartmentRepository _userDepartmentRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository;

    /// <summary>
    /// 用户安全状态仓储
    /// </summary>
    private readonly IUserSecurityRepository _userSecurityRepository;

    /// <summary>
    /// 字段级安全（读脱敏 / 写校验）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 用户数据范围过滤（按角色/用户数据范围 + 部门归属裁决可见用户集）
    /// </summary>
    private readonly IUserDataScopeFilterService _userDataScopeFilter;

    /// <summary>
    /// 超级管理员保护守卫
    /// </summary>
    private readonly ISuperAdminProtector _superAdminProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserQueryService(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IDepartmentRepository departmentRepository,
        IUserSecurityRepository userSecurityRepository,
        IFieldSecurityService fieldSecurityService,
        IUserDataScopeFilterService userDataScopeFilter,
        ISuperAdminProtector superAdminProtector)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _departmentRepository = departmentRepository;
        _userSecurityRepository = userSecurityRepository;
        _fieldSecurity = fieldSecurityService;
        _userDataScopeFilter = userDataScopeFilter;
        _superAdminProtector = superAdminProtector;
    }

    /// <summary>
    /// 获取用户分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.User.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<UserListItemDto>> GetUserPageAsync(UserPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildUserPageRequest(input);

        // 数据范围过滤：将列表收敛到当前用户可见的用户主键集合（超管/全部范围不限制）
        var dataScope = await _userDataScopeFilter.ResolveAccessibleUsersAsync(DateTimeOffset.UtcNow, cancellationToken);
        if (!dataScope.Unrestricted)
        {
            request.Conditions.AddFilterIn<SysUser, long>(user => user.BasicId, dataScope.UserIds.Cast<object>());
        }

        // 超管隐藏：非超管用户排除超管用户（叠加在数据范围过滤之上，超管自身不受限）
        if (!_superAdminProtector.IsCurrentUserSuperAdmin())
        {
            var protectedUserIds = await _superAdminProtector.GetProtectedUserIdsAsync(cancellationToken);
            if (protectedUserIds.Count > 0)
            {
                request.Conditions.AddFilterIn<SysUser, long>(user => user.BasicId, protectedUserIds.Cast<object>(), QueryOperator.NotIn);
            }
        }

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段（防按受保护字段排序泄漏真实顺序）；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysUser", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyUserSorts(request);
        }

        var users = await _userRepository.GetPagedAsync(request, cancellationToken);

        if (users.Items.Count == 0)
        {
            return new PageResultDtoBase<UserListItemDto>([], users.Page)
            {
                ExtendDatas = users.ExtendDatas
            };
        }

        // 按当前页用户主键批量预取关联数据，避免逐行 N+1
        var userIds = users.Items.Select(user => user.BasicId).Distinct().ToList();
        var roleNameMap = await BuildRoleNameMapAsync(userIds, cancellationToken);
        var departmentNameMap = await BuildDepartmentNameMapAsync(userIds, cancellationToken);
        var securityMap = await BuildSecurityMapAsync(userIds, cancellationToken);

        var items = users.Items.Select(user =>
        {
            securityMap.TryGetValue(user.BasicId, out var security);
            return UserApplicationMapper.ToListItemDto(
                user,
                roleNameMap.TryGetValue(user.BasicId, out var roleNames) ? roleNames : [],
                departmentNameMap.TryGetValue(user.BasicId, out var departmentName) ? departmentName : null,
                security.IsLocked,
                security.TwoFactorEnabled);
        }).ToList();

        // 服务端字段脱敏：按当前用户在 SysUser 资源上的有效 FLS 规则就地脱敏
        await _fieldSecurity.ApplyAsync("SysUser", items, cancellationToken);

        return new PageResultDtoBase<UserListItemDto>(items, users.Page)
        {
            ExtendDatas = users.ExtendDatas
        };
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    /// <param name="id">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.User.Read)]
    public async Task<UserDetailDto?> GetUserDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 超管隐藏：非超管不得按 id 读取超管用户详情（列表/选择项已隐藏，详情按 not-found 处理保持一致）
        if (!_superAdminProtector.IsCurrentUserSuperAdmin()
            && await _superAdminProtector.IsProtectedUserAsync(id, cancellationToken))
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var detail = UserApplicationMapper.ToDetailDto(user);
        // 服务端字段脱敏：详情同样按有效 FLS 规则就地脱敏
        await _fieldSecurity.ApplyAsync("SysUser", detail, cancellationToken);
        return detail;
    }

    /// <summary>
    /// 获取已启用用户选择项
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已启用用户选择项</returns>
    [PermissionAuthorize(SaasPermissionCodes.User.Read)]
    public async Task<IReadOnlyList<UserSelectItemDto>> GetEnabledUsersAsync(UserSelectQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildUserSelectRequest(input);

        // 超管隐藏：非超管用户的选择项中排除超管用户（超管自身不受限）
        if (!_superAdminProtector.IsCurrentUserSuperAdmin())
        {
            var protectedUserIds = await _superAdminProtector.GetProtectedUserIdsAsync(cancellationToken);
            if (protectedUserIds.Count > 0)
            {
                request.Conditions.AddFilterIn<SysUser, long>(user => user.BasicId, protectedUserIds.Cast<object>(), QueryOperator.NotIn);
            }
        }

        var users = await _userRepository.GetPagedAsync(request, cancellationToken);

        return [.. users.Items.Select(UserApplicationMapper.ToSelectItemDto)];
    }

    /// <summary>
    /// 构建用户分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>用户分页请求</returns>
    private static BasicAppPRDto BuildUserPageRequest(UserPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        ApplyCommonUserFilters(
            request,
            input.Keyword,
            input.Gender,
            input.Status,
            input.IsSystemAccount,
            input.Language,
            input.Country);
        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetUserPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
        return request;
    }

    /// <summary>
    /// 构建用户选择请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>用户选择请求</returns>
    private static BasicAppPRDto BuildUserSelectRequest(UserSelectQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 500);
        ApplyCommonUserFilters(
            request,
            input.Keyword,
            input.Gender,
            EnableStatus.Enabled,
            input.IsSystemAccount,
            language: null,
            country: null);
        ApplyUserSorts(request);
        return request;
    }

    /// <summary>
    /// 应用用户通用筛选条件
    /// </summary>
    private static void ApplyCommonUserFilters(
        BasicAppPRDto request,
        string? keyword,
        UserGender? gender,
        EnableStatus? status,
        bool? isSystemAccount,
        string? language,
        string? country)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword(
                keyword.Trim(),
                nameof(SysUser.UserName),
                nameof(SysUser.RealName),
                nameof(SysUser.NickName),
                nameof(SysUser.Country),
                nameof(SysUser.Remark));
        }

        if (gender.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUser.Gender), gender.Value);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUser.Status), status.Value);
        }

        if (isSystemAccount.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUser.IsSystemAccount), isSystemAccount.Value);
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            request.Conditions.AddFilter(nameof(SysUser.Language), language.Trim());
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            request.Conditions.AddFilter(nameof(SysUser.Country), country.Trim());
        }
    }

    /// <summary>
    /// 应用用户排序
    /// </summary>
    private static void ApplyUserSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort(nameof(SysUser.IsSystemAccount), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysUser.CreatedTime), SortDirection.Descending, 1);
        request.Conditions.AddSort(nameof(SysUser.UserName), SortDirection.Ascending, 2);
    }

    /// <summary>
    /// 批量构建「用户主键 → 角色名称集合」映射（有效授权 + 启用角色）
    /// </summary>
    private async Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> BuildRoleNameMapAsync(
        List<long> userIds,
        CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleRepository.GetListAsync(
            userRole => userIds.Contains(userRole.UserId)
                && userRole.Status == ValidityStatus.Valid,
            cancellationToken);
        if (userRoles.Count == 0)
        {
            return new Dictionary<long, IReadOnlyList<string>>();
        }

        var roleIds = userRoles.Select(userRole => userRole.RoleId).Distinct().ToList();
        var roles = await _roleRepository.GetEnabledByIdsAsync(roleIds, cancellationToken);
        var roleNameById = roles
            .GroupBy(role => role.BasicId)
            .ToDictionary(group => group.Key, group => group.First().RoleName);

        return userRoles
            .GroupBy(userRole => userRole.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[.. group
                    .Where(userRole => roleNameById.ContainsKey(userRole.RoleId))
                    .Select(userRole => roleNameById[userRole.RoleId])
                    .Distinct()
                    .OrderBy(name => name, StringComparer.CurrentCulture)]);
    }

    /// <summary>
    /// 批量构建「用户主键 → 主部门名称」映射（优先主部门，否则取首个有效归属）
    /// </summary>
    private async Task<IReadOnlyDictionary<long, string>> BuildDepartmentNameMapAsync(
        List<long> userIds,
        CancellationToken cancellationToken)
    {
        var userDepartments = await _userDepartmentRepository.GetListAsync(
            userDepartment => userIds.Contains(userDepartment.UserId)
                && userDepartment.Status == ValidityStatus.Valid,
            cancellationToken);
        if (userDepartments.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        var departmentIds = userDepartments.Select(item => item.DepartmentId).Distinct().ToList();
        var departments = await _departmentRepository.GetListAsync(
            department => departmentIds.Contains(department.BasicId),
            cancellationToken);
        var departmentNameById = departments
            .GroupBy(department => department.BasicId)
            .ToDictionary(group => group.Key, group => group.First().DepartmentName);

        var result = new Dictionary<long, string>();
        foreach (var group in userDepartments.GroupBy(item => item.UserId))
        {
            var chosen = group.OrderByDescending(item => item.IsMain).First();
            if (departmentNameById.TryGetValue(chosen.DepartmentId, out var departmentName))
            {
                result[group.Key] = departmentName;
            }
        }

        return result;
    }

    /// <summary>
    /// 批量构建「用户主键 → 安全标记」映射（锁定 / 双因素）
    /// </summary>
    private async Task<IReadOnlyDictionary<long, (bool IsLocked, bool TwoFactorEnabled)>> BuildSecurityMapAsync(
        List<long> userIds,
        CancellationToken cancellationToken)
    {
        var securities = await _userSecurityRepository.GetListAsync(
            security => userIds.Contains(security.UserId),
            cancellationToken);
        if (securities.Count == 0)
        {
            return new Dictionary<long, (bool IsLocked, bool TwoFactorEnabled)>();
        }

        return securities
            .GroupBy(security => security.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var security = group.First();
                    return (security.IsLocked, security.TwoFactorEnabled);
                });
    }
}
