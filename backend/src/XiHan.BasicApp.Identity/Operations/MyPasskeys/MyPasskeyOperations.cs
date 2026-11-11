// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Profile;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.MyPasskeys;

/// <summary>
/// 列出本人的通行密钥
/// </summary>
/// <remarks>
/// <c>GET /api/my-passkeys</c>。已登录即可；模仿会话看到的是被模仿人的
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="passkeys">通行密钥的仪式</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListMyPasskeys(IRepository<SysUser, Guid> users, UserAccountService accounts, PasskeyCeremonies passkeys) : IOperation<MyPasskeysRequest, MyPasskeysResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "my-passkeys.list";

    /// <inheritdoc />
    public async ValueTask<Result<MyPasskeysResponse>> HandleAsync(MyPasskeysRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var rows = await passkeys.ListAsync(found.User.Id, context.CancellationToken).ConfigureAwait(false);
        return new MyPasskeysResponse([.. rows.Select(MyPasskeyResponse.From)]);
    }
}

/// <summary>
/// 发起登记通行密钥
/// </summary>
/// <remarks>
/// <c>POST /api/my-passkeys:begin-create</c>。先核对当前密码（错即 <c>IDENTITY_DENIED_003</c>），再给创建选项与一次性票据（按选项的超时过期）
/// 已登记的不再重复登记（选项列出排除的凭据）；到了上限即 <c>IDENTITY_CONFLICT_017</c>。宿主没配或登录方式没开放时不可用。模仿会话不能做
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="passkeys">通行密钥的仪式</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class BeginCreateMyPasskey(IRepository<SysUser, Guid> users, UserAccountService accounts, PasskeyCeremonies passkeys) : IOperation<BeginCreateMyPasskeyRequest, BeginCreateMyPasskeyResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "my-passkeys.begin-create";

    /// <inheritdoc />
    public async ValueTask<Result<BeginCreateMyPasskeyResponse>> HandleAsync(BeginCreateMyPasskeyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var open = await passkeys.EnsureOpenAsync(context).ConfigureAwait(false);
        if (!open.TryGetValue(out var ceremonies))
        {
            return open.Error!;
        }

        if (!PasswordHasher.Verify(request.CurrentPassword, found.Security.PasswordHash))
        {
            return Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
        }

        var registered = await passkeys.ListAsync(found.User.Id, context.CancellationToken).ConfigureAwait(false);
        if (registered.Count >= PasskeyCeremonies.MaxPerUser)
        {
            return Error.Conflict(IdentityErrors.PasskeyLimitReached, IdentityMessages.PasskeyLimitReached, MessageArguments.Of(("max", PasskeyCeremonies.MaxPerUser)));
        }

        var (ticket, options) = await passkeys.BeginRegistrationAsync(ceremonies, found.User, registered, context.CancellationToken).ConfigureAwait(false);
        return new BeginCreateMyPasskeyResponse(ticket, options);
    }
}

