// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Workflow.Application.QueryServices;

/// <summary>
/// 审批人选择查询应用服务
/// </summary>
/// <remarks>
/// 设计器配置审批人、待办转办/加签都要选人，但审批人不一定有用户/角色/部门管理权限，
/// 这里按工作流自己的门槛提供轻量选项：选用户登录即可（与聊天选人同语义：启用用户 + 超管隐藏），
/// 角色/岗位/部门只在设计流程时用，需要流程查看权限。角色、部门、岗位以编码返回（规则按编码引用）。
/// </remarks>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "审批人选择")]
public sealed class WorkflowDirectoryQueryService : WorkflowApplicationService, IWorkflowDirectoryQueryService
{
    private const int MaxOptionCount = 200;

    private readonly IUserDirectory _userDirectory;
    private readonly ISuperAdminProtector _superAdminProtector;
    private readonly IRoleRepository _roleRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowDirectoryQueryService(
        IUserDirectory userDirectory,
        ISuperAdminProtector superAdminProtector,
        IRoleRepository roleRepository,
        IPositionRepository positionRepository,
        IDepartmentRepository departmentRepository,
        ICurrentTenant currentTenant)
    {
        _userDirectory = userDirectory;
        _superAdminProtector = superAdminProtector;
        _roleRepository = roleRepository;
        _positionRepository = positionRepository;
        _departmentRepository = departmentRepository;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 用户选项（按主键回显时返回当前上下文可见的这些用户，含已停用者以便回显；否则按关键字搜启用用户）
    /// </summary>
    [HttpPost]
    public async Task<List<WorkflowUserOptionDto>> GetUserOptionsAsync(WorkflowUserOptionQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.Ids is { Count: > 0 })
        {
            var options = new List<WorkflowUserOptionDto>();
            foreach (var id in input.Ids.Where(id => id > 0).Distinct().Take(MaxOptionCount))
            {
                var user = await _userDirectory.FindAsync(id, cancellationToken);
                if (user is not null)
                {
                    options.Add(ToUserOption(user));
                }
            }

            return options;
        }

        var request = new BasicAppPRDto { Conditions = new QueryConditions() };
        request.Page.PageSize = Math.Clamp(input.Limit, 1, MaxOptionCount);
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysUser.UserName),
                nameof(SysUser.RealName),
                nameof(SysUser.NickName));
        }

        request.Conditions.AddFilter(nameof(SysUser.Status), EnableStatus.Enabled);
        request.Conditions.AddSort(nameof(SysUser.CreatedTime), SortDirection.Descending, 0);

        // 超管隐藏：非超管用户的选择项中排除超管用户（超管自身不受限）
        if (!_superAdminProtector.IsCurrentUserSuperAdmin())
        {
            var protectedUserIds = await _superAdminProtector.GetProtectedUserIdsAsync(cancellationToken);
            if (protectedUserIds.Count > 0)
            {
                request.Conditions.AddFilterIn<SysUser, long>(user => user.BasicId, protectedUserIds.Cast<object>(), QueryOperator.NotIn);
            }
        }

        var users = await _userDirectory.GetPagedAsync(request, cancellationToken);
        return [.. users.Items.Select(ToUserOption)];
    }

    /// <summary>
    /// 角色选项（平台角色与本租户角色；租户里不提供只在平台成立的超级管理员）
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Read)]
    public async Task<List<WorkflowOrganizationOptionDto>> GetRoleOptionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scopeId = _currentTenant.Id ?? 0;
        var roles = await _roleRepository.GetListAsync(
            role => (role.TenantId == 0 || role.TenantId == scopeId) && role.Status == EnableStatus.Enabled,
            cancellationToken);

        return [.. roles
            .Where(role => scopeId == 0 || role.RoleCode != SaasRoleCodes.SuperAdmin)
            .OrderBy(role => role.Sort)
            .ThenBy(role => role.RoleCode, StringComparer.Ordinal)
            .Select(role => new WorkflowOrganizationOptionDto { Code = role.RoleCode, Name = role.RoleName })];
    }

    /// <summary>
    /// 岗位选项
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Read)]
    public async Task<List<WorkflowOrganizationOptionDto>> GetPositionOptionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scopeId = _currentTenant.Id ?? 0;
        var positions = await _positionRepository.GetListAsync(
            position => position.TenantId == scopeId && position.Status == EnableStatus.Enabled,
            cancellationToken);

        return [.. positions
            .OrderBy(position => position.Sort)
            .ThenBy(position => position.PositionCode, StringComparer.Ordinal)
            .Select(position => new WorkflowOrganizationOptionDto { Code = position.PositionCode, Name = position.PositionName })];
    }

    /// <summary>
    /// 部门树
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Read)]
    public async Task<List<WorkflowDepartmentOptionDto>> GetDepartmentTreeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scopeId = _currentTenant.Id ?? 0;
        var departments = await _departmentRepository.GetListAsync(
            department => department.TenantId == scopeId && department.Status == EnableStatus.Enabled,
            cancellationToken);

        return BuildTree(departments, parentId: null);
    }

    private static List<WorkflowDepartmentOptionDto> BuildTree(IReadOnlyList<SysDepartment> departments, long? parentId)
    {
        return [.. departments
            .Where(department => (department.ParentId ?? 0) == (parentId ?? 0))
            .OrderBy(department => department.Sort)
            .Select(department => new WorkflowDepartmentOptionDto
            {
                Code = department.DepartmentCode,
                Name = department.DepartmentName,
                Children = BuildTree(departments, department.BasicId)
            })];
    }

    private static WorkflowUserOptionDto ToUserOption(SysUser user)
    {
        return new WorkflowUserOptionDto
        {
            UserId = user.BasicId,
            UserName = user.UserName,
            DisplayName = !string.IsNullOrWhiteSpace(user.RealName) ? user.RealName
                : !string.IsNullOrWhiteSpace(user.NickName) ? user.NickName
                : user.UserName,
            Avatar = user.Avatar
        };
    }
}
