#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentManagementQueryService
// Author:zhaifanhua
// CreateTime:2026/05/20 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 部门管理页面查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "部门管理")]
public sealed class DepartmentManagementQueryService
    : SaasApplicationService, IDepartmentManagementQueryService
{
    private const int MaxChildDepartmentCount = 100;

    private const int MaxMemberCount = 200;

    private readonly IDepartmentRepository _departmentRepository;

    private readonly IUserDepartmentRepository _userDepartmentRepository;

    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentManagementQueryService(
        IDepartmentRepository departmentRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IUserRepository userRepository)
    {
        _departmentRepository = departmentRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Department.Read)]
    public async Task<DepartmentManagementDetailDto?> GetDepartmentManagementDetailAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId), "部门主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
        if (department is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        return new DepartmentManagementDetailDto
        {
            Department = DepartmentApplicationMapper.ToDetailDto(department),
            ChildDepartments = await GetChildDepartmentsAsync(departmentId, cancellationToken),
            Members = await GetMembersAsync(departmentId, cancellationToken),
            GeneratedTime = now,
        };
    }

    private static DepartmentManagementMemberDto ToMemberDto(SysUserDepartment userDepartment, SysUser? user)
    {
        return new DepartmentManagementMemberDto
        {
            BasicId = userDepartment.BasicId,
            UserId = userDepartment.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            IsMain = userDepartment.IsMain,
            Status = userDepartment.Status,
            Remark = userDepartment.Remark,
            CreatedTime = userDepartment.CreatedTime,
        };
    }

    private async Task<List<DepartmentListItemDto>> GetChildDepartmentsAsync(long departmentId, CancellationToken cancellationToken)
    {
        var children = await _departmentRepository.GetListAsync(
            item => item.ParentId == departmentId,
            item => item.Sort,
            cancellationToken);

        return [.. children
            .Select(DepartmentApplicationMapper.ToListItemDto)
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.DepartmentCode)
            .Take(MaxChildDepartmentCount)];
    }

    private async Task<List<DepartmentManagementMemberDto>> GetMembersAsync(long departmentId, CancellationToken cancellationToken)
    {
        var userDepartments = await _userDepartmentRepository.GetListAsync(
            item => item.DepartmentId == departmentId && item.Status == ValidityStatus.Valid,
            item => item.CreatedTime,
            cancellationToken);

        if (userDepartments.Count == 0)
        {
            return [];
        }

        var userIds = userDepartments.Select(item => item.UserId).Distinct().ToArray();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userMap = users.ToDictionary(user => user.BasicId);

        return [.. userDepartments
            .Select(item => ToMemberDto(item, userMap.GetValueOrDefault(item.UserId)))
            .OrderByDescending(item => item.IsMain)
            .ThenBy(item => item.UserName)
            .Take(MaxMemberCount)];
    }
}
