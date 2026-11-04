// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 按 ID 取用户
/// </summary>
/// <remarks>
/// <c>GET /api/users/{id}</c>。租户主体只能看自己租户的用户与外部成员；平台主体（无租户）能看全部
/// 只读目录类操作，同时作为 MCP / AI 工具暴露；与 <c>users.list</c> 一起是内部服务查用户目录的 gRPC 面
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="sideUsers">这一侧的用户</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Grpc | Expose.Mcp | Expose.Ai, Permission = IdentityPermissions.UsersRead)]
public sealed class GetUser(IRepository<SysUser, Guid> users, SideUsers sideUsers) : IOperation<GetUserRequest, UserResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "users.get";

    /// <inheritdoc />
    public async ValueTask<Result<UserResponse>> HandleAsync(GetUserRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenantId = context.GetSecurity().Tenant?.Id;
        var found = tenantId is null
            ? await users.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)
            : await sideUsers.FindAsync(request.Id, tenantId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user) || !(await DataScopeQuery.ResolveAsync(context).ConfigureAwait(false)).Allows(user.Id))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", request.Id)));
        }

        return UserResponse.From(user, tenantId);
    }
}