/// <summary>
/// 确认登记通行密钥
/// </summary>
/// <remarks>
/// <c>POST /api/my-passkeys</c>。票据须是本人发起登记时领的（否则 <c>IDENTITY_VALIDATION_028</c>），凭据由框架核对来源、挑战、依赖方与用户验证（失败即框架的 <c>SECURITY_*</c> 错误）
/// 同一凭据已登记即 <c>IDENTITY_CONFLICT_018</c>，到了上限即 <c>IDENTITY_CONFLICT_017</c>。登记成功给本人发一条安全通知。模仿会话不能做
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="passkeys">通行密钥的仪式</param>
/// <param name="rows">通行密钥仓储</param>
/// <param name="notices">发给本人的安全通知</param>
/// <param name="ids">标识生成器</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CreateMyPasskey(IRepository<SysUser, Guid> users, UserAccountService accounts, PasskeyCeremonies passkeys, IRepository<SysUserPasskey, Guid> rows, SecurityNotices notices, IIdGenerator<Guid> ids) : IOperation<CreateMyPasskeyRequest, MyPasskeyResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "my-passkeys.create";

    /// <inheritdoc />
    public async ValueTask<Result<MyPasskeyResponse>> HandleAsync(CreateMyPasskeyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var open = await passkeys.EnsureOpenAsync(context).ConfigureAwait(false);
        if (!open.TryGetValue(out var ceremonies))
        {
            return open.Error!;
        }

        var user = found.User;
        if (await passkeys.ConsumeRegistrationAsync(request.Ticket, user.Id, context.CancellationToken).ConfigureAwait(false) is not { } challenge)
        {
            return Error.Validation(IdentityErrors.PasskeyTicketInvalid, IdentityMessages.PasskeyTicketInvalid);
        }

        var verified = ceremonies.VerifyRegistration(request.Credential, challenge);
        if (!verified.TryGetValue(out var credential))
        {
            return verified.Error!;
        }

        var registered = await passkeys.ListAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (registered.Count >= PasskeyCeremonies.MaxPerUser)
        {
            return Error.Conflict(IdentityErrors.PasskeyLimitReached, IdentityMessages.PasskeyLimitReached, MessageArguments.Of(("max", PasskeyCeremonies.MaxPerUser)));
        }

        if (await passkeys.FindAsync(credential.CredentialId, context.CancellationToken).ConfigureAwait(false) is not null)
        {
            return Error.Conflict(IdentityErrors.PasskeyTaken, IdentityMessages.PasskeyTaken);
        }

        var passkey = new SysUserPasskey
        {
            Id = ids.NewId(),
            UserId = user.Id,
            HomeTenantId = user.HomeTenantId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? context.Localize(IdentityMessages.PasskeyDefaultName) : request.Name.Trim(),
            CredentialId = credential.CredentialId,
            PublicKey = credential.PublicKey,
            Algorithm = credential.Algorithm,
            SignCount = credential.SignCount,
            Aaguid = credential.Aaguid,
            BackupEligible = credential.BackupEligible,
            BackedUp = credential.BackedUp,
            Transports = credential.Transports.Count == 0 ? null : string.Join(',', credential.Transports),
        };
        var added = await rows.AddAsync(passkey, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        await notices.PasskeyAddedAsync(user, passkey, context).ConfigureAwait(false);
        return MyPasskeyResponse.From(passkey);
    }
}

/// <summary>
/// 改本人一个通行密钥的名字
/// </summary>
/// <remarks>
/// <c>PUT /api/my-passkeys/{id}</c>。不是本人的即 <c>IDENTITY_NOTFOUND_006</c>。模仿会话不能做
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="rows">通行密钥仓储</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UpdateMyPasskey(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserPasskey, Guid> rows) : IOperation<UpdateMyPasskeyRequest, MyPasskeyResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "my-passkeys.update";

    /// <inheritdoc />
    public async ValueTask<Result<MyPasskeyResponse>> HandleAsync(UpdateMyPasskeyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var owned = await MyPasskeyRows.FindOwnedAsync(rows, found.User.Id, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!owned.TryGetValue(out var passkey))
        {
            return owned.Error!;
        }

        passkey.Name = request.Name.Trim();
        var updated = await rows.UpdateAsync(passkey, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : MyPasskeyResponse.From(passkey);
    }
}

/// <summary>
/// 删除本人的通行密钥
/// </summary>
/// <remarks>
/// <c>DELETE /api/my-passkeys?ids=</c>（删除后这个通行密钥立即不能登录；认证器上的那份要本人在设备上删）。模仿会话不能做
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="rows">通行密钥仓储</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DeleteMyPasskeys(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserPasskey, Guid> rows) : IOperation<MyPasskeyIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "my-passkeys.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MyPasskeyIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            var owned = await MyPasskeyRows.FindOwnedAsync(rows, found.User.Id, id, context.CancellationToken).ConfigureAwait(false);
            if (!owned.TryGetValue(out var passkey))
            {
                failed.Add(BatchFailure.From(id, owned.Error!));
            }
            else if ((await rows.RemoveAsync(passkey, context.CancellationToken).ConfigureAwait(false)) is { IsFailure: true } removed)
            {
                failed.Add(BatchFailure.From(id, removed.Error!));
            }
            else
            {
                succeeded.Add(id);
            }
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 按本人取通行密钥
/// </summary>
internal static class MyPasskeyRows
{
    /// <summary>
    /// 取本人的一个通行密钥
    /// </summary>
    /// <returns>通行密钥；不存在或不是本人的即 <c>IDENTITY_NOTFOUND_006</c></returns>
    public static async Task<Result<SysUserPasskey>> FindOwnedAsync(IRepository<SysUserPasskey, Guid> rows, Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var found = await rows.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var passkey) && passkey.UserId == userId
            ? passkey
            : Error.NotFound(IdentityErrors.PasskeyNotFound, IdentityMessages.PasskeyNotFound, MessageArguments.Of(("id", id)));
    }
}
