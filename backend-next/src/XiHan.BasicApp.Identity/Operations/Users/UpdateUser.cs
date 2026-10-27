// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 修改用户资料：<c>PUT /api/users/{id}</c>。租户主体只能改本租户的用户。
/// </summary>
/// <param name="users">用户仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersUpdate)]
public sealed class UpdateUser(IRepository<SysUser, Guid> users) : IOperation<UpdateUserRequest, UserResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.update";

    /// <inheritdoc />
    public async ValueTask<Result<UserResponse>> HandleAsync(UpdateUserRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenantId = context.GetSecurity().Tenant?.Id;
        if (!(await users.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || (tenantId is not null && !string.Equals(user.HomeTenantId, tenantId, StringComparison.Ordinal)) || !(await DataScopeQuery.ResolveAsync(context).ConfigureAwait(false)).Allows(user.Id))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", request.Id)));
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        var updated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : UserResponse.From(user);
    }
}
