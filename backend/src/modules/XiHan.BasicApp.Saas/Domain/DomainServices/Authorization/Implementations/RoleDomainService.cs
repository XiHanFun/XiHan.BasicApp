#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDomainService
// Guid:06498db5-2c6f-4f1e-8199-f86094a4e82b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色领域服务实现
/// </summary>
public sealed class RoleDomainService
    : IRoleDomainService
{
    private readonly IRoleRepository _roleRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    private readonly IRoleDataScopeRepository _roleDataScopeRepository;

    private readonly IPermissionRepository _permissionRepository;

    private readonly IDepartmentRepository _departmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleDomainService(
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IRoleHierarchyRepository roleHierarchyRepository,
        IRoleDataScopeRepository roleDataScopeRepository,
        IPermissionRepository permissionRepository,
        IDepartmentRepository departmentRepository)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
        _permissionRepository = permissionRepository;
        _departmentRepository = departmentRepository;
    }

    /// <inheritdoc />
    public async Task<RoleCommandResult> CreateRoleAsync(RoleCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);

        var roleCode = command.RoleCode.Trim();
        if (await _roleRepository.GetByCodeAsync(roleCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("角色编码已存在。");
        }

        var role = new SysRole
        {
            RoleCode = roleCode,
            RoleName = command.RoleName.Trim(),
            RoleDescription = NormalizeNullable(command.RoleDescription),
            RoleType = command.RoleType,
            DataScope = command.DataScope,
            MaxMembers = command.MaxMembers,
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedRole = await _roleRepository.AddAsync(role, cancellationToken);
        return new RoleCommandResult(savedRole);
    }

    /// <inheritdoc />
    public async Task<RoleCommandResult> UpdateRoleAsync(RoleUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);

        var role = await GetEditableRoleOrThrowAsync(command.BasicId, cancellationToken);
        role.RoleName = command.RoleName.Trim();
        role.RoleDescription = NormalizeNullable(command.RoleDescription);
        role.RoleType = command.RoleType;
        role.DataScope = command.DataScope;
        role.MaxMembers = command.MaxMembers;
        role.Sort = command.Sort;
        role.Remark = NormalizeNullable(command.Remark);

        var savedRole = await _roleRepository.UpdateAsync(role, cancellationToken);
        return new RoleCommandResult(savedRole);
    }

    /// <inheritdoc />
    public async Task<RoleCommandResult> UpdateRoleStatusAsync(RoleStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var role = await GetEditableRoleOrThrowAsync(command.BasicId, cancellationToken);
        role.Status = command.Status;
        role.Remark = NormalizeNullable(command.Remark) ?? role.Remark;

        var savedRole = await _roleRepository.UpdateAsync(role, cancellationToken);
        return new RoleCommandResult(savedRole);
    }

    /// <inheritdoc />
    public async Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await GetEditableRoleOrThrowAsync(id, cancellationToken);
        await EnsureRoleNotReferencedAsync(role.BasicId, cancellationToken);

        if (!await _roleRepository.DeleteAsync(role, cancellationToken))
        {
            throw new InvalidOperationException("角色删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<RolePermissionCommandResult> CreateRolePermissionAsync(RolePermissionGrantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRolePermissionGrantCommand(command);

        _ = await GetEnabledRoleForPermissionOrThrowAsync(command.RoleId, cancellationToken);
        var permission = await GetEnabledPermissionOrThrowAsync(command.PermissionId, cancellationToken);
        if (await _rolePermissionRepository.AnyAsync(
            rolePermission => rolePermission.RoleId == command.RoleId && rolePermission.PermissionId == command.PermissionId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色权限已绑定。");
        }

        var rolePermission = new SysRolePermission
        {
            RoleId = command.RoleId,
            PermissionId = command.PermissionId,
            PermissionAction = command.PermissionAction,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            GrantReason = NormalizeNullable(command.GrantReason),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedRolePermission = await _rolePermissionRepository.AddAsync(rolePermission, cancellationToken);
        return new RolePermissionCommandResult(savedRolePermission, permission);
    }

    /// <inheritdoc />
    public async Task<RolePermissionCommandResult> UpdateRolePermissionAsync(RolePermissionUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRolePermissionUpdateCommand(command);

        var rolePermission = await GetRolePermissionOrThrowAsync(command.BasicId, cancellationToken);
        _ = await GetEnabledRoleForPermissionOrThrowAsync(rolePermission.RoleId, cancellationToken);
        var permission = await GetEnabledPermissionOrThrowAsync(rolePermission.PermissionId, cancellationToken);

        rolePermission.PermissionAction = command.PermissionAction;
        rolePermission.EffectiveTime = command.EffectiveTime;
        rolePermission.ExpirationTime = command.ExpirationTime;
        rolePermission.GrantReason = NormalizeNullable(command.GrantReason);
        rolePermission.Remark = NormalizeNullable(command.Remark);

        var savedRolePermission = await _rolePermissionRepository.UpdateAsync(rolePermission, cancellationToken);
        return new RolePermissionCommandResult(savedRolePermission, permission);
    }

    /// <inheritdoc />
    public async Task<RolePermissionCommandResult> UpdateRolePermissionStatusAsync(RolePermissionStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色权限绑定主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var rolePermission = await GetRolePermissionOrThrowAsync(command.BasicId, cancellationToken);
        var permission = command.Status == ValidityStatus.Valid
            ? await GetEnabledPermissionOrThrowAsync(rolePermission.PermissionId, cancellationToken)
            : await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken);

        if (command.Status == ValidityStatus.Valid)
        {
            _ = await GetEnabledRoleForPermissionOrThrowAsync(rolePermission.RoleId, cancellationToken);
        }

        rolePermission.Status = command.Status;
        rolePermission.Remark = NormalizeNullable(command.Remark);

        var savedRolePermission = await _rolePermissionRepository.UpdateAsync(rolePermission, cancellationToken);
        return new RolePermissionCommandResult(savedRolePermission, permission);
    }

    /// <inheritdoc />
    public async Task DeleteRolePermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rolePermission = await GetRolePermissionOrThrowAsync(id, cancellationToken);
        rolePermission.Status = ValidityStatus.Invalid;

        _ = await _rolePermissionRepository.UpdateAsync(rolePermission, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RoleDataScopeCommandResult> CreateRoleDataScopeAsync(RoleDataScopeGrantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRoleDataScopeGrantCommand(command);

        _ = await GetCustomDataScopeRoleOrThrowAsync(command.RoleId, cancellationToken);
        var department = await GetEnabledDepartmentOrThrowAsync(command.DepartmentId, cancellationToken);
        if (await _roleDataScopeRepository.AnyAsync(
            scope => scope.RoleId == command.RoleId && scope.DepartmentId == command.DepartmentId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色数据范围已绑定。");
        }

        var dataScope = new SysRoleDataScope
        {
            RoleId = command.RoleId,
            DepartmentId = command.DepartmentId,
            IncludeChildren = command.IncludeChildren,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedDataScope = await _roleDataScopeRepository.AddAsync(dataScope, cancellationToken);
        return new RoleDataScopeCommandResult(savedDataScope, department);
    }

    /// <inheritdoc />
    public async Task<RoleDataScopeCommandResult> UpdateRoleDataScopeAsync(RoleDataScopeUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRoleDataScopeUpdateCommand(command);

        var dataScope = await GetRoleDataScopeOrThrowAsync(command.BasicId, cancellationToken);
        _ = await GetCustomDataScopeRoleOrThrowAsync(dataScope.RoleId, cancellationToken);
        var department = await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken);

        dataScope.IncludeChildren = command.IncludeChildren;
        dataScope.EffectiveTime = command.EffectiveTime;
        dataScope.ExpirationTime = command.ExpirationTime;
        dataScope.Remark = NormalizeNullable(command.Remark);

        var savedDataScope = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return new RoleDataScopeCommandResult(savedDataScope, department);
    }

    /// <inheritdoc />
    public async Task<RoleDataScopeCommandResult> UpdateRoleDataScopeStatusAsync(RoleDataScopeStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色数据范围绑定主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var dataScope = await GetRoleDataScopeOrThrowAsync(command.BasicId, cancellationToken);
        var department = command.Status == ValidityStatus.Valid
            ? await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken)
            : await _departmentRepository.GetByIdAsync(dataScope.DepartmentId, cancellationToken);

        if (command.Status == ValidityStatus.Valid)
        {
            _ = await GetCustomDataScopeRoleOrThrowAsync(dataScope.RoleId, cancellationToken);
        }

        dataScope.Status = command.Status;
        dataScope.Remark = NormalizeNullable(command.Remark);

        var savedDataScope = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return new RoleDataScopeCommandResult(savedDataScope, department);
    }

    /// <inheritdoc />
    public async Task DeleteRoleDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataScope = await GetRoleDataScopeOrThrowAsync(id, cancellationToken);
        dataScope.Status = ValidityStatus.Invalid;

        _ = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RoleHierarchyCommandResult> CreateRoleHierarchyAsync(RoleHierarchyCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRoleHierarchyCreateCommand(command);

        var ancestor = await GetRoleForHierarchyOrThrowAsync(command.AncestorId, cancellationToken);
        var descendant = await GetRoleForHierarchyOrThrowAsync(command.DescendantId, cancellationToken);
        EnsureDescendantCanBeMaintainedForHierarchy(descendant);

        if (await _roleHierarchyRepository.AnyAsync(
            hierarchy => hierarchy.AncestorId == command.AncestorId && hierarchy.DescendantId == command.DescendantId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色继承关系已存在。");
        }

        if (await _roleHierarchyRepository.AnyAsync(
            hierarchy => hierarchy.AncestorId == command.DescendantId && hierarchy.DescendantId == command.AncestorId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色继承关系会形成环路。");
        }

        var existingHierarchies = (await _roleHierarchyRepository.GetAllAsync(cancellationToken)).ToList();
        var addList = new List<SysRoleHierarchy>();
        var workingHierarchies = new List<SysRoleHierarchy>(existingHierarchies);

        EnsureSelfHierarchy(ancestor, workingHierarchies, addList);
        EnsureSelfHierarchy(descendant, workingHierarchies, addList);

        var ancestorClosures = workingHierarchies
            .Where(hierarchy => hierarchy.DescendantId == ancestor.BasicId)
            .ToArray();
        var descendantClosures = workingHierarchies
            .Where(hierarchy => hierarchy.AncestorId == descendant.BasicId)
            .ToArray();
        var existingPairs = workingHierarchies
            .Select(hierarchy => new HierarchyPair(hierarchy.AncestorId, hierarchy.DescendantId))
            .ToHashSet();

        foreach (var ancestorClosure in ancestorClosures)
        {
            foreach (var descendantClosure in descendantClosures)
            {
                var pair = new HierarchyPair(ancestorClosure.AncestorId, descendantClosure.DescendantId);
                if (existingPairs.Contains(pair))
                {
                    continue;
                }

                var isDirectEdge = pair.AncestorId == ancestor.BasicId && pair.DescendantId == descendant.BasicId;
                var hierarchy = new SysRoleHierarchy
                {
                    AncestorId = pair.AncestorId,
                    DescendantId = pair.DescendantId,
                    Depth = ancestorClosure.Depth + 1 + descendantClosure.Depth,
                    Path = BuildCombinedPath(ancestorClosure, descendantClosure),
                    Remark = isDirectEdge ? NormalizeNullable(command.Remark) : null
                };

                addList.Add(hierarchy);
                workingHierarchies.Add(hierarchy);
                existingPairs.Add(pair);
            }
        }

        if (addList.Count == 0)
        {
            throw new InvalidOperationException("未生成新的角色继承闭包记录。");
        }

        _ = await _roleHierarchyRepository.AddRangeAsync(addList, cancellationToken);

        var directHierarchy = await _roleHierarchyRepository.GetFirstAsync(
            hierarchy => hierarchy.AncestorId == ancestor.BasicId && hierarchy.DescendantId == descendant.BasicId && hierarchy.Depth == 1,
            cancellationToken)
            ?? throw new InvalidOperationException("角色直接继承关系创建失败。");

        return new RoleHierarchyCommandResult(directHierarchy, ancestor, descendant);
    }

    /// <inheritdoc />
    public async Task DeleteRoleHierarchyAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directHierarchy = await GetDirectHierarchyOrThrowAsync(id, cancellationToken);
        var descendant = await GetRoleForHierarchyOrThrowAsync(directHierarchy.DescendantId, cancellationToken);
        EnsureDescendantCanBeMaintainedForHierarchy(descendant);

        var existingHierarchies = (await _roleHierarchyRepository.GetAllAsync(cancellationToken)).ToList();
        var remainingDirectEdges = existingHierarchies
            .Where(hierarchy => hierarchy.Depth == 1 && hierarchy.BasicId != directHierarchy.BasicId)
            .Where(hierarchy => hierarchy.AncestorId != directHierarchy.AncestorId || hierarchy.DescendantId != directHierarchy.DescendantId)
            .ToArray();
        var remainingPairs = BuildReachablePairs(remainingDirectEdges);
        var directPair = new HierarchyPair(directHierarchy.AncestorId, directHierarchy.DescendantId);
        if (remainingPairs.Contains(directPair))
        {
            throw new InvalidOperationException("该直接继承关系存在替代路径，需先清理替代路径后再删除。");
        }

        var impactedAncestorIds = existingHierarchies
            .Where(hierarchy => hierarchy.DescendantId == directHierarchy.AncestorId)
            .Select(hierarchy => hierarchy.AncestorId)
            .Append(directHierarchy.AncestorId)
            .Distinct()
            .ToHashSet();
        var impactedDescendantIds = existingHierarchies
            .Where(hierarchy => hierarchy.AncestorId == directHierarchy.DescendantId)
            .Select(hierarchy => hierarchy.DescendantId)
            .Append(directHierarchy.DescendantId)
            .Distinct()
            .ToHashSet();
        var deletePairs = existingHierarchies
            .Where(hierarchy => hierarchy.Depth > 0)
            .Select(hierarchy => new HierarchyPair(hierarchy.AncestorId, hierarchy.DescendantId))
            .Where(pair => impactedAncestorIds.Contains(pair.AncestorId))
            .Where(pair => impactedDescendantIds.Contains(pair.DescendantId))
            .Where(pair => !remainingPairs.Contains(pair))
            .Distinct()
            .ToArray();

        foreach (var pair in deletePairs)
        {
            var ancestorId = pair.AncestorId;
            var descendantId = pair.DescendantId;
            _ = await _roleHierarchyRepository.DeleteAsync(
                hierarchy => hierarchy.AncestorId == ancestorId && hierarchy.DescendantId == descendantId,
                cancellationToken);
        }
    }

    private static void ValidateCreateCommand(RoleCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RoleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RoleName);
        ValidateCommonCommand(command.RoleType, command.DataScope, command.MaxMembers);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateUpdateCommand(RoleUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(command.RoleName);
        ValidateCommonCommand(command.RoleType, command.DataScope, command.MaxMembers);
    }

    private static void ValidateCommonCommand(RoleType roleType, DataPermissionScope dataScope, int maxMembers)
    {
        ValidateEnum(roleType, nameof(roleType));
        ValidateEnum(dataScope, nameof(dataScope));

        if (roleType == RoleType.System)
        {
            throw new InvalidOperationException("系统角色必须通过平台种子或运维流程维护。");
        }

        if (maxMembers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMembers), "最大成员数不能小于 0。");
        }
    }

    private static void EnsureRoleCanBeMaintained(SysRole role)
    {
        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色必须通过平台运维流程维护。");
        }
    }

    private static void ValidateRolePermissionGrantCommand(RolePermissionGrantCommand command)
    {
        if (command.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        if (command.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限主键必须大于 0。");
        }

        ValidateEnum(command.PermissionAction, nameof(command.PermissionAction));
        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime);
    }

    private static void ValidateRolePermissionUpdateCommand(RolePermissionUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色权限绑定主键必须大于 0。");
        }

        ValidateEnum(command.PermissionAction, nameof(command.PermissionAction));
        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime);
    }

    private static void ValidateRoleDataScopeGrantCommand(RoleDataScopeGrantCommand command)
    {
        if (command.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        if (command.DepartmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "部门主键必须大于 0。");
        }

        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime);
    }

    private static void ValidateRoleDataScopeUpdateCommand(RoleDataScopeUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色数据范围绑定主键必须大于 0。");
        }

        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime);
    }

    private static void ValidateRoleHierarchyCreateCommand(RoleHierarchyCreateCommand command)
    {
        if (command.AncestorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "祖先角色主键必须大于 0。");
        }

        if (command.DescendantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "后代角色主键必须大于 0。");
        }

        if (command.AncestorId == command.DescendantId)
        {
            throw new InvalidOperationException("角色不能继承自己。");
        }
    }

    private static void EnsureDescendantCanBeMaintainedForHierarchy(SysRole descendant)
    {
        if (descendant.IsGlobal || descendant.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色必须通过平台运维流程维护继承关系。");
        }
    }

    private static void EnsureSelfHierarchy(SysRole role, List<SysRoleHierarchy> workingHierarchies, List<SysRoleHierarchy> addList)
    {
        if (workingHierarchies.Any(hierarchy => hierarchy.AncestorId == role.BasicId && hierarchy.DescendantId == role.BasicId))
        {
            return;
        }

        var hierarchy = new SysRoleHierarchy
        {
            AncestorId = role.BasicId,
            DescendantId = role.BasicId,
            Depth = 0,
            Path = role.BasicId.ToString()
        };

        workingHierarchies.Add(hierarchy);
        if (!role.IsGlobal && role.RoleType != RoleType.System)
        {
            addList.Add(hierarchy);
        }
    }

    private static string BuildCombinedPath(SysRoleHierarchy ancestorClosure, SysRoleHierarchy descendantClosure)
    {
        var pathIds = new List<long>(BuildPathIds(ancestorClosure));
        pathIds.AddRange(BuildPathIds(descendantClosure));
        return string.Join("/", pathIds);
    }

    private static IReadOnlyList<long> BuildPathIds(SysRoleHierarchy hierarchy)
    {
        if (!string.IsNullOrWhiteSpace(hierarchy.Path))
        {
            var ids = hierarchy.Path
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => long.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToArray();

            if (ids.Length > 0 && ids[0] == hierarchy.AncestorId && ids[^1] == hierarchy.DescendantId)
            {
                return ids;
            }
        }

        return hierarchy.AncestorId == hierarchy.DescendantId
            ? [hierarchy.AncestorId]
            : [hierarchy.AncestorId, hierarchy.DescendantId];
    }

    private static HashSet<HierarchyPair> BuildReachablePairs(IEnumerable<SysRoleHierarchy> directEdges)
    {
        var edges = directEdges.ToArray();
        var adjacency = edges
            .GroupBy(edge => edge.AncestorId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.DescendantId).Distinct().ToArray());
        var nodes = edges
            .SelectMany(edge => new[] { edge.AncestorId, edge.DescendantId })
            .Distinct()
            .ToArray();
        var pairs = new HashSet<HierarchyPair>();

        foreach (var startNode in nodes)
        {
            pairs.Add(new HierarchyPair(startNode, startNode));

            var visited = new HashSet<long> { startNode };
            var queue = new Queue<long>();
            if (adjacency.TryGetValue(startNode, out var children))
            {
                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current))
                {
                    continue;
                }

                pairs.Add(new HierarchyPair(startNode, current));

                if (!adjacency.TryGetValue(current, out var nextChildren))
                {
                    continue;
                }

                foreach (var child in nextChildren)
                {
                    queue.Enqueue(child);
                }
            }
        }

        return pairs;
    }

    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException("失效时间必须晚于生效时间。");
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<SysRole> GetEditableRoleOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色主键必须大于 0。");
        }

        var role = await _roleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        EnsureRoleCanBeMaintained(role);
        return role;
    }

    private async Task EnsureRoleNotReferencedAsync(long roleId, CancellationToken cancellationToken)
    {
        if (await _userRoleRepository.AnyAsync(userRole => userRole.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("角色已分配给用户，不能删除。");
        }

        if (await _rolePermissionRepository.AnyAsync(rolePermission => rolePermission.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("角色已绑定权限，不能删除。");
        }

        if (await _roleHierarchyRepository.AnyAsync(
            hierarchy => hierarchy.Depth > 0 && (hierarchy.AncestorId == roleId || hierarchy.DescendantId == roleId),
            cancellationToken))
        {
            throw new InvalidOperationException("角色存在继承关系，不能删除。");
        }

        if (await _roleDataScopeRepository.AnyAsync(dataScope => dataScope.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("角色已配置数据范围，不能删除。");
        }
    }

    private async Task<SysRolePermission> GetRolePermissionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色权限绑定主键必须大于 0。");
        }

        return await _rolePermissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色权限绑定不存在。");
    }

    private async Task<SysRole> GetEnabledRoleForPermissionOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能维护权限绑定。");
        }

        return role;
    }

    private async Task<SysPermission> GetEnabledPermissionOrThrowAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能绑定到角色。");
        }

        return permission;
    }

    private async Task<SysRoleDataScope> GetRoleDataScopeOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色数据范围绑定主键必须大于 0。");
        }

        return await _roleDataScopeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色数据范围绑定不存在。");
    }

    private async Task<SysRole> GetCustomDataScopeRoleOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能维护数据范围。");
        }

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色必须通过平台运维流程维护。");
        }

        if (role.DataScope != DataPermissionScope.Custom)
        {
            throw new InvalidOperationException("只有自定义数据权限范围角色才能维护数据范围。");
        }

        return role;
    }

    private async Task<SysDepartment> GetEnabledDepartmentOrThrowAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        if (department.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用部门不能绑定到角色数据范围。");
        }

        return department;
    }

    private async Task<SysRole> GetRoleForHierarchyOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "角色主键必须大于 0。");
        }

        return await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");
    }

    private async Task<SysRoleHierarchy> GetDirectHierarchyOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色继承主键必须大于 0。");
        }

        var hierarchy = await _roleHierarchyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色继承关系不存在。");

        if (hierarchy.Depth != 1)
        {
            throw new InvalidOperationException("只能删除直接继承关系。");
        }

        return hierarchy;
    }

    private readonly record struct HierarchyPair(long AncestorId, long DescendantId);
}
