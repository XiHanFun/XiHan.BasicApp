// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
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
/// 修改用户资料：<c>PUT /api/users/{id}</c>。租户主体只能改本租户的用户。
/// 字段安全（方案 5.9）：交回原值、脱敏值或空值的受保护字段视为没改、保留库里的原值；改了只读字段即拒绝。邮箱格式因此在决定之后才校验（脱敏后的邮箱不像邮箱）。
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

        var rules = await FieldSecurityQuery.ResolveAsync(context, IdentityFieldSecurity.Users).ConfigureAwait(false);
        var displayName = Decide(rules, IdentityFieldSecurity.DisplayName, user.DisplayName, request.DisplayName.Trim());
        var email = Decide(rules, IdentityFieldSecurity.Email, user.Email, Blank(request.Email));
        var phone = Decide(rules, IdentityFieldSecurity.PhoneNumber, user.PhoneNumber, Blank(request.PhoneNumber));
        foreach (var (field, decision) in new[] { (IdentityFieldSecurity.DisplayName, displayName), (IdentityFieldSecurity.Email, email), (IdentityFieldSecurity.PhoneNumber, phone) })
        {
            if (decision.Write == FieldWrite.Denied)
            {
                return Error.Denied(IdentityErrors.FieldReadOnly, IdentityMessages.FieldReadOnly, MessageArguments.Of(("field", field)));
            }
        }

        if (email.Write == FieldWrite.Accept && email.Value is { } newEmail && !UserEmail.IsValid(newEmail))
        {
            var violations = new[] { new SchemaViolation("$." + IdentityFieldSecurity.Email, IdentityMessages.EmailInvalid.L()) };
            return Error.Validation(KernelErrors.RequestInvalid, KernelMessages.RequestInvalid, MessageArguments.Of(("id", Id), ("reason", violations[0].ToString()), ("errors", violations)));
        }

        user.DisplayName = displayName.Value!;
        user.Email = email.Value;
        user.PhoneNumber = phone.Value;
        var updated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : UserResponse.From(user, rules);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static (FieldWrite Write, string? Value) Decide(FieldRules rules, string field, string? original, string? incoming)
    {
        var write = rules.OnUpdate(field, original, incoming);
        return (write, write == FieldWrite.Keep ? original : incoming);
    }
}
