// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 部门详情：<c>GET /api/departments/{id}</c>（祖先链、负责人登录名、子部门数、成员数）。
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。
/// </summary>
/// <param name="service">部门域服务。</param>
/// <param name="users">用户仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = OrganizationPermissions.DepartmentsRead)]
public sealed class GetDepartment(DepartmentService service, IRepository<SysUser, Guid> users) : IOperation<GetDepartmentRequest, DepartmentDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "departments.get";

    /// <inheritdoc />
    public async ValueTask<Result<DepartmentDetailResponse>> HandleAsync(GetDepartmentRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        if (!(await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var department))
        {
            return Error.NotFound(OrganizationErrors.DepartmentNotFound, OrganizationMessages.DepartmentNotFound, MessageArguments.Of(("id", request.Id)));
        }

        string? leaderUserName = null;
        if (department.LeaderUserId is { } leaderId && (await users.FindAsync(leaderId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var leader))
        {
            leaderUserName = leader.UserName;
        }

        var ancestors = await service.AncestorsAsync(department.Id, context.CancellationToken).ConfigureAwait(false);
        var childCount = await service.ChildCountAsync(department.Id, context.CancellationToken).ConfigureAwait(false);
        var memberCount = await service.MemberCountAsync(department.Id, context.CancellationToken).ConfigureAwait(false);
        return new DepartmentDetailResponse(DepartmentResponse.From(department), ancestors.Select(DepartmentResponse.From).ToList(), leaderUserName, childCount, memberCount);
    }
}
