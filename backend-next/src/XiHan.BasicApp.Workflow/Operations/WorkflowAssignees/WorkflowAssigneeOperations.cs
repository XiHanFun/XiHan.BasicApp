// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Organization;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Workflow.Operations.WorkflowAssignees;

/// <summary>
/// 选用户
/// </summary>
/// <param name="Keyword">关键字（登录名、显示名）</param>
/// <param name="Ids">按 ID 回显（给了就不按关键字搜）</param>
/// <param name="Limit">按关键字搜时最多返回几个（1～200，缺省 50）</param>
public sealed record ListAssigneeUsersRequest([property: StringLength(64)] string? Keyword = null, [property: MaxLength(200)] IReadOnlyList<Guid>? Ids = null, [property: Range(1, ListAssigneeUsers.MaxLimit)] int? Limit = null);

/// <summary>
/// 可选的用户
/// </summary>
/// <param name="Items">用户</param>
public sealed record AssigneeUsersResponse(IReadOnlyList<AssigneeUser> Items);

/// <summary>
/// 一个可选的用户
/// </summary>
/// <param name="Id">用户 ID</param>
/// <param name="UserName">登录名</param>
/// <param name="DisplayName">显示名</param>
public sealed record AssigneeUser(Guid Id, string UserName, string DisplayName);

/// <summary>
/// 选角色、岗位或部门
/// </summary>
public sealed record ListAssigneeOptionsRequest;

/// <summary>
/// 可选的角色或岗位
/// </summary>
/// <param name="Items">选项</param>
public sealed record AssigneeOptionsResponse(IReadOnlyList<AssigneeOption> Items);

/// <summary>
/// 一个可选的角色或岗位
/// </summary>
/// <param name="Id">ID</param>
/// <param name="Code">编码（受理人规则按编码引用）</param>
/// <param name="Name">名称</param>
public sealed record AssigneeOption(Guid Id, string Code, string Name);

/// <summary>
/// 可选的部门
/// </summary>
/// <param name="Items">部门的平铺清单，前端按上级 ID 组树</param>
public sealed record AssigneeDepartmentsResponse(IReadOnlyList<AssigneeDepartment> Items);

/// <summary>
/// 一个可选的部门
/// </summary>
/// <param name="Id">部门 ID</param>
/// <param name="ParentId">上级部门 ID</param>
/// <param name="Code">编码（受理人规则按编码引用）</param>
/// <param name="Name">名称</param>
/// <param name="IsActive">是否启用</param>
public sealed record AssigneeDepartment(Guid Id, Guid? ParentId, string Code, string Name, bool IsActive);

/// <summary>
/// 选用户
/// </summary>
/// <remarks>
/// <c>GET /api/workflow-assignees:users</c>。照 4.x 只要登录（转办、加签也要选人），只返回 ID、登录名与显示名，不带联系方式
/// 按关键字搜的是当前一侧可见的在用账号（运维支持成员不算），缺省 50 个、至多 200 个；按 ID 回显的是这一侧可见的账号，含停用的（已经选过的人照样念得出名字）
/// </remarks>
/// <param name="users">用户目录</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Policy = Policies.Authenticated)]
public sealed class ListAssigneeUsers(IUserDirectory users) : IOperation<ListAssigneeUsersRequest, AssigneeUsersResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "workflow-assignees.users";

    /// <summary>
    /// 一次搜索缺省返回的用户数
    /// </summary>
    public const int DefaultLimit = 50;

    /// <summary>
    /// 一次搜索最多返回的用户数
    /// </summary>
    public const int MaxLimit = 200;

    /// <inheritdoc />
    public async ValueTask<Result<AssigneeUsersResponse>> HandleAsync(ListAssigneeUsersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var side = WorkflowCaller.Of(context).Side;
        if (request.Ids is { Count: > 0 } ids)
        {
            var visible = await users.InSideAsync(side, ids, context.CancellationToken).ConfigureAwait(false);
            var found = await users.FindAsync([.. visible], context.CancellationToken).ConfigureAwait(false);
            return new AssigneeUsersResponse(ids.Distinct().Where(found.ContainsKey).Select(id => found[id]).Select(static u => new AssigneeUser(u.Id, u.UserName, u.DisplayName)).ToList());
        }

        var matched = await users.SearchActiveInSideAsync(side, request.Keyword, request.Limit ?? DefaultLimit, context.CancellationToken).ConfigureAwait(false);
        return new AssigneeUsersResponse(matched.Select(static u => new AssigneeUser(u.Id, u.UserName, u.DisplayName)).ToList());
    }
}

/// <summary>
/// 选角色
/// </summary>
/// <remarks>
/// <c>GET /api/workflow-assignees:roles</c>。当前一侧启用的角色（受理人规则按编码引用）
/// </remarks>
/// <param name="roles">角色成员目录</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Permission = WorkflowPermissions.AssigneesRead)]
public sealed class ListAssigneeRoles(IRoleMemberDirectory roles) : IOperation<ListAssigneeOptionsRequest, AssigneeOptionsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "workflow-assignees.roles";

    /// <inheritdoc />
    public async ValueTask<Result<AssigneeOptionsResponse>> HandleAsync(ListAssigneeOptionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = await roles.RolesAsync(context).ConfigureAwait(false);
        return new AssigneeOptionsResponse(options.Select(static r => new AssigneeOption(r.Id, r.Code, r.Name)).ToList());
    }
}

/// <summary>
/// 选岗位
/// </summary>
/// <remarks>
/// <c>GET /api/workflow-assignees:positions</c>。本租户启用的岗位；平台一侧没有组织，为空
/// </remarks>
/// <param name="organization">组织目录</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Permission = WorkflowPermissions.AssigneesRead)]
public sealed class ListAssigneePositions(IOrganizationDirectory organization) : IOperation<ListAssigneeOptionsRequest, AssigneeOptionsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "workflow-assignees.positions";

    /// <inheritdoc />
    public async ValueTask<Result<AssigneeOptionsResponse>> HandleAsync(ListAssigneeOptionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = await organization.PositionsAsync(context).ConfigureAwait(false);
        return new AssigneeOptionsResponse(options.Select(static p => new AssigneeOption(p.Id, p.Code, p.Name)).ToList());
    }
}

/// <summary>
/// 选部门
/// </summary>
/// <remarks>
/// <c>GET /api/workflow-assignees:departments</c>。本租户的部门（含停用的，前端标出）；平台一侧没有组织，为空
/// </remarks>
/// <param name="organization">组织目录</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Permission = WorkflowPermissions.AssigneesRead)]
public sealed class ListAssigneeDepartments(IOrganizationDirectory organization) : IOperation<ListAssigneeOptionsRequest, AssigneeDepartmentsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "workflow-assignees.departments";

    /// <inheritdoc />
    public async ValueTask<Result<AssigneeDepartmentsResponse>> HandleAsync(ListAssigneeOptionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = await organization.DepartmentsAsync(context).ConfigureAwait(false);
        return new AssigneeDepartmentsResponse(options.Select(static d => new AssigneeDepartment(d.Id, d.ParentId, d.Code, d.Name, d.IsActive)).ToList());
    }
}
