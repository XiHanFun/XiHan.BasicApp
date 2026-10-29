// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure.External;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 本人绑定的一个第三方账号。
/// </summary>
/// <param name="Provider">提供商类型。</param>
/// <param name="DisplayName">第三方的显示名。</param>
/// <param name="Email">第三方的邮箱。</param>
/// <param name="AvatarUrl">第三方的头像。</param>
/// <param name="BoundAt">绑定时间。</param>
/// <param name="LastLoginAt">最近一次用它登录的时间。</param>
public sealed record ExternalLoginResponse(string Provider, string? DisplayName, string? Email, string? AvatarUrl, DateTimeOffset BoundAt, DateTimeOffset? LastLoginAt);

/// <summary>
/// 本人绑定的全部第三方账号。
/// </summary>
/// <param name="Items">绑定。</param>
public sealed record ExternalLoginsResponse(IReadOnlyList<ExternalLoginResponse> Items);

/// <summary>
/// <c>profile.begin-external-bind</c> 的请求。
/// </summary>
/// <param name="Provider">提供方标识（登录页配置里的名称）。</param>
public sealed record BeginExternalBindRequest([Required, StringLength(32)] string Provider);

/// <summary>
/// 发起绑定的地址。
/// </summary>
/// <param name="Url">去第三方授权的地址（相对于后端根，带着一次性的绑定意图）。</param>
public sealed record BeginExternalBindResponse(string Url);

/// <summary>
/// <c>profile.link-external</c> 的请求。
/// </summary>
/// <param name="Ticket">绑定流程回调发给前端的一次性票据。</param>
public sealed record LinkExternalLoginRequest([Required, StringLength(128)] string Ticket);

/// <summary>
/// <c>profile.unlink-external</c> 的请求。
/// </summary>
/// <param name="Provider">提供商类型或名称。</param>
public sealed record UnlinkExternalLoginRequest([Required, StringLength(32)] string Provider);

/// <summary>
/// 本人绑定的第三方账号：<c>GET /api/profile:external-logins</c>（4.x <c>Profile/LinkedAccounts</c>）。已登录即可。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="bindings">第三方绑定仓储。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListExternalLogins(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserExternalLogin, Guid> bindings) : IOperation<ProfileRequest, ExternalLoginsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.external-logins";

    /// <inheritdoc />
    public async ValueTask<Result<ExternalLoginsResponse>> HandleAsync(ProfileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var userId = found.User.Id;
        var rows = await bindings.ListAsync(EntityQuery.All<SysUserExternalLogin>().Where(new Specification<SysUserExternalLogin>(b => b.UserId == userId)), context.CancellationToken).ConfigureAwait(false);
        return new ExternalLoginsResponse([.. rows.OrderBy(static b => b.CreatedAt).Select(static b => new ExternalLoginResponse(b.Provider, b.DisplayName, b.Email, b.AvatarUrl, b.CreatedAt, b.LastLoginAt))]);
    }
}

/// <summary>
/// 发起绑定：<c>POST /api/profile:begin-external-bind</c>。给当前登录的人领一个绑定意图（十分钟、用一次），返回带着它的发起地址；
/// 没有意图的绑定发起不认，回调的票据记着这个人（见 <see cref="ExternalLoginFlow"/>）。提供方不存在或没启用即 <c>IDENTITY_NOTFOUND_005</c>。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="options">第三方登录配置。</param>
/// <param name="cache">共享缓存（绑定意图）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class BeginExternalBind(IRepository<SysUser, Guid> users, UserAccountService accounts, ExternalLoginOptions options, ISharedCache cache) : IOperation<BeginExternalBindRequest, BeginExternalBindResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.begin-external-bind";

    /// <inheritdoc />
    public async ValueTask<Result<BeginExternalBindResponse>> HandleAsync(BeginExternalBindRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        if (options.Find(request.Provider) is not { } config)
        {
            return Error.NotFound(IdentityErrors.ExternalProviderNotFound, IdentityMessages.ExternalProviderNotFound, MessageArguments.Of(("provider", request.Provider)));
        }

        return new BeginExternalBindResponse(await ExternalLoginFlow.BeginBindAsync(cache, config, found.User.Id, context.CancellationToken).ConfigureAwait(false));
    }
}

