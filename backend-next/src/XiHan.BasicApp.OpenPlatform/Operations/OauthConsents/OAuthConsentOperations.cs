// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Domain.Services;
using XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;
using XiHan.BasicApp.OpenPlatform.Operations.MyOauthApps;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Operations.OauthConsents;

/// <summary>
/// 授权请求。
/// </summary>
/// <remarks>
/// 同意页把 <c>/api/oauth2/authorize</c> 的查询参数原样上送；4.x 漏了 nonce，id_token 里永远没有它。
/// </remarks>
/// <param name="ResponseType">response_type。</param>
/// <param name="ClientId">client_id。</param>
/// <param name="RedirectUri">redirect_uri。</param>
/// <param name="Scope">scope。</param>
/// <param name="State">state。</param>
/// <param name="CodeChallenge">code_challenge。</param>
/// <param name="CodeChallengeMethod">code_challenge_method。</param>
/// <param name="Nonce">nonce。</param>
public sealed record OAuthAuthorizeRequest(
    [StringLength(32)] string? ResponseType,
    [StringLength(100)] string? ClientId,
    [StringLength(500)] string? RedirectUri,
    [StringLength(200)] string? Scope,
    [StringLength(500)] string? State,
    [StringLength(128)] string? CodeChallenge,
    [StringLength(10)] string? CodeChallengeMethod,
    [StringLength(200)] string? Nonce)
{
    /// <summary>
    /// 转成校验的输入。
    /// </summary>
    public OAuthAuthorizeInput ToInput() => new(ResponseType, ClientId, RedirectUri, Scope, State, CodeChallenge, CodeChallengeMethod, Nonce);
}

/// <summary>
/// 同意页的预览：应用信息与本次的范围。
/// </summary>
/// <remarks>
/// 请求不对即带 OAuth 错误码。
/// </remarks>
/// <param name="Valid">请求是否有效。</param>
/// <param name="Error">OAuth 错误码。</param>
/// <param name="ErrorDescription">错误说明。</param>
/// <param name="ClientId">ClientId。</param>
/// <param name="AppName">应用名称。</param>
/// <param name="AppDescription">应用描述。</param>
/// <param name="Logo">Logo。</param>
/// <param name="Homepage">主页。</param>
/// <param name="SkipConsent">不必再确认（应用跳过同意页，或此前同意过的范围已覆盖本次的）。</param>
/// <param name="Scopes">本次的范围。</param>
public sealed record OAuthConsentPreviewResponse(bool Valid, string? Error, string? ErrorDescription, string? ClientId, string? AppName, string? AppDescription, string? Logo, string? Homepage, bool SkipConsent, IReadOnlyList<string> Scopes);

/// <summary>
/// 同意或拒绝之后要整页跳去的地址。
/// </summary>
/// <remarks>
/// 即应用的回调地址，带 code / error 与 state。
/// </remarks>
/// <param name="RedirectUri">地址。</param>
public sealed record OAuthConsentRedirectResponse(string RedirectUri);

/// <summary>
/// <c>oauth-consents.list</c> 的请求：没有输入。
/// </summary>
public sealed record OAuthConsentsRequest;

/// <summary>
/// 本人授权过的一个应用。
/// </summary>
/// <param name="ClientId">ClientId。</param>
/// <param name="AppName">应用名称（应用已删除为空）。</param>
/// <param name="Logo">Logo。</param>
/// <param name="Homepage">主页。</param>
/// <param name="Scopes">同意过的范围。</param>
/// <param name="GrantedAt">最近一次同意的时间。</param>
public sealed record OAuthConsentResponse(string ClientId, string? AppName, string? Logo, string? Homepage, IReadOnlyList<string> Scopes, DateTimeOffset GrantedAt);

/// <summary>
/// 本人授权过的全部应用（最近同意的在前）。
/// </summary>
/// <param name="Items">应用。</param>
public sealed record OAuthConsentsResponse(IReadOnlyList<OAuthConsentResponse> Items);

/// <summary>
/// <c>oauth-consents.revoke</c> 的请求。
/// </summary>
/// <param name="ClientId">ClientId。</param>
public sealed record RevokeOAuthConsentRequest([Required, StringLength(100)] string ClientId);

/// <summary>
/// <c>oauth-consents.revoke</c> 的结果。
/// </summary>
/// <param name="Revoked">是否撤销了（成功即 <see langword="true"/>）。</param>
public sealed record RevokeOAuthConsentResponse(bool Revoked);

/// <summary>
/// 同意页的预览。
/// </summary>
/// <remarks>
/// <c>GET /api/oauth-consents:resolve</c>（4.x <c>OAuthConnect/ResolveAuthorization</c>）。已登录即可，模仿会话不能授权第三方应用（4.x 同）。
/// </remarks>
/// <param name="authorizer">授权端点的规则。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ResolveOAuthConsent(OAuthAuthorizer authorizer) : IOperation<OAuthAuthorizeRequest, OAuthConsentPreviewResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-consents.resolve";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthConsentPreviewResponse>> HandleAsync(OAuthAuthorizeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: true);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var check = await authorizer.CheckAsync(request.ToInput(), context.CancellationToken).ConfigureAwait(false);
        var app = check.App;
        var skip = check.IsValid && await authorizer.ConsentedAsync(user.UserId, check, context.CancellationToken).ConfigureAwait(false);
        return new OAuthConsentPreviewResponse(check.IsValid, check.Error, check.ErrorDescription, app?.ClientId, app?.AppName, app?.AppDescription, app?.Logo, app?.Homepage, skip, check.Scopes);
    }
}

