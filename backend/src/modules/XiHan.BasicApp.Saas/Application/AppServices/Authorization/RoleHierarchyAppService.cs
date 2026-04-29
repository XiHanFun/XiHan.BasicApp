#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleHierarchyAppService
// Guid:759a43f1-854c-49a3-bd41-c2f21ab566ae
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 角色继承命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色继承")]
public sealed class RoleHierarchyAppService(
    IRoleRepository roleRepository,
    IRoleHierarchyRepository roleHierarchyRepository)
    : SaasApplicationService, IRoleHierarchyAppService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 角色层级仓储
    /// </summary>
    private readonly IRoleHierarchyRepository _roleHierarchyRepository = roleHierarchyRepository;

    /// <summary>
    /// 创建角色直接继承关系
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色继承详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Create)]
    public async Task<RoleHierarchyDetailDto> CreateRoleHierarchyAsync(RoleHierarchyCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var ancestor = await GetRoleOrThrowAsync(input.AncestorId, cancellationToken);
        var descendant = await GetRoleOrThrowAsync(input.DescendantId, cancellationToken);
        EnsureDescendantCanBeMaintained(descendant);

        if (await _roleHierarchyRepository.AnyAsync(
            hierarchy => hierarchy.AncestorId == input.AncestorId && hierarchy.DescendantId == input.DescendantId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色继承关系已存在。");
        }

        if (await _roleHierarchyRepository.AnyAsync(
            hierarchy => hierarchy.AncestorId == input.DescendantId && hierarchy.DescendantId == input.AncestorId,
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
                    Remark = isDirectEdge ? NormalizeNullable(input.Remark) : null
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

        return RoleHierarchyApplicationMapper.ToDetailDto(directHierarchy, ancestor, descendant);
    }

    /// <summary>
    /// 删除角色直接继承关系
    /// </summary>
    /// <param name="id">角色继承主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Delete)]
    public async Task DeleteRoleHierarchyAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directHierarchy = await GetDirectHierarchyOrThrowAsync(id, cancellationToken);
        var descendant = await GetRoleOrThrowAsync(directHierarchy.DescendantId, cancellationToken);
        EnsureDescendantCanBeMaintained(descendant);

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

    /// <summary>
    /// 获取角色，不存在时抛出异常
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体</returns>
    private async Task<SysRole> GetRoleOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "角色主键必须大于 0。");
        }

        return await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");
    }

    /// <summary>
    /// 获取直接继承关系，不满足规则时抛出异常
    /// </summary>
    /// <param name="id">继承关系主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>直接继承关系</returns>
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

    /// <summary>
    /// 确保角色存在自身闭包记录
    /// </summary>
    /// <param name="role">角色实体</param>
    /// <param name="workingHierarchies">工作闭包集合</param>
    /// <param name="addList">待新增闭包集合</param>
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

    /// <summary>
    /// 校验创建参数
    /// </summary>
    /// <param name="input">创建参数</param>
    private static void ValidateCreateInput(RoleHierarchyCreateDto input)
    {
        if (input.AncestorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "祖先角色主键必须大于 0。");
        }

        if (input.DescendantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "后代角色主键必须大于 0。");
        }

        if (input.AncestorId == input.DescendantId)
        {
            throw new InvalidOperationException("角色不能继承自己。");
        }
    }

    /// <summary>
    /// 校验后代角色是否允许通过本服务维护继承关系
    /// </summary>
    /// <param name="descendant">后代角色</param>
    private static void EnsureDescendantCanBeMaintained(SysRole descendant)
    {
        if (descendant.IsGlobal || descendant.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色必须通过平台运维流程维护继承关系。");
        }
    }

    /// <summary>
    /// 生成合并后的继承路径
    /// </summary>
    /// <param name="ancestorClosure">祖先侧闭包</param>
    /// <param name="descendantClosure">后代侧闭包</param>
    /// <returns>继承路径</returns>
    private static string BuildCombinedPath(SysRoleHierarchy ancestorClosure, SysRoleHierarchy descendantClosure)
    {
        var pathIds = new List<long>(BuildPathIds(ancestorClosure));
        pathIds.AddRange(BuildPathIds(descendantClosure));
        return string.Join("/", pathIds);
    }

    /// <summary>
    /// 解析闭包路径
    /// </summary>
    /// <param name="hierarchy">闭包记录</param>
    /// <returns>路径主键集合</returns>
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

    /// <summary>
    /// 根据直接继承边计算可达闭包键
    /// </summary>
    /// <param name="directEdges">直接继承边集合</param>
    /// <returns>可达闭包键集合</returns>
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

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>规范化后的字符串</returns>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private readonly record struct HierarchyPair(long AncestorId, long DescendantId);
}
