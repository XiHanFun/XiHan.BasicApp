// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Organization;

/// <summary>
/// 组织目录（Organization 实现）。
/// </summary>
/// <remarks>
/// 别的包按编码取部门成员、部门负责人、岗位任职与部门链上的负责人，以及选人用的部门与岗位清单；
/// 读的是调用所在租户的组织数据，平台一侧没有组织，一律为空。不做权限与数据范围判定，候选人要不要再与可见的在用账号求交由调用方定。
/// </remarks>
public interface IOrganizationDirectory
{
    /// <summary>
    /// 部门成员。
    /// </summary>
    /// <remarks>
    /// 只取启用的部门、不含下级部门；按编码顺序、成员去重。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="departmentCodes">部门编码。</param>
    /// <returns>成员的用户 ID。</returns>
    Task<IReadOnlyList<Guid>> DepartmentMembersAsync(InvocationContext context, IReadOnlyCollection<string> departmentCodes);

    /// <summary>
    /// 部门负责人。
    /// </summary>
    /// <remarks>
    /// 只取启用且设了负责人的部门；按编码顺序、去重。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="departmentCodes">部门编码。</param>
    /// <returns>负责人的用户 ID。</returns>
    Task<IReadOnlyList<Guid>> DepartmentLeadersAsync(InvocationContext context, IReadOnlyCollection<string> departmentCodes);

    /// <summary>
    /// 岗位任职。
    /// </summary>
    /// <remarks>
    /// 只取启用的岗位与启用部门里的任职；按编码顺序、去重。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="positionCodes">岗位编码。</param>
    /// <returns>任职人的用户 ID。</returns>
    Task<IReadOnlyList<Guid>> PositionHoldersAsync(InvocationContext context, IReadOnlyCollection<string> positionCodes);

    /// <summary>
    /// 用户主部门往上的部门链。
    /// </summary>
    /// <remarks>
    /// 从主部门（没有标主部门时取最早加入的那个）起逐级到根，每级带启用状态与负责人；用户没有任职即为空。
    /// 「第 N 级主管」怎么数（跳过谁、去不去重）由调用方按自己的规则在这条链上算。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="userId">用户 ID。</param>
    /// <returns>由近到远的部门链。</returns>
    Task<IReadOnlyList<DepartmentLeaderStep>> LeaderPathAsync(InvocationContext context, Guid userId);

    /// <summary>
    /// 全部部门。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <returns>部门的平铺清单（含停用的），调用方按上级 ID 组树。</returns>
    Task<IReadOnlyList<DepartmentOption>> DepartmentsAsync(InvocationContext context);

    /// <summary>
    /// 启用的岗位。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <returns>按排序的岗位清单。</returns>
    Task<IReadOnlyList<PositionOption>> PositionsAsync(InvocationContext context);
}

/// <summary>
/// 部门链上的一级。
/// </summary>
/// <param name="DepartmentId">部门 ID。</param>
/// <param name="IsActive">部门是否启用。</param>
/// <param name="LeaderUserId">负责人的用户 ID，没设为空。</param>
public sealed record DepartmentLeaderStep(Guid DepartmentId, bool IsActive, Guid? LeaderUserId);

/// <summary>
/// 选人用的部门。
/// </summary>
/// <param name="Id">部门 ID。</param>
/// <param name="ParentId">上级部门 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="IsActive">是否启用。</param>
/// <param name="Order">排序。</param>
public sealed record DepartmentOption(Guid Id, Guid? ParentId, string Code, string Name, bool IsActive, int Order);

/// <summary>
/// 选人用的岗位。
/// </summary>
/// <param name="Id">岗位 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
public sealed record PositionOption(Guid Id, string Code, string Name);
