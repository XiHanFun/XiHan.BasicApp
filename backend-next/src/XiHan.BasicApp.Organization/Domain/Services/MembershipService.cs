// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Operations.Memberships;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Domain.Services;

/// <summary>
/// 归属域服务（作用域）：核对用户注册地、主部门唯一、把归属行配上用户 / 部门 / 岗位名称（用户在平台库、部门与岗位在租户库，分库时不能联表，按主键批量取）。
/// </summary>
/// <param name="users">用户仓储（平台库）。</param>
/// <param name="departments">部门仓储。</param>
/// <param name="positions">岗位仓储。</param>
/// <param name="memberships">归属仓储。</param>
public sealed class MembershipService(
    IRepository<SysUser, Guid> users,
    IRepository<SysDepartment, Guid> departments,
    IRepository<SysPosition, Guid> positions,
    IRepository<SysUserDepartment, Guid> memberships)
{
    /// <summary>
    /// 注册地是 <paramref name="tenantId"/> 的用户；别的租户或平台的用户视为不存在。
    /// </summary>
    public async Task<Maybe<SysUser>> FindUserInTenantAsync(Guid userId, string tenantId, CancellationToken cancellationToken)
    {
        var found = await users.FindAsync(userId, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var user) && string.Equals(user.HomeTenantId, tenantId, StringComparison.Ordinal) ? Maybe.Some(user) : Maybe.None<SysUser>();
    }

    /// <summary>
    /// 用户在某部门的归属行。
    /// </summary>
    public async Task<Maybe<SysUserDepartment>> FindAsync(Guid userId, Guid departmentId, CancellationToken cancellationToken)
    {
        var rows = await memberships.ListAsync(EntityQuery.All<SysUserDepartment>().Where(new Specification<SysUserDepartment>(m => m.UserId == userId && m.DepartmentId == departmentId)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysUserDepartment>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 用户的全部归属行。
    /// </summary>
    public Task<IReadOnlyList<SysUserDepartment>> OfUserAsync(Guid userId, CancellationToken cancellationToken)
        => memberships.ListAsync(EntityQuery.All<SysUserDepartment>().Where(new Specification<SysUserDepartment>(m => m.UserId == userId)), cancellationToken);

    /// <summary>
    /// 把 <paramref name="primary"/> 设为该用户唯一的主部门：其他归属行的主标记清掉。
    /// </summary>
    public async Task<Result> MakePrimaryAsync(SysUserDepartment primary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(primary);
        var others = (await OfUserAsync(primary.UserId, cancellationToken).ConfigureAwait(false)).Where(m => m.Id != primary.Id && m.IsPrimary).ToList();
        foreach (var other in others)
        {
            other.IsPrimary = false;
        }

        if (others.Count > 0)
        {
            foreach (var outcome in await memberships.UpdateRangeAsync(others, cancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome;
                }
            }
        }

        primary.IsPrimary = true;
        return Result.Success();
    }

    /// <summary>
    /// 把归属行配上名称；用户、部门或岗位已不存在时名称为空串，不丢行。
    /// </summary>
    public async Task<IReadOnlyList<MembershipResponse>> ComposeAsync(IReadOnlyList<SysUserDepartment> rows, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            return [];
        }

        var userMap = (await users.FindManyAsync(rows.Select(static m => m.UserId).Distinct().ToList(), cancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id);
        var departmentMap = (await departments.FindManyAsync(rows.Select(static m => m.DepartmentId).Distinct().ToList(), cancellationToken).ConfigureAwait(false)).ToDictionary(static d => d.Id);
        var positionIds = rows.Where(static m => m.PositionId.HasValue).Select(static m => m.PositionId!.Value).Distinct().ToList();
        var positionMap = positionIds.Count == 0
            ? new Dictionary<Guid, SysPosition>()
            : (await positions.FindManyAsync(positionIds, cancellationToken).ConfigureAwait(false)).ToDictionary(static p => p.Id);
        return rows.Select(m =>
        {
            var user = userMap.GetValueOrDefault(m.UserId);
            var position = m.PositionId is { } positionId ? positionMap.GetValueOrDefault(positionId) : null;
            return new MembershipResponse(
                m.Id,
                m.UserId,
                user?.UserName ?? string.Empty,
                user?.DisplayName ?? string.Empty,
                m.DepartmentId,
                departmentMap.GetValueOrDefault(m.DepartmentId)?.Name ?? string.Empty,
                m.PositionId,
                position?.Name,
                m.IsPrimary,
                m.EmployeeNumber,
                m.JobGrade,
                m.HiredAt,
                m.Order,
                m.CreatedAt);
        }).ToList();
    }
}
