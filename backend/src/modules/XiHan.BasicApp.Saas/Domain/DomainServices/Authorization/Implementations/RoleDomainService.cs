// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;

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

    private readonly ICurrentTenant _currentTenant;

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
        IDepartmentRepository departmentRepository,
        ICurrentTenant currentTenant)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
        _permissionRepository = permissionRepository;
        _departmentRepository = departmentRepository;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    public async Task<RoleCommandResult> CreateRoleAsync(RoleCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);

        var roleCode = command.RoleCode.Trim();
        if (await _roleRepository.GetByCodeAsync(roleCode, cancellationToken) is not null)
        {
            throw new UserFriendlyException(new ResourceLocalizableString("Errors", "Authorization.Role.CodeAlreadyExists"), "角色编码已存在。");
        }

        var role = new SysRole
        {
            RoleCode = roleCode,
            RoleName = command.RoleName.Trim(),
            RoleDescription = NormalizeNullable(command.RoleDescription),
            RoleType = command.RoleType,
            MaxMembers = command.MaxMembers,
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedRole = await _roleRepository.AddAsync(role, cancellationToken);
        return new RoleCommandResult(savedRole);
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    public async Task<RoleCommandResult> UpdateRoleAsync(RoleUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);

        var role = await GetEditableRoleOrThrowAsync(command.BasicId, cancellationToken);
        role.RoleName = command.RoleName.Trim();
        role.RoleDescription = NormalizeNullable(command.RoleDescription);
        role.RoleType = command.RoleType;
        role.MaxMembers = command.MaxMembers;
        role.Sort = command.Sort;
        role.Remark = NormalizeNullable(command.Remark);

        var savedRole = await _roleRepository.UpdateAsync(role, cancellationToken);
        return new RoleCommandResult(savedRole);
    }

    /// <summary>
    /// 更新角色状态
    /// </summary>
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

    /// <summary>
    /// 删除角色
    /// </summary>
    public async Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await GetEditableRoleOrThrowAsync(id, cancellationToken);
        await EnsureRoleNotReferencedAsync(role, cancellationToken);

        if (!await _roleRepository.DeleteAsync(role, cancellationToken))
        {
            throw new InvalidOperationException("角色删除失败。");
        }
    }

    /// <summary>
    /// 批量变更角色权限（批量撤销 + 批量授予，底层走 UpdateRange/AddRange 单次提交）
    /// </summary>
    /// <returns>本次实际发生变化的授予/撤销权限ID（供审计发事件）</returns>
    public async Task<RolePermissionBatchUpdateResult> BatchUpdateRolePermissionsAsync(RolePermissionBatchUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        var grantPermissionIds = command.GrantPermissionIds.Where(id => id > 0).Distinct().ToList();
        var revokeRolePermissionIds = command.RevokeRolePermissionIds.Where(id => id > 0).Distinct().ToList();
        if (grantPermissionIds.Count == 0 && revokeRolePermissionIds.Count == 0)
        {
            return new RolePermissionBatchUpdateResult([], []);
        }

        // 本次实际发生变化的权限（供审计发事件）
        var grantedPermissionIds = new List<long>();
        var revokedPermissionIds = new List<long>();

        // 角色启用校验（一次）
        _ = await GetEnabledRoleForPermissionOrThrowAsync(command.RoleId, cancellationToken);

        // 撤销（软删除）：批量加载 → 置为无效 → 批量更新（单次）
        if (revokeRolePermissionIds.Count > 0)
        {
            var revoking = (await _rolePermissionRepository.GetListAsync(
                rolePermission => revokeRolePermissionIds.Contains(rolePermission.BasicId) && rolePermission.RoleId == command.RoleId,
                cancellationToken)).ToList();
            if (revoking.Count > 0)
            {
                foreach (var rolePermission in revoking)
                {
                    rolePermission.Status = ValidityStatus.Invalid;
                }

                _ = await _rolePermissionRepository.UpdateRangeAsync(revoking, cancellationToken);
                revokedPermissionIds.AddRange(revoking.Select(rolePermission => rolePermission.PermissionId));
            }
        }

        // 授予：批量校验权限启用 + 去重已绑定 → 批量插入（单次）
        if (grantPermissionIds.Count > 0)
        {
            var permissions = await _permissionRepository.GetListAsync(
                permission => grantPermissionIds.Contains(permission.BasicId), cancellationToken);
            var permissionMap = permissions.ToDictionary(permission => permission.BasicId);
            foreach (var permissionId in grantPermissionIds)
            {
                if (!permissionMap.TryGetValue(permissionId, out var permission))
                {
                    throw new InvalidOperationException("权限不存在。");
                }

                if (permission.Status != EnableStatus.Enabled)
                {
                    throw new InvalidOperationException("停用权限不能绑定到角色。");
                }
            }

            // 已存在的绑定（任意状态）：此刻不生效的（已撤销或已过期）就地复用，避免「撤销后无法再次授予」。
            // 复用即「从现在起生效的授予」：动作改回授予、时间窗清空——只改状态的话，过期行保存后仍不生效，
            // 历史上的拒绝行也会被原样复活成拒绝
            var now = DateTimeOffset.UtcNow;
            var existing = await _rolePermissionRepository.GetListAsync(
                rolePermission => rolePermission.RoleId == command.RoleId && grantPermissionIds.Contains(rolePermission.PermissionId),
                cancellationToken);
            var existingPermissionIds = existing.Select(rolePermission => rolePermission.PermissionId).ToHashSet();

            var reactivating = existing
                .Where(rolePermission => !IsEffective(rolePermission.Status, rolePermission.EffectiveTime, rolePermission.ExpirationTime, now))
                .ToList();
            if (reactivating.Count > 0)
            {
                foreach (var rolePermission in reactivating)
                {
                    rolePermission.PermissionAction = PermissionAction.Grant;
                    rolePermission.EffectiveTime = null;
                    rolePermission.ExpirationTime = null;
                    rolePermission.Status = ValidityStatus.Valid;
                }

                _ = await _rolePermissionRepository.UpdateRangeAsync(reactivating, cancellationToken);
                grantedPermissionIds.AddRange(reactivating.Select(rolePermission => rolePermission.PermissionId));
            }

            // 从未绑定过的新增（单次批量插入）
            var newBindings = grantPermissionIds
                .Where(permissionId => !existingPermissionIds.Contains(permissionId))
                .Select(permissionId => new SysRolePermission
                {
                    RoleId = command.RoleId,
                    PermissionId = permissionId,
                    PermissionAction = PermissionAction.Grant,
                    Status = ValidityStatus.Valid
                })
                .ToList();

            if (newBindings.Count > 0)
            {
                _ = await _rolePermissionRepository.AddRangeAsync(newBindings, cancellationToken);
                grantedPermissionIds.AddRange(newBindings.Select(rolePermission => rolePermission.PermissionId));
            }
        }

        return new RolePermissionBatchUpdateResult(grantedPermissionIds, revokedPermissionIds);
    }

    /// <summary>
    /// 更新角色权限
    /// </summary>
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

    /// <summary>
    /// 更新角色权限状态
    /// </summary>
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

    /// <summary>
    /// 设置角色数据范围：档位与自定义部门一次落地
    /// </summary>
    /// <remarks>
    /// 全局角色是各租户共用的模板，只在平台设档位；部门是租户自己的数据，全局角色不能用自定义档位。
    /// 部门明细与目标比出差量：新部门授予；改了含下级或此刻不生效的历史行就地复用、从现在起生效；
    /// 目标之外仍有效的撤销（只置无效不删行）。档位不是自定义时，已有的部门明细全部撤销。
    /// </remarks>
    /// <returns>档位是否改变、本次实际变化的部门</returns>
    public async Task<DataScopeSetResult> SetRoleDataScopeAsync(RoleDataScopeSetCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateEnum(command.DataScope, nameof(command.DataScope));
        var departments = DataScopeDepartments.Normalize(command.DataScope, command.Departments);

        var role = await GetEditableRoleOrThrowAsync(command.RoleId, cancellationToken);
        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能维护数据范围。");
        }

        if (role.IsGlobal && command.DataScope == DataPermissionScope.Custom)
        {
            throw new InvalidOperationException("全局角色是各租户共用的模板，部门是租户自己的数据，不能设为自定义数据范围。");
        }

        foreach (var departmentId in departments.Keys)
        {
            _ = await GetEnabledDepartmentOrThrowAsync(departmentId, cancellationToken);
        }

        // 同一 角色×部门 只有一行：撤销只置无效，历史行命中即就地复用
        var rows = (await _roleDataScopeRepository.GetListAsync(scope => scope.RoleId == role.BasicId, cancellationToken))
            .ToDictionary(scope => scope.DepartmentId);
        var now = DateTimeOffset.UtcNow;
        var updating = new List<SysRoleDataScope>();
        var adding = new List<SysRoleDataScope>();
        var grantedDepartmentIds = new List<long>();
        foreach (var (departmentId, includeChildren) in departments)
        {
            if (!rows.TryGetValue(departmentId, out var dataScope))
            {
                adding.Add(new SysRoleDataScope
                {
                    RoleId = role.BasicId,
                    DepartmentId = departmentId,
                    IncludeChildren = includeChildren,
                    Status = ValidityStatus.Valid
                });
                grantedDepartmentIds.Add(departmentId);
                continue;
            }

            var effective = IsEffective(dataScope.Status, dataScope.EffectiveTime, dataScope.ExpirationTime, now);
            if (effective && dataScope.IncludeChildren == includeChildren)
            {
                continue;
            }

            // 此刻不生效的历史行按「从现在起生效」复用：只改状态不动时间窗，保存后仍不生效
            if (!effective)
            {
                dataScope.EffectiveTime = null;
                dataScope.ExpirationTime = null;
            }

            dataScope.IncludeChildren = includeChildren;
            dataScope.Status = ValidityStatus.Valid;
            updating.Add(dataScope);
            grantedDepartmentIds.Add(departmentId);
        }

        var revoking = rows.Values
            .Where(scope => scope.Status == ValidityStatus.Valid && !departments.ContainsKey(scope.DepartmentId))
            .ToList();
        foreach (var dataScope in revoking)
        {
            dataScope.Status = ValidityStatus.Invalid;
        }

        if (revoking.Count > 0 || updating.Count > 0)
        {
            _ = await _roleDataScopeRepository.UpdateRangeAsync([.. revoking, .. updating], cancellationToken);
        }

        if (adding.Count > 0)
        {
            _ = await _roleDataScopeRepository.AddRangeAsync(adding, cancellationToken);
        }

        var scopeChanged = role.DataScope != command.DataScope;
        if (scopeChanged)
        {
            role.DataScope = command.DataScope;
            _ = await _roleRepository.UpdateAsync(role, cancellationToken);
        }

        return new DataScopeSetResult(scopeChanged, grantedDepartmentIds, [.. revoking.Select(scope => scope.DepartmentId)]);
    }

    /// <summary>
    /// 批量变更角色的直接父角色（一次性提交新增与移除，先移除后新增）
    /// </summary>
    /// <remarks>
    /// 继承关系以闭包表存储，整表读出后在内存里先摘边、再加边，最后一次性删除与写入闭包行。
    /// 先摘后加：本次一并移除的旧路径不会挡住新增，一并移除的多条父边也不会互为替代路径
    /// </remarks>
    /// <returns>本次实际发生变化的直接父角色</returns>
    public async Task<RoleHierarchyBatchUpdateResult> BatchUpdateRoleParentsAsync(RoleHierarchyBatchUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        var addParentIds = command.AddParentRoleIds.Where(id => id > 0).Distinct().ToList();
        // 同一父角色本次既加又移时以新增为准
        var removeParentIds = command.RemoveParentRoleIds
            .Where(id => id > 0 && !addParentIds.Contains(id))
            .ToHashSet();
        if (addParentIds.Count == 0 && removeParentIds.Count == 0)
        {
            return new RoleHierarchyBatchUpdateResult([], []);
        }

        if (addParentIds.Contains(command.RoleId))
        {
            throw new InvalidOperationException("角色不能继承自己。");
        }

        var role = await GetRoleForHierarchyOrThrowAsync(command.RoleId, cancellationToken);
        EnsureDescendantCanBeMaintainedForHierarchy(role);

        var workingHierarchies = (await _roleHierarchyRepository.GetAllAsync(cancellationToken)).ToList();

        // 移除只认本角色现有的直接父边
        var removedParentIds = workingHierarchies
            .Where(hierarchy => hierarchy.Depth == 1
                && hierarchy.DescendantId == role.BasicId
                && removeParentIds.Contains(hierarchy.AncestorId))
            .Select(hierarchy => hierarchy.AncestorId)
            .Distinct()
            .ToList();
        var deleting = removedParentIds.Count == 0
            ? []
            : RemoveDirectEdges([.. removedParentIds.Select(parentId => new HierarchyPair(parentId, role.BasicId))], workingHierarchies);

        // 已是直接父角色的视为已达成
        var adding = new List<SysRoleHierarchy>();
        var addedParentIds = new List<long>();
        foreach (var parentId in addParentIds)
        {
            if (workingHierarchies.Any(hierarchy => hierarchy.Depth == 1 && hierarchy.AncestorId == parentId && hierarchy.DescendantId == role.BasicId))
            {
                continue;
            }

            var parent = await GetRoleForHierarchyOrThrowAsync(parentId, cancellationToken);
            AddDirectEdge(parent, role, workingHierarchies, adding);
            addedParentIds.Add(parentId);
        }

        // 摘掉又被新路径补回的闭包行照删照加，深度与路径按新路径重算
        if (deleting.Count > 0)
        {
            var deletingIds = deleting.Select(hierarchy => hierarchy.BasicId).ToList();
            _ = await _roleHierarchyRepository.DeleteAsync(hierarchy => deletingIds.Contains(hierarchy.BasicId), cancellationToken);
        }

        if (adding.Count > 0)
        {
            _ = await _roleHierarchyRepository.AddRangeAsync(adding, cancellationToken);
        }

        return new RoleHierarchyBatchUpdateResult(addedParentIds, removedParentIds);
    }

    /// <summary>
    /// 在工作集中加一条直接继承边并补齐闭包，新生成的闭包行追加到 addList
    /// </summary>
    private static void AddDirectEdge(SysRole ancestor, SysRole descendant, List<SysRoleHierarchy> workingHierarchies, List<SysRoleHierarchy> addList)
    {
        if (workingHierarchies.Any(hierarchy => hierarchy.AncestorId == ancestor.BasicId && hierarchy.DescendantId == descendant.BasicId))
        {
            throw new InvalidOperationException("角色已间接继承该父角色，无需再直接继承。");
        }

        if (workingHierarchies.Any(hierarchy => hierarchy.AncestorId == descendant.BasicId && hierarchy.DescendantId == ancestor.BasicId))
        {
            throw new InvalidOperationException("角色继承关系会形成环路。");
        }

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
                if (!existingPairs.Add(pair))
                {
                    continue;
                }

                var hierarchy = new SysRoleHierarchy
                {
                    AncestorId = pair.AncestorId,
                    DescendantId = pair.DescendantId,
                    Depth = ancestorClosure.Depth + 1 + descendantClosure.Depth,
                    Path = BuildCombinedPath(ancestorClosure, descendantClosure)
                };

                addList.Add(hierarchy);
                workingHierarchies.Add(hierarchy);
            }
        }
    }

    /// <summary>
    /// 从工作集中摘掉一组直接继承边，连带摘掉只经由它们可达的闭包行，返回被摘掉的行
    /// </summary>
    private static List<SysRoleHierarchy> RemoveDirectEdges(IReadOnlyCollection<HierarchyPair> directPairs, List<SysRoleHierarchy> workingHierarchies)
    {
        var remainingDirectEdges = workingHierarchies
            .Where(hierarchy => hierarchy.Depth == 1 && !directPairs.Contains(new HierarchyPair(hierarchy.AncestorId, hierarchy.DescendantId)))
            .ToArray();
        var remainingPairs = BuildReachablePairs(remainingDirectEdges);
        if (directPairs.Any(remainingPairs.Contains))
        {
            throw new InvalidOperationException("该直接继承关系存在替代路径，需先清理替代路径后再删除。");
        }

        var impactedAncestorIds = workingHierarchies
            .Where(hierarchy => directPairs.Any(pair => pair.AncestorId == hierarchy.DescendantId))
            .Select(hierarchy => hierarchy.AncestorId)
            .Concat(directPairs.Select(pair => pair.AncestorId))
            .ToHashSet();
        var impactedDescendantIds = workingHierarchies
            .Where(hierarchy => directPairs.Any(pair => pair.DescendantId == hierarchy.AncestorId))
            .Select(hierarchy => hierarchy.DescendantId)
            .Concat(directPairs.Select(pair => pair.DescendantId))
            .ToHashSet();
        var removing = workingHierarchies
            .Where(hierarchy => hierarchy.Depth > 0
                && impactedAncestorIds.Contains(hierarchy.AncestorId)
                && impactedDescendantIds.Contains(hierarchy.DescendantId)
                && !remainingPairs.Contains(new HierarchyPair(hierarchy.AncestorId, hierarchy.DescendantId)))
            .ToList();

        var removingSet = new HashSet<SysRoleHierarchy>(removing, ReferenceEqualityComparer.Instance);
        _ = workingHierarchies.RemoveAll(removingSet.Contains);
        return removing;
    }

    private static void ValidateCreateCommand(RoleCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RoleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RoleName);
        ValidateCommonCommand(command.RoleType, command.MaxMembers);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateUpdateCommand(RoleUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(command.RoleName);
        ValidateCommonCommand(command.RoleType, command.MaxMembers);
    }

    private static void ValidateCommonCommand(RoleType roleType, int maxMembers)
    {
        ValidateEnum(roleType, nameof(roleType));

        if (roleType == RoleType.System)
        {
            throw new InvalidOperationException("系统角色必须通过平台种子或运维流程维护。");
        }

        if (maxMembers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMembers), "最大成员数不能小于 0。");
        }
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

    /// <summary>
    /// 授权记录此刻是否生效：状态有效且落在生效 / 失效时间之间（与仓储取有效授权同一口径）
    /// </summary>
    private static bool IsEffective(ValidityStatus status, DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime, DateTimeOffset now)
    {
        return status == ValidityStatus.Valid
            && (effectiveTime is null || effectiveTime <= now)
            && (expirationTime is null || expirationTime > now);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void EnsureRoleCanBeMaintained(SysRole role)
    {
        if ((role.IsGlobal || role.RoleType == RoleType.System) && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台全局角色或系统角色仅平台运维态可维护，请切换到平台运维后操作。");
        }
    }

    private void EnsureDescendantCanBeMaintainedForHierarchy(SysRole descendant)
    {
        if ((descendant.IsGlobal || descendant.RoleType == RoleType.System) && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台全局角色或系统角色仅平台运维态可维护继承关系，请切换到平台运维后操作。");
        }
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

    /// <summary>
    /// 校验角色没有被引用：全局角色被各租户分配、继承，平台删除前要看所有租户，不能只看平台这一侧
    /// </summary>
    private async Task EnsureRoleNotReferencedAsync(SysRole role, CancellationToken cancellationToken)
    {
        var roleId = role.BasicId;
        var acrossTenants = role.IsGlobal;

        if (acrossTenants
                ? await _userRoleRepository.AnyIgnoreTenantAsync(userRole => userRole.RoleId == roleId, cancellationToken)
                : await _userRoleRepository.AnyAsync(userRole => userRole.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException(acrossTenants ? "全局角色已分配给租户成员，不能删除。" : "角色已分配给用户，不能删除。");
        }

        if (await _rolePermissionRepository.AnyAsync(rolePermission => rolePermission.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("角色已绑定权限，不能删除。");
        }

        if (acrossTenants
                ? await _roleHierarchyRepository.AnyIgnoreTenantAsync(
                    hierarchy => hierarchy.Depth > 0 && (hierarchy.AncestorId == roleId || hierarchy.DescendantId == roleId),
                    cancellationToken)
                : await _roleHierarchyRepository.AnyAsync(
                    hierarchy => hierarchy.Depth > 0 && (hierarchy.AncestorId == roleId || hierarchy.DescendantId == roleId),
                    cancellationToken))
        {
            throw new InvalidOperationException(acrossTenants ? "全局角色被租户角色继承，不能删除。" : "角色存在继承关系，不能删除。");
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

        // 全局角色是各租户共用的模板：租户不能往上叠加自己的授权，要改就新建租户角色
        EnsureRoleCanBeMaintained(role);

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

    private readonly record struct HierarchyPair(long AncestorId, long DescendantId);
}