/// <summary>
/// 同意授权。
/// </summary>
/// <remarks>
/// <c>POST /api/oauth-consents:approve</c>（4.x <c>OAuthConnect/Authorize</c>）。发授权码、记下同意，返回带 code 与 state 的回调地址；请求不对即 <c>OPENPLATFORM_VALIDATION_014</c>。模仿会话不能做。
/// </remarks>
/// <param name="authorizer">授权端点的规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ApproveOAuthConsent(OAuthAuthorizer authorizer) : IOperation<OAuthAuthorizeRequest, OAuthConsentRedirectResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-consents.approve";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthConsentRedirectResponse>> HandleAsync(OAuthAuthorizeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: true);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var input = request.ToInput();
        var check = await authorizer.CheckAsync(input, context.CancellationToken).ConfigureAwait(false);
        if (!check.IsValid)
        {
            return Error.Validation(OpenPlatformErrors.AuthorizeRequestInvalid, OpenPlatformMessages.AuthorizeRequestInvalid, MessageArguments.Of(("error", check.Error), ("description", check.ErrorDescription)));
        }

        var approved = await authorizer.ApproveAsync(user.UserId, input, check, context.CancellationToken).ConfigureAwait(false);
        return approved.TryGetValue(out var redirect) ? new OAuthConsentRedirectResponse(redirect) : approved.Error!;
    }
}

/// <summary>
/// 拒绝授权。
/// </summary>
/// <remarks>
/// <c>POST /api/oauth-consents:deny</c>。客户端与回调地址都对上了才返回 <c>error=access_denied</c> 的回调地址（4.x 在页面上自己拼，没校验过的地址也会跳）；对不上即 <c>OPENPLATFORM_VALIDATION_014</c>。
/// </remarks>
/// <param name="authorizer">授权端点的规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DenyOAuthConsent(OAuthAuthorizer authorizer) : IOperation<OAuthAuthorizeRequest, OAuthConsentRedirectResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-consents.deny";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthConsentRedirectResponse>> HandleAsync(OAuthAuthorizeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var check = await authorizer.CheckAsync(request.ToInput(), context.CancellationToken).ConfigureAwait(false);
        if (!check.CanRedirect)
        {
            return Error.Validation(OpenPlatformErrors.AuthorizeRequestInvalid, OpenPlatformMessages.AuthorizeRequestInvalid, MessageArguments.Of(("error", check.Error), ("description", check.ErrorDescription)));
        }

        return new OAuthConsentRedirectResponse(OAuthAuthorizer.ErrorRedirect(request.RedirectUri!, OAuthErrors.AccessDenied, "The user denied the request.", request.State));
    }
}

/// <summary>
/// 列出本人授权过的应用。
/// </summary>
/// <remarks>
/// <c>GET /api/oauth-consents</c>。已登录即可。
/// </remarks>
/// <param name="consents">同意记录仓储。</param>
/// <param name="apps">应用仓储。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListOAuthConsents(IRepository<SysOAuthConsent, Guid> consents, IRepository<SysOAuthApp, Guid> apps) : IOperation<OAuthConsentsRequest, OAuthConsentsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-consents.list";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthConsentsResponse>> HandleAsync(OAuthConsentsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: false);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var userId = user.UserId;
        var rows = await consents.ListAsync(EntityQuery.All<SysOAuthConsent>().Where(new Specification<SysOAuthConsent>(c => c.UserId == userId)), context.CancellationToken).ConfigureAwait(false);
        var clientIds = rows.Select(static c => c.ClientId).Distinct(StringComparer.Ordinal).ToList();
        var known = clientIds.Count == 0
            ? []
            : (await apps.ListAsync(EntityQuery.All<SysOAuthApp>().Where(new Specification<SysOAuthApp>(a => clientIds.Contains(a.ClientId))), context.CancellationToken).ConfigureAwait(false)).ToDictionary(static a => a.ClientId, StringComparer.Ordinal);
        return new OAuthConsentsResponse([.. rows.OrderByDescending(static c => c.GrantedAt).Select(c =>
        {
            var app = known.GetValueOrDefault(c.ClientId);
            return new OAuthConsentResponse(c.ClientId, app?.AppName, app?.Logo, app?.Homepage, OAuthAppService.Split(c.Scopes), c.GrantedAt);
        })]);
    }
}

/// <summary>
/// 撤销对一个应用的授权。
/// </summary>
/// <remarks>
/// <c>POST /api/oauth-consents:revoke</c>。删掉同意记录并吊销这个应用拿到的本人的全部令牌；没授权过即 <c>OPENPLATFORM_NOTFOUND_002</c>。模仿会话不能做。
/// </remarks>
/// <param name="consents">同意记录仓储。</param>
/// <param name="tokens">令牌服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class RevokeOAuthConsent(IRepository<SysOAuthConsent, Guid> consents, OAuthTokenService tokens) : IOperation<RevokeOAuthConsentRequest, RevokeOAuthConsentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-consents.revoke";

    /// <inheritdoc />
    public async ValueTask<Result<RevokeOAuthConsentResponse>> HandleAsync(RevokeOAuthConsentRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: true);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var userId = user.UserId;
        var clientId = request.ClientId;
        var rows = await consents.ListAsync(EntityQuery.All<SysOAuthConsent>().Where(new Specification<SysOAuthConsent>(c => c.UserId == userId && c.ClientId == clientId)), context.CancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return Error.NotFound(OpenPlatformErrors.ConsentNotFound, OpenPlatformMessages.ConsentNotFound, MessageArguments.Of(("clientId", clientId)));
        }

        foreach (var row in rows)
        {
            var removed = await consents.RemoveAsync(row, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }
        }

        await tokens.RevokeAllAsync(userId, clientId, context.CancellationToken).ConfigureAwait(false);
        return new RevokeOAuthConsentResponse(true);
    }
}
