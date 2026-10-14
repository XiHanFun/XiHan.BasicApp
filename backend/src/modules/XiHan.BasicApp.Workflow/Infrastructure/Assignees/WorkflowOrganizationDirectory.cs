// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Workflow.Infrastructure.Assignees;

/// <summary>
/// 工作流组织目录（受理人解析器读取 Saas 组织数据的统一入口）
/// </summary>
/// <remarks>
/// 一切候选人最后都与「当前上下文可见的启用账号」求交：平台是平台账号，租户是本租户已接受的成员；
/// 停用账号、离开租户的成员不会收到待办。仓储自带租户过滤，解析在实例所属租户的上下文中执行。
/// </remarks>
public sealed class WorkflowOrganizationDirectory
{
    private readonly IUserDirectory _userDirectory;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserDepartmentRepository _userDepartmentRepository;
    private readonly IPositionRepository _positionRepository;

    private HashSet<long>? _enabledUserIds;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowOrganizationDirectory(
        IUserDirectory userDirectory,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IDepartmentRepository departmentRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IPositionRepository positionRepository)
    {
        _userDirectory = userDirectory;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _departmentRepository = departmentRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _positionRepository = positionRepository;
    }

    /// <summary>
    /// 只保留当前上下文可见的启用账号（保持原顺序、去重），转为受理人标识
    /// </summary>
    /// <param name="userIds">候选用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受理人标识列表</returns>
    public async Task<IReadOnlyList<string>> FilterEnabledAsync(IEnumerable<long> userIds, CancellationToken cancellationToken = default)
    {
        _enabledUserIds ??= [.. await _userDirectory.GetEnabledIdsAsync(cancellationToken)];

        return [.. userIds
            .Where(_enabledUserIds.Contains)
            .Distinct()
            .Select(userId => userId.ToString(CultureInfo.InvariantCulture))];
    }

    /// <summary>
    /// 角色成员（启用角色、有效期内的授予）
    /// </summary>
    public async Task<IReadOnlyList<long>> GetRoleMemberIdsAsync(IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default)
    {
        var codes = roleCodes.ToList();
        var roles = await _roleRepository.GetListAsync(
            role => codes.Contains(role.RoleCode) && role.Status == EnableStatus.Enabled,
            cancellationToken);
        if (roles.Count == 0)
        {
            return [];
        }

        var userRoles = await _userRoleRepository.GetValidByRoleIdsAsync(
            [.. roles.Select(role => role.BasicId)], DateTimeOffset.UtcNow, cancellationToken);
        return [.. userRoles.Select(userRole => userRole.UserId).Distinct().Order()];
    }

    /// <summary>
    /// 按编码取启用部门
    /// </summary>
    public async Task<IReadOnlyList<SysDepartment>> GetDepartmentsAsync(IReadOnlyCollection<string> departmentCodes, CancellationToken cancellationToken = default)
    {
        var codes = departmentCodes.ToList();
        return await _departmentRepository.GetListAsync(
            department => codes.Contains(department.DepartmentCode) && department.Status == EnableStatus.Enabled,
            cancellationToken);
    }

    /// <summary>
    /// 部门的有效成员
    /// </summary>
    public async Task<IReadOnlyList<long>> GetDepartmentMemberIdsAsync(IEnumerable<long> departmentIds, CancellationToken cancellationToken = default)
    {
        var members = await _userDepartmentRepository.GetUserIdsByDepartmentIdsAsync(departmentIds, cancellationToken);
        return [.. members.Order()];
    }

    /// <summary>
    /// 岗位成员（按有效任职上的岗位）
    /// </summary>
    public async Task<IReadOnlyList<long>> GetPositionMemberIdsAsync(IReadOnlyCollection<string> positionCodes, CancellationToken cancellationToken = default)
    {
        var codes = positionCodes.ToList();
        var positions = await _positionRepository.GetListAsync(
            position => codes.Contains(position.PositionCode) && position.Status == EnableStatus.Enabled,
            cancellationToken);
        if (positions.Count == 0)
        {
            return [];
        }

        var positionIds = positions.Select(position => (long?)position.BasicId).ToList();
        var assignments = await _userDepartmentRepository.GetListAsync(
            assignment => positionIds.Contains(assignment.PositionId) && assignment.Status == ValidityStatus.Valid,
            cancellationToken);
        return [.. assignments.Select(assignment => assignment.UserId).Distinct().Order()];
    }

    /// <summary>
    /// 发起人逐级主管链（从主部门起向上，跳过发起人自己与连续重复的负责人，最多 <paramref name="maxLevels"/> 级）
    /// </summary>
    public async Task<IReadOnlyList<long>> GetLeaderChainAsync(long starterUserId, int maxLevels, CancellationToken cancellationToken = default)
    {
        var assignments = await _userDepartmentRepository.GetValidByUserIdAsync(starterUserId, cancellationToken);
        var startAssignment = assignments
            .OrderByDescending(assignment => assignment.IsMain)
            .ThenBy(assignment => assignment.CreatedTime)
            .FirstOrDefault();
        if (startAssignment is null)
        {
            return [];
        }

        _enabledUserIds ??= [.. await _userDirectory.GetEnabledIdsAsync(cancellationToken)];

        var leaders = new List<long>();
        var visited = new HashSet<long>();
        long? departmentId = startAssignment.DepartmentId;

        while (departmentId is { } currentId && leaders.Count < maxLevels && visited.Add(currentId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var department = await _departmentRepository.GetByIdAsync(currentId, cancellationToken);
            if (department is null)
            {
                break;
            }

            if (department.Status == EnableStatus.Enabled
                && department.LeaderId is { } leaderId
                && leaderId != starterUserId
                && !leaders.Contains(leaderId)
                && _enabledUserIds.Contains(leaderId))
            {
                leaders.Add(leaderId);
            }

            departmentId = department.ParentId is > 0 ? department.ParentId : null;
        }

        return leaders;
    }
}
