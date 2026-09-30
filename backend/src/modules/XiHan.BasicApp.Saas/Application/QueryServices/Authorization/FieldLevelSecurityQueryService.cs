// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 字段级安全查询应用服务
/// </summary>
/// <remarks>
/// 规则列表本身不参与字段安全：配置页要看到规则的全部内容才能维护。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "字段级安全")]
public sealed class FieldLevelSecurityQueryService
    : SaasApplicationService, IFieldLevelSecurityQueryService
{
    private readonly IFieldSecurityEntityCatalog _catalog;

    private readonly IDepartmentRepository _departmentRepository;

    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldLevelSecurityQueryService(
        IFieldLevelSecurityRepository fieldLevelSecurityRepository,
        IFieldSecurityEntityCatalog catalog,
        IRoleRepository roleRepository,
        IDepartmentRepository departmentRepository,
        ITenantUserRepository tenantUserRepository,
        IUserRepository userRepository)
    {
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
        _catalog = catalog;
        _roleRepository = roleRepository;
        _departmentRepository = departmentRepository;
        _tenantUserRepository = tenantUserRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 获取字段级安全分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<FieldLevelSecurityListItemDto>> GetFieldLevelSecurityPageAsync(FieldLevelSecurityPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);
        var policies = await _fieldLevelSecurityRepository.GetPagedAsync(request, cancellationToken);
        if (policies.Items.Count == 0)
        {
            return new PageResultDtoBase<FieldLevelSecurityListItemDto>([], policies.Page);
        }

        var targets = await LoadTargetsAsync(policies.Items, cancellationToken);
        var items = policies.Items
            .Select(policy =>
            {
                var (targetCode, targetName) = targets.Resolve(policy);
                return FieldLevelSecurityApplicationMapper.ToListItemDto(policy, _catalog, targetCode, targetName);
            })
            .ToList();
        return new PageResultDtoBase<FieldLevelSecurityListItemDto>(items, policies.Page);
    }

    /// <summary>
    /// 获取字段级安全详情
    /// </summary>
    /// <param name="id">字段级安全主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Read)]
    public async Task<FieldLevelSecurityDetailDto?> GetFieldLevelSecurityDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "字段级安全主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var policy = await _fieldLevelSecurityRepository.GetByIdAsync(id, cancellationToken);
        if (policy is null)
        {
            return null;
        }

        var (targetCode, targetName) = (await LoadTargetsAsync([policy], cancellationToken)).Resolve(policy);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(policy, _catalog, targetCode, targetName);
    }

    /// <summary>
    /// 获取可配置字段安全的实体与字段（配置页的实体、字段下拉）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体与字段，按实体显示名排序</returns>
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Read)]
    public Task<IReadOnlyList<FieldSecurityEntityDto>> GetFieldSecurityEntitiesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<FieldSecurityEntityDto> entities = [.. _catalog.GetEntities().Select(FieldLevelSecurityApplicationMapper.ToEntityDto)];
        return Task.FromResult(entities);
    }

    /// <summary>
    /// 构建分页请求：显式条件 + 前端通用排序与过滤；无排序时按实体、字段、目标稳定排序
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(FieldLevelSecurityPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysFieldLevelSecurity>(
                input.Keyword.Trim(),
                policy => policy.FieldName,
                policy => policy.Remark);
        }

        if (input.TargetType.HasValue)
        {
            request.Conditions.AddFilter((SysFieldLevelSecurity policy) => policy.TargetType, input.TargetType.Value);
        }

        if (input.TargetId.HasValue)
        {
            request.Conditions.AddFilter((SysFieldLevelSecurity policy) => policy.TargetId, input.TargetId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.EntityName))
        {
            request.Conditions.AddFilter((SysFieldLevelSecurity policy) => policy.EntityName, input.EntityName.Trim());
        }

        if (input.MaskStrategy.HasValue)
        {
            request.Conditions.AddFilter((SysFieldLevelSecurity policy) => policy.MaskStrategy, input.MaskStrategy.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysFieldLevelSecurity policy) => policy.Status, input.Status.Value);
        }

        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        if (request.Conditions.Sorts.Count == 0)
        {
            request.Conditions.AddSort((SysFieldLevelSecurity policy) => policy.EntityName, SortDirection.Ascending, 0);
            request.Conditions.AddSort((SysFieldLevelSecurity policy) => policy.FieldName, SortDirection.Ascending, 1);
            request.Conditions.AddSort((SysFieldLevelSecurity policy) => policy.TargetType, SortDirection.Ascending, 2);
            request.Conditions.AddSort((SysFieldLevelSecurity policy) => policy.BasicId, SortDirection.Ascending, 3);
        }

        return request;
    }

    /// <summary>
    /// 批量加载规则目标（角色/部门/成员）的编码与名称
    /// </summary>
    private async Task<TargetLookup> LoadTargetsAsync(IEnumerable<SysFieldLevelSecurity> policies, CancellationToken cancellationToken)
    {
        long[] IdsOf(FieldSecurityTargetType type) =>
            [.. policies.Where(policy => policy.TargetType == type).Select(policy => policy.TargetId).Where(id => id > 0).Distinct()];

        var roleIds = IdsOf(FieldSecurityTargetType.Role);
        var departmentIds = IdsOf(FieldSecurityTargetType.Department);
        var userIds = IdsOf(FieldSecurityTargetType.User);

        var roles = roleIds.Length == 0 ? [] : await _roleRepository.GetByIdsAsync(roleIds, cancellationToken);
        var departments = departmentIds.Length == 0 ? [] : await _departmentRepository.GetByIdsAsync(departmentIds, cancellationToken);
        var members = userIds.Length == 0
            ? []
            : await _tenantUserRepository.GetListAsync(member => userIds.Contains(member.UserId), cancellationToken);
        // 账号是账号域数据，成员可能注册在别处，按主键跨租户取
        var users = userIds.Length == 0 ? [] : await _userRepository.GetListByIdsIgnoreTenantAsync(userIds, cancellationToken);

        return new TargetLookup(
            roles.ToDictionary(role => role.BasicId),
            departments.ToDictionary(department => department.BasicId),
            members.GroupBy(member => member.UserId).ToDictionary(group => group.Key, group => group.First()),
            users.ToDictionary(user => user.BasicId));
    }

    /// <summary>
    /// 目标摘要查找表
    /// </summary>
    private sealed record TargetLookup(
        IReadOnlyDictionary<long, SysRole> Roles,
        IReadOnlyDictionary<long, SysDepartment> Departments,
        IReadOnlyDictionary<long, SysTenantUser> Members,
        IReadOnlyDictionary<long, SysUser> Users)
    {
        public (string? Code, string? Name) Resolve(SysFieldLevelSecurity policy) => policy.TargetType switch
        {
            FieldSecurityTargetType.Role => Roles.TryGetValue(policy.TargetId, out var role) ? (role.RoleCode, role.RoleName) : (null, null),
            FieldSecurityTargetType.Department => Departments.TryGetValue(policy.TargetId, out var department) ? (department.DepartmentCode, department.DepartmentName) : (null, null),
            FieldSecurityTargetType.User => ResolveUser(policy.TargetId),
            _ => (null, null)
        };

        /// <summary>
        /// 用户目标：成员名片优先，没填时取账号的姓名、昵称、用户名；编码为用户名
        /// </summary>
        private (string? Code, string? Name) ResolveUser(long userId)
        {
            var user = Users.GetValueOrDefault(userId);
            var memberName = Members.GetValueOrDefault(userId)?.DisplayName;
            var name = !string.IsNullOrWhiteSpace(memberName)
                ? memberName
                : user is null ? null : user.RealName ?? user.NickName ?? user.UserName;
            return (user?.UserName, name);
        }
    }
}
