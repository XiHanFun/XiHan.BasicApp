// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 主体属性加载器：按指定租户读角色、任职、成员类型与租户编码
/// </summary>
/// <remarks>
/// 每次读取都切到指定租户的上下文，登录检查、试算这类主体与当前上下文不同的场景也读得对。
/// </remarks>
public sealed class SaasConditionSubjectLoader
{
    private readonly IAuthorizationSnapshotQueryService _authorizationSnapshotQueryService;
    private readonly IUserDepartmentRepository _userDepartmentRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasConditionSubjectLoader(
        IAuthorizationSnapshotQueryService authorizationSnapshotQueryService,
        IUserDepartmentRepository userDepartmentRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        ITenantUserRepository tenantUserRepository,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant)
    {
        _authorizationSnapshotQueryService = authorizationSnapshotQueryService;
        _userDepartmentRepository = userDepartmentRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _tenantUserRepository = tenantUserRepository;
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 租户里有效的角色编码（取授权快照）
    /// </summary>
    public async Task<IReadOnlyList<string>> GetRoleCodesAsync(long userId, long tenantId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        using var _ = ChangeTenant(tenantId);
        var snapshot = await _authorizationSnapshotQueryService.BuildAsync(userId, now, cancellationToken);
        return snapshot.Roles;
    }

    /// <summary>
    /// 租户里有效任职的部门与岗位：停用或已删的部门、岗位不算
    /// </summary>
    public async Task<SaasConditionOrganization> GetOrganizationAsync(long userId, long tenantId, CancellationToken cancellationToken = default)
    {
        using var _ = ChangeTenant(tenantId);
        var assignments = await _userDepartmentRepository.GetValidByUserIdAsync(userId, cancellationToken);
        if (assignments.Count == 0)
        {
            return SaasConditionOrganization.Empty;
        }

        var departments = (await _departmentRepository.GetByIdsAsync(assignments.Select(item => item.DepartmentId).Distinct(), cancellationToken))
            .Where(department => department.Status == EnableStatus.Enabled && !string.IsNullOrWhiteSpace(department.DepartmentCode))
            .ToDictionary(department => department.BasicId, department => department.DepartmentCode);
        var positionIds = assignments.Where(item => item.PositionId is > 0).Select(item => item.PositionId!.Value).Distinct().ToList();
        var positions = positionIds.Count == 0
            ? []
            : (await _positionRepository.GetByIdsAsync(positionIds, cancellationToken))
                .Where(position => position.Status == EnableStatus.Enabled && !string.IsNullOrWhiteSpace(position.PositionCode))
                .ToDictionary(position => position.BasicId, position => position.PositionCode);

        // 岗位挂在任职上：所在部门停用的任职，岗位也不算
        var effective = assignments.Where(item => departments.ContainsKey(item.DepartmentId)).ToList();
        var main = effective.FirstOrDefault(item => item.IsMain);
        return new SaasConditionOrganization(
            [.. effective.Select(item => departments[item.DepartmentId]).Distinct(StringComparer.OrdinalIgnoreCase)],
            main is null ? null : departments[main.DepartmentId],
            [.. effective
                .Where(item => item.PositionId is > 0 && positions.ContainsKey(item.PositionId.Value))
                .Select(item => positions[item.PositionId!.Value])
                .Distinct(StringComparer.OrdinalIgnoreCase)]);
    }

    /// <summary>
    /// 租户成员类型；平台或不是成员时为空
    /// </summary>
    public async Task<TenantMemberType?> GetMemberTypeAsync(long userId, long tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
        {
            return null;
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(tenantId, userId, cancellationToken);
        return membership?.MemberType;
    }

    /// <summary>
    /// 租户编码；平台为空
    /// </summary>
    public async Task<string?> GetTenantCodeAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
        {
            return null;
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        return tenant?.TenantCode;
    }

    private IDisposable ChangeTenant(long tenantId)
    {
        var target = tenantId > 0 ? tenantId : (long?)null;
        return _currentTenant.Change(target, target?.ToString());
    }
}

/// <summary>
/// 主体的任职信息
/// </summary>
/// <param name="DepartmentCodes">部门编码</param>
/// <param name="MainDepartmentCode">主部门编码</param>
/// <param name="PositionCodes">岗位编码</param>
public sealed record SaasConditionOrganization(
    IReadOnlyList<string> DepartmentCodes,
    string? MainDepartmentCode,
    IReadOnlyList<string> PositionCodes)
{
    /// <summary>
    /// 没有任职
    /// </summary>
    public static SaasConditionOrganization Empty { get; } = new([], null, []);
}