/// <summary>
/// 确认绑定：<c>POST /api/profile:link-external</c>。凭绑定流程的一次性票据，把第三方账号绑到当前登录的人；票据记着发起绑定的人，不是本人即 <c>IDENTITY_VALIDATION_015</c>
/// （发起要本人领的意图、确认要本人：拿着别人发起的链接或别人完成的回调地址都绑不上）；
/// 已被同一注册地的别人绑定即 <c>IDENTITY_CONFLICT_008</c>，本人已绑过这个提供商的另一个账号即换成这一个。模仿会话不能做（4.x 同）。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="bindings">第三方绑定仓储。</param>
/// <param name="cache">共享缓存（票据）。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class LinkExternalLogin(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserExternalLogin, Guid> bindings, ISharedCache cache, IIdGenerator<Guid> ids) : IOperation<LinkExternalLoginRequest, ExternalLoginResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.link-external";

    /// <inheritdoc />
    public async ValueTask<Result<ExternalLoginResponse>> HandleAsync(LinkExternalLoginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var ticket = await ExternalLoginFlow.ConsumeTicketAsync(cache, request.Ticket, ExternalLoginFlow.BindMode, context.CancellationToken).ConfigureAwait(false);
        var user = found.User;
        if (ticket is null || ticket.UserId != user.Id)
        {
            return Error.Validation(IdentityErrors.ExternalTicketInvalid, IdentityMessages.ExternalTicketInvalid);
        }

        var tenant = user.HomeTenantId;
        var provider = ticket.ProviderType;
        var key = ticket.Identity.ProviderKey;
        var taken = await bindings.ListAsync(EntityQuery.All<SysUserExternalLogin>().Where(new Specification<SysUserExternalLogin>(b => b.HomeTenantId == tenant && b.Provider == provider && b.ProviderKey == key)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        if (taken.Count > 0 && taken[0].UserId != user.Id)
        {
            return Error.Conflict(IdentityErrors.ExternalLoginTaken, IdentityMessages.ExternalLoginTaken, MessageArguments.Of(("provider", ticket.Provider)));
        }

        var userId = user.Id;
        var existing = taken.Count > 0
            ? taken
            : await bindings.ListAsync(EntityQuery.All<SysUserExternalLogin>().Where(new Specification<SysUserExternalLogin>(b => b.UserId == userId && b.Provider == provider)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        var binding = existing.Count > 0 ? existing[0] : new SysUserExternalLogin { Id = ids.NewId(), UserId = userId, HomeTenantId = tenant, Provider = provider };
        binding.ProviderKey = key;
        binding.DisplayName = Clip(ticket.Identity.DisplayName, 200);
        binding.Email = Clip(ticket.Identity.Email, 200);
        binding.AvatarUrl = Clip(ticket.Identity.AvatarUrl, 500);
        var saved = existing.Count > 0
            ? await bindings.UpdateAsync(binding, context.CancellationToken).ConfigureAwait(false)
            : await bindings.AddAsync(binding, context.CancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? saved.Error! : new ExternalLoginResponse(binding.Provider, binding.DisplayName, binding.Email, binding.AvatarUrl, binding.CreatedAt, binding.LastLoginAt);
    }

    private static string? Clip(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}

/// <summary>
/// 解绑：<c>POST /api/profile:unlink-external</c>（4.x <c>Profile/UnlinkAccount</c>）。没绑过即 <c>IDENTITY_NOTFOUND_004</c>。
/// 账号都有密码（不自动建号），不需要 4.x「这是唯一的登录方式」的限制。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="bindings">第三方绑定仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UnlinkExternalLogin(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserExternalLogin, Guid> bindings) : IOperation<UnlinkExternalLoginRequest, ExternalLoginsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.unlink-external";

    /// <inheritdoc />
    public async ValueTask<Result<ExternalLoginsResponse>> HandleAsync(UnlinkExternalLoginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var userId = found.User.Id;
        var provider = ExternalProviderTypes.Normalize(request.Provider) ?? request.Provider.Trim().ToLowerInvariant();
        var rows = await bindings.ListAsync(EntityQuery.All<SysUserExternalLogin>().Where(new Specification<SysUserExternalLogin>(b => b.UserId == userId)), context.CancellationToken).ConfigureAwait(false);
        var matched = rows.Where(b => b.Provider == provider).ToList();
        if (matched.Count == 0)
        {
            return Error.NotFound(IdentityErrors.ExternalLoginNotFound, IdentityMessages.ExternalLoginNotFound, MessageArguments.Of(("provider", request.Provider)));
        }

        foreach (var binding in matched)
        {
            var removed = await bindings.RemoveAsync(binding, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }
        }

        return new ExternalLoginsResponse([.. rows.Except(matched).OrderBy(static b => b.CreatedAt).Select(static b => new ExternalLoginResponse(b.Provider, b.DisplayName, b.Email, b.AvatarUrl, b.CreatedAt, b.LastLoginAt))]);
    }
}
