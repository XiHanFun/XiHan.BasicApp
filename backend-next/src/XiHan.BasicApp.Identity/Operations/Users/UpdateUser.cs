// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Kernel.Validation;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 修改用户资料。
/// </summary>
/// <remarks>
/// <c>PUT /api/users/{id}</c>。租户主体只能改本租户的用户。
/// 字段安全（方案 5.9）：交回原值、脱敏值或空值的受保护字段视为没改、保留库里的原值；改了只读字段即拒绝。邮箱格式因此在决定之后才校验（脱敏后的邮箱不像邮箱）。
/// </remarks>
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

        // 字段安全（方案 5.9）：只读字段的改动在 Access 的中间件里拒绝，交回的脱敏值已换回原值
        var displayName = request.DisplayName.Trim();
        var email = Blank(request.Email);
        var phone = Blank(request.PhoneNumber);
        if (email is { } newEmail && !string.Equals(newEmail, user.Email, StringComparison.Ordinal) && !UserEmail.IsValid(newEmail))
        {
            var violations = new[] { new SchemaViolation("$." + IdentityFieldSecurity.Email, IdentityMessages.EmailInvalid.L()) };
            return Error.Validation(KernelErrors.RequestInvalid, KernelMessages.RequestInvalid, MessageArguments.Of(("id", Id), ("reason", violations[0].ToString()), ("errors", violations)));
        }

        user.DisplayName = displayName;
        // 管理员改了邮箱 / 手机号即复位验证状态（4.x 同），本人要重新收码验证
        user.EmailVerified &= string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        user.PhoneVerified &= string.Equals(user.PhoneNumber, phone, StringComparison.Ordinal);
        user.Email = email;
        user.PhoneNumber = phone;
        var updated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : UserResponse.From(user);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
