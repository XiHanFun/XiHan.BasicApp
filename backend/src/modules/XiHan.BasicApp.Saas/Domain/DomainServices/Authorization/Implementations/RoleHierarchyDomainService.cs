// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色继承领域服务实现
/// </summary>
public sealed class RoleHierarchyDomainService
    : IRoleHierarchyDomainService
{
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    private readonly IConstraintRuleEnforcementDomainService _constraintRuleEnforcementDomainService;

    private readonly ICurrentTenant _currentTenant;

    private readonly ILogger<RoleHierarchyDomainService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleHierarchyDomainService(
        IRoleHierarchyRepository roleHierarchyRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IConstraintRuleEnforcementDomainService constraintRuleEnforcementDomainService,
        ICurrentTenant currentTenant,
        ILogger<RoleHierarchyDomainService> logger)
    {
        _roleHierarchyRepository = roleHierarchyRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _constraintRuleEnforcementDomainService = constraintRuleEnforcementDomainService;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    /// <summary>
    /// 当前上下文的角色继承图
    /// </summary>
    public async Task<RoleInheritanceGraph> GetGraphAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return RoleInheritanceGraph.FromEdges(await _roleHierarchyRepository.GetEdgesAsync(cancellationToken));
    }

    /// <summary>
    /// 给定角色的有效上级：经启用角色可达的上级及最短路径
    /// </summary>
    public async Task<IReadOnlyDictionary<long, IReadOnlyDictionary<long, RoleInheritancePath>>> GetEffectiveAncestorsAsync(
        IEnumerable<long> roleIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        cancellationToken.ThrowIfCancellationRequested();

        var ids = roleIds.Where(id => id > 0).Distinct().ToList();
        var result = new Dictionary<long, IReadOnlyDictionary<long, RoleInheritancePath>>(ids.Count);
        if (ids.Count == 0)
        {
            return result;
        }

        var graph = await GetGraphAsync(cancellationToken);
        var candidateIds = ids.SelectMany(id => graph.AncestorsOf(id).Keys).ToHashSet();
        var enabledIds = candidateIds.Count == 0
            ? []
            : (await _roleRepository.GetEnabledByIdsAsync(candidateIds, cancellationToken)).Select(role => role.BasicId).ToHashSet();

        foreach (var id in ids)
        {
            result[id] = graph.AncestorsOf(id, enabledIds.Contains);
        }

        return result;
    }

    /// <summary>
    /// 批量变更角色的直接上级（一次提交新增与解除，先解除后新增）
    /// </summary>
    /// <remarks>
    /// 先解除：本次一并解除的上级不会挡住新增（如把上级从 A 换成 A 的上级）。
    /// 写入后按新的继承关系复核职责分离，阻断类违规抛出，由工作单元整体回滚。
    /// </remarks>
    public async Task<RoleHierarchyBatchUpdateResult> UpdateParentsAsync(
        RoleHierarchyBatchUpdateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        var addParentIds = command.AddParentRoleIds.Where(id => id > 0).Distinct().ToList();
        // 同一上级本次既加又解时以新增为准
        var removeParentIds = command.RemoveParentRoleIds
            .Where(id => id > 0 && !addParentIds.Contains(id))
            .Distinct()
            .ToList();
        if (addParentIds.Count == 0 && removeParentIds.Count == 0)
        {
            return new RoleHierarchyBatchUpdateResult([], []);
        }

        if (addParentIds.Contains(command.RoleId))
        {
            throw new InvalidOperationException("角色不能继承自己。");
        }

        var role = await GetRoleOrThrowAsync(command.RoleId, "角色不存在。", cancellationToken);
        EnsureParticipatesInInheritance(role);
        if (role.IsGlobal && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台全局角色的继承关系仅平台运维态可维护，请切换到平台运维后操作。");
        }

        var edges = await _roleHierarchyRepository.GetEdgesAsync(cancellationToken);
        var graph = RoleInheritanceGraph.FromEdges(edges);

        var removedParentIds = removeParentIds.Where(parentId => graph.HasEdge(parentId, role.BasicId)).ToList();
        foreach (var parentId in removedParentIds)
        {
            graph.RemoveEdge(parentId, role.BasicId);
        }

        var addedParentIds = new List<long>();
        foreach (var parentId in addParentIds)
        {
            // 已是直接上级的视为已达成
            if (graph.HasEdge(parentId, role.BasicId))
            {
                continue;
            }

            var parent = await GetRoleOrThrowAsync(parentId, "上级角色不存在。", cancellationToken);
            EnsureCanBeParent(role, parent, graph);
            graph.AddEdge(parent.BasicId, role.BasicId);
            addedParentIds.Add(parent.BasicId);
        }

        if (addedParentIds.Count == 0 && removedParentIds.Count == 0)
        {
            return new RoleHierarchyBatchUpdateResult([], []);
        }

        if (removedParentIds.Count > 0)
        {
            // 下级角色的上级边与下级同属一个作用域：租户角色的边在本租户，全局角色的边在平台
            var removingRowIds = edges
                .Where(edge => edge.DescendantId == role.BasicId && removedParentIds.Contains(edge.AncestorId))
                .Select(edge => edge.BasicId)
                .ToList();
            _ = await _roleHierarchyRepository.DeleteAsync(edge => removingRowIds.Contains(edge.BasicId), cancellationToken);
        }

        if (addedParentIds.Count > 0)
        {
            _ = await _roleHierarchyRepository.AddRangeAsync(
                addedParentIds.Select(parentId => new SysRoleHierarchy { AncestorId = parentId, DescendantId = role.BasicId }),
                cancellationToken);
        }

        await EnsureSeparationOfDutyAsync(role.BasicId, cancellationToken);
        if (role.IsGlobal)
        {
            await EnsureSeparationOfDutyInTenantsAsync(role.BasicId, graph, cancellationToken);
        }

        return new RoleHierarchyBatchUpdateResult(addedParentIds, removedParentIds);
    }

    /// <summary>
    /// 系统角色的权限由授权快照按上下文整体给出，继承它得不到任何权限，给它设上级也没有意义
    /// </summary>
    private static void EnsureParticipatesInInheritance(SysRole role)
    {
        if (role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException($"系统角色「{role.RoleName}」不参与角色继承。");
        }
    }

    /// <summary>
    /// 新上级的校验：参与继承、已启用、作用范围相容、不成环、不重复
    /// </summary>
    private static void EnsureCanBeParent(SysRole role, SysRole parent, RoleInheritanceGraph graph)
    {
        EnsureParticipatesInInheritance(parent);

        if (parent.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException($"停用角色「{parent.RoleName}」不能设为上级。");
        }

        // 全局角色是各租户共用的模板，只能由全局角色组成继承链
        if (role.IsGlobal && !parent.IsGlobal)
        {
            throw new InvalidOperationException("平台全局角色只能继承全局角色。");
        }

        if (graph.DescendantsOf(role.BasicId).ContainsKey(parent.BasicId))
        {
            throw new InvalidOperationException($"「{parent.RoleName}」已继承本角色，再设为上级会形成环路。");
        }

        if (graph.AncestorsOf(role.BasicId).ContainsKey(parent.BasicId))
        {
            throw new InvalidOperationException($"本角色已经通过其他上级间接继承「{parent.RoleName}」，无需再直接继承。");
        }
    }

    /// <summary>
    /// 按当前上下文的继承关系复核静态职责分离：受影响角色（本角色及其下级）各自的继承链，以及持有它们的成员的全部角色
    /// </summary>
    /// <remarks>
    /// 继承边已写入本事务，约束评估读到的就是变更后的继承关系；拒绝 / 需审批类违规抛出，警告 / 记录日志类放行留痕。
    /// </remarks>
    private async Task EnsureSeparationOfDutyAsync(long roleId, CancellationToken cancellationToken)
    {
        var graph = await GetGraphAsync(cancellationToken);
        var affectedRoleIds = graph.DescendantsOf(roleId).Keys.Append(roleId).ToList();
        var scopeTenantId = _currentTenant.Id ?? 0;
        var now = DateTimeOffset.UtcNow;

        // 授权绑定只在所属上下文生效，与授权快照同一口径
        var holderIds = (await _userRoleRepository.GetValidByRoleIdsAsync(affectedRoleIds, now, cancellationToken))
            .Where(binding => binding.TenantId == scopeTenantId)
            .Select(binding => binding.UserId)
            .Distinct()
            .ToList();
        var holderRoleSets = holderIds.Count == 0
            ? []
            : (await _userRoleRepository.GetValidByUserIdsAsync(holderIds, now, cancellationToken))
                .Where(binding => binding.TenantId == scopeTenantId)
                .GroupBy(binding => binding.UserId)
                .Select(group => (UserId: (long?)group.Key, RoleIds: (IReadOnlyCollection<long>)[.. group.Select(binding => binding.RoleId).Distinct().Order()]))
                .ToList();

        var subjects = affectedRoleIds
            .Select(id => (UserId: (long?)null, RoleIds: (IReadOnlyCollection<long>)[id]))
            .Concat(holderRoleSets)
            .ToList();
        var results = await _constraintRuleEnforcementDomainService.EvaluateRoleSetsAsync(
            [.. subjects.Select(subject => subject.RoleIds)],
            ConstraintType.SSD,
            cancellationToken);

        for (var index = 0; index < subjects.Count; index++)
        {
            var result = results[index];
            var blocking = result.FirstBlockingViolation;
            if (blocking is not null)
            {
                var conflict = string.Join(",", blocking.MatchedTargetIds);
                if (subjects[index].UserId is { } userId)
                {
                    throw new InvalidOperationException(
                        $"调整后成员（用户主键 {userId}）的角色违反职责分离约束规则[{blocking.RuleCode}]《{blocking.RuleName}》（冲突角色主键：{conflict}）。");
                }

                var subjectRoleId = subjects[index].RoleIds.First();
                var subjectRole = await _roleRepository.GetByIdAsync(subjectRoleId, cancellationToken);
                throw new InvalidOperationException(
                    $"调整后角色「{subjectRole?.RoleName ?? subjectRoleId.ToString()}」的继承链违反职责分离约束规则[{blocking.RuleCode}]《{blocking.RuleName}》（冲突角色主键：{conflict}）。");
            }

            foreach (var violation in result.Violations)
            {
                _logger.LogWarning(
                    "角色继承调整命中职责分离约束规则[{RuleCode}]《{RuleName}》，按 {ViolationAction} 处理放行。",
                    violation.RuleCode,
                    violation.RuleName,
                    violation.ViolationAction);
            }
        }
    }

    /// <summary>
    /// 全局角色的继承随读取即时作用到各租户：继承了它或持有它的租户逐个切入复核
    /// </summary>
    private async Task EnsureSeparationOfDutyInTenantsAsync(long roleId, RoleInheritanceGraph platformGraph, CancellationToken cancellationToken)
    {
        var affectedRoleIds = platformGraph.DescendantsOf(roleId).Keys.Append(roleId).ToList();
        var tenantIds = (await _roleHierarchyRepository.GetTenantIdsInheritingAsync(affectedRoleIds, cancellationToken))
            .Concat(await _userRoleRepository.GetValidTenantIdsByRoleIdsIgnoreTenantAsync(affectedRoleIds, DateTimeOffset.UtcNow, cancellationToken))
            .Distinct()
            .Order()
            .ToList();

        foreach (var tenantId in tenantIds)
        {
            using (_currentTenant.Change(tenantId))
            {
                await EnsureSeparationOfDutyAsync(roleId, cancellationToken);
            }
        }
    }

    private async Task<SysRole> GetRoleOrThrowAsync(long roleId, string notFoundMessage, CancellationToken cancellationToken)
    {
        return await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException(notFoundMessage);
    }
}
