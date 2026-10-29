// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Domain.Services;
using XiHan.BasicApp.OpenPlatform.Operations.OauthApps;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Operations.MyOauthApps;

/// <summary>
/// <c>my-oauth-apps.list</c> 的请求：没有输入。
/// </summary>
public sealed record MyOAuthAppsRequest;

/// <summary>
/// 本人的全部应用（按创建先后）。
/// </summary>
/// <param name="Items">应用。</param>
public sealed record MyOAuthAppsResponse(IReadOnlyList<OAuthAppResponse> Items);

/// <summary>
/// <c>my-oauth-apps.create</c> 的请求。
/// </summary>
/// <remarks>
/// 4.x 同：授权类型、范围与有效期固定，不跳过同意页。
/// </remarks>
/// <param name="AppName">应用名称。</param>
/// <param name="ClientType">客户端类型（新建后不能改）。</param>
/// <param name="Homepage">主页。</param>
/// <param name="AppDescription">描述。</param>
/// <param name="RedirectUris">回调地址（1 到 10 条）。</param>
/// <param name="Logo">Logo 地址。</param>
public sealed record CreateMyOAuthAppRequest(
    [Required, StringLength(100, MinimumLength = 1)] string AppName,
    OAuthClientType ClientType,
    [StringLength(500)] string? Homepage,
    [StringLength(500)] string? AppDescription,
    [Required, MaxLength(20)] IReadOnlyList<string> RedirectUris,
    [StringLength(500)] string? Logo);

/// <summary>
/// <c>my-oauth-apps.update</c> 的请求。
/// </summary>
/// <remarks>
/// 只能改名称、主页、描述、回调地址、Logo 与启停，4.x 同。
/// </remarks>
/// <param name="Id">应用 ID。</param>
/// <param name="AppName">应用名称。</param>
/// <param name="Homepage">主页。</param>
/// <param name="AppDescription">描述。</param>
/// <param name="RedirectUris">回调地址。</param>
/// <param name="Logo">Logo 地址。</param>
/// <param name="IsEnabled">是否启用。</param>
public sealed record UpdateMyOAuthAppRequest(
    Guid Id,
    [Required, StringLength(100, MinimumLength = 1)] string AppName,
    [StringLength(500)] string? Homepage,
    [StringLength(500)] string? AppDescription,
    [Required, MaxLength(20)] IReadOnlyList<string> RedirectUris,
    [StringLength(500)] string? Logo,
    bool IsEnabled);

/// <summary>
/// 我的应用的主体：当前令牌的用户。
/// </summary>
/// <remarks>
/// 写操作模仿会话一律拒绝（<c>IDENTITY_CONFLICT_003</c>，4.x 同）。
/// </remarks>
internal static class MyOAuthAppSubject
{
    public static Result<(Guid UserId, string? UserName)> Resolve(InvocationContext context, bool rejectImpersonation)
    {
        var principal = context.GetSecurity().Principal;
        if (rejectImpersonation && principal.ClaimValues(IdentityClaims.Actor).Any())
        {
            return Error.Conflict(IdentityErrors.ImpersonationActive, IdentityMessages.ImpersonationActive);
        }

        return Guid.TryParse(principal.Id, out var userId)
            ? (userId, principal.ClaimValues(IdentityClaims.UserName).FirstOrDefault())
            : Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", principal.Id)));
    }

    /// <summary>
    /// 我的应用的固定内容。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
    public static OAuthAppDraft Draft(string appName, string? homepage, string? description, IReadOnlyList<string> redirectUris, string? logo, bool isEnabled)
        => new(appName, description, OAuthAppType.Web, OAuthAppService.PersonalGrantTypes, redirectUris, OAuthAppService.PersonalScopes, 3600, 2592000, 300, logo, homepage, SkipConsent: false, isEnabled, Remark: null);
}

/// <summary>
/// 列出本人的 OAuth 应用。
/// </summary>
/// <remarks>
/// <c>GET /api/my-oauth-apps</c>。已登录即可。
/// </remarks>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListMyOAuthApps(OAuthAppService service) : IOperation<MyOAuthAppsRequest, MyOAuthAppsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-oauth-apps.list";

    /// <inheritdoc />
    public async ValueTask<Result<MyOAuthAppsResponse>> HandleAsync(MyOAuthAppsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: false);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var rows = await service.ListOwnedAsync(user.UserId, context.CancellationToken).ConfigureAwait(false);
        return new MyOAuthAppsResponse([.. rows.Select(OAuthAppResponse.From)]);
    }
}

