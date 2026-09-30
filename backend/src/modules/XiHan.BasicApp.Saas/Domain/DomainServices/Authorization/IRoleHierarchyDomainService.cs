// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色继承领域服务
/// </summary>
/// <remarks>
/// 职责：继承图读取、有效继承链展开、继承边维护及其校验（环路、重复、系统角色、职责分离复核）。
/// 继承语义见 <see cref="Entities.SysRoleHierarchy"/>。
/// </remarks>
public interface IRoleHierarchyDomainService
{
    /// <summary>
    /// 当前上下文的角色继承图（租户里含平台全局角色之间的边）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色继承图</returns>
    Task<RoleInheritanceGraph> GetGraphAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 给定角色的有效上级：经启用角色可达的上级及最短路径
    /// </summary>
    /// <remarks>不含角色自身，也不看角色自身的启停；停用的上级不计入，也切断经由它的继承。</remarks>
    /// <param name="roleIds">角色主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色主键 → （有效上级主键 → 最短路径）</returns>
    Task<IReadOnlyDictionary<long, IReadOnlyDictionary<long, RoleInheritancePath>>> GetEffectiveAncestorsAsync(
        IEnumerable<long> roleIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量变更角色的直接上级（一次提交新增与解除，先解除后新增）
    /// </summary>
    /// <param name="command">变更命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本次实际发生变化的直接上级</returns>
    Task<RoleHierarchyBatchUpdateResult> UpdateParentsAsync(
        RoleHierarchyBatchUpdateCommand command,
        CancellationToken cancellationToken = default);
}