/// <summary>
/// 取本人的一个 OAuth 应用。
/// </summary>
/// <remarks>
/// <c>GET /api/my-oauth-apps/{id}</c>（别人的与平台的当作不存在）。
/// </remarks>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetMyOAuthApp(OAuthAppService service) : IOperation<OAuthAppIdRequest, OAuthAppResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-oauth-apps.get";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthAppResponse>> HandleAsync(OAuthAppIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: false);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var found = await service.FindOwnedAsync(user.UserId, request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var app) ? OAuthAppResponse.From(app) : found.Error!;
    }
}

/// <summary>
/// 新建本人的 OAuth 应用。
/// </summary>
/// <remarks>
/// <c>POST /api/my-oauth-apps</c>（ClientId 为 32 位十六进制；机密客户端的密钥只在这一次返回）。模仿会话不能做。
/// </remarks>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CreateMyOAuthApp(OAuthAppService service) : IOperation<CreateMyOAuthAppRequest, OAuthAppSecretResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-oauth-apps.create";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthAppSecretResponse>> HandleAsync(CreateMyOAuthAppRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: true);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var draft = MyOAuthAppSubject.Draft(request.AppName, request.Homepage, request.AppDescription, request.RedirectUris, request.Logo, isEnabled: true);
        var created = await service.CreateAsync(null, request.ClientType, draft, user.UserId, user.UserName, context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var pair) ? new OAuthAppSecretResponse(pair.App.Id, pair.App.ClientId, pair.App.ClientType, pair.Secret) : created.Error!;
    }
}

/// <summary>
/// 修改本人的 OAuth 应用。
/// </summary>
/// <remarks>
/// <c>PUT /api/my-oauth-apps/{id}</c>（名称、主页、描述、回调地址、Logo 与启停；授权类型、范围与有效期不变）。模仿会话不能做。
/// </remarks>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UpdateMyOAuthApp(OAuthAppService service) : IOperation<UpdateMyOAuthAppRequest, OAuthAppResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-oauth-apps.update";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthAppResponse>> HandleAsync(UpdateMyOAuthAppRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: true);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var found = await service.FindOwnedAsync(user.UserId, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var app))
        {
            return found.Error!;
        }

        // 授权类型、范围、有效期与同意页沿用应用自己的（平台可能为它调过），只换用户能改的几项
        var draft = new OAuthAppDraft(request.AppName, request.AppDescription, app.AppType, OAuthAppService.Split(app.GrantTypes), request.RedirectUris, OAuthAppService.Split(app.Scopes), app.AccessTokenLifetime, app.RefreshTokenLifetime, app.AuthorizationCodeLifetime, request.Logo, request.Homepage, app.SkipConsent, request.IsEnabled, app.Remark);
        var updated = await service.UpdateAsync(app, draft, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : OAuthAppResponse.From(app);
    }
}

/// <summary>
/// 重置本人 OAuth 应用的密钥。
/// </summary>
/// <remarks>
/// <c>POST /api/my-oauth-apps/{id}:reset-secret</c>（公开客户端 <c>OPENPLATFORM_CONFLICT_003</c>）。模仿会话不能做。
/// </remarks>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ResetMyOAuthAppSecret(OAuthAppService service) : IOperation<OAuthAppIdRequest, OAuthAppSecretResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-oauth-apps.reset-secret";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthAppSecretResponse>> HandleAsync(OAuthAppIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: true);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        var found = await service.FindOwnedAsync(user.UserId, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var app))
        {
            return found.Error!;
        }

        var reset = await service.ResetSecretAsync(app, context.CancellationToken).ConfigureAwait(false);
        return reset.TryGetValue(out var secret) ? new OAuthAppSecretResponse(app.Id, app.ClientId, app.ClientType, secret) : reset.Error!;
    }
}

/// <summary>
/// 删除本人的 OAuth 应用。
/// </summary>
/// <remarks>
/// <c>DELETE /api/my-oauth-apps?ids=</c>（别人的逐项 <c>OPENPLATFORM_NOTFOUND_001</c>）。模仿会话不能做。
/// </remarks>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DeleteMyOAuthApps(OAuthAppService service) : IOperation<OAuthAppIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-oauth-apps.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(OAuthAppIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = MyOAuthAppSubject.Resolve(context, rejectImpersonation: true);
        if (!subject.TryGetValue(out var user))
        {
            return subject.Error!;
        }

        return await service.DeleteAsync(request.Ids, user.UserId, context.CancellationToken).ConfigureAwait(false);
    }
}
